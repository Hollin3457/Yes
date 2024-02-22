using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NUHS.UltraSound.ImageReceiving
{
    public interface IUltrasoundImageReceiver : IDisposable
    {
        /// <summary>
        /// Start the image receiving process
        /// See: IsRunning() for status
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the image receiving process
        /// See: IsRunning() for status
        /// </summary>
        void Stop();

        /// <summary>
        /// Flag to check if the image receiving process is running
        /// </summary>
        /// <returns>True if image receiving process is running</returns>
        bool IsRunning();

        /// <summary>
        /// Get the latest Ultrasound image
        /// </summary>
        /// <returns>A Texture2D containing the ultrasound image</returns>
        Texture2D GetImage();

        /// <summary>
        /// Update the texture from "GetImage" to the latest received image, just that it does not return the texture
        /// </summary>
        void UpdateImage();
        
        /// <summary>
        /// Get the latest real image size
        /// </summary>
        /// <returns>Image size, in centimeter</returns>
        Vector2 GetImageSize();

        /// <summary>
        /// Get the current pixels per centimeter on the X and Y image axis
        /// </summary>
        /// <returns>Number of pixels per centimeter</returns>
        Vector2 GetPixelsPerCm();
    }
}
