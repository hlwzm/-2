using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Jx3.Core.Network;

namespace Jx3.Core
{
    public class HeroScreenManager : MonoBehaviour
    {
        public static HeroScreenManager Instance { get; private set; } = null!;

        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            switch ((MsgId)msgId)
            {
                case MsgId.SCHeroList:
                    var count = r.ReadInt32();
                    GameManager.Instance.Heroes.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        GameManager.Instance.Heroes.Add(new GameManager.HeroData
                        {
                            HeroUid = r.ReadUInt64(),
                            TemplateId = r.ReadUInt32(),
                            Name = r.ReadString(),
                            Level = r.ReadInt32(),
                            Star = r.ReadInt32(),
                            Quality = r.ReadInt32(),
                            InTeam = r.ReadBoolean(),
                        });
                    }
                    Debug.Log($"[Hero] Loaded {count} heroes");
                    break;
            }
        }

        public void RequestHeroList()
        {
            GameManager.Instance.Network.Send((uint)MsgId.CSHeroList, Array.Empty<byte>());
        }

        public void SendRecruitDraw(uint poolId)
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(GameManager.Instance.Player.PlayerId);
            w.Write(poolId);
            w.Write(1); // 单抽
            GameManager.Instance.Network.Send((uint)MsgId.CSRecruitDraw, ms.ToArray());
        }
    }
}
