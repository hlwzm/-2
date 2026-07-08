using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Jx3.Core.Network
{
    public class NetworkClient : IDisposable
    {
        private TcpClient _client = new();
        private NetworkStream? _stream;
        private readonly byte[] _readBuffer = new byte[1024 * 64];
        private readonly Queue<(uint MsgId, byte[] Body)> _messageQueue = new();
        private CancellationTokenSource? _cts;
        private bool _connected;

        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<uint, byte[]>? OnMessage;

        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();
                _connected = true;
                _cts = new CancellationTokenSource();
                _ = ReceiveLoopAsync(_cts.Token);
                Debug.Log($"[Network] Connected to {host}:{port}");
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Connect failed: {ex.Message}");
            }
        }

        public async Task SendAsync(uint msgId, byte[] body)
        {
            if (_stream == null || !_connected) return;
            try
            {
                // 4字节长度前缀 + MessagePacket
                var packet = new MessagePacket { MsgId = msgId, Body = body };
                var data = packet.Encode();
                var lenBytes = BitConverter.GetBytes(data.Length);
                await _stream.WriteAsync(lenBytes);
                await _stream.WriteAsync(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Send failed: {ex.Message}");
                Disconnect();
            }
        }

        public void Send(uint msgId, byte[] body)
        {
            _ = SendAsync(msgId, body);
        }

        public void Update()
        {
            while (_messageQueue.Count > 0)
            {
                var (msgId, msgBody) = _messageQueue.Dequeue();
                OnMessage?.Invoke(msgId, msgBody);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var headerBuf = new byte[4];
            try
            {
                while (!ct.IsCancellationRequested && _connected)
                {
                    // 读4字节长度
                    var read = await _stream!.ReadAsync(headerBuf, 0, 4, ct);
                    if (read < 4) break;
                    var bodyLen = BitConverter.ToUInt32(headerBuf, 0);
                    if (bodyLen == 0 || bodyLen > 65536) break;

                    // 读消息体
                    var bodyBuf = new byte[bodyLen];
                    var totalRead = 0;
                    while (totalRead < bodyLen)
                    {
                        read = await _stream.ReadAsync(bodyBuf, totalRead, (int)(bodyLen - totalRead), ct);
                        if (read == 0) break;
                        totalRead += read;
                    }
                    if (totalRead < bodyLen) break;

                    var packet = MessagePacket.Decode(bodyBuf);
                    if (packet != null)
                    {
                        lock (_messageQueue) { _messageQueue.Enqueue((packet.MsgId, packet.Body)); }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[Network] Receive error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _connected = false;
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
            OnDisconnected?.Invoke();
            Debug.Log("[Network] Disconnected");
        }

        public void Dispose() => Disconnect();
    }

    // 消息包 (匹配服务端格式)
    public class MessagePacket
    {
        public const ushort Magic = 0x9A7B;
        public uint MsgId;
        public uint Seq;
        public byte[] Body = Array.Empty<byte>();

        public byte[] Encode()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(Magic);
            w.Write(MsgId);
            w.Write(Seq);
            w.Write(Body.Length);
            w.Write((ushort)0); // Flag
            w.Write(Body);
            return ms.ToArray();
        }

        public static MessagePacket? Decode(byte[] data)
        {
            if (data.Length < 16) return null;
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            if (r.ReadUInt16() != Magic) return null;
            return new MessagePacket
            {
                MsgId = r.ReadUInt32(),
                Seq = r.ReadUInt32(),
                Body = r.ReadBytes(data.Length - 16)
            };
        }
    }
}
