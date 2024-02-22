using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using Grpc.Core;
using NUHS.VeinServer;

namespace NUHS.VeinMapping.VeinProcess
{
    public class ServerVeinProcessor : IVeinProcessor
    {
        private string _deviceId, _veinMapperType;
        private IServerImageCompressor _compressor;
        private byte[] _lut;
        bool _isRunning = false;

        private VeinMapperServiceV1.VeinMapperServiceV1Client _service;
        private IAsyncStreamWriter<VeinMapperStreamRequestV1> _requestStream;
        private IAsyncStreamReader<VeinMapperStreamResponseV1> _responseStream;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _responseTask;
        private int _pointCloudSize;

        private NativeArray<Vector4> _pointCloudBuffer;
        private int _pointCloudBufferLength;

        public event UpdatePointCloud OnPointCloudUpdated;
        public event UpdateCrop OnCropChanged;
        public event UpdateDeltaTime OnDeltaTimeChanged;

        private AppManager _appManager;

        public ServerVeinProcessor(AppManager appManager, Channel channel, string deviceId, byte[] lut, string veinMapperType, ServerImageCompressionType compressionType, int pointCloudSize = 512*512)
        {
            _appManager = appManager;
            _service = new VeinMapperServiceV1.VeinMapperServiceV1Client(channel);
            _deviceId = deviceId;
            _lut = lut;
            _veinMapperType = veinMapperType;
            _compressor = ServerImageCompressorFactory.CreateFromType(compressionType);
            _pointCloudSize = pointCloudSize;
            AllocatePointCloudBuffer();
        }

        public void Start()
        {
            // Prevent starting if already started
            if (_isRunning) return;

            SetupServerWithDeviceAndLUT();
            CreateStreamCall();
            StartResponseReader();
        }

        public void Stop()
        {
            StopResponseReader();
        }

        public void Dispose()
        {
            DeallocatePointCloudBuffer();
        }

        /// <summary>
        /// Process current ir, depth, d2w with used crop, and next current cuboid position 
        /// </summary>
        /// <param name="ir">Infra Red data UINT16 in bytes</param>
        /// <param name="depth">Depth data UINT16 in bytes</param>
        /// <param name="d2w">Depth to world matrix</param>
        /// <param name="cuboid">cuboid in default rgb coordinate</param>
        /// <param name="crop">Imagesize used to retrieved IR, Depth data</param>
        /// <param name="saveData">Flag to inform server whether to save data received</param>
        public async Task Process(byte[] ir, byte[] depth, string d2w, CuboidPoints cuboid, ImageSize crop, bool saveData = false)
        {
            var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await _requestStream?.WriteAsync(new VeinMapperStreamRequestV1
            {
                DeviceID = _deviceId,
                IrImage = Google.Protobuf.ByteString.CopyFrom(_compressor.Compress(ir)),
                DepthImage = Google.Protobuf.ByteString.CopyFrom(_compressor.Compress(depth)),
                DepthToWorldMatrix = d2w,
                Cuboid = ConvertToGrpcCuboid(cuboid),
                VeinMapperType = _veinMapperType,
                ImageCompressionType = _compressor.TypeString(),
                SaveData = saveData,
                CropRegion = ConverToGrpcImageSize(crop),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var processTime = endTime - startTime;
            Debug.Log($"ServerVeinProcessor.Process(): gRPC Write Time: {processTime}ms");
        }

        private void SetupServerWithDeviceAndLUT()
        {
            // Call gRPC Setup() to set up the device LUT
            var response = _service.Setup(new VeinMapperSetupRequestV1
            {
                DeviceID = _deviceId,
                Lut = Google.Protobuf.ByteString.CopyFrom(_lut)
            });

            // Should never be the case but who knows
            if (response.DeviceID != _deviceId) throw new Exception("Server responded with different device ID!?");
        }

        private void AllocatePointCloudBuffer()
        {
            // Allocate persistent memory for point cloud
            _pointCloudBuffer = new NativeArray<Vector4>(_pointCloudSize, Allocator.Persistent);
            _pointCloudBufferLength = 0; // Nothing in the array, set to zero first
        }

        private void DeallocatePointCloudBuffer()
        {
            _pointCloudBuffer.Dispose();
        }

        private void CreateStreamCall()
        {
            // Cancellation token for the stream
            _cancellationTokenSource = new CancellationTokenSource();

            var call = _service.Stream(cancellationToken: _cancellationTokenSource.Token);
            _requestStream = call.RequestStream;
            _responseStream = call.ResponseStream;
        }

        private void StartResponseReader()
        {
            _responseTask = Task.Run(ResponseReader);
        }

        private void StopResponseReader()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _responseTask?.Wait();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _cancellationTokenSource = null;
                _responseTask = null;
                _requestStream = null;
                _responseStream = null;
                _isRunning = false;
            }
        }

        private async Task ResponseReader()
        {
            _isRunning = true;

            try
            {
                while (await _responseStream.MoveNext() && !_cancellationTokenSource.IsCancellationRequested)
                {
                    CopyPointsToPointCloudBuffer(_responseStream.Current.Points);
                    var pointCloud = _pointCloudBuffer.GetSubArray(0, _pointCloudBufferLength);
                    OnPointCloudUpdated?.Invoke(pointCloud);
                    OnCropChanged?.Invoke(ConvertFromGrpcImageSize(_responseStream.Current.CropRegion));
                    OnDeltaTimeChanged?.Invoke(_responseStream.Current.DeltaTime);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRunning = false;
                if (!_appManager.IsSettingUp())
                {
                    _appManager.RetrySetup();
                }
            }
        }

        private void CopyPointsToPointCloudBuffer(Google.Protobuf.Collections.RepeatedField<Vec4> points)
        {
            if (points.Count > _pointCloudSize)
            {
                Debug.Log($"ServerVeinProcessor.CopyPointsToPointCloudBuffer(): Received point cloud {points.Count} exceeds buffer length ({_pointCloudSize})!");
                return;
            }

            for (int i = 0; i < points.Count; i++)
            {
                _pointCloudBuffer[i] = ConvertFromGrpcVec4ToVector4(points[i]);
            }

            _pointCloudBufferLength = points.Count;
        }

        private Vector4 ConvertFromGrpcVec4ToVector4(Vec4 v)
        {
            return new Vector4(v.X, v.Y, v.Z, v.W);
        }

        private Vec3 ConvertToGrpcVec3FromVector3(Vector4 v)
        {
            return new Vec3()
            {
                X = v.x,
                Y = v.y,
                Z = v.z
            };
        }

        private ImageSize ConvertFromGrpcImageSize(VeinServer.ImageSize s)
        {
            // this s == null can be removed in future
            if(s == null) { return new ImageSize() { x = 0, y = 0, w = 512, h = 512 }; }
            return new ImageSize()
            {
                x = s.X,
                y = s.Y,
                w = s.W,
                h = s.H
            };
        }

        private VeinServer.ImageSize ConverToGrpcImageSize(ImageSize s)
        {
            return new VeinServer.ImageSize()
            {
                X = s.x,
                Y = s.y,
                W = s.w,
                H = s.h
            };
        }

        private VeinServer.Cuboid ConvertToGrpcCuboid(CuboidPoints c)
        {
            return new VeinServer.Cuboid()
            {
                Origin = ConvertToGrpcVec3FromVector3(c.origin),
                Corner1 = ConvertToGrpcVec3FromVector3(c.corner1),
                Corner2 = ConvertToGrpcVec3FromVector3(c.corner2),
                Corner3 = ConvertToGrpcVec3FromVector3(c.corner3)
            };
        }

        public bool IsRunning()
        {
            return _isRunning;
        }
    }
}
