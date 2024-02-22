using System;
using UnityEngine;
using Unity.Collections;
using System.Threading.Tasks;

namespace NUHS.VeinMapping.VeinProcess
{
    public struct ImageSize
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }
    
    public delegate void UpdatePointCloud(NativeArray<Vector4> pointCloud);
    public delegate void UpdateCrop(ImageSize crop);
    public delegate void UpdateDeltaTime(float deltaTime);

    public interface IVeinProcessor : IDisposable
    {        
        event UpdatePointCloud OnPointCloudUpdated;
        event UpdateCrop OnCropChanged;
        event UpdateDeltaTime OnDeltaTimeChanged;
        void Start();
        void Stop();
        Task Process(byte[] ir, byte[] depth, string d2w, CuboidPoints cuboid, ImageSize crop, bool saveData = false);
        bool IsRunning();
    }
}
