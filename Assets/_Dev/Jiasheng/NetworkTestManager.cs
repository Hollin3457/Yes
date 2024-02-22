using System.Collections;
using System.IO;
using NUHS.UltraSound.Reconstruction;
using NUHS.UltraSound.Recording;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class NetworkTestManager : MonoBehaviour
{
    [Header("Dependency")]
    [SerializeField] private Transform mockScannerTip;
    [SerializeField] private Material imageMat;
    [SerializeField] private bool autoControlMockScanner;
    [SerializeField] private Vector3 speed;
    [SerializeField] private Transform cuboid;
    [FormerlySerializedAs("objMeshLoader")] [SerializeField] private MeshVisualizationManager meshVisualizationManager;

    [Header("UI")]
    [SerializeField] private Button triggerScanButton;
    [SerializeField] private Text processStatus;

    private IUltrasoundReconstructor _reconstructor;
    private IUltrasoundRecorder _recorder;
    private string[] _imageFilePaths;

    void Start()
    {
        triggerScanButton.onClick.AddListener(StartScan);
        processStatus.text = "not started";
    }

    void StartScan()
    {
        processStatus.text = "waiting for results";
        
        _recorder = new TimeSamplingUltrasoundRecorder(30);
        
        _reconstructor = new BackendUltrasoundReconstructor();
        
        _imageFilePaths = Directory.GetFiles(Application.streamingAssetsPath + "/ImageSequence", "*.jpg"); // Modify the file extension if needed
        
        StartCoroutine(ScanningLoop());
    }

    private IEnumerator ScanningLoop()
    {
        int index = 0;
        while (index < _imageFilePaths.Length)
        {
            // Get frame from the dummy data files
            var filePath = _imageFilePaths[index];
            byte[] imageData = File.ReadAllBytes(filePath);
            Texture2D frame = new Texture2D(353, 253, TextureFormat.RGBA32, false);
            frame.LoadImage(imageData);
            imageMat.SetTexture("_MainTex", frame);

            if (autoControlMockScanner)
            {
                mockScannerTip.position += speed;
            }
            
            // Get the position and rotation from the mock scanner tip 
            _recorder.RecordFrame(frame, mockScannerTip.position, mockScannerTip.eulerAngles, cuboid.transform);
            index++;
            yield return new WaitForSeconds(0.02f);
        }
        // _reconstructor.Reconstruct(_recorder,new Vector2(50,50));
        StartCoroutine(ConstructionLoop());
    }

    private IEnumerator ConstructionLoop()
    {
        while (true)
        {
            if (_reconstructor.Status == UltrasoundReconstructionStatus.Complete)
            {
                processStatus.text = "done";
                break;
            }
            yield return new WaitForSeconds(1);
        }

        var meshString = _reconstructor.Mesh;
        meshVisualizationManager.LoadObj(meshString);
    } 
}
