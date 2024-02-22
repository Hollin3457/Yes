using NUHS.UltraSound.Process;
using System;
using UnityEngine;

#if !UNITY_EDITOR && UNITY_WSA_10_0
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
#endif

namespace NUHS.UltraSound.FileFinder
{
    public class FileFinderManager : ProcessManager
    {
        [Header("Sending Events")]
        [SerializeField] private StringEvent onLoadMesh;
        [SerializeField] private GameObject cuboid;
        [SerializeField] private GameObject visualizationButton;

        private AppManager _appManager;

        public override void StartProcess(AppManager appManager)
        {
            _appManager = appManager;
            _appManager.StartMarkerDetect();
            OpenFilePicker();
        }

        public override void StopProcess()
        {
            _appManager.StopMarkerDetect();
            _appManager = null;
            cuboid.SetActive(false);
            visualizationButton.SetActive(false);
        }

        private void OpenFilePicker()
        {
#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("Select saved mesh", "", "txt");
            string fileContents = null;
            if (!string.IsNullOrEmpty(path))
            {
                fileContents = System.IO.File.ReadAllText(path);
            }
            HandleFilePickerResult(fileContents);
#elif UNITY_WSA_10_0
            _appManager.SetCurrentProcessBusy(this, true);
            UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
            {
                var filePicker = new FileOpenPicker();
                filePicker.FileTypeFilter.Add(".txt");
            
                Debug.Log("Opening file picker");
                var file = await filePicker.PickSingleFileAsync();
                Debug.Log("Path: " + file?.Path);
                string fileContents = null;
                if (file != null)
                {
                    fileContents = await Windows.Storage.FileIO.ReadTextAsync(file);
                }
                UnityEngine.WSA.Application.InvokeOnAppThread(() => 
                {
                    HandleFilePickerResult(fileContents);
                }, false);
            }, false);
#else
            Debug.Log("Loading a mesh is only supported on UWP and in the editor.");
#endif
        }

        private void HandleFilePickerResult(string meshData)
        {
            _appManager.SetCurrentProcessBusy(this, false);
            if (string.IsNullOrEmpty(meshData))
            {
                return;
            }

            onLoadMesh.Send(meshData);
        }

        // TODO: Test this basic validation before using.
        // Using a span instead of string.Split to avoid allocation.
        private static bool ValidateLinesStartWithCharacter(ReadOnlySpan<char> span)
        {
            if (span[0] != 'v')
            {
                return false;
            }

            var index = span.IndexOf('\n');
            while (index > -1)
            {
                if (index + 1 >= span.Length)
                {
                    break;
                }
                if (span[index + 1] != 'v' && span[index + 1] != 'f')
                {
                    return false;
                }

                index = span.Slice(index + 1).IndexOf('\n');
            }

            return true;
        }
    }
}
