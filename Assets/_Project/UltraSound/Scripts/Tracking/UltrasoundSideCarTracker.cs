using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Grpc.Core;
using Google.Protobuf;
using NUHS.UltraSound.Process;
using NUHS.UltraSound.Config;
using NUHS.Common.InternalDebug;
using NUHS.Sidecar;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UtilsModule;

namespace NUHS.UltraSound.Tracking
{
    public class UltrasoundSideCarTracker : IMarkerTracker
    {
#if ENABLE_WINMD_SUPPORT
        private Windows.Perception.Spatial.SpatialCoordinateSystem _unityWorldOrigin;
#endif
        private bool _isRunning = false;
        public bool IsRunning() => _isRunning;
        private int _loopDuaraionInMS;
        private long _timeOutInMS;
        private long _maxSyncDiffInTicks;
        private long _numDiscardedFrames;

        private object _bufferSync = new object();
        private byte[] _leftCamBuffer, _rightCamBuffer;
        private long _leftCamTimestamp, _rightCamTimestamp;
        private string _leftToWorld, _rightToWorld;
        private Mat _leftMat, _rightMat;

        private object _markersSync = new object();
        private Dictionary<int, float[]> _markersDataDict = new Dictionary<int, float[]>();
        private Dictionary<int, long> _markersLastUpdateDict = new Dictionary<int, long>();

        private Task _streamingTask;
        private CancellationTokenSource _streamingTaskCTS;
        private Task _responseTask;
        private CancellationTokenSource _responseTaskCTS;

        private AppManager _appManager;
        private Channel _channel;
        private MarkerTrackerServiceV1.MarkerTrackerServiceV1Client _client;
        private IAsyncStreamReader<MarkerPositions> _responseStream;
        private IAsyncStreamWriter<MarkerTrackerStereoRequest> _requestStream;
        public bool IsUpdated { get; set; }
        public UltrasoundSideCarTracker(Channel channel, AppManager appManager)
        {
            _loopDuaraionInMS = UltraSoundAppConfigManager.Instance.AppConfig.SidecarTrackerLoopDurationInMS;
            _timeOutInMS = UltraSoundAppConfigManager.Instance.AppConfig.SidecarTrackerTimeOutInMS;
            _maxSyncDiffInTicks = UltraSoundAppConfigManager.Instance.AppConfig.SidecarTrackerMaxSyncDiffInMS * 10000L; // Convert to ticks

            _leftMat = new Mat(480, 640, CvType.CV_8UC1);
            _rightMat = new Mat(480, 640, CvType.CV_8UC1);

#if ENABLE_WINMD_SUPPORT
    #if UNITY_2020_1_OR_NEWER // note: Unity 2021.2 and later not supported
            IntPtr WorldOriginPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
            _unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
    #else
            IntPtr WorldOriginPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
            _unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
#endif
            _appManager = appManager;
            _appManager.ResearchMode.InitializeSpatialCamerasFront();
            _appManager.ResearchMode.SetReferenceCoordinateSystem(_unityWorldOrigin);
#endif
            // app config for ultrasound and instrument can turn to list in future for tracking more objects
            _markersDataDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId] = new float[7] { 0f, 0f, 0f, 0f, 0f, 0f, 1f };
            _markersDataDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId] = new float[7] { 0f, 0f, 0f, 0f, 0f, 0f, 1f };
            _markersLastUpdateDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _markersLastUpdateDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId] = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            _client = new MarkerTrackerServiceV1.MarkerTrackerServiceV1Client(channel);
        }

        async Task StartStreamingAsync()
        {
            try
            {
                Debug.Log("Starting Research Mode RF and LF camera streams...");
#if ENABLE_WINMD_SUPPORT
                _appManager.ResearchMode.StartLFLoop();
                _appManager.ResearchMode.StartRFLoop();
#endif

                // Make a streaming call to gRPC and set up the two-way streams
                var call = _client.TrackStereoStreaming();
                _responseStream = call.ResponseStream;
                _requestStream = call.RequestStream;

                // Start the response stream reader in a separate thread
                _responseTaskCTS = new CancellationTokenSource();
                _responseTask = Task.Run(ReadFromSideCarStreaming, _responseTaskCTS.Token);

                // Flags to track when new stereo camera buffers are ready to be sent
                bool hasLeftBuffer = false;
                bool hasRightBuffer = false;

                Debug.Log("Starting marker tracking REQUEST stream to Sidecar...");
                while (!_streamingTaskCTS.IsCancellationRequested)
                {
                    // Profiling to determine how long each request took
                    var t0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

#if ENABLE_WINMD_SUPPORT
                    if (_appManager.ResearchMode.LFImageUpdated() && !hasLeftBuffer) // get left
                    {
                        lock (_bufferSync)
                        {
                            _leftCamBuffer = _appManager.ResearchMode.GetLFCameraBuffer(out _leftCamTimestamp);
                            _leftToWorld = _appManager.ResearchMode.PrintLeftToWorld();
                        }

                        hasLeftBuffer = true;
                    }

                    if (_appManager.ResearchMode.RFImageUpdated() && !hasRightBuffer) // get right
                    {
                        lock (_bufferSync)
                        {
                            _rightCamBuffer = _appManager.ResearchMode.GetRFCameraBuffer(out _rightCamTimestamp);
                            _rightToWorld = _appManager.ResearchMode.PrintRightToWorld();
                        }

                        hasRightBuffer = true;
                    }

                    // Check if frames are out of sync
                    var diff = Math.Abs(_leftCamTimestamp - _rightCamTimestamp);
                    if (diff > _maxSyncDiffInTicks)
                    {
                        // Discard older frame
                        if (_leftCamTimestamp > _rightCamTimestamp)
                            hasRightBuffer = false;
                        else
                            hasLeftBuffer = false;

                        // Increment the number of discarded frames
                        _numDiscardedFrames++;
                        InternalDebug.Log(LogLevel.Debug, $"Discarding frame (diff: {diff}, counter: {_numDiscardedFrames})");

                        // Loop at speed 1/10th the sync diff to find a sync
                        await Task.Delay((int) (_maxSyncDiffInTicks / 100000L));
                        continue;
                    }

                    if (hasLeftBuffer && hasRightBuffer)
                    {
                        hasLeftBuffer = false;
                        hasRightBuffer = false;
                        _numDiscardedFrames = 0L;

                        await SendToSideCarStreaming();
                    }
#endif
                    // Profiling to determine how long each request took
                    var t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var duration = (int)(t1 - t0);

                    // We don't want to run an infinite loop chewing up CPU
                    // Add a reasonable delay to keep a constant rate
                    if (duration < _loopDuaraionInMS)
                        await Task.Delay(_loopDuaraionInMS - duration);
                }

                // Cancel response task
                _responseTaskCTS.Cancel();
                _responseTask.Wait();
                _responseTaskCTS.Dispose();
                call.Dispose();
                Debug.Log("Stopped marker tracking REQUEST stream to Sidecar.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        async Task ReadFromSideCarStreaming()
        {
            try
            {
                Debug.Log("Starting marker tracking RESPONSE stream from Sidecar...");
                while (await _responseStream.MoveNext(_responseTaskCTS.Token))
                {
                    var response = _responseStream.Current;

                    // Don't do anything if no markers found
                    if (response.Markers.Count > 0)
                    {
                        foreach (var marker in response.Markers)
                        {
                            if (InternalDebug.IsDebugMode)
                            {
                                InternalDebug.Log(LogLevel.Debug, $"marker id {marker.MarkerId} = {marker.PoseMatrix}");
                            }

                            // Don't do anything if null_pose
                            if (marker.PoseMatrix == "null_pose") continue;

                            if (_markersDataDict.ContainsKey(marker.MarkerId))
                            {
                                // First 3 values are translation
                                // Last 4 values are rotation (quaternion)
                                var poseMatrix = Array.ConvertAll(marker.PoseMatrix.Split(','), float.Parse);
                                lock (_markersSync)
                                {
                                    poseMatrix.CopyTo(_markersDataDict[marker.MarkerId], 0);
                                    _markersLastUpdateDict[marker.MarkerId] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                                }
                            }
                        }
                    }

                    DebugUtils.TrackTick();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            Debug.Log("Stopped marker tracking RESPONSE stream from Sidecar.");
        }

        async Task SendToSideCarStreaming()
        {
            try
            {
                lock (_bufferSync)
                {
                    // Copy camera buffers to Mat
                    MatUtils.copyToMat(_leftCamBuffer, _leftMat);
                    MatUtils.copyToMat(_rightCamBuffer, _rightMat);
                }

                // Use OpenCV to encode JPEG; Unity ImageConversion.EncodeArrayToJPG crashses with grayscale images
                var leftJpg = new MatOfByte();
                Imgcodecs.imencode(".jpg", _leftMat, leftJpg);
                var rightJpg = new MatOfByte();
                Imgcodecs.imencode(".jpg", _rightMat, rightJpg);

                if (InternalDebug.IsDebugMode)
                {
                    long diff = Math.Abs(_leftCamTimestamp - _rightCamTimestamp);
                    InternalDebug.Log(LogLevel.Debug, leftJpg.toArray(), "marker_LF", ".jpg");
                    InternalDebug.Log(LogLevel.Debug, rightJpg.toArray(), "marker_RF", ".jpg");
                    InternalDebug.Log(LogLevel.Debug, $"Tracker frame timestamps: {_leftCamTimestamp}, {_rightCamTimestamp} (diff: {diff})");
                }

                var request = new MarkerTrackerStereoRequest();
                request.LeftImage = ByteString.CopyFrom(leftJpg.toArray());
                request.RightImage = ByteString.CopyFrom(rightJpg.toArray());
                request.LeftMatrix = _leftToWorld;
                request.RightMatrix = _rightToWorld;

                leftJpg.Dispose();
                rightJpg.Dispose();

                await _requestStream.WriteAsync(request);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                Debug.Log("Timed out sending stero image feed to Sidecar");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Start()
        {
            if (IsRunning()) return;

            Debug.Log("Starting Marker Tracker...");
            _isRunning = true;
            _streamingTaskCTS = new CancellationTokenSource();
            _streamingTask = Task.Run(StartStreamingAsync, _streamingTaskCTS.Token);
        }

        public void Stop()
        {
            if (!IsRunning()) return;

            Debug.Log("Stopping Marker Tracker...");
            // Cancel a streaming task
            _streamingTaskCTS?.Cancel();

            try
            {
                // Wait for thread to exit
                _streamingTask?.Wait();
                _streamingTask = null;
                _streamingTaskCTS?.Dispose();
                _streamingTaskCTS = null;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _isRunning = false;
            Debug.Log("Marker Tracker stopped.");
        }

        public Vector3 GetWorldPosition(int id)
        {
            lock (_markersSync)
            {
                if(_markersDataDict.TryGetValue(id, out float[] arr) && IsDetected(id))
                    return new Vector3(arr[0], arr[1], arr[2]);
            }
            return Vector3.zero;
        }

        public Quaternion GetWorldRotation(int id)
        {
            lock (_markersSync)
            {
                if (_markersDataDict.TryGetValue(id, out float[] arr) && IsDetected(id))
                    return new Quaternion(arr[3], arr[4], arr[5], arr[6]);
            }
            return Quaternion.identity;
        }

        public bool IsDetected(int id)
        {
            lock (_markersSync)
            {
                return DateTimeOffset.Now.ToUnixTimeMilliseconds() - _markersLastUpdateDict[id] < _timeOutInMS;
            }
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            _responseTask?.Dispose();
            _streamingTask?.Dispose();
        }
    }
}
