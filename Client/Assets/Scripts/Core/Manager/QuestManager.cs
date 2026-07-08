using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Jx3.Core
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; } = null!;
        void Awake() { Instance = this; }

        public void HandleMessage(uint msgId, byte[] body)
        {
            if (msgId == (uint)MsgId.SCQuestList) Debug.Log("[Quest] List");
            if (msgId == (uint)MsgId.SCQuestReward) Debug.Log("[Quest] Reward");
        }

        public void RequestQuests() { GameManager.Instance.Network.Send((uint)MsgId.CSQuestList, new byte[0]); }
    }
}