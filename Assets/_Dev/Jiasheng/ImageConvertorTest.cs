using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUHS.VeinMapping.VeinProcess;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

public class ImageConvertorTest : MonoBehaviour
{
    public Transform cubeTransform;
    public Material material;
    public GPUInstancingPointCloud gpuInstancingPointCloud;
    public BoxCollider cuboidBoxCollider;

    private string _file =
        "C:/Users/Jiasheng Tang/AppData/LocalLow/NUHS/MR Ultrasound/Test/PointCloud_data-2023-08-07_12-57-20-981.raw";
    private string[] pcFiles;
    private int _count = 0;
    private VeinProcessor _veinProcessor;
    private Texture2D _texture2D;
    private List<Vector4> _outputPoints = new List<Vector4>();

    public BoolEvent onVisibilityToggled;
    
    void Start()
    {
        pcFiles = Directory.GetFiles(Application.persistentDataPath + "/Test", "PointCloud_data*");
        _veinProcessor = new VeinProcessor(cubeTransform, new ThresholdVeinSegmentator(), new BurstPointCloudFilter(512 * 512), 256);
        _texture2D = new Texture2D(256, 256, TextureFormat.RGB24, false);
        material.SetTexture("_MainTex", _texture2D);

        // CheckFile(_file);
    }
    
    private void Update()
    {
        if (_count < pcFiles.Length)
        {
            var file = pcFiles[_count];
            CheckFile(file);
            _count++;
        }
    }

    void CheckFile(string file)
    {
        byte[] PointClouddata = File.ReadAllBytes(file);
        float[] floatArray = new float[PointClouddata.Length / sizeof(float)];
        Buffer.BlockCopy(PointClouddata, 0, floatArray, 0, PointClouddata.Length);

        // TODO: Adapt test code to work with new _veinProcessor.Process signature (float[] -> NativeArray<float>)
        //_veinProcessor.Process(floatArray,_outputPoints);
        //gpuInstancingPointCloud.UpdatePoints(_outputPoints,cuboidBoxCollider.bounds);
        // Utils.matToTexture2D(_veinProcessor.SegmentationOutputMat,_texture2D, false);
    }

    void VisualizePointCloud(Vector4[] points)
    {
        foreach (Vector4 point in points)
        {
            // Instantiate a point visualization object (e.g., a sphere) at the 3D position.
            GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointObject.transform.position = new Vector3((float) point.x, (float) point.y, (float) point.z);
            pointObject.transform.localScale =
                new Vector3(0.005f, 0.005f, 0.005f); // Adjust the scale of the points for better visibility.
        }
    }

    [ContextMenu("SetVisibilityOn")]
    public void SetVisibilityOn()
    {
        onVisibilityToggled.Send(true);
    }
    [ContextMenu("SetVisibilityOff")]
    public void SetVisibilityOff()
    {
        onVisibilityToggled.Send(false);
    }
}
