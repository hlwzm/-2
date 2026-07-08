using UnityEngine;

namespace Jx3.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        public virtual void OnOpen(object data = null) { }
        public virtual void OnClose() { }
        public virtual void OnRefresh() { }
    }
}
