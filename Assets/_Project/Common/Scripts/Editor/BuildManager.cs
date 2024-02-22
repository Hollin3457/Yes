using System;
using Microsoft.MixedReality.Toolkit.Build.Editor;
using Microsoft.MixedReality.Toolkit.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public static class BuildManager
{
    [MenuItem("NUHS/Build/MR Ultrasound")]
    public static void BuildUltraSound()
    {
        BuildUtility.SetProjectSettingsForUltraSound();
        BuildHololens(BuildUtility.GetOutputDirectory("UltraSound"));
    }
    [MenuItem("NUHS/Build/Vein Mapping")]
    public static void BuildVeinMapping()
    {
        BuildUtility.SetProjectSettingsForVeinMapping();
        BuildHololens(BuildUtility.GetOutputDirectory("VeinMapping"));
    }

    private static async void BuildHololens(string outputDirectory)
    {
        try
        {
            EditorAssemblyReloadManager.LockReloadAssemblies = true;

            var buildInfo = BuildUtility.GetHololensUwpBuildInfo(outputDirectory);

            var successBuildPlayer = await UwpPlayerBuildTools.BuildPlayer(buildInfo);
            if (!successBuildPlayer)
            {
                throw new Exception("Failed BuildPlayer");
            }

            var successBuildAppx = await UwpAppxBuildTools.BuildAppxAsync(buildInfo);
            if (!successBuildAppx)
            {
                throw new Exception("Failed BuildAppxAsync");
            }
        }
        catch(Exception exception)
        {
            Debug.LogError(exception.Message);
        }
        finally
        {
            EditorAssemblyReloadManager.LockReloadAssemblies = false;
        }
    }
}
