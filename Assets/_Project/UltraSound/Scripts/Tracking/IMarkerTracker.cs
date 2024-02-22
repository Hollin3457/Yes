using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NUHS.UltraSound.Tracking
{
    public interface IMarkerTracker : IDisposable
    {
        /// <summary>
        /// Get marker position in world coordinate
        /// </summary>
        /// <param name="id">Marker Id</param>
        /// <returns></returns>
        Vector3 GetWorldPosition(int id);

        /// <summary>
        /// Get marker rotation in world coordinate
        /// </summary>
        /// <param name="id">Marker Id</param>
        /// <returns></returns>
        Quaternion GetWorldRotation(int id);

        /// <summary>
        /// Check if a marker is detected
        /// </summary>
        /// <returns></returns>
        bool IsDetected(int id);

        /// <summary>
        /// Start the marker tracking process
        /// See: IsRunning() for status
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the marker tracking process
        /// See: IsRunning() for status
        /// </summary>
        void Stop();

        /// <summary>
        /// Is the marker tracking process running
        /// </summary>
        /// <returns>True if tracker is running</returns>
        bool IsRunning();
    }

}
