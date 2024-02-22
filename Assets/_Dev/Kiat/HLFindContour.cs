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
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.UnityUtils;
using HoloLensCameraStream;
using NUHS.UltraSound.Tracking;
using Microsoft.MixedReality.Toolkit.Input;
using NUHS.UltraSound;
using NUHS.Common.InternalDebug;

namespace HoloLensWithOpenCVForUnityExample
{
    /// <summary>
    /// HoloLens ArUco Example
    /// An example of marker based AR using OpenCVForUnity on Hololens.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp.
    /// </summary>
    [RequireComponent(typeof(HLCameraStreamToMatHelper), typeof(ImageOptimizationHelper))]
    public class HLFindContour : MonoBehaviour
    {
        [HeaderAttribute("Preview")]

        /// <summary>
        /// The preview quad.
        /// </summary>
        public GameObject previewQuad;

        /// <summary>
        /// Determines if displays the camera preview.
        /// </summary>
        public bool displayCameraPreview;

        /// <summary>
        /// The toggle for switching the camera preview display state.
        /// </summary>
        public Toggle displayCameraPreviewToggle;


        [HeaderAttribute("Detection")]

        /// <summary>
        /// Determines if enables the detection.
        /// </summary>
        public bool enableDetection = true;

        /// <summary>
        /// Determines if restores the camera parameters when the file exists.
        /// </summary>
        public bool useStoredCameraParameters = false;

        /// <summary>
        /// The toggle for switching to use the stored camera parameters.
        /// </summary>
        public Toggle useStoredCameraParametersToggle;

        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool enableDownScale;

        /// <summary>
        /// The enable downscale toggle.
        /// </summary>
        public Toggle enableDownScaleToggle;


        [HeaderAttribute("AR")]

        /// <summary>
        /// Determines if applied the pose estimation.
        /// </summary>
        public bool applyEstimationPose = true;

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public int dictionaryId = Objdetect.DICT_6X6_250;

        /// <summary>
        /// The length of the markers' side. Normally, unit is meters.
        /// </summary>
        public float markerLength = 0.188f;

        /// <summary>
        /// The AR cube.
        /// </summary>
        public GameObject arCube;

        /// <summary>
        /// The AR game object.
        /// </summary>
        public ARGameObject arGameObject;

        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera arCamera;

        /// <summary>
        /// Track Image
        /// </summary>
        public Texture2D trackImage;

        [Space(10)]

        /// <summary>
        /// Determines if enable lerp filter.
        /// </summary>
        public bool enableLerpFilter;

        /// <summary>
        /// The enable lerp filter toggle.
        /// </summary>
        public Toggle enableLerpFilterToggle;

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

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        HLCameraStreamToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        Mat rgbMat4preview;
        Texture2D texture;

        // for CanonicalMarker.
        //Mat ids;
        //List<Mat> corners;
        //List<Mat> rejectedCorners;
        //Dictionary dictionary;
        //ArucoDetector arucoDetector;

        Mat rvecs;
        Mat tvecs;

        // for SIFT n RANSAC
        SIFT sift;
        MatOfKeyPoint keypointsOriginal;
        MatOfKeyPoint keypointsTransformed;
        Mat descriptorsOriginal;
        Mat descriptorsTransformed;
        BFMatcher matcher;

        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

        Mat downScaleMat;
        float DOWNSCALE_RATIO;

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

        bool _isDetecting = false;
        bool isDetecting
        {
            get
            {
                lock (sync)
                    return _isDetecting;
            }
            set
            {
                lock (sync)
                    _isDetecting = value;
            }
        }

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
            displayCameraPreviewToggle.isOn = displayCameraPreview;
            useStoredCameraParametersToggle.isOn = useStoredCameraParameters;
            enableDownScaleToggle.isOn = enableDownScale;
            enableLerpFilterToggle.isOn = enableLerpFilter;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
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

            if (enableDownScale)
            {
                downScaleMat = imageOptimizationHelper.GetDownScaleMat(grayMat);
                DOWNSCALE_RATIO = imageOptimizationHelper.downscaleRatio;
            }
            else
            {
                downScaleMat = grayMat;
                DOWNSCALE_RATIO = 1.0f;
            }

            float width = downScaleMat.width();
            float height = downScaleMat.height();

            texture = new Texture2D((int)width, (int)height, TextureFormat.RGB24, false);
            previewQuad.GetComponent<MeshRenderer>().material.mainTexture = texture;
            previewQuad.transform.localScale = new Vector3(0.2f * width / height, 0.2f, 1);
            previewQuad.SetActive(displayCameraPreview);


            //Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            DebugUtils.AddDebugStr(webCamTextureToMatHelper.outputColorFormat.ToString() + " " + webCamTextureToMatHelper.GetWidth() + " x " + webCamTextureToMatHelper.GetHeight() + " : " + webCamTextureToMatHelper.GetFPS());
            if (enableDownScale)
                DebugUtils.AddDebugStr("enableDownScale = true: " + DOWNSCALE_RATIO + " / " + width + " x " + height);


            // create camera matrix and dist coeffs.
            string loadDirectoryPath = Path.Combine(Application.persistentDataPath, "HoloLensArUcoCameraCalibrationExample");
            string calibratonDirectoryName = "camera_parameters" + rawFrameWidth + "x" + rawFrameWidth;
            string loadCalibratonFileDirectoryPath = Path.Combine(loadDirectoryPath, calibratonDirectoryName);
            string loadPath = Path.Combine(loadCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            if (useStoredCameraParameters && File.Exists(loadPath))
            {
                // If there is a camera parameters stored by HoloLensArUcoCameraCalibrationExample, use it

                CameraParameters param;
                XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
                using (var stream = new FileStream(loadPath, FileMode.Open))
                {
                    param = (CameraParameters)serializer.Deserialize(stream);
                }

                double fx = param.camera_matrix[0];
                double fy = param.camera_matrix[4];
                double cx = param.camera_matrix[2];
                double cy = param.camera_matrix[5];

                camMatrix = CreateCameraMatrix(fx, fy, cx / DOWNSCALE_RATIO, cy / DOWNSCALE_RATIO);
                distCoeffs = new MatOfDouble(param.GetDistortionCoefficients());

                Debug.Log("Loaded CameraParameters from a stored XML file.");
                Debug.Log("loadPath: " + loadPath);

                DebugUtils.AddDebugStr("Loaded CameraParameters from a stored XML file.");
                DebugUtils.AddDebugStr("loadPath: " + loadPath);
            }
            else
            {
                if (useStoredCameraParameters && !File.Exists(loadPath))
                {
                    DebugUtils.AddDebugStr("The CameraParameters XML file (" + loadPath + ") does not exist.");
                }

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

                CameraIntrinsics cameraIntrinsics = webCamTextureToMatHelper.GetCameraIntrinsics();

                camMatrix = CreateCameraMatrix(cameraIntrinsics.FocalLengthX, cameraIntrinsics.FocalLengthY, cameraIntrinsics.PrincipalPointX / DOWNSCALE_RATIO, cameraIntrinsics.PrincipalPointY / DOWNSCALE_RATIO);
                distCoeffs = new MatOfDouble(cameraIntrinsics.RadialDistK1, cameraIntrinsics.RadialDistK2, cameraIntrinsics.RadialDistK3, cameraIntrinsics.TangentialDistP1, cameraIntrinsics.TangentialDistP2);

                Debug.Log("Created CameraParameters from VideoMediaFrame.CameraIntrinsics on device.");

                DebugUtils.AddDebugStr("Created CameraParameters from VideoMediaFrame.CameraIntrinsics on device.");

#endif
            }

            Debug.Log("camMatrix " + camMatrix.dump());
            Debug.Log("distCoeffs " + distCoeffs.dump());

            //DebugUtils.AddDebugStr("camMatrix " + camMatrix.dump());
            //DebugUtils.AddDebugStr("distCoeffs " + distCoeffs.dump());


            //Calibration camera
            Size imageSize = new Size(width, height);
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
            arCamera.nearClipPlane = 0.01f;

            rvecs = new Mat(1, 10, CvType.CV_64FC3);
            tvecs = new Mat(1, 10, CvType.CV_64FC3);

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

            rgbMat4preview = new Mat();

            // Convert Texture2D to Mat
            Mat imgMat = new Mat(trackImage.height, trackImage.width, CvType.CV_8UC4); // Assuming the Texture2D has RGBA format
            Utils.texture2DToMat(trackImage, imgMat);

            // Check if the image was loaded successfully
            if (imgMat.empty())
            {
                Debug.LogError("Failed to load image");
                return;
            }

            // Do further processing with the loaded image
            // Convert the images to grayscale
            Mat grayOriginalMat = new Mat();
            Imgproc.cvtColor(imgMat, grayOriginalMat, Imgproc.COLOR_RGBA2GRAY);

            // Create SIFT detector and descriptor extractor
            sift = SIFT.create();

            // Detect and compute keypoints and descriptors
            keypointsOriginal = new MatOfKeyPoint();
            keypointsTransformed = new MatOfKeyPoint();
            descriptorsOriginal = new Mat();
            descriptorsTransformed = new Mat();
            sift.detectAndCompute(grayOriginalMat, new Mat(), keypointsOriginal, descriptorsOriginal);

            matcher = BFMatcher.create();
            Debug.Log("before image release");
            // Release the image resources
            imgMat.release();
            Debug.Log("after image release");
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

            if (sift != null)
                sift.Dispose();
            if (keypointsOriginal != null)
                keypointsOriginal.Dispose();
            if (descriptorsOriginal != null)
                descriptorsOriginal.Dispose();
            if (keypointsTransformed != null)
                keypointsTransformed.Dispose();
            if (descriptorsTransformed != null)
                descriptorsTransformed.Dispose();
            if (matcher != null)
                matcher.Dispose();
            if (rvecs != null)
                rvecs.Dispose();
            if (tvecs != null)
                tvecs.Dispose();

            if (rgbMat4preview != null)
                rgbMat4preview.Dispose();

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

            Mat downScaleMat = null;
            float DOWNSCALE_RATIO;
            if (enableDownScale)
            {
                downScaleMat = imageOptimizationHelper.GetDownScaleMat(grayMat);
                DOWNSCALE_RATIO = imageOptimizationHelper.downscaleRatio;
            }
            else
            {
                downScaleMat = grayMat;
                DOWNSCALE_RATIO = 1.0f;
            }

            Mat camMatrix = null;
            MatOfDouble distCoeffs = null;
            if (useStoredCameraParameters)
            {
                camMatrix = this.camMatrix;
                distCoeffs = this.distCoeffs;
            }
            else
            {
                camMatrix = CreateCameraMatrix(cameraIntrinsics.FocalLengthX, cameraIntrinsics.FocalLengthY, cameraIntrinsics.PrincipalPointX / DOWNSCALE_RATIO, cameraIntrinsics.PrincipalPointY / DOWNSCALE_RATIO);
                distCoeffs = new MatOfDouble(cameraIntrinsics.RadialDistK1, cameraIntrinsics.RadialDistK2, cameraIntrinsics.RadialDistK3, cameraIntrinsics.TangentialDistP1, cameraIntrinsics.TangentialDistP2);
            }

            if (enableDetection)
            {
                // Detect markers and estimate Pose
                Calib3d.undistort(downScaleMat, downScaleMat, camMatrix, distCoeffs);
                sift.detectAndCompute(downScaleMat, new Mat(), keypointsTransformed, descriptorsTransformed);

                // Perform feature matching
                MatOfDMatch matches = new MatOfDMatch();
                matcher.match(descriptorsOriginal, descriptorsTransformed, matches);
                if (!matches.empty())
                {
                    Debug.Log("Found match");

                    // Apply Lowe's ratio test to filter good matches
                    double ratioThreshold = 0.75;
                    MatOfDMatch goodMatches = new MatOfDMatch();
                    DMatch[] matchesArray = matches.toArray();
                    if(matchesArray.Length > 0)
                    {
                        for (int i = 0; i < matchesArray.Length - 1; i++)
                        {
                            if (matchesArray[i].distance < ratioThreshold * matchesArray[i + 1].distance)
                            {
                                goodMatches.push_back(new MatOfDMatch(matchesArray[i]));
                            }
                        }
                        // Extract keypoints from good matches
                        List<Point> goodPointsOriginalList = new List<Point>();
                        List<Point> goodPointsTransformedList = new List<Point>();
                        KeyPoint[] keypointsOriginalArray = keypointsOriginal.toArray();
                        KeyPoint[] keypointsTransformedArray = keypointsTransformed.toArray();
                        DMatch[] goodMatchesArray = goodMatches.toArray();
                        if (goodMatchesArray.Length > 0)
                        {
                            for (int i = 0; i < goodMatchesArray.Length; i++)
                            {
                                goodPointsOriginalList.Add(keypointsOriginalArray[goodMatchesArray[i].queryIdx].pt);
                                goodPointsTransformedList.Add(keypointsTransformedArray[goodMatchesArray[i].trainIdx].pt);
                            }
                            Debug.Log("Good point Found match");
                            MatOfPoint2f goodPointsOriginal = new MatOfPoint2f();
                            goodPointsOriginal.fromList(goodPointsOriginalList);
                            MatOfPoint2f goodPointsTransformed = new MatOfPoint2f();
                            goodPointsTransformed.fromList(goodPointsTransformedList);
                            Mat homography = Calib3d.findHomography(goodPointsOriginal, goodPointsTransformed, Calib3d.RANSAC, 5.0f, new Mat());

                            // Convert the homography matrix to a transformation matrix
                            Matrix4x4 transformationMatrix = new Matrix4x4();
                            //transformationMatrix.SetRow(0, new Vector4((float)homography.get(0, 0)[0], (float)homography.get(0, 1)[0], (float)homography.get(0, 2)[0], 0));
                            //transformationMatrix.SetRow(1, new Vector4((float)homography.get(1, 0)[0], (float)homography.get(1, 1)[0], (float)homography.get(1, 2)[0], 0));
                            //transformationMatrix.SetRow(2, new Vector4((float)homography.get(2, 0)[0], (float)homography.get(2, 1)[0], (float)homography.get(2, 2)[0], 0));
                            //transformationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));
                            //// Assuming you have the scaling factor used for downsampling
                            //float downsampleScale = 1.0f / DOWNSCALE_RATIO; // Example value, adjust as needed

                            //// Scale the transformation matrix back to the original size
                            //Matrix4x4 scaledTransformationMatrix = Matrix4x4.Scale(new Vector3(1.0f / downsampleScale, 1.0f / downsampleScale, 1.0f)) * transformationMatrix;

                            transformationMatrix = HomographyToMatrix4x4(homography);

                            lock (sync)
                            {
                                // Right-handed coordinates system (OpenCV) to left-handed one (Unity)
                                // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
                                //ARM = invertYM * scaledTransformationMatrix * invertYM;

                                // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
                                //ARM = ARM * invertYM * invertZM;
                                ARM = transformationMatrix;
                            }

                            hasUpdatedARTransformMatrix = true;
                        }
                    }
                }
            }

            if (!useStoredCameraParameters)
            {
                camMatrix.Dispose();
                distCoeffs.Dispose();
            }

            DebugUtils.TrackTick();

            Enqueue(() =>
            {
                if (!webCamTextureToMatHelper.IsPlaying()) return;

                if (displayCameraPreview && rgbMat4preview != null)
                {
                    Utils.matToTexture2D(rgbMat4preview, texture);
                    rgbMat4preview.Dispose();
                }

                if (applyEstimationPose)
                {
                    if (hasUpdatedARTransformMatrix)
                    {
                        hasUpdatedARTransformMatrix = false;

                        lock (sync)
                        {
                            Matrix4x4 localToWorldMatrix = cameraToWorldMatrix * invertZM;
                            ARM = localToWorldMatrix * ARM;

                            if (enableLerpFilter)
                            {
                                arGameObject.SetMatrix4x4(ARM);
                            }
                            else
                            {
                                ARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);
                            }
                        }
                    }
                }

                grayMat.Dispose();
            });

            isDetectingInFrameArrivedThread = false;
        }

        private void Update()
        {
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
            imageOptimizationHelper.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("HoloLensWithOpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
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
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the display camera preview toggle value changed event.
        /// </summary>
        public void OnDisplayCamreaPreviewToggleValueChanged()
        {
            displayCameraPreview = displayCameraPreviewToggle.isOn;

            previewQuad.SetActive(displayCameraPreview);
        }

        /// <summary>
        /// Raises the use stored camera parameters toggle value changed event.
        /// </summary>
        public void OnUseStoredCameraParametersToggleValueChanged()
        {
            useStoredCameraParameters = useStoredCameraParametersToggle.isOn;

            if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
            {
                webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the enable downscale toggle value changed event.
        /// </summary>
        public void OnEnableDownScaleToggleValueChanged()
        {
            enableDownScale = enableDownScaleToggle.isOn;

            if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
            {
                webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the enable lerp filter toggle value changed event.
        /// </summary>
        public void OnEnableLerpFilterToggleValueChanged()
        {
            enableLerpFilter = enableLerpFilterToggle.isOn;
        }

        Matrix4x4 HomographyToMatrix4x4(Mat homography)
        {
            Matrix4x4 matrix = Matrix4x4.identity;

            // Extract the transformation parameters from the homography matrix
            double[] hData = new double[9];
            homography.get(0, 0, hData);

            double h11 = hData[0];
            double h12 = hData[1];
            double h13 = hData[2];
            double h21 = hData[3];
            double h22 = hData[4];
            double h23 = hData[5];
            double h31 = hData[6];
            double h32 = hData[7];
            double h33 = hData[8];

            // Calculate the scaling factor
            double s = 1.0 / Math.Sqrt(h31 * h31 + h32 * h32 + h33 * h33);

            // Calculate the translation vector
            Vector3 translation = new Vector3((float)(h13 * s), (float)(h23 * s), (float)(h33 * s));

            // Calculate the rotation matrix
            Quaternion rotation = Quaternion.LookRotation(new Vector3((float)(h31 * s), (float)(h32 * s), (float)(h33 * s)),
                                                          new Vector3((float)(h21 * s), (float)(h22 * s), (float)(h23 * s)));

            // Assign the translation and rotation to the transformation matrix
            matrix.SetTRS(translation, rotation, Vector3.one);

            // Adjust for camera perspective
            Matrix4x4 cameraProjectionMatrix = arCamera.projectionMatrix;
            Matrix4x4 cameraToWorldMatrix = arCamera.cameraToWorldMatrix;
            matrix = cameraToWorldMatrix * cameraProjectionMatrix.inverse * matrix * cameraProjectionMatrix * cameraToWorldMatrix.inverse;

            return matrix;
        }
    }
}