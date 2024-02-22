using System;
using UnityEngine;

namespace NUHS.UltraSound.ImageReceiving
{
    /// <summary>
    /// A dummy Image Receiver which returns a blank image and fixed size
    /// </summary>
    public class MockUltrasoundImageReceiver : IUltrasoundImageReceiver
    {
        private Texture2D _image;
        private Vector2 _size;
        private Vector2 _ppcm;
        private bool _isConnected = false;
        private bool _isRunning = false;

        public Texture2D GetImage() => _image;
        public void UpdateImage() { }

        public Vector2 GetImageSize() => _size;
        public Vector2 GetPixelsPerCm() => _ppcm;
        public bool IsRunning() => _isRunning;

        public MockUltrasoundImageReceiver() : this(300, 400, 10, 10) { }
        public MockUltrasoundImageReceiver(int width, int height, int ppcmX, int ppcmY)
        {
            _image = new Texture2D(width, height);
            _ppcm = new Vector2(ppcmX, ppcmY);
            _size = new Vector2((width / ppcmX) * 0.01f, (height / ppcmY) * 0.01f);
        }

        public void Start()
        {
            if (_isRunning) throw new InvalidOperationException("Can not start: Receiver is already running.");
            _isRunning = true;
        }

        public void Stop()
        {
            if (!_isRunning) throw new InvalidOperationException("Can not stop: Receiver is not running.");
            _isRunning = false;
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
