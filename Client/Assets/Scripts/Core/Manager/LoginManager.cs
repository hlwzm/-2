using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Jx3.Core.Network;

namespace Jx3.Core
{
    public class LoginManager : MonoBehaviour
    {
        public static LoginManager Instance { get; private set; } = null!;

        void Awake()
        {
            Instance = this;
            GameManager.Instance.OnLoginSuccess += () =>
            {
                Debug.Log("[Login] Success! Loading main scene...");
                // 切换到主城场景
                GameManager.Instance.ShowNotice("登录成功！");
            };
        }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            switch ((MsgId)msgId)
            {
                case MsgId.SCLoginAuthResult:
                    var code = r.ReadInt32();
                    if (code == 0)
                    {
                        GameManager.Instance.Player.Token = r.ReadString();
                        GameManager.Instance.Player.PlayerId = r.ReadUInt64();
                        Debug.Log($"[Login] Auth OK! PlayerId={GameManager.Instance.Player.PlayerId}");
                        // 请求角色列表/进入游戏
                        SendEnterGame();
                    }
                    else
                    {
                        Debug.LogError($"[Login] Auth failed: code={code}");
                    }
                    break;

                case MsgId.SCLoginEnterGame:
                    GameManager.Instance.Player.Name = r.ReadString();
                    GameManager.Instance.Player.Level = r.ReadInt32();
                    GameManager.Instance.Player.MapId = r.ReadInt32();
                    GameManager.Instance.OnLoginSuccess?.Invoke();
                    break;
            }
        }

        public void SendLogin(string phone, string password)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(phone);
            w.Write(password);
            GameManager.Instance.Network.Send((uint)MsgId.CSLoginAuth, ms.ToArray());
            Debug.Log("[Login] Sent login request");
        }

        public void SendRegister(string phone, string password)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(phone);
            w.Write(password);
            GameManager.Instance.Network.Send((uint)MsgId.CSLoginRegister, ms.ToArray());
        }

        void SendEnterGame()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(GameManager.Instance.Player.Token);
            GameManager.Instance.Network.Send((uint)MsgId.CSLoginEnterGame, ms.ToArray());
        }
    }
}
