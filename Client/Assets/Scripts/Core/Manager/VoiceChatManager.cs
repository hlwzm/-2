using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace Jx3.Core
{
    /// <summary>
    /// 语音聊天管理器
    /// - 基于Unity Microphone API录制/播放
    /// - PCM编码分包(每帧50ms @16000Hz)
    /// - 按键说话(Push-to-Talk, 默认V键)
    /// - 语音激活指示 + 音量检测(静音门限)
    /// - 频道切换(跟随当前聊天频道)
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance { get; private set; } = null!;

        // ===== 常量 =====
        public const int SampleRate = 16000;          // 录音采样率
        public const int Channels = 1;                // 单声道
        public const int FrameMs = 50;                // 每帧时长(ms)
        public const int FrameSamples = SampleRate * FrameMs / 1000; // 每帧采样数 800
        public const float SilenceThreshold = 0.005f; // 静音门限
        public const int MaxRecordingSeconds = 30;    // 最长录音30秒
        public const int VoiceRoomMaxPlayers = 8;      // 每房间最多8人

        // ===== 按键设置 =====
        public KeyCode PushToTalkKey = KeyCode.V;
        public bool IsPressingTalk { get; private set; }

        // ===== 状态 =====
        public bool IsRecording { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool IsVoiceActive { get; private set; } // 说话或接收中
        public float CurrentVolume { get; private set; } // 当前音量(0~1)
        public int CurrentChannel { get; set; } // 跟随聊天频道

        // ===== 事件 =====
        public event Action? OnVoiceStarted;     // 开始说话
        public event Action? OnVoiceStopped;     // 停止说话
        public event Action<ulong, byte[]>? OnVoiceDataReceived; // 收到语音数据(发送者ID, PCM帧)

        // ===== 内部 =====
        private AudioClip? _recordingClip;
        private int _recordingPos;
        private string? _selectedMic;
        private Thread? _encodeThread;
        private volatile bool _encodeRunning;

        private readonly Queue<VoicePacket> _playQueue = new();
        private AudioSource? _playSource;
        private GameObject? _audioGo;

        private float _lastVolumeCheck;

        // ===== 网络UDP =====
        private System.Net.Sockets.UdpClient? _udpClient;
        private string _voiceServerHost = "127.0.0.1";
        private int _voiceServerPort = 9100;
        private bool _udpConnected;

        // ===== 消息 =====
        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // 创建用于播放的AudioSource
            _audioGo = new GameObject("VoicePlayback");
            _audioGo.transform.SetParent(transform);
            _playSource = _audioGo.AddComponent<AudioSource>();
            _playSource.loop = false;
            _playSource.playOnAwake = false;
            _playSource.volume = 1.0f;

            // 检查麦克风
            if (Microphone.devices.Length > 0)
                _selectedMic = Microphone.devices[0];

            // 连接语音UDP服务器
            ConnectVoiceServer();
        }

        void Update()
        {
            // ===== 按键检测 =====
            bool pressing = Input.GetKey(PushToTalkKey);

            if (pressing && !IsPressingTalk)
            {
                // 按下 — 开始录音
                IsPressingTalk = true;
                StartRecording();
            }
            else if (!pressing && IsPressingTalk)
            {
                // 松开 — 停止录音
                IsPressingTalk = false;
                StopRecording();
            }

            // ===== 播放队列 =====
            if (_playQueue.Count > 0 && !_playSource!.isPlaying)
            {
                var packet = _playQueue.Dequeue();
                PlayVoicePacket(packet);
            }

            // ===== 语音激活指示 =====
            IsVoiceActive = IsRecording || _playSource!.isPlaying;

            // ===== 更新UDP =====
            if (_udpConnected)
                ReceiveUdpPackets();
        }

        void OnDestroy()
        {
            _encodeRunning = false;

            if (Microphone.IsRecording(_selectedMic))
                Microphone.End(_selectedMic);

            _udpClient?.Close();
        }

        // ===== 麦克风录制 =====

        public void StartRecording()
        {
            if (_selectedMic == null)
            {
                Debug.LogWarning("[Voice] 未找到麦克风设备");
                return;
            }

            // 停止之前的录制
            if (Microphone.IsRecording(_selectedMic))
                Microphone.End(_selectedMic);

            _recordingClip = Microphone.Start(_selectedMic, true, MaxRecordingSeconds, SampleRate);
            _recordingPos = 0;

            if (_recordingClip == null)
            {
                Debug.LogError("[Voice] Microphone.Start 失败");
                return;
            }

            IsRecording = true;
            OnVoiceStarted?.Invoke();
            Debug.Log("[Voice] 开始录音");

            // 启动编码线程
            _encodeRunning = true;
            _encodeThread = new Thread(EncodeLoop) { IsBackground = true };
            _encodeThread.Start();
        }

        public void StopRecording()
        {
            if (!IsRecording) return;

            IsRecording = false;
            _encodeRunning = false;
            _encodeThread?.Join(500);
            _encodeThread = null;

            if (_selectedMic != null && Microphone.IsRecording(_selectedMic))
                Microphone.End(_selectedMic);

            OnVoiceStopped?.Invoke();
            Debug.Log("[Voice] 停止录音");
        }

        // ===== PCM编码线程 =====

        private void EncodeLoop()
        {
            float[] buffer = new float[FrameSamples];

            while (_encodeRunning && IsRecording && _recordingClip != null)
            {
                int currentPos = Microphone.GetPosition(_selectedMic);
                int availableSamples = currentPos - _recordingPos;

                if (availableSamples < 0)
                    availableSamples += _recordingClip.samples;

                if (availableSamples < FrameSamples)
                {
                    Thread.Sleep(10);
                    continue;
                }

                // 读取一帧音频数据
                if (_recordingPos + FrameSamples <= _recordingClip.samples)
                {
                    _recordingClip.GetData(buffer, _recordingPos);
                    _recordingPos += FrameSamples;
                }
                else
                {
                    // 环绕读取
                    int firstPart = _recordingClip.samples - _recordingPos;
                    var buf1 = new float[firstPart];
                    var buf2 = new float[FrameSamples - firstPart];
                    _recordingClip.GetData(buf1, _recordingPos);
                    _recordingClip.GetData(buf2, 0);
                    buf1.CopyTo(buffer, 0);
                    buf2.CopyTo(buffer, firstPart);
                    _recordingPos = FrameSamples - firstPart;
                }

                // 音量检测
                float sum = 0;
                for (int i = 0; i < FrameSamples; i++)
                    sum += Mathf.Abs(buffer[i]);
                CurrentVolume = sum / FrameSamples;

                // 静音检测 - 低于门限则跳过发送
                if (CurrentVolume < SilenceThreshold)
                    continue;

                // PCM 16bit编码
                byte[] pcmData = ConvertFloatToPcm16(buffer);

                // 发送到UDP服务器
                SendVoicePacket(pcmData);

                // 通知数据接收(本地回环用于测试)
                OnVoiceDataReceived?.Invoke(GameManager.Instance.Player.PlayerId, pcmData);
            }
        }

        // ===== PCM转换 =====

        private static byte[] ConvertFloatToPcm16(float[] samples)
        {
            var bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short pcm = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
                bytes[i * 2] = (byte)(pcm & 0xFF);
                bytes[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }
            return bytes;
        }

        private static float[] ConvertPcm16ToFloat(byte[] pcmData)
        {
            int sampleCount = pcmData.Length / 2;
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short pcm = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
                samples[i] = pcm / (float)short.MaxValue;
            }
            return samples;
        }

        // ===== 语音播放 =====

        public void EnqueueVoicePacket(ulong senderId, byte[] pcmData)
        {
            _playQueue.Enqueue(new VoicePacket { SenderId = senderId, PcmData = pcmData });
        }

        private void PlayVoicePacket(VoicePacket packet)
        {
            if (_playSource == null) return;

            float[] samples = ConvertPcm16ToFloat(packet.PcmData);

            // 创建临时AudioClip播放
            var clip = AudioClip.Create("VoiceFrame", FrameSamples, Channels, SampleRate, false);
            clip.SetData(samples, 0);
            _playSource.PlayOneShot(clip);
            Destroy(clip, (float)FrameMs / 1000 + 0.1f);
        }

        // ===== UDP网络 =====

        private void ConnectVoiceServer()
        {
            try
            {
                _udpClient = new System.Net.Sockets.UdpClient();
                _udpClient.Connect(_voiceServerHost, _voiceServerPort);
                _udpClient.BeginReceive(OnUdpReceive, null);
                _udpConnected = true;
                Debug.Log($"[Voice] UDP连接 {_voiceServerHost}:{_voiceServerPort}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Voice] UDP连接失败: {ex.Message}");
            }
        }

        private void SendVoicePacket(byte[] pcmData)
        {
            if (_udpClient == null || !_udpConnected) return;

            try
            {
                // 打包: [4字节PlayerId][2字节数据长度][PCM数据]
                using var ms = new MemoryStream();
                using var w = new BinaryWriter(ms);
                w.Write(GameManager.Instance.Player.PlayerId);
                w.Write((ushort)pcmData.Length);
                w.Write(pcmData);
                byte[] packet = ms.ToArray();
                _udpClient.Send(packet, packet.Length);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Voice] UDP发送失败: {ex.Message}");
            }
        }

        private void ReceiveUdpPackets()
        {
            // 在Update中轮询处理UDP接收(非阻塞)
        }

        private void OnUdpReceive(IAsyncResult ar)
        {
            if (_udpClient == null) return;

            try
            {
                var remoteEp = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);
                byte[] data = _udpClient.EndReceive(ar, ref remoteEp);

                if (data != null && data.Length > 6)
                {
                    using var ms = new MemoryStream(data);
                    using var r = new BinaryReader(ms);
                    ulong senderId = r.ReadUInt64();
                    ushort dataLen = r.ReadUInt16();
                    byte[] pcmData = r.ReadBytes(dataLen);

                    // 主线程入队
                    MainThreadDispatch(() =>
                    {
                        EnqueueVoicePacket(senderId, pcmData);
                    });
                }

                // 继续接收
                _udpClient.BeginReceive(OnUdpReceive, null);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[Voice] UDP接收错误: {ex.Message}");
            }
        }

        // ===== 主线程分发 =====

        private readonly Queue<Action> _mainThreadActions = new();

        private void MainThreadDispatch(Action action)
        {
            lock (_mainThreadActions)
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        private void LateUpdate()
        {
            lock (_mainThreadActions)
            {
                while (_mainThreadActions.Count > 0)
                    _mainThreadActions.Dequeue()?.Invoke();
            }
        }

        // ===== 工具方法 =====

        /// <summary>获取当前录音设备名</summary>
        public string GetMicDevice() => _selectedMic ?? "无麦克风";

        /// <summary>设置语音服务器地址</summary>
        public void SetVoiceServer(string host, int port)
        {
            _voiceServerHost = host;
            _voiceServerPort = port;
            _udpClient?.Close();
            _udpConnected = false;
            ConnectVoiceServer();
        }

        /// <summary>切换说话按键</summary>
        public void SetPushToTalkKey(KeyCode key) => PushToTalkKey = key;
    }

    /// <summary>语音数据包</summary>
    public class VoicePacket
    {
        public ulong SenderId;
        public byte[] PcmData = Array.Empty<byte>();
    }
}
