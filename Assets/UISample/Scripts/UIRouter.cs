using System.Collections.Generic;
using UnityEngine;

public class UIRouter : MonoBehaviour {
  [System.Serializable] public class Route { public string key; public UIScreen screen; }
  public List<Route> routes = new(); UIScreen current;
  public void Navigate(string key) => Navigate(key, null);
  public void Navigate(string key, object args){
    var r = routes.Find(x => x.key == key);
    if (r == null){ Debug.LogWarning($"Route '{key}' not found"); return; }
    if (current) current.Hide();
    current = r.screen;
    if (!current){ Debug.LogWarning($"Route '{key}' has no screen"); return; }
    current.Show(args); current.OnNavigateTo(args);
  }
}
