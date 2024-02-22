using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using NUHS.Common.InternalDebug;
using NUHS.VeinMapping.VeinProcess;
using UnityEngine;
using Unity.Collections;
using OpenCVForUnity.CoreModule;
//using Google.Protobuf; // for bytestring (might not need anymore)

namespace NUHS.VeinMapping.ImageProcessing
{
    public delegate Task OnSensorStreamReceived(byte[] ir, byte[] depth, string d2w, ImageSize crop);

    public class VeinSensorStream
    {
        private const int MAX_STREAM_FPS = 30;

        public event OnSensorStreamReceived OnSensorStreamUpdated;

        public const int MaxPoints = 512 * 512;

        private bool _enablePointCloud = true; // set to false 20/3/22

        private bool _isStreaming = false;

        private object _bufferLock = new object();

        private float[] _pointCloudData;

        private NativeArray<float> _pointClouds;
        
        public bool IsUpdated = false;

        // incoming data
        private ushort[] _depthDataRaw, _irDataRaw;
        private byte[] _depthLUT;
        private bool _isDepthLUTReady;
        private object _cropLock = new object();
        private ImageSize _crop;
        private object _deltaTimeLock = new object();
        private int _deltaTime = 33;
        private Mat _dataMat;

        // reference
        private AppManager _appManager;

        public VeinSensorStream(AppManager appManager)
        {
            _appManager = appManager;
        }

        public bool IsDepthLUTReady()
        {
            return _isDepthLUTReady;
        }
        public byte[] GetDepthLUT()
        {
            return _depthLUT;
        }

        public bool IsStreaming()
        {
            return _isStreaming;
        }

        public void SetImageSize(ImageSize crop)
        {
            lock (_cropLock)
            {
                _crop = new ImageSize();
                _crop.x = crop.x;
                _crop.y = crop.y;
                _crop.w = crop.w;
                _crop.h = crop.h;
            }
            
        }

        public void SetDeltaTime(float deltaTime)
        {
            lock (_deltaTimeLock)
            {
                _deltaTime = (int) (deltaTime * 1000);
            }
        }

        async Task OnResearchModeAsync()
        {
            try
            {
                var targetLoopTime = 1000 / MAX_STREAM_FPS;

#if ENABLE_WINMD_SUPPORT
                while (_isStreaming)
                {
                    var t0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    //if (AppManager.ResearchMode.PointCloudUpdated())
                    if (AppManager.ResearchMode.DepthMapDataUpdated())
                    {
                        DebugUtils.VideoTick();
                        if (!_isDepthLUTReady)
                        {
                            _depthLUT = FLOATToBytes(AppManager.ResearchMode.GetDepthIntrinsics());
                            if (InternalDebug.IsDebugMode)
                            {
                                InternalDebug.Log(LogLevel.Debug, _depthLUT, "Depth_lut_data", ".raw");
                                Debug.Log("lut ..." + _depthLUT.Length.ToString());
                            }
                            _isDepthLUTReady = true;
                        }

                        OpenCVForUnity.CoreModule.Rect cropSize;
                        ImageSize _cropImageSize;
                        lock (_cropLock)
                        {
                            cropSize = new OpenCVForUnity.CoreModule.Rect(_crop.x, _crop.y, _crop.w, _crop.h);
                            _cropImageSize = new ImageSize();
                            _cropImageSize.x = _crop.x;
                            _cropImageSize.y = _crop.y;
                            _cropImageSize.w = _crop.w;
                            _cropImageSize.h = _crop.h;
                        }

                        //_pointCloudData = AppManager.ResearchMode.GetPointCloudBuffer(); // this is Point cloud raw
                        _depthDataRaw = AppManager.ResearchMode.GetDepthMapBuffer();
                        _irDataRaw = AppManager.ResearchMode.GetShortAbImageBuffer();
                        byte[] _depthData = UINT16ToCroppedBytes(_depthDataRaw, 512, 512, cropSize);
                        byte[] _irData = UINT16ToCroppedBytes(_irDataRaw, 512, 512, cropSize);
                        string _d2W = AppManager.ResearchMode.PrintDepthToWorld().ToString();

                        Debug.Log("depth ..." + _depthData.Length.ToString());
                        Debug.Log("infra ..." + _irData.Length.ToString());

                        if (InternalDebug.IsDebugMode)
                        {
                            InternalDebug.Log(LogLevel.Debug, UINT16ToBytes(_irDataRaw), "IR_raw", ".raw");
                            InternalDebug.Log(LogLevel.Debug, UINT16ToBytes(_depthDataRaw), "Depth_raw", ".raw");
                            InternalDebug.Log(LogLevel.Debug, $"Depth to world: {_d2W}");
                            InternalDebug.Log(LogLevel.Debug, $"Cropsize: {_cropImageSize.x}, {_cropImageSize.y}, {_cropImageSize.w}, {_cropImageSize.h}");
                        }

                        await OnSensorStreamUpdated?.Invoke(_irData, _depthData, _d2W, _cropImageSize);
                    }                    
                    var t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    var duration = (int)(t1 - t0);
                    Debug.Log($"VeinSensorStream.OnResearchModeAsync(): Loop Time {duration}ms, Delta Time {_deltaTime}ms");

                    if (_deltaTime < targetLoopTime) _deltaTime = targetLoopTime;
                    if (duration < _deltaTime)
                        await Task.Delay(_deltaTime - duration);
                }
#endif
            }
            catch (Exception e)
            {
                Debug.Log("Logging Exception ...");
                Debug.LogException(e);
            }
            finally{
                _isStreaming = false;
                if (!_appManager.IsSettingUp())
                {
                    _appManager.RetrySetup();
                }
            }
        }
        
        public void StartSensorStream()
        {
            Debug.Log("Start ResearchMode Streaming ...");
#if ENABLE_WINMD_SUPPORT
            AppManager.ResearchMode.StartDepthSensorLoop(_enablePointCloud);
#endif
            Debug.Log("Started Depth Sensor Loop ...");
            // Allocate the max possible capacity that ResearchMode.GetPointCloudBuffer
            // can return so we can allocate just once and reuse.
            // _pointClouds = new NativeArray<float>(MaxPoints * 4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _depthDataRaw = new ushort[MaxPoints];
            _irDataRaw = new ushort[MaxPoints];
            _depthLUT = new byte[MaxPoints * sizeof(float)];
            _dataMat = new Mat(512, 512, CvType.CV_16UC1);
            _crop = new ImageSize { x = 0, y = 0, w = 512, h = 512 };
            _isDepthLUTReady = false;

            Debug.Log("Allocated space for PC ...");
            _isStreaming = true;
            Task.Run(OnResearchModeAsync);
            Debug.Log("Start ResearchMode Async ...");
        }

        public void StopSensorStream()
        {
            Debug.Log("Stop ResearchMode Stream.");
            _isStreaming = false;
        }

        public void Dispose()
        {
            Debug.Log("Dispose vein sensor stream");
            _isStreaming = false;

            if (_pointClouds.IsCreated)
            {
                _pointClouds.Dispose();
            }
            _dataMat?.Dispose();
        }

        byte[] UINT16ToBytes(ushort[] data)
        {
            byte[] ushortInBytes = new byte[data.Length * sizeof(ushort)];
            System.Buffer.BlockCopy(data, 0, ushortInBytes, 0, ushortInBytes.Length);
            return ushortInBytes;
        }
        byte[] FLOATToBytes(float[] data)
        {
            byte[] floatInBytes = new byte[data.Length * sizeof(float)];
            System.Buffer.BlockCopy(data, 0, floatInBytes, 0, floatInBytes.Length);
            return floatInBytes;
        }

        byte[] UINT16ToCroppedBytes(ushort[] data, int width, int height, OpenCVForUnity.CoreModule.Rect cropSize)
        {
            OpenCVForUnity.UtilsModule.MatUtils.copyToMat(data, _dataMat);
            Mat croppedMat = new Mat(_dataMat, cropSize);

            byte[] croppedArray = new byte[croppedMat.total() * croppedMat.elemSize()];
            OpenCVForUnity.UtilsModule.MatUtils.copyFromMat(croppedMat, croppedArray);

            croppedMat.Dispose();

            return croppedArray;
        }
    }
}
