using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using UnityEngine;
using NUHS.Common.InternalDebug;
using NUHS.Sidecar;

namespace NUHS.UltraSound.ImageReceiving
{
    /// <summary>
    /// An Image Receiver that connects to the Sidecar via a gRPC Channel
    /// </summary>
    public class SidecarUltrasoundImageReceiver : IUltrasoundImageReceiver
    {
        private SidecarUltrasoundConfig _config;
        private Texture2D _image;
        private Vector2 _pixelsPerCm;
        private Vector2 _size;

        private bool _isRunning = false;

        private Channel _channel;
        private Task _streamingTask;
        private CancellationTokenSource _streamingTaskCTS;

        private object _bufferLock = new object();
        private byte[] _buffer;
        private bool _bufferChanged = false;

        public bool IsRunning() => _isRunning;
        public Vector2 GetImageSize() => _size;
        public Vector2 GetPixelsPerCm() => _pixelsPerCm;

        public SidecarUltrasoundImageReceiver(SidecarUltrasoundConfig config, Channel channel)
        {
            _config = config;
            _channel = channel;

            // Create an empty texture for use later
            _image = new Texture2D((int)_config.CaptureW, (int)_config.CaptureH, TextureFormat.RGBA32, false);

            // Load PPCM from config
            // TODO: PPCM should have different X and Y values
            _pixelsPerCm = new Vector2(_config.PixelsPerCm, _config.PixelsPerCm);

            // Calculate its static size if PPCM detection is not enabled
            // Also prevent divide by zero error if PPCM is zero
            if (_config.DetectPixelsPerCm || _config.PixelsPerCm == 0) _size = new Vector2(1f, 1f);
            else _size = new Vector2(_config.CropW, _config.CropH) / _pixelsPerCm * 0.01f;
        }
        

        public void Start()
        {
            if (_isRunning) return;

            // Run an async thread to read the frames from the stream
            _streamingTaskCTS = new CancellationTokenSource();
            _streamingTask = Task.Run(async () =>
            {                
                try
                {
                    // Flag to track if streaming is active
                    _isRunning = true;

                    var captureService = new UltrasoundCaptureServiceV1.UltrasoundCaptureServiceV1Client(_channel);

                    // Setup the capture on the sidecar with the desired parameters
                    Debug.Log("Performing Image Receiver Setup...");
                    var setupReply = captureService.Setup(new UltrasoundCaptureSetupRequestV1
                    {
                        DeviceId = _config.DeviceId,
                        CaptureW = _config.CaptureW,
                        CaptureH = _config.CaptureH,
                        CropX = _config.CropX,
                        CropY = _config.CropY,
                        CropW = _config.CropW,
                        CropH = _config.CropH,
                        DetectPixelsPerCm = _config.DetectPixelsPerCm,
                        DetectPixelsPerCmMethod = _config.DetectPixelsPerCmMethod
                    });

                    // Start streaming
                    Debug.Log("Image Receiver Streaming starting...");
                    var call = captureService.Stream(new UltrasoundCaptureRequestV1
                    {
                        Fps = _config.FPS,
                        Quality = _config.Quality,
                        Restart = setupReply.RestartRequired
                    });

                    while (await call.ResponseStream.MoveNext(_streamingTaskCTS.Token))
                    {
                        lock (_bufferLock)
                        {
                            _buffer = call.ResponseStream.Current.Data.ToByteArray();

                            // Update the PPCM if we are doing auto detection
                            // WARNING: THIS FEATURE IS NOT IMPLEMENTED ON THE SIDECAR YET
                            if (_config.DetectPixelsPerCm && false) // TODO: Remove && false once feature is ready
                            {
                                _pixelsPerCm = new Vector2(call.ResponseStream.Current.PixelsPerCm, call.ResponseStream.Current.PixelsPerCm);
                                _size = new Vector2(_config.CropW, _config.CropH) * _pixelsPerCm * 0.01f;
                            }

                            // Flag to indicate that the buffer has changed
                            _bufferChanged = true;
                        }
                    }

                    Debug.Log("Image Receiver Streaming stopped.");
                    call.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                // Set flag if loop exits for whatever reason
                _isRunning = false;
            }, _streamingTaskCTS.Token);
        }

        public void Stop()
        {
            if (!_isRunning) return;

            Debug.Log("Stopping Image Receiver...");
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

            _buffer = null;
            _isRunning = false;
            Debug.Log("Image Receiver stopped.");
        }

        public Texture2D GetImage()
        {
            lock (_bufferLock)
            {
                if (_bufferChanged && _buffer != null && _buffer.Length > 0)
                {
                    _image.LoadImage(_buffer);
                    _bufferChanged = false;
                }
            }

            return _image;
        }

        public void UpdateImage()
        {
            lock (_bufferLock)
            {
                if (_bufferChanged && _buffer != null && _buffer.Length > 0)
                {
                    _image.LoadImage(_buffer);
                    DebugUtils.VideoTick();
                    if (InternalDebug.IsDebugMode)
                    {
                        InternalDebug.Log(LogLevel.Debug,_image,"receiving");
                    }
                    _bufferChanged = false;
                }
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

            _streamingTask?.Dispose();
        }
    }
}
