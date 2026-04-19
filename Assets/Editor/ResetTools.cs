using UnityEditor;
using UnityEngine;

public class ResetTools
{
    [MenuItem("ZenMatch3/Reset Powerups")]
    public static void ResetPowerups()
    {
        PlayerPrefs.DeleteKey("MagnetCount");
        PlayerPrefs.DeleteKey("XBombCount");
        PlayerPrefs.DeleteKey("AreaBombCount");
        PlayerPrefs.DeleteKey("ReceivedStarterPack");
        PlayerPrefs.Save();
        Debug.Log("Powerups and Starter Pack flag reset successfully!");
    }
}
