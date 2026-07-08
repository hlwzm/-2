using System.IO;
using UnityEngine;

namespace Jx3.Core
{
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; } = null!;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            using var r = new BinaryReader(new MemoryStream(body));
            if (msgId == (uint)MsgId.SCDungeonEnterResult) Debug.Log("[Dungeon] Result: " + r.ReadInt32());
            if (msgId == (uint)MsgId.SCDungeonBossHP) Debug.Log("[Dungeon] Boss HP");
            if (msgId == (uint)MsgId.SCDungeonComplete) Debug.Log("[Dungeon] Complete!");
            if (msgId == (uint)MsgId.SCDungeonFail) Debug.Log("[Dungeon] Failed");
        }

        public void RequestDungeonList() { GameManager.Instance.Network.Send((uint)MsgId.CSDungeonList, new byte[0]); }
        public void EnterDungeon(int dungeonId, int difficulty, ulong teamId)
        {
            using var ms = new MemoryStream(); using var w = new BinaryWriter(ms);
            w.Write(dungeonId); w.Write(difficulty); w.Write(teamId);
            GameManager.Instance.Network.Send((uint)MsgId.CSDungeonEnter, ms.ToArray());
        }
    }
}