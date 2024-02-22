using System.Collections.Generic;
using UnityEngine;

namespace NUHS.UltraSound.Recording
{
    public class UltrasoundRecorderFrame {
        public byte[] Frame { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
    }

    /// <summary>
    /// Interface for recording scans; can be used for reconstruction or other functions
    /// How to use:
    /// 1. During recording, on every Update(), call RecordFrame(receiver, tracker, cuboid);
    /// 2. At the end of the recording, get the frames from the Frames property.
    /// </summary>
    public interface IUltrasoundRecorder
    {
        IEnumerable<UltrasoundRecorderFrame> Frames { get; }
        Transform Cuboid { get; }
        Vector3 LocalPosition { get; }
        Vector3 LocalRotation { get; }

        /// <summary>
        /// Record the current frame from the receiver and tracker. The recorder will decide whether to use or discard the frame.
        /// This should be called on the Main Thread in Update() when recording is in progress.
        /// </summary>
        /// <param name="frame">Ultrasound image</param>
        /// <param name="tracker">Ultrasound position tracker</param>
        /// <param name="cuboid">Recording cuboid transform</param>
        void RecordFrame(Texture2D frame, Vector3 pos, Vector3 rot, Transform cuboid);
        
        /// <summary>
        /// Clear all the buffers that has already been captured
        /// </summary>
        void ResetRecord();
    }
}
