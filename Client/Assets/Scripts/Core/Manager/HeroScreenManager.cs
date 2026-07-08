using System;
using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class HeroScreenManager : MonoBehaviour
    {
        public static HeroScreenManager Instance { get; private set; } = null!;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            if (msgId == (uint)MsgId.SCHeroList)
            {
                var count = r.ReadInt32();
                GameManager.Instance.Heroes.Clear();
                for (int i = 0; i < count; i++)
                    GameManager.Instance.Heroes.Add(new GameManager.HeroData { HeroUid = r.ReadUInt64(), TemplateId = r.ReadUInt32(), Name = r.ReadString(), Level = r.ReadInt32(), Star = r.ReadInt32(), Quality = r.ReadInt32(), InTeam = r.ReadBoolean() });
                Debug.Log($"[Hero] Loaded {count} heroes");
            }
        }

        public void RequestHeroList() { GameManager.Instance.Network.Send((uint)MsgId.CSHeroList, Array.Empty<byte>()); }
    }
}
