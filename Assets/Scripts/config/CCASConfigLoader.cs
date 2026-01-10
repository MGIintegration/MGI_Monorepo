using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public static class CCASConfigLoader
{
    public static T Load<T>(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"[CCASConfigLoader] Missing config: {path}");
            return default;
        }

        string json = File.ReadAllText(path);
        Debug.Log($"[CCASConfigLoader] Loaded {fileName}");
        return JsonConvert.DeserializeObject<T>(json);
    }
}
