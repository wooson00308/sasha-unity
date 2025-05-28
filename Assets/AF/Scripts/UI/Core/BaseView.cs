using UnityEngine;
namespace AF.UI
{
    public abstract class BaseView : MonoBehaviour, IView
    {
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
    }
}
