using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TestCroppedSpeed : MonoBehaviour
{
    [SerializeField] Image debugImage0;
    [SerializeField] Image debugImage1;
    [SerializeField] Image debugImage2;
    [SerializeField] Image debugImage3;
    private Texture2D image0, image1, image2, image3;

    private string[] depthDataFiles, depthRawFiles;
    private string[] irDataFiles, irRawFiles;
    private int fileCount;
    private int count;
    private Mat originalIR, originalDepth;
    private float timeElapsed;

    private byte[] IRrawMatinBytes;

    private bool hasNew = true;

    // Start is called before the first frame update
    void Start()
    {

        depthDataFiles = Directory.GetFiles(Application.persistentDataPath + "/Test", "Depth_data*");
        depthRawFiles = Directory.GetFiles(Application.persistentDataPath + "/Test", "Depth_raw*");
        irDataFiles = Directory.GetFiles(Application.persistentDataPath + "/Test", "IR_data*");
        irRawFiles = Directory.GetFiles(Application.persistentDataPath + "/Test", "IR_raw*");
        fileCount = depthDataFiles.Length;
        count = 0;

        timeElapsed = 0;

        image0 = new Texture2D(512, 512, TextureFormat.R8, false);
        image1 = new Texture2D(512, 512, TextureFormat.R8, false);
        image2 = new Texture2D(512, 512, TextureFormat.R8, false);
        image3 = new Texture2D(512, 512, TextureFormat.R8, false);

        debugImage0.sprite = Sprite.Create(image0, new UnityEngine.Rect(0, 0, 512, 512), new Vector2(256f, 256f));
        debugImage1.sprite = Sprite.Create(image1, new UnityEngine.Rect(0, 0, 512, 512), new Vector2(256f, 256f));
        debugImage2.sprite = Sprite.Create(image2, new UnityEngine.Rect(0, 0, 512, 512), new Vector2(256f, 256f));
        debugImage3.sprite = Sprite.Create(image3, new UnityEngine.Rect(0, 0, 512, 512), new Vector2(256f, 256f));
        Debug.Log("end");

        IRrawMatinBytes = new byte[512 * 512 * sizeof(ushort)];
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        if (hasNew)
        {
            hasNew = false;
            byte[] IRraw = File.ReadAllBytes(irRawFiles[count]);
            byte[] Depthraw = File.ReadAllBytes(depthRawFiles[count]);

            image0.LoadRawTextureData(Process(IRraw, 512, 512));
            image0.Apply();
            image1.LoadRawTextureData(Process(Depthraw, 512, 512));
            image1.Apply();

            byte[] IRdata = File.ReadAllBytes(irDataFiles[count]);
            byte[] Depthdata = File.ReadAllBytes(depthDataFiles[count]);
            Debug.Log("Update: " + count);
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

    private byte[] Process(byte[] data, int width, int height)
    {
        byte[] result = new byte[data.Length];
        Mat Mat16bit = new Mat(height, width, CvType.CV_16UC1);
        MatUtils.copyToMat(data, Mat16bit);
        Mat Mat8bit = new Mat(height, width, CvType.CV_8UC1);
        Core.convertScaleAbs(Mat16bit, Mat8bit);
        Core.normalize(Mat8bit, Mat8bit, 0, 255, Core.NORM_MINMAX, CvType.CV_8UC1);
        MatUtils.copyFromMat(Mat8bit, result);

        Mat16bit.Dispose();
        Mat8bit.Dispose();
        return result;
    }

    private ushort[] ByteToUShortArray(byte[] data)
    {
        ushort[] ushortArray = new ushort[data.Length / 4];
        Buffer.BlockCopy(data, 0, ushortArray, 0, ushortArray.Length);
        return ushortArray;
    }

    private byte[] UINT16ToBytes(ushort[] data)
    {
        byte[] ushortInBytes = new byte[data.Length * sizeof(ushort)];
        Buffer.BlockCopy(data, 0, ushortInBytes, 0, ushortInBytes.Length);
        return ushortInBytes;
    }
}
