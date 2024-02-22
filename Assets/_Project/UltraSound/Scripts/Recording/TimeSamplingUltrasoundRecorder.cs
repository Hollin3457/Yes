using System.Collections.Generic;
using NUHS.Common.InternalDebug;
using UnityEngine;

namespace NUHS.UltraSound.Recording
{
    /// <summary>
    /// A simple recorder that will record based on a minimum time interval.
    /// </summary>
    public class TimeSamplingUltrasoundRecorder : IUltrasoundRecorder
    {
        private readonly int MAX_NUMBER_OF_RECORD = 1000;

        private int _samplesPerSecond;
        private float _sampleInterval;
        private float _lastFrameTime;
        private Stack<UltrasoundRecorderFrame> _frames;

        public IEnumerable<UltrasoundRecorderFrame> Frames { get { return _frames; } }
        public Transform Cuboid { get; private set; }
        public Vector3 LocalPosition { get; private set; }
        public Vector3 LocalRotation { get; private set; }

        public TimeSamplingUltrasoundRecorder(int samplesPerSecond = 5)
        {
            _samplesPerSecond = samplesPerSecond;
            _sampleInterval = (_samplesPerSecond == 0) ? 1 :  1.0f / _samplesPerSecond;
            _frames = new Stack<UltrasoundRecorderFrame>();
            _lastFrameTime = Time.time;
        }

        public void RecordFrame(Texture2D frame, Vector3 pos, Vector3 rot, Transform cuboid)
        {
            //ToDo: show some message when the maximum number of records reached
            if (_frames.Count > MAX_NUMBER_OF_RECORD)
            {
                return;
            }
            
            Cuboid = cuboid;

            // Skip frame if not yet due
            if (_lastFrameTime > 0 && _lastFrameTime + _sampleInterval > Time.time)
            {
                return;
            }
            
            _lastFrameTime = Time.time;

            // Convert image world position to position in cuboid's coord system, with the cuboid's bottom-left corner as origin.
            // InverseTransformPoint uses this matrix internally but here we want to exclude scale.
            var worldToLocalMatrix = Matrix4x4.TRS(cuboid.position, cuboid.rotation, Vector3.one).inverse;
            LocalPosition = worldToLocalMatrix.MultiplyPoint3x4(pos) + cuboid.localScale * 0.5f;

            // Convert image world rotation to rotation in cuboid space
            LocalRotation = (Quaternion.Inverse(cuboid.rotation) * Quaternion.Euler(rot)).eulerAngles;
            
            // Push the next frame onto the stack
            _frames.Push(new UltrasoundRecorderFrame()
            {
                Frame = frame.EncodeToJPG(),
                Position = LocalPosition,
                Rotation = LocalRotation
            });
            if (InternalDebug.IsDebugMode)
            {
                InternalDebug.Log(LogLevel.Debug,frame,"recording");
                InternalDebug.Log(LogLevel.Debug,$"Record frame at world pos {pos}, world rot {rot}, local pos {LocalPosition}, and local rot {LocalRotation}");
            }
        }

        public void ResetRecord()
        {
            _lastFrameTime = Time.time;
            _frames.Clear();
        }
    }
}
