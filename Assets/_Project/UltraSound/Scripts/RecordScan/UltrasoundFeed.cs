using NUHS.UltraSound.Process;
using UnityEngine;

namespace NUHS.UltraSound
{
    public class UltrasoundFeed : MonoBehaviour
    {
        [SerializeField] private Renderer meshRenderer;

        private AppManager _appManager;
        private float _defaultWidth;

        private void Awake()
        {
            _defaultWidth = transform.localScale.x;
            gameObject.SetActive(false);
            meshRenderer.enabled = true;
        }

        public void StartFeed(AppManager appManager)
        {
            gameObject.SetActive(true);
            _appManager = appManager;

            var texture2D = _appManager.GetImage();
            if (texture2D == null)
            {
                Debug.LogError("no valid image received");
                return;
            }
            meshRenderer.material.SetTexture("_MainTex", texture2D);

            // Keep height the same (only scale the width).
            var imageSize = _appManager.GetImageSize();
            var aspectRatio = imageSize.x / imageSize.y;
            var scale = meshRenderer.transform.localScale;
            scale.x = _defaultWidth * aspectRatio;
            meshRenderer.transform.localScale = scale;
        }

        private void Update()
        {
            if (_appManager == null)
            {
                return;
            }

            _appManager.UpdateImage();
        }
    }
}
