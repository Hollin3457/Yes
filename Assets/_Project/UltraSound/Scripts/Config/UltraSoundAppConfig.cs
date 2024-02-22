using UnityEngine;

namespace NUHS.UltraSound.Config
{
    /// <summary>
    /// Global Application Configuration
    /// Read this from AppConfigManager.Instance.AppConfig
    /// </summary>
    public class UltraSoundAppConfig
    {
        public string UltraSoundProfileLoaderType = "backend";
        public string InstrumentProfileLoaderType = "mock";
        public string TrackerType = "sidecar";
        public string RecorderType = "timesampling";
        public string ReconstructorType = "backend";

        public string BackendAddress = "10.65.33.83";
        public string BackendPort = "50000";
        public string BackendFullAddress { get { return $"{BackendAddress}:{BackendPort}"; } }

        public string SidecarAddress = "10.65.33.154";
        public string SidecarPort = "51000";
        public string SidecarFullAddress { get { return $"{SidecarAddress}:{SidecarPort}"; } }

        // Default UltraSound Device Profile
        // TODO: This will be removed once there's a UI to select the U/S Probe
        public int UltraSoundProfileLoaderDefaultIndex = 1;

        // Marker Tracker IDs to use
        public int TrackerProbeMarkerId = 7;
        public Vector3 TrackerProbeMarkerPosition = Vector3.zero;
        public Vector3 TrackerProbeMarkerRotation = Vector3.zero;
        public int TrackerInstrumentMarkerId = 2;
        public Vector3 TrackerInstrumentMarkerPosition = Vector3.zero;
        public Vector3 TrackerInstrumentMarkerRotation = Vector3.zero;

        // Marker Tracker Settings
        public int SidecarTrackerLoopDurationInMS = 20;
        public int SidecarTrackerTimeOutInMS = 300;
        public int SidecarTrackerMaxSyncDiffInMS = 10;

        // Time Sampling Recorder settings
        public int TimeSamplingRecorderSamplesPerSecond = 5;

        // Backend Reconstructor Settings
        public int BackendReconstructionPollIntervalMS = 2000;
        public int BackendReconstructionMethod = 0;

        // aruco tracker config settings
        public int InputWidth = 500;
        public int InputHeight = 282;
        public int InputFPS = 15;
        public int SolvePnPIteration = 20;
        public float ErrorThreshold = 0.0001f;

        public int[] ProbeIds = new int[] { 0, 4, 47, 14, 20 };
        // every 4 probe lines corresponding to 1 probe id
        public Vector3[] ProbeMapping = new Vector3[]
        {
            new Vector3(0, -0.01f, 0),
            new Vector3(0.01f, 0.01f, 0),
            new Vector3(0.01f, -0.01f, 0),
            new Vector3(-0.01f, -0.01f, 0),
            new Vector3(-0.04f, 0.01f, 0),
            new Vector3(-0.02f, 0.01f, 0),
            new Vector3(-0.02f, -0.01f, 0),
            new Vector3(-0.04f, -0.01f, 0),
            new Vector3(0.02f, 0.01f, 0),
            new Vector3(0.04f, 0.01f, 0),
            new Vector3(0.04f, -0.01f, 0),
            new Vector3(0.02f, -0.01f, 0),
            new Vector3(-0.01f, 0.04f, 0),
            new Vector3(0.01f, 0.04f, 0),
            new Vector3(0.01f, 0.02f, 0),
            new Vector3(-0.01f, 0.02f, 0),
            new Vector3(-0.01f, -0.02f, 0),
            new Vector3(0.01f, -0.02f, 0),
            new Vector3(0.01f, -0.04f, 0),
            new Vector3(-0.01f, -0.04f, 0)
        };
        public int[] InstrumentIds = new int[] { 7, 6, 8, 9, 10 };
        public Vector3[] InstrumentMapping = new Vector3[]
        {
            new Vector3(-0.01f, 0.01f, 0),
            new Vector3(0.01f, 0.01f, 0),
            new Vector3(0.01f, -0.01f, 0),
            new Vector3(-0.01f, -0.01f, 0),
            new Vector3(-0.04f, 0.01f, 0),
            new Vector3(-0.02f, 0.01f, 0),
            new Vector3(-0.02f, -0.01f, 0),
            new Vector3(-0.04f, -0.01f, 0),
            new Vector3(0.02f, 0.01f, 0),
            new Vector3(0.04f, 0.01f, 0),
            new Vector3(0.04f, -0.01f, 0),
            new Vector3(0.02f, -0.01f, 0),
            new Vector3(-0.01f, 0.04f, 0),
            new Vector3(0.01f, 0.04f, 0),
            new Vector3(0.01f, 0.02f, 0),
            new Vector3(-0.01f, 0.02f, 0),
            new Vector3(-0.01f, -0.02f, 0),
            new Vector3(0.01f, -0.02f, 0),
            new Vector3(0.01f, -0.04f, 0),
            new Vector3(-0.01f, -0.04f, 0)
        };

        //calibration threshold
        public float positionDiff = 0.03f; //set to 3cm
        public float angleDiff = 10f; //set to 10degree
    }
}
