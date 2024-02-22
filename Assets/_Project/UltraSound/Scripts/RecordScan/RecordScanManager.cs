using Microsoft.MixedReality.Toolkit.UI;
using NUHS.UltraSound.UI;
using System.Collections;
using NUHS.UltraSound.Process;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;
using NUHS.UltraSound.Reconstruction;
using NUHS.Common;
using NUHS.Common.UI;

#if !UNITY_EDITOR && UNITY_WSA_10_0
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
#endif

namespace NUHS.UltraSound.Recording
{
    public sealed class RecordScanManager : ProcessManager
    {
        [Header("Distances & Durations")]
        [SerializeField] private Vector3 farGuideTextOffset = new Vector3(0f, .15f, .7f);
        [SerializeField] private Vector3 cuboidOffset = new Vector3(0f, .05f, .6f);
        [SerializeField] private float guideText1Duration = 4f;

        [Header("Configuration")]
        [SerializeField] private int reconstructionPollIntervalMs = 200;
        [SerializeField] private int reconstructionTimeoutMs = 180_000;

        [Header("Receiving Events")]
        [SerializeField] private StringEvent onLoadMesh;

        [Header("Sending Events")]
        [SerializeField] private SimpleEvent onSaveMesh;

        [Header("UI Dependency")]
        [SerializeField] private TransactionalPrompt startScanPrompt;
        [SerializeField] private TransactionalPrompt meshGenFailedPrompt;
        [SerializeField] private PressableButton stopScanButton;
        [SerializeField] private PressableButton restartScanButton;
        [SerializeField] private PressableButton generate3DButton;
        [SerializeField] private PressableButton visualizationButton;
        [SerializeField] private PressableButton saveMeshButton;
        [SerializeField] private Transform nearGroup;
        [SerializeField] private Transform guideTextParent;
        [SerializeField] private GameObject guideText1;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject scanningButtons;
        [SerializeField] private GameObject scanningMiniScreens;

        [Header("Domain Logic")]
        [SerializeField] private Cuboid cuboid;
        [SerializeField] private MirrorCube mirrorCube;
        [SerializeField] private MeshVisualizationManager meshVisualizationManager;
        [SerializeField] private UltrasoundFeed ultrasoundFeed;

        private Transform camTransform;
        private AppManager appManager;
        private IUltrasoundRecorder recorder;
        private IUltrasoundReconstructor reconstructor;
        private Coroutine scanningLoop;
        private Size2D scannerHeadSize;
        private bool savePickerOpen;

        private void Awake()
        {
            stopScanButton.ButtonReleased.AddListener(StopScan);
            restartScanButton.ButtonReleased.AddListener(RestartScan);
            generate3DButton.ButtonReleased.AddListener(GenerateMesh);
            visualizationButton.ButtonReleased.AddListener(() => meshVisualizationManager.SwitchMaterial());
            saveMeshButton.ButtonReleased.AddListener(() => onSaveMesh.Send());

            camTransform = Camera.main.transform;
            cuboid.Init();
        }

        private void OnEnable()
        {
            onSaveMesh.Register(SaveMesh);
            onLoadMesh.Register(LoadMesh);
        }

        private void OnDisable()
        {
            onSaveMesh.Unregister(SaveMesh);
            onLoadMesh.Unregister(LoadMesh);

            // ScaleHandlesConfig is a scriptable object, so set the it back
            // to the default value when disabling/destroying.
            // Consider using another config object instead.
            cuboid.SetScaleMode(HandleScaleMode.NonUniform);
        }

        public override void StartProcess(AppManager appManager)
        {
            Debug.Log("[RecordScanManager] StartProcess");
            this.appManager = appManager;
            this.appManager.PromptIPAddress(PresentGuide);
            var scannerSize = appManager.GetDeviceSizeInCm();
            scannerHeadSize = Size2D.FromCM(scannerSize.x, scannerSize.z);
        }


        private void PresentGuide()
        {
            Debug.Log("[RecordScanManager] PresentGuide");
            appManager.StartMarkerDetect();
            appManager.StartImageReceiving();
            StartCoroutine(CoPresentGuide());
        }

        public override void StopProcess()
        {
            Debug.Log("[RecordScanManager] StopProcess");
            appManager.StopImageReceiving();
            appManager.StopMarkerDetect();
            appManager = null;

            recorder = null;
            reconstructor = null;
            
            StopAllCoroutines();
            cuboid.gameObject.SetActive(false);
            cuboid.EnableManipulation(true);
            cuboid.SetScaleMode(HandleScaleMode.NonUniform);
            mirrorCube.DestroyGrid();
            ultrasoundFeed.gameObject.SetActive(false);
            generate3DButton.gameObject.SetActive(false);
            stopScanButton.gameObject.SetActive(false);
            restartScanButton.gameObject.SetActive(false);
            visualizationButton.gameObject.SetActive(false);
            saveMeshButton.gameObject.SetActive(false);
            startScanPrompt.gameObject.SetActive(false);
            meshGenFailedPrompt.gameObject.SetActive(false);
            loadingIndicator.SetActive(false);
            meshVisualizationManager.ResetMesh();
            guideText1.SetActive(false);
        }

        private IEnumerator CoPresentGuide()
        {
            Debug.Log("[RecordScanManager] CoPresentGuide");
            cuboid.ResetScale();
            cuboid.SetScaleMode(HandleScaleMode.NonUniform);
            cuboid.transform.position = camTransform.position + camTransform.forward * cuboidOffset.z - Vector3.up * cuboidOffset.y;
            var cuboidRot = camTransform.rotation.eulerAngles;
            cuboidRot.z = 0f;
            cuboidRot.x -= 25;
            cuboid.transform.rotation = Quaternion.Euler(cuboidRot);
            guideTextParent.position = camTransform.position + camTransform.forward * farGuideTextOffset.z + Vector3.up * farGuideTextOffset.y;
            guideTextParent.rotation = Quaternion.LookRotation(guideText1.transform.position - camTransform.position);

            meshVisualizationManager.ResetMesh();
            cuboid.gameObject.SetActive(true);
            guideText1.gameObject.SetActive(true);
            DisplayStartScanPrompt();
            yield return new WaitForSeconds(guideText1Duration);
            guideText1.gameObject.SetActive(false);
        }

        private void DisplayStartScanPrompt()
        {
            Debug.Log("[RecordScanManager] DisplayStartScanPrompt");
            startScanPrompt.gameObject.SetActive(true);
            startScanPrompt.SetupButtonAction(StartScan, null);
        }

        private void DisplayMeshGenFailedPrompt()
        {
            Debug.Log("[RecordScanManager] DisplayMeshGenFailedPrompt");
            meshGenFailedPrompt.gameObject.SetActive(true);
            meshGenFailedPrompt.SetupButtonAction(RetryMeshGeneration, RetryFromScan);
            meshGenFailedPrompt.SetContentText(reconstructor.FailMessage);
            PositionObject(meshGenFailedPrompt.transform, cuboid.transform, Vector3.right * -0.25f);
        }

        private void RetryFromScan()
        {
            Debug.Log("[RecordScanManager] RetryFromScan");
            DisplayStartScanPrompt();
            cuboid.EnableManipulation(true);
        }

        private void StartScan()
        {
            Debug.Log("[RecordScanManager] StartScan");

            recorder = new TimeSamplingUltrasoundRecorder(5);
            scanningLoop = StartCoroutine(ScanningLoop());

            // Position buttons to the left of the cuboid.
            nearGroup.transform.position = cuboid.transform.position;
            scanningButtons.GetComponent<AnchorAndFaceCamera>().Anchor();
            scanningMiniScreens.GetComponent<AnchorAndFaceCamera>().Anchor();

            generate3DButton.gameObject.SetActive(true);
            stopScanButton.gameObject.SetActive(true);
            restartScanButton.gameObject.SetActive(true);
            ultrasoundFeed.StartFeed(appManager);
            cuboid.EnableManipulation(false);
            mirrorCube.GenerateGrid(new Vector2(cuboid.transform.localScale.x, cuboid.transform.localScale.z));
        }

        private IEnumerator ScanningLoop()
        {
            while (true)
            {
                recorder.RecordFrame(appManager.GetImage(), appManager.GetImagePos(), appManager.GetImageRot(), cuboid.transform);
                
                // ToDo: the recorder stops if number of recorded frames exceeds the maximum cap, the mirror logic should also be skipped when that happens
                // Mirror logic
                var cuboidLocalScale = cuboid.transform.localScale;
                var localPosInCuboid = recorder.LocalPosition;
                var normalizedLocalPosInCuboid = new Vector2(localPosInCuboid.x / cuboidLocalScale.x, localPosInCuboid.z / cuboidLocalScale.z);
                var cuboidSurfaceScale = new Vector2(cuboidLocalScale.x, cuboidLocalScale.z);
                mirrorCube.Fill(normalizedLocalPosInCuboid, scannerHeadSize, cuboidSurfaceScale);
                
                yield return null;
            }
        }
        
        private void RestartScan()
        {
            Debug.Log("[RecordScanManager] RestartScan");
            recorder.ResetRecord();
            mirrorCube.ResetFill();
        }

        private void StopScan()
        {
            Debug.Log("[RecordScanManager] StopScan");
            if (scanningLoop != null)
            {
                StopCoroutine(scanningLoop);
                scanningLoop = null;
            }
            
            generate3DButton.gameObject.SetActive(false);
            stopScanButton.gameObject.SetActive(false);
            restartScanButton.gameObject.SetActive(false);
            ultrasoundFeed.gameObject.SetActive(false);
            mirrorCube.DestroyGrid();
            DisplayStartScanPrompt();
        }

        private void RetryMeshGeneration()
        {
            Debug.Log("[RecordScanManager] RetryMeshGeneration");
            GenerateMesh();
        }

        private void GenerateMesh()
        {
            Debug.Log("[RecordScanManager] GenerateMesh");
            if (scanningLoop != null)
            {
                StopCoroutine(scanningLoop);
                scanningLoop = null;
            }

            reconstructor = new BackendUltrasoundReconstructor(
                pollIntervalMilliseconds: reconstructionPollIntervalMs,
                timeoutMilliseconds: reconstructionTimeoutMs);
            reconstructor.Reconstruct(recorder,appManager.GetPixelsPerCm(), appManager.BackendChannel, appManager.CheckBackendConnectionAsync);

            generate3DButton.gameObject.SetActive(false);
            stopScanButton.gameObject.SetActive(false);
            restartScanButton.gameObject.SetActive(false);

            mirrorCube.DestroyGrid();
            meshVisualizationManager.ResetMesh();
            ultrasoundFeed.gameObject.SetActive(false);

            StartCoroutine(IGenerateMesh());
        }

        private IEnumerator IGenerateMesh()
        {
            Debug.Log("[RecordScanManager] Mesh generation started");
            appManager.SetCurrentProcessBusy(this, true);
            loadingIndicator.SetActive(true);

            while (true)
            {
                if (reconstructor.Status == UltrasoundReconstructionStatus.Complete)
                {
                    break;
                }
                else if (reconstructor.Status == UltrasoundReconstructionStatus.Failed)
                {
                    appManager.SetCurrentProcessBusy(this, false);
                    loadingIndicator.SetActive(false);
                    DisplayMeshGenFailedPrompt();
                    yield break;
                }
                yield return new WaitForSeconds(1);
            }

            var meshString = reconstructor.Mesh;
            meshVisualizationManager.LoadObj(meshString);
            visualizationButton.gameObject.SetActive(true);
            saveMeshButton.gameObject.SetActive(true);
            visualizationButton.GetComponent<AnchorAndFaceCamera>().offset.y = 0.03f;
            saveMeshButton.GetComponent<AnchorAndFaceCamera>().offset.y = -0.03f;

            // Position the mesh inside the cuboid.
            var cuboidScale = cuboid.transform.localScale;
            meshVisualizationManager.transform.localPosition = Vector3.one * -0.5f;
            meshVisualizationManager.transform.localScale = new Vector3(1/cuboidScale.x,1/cuboidScale.y,1/cuboidScale.z);

            appManager.SetCurrentProcessBusy(this, false);
            loadingIndicator.SetActive(false);
        }

        private void SaveMesh()
        {
            Debug.Log("[RecordScanManager] SaveMesh");
            if (savePickerOpen)
            {
                return;
            }

            var meshString = reconstructor.Mesh;
            if (string.IsNullOrEmpty(meshString))
            {
                Debug.LogError("Unable to save mesh because mesh data is null or empty");
                return;
            }

#if UNITY_EDITOR
            savePickerOpen = true;
            var path = UnityEditor.EditorUtility.SaveFilePanel(
                title: "Save mesh",
                directory: "",
                defaultName: System.DateTime.Now.ToString("mesh-yyyy-MM-dd-HH-mm-ss"),
                extension: "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, meshString);
            }
            savePickerOpen = false;
#elif UNITY_WSA_10_0
            savePickerOpen = true;
            appManager.SetCurrentProcessBusy(this, true);
            UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                //savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Unknown", new List<string>() { ".txt" });
                savePicker.SuggestedFileName = DateTime.Now.ToString("mesh-yyyy-MM-dd-HH-mm-ss");

                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await Windows.Storage.FileIO.WriteTextAsync(file, meshString);
                    Debug.Log($"Mesh saved: {file.Path}");
                }
                UnityEngine.WSA.Application.InvokeOnAppThread(() => 
                {
                    savePickerOpen = false;
                    appManager.SetCurrentProcessBusy(this, false);
                }, false);
            }, false);
#else
            Debug.Log("Saving a mesh is only supported on UWP and in the editor.");
#endif
        }

        [ContextMenu("TestCuboidPositioning")]
        public void TestCuboidPositioning()
        {
            cuboid.transform.position = camTransform.position + camTransform.forward * cuboidOffset.z - Vector3.up * cuboidOffset.y;

            var cuboidRot = camTransform.rotation.eulerAngles;
            cuboidRot.z = 0f;
            cuboidRot.x -= 90;
            cuboid.transform.rotation = Quaternion.Euler(cuboidRot);
        }

        private void LoadMesh(string meshString)
        {
            Debug.Log("[RecordScanManager] LoadMesh");
            cuboid.gameObject.SetActive(true);
            meshVisualizationManager.LoadObj(meshString);
            visualizationButton.gameObject.SetActive(true);
            visualizationButton.GetComponent<AnchorAndFaceCamera>().offset.y = 0f;

            // Configure the cuboid.
            cuboid.EnableManipulation(true);
            cuboid.SetScaleMode(HandleScaleMode.Uniform);
            cuboid.ResetScale();
            cuboid.transform.position = camTransform.position + camTransform.forward * cuboidOffset.z - Vector3.up * cuboidOffset.y;
            var cuboidScale = cuboid.transform.localScale;

            var cuboidRot = camTransform.rotation.eulerAngles;
            cuboidRot.z = 0f;
            cuboidRot.x -= 90;
            cuboid.transform.rotation = Quaternion.Euler(cuboidRot);

            // Position the mesh inside the cuboid.
            meshVisualizationManager.transform.localPosition = Vector3.one * -0.5f;
            meshVisualizationManager.transform.localScale = new Vector3(1/cuboidScale.x,1/cuboidScale.y,1/cuboidScale.z);

            // Position the buttons to the right of the cube.
            PositionObject(nearGroup, cuboid.transform, Vector3.right * -0.15f);
        }

        /// <summary>
        /// Position <paramref name="target"/> relative to <paramref name="relativeObj"/> with given
        /// <paramref name="offset"/>, and face the camera.
        /// </summary>
        private void PositionObject(Transform target, Transform relativeObj, Vector3 offset)
        {
            var relativeObjToCam = (camTransform.position - relativeObj.position).normalized;
            var right = Vector3.Cross(relativeObjToCam, Vector3.up) * offset.x;
            var up = Vector3.up * offset.y;
            target.position = relativeObj.position + right + up;
            target.rotation = Quaternion.LookRotation(target.position - camTransform.position);
        }
    }
}
