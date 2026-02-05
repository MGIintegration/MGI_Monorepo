using UnityEngine;

public static class DeviceIdProvider
{
    private const string Key = "DEVICE_ID";

    /// <summary>
    /// Returns a stable ID for this device.
    /// If it does not exist yet, it creates one and stores it in PlayerPrefs.
    /// </summary>
    public static string GetOrCreateDeviceId()
    {
        if (!PlayerPrefs.HasKey(Key))
        {
            // First run on this device: create a new ID and persist it.
            var id = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString(Key, id);
            PlayerPrefs.Save();
        }

        return PlayerPrefs.GetString(Key);
    }
}
