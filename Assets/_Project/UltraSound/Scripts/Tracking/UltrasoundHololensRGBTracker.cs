using System;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using HoloLensCameraStream;
using NUHS.UltraSound.Config;
using NUHS.Common.InternalDebug;
using OpenCVForUnity.ImgcodecsModule;
using System.IO;

namespace NUHS.UltraSound.Tracking
{
    public class UltrasoundHololensRGBTracker : IMarkerTracker
    {
        public class ArucoMarker
        {
            public float ScaleFactor;
            public List<MatOfPoint3f> Mapping = new List<MatOfPoint3f>();
            public List<int> ExpectedIds = new List<int>();
            public Mat ExpectedIdsMat = new Mat();
        }
        private int _dictionaryId = Objdetect.DICT_4X4_50;

        // The webcam texture to mat helper.(To change researchmode in future)
        private HLCameraStreamToMatHelper _webCamTextureToMatHelper;

        private Mat _camMatrix;
        private MatOfDouble _distCoeffs;
        private ArucoMarker _probeMarker;
        private ArucoMarker _instrumentMarker;
        private MatOfPoint3f _probeObjPoints, _instrumentObjPoints;
        private MatOfPoint2f _probeImagePoints, _instrumentImagePoints;
        // Max iterations and epsilon for iterative optimization
        private int _maxIterations = 20;
        private double _epsilon = 1e-5;
        private Board _boardProbe;
        private Board _boardInstrument;

        private Mat _probeRvec, _probeTvec;
        private Mat _instrumentRvec, _instrumentTvec;

        //The matrix that inverts the Y-axis.
        private Matrix4x4 _invertYM;

        //The matrix that inverts the Z-axis.
        private Matrix4x4 _invertZM;

        //The transformation matrix.
        private Matrix4x4 _transformationM;
        private Matrix4x4 _localToWorldMatrix;
        //The matrix for AR Marker
        private Matrix4x4 _ARM;

        // for CanonicalMarker.
        private Mat _ids;
        private List<Mat> _corners;
        private List<Mat> _rejectedCorners;
        private Dictionary _dictionary;
        private ArucoDetector _arucoDetector;

        private bool _isDetecting = false;
        private bool _isRunning = false;
        public bool IsRunning() => _isRunning;
        private long _timeOutInMS;

        private object _markersSync = new object();
        private Dictionary<int, float[]> _markersDataDict = new Dictionary<int, float[]>();
        private Dictionary<int, long> _markersLastUpdateDict = new Dictionary<int, long>();

        public UltrasoundHololensRGBTracker(HLCameraStreamToMatHelper hLCameraStreamToMatHelper)
        {
            _timeOutInMS = UltraSoundAppConfigManager.Instance.AppConfig.SidecarTrackerTimeOutInMS;

            _markersDataDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId] = new float[7] { 0f, 0f, 0f, 0f, 0f, 0f, 1f };
            _markersDataDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId] = new float[7] { 0f, 0f, 0f, 0f, 0f, 0f, 1f };
            _markersLastUpdateDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _markersLastUpdateDict[UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId] = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            _maxIterations = UltraSoundAppConfigManager.Instance.AppConfig.SolvePnPIteration;
            _epsilon = UltraSoundAppConfigManager.Instance.AppConfig.ErrorThreshold;

            _webCamTextureToMatHelper = hLCameraStreamToMatHelper;
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            _webCamTextureToMatHelper.onInitialized.AddListener(OnWebCamTextureToMatHelperInitialized);
            _webCamTextureToMatHelper.onErrorOccurred.AddListener(OnWebCamTextureToMatHelperErrorOccurred);
            _webCamTextureToMatHelper.onDisposed.AddListener(OnWebCamTextureToMatHelperDisposed);
            _webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
#endif
            _webCamTextureToMatHelper.requestedWidth = UltraSoundAppConfigManager.Instance.AppConfig.InputWidth;
            _webCamTextureToMatHelper.requestedHeight = UltraSoundAppConfigManager.Instance.AppConfig.InputHeight;
            _webCamTextureToMatHelper.requestedFPS = UltraSoundAppConfigManager.Instance.AppConfig.InputFPS;
            _webCamTextureToMatHelper.outputColorFormat = WebCamTextureToMatHelper.ColorFormat.GRAY;
            _webCamTextureToMatHelper.Initialize();
        }

        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat grayMat = _webCamTextureToMatHelper.GetMat();

            float rawFrameWidth = grayMat.width();
            float rawFrameHeight = grayMat.height();
            DebugUtils.AddDebugStr(_webCamTextureToMatHelper.outputColorFormat.ToString() + " " + _webCamTextureToMatHelper.GetWidth() + " x " + _webCamTextureToMatHelper.GetHeight() + " : " + _webCamTextureToMatHelper.GetFPS());

#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API

            CameraIntrinsics cameraIntrinsics = _webCamTextureToMatHelper.GetCameraIntrinsics();

            _camMatrix = CreateCameraMatrix(cameraIntrinsics.FocalLengthX, cameraIntrinsics.FocalLengthY, cameraIntrinsics.PrincipalPointX, cameraIntrinsics.PrincipalPointY);
            _distCoeffs = new MatOfDouble(cameraIntrinsics.RadialDistK1, cameraIntrinsics.RadialDistK2, cameraIntrinsics.RadialDistK3, cameraIntrinsics.TangentialDistP1, cameraIntrinsics.TangentialDistP2);

            Debug.Log("Created CameraParameters from VideoMediaFrame.CameraIntrinsics on device.");
#else
            _camMatrix = CreateCameraMatrix(1000, 1000, 960, 540);
            _distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);
#endif
            Debug.Log("camMatrix " + _camMatrix.dump());
            Debug.Log("distCoeffs " + _distCoeffs.dump());

            //Calibration camera
            Size imageSize = new Size(rawFrameWidth, rawFrameHeight);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(_camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);

            _ids = new Mat();
            _corners = new List<Mat>();
            _rejectedCorners = new List<Mat>();
            _dictionary = Objdetect.getPredefinedDictionary(_dictionaryId);

            DetectorParameters _detectorParams = new DetectorParameters();
            _detectorParams.set_cornerRefinementWinSize(2);
            RefineParameters _refineParameters = new RefineParameters(1f, 0.5f, true);
            _arucoDetector = new ArucoDetector(_dictionary, _detectorParams, _refineParameters);

            _transformationM = new Matrix4x4();

            _invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
            _invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));

            _probeImagePoints = new MatOfPoint2f();
            _probeObjPoints = new MatOfPoint3f();
            _instrumentImagePoints = new MatOfPoint2f();
            _instrumentObjPoints = new MatOfPoint3f();

            _probeMarker = new ArucoMarker();
            LoadMarkerData(_probeMarker, UltraSoundAppConfigManager.Instance.AppConfig.ProbeIds, UltraSoundAppConfigManager.Instance.AppConfig.ProbeMapping);
            _instrumentMarker = new ArucoMarker();
            LoadMarkerData(_instrumentMarker, UltraSoundAppConfigManager.Instance.AppConfig.InstrumentIds, UltraSoundAppConfigManager.Instance.AppConfig.InstrumentMapping);
            _boardProbe = CreateBoard(_probeMarker.Mapping, _probeMarker.ExpectedIds);
            _boardInstrument = CreateBoard(_instrumentMarker.Mapping, _instrumentMarker.ExpectedIds);

            _probeRvec = new Mat(3, 1, CvType.CV_64FC1);
            _probeTvec = new Mat(3, 1, CvType.CV_64FC1);
            _instrumentRvec = new Mat(3, 1, CvType.CV_64FC1);
            _instrumentTvec = new Mat(3, 1, CvType.CV_64FC1);

            if (!_webCamTextureToMatHelper.IsPlaying())
            {
                Debug.Log("Play webcam");
                _webCamTextureToMatHelper.Play();
            }
        }

        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            _isDetecting = false;

            if (_arucoDetector != null)
                _arucoDetector.Dispose();
            if (_ids != null)
                _ids.Dispose();
            foreach (var item in _corners)
            {
                item.Dispose();
            }
            _corners.Clear();
            foreach (var item in _rejectedCorners)
            {
                item.Dispose();
            }
            _rejectedCorners.Clear();

            _camMatrix?.Dispose();
            _distCoeffs?.Dispose();
            _probeObjPoints?.Dispose();
            _instrumentObjPoints?.Dispose();
            _probeImagePoints?.Dispose();
            _instrumentImagePoints?.Dispose();
            _boardProbe?.Dispose();
            _boardInstrument?.Dispose();
            _probeRvec?.Dispose();
            _probeTvec?.Dispose();
            _instrumentRvec?.Dispose();
            _instrumentTvec?.Dispose();

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
            DebugUtils.VideoTick();
            if (_isRunning)
            {
                // Profiling to determine how long each request took
                var t0 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (InternalDebug.IsDebugMode)
                {
                    var grayJpg = new MatOfByte();
                    Imgcodecs.imencode(".jpg", grayMat, grayJpg);
                    InternalDebug.Log(LogLevel.Debug, grayJpg.toArray(), "arucoImage", ".jpg");
                    InternalDebug.Log(LogLevel.Debug, "camToWorldMatrix: " + cameraToWorldMatrix.ToString());
                }

                // Detect markers and estimate Pose
                _arucoDetector.detectMarkers(grayMat, _corners, _ids, _rejectedCorners);
                _isDetecting = _ids.total() > 0;

                if (_isDetecting)
                {
                    (_probeObjPoints, _probeImagePoints)= RefineDetection(grayMat, _probeMarker, _boardProbe, _corners, _ids, _rejectedCorners);
                    (_instrumentObjPoints, _instrumentImagePoints) = RefineDetection(grayMat, _instrumentMarker, _boardInstrument, _corners, _ids, _rejectedCorners);

                    if (_probeImagePoints.total() > 0)
                    {
                        SolvePointToPoint(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId, _probeObjPoints, _probeImagePoints, cameraToWorldMatrix, _probeRvec, _probeTvec);
                    }
                    if (_instrumentImagePoints.total() > 0)
                    {
                        SolvePointToPoint(UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId, _instrumentObjPoints, _instrumentImagePoints, cameraToWorldMatrix, _instrumentRvec, _instrumentTvec);
                    }
                }
                grayMat.Dispose();
                DebugUtils.TrackTick();
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

        public void Start()
        {
            if (IsRunning()) return;

            Debug.Log("Starting Marker Tracker...");

            _isRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning()) return;

            Debug.Log("Stopping Marker Tracker...");

            _isRunning = false;
            Debug.Log("Marker Tracker stopped.");
        }

        public Vector3 GetWorldPosition(int id)
        {
            lock (_markersSync)
            {
                if (_markersDataDict.TryGetValue(id, out float[] arr) && IsDetected(id))
                    return new Vector3(arr[0], arr[1], arr[2]);
            }
            return Vector3.zero;
        }

        public Quaternion GetWorldRotation(int id)
        {
            lock (_markersSync)
            {
                if (_markersDataDict.TryGetValue(id, out float[] arr) && IsDetected(id))
                    return new Quaternion(arr[3], arr[4], arr[5], arr[6]);
            }
            return Quaternion.identity;
        }

        public bool IsDetected(int id)
        {
            lock (_markersSync)
            {
                return DateTimeOffset.Now.ToUnixTimeMilliseconds() - _markersLastUpdateDict[id] < _timeOutInMS;
            }
        }

        private void LoadMarkerData(ArucoMarker arucoMarker, int[] ids, Vector3[] mappings)
        {
            List<int> idList = new List<int>();
            List<MatOfPoint3f> dataMatList = new List<MatOfPoint3f>();
            for (int i = 0; i < ids.Length; i++)
            {
                idList.Add(ids[i]);
                
                List<Point3> dataPoints = new List<Point3>();
                for (int j = i * 4; j < (i * 4) + 4; j++)
                {
                    Point3 point = new Point3(mappings[j].x, mappings[j].y, mappings[j].z);
                    dataPoints.Add(point);
                }
                MatOfPoint3f dataMat = new MatOfPoint3f(dataPoints.ToArray());
                dataMatList.Add(dataMat);
            }
            int[] idArray = idList.ToArray();
            Mat idMat = new Mat(1, idArray.Length, CvType.CV_32S);
            idMat.put(0, 0, idArray);
            arucoMarker.ExpectedIds = idList;
            arucoMarker.ExpectedIdsMat = idMat;
            arucoMarker.Mapping = dataMatList;
        }

        private Board CreateBoard(List<MatOfPoint3f> ptList, List<int> idList)
        {
            List<Mat> ptMatList = new List<Mat>();
            foreach (MatOfPoint3f matOfPoint3f in ptList)
            {
                ptMatList.Add(matOfPoint3f);
            }

            int[] idArray = idList.ToArray();
            Mat idMat = new Mat(1, idArray.Length, CvType.CV_32S);
            idMat.put(0, 0, idArray);

            return new Board(ptMatList, _dictionary, idMat);
        }

        private (MatOfPoint3f, MatOfPoint2f) RefineDetection(Mat img, ArucoMarker arucoMarker, Board board, List<Mat> corners, Mat ids, List<Mat> rejecteds)
        {
            List<Mat> filteredCornersList = new List<Mat>();
            List<int> filteredIdsList = new List<int>();

            int[] idArray = new int[ids.rows() * ids.cols()];
            ids.get(0, 0, idArray);
            for (int i = 0; i < arucoMarker.ExpectedIds.Count; i++)
            {
                bool exists = Array.Exists(idArray, element => element == arucoMarker.ExpectedIds[i]);
                if (exists)
                {
                    int index = Array.IndexOf(idArray, arucoMarker.ExpectedIds[i]);
                    filteredIdsList.Add(arucoMarker.ExpectedIds[i]);
                    filteredCornersList.Add(corners[index]);
                }
            }
            int[] filteredIdsArray = filteredIdsList.ToArray();
            Mat filteredIds = new Mat(1, filteredIdsArray.Length, CvType.CV_32S);
            filteredIds.put(0, 0, filteredIdsArray);

            _arucoDetector.refineDetectedMarkers(img, board, filteredCornersList, filteredIds, rejecteds, _camMatrix, _distCoeffs);
            List<Point3> objPointList = new List<Point3>();
            List<Point> imgPointList = new List<Point>();
            for (int i = 0; i < filteredIdsArray.Length; i++)
            {
                int id = arucoMarker.ExpectedIds.IndexOf(filteredIdsArray[i]);
                MatOfPoint2f imagePoints = new MatOfPoint2f(filteredCornersList[i].reshape(2, 4));
                imgPointList.AddRange(imagePoints.toList());
                objPointList.AddRange(arucoMarker.Mapping[id].toList());
            }
            MatOfPoint3f objPoints = new MatOfPoint3f(objPointList.ToArray());
            MatOfPoint2f imgPoints = new MatOfPoint2f(imgPointList.ToArray());
            return (objPoints, imgPoints);
        }

        private void SolvePointToPoint(int id, MatOfPoint3f objPoints, MatOfPoint2f imagePoints, Matrix4x4 cameraToWorldMatrix, Mat rvec, Mat tvec)
        {
            MatOfPoint2f reprojectedPoints = new MatOfPoint2f();
            // Iterate to refine pose estimate
            for (int i = 0; i < _maxIterations; i++)
            {
                // Calculate pose for each marker
                Calib3d.solvePnP(objPoints, imagePoints, _camMatrix, _distCoeffs, rvec, tvec);

                // Calculate reprojected points and check convergence
                Calib3d.projectPoints(objPoints, rvec, tvec, _camMatrix, _distCoeffs, reprojectedPoints);

                double error = Core.norm(imagePoints, reprojectedPoints, Core.NORM_L2);
                if (error < _epsilon)
                {
                    Debug.Log("Converged after " + (i + 1) + " iterations.");
                    break;
                }
            }
            reprojectedPoints.Dispose();

            // Convert to unity pose data.
            double[] rvecArr = new double[3];
            rvec.get(0, 0, rvecArr);
            double[] tvecArr = new double[3];
            tvec.get(0, 0, tvecArr);
            PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);
            // Create transform matrix.  
            _transformationM = Matrix4x4.TRS(poseData.pos, poseData.rot, Vector3.one);

            // Right-handed coordinates system (OpenCV) to left-handed one (Unity)
            // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
            _ARM = _invertYM * _transformationM * _invertYM;

            // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
            _ARM = _ARM * _invertYM * _invertZM;

            _localToWorldMatrix = cameraToWorldMatrix * _invertZM;
            _ARM = _localToWorldMatrix * _ARM;

            Vector3 pos = ARUtils.ExtractTranslationFromMatrix(ref _ARM);
            Quaternion rot = ARUtils.ExtractRotationFromMatrix(ref _ARM);

            lock (_markersSync)
            {
                _markersDataDict[id][0] = pos.x;
                _markersDataDict[id][1] = pos.y;
                _markersDataDict[id][2] = pos.z;
                _markersDataDict[id][3] = rot.x;
                _markersDataDict[id][4] = rot.y;
                _markersDataDict[id][5] = rot.z;
                _markersDataDict[id][6] = rot.w;
                _markersLastUpdateDict[id] = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // update time
            }
        }

        public void Dispose()
        {
            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
#if WINDOWS_UWP && !DISABLE_HOLOLENSCAMSTREAM_API
            _webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
#endif
            _webCamTextureToMatHelper.Dispose();
        }
    }
}

