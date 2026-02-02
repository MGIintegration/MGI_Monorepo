using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour {
  RectTransform rt;
  
  void Awake(){ 
    rt = (RectTransform)transform; 
    Apply(); 
  }
  
  void OnRectTransformDimensionsChange(){ 
    Apply(); 
  }
  
  void Apply(){
    // Ensure rt is initialized before using it
    if (rt == null) {
      rt = (RectTransform)transform;
    }
    
    var sa = Screen.safeArea;
    Vector2 min = sa.position, max = sa.position + sa.size;
    min.x /= Screen.width; 
    min.y /= Screen.height; 
    max.x /= Screen.width; 
    max.y /= Screen.height;
    rt.anchorMin = min; 
    rt.anchorMax = max;
  }
}
