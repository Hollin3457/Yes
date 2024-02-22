using UnityEngine;
using Grpc.Core;
using NUHS.Backend;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace NUHS.UltraSound.Network
{
    /// <summary>
    /// This img obj are used to store 
    /// </summary>
    public class ImageObj
    {
        public string ImageFilePath;
        public Vector3 Pos = new Vector3(0, 0, 0);
        public Vector3 Rot = new Vector3(0, 0, 0);
    }

    public class NetworkController : MonoBehaviour
    {
        public enum NetworkState { Idle, LoadImageSequence, SendImageSequence, WaitForProcess, Receive3dMesh }
        public NetworkState CurrentNetworkState;

        // Configuration python server parameters
        public string ServerAddress = "192.168.166.32";
        public int ServerPort = 51000;

        // Image Sequence parameters
        public int BatchSize = 10;

        private Channel channel;
        private UltrasoundReconstructionServiceV1.UltrasoundReconstructionServiceV1Client reconService;
        private List<ImageObj> imageObjList = new List<ImageObj>();

        private bool _status;
        private string _meshResult;

        private void Start()
        {
            CurrentNetworkState = NetworkState.Idle;

            // Generate Dummy Image Sequence
            string[] imageFilePaths = Directory.GetFiles(Application.persistentDataPath + "/ImageSequence", "*.jpg"); // Modify the file extension if needed
            for (int i = 0; i < imageFilePaths.Length; i ++)
            {
                ImageObj tempImgObj = new ImageObj();
                tempImgObj.ImageFilePath = imageFilePaths[i];
                tempImgObj.Pos = new Vector3(50, 90, 0.2f * (i+10));
                imageObjList.Add(tempImgObj);
            }
        }
        
        public bool GetStatus()
        {
            return _status;
        }

        public string GetMeshString()
        {
            return _meshResult;
        }

        public void StartReconSequence()
        {
            StartCoroutine(IStartReconSequence());
        }

        private IEnumerator IStartReconSequence()
        {
            _status = false;
            
            // Set state
            CurrentNetworkState = NetworkState.SendImageSequence;

            // Create a gRPC channel and client
            var channelOptions = new List<ChannelOption>();
            channelOptions.Add(new ChannelOption("grpc.max_receive_message_length", 128000000));
            channel = new Channel(ServerAddress, ServerPort, ChannelCredentials.Insecure,channelOptions);

            var reconService = new UltrasoundReconstructionServiceV1.UltrasoundReconstructionServiceV1Client(channel);
            var request = new UltrasoundReconstructionRequestV1();
            // set cuboid resolution (note: cuboid size in meter)
            request.CuboidSize = new Vec3();
            request.CuboidSize.X = 100f;
            request.CuboidSize.Y = 100f;
            request.CuboidSize.Z = 100f;
            request.PixelsPerMM = new Vec2();
            request.PixelsPerMM.X = 5;
            request.PixelsPerMM.Y = 5;
            request.ReconstructionMethod = 1;

            // read imagesSequence and add to request
            yield return StartCoroutine(LoadImageSequence(request));
            Debug.Log($"Image count: {request.Images.Count}");

            // send request to reconservice
            var response = reconService.Reconstruct(request);
            Debug.Log($"Got an ID: {response.Id}");

            // ping for status to receive mesh
            while (!_status)
            {
                _status = reconService.Status(new UltrasoundReconstructionProcessIdV1 { Id = response.Id }).Completed;
                yield return new WaitForSeconds(1);
            }
            
            // get mesh
            var meshResponse = reconService.Mesh(new UltrasoundReconstructionProcessIdV1 { Id = response.Id });

            _meshResult = meshResponse.Data; // OBJ text

            // Shutdown the gRPC channel
            channel.ShutdownAsync().Wait();
        }

        private IEnumerator LoadImageSequence(UltrasoundReconstructionRequestV1 request)
        {
            CurrentNetworkState = NetworkState.LoadImageSequence;

            // Load images incrementally in batches to request
            for (int i = 0; i < imageObjList.Count; i += BatchSize)
            {
                int endIndex = Mathf.Min(i + BatchSize, imageObjList.Count);

                for (int j = i; j < endIndex; j++)
                {
                    byte[] imageData = File.ReadAllBytes(imageObjList[j].ImageFilePath);

                    var image = new UltrasoundReconstructionCaptureImageV1();
                    image.Data = Google.Protobuf.ByteString.CopyFrom(imageData);
                    image.Position = new Vec3();
                    image.Position.X = imageObjList[j].Pos.x;
                    image.Position.Y = imageObjList[j].Pos.y;
                    image.Position.Z = imageObjList[j].Pos.z;
                    image.Rotation = new Vec3();
                    image.Rotation.X = imageObjList[j].Rot.x;
                    image.Rotation.Y = imageObjList[j].Rot.y;
                    image.Rotation.Z = imageObjList[j].Rot.z;
                    request.Images.Add(image);

                    yield return null;
                }
            }
            Debug.Log("Finished loading images...");
        }

        private void OnDestroy()
        {
            if (channel != null)
                channel.ShutdownAsync().Wait();
        }
    }
}

