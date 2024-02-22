using NUHS.UltraSound.Recording;
using NUHS.Backend;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Grpc.Core;
using NUHS.Common.InternalDebug;
using NUHS.UltraSound.Config;
using UnityEngine;

namespace NUHS.UltraSound.Reconstruction
{
    public class BackendUltrasoundReconstructor : IUltrasoundReconstructor
    {
        private int _pollIntervalMilliseconds;
        private int _timeoutMilliseconds;
        private Task _pollTask;
        private string _reconstructionId;

        public string FailMessage { get; private set; } = null;
        public string Mesh { get; private set; } = null;
        public UltrasoundReconstructionStatus Status { get; private set; } = UltrasoundReconstructionStatus.Idle;

        public BackendUltrasoundReconstructor(int pollIntervalMilliseconds = 2000, int timeoutMilliseconds = 180_000)
        {
            _pollIntervalMilliseconds = pollIntervalMilliseconds;
            _timeoutMilliseconds = timeoutMilliseconds;
        }

        public async void Reconstruct(IUltrasoundRecorder recorder, Vector2 pixelsPerCm, Channel reconstructionChannel, Func<Task> checkChannelConnection)
        {
            UltrasoundReconstructionServiceV1.UltrasoundReconstructionServiceV1Client service;
            UltrasoundReconstructionRequestV1 request;
            try
            {
                await checkChannelConnection();
                service = new UltrasoundReconstructionServiceV1.UltrasoundReconstructionServiceV1Client(reconstructionChannel);
                request = new UltrasoundReconstructionRequestV1()
                {
                    ReconstructionMethod = UltraSoundAppConfigManager.Instance.AppConfig.BackendReconstructionMethod,
                    CuboidSize = new Vec3()
                    {
                        X = recorder.Cuboid.localScale.x * 1000f,
                        Y = recorder.Cuboid.localScale.y * 1000f,
                        Z = recorder.Cuboid.localScale.z * 1000f
                    }, // Convert from unity unit (meter) to mm
                    PixelsPerMM = new Vec2() { X = pixelsPerCm.x * 0.1f, Y = pixelsPerCm.y * 0.1f }
                };

                // Add frames to request
                foreach (var f in _RemoveFramesOutsideCuboid(recorder.Frames, recorder.Cuboid.localScale))
                {
                    request.Images.Add(_ConvertToUltrasoundReconstructionCaptureImageV1(f));
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                FailMessage = "Failed to reach the server, please check your backend connection.";
                Status = UltrasoundReconstructionStatus.Failed;
                return;
            }

            var cts = new CancellationTokenSource();
            cts.CancelAfter(_timeoutMilliseconds);

            _pollTask = Task.Run(async () =>
            {
                try
                {
                    // Send the request to the API
                    var reconstructResponse = service.Reconstruct(request);
                    _reconstructionId = reconstructResponse.Id;
                    Status = UltrasoundReconstructionStatus.Reconstructing;

                    // Poll until status is Completed
                    while (Status == UltrasoundReconstructionStatus.Reconstructing)
                    {
                        if ((await service.StatusAsync(new UltrasoundReconstructionProcessIdV1() { Id = _reconstructionId })).Completed) break;
                        await Task.Delay(_pollIntervalMilliseconds);
                        if (cts.IsCancellationRequested)
                        {
                            Debug.LogError("Failed to reconstruct mesh because it took too long");
                            FailMessage = $"Timed out ({_timeoutMilliseconds}ms).";
                            Status = UltrasoundReconstructionStatus.Failed;
                            return;
                        }
                    }

                    // Get Mesh from API
                    var meshResponse = await service.MeshAsync(new UltrasoundReconstructionProcessIdV1() { Id = _reconstructionId });
                    if (string.IsNullOrEmpty(meshResponse.Data))
                    {
                        Debug.LogError("Failed to reconstruct mesh: server returned an empty string");
                        FailMessage = "Server returned an empty mesh.";
                        Status = UltrasoundReconstructionStatus.Failed;
                        return;
                    }
                    Mesh = meshResponse.Data;
                    if (InternalDebug.IsDebugMode)
                    {
                        InternalDebug.Log(LogLevel.Debug,Encoding.ASCII.GetBytes(Mesh),"recording","obj");
                    }
                    Status = UltrasoundReconstructionStatus.Complete;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to reconstruct mesh.");
                    Debug.LogException(ex);
                    FailMessage = ex.Message;
                    if (FailMessage.Length > 400)
                    {
                        FailMessage = FailMessage.Substring(0, 400); //truncate the message if it's too long
                    }
                    Status = UltrasoundReconstructionStatus.Failed;
                }
                finally
                {
                    cts.Dispose();
                }
            });
        }

        private IEnumerable<UltrasoundRecorderFrame> _RemoveFramesOutsideCuboid(IEnumerable<UltrasoundRecorderFrame> frames, Vector3 cuboidSize)
        {
            // TODO: Reject if frame is outside cuboid?
            //       Don't do this during capturing to reduce load on the HL2
            //       HOW do we determine the point? We don't know the center of the frame *yet

            //var bounds = new Bounds(Vector3.zero, _cuboidSize);
            //if (!bounds.Contains(localPosition)) ...;

            return frames;
        }

        private UltrasoundReconstructionCaptureImageV1 _ConvertToUltrasoundReconstructionCaptureImageV1(UltrasoundRecorderFrame frame)
        {
            return new UltrasoundReconstructionCaptureImageV1()
            {
                Data = Google.Protobuf.ByteString.CopyFrom(frame.Frame), // Convert texture to JPEG byte array
                Position = new Vec3() { X = frame.Position.x*1000f, Y = frame.Position.y*1000f, Z = frame.Position.z*1000f }, // Convert from unity unit (meter) to mm
                Rotation = new Vec3() { X = frame.Rotation.x, Y = frame.Rotation.y, Z = frame.Rotation.z }
            };
        }
    }
}
