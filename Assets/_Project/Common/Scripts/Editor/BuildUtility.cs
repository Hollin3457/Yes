using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.MixedReality.Toolkit.Build.Editor;
using UnityEditor;
using UnityEngine;

public static class BuildUtility
{
    //app names
    private const string HoloTwinAppName = "Holo Twin";
    private const string UltraSoundAppName = "MR Ultrasound";
    private const string VeinMappingAppName = "Vein Mapping";
        
    //app bundle id
    private const string HoloTwinBundleID = "com.nuhs.holotwin";
    private const string UltraSoundBundleID = "com.nuhs.ultrasound";
    private const string VeinMappingBundleID = "com.nuhs.veinmapping";
    
    //scenes
    private static readonly string[] HoloTwinScenes = new[] {"Assets/_Project/HoloTwin/Scenes/HoloTwin.unity"};
    private static readonly string[] UltraSoundScenes = new[] {"Assets/_Project/UltraSound/Scenes/UltraSound.unity"};
    private static readonly string[] VeinMappingScenes = new[] {"Assets/_Project/VeinMapping/Scenes/VeinMapping.unity"};
    private static string[] TargetScenes;
    
    //icons
    private const string HoloTwinIconPrefix = "Assets/_Project/HoloTwin/Art/AppIconSplashScreen/";
    private const string UltraSoundIconPrefix = "Assets/_Project/UltraSound/Art/AppIconSplashScreen/";
    private const string VeinMappingIconPrefix = "Assets/_Project/VeinMapping/Art/AppIconSplashScreen/";
    
    
    #region Set Project Settings
    public static void SetProjectSettingsForHoloTwin()
    {
        SetAppIcons(HoloTwinIconPrefix);
        SetAppInfo(HoloTwinAppName,HoloTwinBundleID);
        TargetScenes = HoloTwinScenes;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA, "master");
    }
    
    public static void SetProjectSettingsForUltraSound()
    {
        SetAppIcons(UltraSoundIconPrefix);
        SetAppInfo(UltraSoundAppName,UltraSoundBundleID);
        TargetScenes = UltraSoundScenes;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA, "master;BUILTIN_XR");
    }
    public static void SetProjectSettingsForVeinMapping()
    {
        SetAppIcons(VeinMappingIconPrefix);
        SetAppInfo(VeinMappingAppName,VeinMappingBundleID);
        TargetScenes = VeinMappingScenes;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WSA, "master");
    }
    private static void SetAppInfo(string appName, string packageName)
    {
        PlayerSettings.productName = appName;
        PlayerSettings.WSA.packageName = packageName;
        PlayerSettings.WSA.tileShortName = appName;
        PlayerSettings.WSA.applicationDescription = appName;
    }
    
    private static void SetAppIcons(string iconPrefix)
    {
        var typeToDisplayMap = new Dictionary<PlayerSettings.WSAImageType, string>
        {
            { PlayerSettings.WSAImageType.UWPSquare44x44Logo, "Square44x44Logo" },
            { PlayerSettings.WSAImageType.UWPSquare71x71Logo, "Square71x71Logo" },
            { PlayerSettings.WSAImageType.UWPSquare150x150Logo, "Square150x150Logo" },
            { PlayerSettings.WSAImageType.UWPSquare310x310Logo, "Square310x310Logo" },
            { PlayerSettings.WSAImageType.UWPWide310x150Logo, "Wide310x150Logo" },
            { PlayerSettings.WSAImageType.PackageLogo, "StoreLogo" },
            { PlayerSettings.WSAImageType.SplashScreenImage, "SplashScreen" },
        };
        var scaleFactors = new PlayerSettings.WSAImageScale[]
        {
            PlayerSettings.WSAImageScale._100,
            PlayerSettings.WSAImageScale._125,
            PlayerSettings.WSAImageScale._150,
            PlayerSettings.WSAImageScale._200,
            PlayerSettings.WSAImageScale._400,
        };
        var targetSizes = new PlayerSettings.WSAImageScale[]
        {
            PlayerSettings.WSAImageScale.Target16,
            PlayerSettings.WSAImageScale.Target24,
            PlayerSettings.WSAImageScale.Target32,
            PlayerSettings.WSAImageScale.Target48,
            PlayerSettings.WSAImageScale.Target256,
        };
        var scaleToValueMap = new Dictionary<PlayerSettings.WSAImageScale, string>
        {
            {  PlayerSettings.WSAImageScale._100, "100" },
            {  PlayerSettings.WSAImageScale._125, "125" },
            {  PlayerSettings.WSAImageScale._150, "150" },
            {  PlayerSettings.WSAImageScale._200, "200" },
            {  PlayerSettings.WSAImageScale._400, "400" },
            {  PlayerSettings.WSAImageScale.Target16, "16" },
            {  PlayerSettings.WSAImageScale.Target24, "24" },
            {  PlayerSettings.WSAImageScale.Target32, "32" },
            {  PlayerSettings.WSAImageScale.Target48, "48" },
            {  PlayerSettings.WSAImageScale.Target256, "256" },
        };

        // Example: Square71x71Logo.scale-100.png
        foreach (PlayerSettings.WSAImageType imageType in System.Enum.GetValues(typeof(PlayerSettings.WSAImageType)))
        {
            foreach (PlayerSettings.WSAImageScale imageScale in scaleFactors)
            {
                if (!typeToDisplayMap.TryGetValue(imageType, out var imageTypeString) || !scaleToValueMap.TryGetValue(imageScale, out var scaleValue))
                {
                    continue;
                }

                var path = $"{iconPrefix}{imageTypeString}.scale-{scaleValue}.png";
                if (File.Exists(path))
                {
                    PlayerSettings.WSA.SetVisualAssetsImage(path, imageType, imageScale);
                }
            }
        }

        // Example: Square44x44Logo.targetsize-24
        // Target sizes are only applicable for the Square44x44Logo type.
        // https://docs.unity3d.com/ScriptReference/PlayerSettings.WSAImageScale.html
        foreach (PlayerSettings.WSAImageScale imageScale in targetSizes)
        {
            if (!scaleToValueMap.TryGetValue(imageScale, out var scaleValue))
            {
                continue;
            }

            var path = $"{iconPrefix}Square44x44Logo.targetsize-{scaleValue}.png";
            if (File.Exists(path))
            {
                PlayerSettings.WSA.SetVisualAssetsImage(path, PlayerSettings.WSAImageType.UWPSquare44x44Logo, imageScale);
            }
        }

        var splashScreenPath = iconPrefix + "SplashScreen.png";
        if (!File.Exists(splashScreenPath))
        {
            Debug.LogError($"Splash screen file is missing: {splashScreenPath}");
            return;
        }

        var splashScreenTex = AssetDatabase.LoadAssetAtPath<Texture2D>(splashScreenPath);
        PlayerSettings.virtualRealitySplashScreen = splashScreenTex;
    }
    public static UwpBuildInfo GetHololensUwpBuildInfo(string outputDirectory)
    {
        return new UwpBuildInfo
        {
            BuildAppx = true,
            OutputDirectory = outputDirectory,
            BuildPlatform = "ARM64",
            Configuration = "master",
            BuildOptions = BuildOptions.None,
            Scenes = TargetScenes
        };
    }
    #endregion
    
    
    #region Build Path
    public static string GetOutputDirectory(string name)
    {
        return GetBuildPathRoot() + name;
    }
    private static string GetBuildPathRoot()
    {
        return "c:/tmp/Builds/";
    }
    private static string GetTimestamp() => DateTime.Now.ToString("yyyy-MM-dd-HHmmssfff");
    #endregion
}
