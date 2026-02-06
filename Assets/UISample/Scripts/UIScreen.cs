using UnityEngine;
public abstract class UIScreen : MonoBehaviour {
  public virtual void Show(object args=null){ gameObject.SetActive(true); }
  public virtual void Hide(){ gameObject.SetActive(false); }
  public virtual void OnNavigateTo(object args) {}
}
