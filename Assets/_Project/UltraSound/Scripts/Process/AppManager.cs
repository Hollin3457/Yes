using System;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using Grpc.Core;
using Microsoft.MixedReality.Toolkit.Input;
using NUHS.Common.InternalDebug;
using NUHS.Common.UI;
using NUHS.Backend;
using NUHS.Sidecar;
using NUHS.UltraSound.Config;
using NUHS.UltraSound.ImageReceiving;
using NUHS.UltraSound.Profile;
using NUHS.UltraSound.Tracking;
using NUHS.UltraSound.UI;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

namespace NUHS.UltraSound.Process
{
    public class AppManager : MonoBehaviour
    {
        private readonly string PLAYERPREFS_SIDECAR_IP_KEY = "SidecarIP";

        [Header("Instrument")]
        [SerializeField] private Transform instrumentObj;

        [Header("Core modules")]
        [SerializeField] private Transform ultrasoundObj;
        [SerializeField] private Transform ultrasoundImagePivot;
        [SerializeField] private MarkerConfig markerConfig;
        [SerializeField] private GameObject camHelper;

        [Header("Manager for each process")]
        [SerializeField] private ProcessManager quickViewManager;
        [SerializeField] private ProcessManager recordScanManager;
        [SerializeField] private ProcessManager fileFinderManager;
        [SerializeField] private ProcessManager calibrateManager;
        [SerializeField] private GameObject HomeScreen;

        [Header("UI Dependency")]
        [SerializeField] private UltrasoundConnectionController ultrasoundConnectionController;
        [SerializeField] private TransactionalPrompt profileLoadFailedPrompt;

        [Header("Sending Events")]
        [SerializeField] private BoolEvent onProcessBusy;
        [SerializeField] private SimpleEvent ultrasoundProfileLoaded;

        [Header("Receiving Events")]
        [SerializeField] private SimpleEvent onQuickViewButtonPressed;
        [SerializeField] private SimpleEvent onRecordScanButtonPressed;
        [SerializeField] private SimpleEvent onFileFinderButtonPressed;
        [SerializeField] private SimpleEvent onRecalibrateButtonPressed;
        [SerializeField] private StringEvent onIPAddressConfirmed;
        [SerializeField] private SimpleEvent onIPAddressRetry;
        [SerializeField] private BoolEvent onDebugModeButtonPressed;

        [Header("Debug")]
        public Text renderFPS;
        public Text videoFPS;
        public Text trackFPS;
        public Text detectionStr;
        public Text debugStr;

        private IInternalLogger debugLogger;
        private ProcessManager currentProcess;
        private bool currentProcessIsBusy;
        public bool isInit = false;

#if ENABLE_WINMD_SUPPORT
        // We need to maintain a singleton Research Mode object
        // If it is instantiated twice, it causes a crash
        private static HL2ResearchMode _researchMode;
        public HL2ResearchMode ResearchMode
        {
            get
            {
                if (_researchMode == null) _researchMode = new HL2ResearchMode();
                return _researchMode;
            }
        }
#endif

        private Channel _backendChannel; // DO NOT use directly, use public property instead
        public Channel BackendChannel
        {
            get
            {
                if (_backendChannel == null)
                {
                    Debug.Log($"Setting up new Backend channel to {UltraSoundAppConfigManager.Instance.AppConfig.BackendFullAddress}");
                    _backendChannel = new Channel(UltraSoundAppConfigManager.Instance.AppConfig.BackendFullAddress, ChannelCredentials.Insecure);
                }

                return _backendChannel;
            }
        }

        public async Task CheckBackendConnectionAsync()
        {
            // Ping the Sidecar as an initial test
            var coreService = new BackendCoreServiceV1.BackendCoreServiceV1Client(BackendChannel);
            var pingQuery = Guid.NewGuid().ToString();
            var pingReply = await coreService.PingAsync(new BackendPingRequestV1 { S = pingQuery });
            if (pingReply.S != pingQuery)
            {
                throw new Exception("Invalid ping response from Backend?!");
            }
        }

        private Channel _sidecarChannel; // DO NOT use directly, use public property instead
        public Channel SidecarChannel
        {
            get
            {
                if (_sidecarChannel == null)
                {
                    Debug.Log($"Setting up new Sidecar channel to {UltraSoundAppConfigManager.Instance.AppConfig.SidecarFullAddress}");
                    _sidecarChannel = new Channel(UltraSoundAppConfigManager.Instance.AppConfig.SidecarFullAddress, ChannelCredentials.Insecure);
                }

                return _sidecarChannel;
            }
        }

        public async Task CheckSidecarConnectionAsync()
        {
            // Ping the Sidecar as an initial test
            var coreService = new SidecarCoreServiceV1.SidecarCoreServiceV1Client(SidecarChannel);
            var pingQuery = Guid.NewGuid().ToString();
            var pingReply = await coreService.PingAsync(new SidecarPingRequestV1 { S = pingQuery });
            if (pingReply.S != pingQuery)
            {
                throw new Exception("Invalid ping response from Sidecar?!");
            }
        }

        private IUltrasoundProfileLoader _ultrasoundProfileLoader; // DO NOT use directly, use public property instead
        public IUltrasoundProfileLoader UltrasoundProfileLoader
        {
            get
            {
                if (_ultrasoundProfileLoader == null)
                {
                    switch (UltraSoundAppConfigManager.Instance.AppConfig.UltraSoundProfileLoaderType)
                    {
                        case "backend":
                            Debug.Log("Setting up BackendUltrasoundProfileLoader");
                            _ultrasoundProfileLoader = new BackendUltrasoundProfileLoader(BackendChannel);
                            break;

                        default:
                            Debug.Log("Setting up MockUltrasoundProfileLoader");
                            _ultrasoundProfileLoader = new MockUltrasoundProfileLoader();
                            break;
                    }
                }

                return _ultrasoundProfileLoader;
            }
        }

        private UltrasoundProfile _currentUltrasoundProfile;

        private IMarkerTracker _markerTracker; // DO NOT use directly, use public property instead
        public IMarkerTracker MarkerTracker
        {
            get
            {
                if (_markerTracker == null)
                {
                    switch (UltraSoundAppConfigManager.Instance.AppConfig.TrackerType)
                    {
                        case "hololensRGB":
                            Debug.Log("Setting up UltrasoundArucoTracker");
                            GameObject camHelperObj = GameObject.Instantiate(camHelper);
                            _markerTracker = new UltrasoundHololensRGBTracker(camHelperObj.GetComponent<HLCameraStreamToMatHelper>());
                            break;
                        case "sidecar":
                        default:
                            Debug.Log("Setting up UltrasoundSideCarTracker");
                            _markerTracker = new UltrasoundSideCarTracker(SidecarChannel, this);
                            break;
                    }
                }

                return _markerTracker;
            }
        }

        private IUltrasoundImageReceiver _imageReceiver; // DO NOT use directly, use public property instead
        public IUltrasoundImageReceiver ImageReceiver
        {
            get
            {
                if (_imageReceiver == null)
                {
                    if (_currentUltrasoundProfile == null)
                    {
                        Debug.LogError("Tried to create an image receiver but _currentUltrasoundProfile is null. " +
                            "This likely means there was a BackendUltrasoundProfileLoader connection issue.");
                        return null;
                    }
                    switch (_currentUltrasoundProfile.DeviceType)
                    {
                        case "sidecar":
                            Debug.Log("Setting up SidecarUltrasoundImageReceiver");
                            _imageReceiver = new SidecarUltrasoundImageReceiver(JsonUtility.FromJson<SidecarUltrasoundConfig>(_currentUltrasoundProfile.DeviceConfig), SidecarChannel);
                            break;
                        default:
                            Debug.Log("Setting up MockUltrasoundImageReceiver");
                            _imageReceiver =  new MockUltrasoundImageReceiver();
                            break;
                    }
                }

                return _imageReceiver;
            }
        }

        private void Start()
        {
#if ENABLE_WINMD_SUPPORT
            // Prompt for Research Mode Camera consent on app start
            // DO NOT REMOVE THIS LINE FROM Start()
            ResearchMode.InitializeSpatialCamerasFront();
#endif
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff);
            UltraSoundAppConfigManager.Instance.OnReady.AddListener(Setup);
            profileLoadFailedPrompt.SetupButtonAction(LoadDefaultUltrasoundProfile, null);
        }
        
        private void Setup()
        {
            Debug.Log($"[AppConfigManager] AppConfig = {JsonUtility.ToJson(UltraSoundAppConfigManager.Instance.AppConfig)}");

            // TODO: This should be removed once there's a UI to select the Ultrasound Probe Device Profile
            LoadDefaultUltrasoundProfile();

            LoadMarkerCalibrationFromAppConfig();
            TransitToHome();
            isInit = true;
        }

        private async void LoadDefaultUltrasoundProfile()
        {
            onProcessBusy.Send(true);
            try
            {
                // Load the profiles
                // TODO: Implement UI to show the list of profiles
                var profiles = await UltrasoundProfileLoader.List(false);
                Debug.Log($"Ultrasound Device Profiles Found: {profiles.Count}");

                // Select the desired profile
                // TODO: Implement UI to select the desired profile
                var selectedProfileId = profiles[UltraSoundAppConfigManager.Instance.AppConfig.UltraSoundProfileLoaderDefaultIndex].Id;
                _currentUltrasoundProfile = await UltrasoundProfileLoader.Get(selectedProfileId);
                Debug.Log($"Loaded Ultrasound Device Profile: {_currentUltrasoundProfile.Id} {_currentUltrasoundProfile.Name} {_currentUltrasoundProfile.DeviceType} {_currentUltrasoundProfile.DeviceConfig}");
                ultrasoundProfileLoaded.Send();
            }
            catch (RpcException ex)
            {
                profileLoadFailedPrompt.SetContentText($"An RPC exception occurred with status code '{ex.StatusCode}': {ex.Status.Detail}");
                profileLoadFailedPrompt.gameObject.SetActive(true);
                Debug.LogError($"Failed to load default ultrasound profile due to RpcException. " +
                    $"StatusCode: {ex.StatusCode}. Detail: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                profileLoadFailedPrompt.SetContentText($"An unexpected error occurred: {ex.Message}");
                profileLoadFailedPrompt.gameObject.SetActive(true);
                Debug.LogError($"Failed to load default ultrasound profile due to: {ex.Message}");
            }
            finally
            {
                onProcessBusy.Send(false);
            }
        }

        private void LoadMarkerCalibrationFromAppConfig()
        {
            SetProbeOffset(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerPosition, UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerRotation);
            SetInstrumentOffset(UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerPosition, UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerRotation);
        }

        private void OnEnable()
        {
            onQuickViewButtonPressed.Register(TransitToQuickView);
            onRecordScanButtonPressed.Register(TransitToRecordScan);
            onFileFinderButtonPressed.Register(TransitToFileFinder);
            onRecalibrateButtonPressed.Register(TransitToCalibrate);
            onIPAddressConfirmed.Register(OnIPAddressConfirmed);
            onIPAddressRetry.Register(RetryIPAddress);
            onDebugModeButtonPressed.Register(SetDebugMode);
        }
        private void OnDisable()
        {
            onQuickViewButtonPressed.Unregister(TransitToQuickView);
            onRecordScanButtonPressed.Unregister(TransitToRecordScan);
            onFileFinderButtonPressed.Unregister(TransitToFileFinder);
            onRecalibrateButtonPressed.Unregister(TransitToCalibrate);
            onIPAddressConfirmed.Unregister(OnIPAddressConfirmed);
            onIPAddressRetry.Unregister(RetryIPAddress);
            onDebugModeButtonPressed.Unregister(SetDebugMode);
        }
        
        private void OnDestroy()
        {
            _imageReceiver?.Dispose();
            _markerTracker?.Dispose();

#if ENABLE_WINMD_SUPPORT
            Debug.Log("Stopping all Research Mode sensors...");
            ResearchMode?.StopAllSensorDevice();
#endif
        }


        #region Process Transition
        private void TransitToQuickView()
        {
            TransitProcess(quickViewManager);
        }
        private void TransitToRecordScan()
        {
            TransitProcess(recordScanManager);
        }
        private void TransitToFileFinder()
        {
            TransitProcess(fileFinderManager);
        }
        private void TransitToCalibrate()
        {
            TransitProcess(calibrateManager);
        }

        public void TransitToHome()
        {
            TransitProcess(null);
        }

        private void TransitProcess(ProcessManager process)
        {
            StartCoroutine(ITransitProcess(process));
        }

        IEnumerator ITransitProcess(ProcessManager process)
        {
            ultrasoundConnectionController.CloseIPAddressUI();
            if (currentProcess != null)
            {
                currentProcess.StopProcess();
                while (currentProcessIsBusy)
                {
                    yield return null;
                }
            }
            currentProcess = process;
            if (currentProcess != null)
            {
                HomeScreen.SetActive(false);
                currentProcess.StartProcess(this);
            }
            else
            {
                HomeScreen.SetActive(true);
            }
        }

        public void SetCurrentProcessBusy(ProcessManager process, bool isBusy)
        {
            if (process != currentProcess) return;
            currentProcessIsBusy = isBusy;
            onProcessBusy.Send(currentProcessIsBusy);
        }
        #endregion
        
        #region Debug Mode
        private void SetDebugMode(bool debugMode)
        {
            InternalDebug.IsDebugMode = debugMode;
            
            if (debugMode)
            {
                AddDebugLogger();
            }
            else
            {
                RemoveDebugLogger();
            }
        }

        private void AddDebugLogger()
        {
            if (debugLogger == null)
            {
                debugLogger = new FileInternalLogger(LogLevel.Debug,
                    $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}{DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss")}");
            }
            InternalDebugManager.Instance.AddLogger(debugLogger);
        }

        private void RemoveDebugLogger()
        {
            if (debugLogger == null) return;
            InternalDebugManager.Instance.RemoveLogger(debugLogger);
        }
        #endregion

        #region IP Address Prompt
        private Action _onIpAddressConfirmed;
        public void PromptIPAddress(Action next, bool forcePrompt = false)
        {
            _onIpAddressConfirmed = next;

            // Check if there is saved IP address
            if (PlayerPrefs.HasKey(PLAYERPREFS_SIDECAR_IP_KEY) && !forcePrompt)
            {
                // Skip the prompt and attempt to connect directly
                OnIPAddressConfirmed(PlayerPrefs.GetString(PLAYERPREFS_SIDECAR_IP_KEY));
            }
            else
            {
                ultrasoundConnectionController.PromptForIPAddress(PlayerPrefs.HasKey(PLAYERPREFS_SIDECAR_IP_KEY)
                    ? PlayerPrefs.GetString(PLAYERPREFS_SIDECAR_IP_KEY)
                    : UltraSoundAppConfigManager.Instance.AppConfig.SidecarAddress);
            }

        }

        public void RetryIPAddress()
        {
            PromptIPAddress(_onIpAddressConfirmed, true);
        }

        private async void OnIPAddressConfirmed(string ip)
        {
            try
            {
                // Check if IP address changed
                if (ip != UltraSoundAppConfigManager.Instance.AppConfig.SidecarAddress || _sidecarChannel == null)
                {
                    Debug.Log($"IP address changed: old={UltraSoundAppConfigManager.Instance.AppConfig.SidecarAddress} new={ip}, resetting receiver and trackers.");
                    ResetSidecar();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                // Set the new IP to config
                UltraSoundAppConfigManager.Instance.AppConfig.SidecarAddress = ip;

                // This will cause SidecarChannel to instantiate if is null, so it will take the new IP from the config
                await CheckSidecarConnectionAsync();

                // Save only after the connection is successful
                UltraSoundAppConfigManager.Instance.SaveConfig();
                PlayerPrefs.SetString(PLAYERPREFS_SIDECAR_IP_KEY, ip);

                _onIpAddressConfirmed?.Invoke();
                _onIpAddressConfirmed = null;
                onProcessBusy.Send(false);
            }
            catch (Exception ex)
            {
                ultrasoundConnectionController.ShowConnectionFail();
                Debug.LogException(ex);
                ResetSidecar();
            }
        }

        private void ResetSidecar()
        {
            _imageReceiver?.Dispose();
            _imageReceiver = null;
            if(_markerTracker is UltrasoundSideCarTracker)
            {
                _markerTracker?.Dispose();
                _markerTracker = null;
            }
            
            _sidecarChannel?.ShutdownAsync().Wait();
            _sidecarChannel = null;
        }
        #endregion

        #region Image Receiving APIs
        public void StartImageReceiving()
        {
            ImageReceiver?.Start();
        }

        public void StopImageReceiving()
        {            
            ImageReceiver?.Stop();
        }

        public Texture2D GetImage()
        {
            return ImageReceiver?.GetImage();
        }

        public void UpdateImage()
        {
            if (ImageReceiver == null || !ImageReceiver.IsRunning()) return;
            ImageReceiver.UpdateImage();
        }

        public Vector2 GetImageSize()
        {
            return ImageReceiver?.GetImageSize() ?? Vector2.one;
        }

        /// <summary>
        /// width, height, thickness
        /// </summary>
        /// <remarks>
        /// For example, the head size is (size.x, size.z)
        /// </remarks>
        public Vector3 GetDeviceSizeInCm()
        {
            return _currentUltrasoundProfile.DeviceSizeInCm;
        }

        public Vector2 GetPixelsPerCm()
        {
            return ImageReceiver?.GetPixelsPerCm() ?? Vector2.one;
        }
        #endregion

        #region Marker Tracker APIs
        public void StartMarkerDetect()
        {
            MarkerTracker.Start();
        }

        public void StopMarkerDetect()
        {
            MarkerTracker.Stop();
        }

        public Vector3 GetImagePos()
        {
            return ultrasoundImagePivot.position;
        }

        public Vector3 GetImageRot()
        {
            return ultrasoundImagePivot.eulerAngles;
        }

        public Vector3 GetMarkerPos(int id)
        {
            return MarkerTracker.GetWorldPosition(id);
        }

        public Quaternion GetMarkerRot(int id)
        {
            return MarkerTracker.GetWorldRotation(id);
        }

        public bool IsMarkerDetected(int id)
        {
            return MarkerTracker.IsDetected(id);
        }

        public void SetProbeOffset(Vector3 pos, Vector3 rot, bool save = false)
        {
            //markerConfig.posOffset = pos;
            //markerConfig.rotationOffset = rot;
            //markerConfig.Save();

            if (save)
            {
                UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerPosition = pos;
                UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerRotation = rot;
                UltraSoundAppConfigManager.Instance.SaveConfig();
            }

            ultrasoundImagePivot.localPosition = pos;
            ultrasoundImagePivot.localEulerAngles = rot;
        }

        public void SetInstrumentOffset(Vector3 pos, Vector3 rot, bool save = false)
        {
            if (save)
            {
                UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerPosition = pos;
                UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerRotation = rot;
                UltraSoundAppConfigManager.Instance.SaveConfig();
            }

            // TODO: Set instrument pivot
        }
        #endregion

        private void Update()
        {
            if (ultrasoundObj == null || instrumentObj == null || !isInit) return;
            ultrasoundObj.position = MarkerTracker.GetWorldPosition(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId);
            ultrasoundObj.rotation = MarkerTracker.GetWorldRotation(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId);
            instrumentObj.position = MarkerTracker.GetWorldPosition(UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId);
            instrumentObj.rotation = MarkerTracker.GetWorldRotation(UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId);
        }

        private void LateUpdate()
        {
            DebugUtils.RenderTick();
            if (!isInit || !MarkerTracker.IsRunning()) return;
            float renderDeltaTime = DebugUtils.GetRenderDeltaTime();
            float videoDeltaTime = DebugUtils.GetVideoDeltaTime();
            float trackDeltaTime = DebugUtils.GetTrackDeltaTime();

            if (renderFPS != null)
            {
                renderFPS.text = string.Format("Render: {0:0.0} ms ({1:0.} fps)", renderDeltaTime, 1000.0f / renderDeltaTime);
            }
            if (videoFPS != null)
            {
                videoFPS.text = string.Format("Video: {0:0.0} ms ({1:0.} fps)", videoDeltaTime, 1000.0f / videoDeltaTime);
            }
            if (trackFPS != null)
            {
                trackFPS.text = string.Format("Track: {0:0.0} ms ({1:0.} fps)", trackDeltaTime, 1000.0f / trackDeltaTime);
            }
            if (detectionStr != null && _markerTracker != null) // Need to use the private var _markerTracker here as the public property will cause it to instantiate immediately
            {
                detectionStr.text = string.Format("Detect: {0}", MarkerTracker.IsDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId).ToString());
            }
            if (debugStr != null)
            {
                if (DebugUtils.GetDebugStrLength() > 0)
                {
                    if (debugStr.preferredHeight >= debugStr.rectTransform.rect.height)
                        debugStr.text = string.Empty;

                    debugStr.text += DebugUtils.GetDebugStr();
                    DebugUtils.ClearDebugStr();
                }
            }
        }
    }
}
