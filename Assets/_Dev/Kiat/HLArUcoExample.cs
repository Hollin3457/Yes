using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using NUHS.UltraSound.Tracking;
using HoloLensCameraStream;
using Microsoft.MixedReality.Toolkit.Input;
using NUHS.UltraSound;
using NUHS.Common.InternalDebug;
using OpenCVForUnity.ImgcodecsModule;

namespace HoloLensWithOpenCVForUnityExample
{
    /// <summary>
    /// HoloLens ArUco Example
    /// An example of marker based AR using OpenCVForUnity on Hololens.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp.
    /// </summary>
    [RequireComponent(typeof(HLCameraStreamToMatHelper))]
    public class HLArUcoExample : MarkerTrackerBase
    {
        [HeaderAttribute("Detection")]

        /// <summary>
        /// Determines if enables the detection.
        /// </summary>
        public bool enableDetection = false;

        [HeaderAttribute("AR")]

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        private int dictionaryId = Objdetect.DICT_4X4_50;

        /// <summary>
        /// The length of the markers' side. Normally, unit is meters.
        /// </summary>
        public float markerLength = 0.03f;

        public ARGameObject ultrasoundObj;

        public Transform ultrasoundImage;
        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera arCamera;


        [Space(10)]

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The distCoeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The matrix that inverts the Y-axis.
        /// </summary>
        Matrix4x4 invertYM;

        /// <summary>
        /// The matrix that inverts the Z-axis.
        /// </summary>
        Matrix4x4 invertZM;

        /// <summary>
        /// The transformation matrix.
        /// </summary>
        Matrix4x4 transformationM;

        Matrix4x4 localToWorldMatrix;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        HLCameraStreamToMatHelper webCamTextureToMatHelper;

        // for CanonicalMarker.
        Mat ids;
        List<Mat> corners;
        List<Mat> rejectedCorners;
        Dictionary dictionary;
        ArucoDetector arucoDetector;

        Mat rvecs;
        Mat tvecs;
        private Mat _mat;
        bool isUpdated = false;

        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

        bool _isThreadRunning = false;
        bool isThreadRunning
        {
            get
            {
                lock (sync)
                    return _isThreadRunning;
            }
            set
            {
                lock (sync)
                    _isThreadRunning = value;
            }
        }

        public bool isDetecting = false;

        bool _hasUpdatedARTransformMatrix = false;
        bool hasUpdatedARTransformMatrix
        {
            get
            {
                lock (sync)
                    return _hasUpdatedARTransformMatrix;
            }
            set
            {
                lock (sync)
                    _hasUpdatedARTransformMatrix = value;
            }
        }

        bool _isDetectingInFrameArrivedThread = false;
        bool isDetectingInFrameArrivedThread
        {
            get
            {
                lock (sync)
                    return _isDetectingInFrameArrivedThread;
            }
            set
            {
                lock (sync)
                    _isDetectingInFrameArrivedThread = value;
            }
        }

        [HeaderAttribute("Debug")]

        public Text renderFPS;
        public Text videoFPS;
        public Text trackFPS;
        public Text debugStr;


        // Use this for initialization
        protected void Start()
        {
            webCamTextureToMatHelper = gameObject.GetComponent<HLCameraStreamToMatHelper>();
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
#endif
            webCamTextureToMatHelper.outputColorFormat = WebCamTextureToMatHelper.ColorFormat.GRAY;
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat grayMat = webCamTextureToMatHelper.GetMat();

            float rawFrameWidth = grayMat.width();
            float rawFrameHeight = grayMat.height();

            DebugUtils.AddDebugStr(webCamTextureToMatHelper.outputColorFormat.ToString() + " " + webCamTextureToMatHelper.GetWidth() + " x " + webCamTextureToMatHelper.GetHeight() + " : " + webCamTextureToMatHelper.GetFPS());
    
            // create camera matrix and dist coeffs.
            string loadDirectoryPath = Path.Combine(Application.persistentDataPath, "HoloLensArUcoCameraCalibrationExample");
            string calibratonDirectoryName = "camera_parameters" + rawFrameWidth + "x" + rawFrameWidth;
            string loadCalibratonFileDirectoryPath = Path.Combine(loadDirectoryPath, calibratonDirectoryName);
            string loadPath = Path.Combine(loadCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");

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

            // Display objects near the camera.
            arCamera.nearClipPlane = 0.1f;

            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();
            rvecs = new Mat(1, 10, CvType.CV_64FC3);
            tvecs = new Mat(1, 10, CvType.CV_64FC3);
            _mat = new Mat(360, 640, CvType.CV_8UC4);
            dictionary = Objdetect.getPredefinedDictionary(dictionaryId);

            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_useAruco3Detection(true);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);

            transformationM = new Matrix4x4();

            invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
            Debug.Log("invertYM " + invertYM.ToString());

            invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
            Debug.Log("invertZM " + invertZM.ToString());

            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection of AR markers.
            if (webCamTextureToMatHelper.IsFrontFacing() && !webCamTextureToMatHelper.flipHorizontal)
            {
                webCamTextureToMatHelper.flipHorizontal = true;
            }
            else if (!webCamTextureToMatHelper.IsFrontFacing() && webCamTextureToMatHelper.flipHorizontal)
            {
                webCamTextureToMatHelper.flipHorizontal = false;
            }

            if(!webCamTextureToMatHelper.IsPlaying())
                webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

            while (isDetectingInFrameArrivedThread)
            {
                //Wait detecting stop
            }

            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Clear();
            }
#endif

            hasUpdatedARTransformMatrix = false;

            if (arucoDetector != null)
                arucoDetector.Dispose();

            if (ids != null)
                ids.Dispose();
            foreach (var item in corners)
            {
                item.Dispose();
            }
            corners.Clear();
            foreach (var item in rejectedCorners)
            {
                item.Dispose();
            }
            rejectedCorners.Clear();
            if (rvecs != null)
                rvecs.Dispose();
            if (tvecs != null)
                tvecs.Dispose();

            if (debugStr != null)
            {
                debugStr.text = string.Empty;
            }
            DebugUtils.ClearDebugStr();
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
            isDetectingInFrameArrivedThread = true;

            DebugUtils.VideoTick();
            lock (sync)
            {
                Imgproc.cvtColor(grayMat, _mat, Imgproc.COLOR_BGRA2RGBA);
                isUpdated = true;
            }
           
            if (enableDetection)
            {
                // Detect markers and estimate Pose
                Calib3d.undistort(grayMat, grayMat, camMatrix, distCoeffs);
                arucoDetector.detectMarkers(grayMat, corners, ids, rejectedCorners);

                isDetecting = ids.total() > 0;
                Debug.Log("isdetecting: " + isDetecting);
                if (isDetecting)
                {
                    if (rvecs.cols() < ids.total())
                        rvecs.create(1, (int)ids.total(), CvType.CV_64FC3);
                    if (tvecs.cols() < ids.total())
                        tvecs.create(1, (int)ids.total(), CvType.CV_64FC3);

                    using (MatOfPoint3f objPoints = new MatOfPoint3f(
                        new Point3(-markerLength / 2f, markerLength / 2f, 0),
                        new Point3(markerLength / 2f, markerLength / 2f, 0),
                        new Point3(markerLength / 2f, -markerLength / 2f, 0),
                        new Point3(-markerLength / 2f, -markerLength / 2f, 0)
                    ))
                    {
                        for (int i = 0; i < ids.total(); i++)
                        {
                            using (Mat rvec = new Mat(3, 1, CvType.CV_64FC1))
                            using (Mat tvec = new Mat(3, 1, CvType.CV_64FC1))
                            using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                            using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                            {
                                // Calculate pose for each marker
                                Calib3d.solvePnP(objPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                                rvec.reshape(3, 1).copyTo(new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)));
                                tvec.reshape(3, 1).copyTo(new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)));

                                // This example can display the ARObject on only first detected marker.
                                if (i == 0)
                                {
                                    // Convert to unity pose data.
                                    double[] rvecArr = new double[3];
                                    rvec.get(0, 0, rvecArr);
                                    double[] tvecArr = new double[3];
                                    tvec.get(0, 0, tvecArr);
                                    PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

                                    // Create transform matrix.  
                                    transformationM = Matrix4x4.TRS(poseData.pos, poseData.rot, Vector3.one);
                                    lock (sync)
                                    {
                                        // Right-handed coordinates system (OpenCV) to left-handed one (Unity)
                                        // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
                                        ARM = invertYM * transformationM * invertYM;

                                        // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
                                        ARM = ARM * invertYM * invertZM;
                                    }

                                    hasUpdatedARTransformMatrix = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            DebugUtils.TrackTick();

            Enqueue(() =>
            {
                if (!webCamTextureToMatHelper.IsPlaying()) return;

                if (hasUpdatedARTransformMatrix)
                {
                    hasUpdatedARTransformMatrix = false;

                    lock (sync)
                    {
                        localToWorldMatrix = cameraToWorldMatrix * invertZM;
                        ARM = localToWorldMatrix * ARM;

                        ultrasoundObj.SetMatrix4x4(ARM);
                    }
                }

                grayMat.Dispose();
            });

            isDetectingInFrameArrivedThread = false;
        }

        private void Update()
        {
            if (isUpdated)
            {
                lock (sync)
                {
                    Imgcodecs.imwrite(Application.persistentDataPath + "/test/jpg_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ".jpg", _mat);
                    isUpdated = false;
                }
            }
            lock (ExecuteOnMainThread)
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }
        }

        private void Enqueue(Action action)
        {
            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Enqueue(action);
            }
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


        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            //webCamTextureToMatHelper.Play();
            enableDetection = true;
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            //webCamTextureToMatHelper.Stop();
            enableDetection = false;
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }

        
        [SerializeField] private MarkerConfig markerConfig;
        public override Vector3 GetImagePos()
        {
            return ultrasoundImage.position;
        }

        public override Vector3 GetImageRot()
        {
            return ultrasoundImage.eulerAngles;
        }

        public override Vector3 GetMarkerPos()
        {
            return ultrasoundObj.gameObject.transform.position;
        }

        public override Quaternion GetMarkerRot()
        {
            return ultrasoundObj.gameObject.transform.rotation;
        }

        public override bool IsMarkerDetected()
        {
            return isDetecting;
        }

        public override void StartMarkerDetect()
        {
            markerConfig.Load();
            OnPlayButtonClick();
        }

        public override void StopMarkerDetect()
        {
            OnStopButtonClick();
        }

        public override void SetOffset(Vector3 pos, Vector3 rot)
        {
            markerConfig.posOffset = pos;
            markerConfig.rotationOffset = rot;
            markerConfig.Save();

            ultrasoundImage.localPosition = pos;
            ultrasoundImage.localEulerAngles = rot;
        }
    }
}