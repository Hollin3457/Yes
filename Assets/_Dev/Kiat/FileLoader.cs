using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FileLoader : MonoBehaviour
{
    public string filePath;
    [SerializeField] Image debugImage1;
    [SerializeField] Image debugImage2;
    public Transform cubeObj;
    private Texture2D debugTexture1, debugTexture2;
    private string[] pcFiles;
    private string[] irFiles;
    private int fileCount;
    private int count;
    private Mat originalIR, originalDepth;
    private float timeElapsed;
    private bool hasNew = true;
    private bool spawn = false;
 
    private List<GameObject> gameobjectList = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        originalIR = new Mat(512, 512, CvType.CV_8UC1);
        originalDepth = new Mat(512, 512, CvType.CV_32FC4);

        pcFiles = Directory.GetFiles(Application.persistentDataPath + "/Test", "PointCloud_data*");
        irFiles = Directory.GetFiles(Application.persistentDataPath + "/Test", "ir_data*");
        foreach(var f in pcFiles)
        {
            Debug.Log(f);
        }
        foreach (var f in irFiles)
        {
            Debug.Log(f);
        }
        fileCount = pcFiles.Length;
        count = 0;

        timeElapsed = 0;
        Debug.Log("end");
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (hasNew)
        {
            hasNew = false;
            Debug.Log(pcFiles[count]);
            Debug.Log(irFiles[count]);
            byte[] IRdata = File.ReadAllBytes(irFiles[count]);

            byte[] PointClouddata = File.ReadAllBytes(pcFiles[count]);
            float[] floatArray = new float[PointClouddata.Length / sizeof(float)];
            Debug.Log("Length float:" + floatArray.Length);
            Buffer.BlockCopy(PointClouddata, 0, floatArray, 0, PointClouddata.Length);

            Matrix4x4 matrixTrans = Matrix4x4.identity;
            matrixTrans.SetTRS(cubeObj.transform.position, cubeObj.transform.rotation, cubeObj.transform.localScale);

            ConcurrentQueue<Vector4> vector4Array = new ConcurrentQueue<Vector4>();
            Parallel.For(0, floatArray.Length / 4, i =>
            {
                Vector4 vector4 = new Vector4(floatArray[i * 4], floatArray[i * 4 + 1], floatArray[i * 4 + 2], floatArray[i * 4 + 3]);
                Vector3 localPos = matrixTrans.inverse.MultiplyPoint3x4(vector4);
                
                if (Mathf.Abs(localPos.x) < 0.5f && Mathf.Abs(localPos.y) < 0.5f && Mathf.Abs(localPos.z) < 0.5f)
                {
                    vector4Array.Enqueue(vector4);
                }
            });
            Debug.Log("Length vector4:" + vector4Array.Count);

            VisualizePointCloud(vector4Array.ToArray());
        }
        if (timeElapsed > 0.2f)
        {
            timeElapsed = 0;
            count++;
            if (count >= fileCount)
            {
                count = 0;
            }
            hasNew = true;
        }

    }

    public byte[] MatToByteArray(Mat mat)
    {
        Mat flattenedMat = mat.reshape(1, (int)mat.total());
        byte[] byteArray = new byte[flattenedMat.total() * flattenedMat.elemSize()];
        MatUtils.copyFromMat(flattenedMat, byteArray);

        return byteArray;
    }

    async void VisualizePointCloud(Vector4[] points)
    {
        foreach(var gameObj in gameobjectList)
        {
            Destroy(gameObj);
        }
        gameobjectList.Clear();
        foreach (Vector4 point in points)
        {
            // Instantiate a point visualization object (e.g., a sphere) at the 3D position.
            GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointObject.transform.position = new Vector3((float)point.x, (float)point.y, (float)point.z);
            pointObject.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f); // Adjust the scale of the points for better visibility.
            gameobjectList.Add(pointObject);
        }
    }
}
