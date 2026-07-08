using System;
using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class LoginManager : MonoBehaviour
    {
        public static LoginManager Instance { get; private set; } = null!;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            if (msgId == (uint)MsgId.SCLoginAuthResult)
            {
                var code = r.ReadInt32();
                if (code == 0) {
                    GameManager.Instance.Player.Token = r.ReadString();
                    GameManager.Instance.Player.PlayerId = r.ReadUInt64();
                    SendEnterGame();
                } else Debug.LogError("[Login] Auth failed: " + code);
            }
            if (msgId == (uint)MsgId.SCLoginEnterGame)
            {
                GameManager.Instance.Player.Name = r.ReadString();
                GameManager.Instance.Player.Level = r.ReadInt32();
                GameManager.Instance.FireLoginSuccess();
            }
        }

        public void SendLogin(string phone, string password)
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(phone); w.Write(password);
            GameManager.Instance.Network.Send((uint)MsgId.CSLoginAuth, ms.ToArray());
        }

        public void SendRegister(string phone, string password)
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(phone); w.Write(password);
            GameManager.Instance.Network.Send((uint)MsgId.CSLoginRegister, ms.ToArray());
        }

        void SendEnterGame()
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(GameManager.Instance.Player.Token);
            GameManager.Instance.Network.Send((uint)MsgId.CSLoginEnterGame, ms.ToArray());
        }
    }
}
