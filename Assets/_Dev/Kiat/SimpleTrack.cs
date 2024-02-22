using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils.Helper;
using HoloLensCameraStream;
using NUHS.UltraSound.Tracking;
using NUHS.Common.InternalDebug;
#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class SimpleTrack : MonoBehaviour
{
    // The webcam texture to mat helper.(To change researchmode in future)
    private HLCameraStreamToMatHelper webCamTextureToMatHelper;

    private Mat camMatrix;
    private MatOfDouble distCoeffs;

    //The matrix that inverts the Y-axis.
    private Matrix4x4 invertYM;

    //The matrix that inverts the Z-axis.
    private Matrix4x4 invertZM;

    //The transformation matrix.
    private Matrix4x4 transformationM;
    private Matrix4x4 localToWorldMatrix;
    //The matrix for AR Marker
    private Matrix4x4 ARM;

    // for CanonicalMarker.
    private Mat ids;
    private List<Mat> corners;
    private List<Mat> rejectedCorners;
    private Dictionary dictionary;
    private ArucoDetector arucoDetector;

    private Mat rvecs;
    private Mat tvecs;
    private bool hasUpdatedARTransformMatrix = false;
    private bool enableDetection = true;
    private bool isDetecting = false;

    object sync = new object();

    [HeaderAttribute("Debug")]

    public Text renderFPS;
    public Text videoFPS;
    public Text trackFPS;
    public Text debugStr;

    // Start is called before the first frame update
    protected void Start()
    {
        webCamTextureToMatHelper = gameObject.GetComponent<HLCameraStreamToMatHelper>();
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
        webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
#endif
        webCamTextureToMatHelper.outputColorFormat = WebCamTextureToMatHelper.ColorFormat.GRAY;
        webCamTextureToMatHelper.Initialize();
    }

    public void OnWebCamTextureToMatHelperInitialized()
    {
        Debug.Log("OnWebCamTextureToMatHelperInitialized");

        Mat grayMat = webCamTextureToMatHelper.GetMat();

        float rawFrameWidth = grayMat.width();
        float rawFrameHeight = grayMat.height();
        DebugUtils.AddDebugStr(webCamTextureToMatHelper.outputColorFormat.ToString() + " " + webCamTextureToMatHelper.GetWidth() + " x " + webCamTextureToMatHelper.GetHeight() + " : " + webCamTextureToMatHelper.GetFPS());
        grayMat.Dispose();

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

        CameraIntrinsics cameraIntrinsics = webCamTextureToMatHelper.GetCameraIntrinsics();

        camMatrix = CreateCameraMatrix(cameraIntrinsics.FocalLengthX, cameraIntrinsics.FocalLengthY, cameraIntrinsics.PrincipalPointX, cameraIntrinsics.PrincipalPointY);
        distCoeffs = new MatOfDouble(cameraIntrinsics.RadialDistK1, cameraIntrinsics.RadialDistK2, cameraIntrinsics.RadialDistK3, cameraIntrinsics.TangentialDistP1, cameraIntrinsics.TangentialDistP2);

        Debug.Log("Created CameraParameters from VideoMediaFrame.CameraIntrinsics on device.");
        DebugUtils.AddDebugStr("Created CameraParameters from VideoMediaFrame.CameraIntrinsics on device.");
#else
        camMatrix = CreateCameraMatrix(1000, 1000, 960, 540);
        distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);
#endif
        Debug.Log("camMatrix " + camMatrix.dump());
        Debug.Log("distCoeffs " + distCoeffs.dump());

        //Calibration camera
        Size imageSize = new Size(rawFrameWidth, rawFrameHeight);
        double apertureWidth = 0;
        double apertureHeight = 0;
        double[] fovx = new double[1];
        double[] fovy = new double[1];
        double[] focalLength = new double[1];
        Point principalPoint = new Point(0, 0);
        double[] aspectratio = new double[1];

        Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

        Debug.Log("imageSize " + imageSize.ToString());
        Debug.Log("apertureWidth " + apertureWidth);
        Debug.Log("apertureHeight " + apertureHeight);
        Debug.Log("fovx " + fovx[0]);
        Debug.Log("fovy " + fovy[0]);
        Debug.Log("focalLength " + focalLength[0]);
        Debug.Log("principalPoint " + principalPoint.ToString());
        Debug.Log("aspectratio " + aspectratio[0]);

        //        // Display objects near the camera.
        //        arCamera.nearClipPlane = 0.1f;

        ids = new Mat();
        corners = new List<Mat>();
        rejectedCorners = new List<Mat>();
        rvecs = new Mat(1, 10, CvType.CV_64FC3);
        tvecs = new Mat(1, 10, CvType.CV_64FC3);
        dictionary = Objdetect.getPredefinedDictionary(Objdetect.DICT_4X4_50);

        DetectorParameters detectorParams = new DetectorParameters();
        detectorParams.set_useAruco3Detection(true);
        RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
        arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);

        //        transformationM = new Matrix4x4();

        //        invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
        //        Debug.Log("invertYM " + invertYM.ToString());

        //        invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        //        Debug.Log("invertZM " + invertZM.ToString());

        if (!webCamTextureToMatHelper.IsPlaying())
        {
            webCamTextureToMatHelper.Play();
        }
    }

    public void OnWebCamTextureToMatHelperDisposed()
    {
        Debug.Log("OnWebCamTextureToMatHelperDisposed");

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

        //lock (ExecuteOnMainThread)
        //{
        //    ExecuteOnMainThread.Clear();
        //}
#endif
        //hasUpdatedARTransformMatrix = false;
        enableDetection = true;
        //isDetecting = false;

        //if (arucoDetector != null)
        //    arucoDetector.Dispose();

        //if (ids != null)
        //    ids.Dispose();
        //foreach (var item in corners)
        //{
        //    item.Dispose();
        //}
        //corners.Clear();
        //foreach (var item in rejectedCorners)
        //{
        //    item.Dispose();
        //}
        //rejectedCorners.Clear();
        //if (rvecs != null)
        //    rvecs.Dispose();
        //if (tvecs != null)
        //    tvecs.Dispose();
        //DebugUtils.ClearDebugStr();
    }

    /// <summary>
    /// Raises the web cam texture to mat helper error occurred event.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
    }

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

    public void OnFrameMatAcquired(Mat grayMat, Matrix4x4 cameraToWorldMatrix, CameraIntrinsics cameraIntrinsics)
    {
        DebugUtils.VideoTick();
        if (enableDetection)
        {
            try
            {
                Debug.Log("Detect start !");
                // Detect markers and estimate Pose
                Mat undistortMat = new Mat();
                Calib3d.undistort(grayMat, undistortMat, camMatrix, distCoeffs);
                grayMat.Dispose();
                undistortMat.Dispose();
                //arucoDetector.detectMarkers(grayMat, corners, ids, rejectedCorners);
                Debug.Log("Detect end !");
                //isDetecting = ids.total() > 0;
                //Debug.Log("isdetecting: " + isDetecting);
                //if (isDetecting)
                //{
                //    if (rvecs.cols() < ids.total())
                //        rvecs.create(1, (int)ids.total(), CvType.CV_64FC3);
                //    if (tvecs.cols() < ids.total())
                //        tvecs.create(1, (int)ids.total(), CvType.CV_64FC3);

                //    using (MatOfPoint3f objPoints = new MatOfPoint3f(
                //        new Point3(-markerLength / 2f, markerLength / 2f, 0),
                //        new Point3(markerLength / 2f, markerLength / 2f, 0),
                //        new Point3(markerLength / 2f, -markerLength / 2f, 0),
                //        new Point3(-markerLength / 2f, -markerLength / 2f, 0)
                //    ))
                //    {
                //        for (int i = 0; i < ids.total(); i++)
                //        {
                //            using (Mat rvec = new Mat(3, 1, CvType.CV_64FC1))
                //            using (Mat tvec = new Mat(3, 1, CvType.CV_64FC1))
                //            using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                //            using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                //            {
                //                // Calculate pose for each marker
                //                Calib3d.solvePnP(objPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                //                rvec.reshape(3, 1).copyTo(new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)));
                //                tvec.reshape(3, 1).copyTo(new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)));

                //                // This example can display the ARObject on only first detected marker.
                //                if (i == 0)
                //                {
                //                    // Convert to unity pose data.
                //                    double[] rvecArr = new double[3];
                //                    rvec.get(0, 0, rvecArr);
                //                    double[] tvecArr = new double[3];
                //                    tvec.get(0, 0, tvecArr);
                //                    PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);
                //                    Debug.Log(poseData.pos.x + "," + poseData.pos.y + "," + poseData.pos.z);
                //                    Debug.Log(poseData.rot.x + "," + poseData.rot.y + "," + poseData.rot.z);

                //                    // Create transform matrix.  
                //                    transformationM = Matrix4x4.TRS(poseData.pos, poseData.rot, Vector3.one);
                //                    lock (sync)
                //                    {
                //                        // Right-handed coordinates system (OpenCV) to left-handed one (Unity)
                //                        // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
                //                        ARM = invertYM * transformationM * invertYM;

                //                        // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
                //                        ARM = ARM * invertYM * invertZM;

                //                        localToWorldMatrix = cameraToWorldMatrix * invertZM;
                //                        ARM = localToWorldMatrix * ARM;
                //                    }

                //                    hasUpdatedARTransformMatrix = true;
                //                    break;
                //                }
                //            }
                //        }
                //    }
                //}
            }
            catch (CvException e)
            {
                Debug.Log("Exception cv Frame: " + e.Message);
                Debug.Log(e.StackTrace);
            }
        }
        grayMat.Dispose();
        DebugUtils.TrackTick();
    }

    private void Update()
    {
        //if (hasUpdatedARTransformMatrix)
        //{
        //    hasUpdatedARTransformMatrix = false;

        //    lock (sync)
        //    {
        //        ultrasoundObj.SetMatrix4x4(ARM);
        //    }
        //}
    }
#endif

    private Mat CreateCameraMatrix(double fx, double fy, double cx, double cy)
    {
        Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
        camMatrix.put(0, 0, fx);
        camMatrix.put(0, 1, 0);
        camMatrix.put(0, 2, cx);
        camMatrix.put(1, 0, 0);
        camMatrix.put(1, 1, fy);
        camMatrix.put(1, 2, cy);
        camMatrix.put(2, 0, 0);
        camMatrix.put(2, 1, 0);
        camMatrix.put(2, 2, 1.0f);

        return camMatrix;
    }

    //UI diag update
    void LateUpdate()
    {
        DebugUtils.RenderTick();
        float renderDeltaTime = DebugUtils.GetRenderDeltaTime();
        float videoDeltaTime = DebugUtils.GetVideoDeltaTime();
        float trackDeltaTime = DebugUtils.GetTrackDeltaTime();

        if (renderFPS != null)
        {
            renderFPS.text = string.Format("Render: {0:0.0} ms ({1:0.} fps)", renderDeltaTime, 1000.0f / renderDeltaTime);
        }
        if (videoFPS != null)
        {
            videoFPS.text = string.Format("Video: {0:0.0} ms ({1:0.} fps)", videoDeltaTime, 1000.0f / videoDeltaTime);
        }
        if (trackFPS != null)
        {
            trackFPS.text = string.Format("Track:   {0:0.0} ms ({1:0.} fps)", trackDeltaTime, 1000.0f / trackDeltaTime);
        }
        if (debugStr != null)
        {
            if (DebugUtils.GetDebugStrLength() > 0)
            {
                if (debugStr.preferredHeight >= debugStr.rectTransform.rect.height)
                    debugStr.text = string.Empty;

                debugStr.text += DebugUtils.GetDebugStr();
                DebugUtils.ClearDebugStr();
            }
        }
    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
        webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
#endif
        webCamTextureToMatHelper.Dispose();
    }
}
