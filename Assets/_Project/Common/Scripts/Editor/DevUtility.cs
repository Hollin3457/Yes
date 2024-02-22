using UnityEditor;
using UnityEngine;

public static class DevUtilility
{
    [MenuItem("NUHS/Dev/Clear Player Prefs")]
    public static void ClearPlayPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
}
