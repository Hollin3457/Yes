using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.MixedReality.Toolkit.Input;
using NUHS.VeinServer;
using NUHS.Common.InternalDebug;
using NUHS.VeinMapping.Config;
using NUHS.VeinMapping.ImageProcessing;
using NUHS.VeinMapping.VeinProcess;
using NUHS.VeinMapping.UI;
// using Unity.Barracuda;
using UnityEngine;
using Unity.Collections;
#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.ApplicationModel.Core; 
#endif
#if !UNITY_EDITOR && UNITY_WSA_10_0
using Windows.Security.Authorization.AppCapabilityAccess;
#endif

namespace NUHS.VeinMapping
{
    public class AppManager : MonoBehaviour
    {
        [Header("UI Dependency")]
        [SerializeField] private VeinMappingConnectionController veinmappingConnectionController;

        [Header("Veinmapping Scene Parent Object")]
        [SerializeField] private GameObject veinmappingSceneParent;

        [Header("Receiving Events")]
        [SerializeField] private StringEvent onIPAddressConfirmed;
        [SerializeField] private SimpleEvent onIPAddressRetry;
        [SerializeField] private BoolEvent onDebugModeToggled;
        [SerializeField] private BoolEvent onCuboidLockToggled;
        [SerializeField] private SimpleEvent onApplicationQuitEvent;

        // [SerializeField] private NNModel onnxModel;
        [SerializeField] private NUHS.Common.Cuboid cuboid;

        private string _deviceName;
        private byte[] _depthLut;
        private VeinSensorStream _veinSensorStream;
        private IVeinProcessor _serverVeinProcessor;
        //private VeinProcessor _veinProcessor;
        //private List<Vector4> _pointsToRender = new List<Vector4>();
        private bool _isIPConfirmed = false;
        private bool _setupDone = false;
        private bool _isSettingUp = false;
        private IInternalLogger _debugLogger;
        private GPUInstancingPointCloud _gpuInstancingPointCloud;
        private BoxCollider _cuboidBoxCollider;
        private CuboidInfo _cuboidInfo;
        private CuboidPoints _cuboidPoints;

        private bool _onPointCloudUpdated = false;
        private NativeArray<Vector4> _pointCloud;
        private Queue<int> _batchSizeQueue = new Queue<int>(3);
        private List<Vector4> _pointCloudBatch = new List<Vector4>(256 * 256);

        private object _cuboidLock = new object();
        private object _pointCloudLock = new object();

        private bool _retrySetup = false;
        public void RetrySetup()
        {
            _retrySetup = true;
        }

#if ENABLE_WINMD_SUPPORT
        // We need to maintain a singleton Research Mode object
        // If it is instantiated twice, it causes a crash
        // 
        private static HL2ResearchMode _researchMode;
        public static HL2ResearchMode ResearchMode
        {
            get
            {
                if (_researchMode == null)
                {
                    _researchMode = new HL2ResearchMode();
                }

                return _researchMode;
            }
        }
#endif

        private Channel _veinServerChannel; // DO NOT use directly, use public property instead
        public Channel VeinServerChannel
        {
            get
            {
                if (_veinServerChannel == null)
                {
                    Debug.Log($"Setting up new Server channel to {VeinMappingAppConfigManager.Instance.AppConfig.ServerFullAddress}");
                    _veinServerChannel = new Channel(VeinMappingAppConfigManager.Instance.AppConfig.ServerFullAddress, ChannelCredentials.Insecure);
                }

                return _veinServerChannel;
            }
        }
        
        public async Task CheckServerConnectionAsync()
        {
            // Ping the Sidecar as an initial test
            var coreService = new VeinServerCoreServiceV1.VeinServerCoreServiceV1Client(VeinServerChannel);
            var pingQuery = Guid.NewGuid().ToString();
            var pingReply = await coreService.PingAsync(new VeinServerPingRequestV1 { S = pingQuery }, deadline: DateTime.UtcNow.AddSeconds(VeinMappingAppConfigManager.Instance.AppConfig.ServerTimeoutSeconds));
            if (pingReply.S != pingQuery)
            {
                throw new Exception("Invalid ping response from Server?!");
            }
        }

        private void Start()
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff);
            SetupResearchMode();
            VeinMappingAppConfigManager.Instance.OnReady.AddListener(() => Setup());
            onApplicationQuitEvent.Register(QuitApplication);
            
#if ENABLE_WINMD_SUPPORT
            CoreApplication.EnteredBackground += CoreApplication_EnteredBackground;
            CoreApplication.Suspending += CoreApplication_Suspending;
#endif
        }

        private void Update()
        {
            if(_retrySetup)
            {
                OnRetrySetup();
                return;
            }
            if (!_setupDone) return;
            if (_veinSensorStream == null || _serverVeinProcessor == null || _cuboidInfo == null)  return;
            if (!_veinSensorStream.IsStreaming()) return;
            lock (_cuboidLock)
            {
                _cuboidPoints = _cuboidInfo.GetCuboidInfo();
            }

            if (_onPointCloudUpdated)
            {
                _onPointCloudUpdated = false;
                lock (_pointCloudLock)
                {
                    UpdatePointCloudRendering();
                }
            }
        }

        private void OnRetrySetup()
        {
            _retrySetup = false;
            _setupDone = false;
            veinmappingSceneParent.SetActive(false);
            Setup(true);
        }

        private void UpdatePointCloudRendering()
        {
            if (_batchSizeQueue.Count >= 1)
            {
                var removeCount = _batchSizeQueue.Dequeue();
                _pointCloudBatch.RemoveRange(0, removeCount);
            }
            _pointCloudBatch.AddRange(_pointCloud);
            _batchSizeQueue.Enqueue(_pointCloud.Length);
            _gpuInstancingPointCloud.UpdatePoints(_pointCloudBatch, _cuboidBoxCollider.bounds);
        }

        private void OnDestroy()
        {
            _serverVeinProcessor?.Dispose();
            _veinSensorStream?.Dispose();
#if ENABLE_WINMD_SUPPORT
            ResearchMode.StopAllSensorDevice();
#endif
        }

        private void OnEnable()
        {
            onIPAddressConfirmed.Register(OnIPAddressConfirmed);
            onIPAddressRetry.Register(RetryIPAddress);
            onDebugModeToggled.Register(SetDebugMode);
            onCuboidLockToggled.Register(ToggleCuboidLock);
        }
        private void OnDisable()
        {
            onIPAddressConfirmed.Unregister(OnIPAddressConfirmed);
            onIPAddressRetry.Unregister(RetryIPAddress);
            onDebugModeToggled.Unregister(SetDebugMode);
            onCuboidLockToggled.Unregister(ToggleCuboidLock);
        }
        
#if ENABLE_WINMD_SUPPORT
        private void CoreApplication_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            QuitApplication();
        }

        private void CoreApplication_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            QuitApplication();
        }
#endif
        public bool IsSettingUp()
        {
            return _isSettingUp;
        }

        public void Setup(bool isRetry = false) => StartCoroutine(ISetup(isRetry));

        private IEnumerator ISetup(bool isRetry)
        {
            if (IsSettingUp()) yield break;
            _isSettingUp = true;

            // this is to prevent first installation camera haven't granted
            while (true)
            {
                if (CheckCameraAndMicPermission() == true)
                {
                    break;
                }
                Debug.Log("AppManager.ISetup(): Camera and mic permission not granted, waiting for 1 second to retry...");
                yield return new WaitForSeconds(1f);
            }
            
            SetupServerIP(isRetry);
            while (!_isIPConfirmed)
            {
                yield return null;
            }
            yield return StartCoroutine(SetupVeinSensorStream());
            SetupVeinProcess();
            SetupPointCloudRendering();

            _isSettingUp = false;
            _setupDone = true;
        }

        private bool CheckCameraAndMicPermission()
        {
#if !UNITY_EDITOR && UNITY_WSA_10_0
            var cameraCapability = AppCapability.Create("webcam");
            var microphoneCapability = AppCapability.Create("microphone");
            if (cameraCapability.CheckAccess() == AppCapabilityAccessStatus.Allowed &&
                microphoneCapability.CheckAccess() == AppCapabilityAccessStatus.Allowed)
            {
                return true;
            }
#endif
            return false;
        }

        private void SetupServerIP(bool isRetry)
        {
            _isIPConfirmed = false;
#if WINDOWS_UWP
            var info = new EasClientDeviceInformation();
            _deviceName = info.FriendlyName.ToString();
#else
            _deviceName = "mockDevice";
#endif
            // Prompt the user for an IP address if this is the first time the app is launched
            PromptIPAddress(StartVeinMappingScene, isRetry);
        }

        private void SetupResearchMode()
        {
#if ENABLE_WINMD_SUPPORT
            IntPtr WorldOriginPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
            var unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
            ResearchMode.InitializeSpatialCamerasFront();
            ResearchMode.SetReferenceCoordinateSystem(unityWorldOrigin);
            ResearchMode.SetPointCloudDepthOffset(0);
            ResearchMode.InitializeDepthSensor();
#endif
        }

        private IEnumerator SetupVeinSensorStream()
        {
            _veinSensorStream = new VeinSensorStream(this);
            _veinSensorStream.OnSensorStreamUpdated += OnSensorStreamUpdated;
            _veinSensorStream.StartSensorStream();
            // wait for LUT ready
            while (!_veinSensorStream.IsDepthLUTReady())
            {
                yield return null;
            }
            _depthLut = _veinSensorStream.GetDepthLUT();
        }

        private async Task OnSensorStreamUpdated(byte[] ir, byte[] depth, string d2w, VeinProcess.ImageSize crop)
        {
            if(_serverVeinProcessor != null && _serverVeinProcessor.IsRunning())
            {
                CuboidPoints cuboidPoints = new CuboidPoints();
                lock (_cuboidLock)
                {
                    cuboidPoints.origin = _cuboidPoints.origin;
                    cuboidPoints.corner1 = _cuboidPoints.corner1;
                    cuboidPoints.corner2 = _cuboidPoints.corner2;
                    cuboidPoints.corner3 = _cuboidPoints.corner3;
                }

                Debug.Log("AppManager.OnSensorStreamUpdated(): Sending sensor stream...");
                await _serverVeinProcessor.Process(ir, depth, d2w, cuboidPoints, crop, InternalDebug.IsDebugMode);
            }  
        }

        private void SetupVeinProcess()
        {
            // setup server vein processor
            _serverVeinProcessor = new ServerVeinProcessor(
                this,
                VeinServerChannel,
                _deviceName,
                _depthLut,
                VeinMappingAppConfigManager.Instance.AppConfig.ServerVeinMapperType,
                ServerImageCompressionTypeFactory.CreateFromString(VeinMappingAppConfigManager.Instance.AppConfig.ServerImageCompressionType),
                VeinSensorStream.MaxPoints);
            _serverVeinProcessor.OnPointCloudUpdated += OnPointCloudUpdated;
            _serverVeinProcessor.OnCropChanged += _veinSensorStream.SetImageSize;
            _serverVeinProcessor.OnDeltaTimeChanged += _veinSensorStream.SetDeltaTime;
            _serverVeinProcessor.Start();
            Debug.Log("AppManager.SetupVeinProcess(): Started ServerVeinProcessor...");

            _cuboidInfo = cuboid.GetComponent<CuboidInfo>();
            if (_cuboidInfo == null)
            {
                Debug.LogError("AppManager.SetupVeinProcess(): The cuboid gameobject should have the CuboidInfo script attached!");
            }
        }

        private void OnPointCloudUpdated(NativeArray<Vector4> pointCloud)
        {
            lock (_pointCloudLock)
            {
                _pointCloud = pointCloud;
                Debug.Log($"AppManager.OnPointCloudUpdated(): PointCloud Length: {pointCloud.Length}");
            }
            
            _onPointCloudUpdated = true;
        }

        private void SetupPointCloudRendering()
        {
            _gpuInstancingPointCloud = cuboid.GetComponent<GPUInstancingPointCloud>();
            if (_gpuInstancingPointCloud == null)
            {
                Debug.LogError("AppManager.SetupPointCloudRendering(): The cuboid does not have GPUInstancingPointCloud");
            }

            _cuboidBoxCollider = cuboid.GetComponent<BoxCollider>();
            if (_cuboidBoxCollider == null)
            {
                Debug.LogError("AppManager.SetupPointCloudRendering(): The cuboid does not have BoxCollider");
            }

            var pointColorHex = VeinMappingAppConfigManager.Instance.AppConfig.PointColor;
            if (!ColorUtility.TryParseHtmlString(pointColorHex, out var pointColor))
            {
                Debug.LogWarning($"AppManager.SetupPointCloudRendering(): Failed to parse AppConfig.PointColor '{pointColorHex}'");
                return;
            }

            _gpuInstancingPointCloud.SetColor(pointColor);
        }
        private void StartVeinMappingScene()
        {
            if (veinmappingSceneParent.activeInHierarchy) return;
            veinmappingSceneParent.SetActive(true);
        }

        private void ToggleCuboidLock(bool doLock)
        {
            cuboid.EnableManipulation(!doLock);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Debug.LogError("TaskScheduler_UnobservedTaskException");
            Debug.LogException(e.Exception);
        }

        private void QuitApplication()
        {
            _serverVeinProcessor.Stop();
            _veinSensorStream.StopSensorStream();
#if ENABLE_WINMD_SUPPORT
            CoreApplication.Exit();
#else 
            Application.Quit();
#endif
        }

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
            if (_debugLogger == null)
            {
                _debugLogger = new FileInternalLogger(LogLevel.Debug,
                    $"{Application.persistentDataPath}{Path.DirectorySeparatorChar}{DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss")}");
            }
            InternalDebugManager.Instance.AddLogger(_debugLogger);
        }

        private void RemoveDebugLogger()
        {
            if (_debugLogger == null) return;
            InternalDebugManager.Instance.RemoveLogger(_debugLogger);
        }
#endregion

#region IP Address Prompt
        private Action _onIpAddressConfirmed;
        public void PromptIPAddress(Action next, bool isRetry = false)
        {
            _onIpAddressConfirmed = next;

            var IPAddress = VeinMappingAppConfigManager.Instance.AppConfig.ServerAddress;
            // Check if there is saved IP address
            if (!isRetry && !String.IsNullOrEmpty(IPAddress))
            {
                // Skip the prompt and attempt to connect directly
                OnIPAddressConfirmed(IPAddress);
            }
            else
            {
                veinmappingConnectionController.PromptForIPAddress(IPAddress);
            }
        }

        public void RetryIPAddress()
        {
            PromptIPAddress(_onIpAddressConfirmed, true);
        }

        private async void OnIPAddressConfirmed(string ip)
        {
            Debug.Log("Attempt to connect: " + ip);
            try
            {
                // Check if IP address changed
                if (ip != VeinMappingAppConfigManager.Instance.AppConfig.ServerAddress && _veinServerChannel != null)
                {
                    Debug.Log($"IP address changed: old={VeinMappingAppConfigManager.Instance.AppConfig.ServerAddress} new={ip}, resetting receiver and trackers.");
                    ResetServer();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                // Set the new IP to config
                VeinMappingAppConfigManager.Instance.AppConfig.ServerAddress = ip;

                // This will cause SidecarChannel to instantiate if is null, so it will take the new IP from the config
                await CheckServerConnectionAsync();

                // Save only after the connection is successful
                VeinMappingAppConfigManager.Instance.SaveConfig();

                _onIpAddressConfirmed?.Invoke();
                _onIpAddressConfirmed = null;
                _isIPConfirmed = true;
                Debug.Log("IPAddressConfirmed");
            }
            catch (Exception ex)
            {
                veinmappingConnectionController.ShowConnectionFail();
                Debug.LogException(ex);
                ResetServer();
            }
        }

        private void ResetServer()
        {
            //TODO: not entirely sure what needs to be reset here --Sitian
            _veinServerChannel?.ShutdownAsync().Wait();
            _veinServerChannel = null;
        }
#endregion
    }
}
