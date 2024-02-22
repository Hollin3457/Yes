using UnityEngine;
using UnityEngine.UI;
using NUHS.UltraSound.Config;
using NUHS.UltraSound.Process;

namespace NUHS.UltraSound.QuickView
{
    public class QuickViewManager : ProcessManager
    {
        [Header("UI Dependency")]
        [SerializeField] private GameObject instrumentObj;
        [SerializeField] private GameObject smallUltrasoundController;
        [SerializeField] private GameObject bigUltrasoundController;
        [SerializeField] private GameObject zoomInButton;
        [SerializeField] private Image bigDisplayImage;
        [SerializeField] private Renderer onPatientDisplayRenderer;
        [SerializeField] private Transform onPatiendDisplayPivot;
        [SerializeField] private Transform markerTransform;

        private bool _doingQuickView;
        private AppManager _appManager;

        public override void StartProcess(AppManager appManager)
        {
            _appManager = appManager;
            _appManager.PromptIPAddress(StartQuickView);
        }
        
        private void StartQuickView()
        {
            _appManager.StartMarkerDetect();
            _appManager.StartImageReceiving();

            //set image content
            var texture2D = _appManager.GetImage();
            if (texture2D == null)
            {
                Debug.LogError("no valid image received");
                return;
            }
            onPatientDisplayRenderer.material.SetTexture("_MainTex", texture2D);

            SetScale();
            
            //set up UI
            OnZoomToBigPressed();

            // set bool
            _doingQuickView = true;
        }

        public override void StopProcess()
        {
            _appManager.StopImageReceiving();
            _appManager.StopMarkerDetect();
            _appManager = null;
            
            //disable UI
            OnZoomEnd();

            _doingQuickView = false;
        }

        private void SetBigDisplay()
        {
            //set image content
            var texture2D = _appManager.GetImage();
            if (texture2D == null)
            {
                Debug.LogError("no valid image received");
                return;
            }
            bigDisplayImage.sprite = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), Vector2.one * 0.5f);
        }

        private void SetScale()
        {
            //set the on patient image physical size
            var physicalScale = _appManager.GetImageSize();
            onPatiendDisplayPivot.localScale = new Vector3(physicalScale.x, physicalScale.y, 1);

            //set big display
            if(physicalScale.x < physicalScale.y)
            {
                float scale = physicalScale.x / physicalScale.y;
                bigDisplayImage.transform.localScale = new Vector3( scale, 1, 1);
            }
            else
            {
                float scale = physicalScale.y / physicalScale.x;
                bigDisplayImage.transform.localScale = new Vector3(1, scale, 1);
            }
        }

        public void OnZoomToBigPressed()
        {
            zoomInButton.SetActive(false);
            bigUltrasoundController.gameObject.SetActive(true);
            SetBigDisplay();
        }

        public void OnZoomToSmallPressed()
        {
            zoomInButton.SetActive(true);
            bigUltrasoundController.gameObject.SetActive(false);
        }

        private void OnZoomEnd()
        {
            smallUltrasoundController.gameObject.SetActive(false);
            bigUltrasoundController.gameObject.SetActive(false);
        }
        
        private void Update()
        {
            if (!_doingQuickView) return;
            if (_appManager == null && !_appManager.isInit) return;
            //the get image will do image.load to update th 
            _appManager.UpdateImage();

            if (smallUltrasoundController != null)
            {
                smallUltrasoundController.SetActive(_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId));
            }
            if (instrumentObj != null)
            {
                instrumentObj.SetActive(_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerInstrumentMarkerId));
            }
        }
    }
}