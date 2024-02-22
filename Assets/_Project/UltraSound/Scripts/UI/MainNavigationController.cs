using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;
using NUHS.Common.UI;

namespace NUHS.UltraSound.UI
{
    public class MainNavigationController : MonoBehaviour
    {
        [Header("Sending Events")]
        [SerializeField] private SimpleEvent onRecordScanButtonPressed;
        [SerializeField] private SimpleEvent onQuickViewButtonPressed;
        [SerializeField] private SimpleEvent onFindFileButtonPressed;
        [SerializeField] private SimpleEvent onRecalibrateButtonPressed;
        [SerializeField] private BoolEvent onDebugModeButtonPressed;
        
        [Header("Receiving Events")]
        [SerializeField] private BoolEvent onProcessBusy;
        [SerializeField] private SimpleEvent ultrasoundProfileLoaded;

        [Header("UI Dependency")] 
        [SerializeField] private GameObject loadingLogo;
        [SerializeField] private GameObject staticLogo;
        [SerializeField] private GameObject buttonGroup;
        [SerializeField] private PressableButton cubeButton;
        [SerializeField] private PressableButton recordScanButton;
        [SerializeField] private PressableButton quickViewButton;
        [SerializeField] private PressableButton findFileButton;
        [SerializeField] private PressableButton recalibrateButton;
        [SerializeField] private PressableButton debugModeButton;
        [SerializeField] private SolverHandler solverHandler;
        private CommonButton debugModeCommonButton;

        private bool inDebugMode;
        private bool isUltrasoundProfileLoaded;
        private Vector3 grabStartPosition;

        private void Awake()
        {
            recordScanButton.ButtonReleased.AddListener(()=>onRecordScanButtonPressed.Send());
            quickViewButton.ButtonReleased.AddListener(()=>onQuickViewButtonPressed.Send());
            findFileButton.ButtonReleased.AddListener(()=>onFindFileButtonPressed.Send());
            recalibrateButton.ButtonReleased.AddListener(()=>onRecalibrateButtonPressed.Send());
            debugModeButton.ButtonReleased.AddListener(SetDebugMode);
            debugModeCommonButton = debugModeButton.GetComponent<CommonButton>();
            solverHandler = GetComponent<SolverHandler>();

            cubeButton.ButtonReleased.AddListener(ToggleHandMenu);

            OnProcessBusy(false);
        }

        private void OnEnable()
        {
            onProcessBusy.Register(OnProcessBusy);
            ultrasoundProfileLoaded.Register(EnableMainMenu);
        }

        private void OnDisable()
        {
            onProcessBusy.Unregister(OnProcessBusy);
            ultrasoundProfileLoaded.Unregister(EnableMainMenu);
        }

        public void OnGrabStart()
        {
            solverHandler.UpdateSolvers = false;
            grabStartPosition = Camera.main.transform.InverseTransformPoint(transform.position);
        }

        public void OnGrabEnd()
        {
            var diff = Camera.main.transform.InverseTransformPoint(transform.position) - grabStartPosition;
            var offset = solverHandler.AdditionalOffset;
            offset += diff;
            offset.z = 0f;
            solverHandler.AdditionalOffset = offset;
            solverHandler.UpdateSolvers = true;
        }

        private void ToggleHandMenu()
        {
            // Only allow the main menu to be opened when processes are not
            // busy and after the ultrasound profile is loaded.
            if (loadingLogo.activeSelf || !isUltrasoundProfileLoaded)
            {
                return;
            }

            buttonGroup.SetActive(!buttonGroup.activeSelf);
        }

        private void EnableMainMenu()
        {
            isUltrasoundProfileLoaded = true;
        }

        private void OnProcessBusy(bool isBusy)
        {
            buttonGroup.SetActive(false);
            staticLogo.SetActive(!isBusy);
            loadingLogo.SetActive(isBusy);
        }

        private void SetDebugMode()
        {
            inDebugMode = !inDebugMode;
            onDebugModeButtonPressed.Send(inDebugMode);
            debugModeCommonButton.SetEmphasis(inDebugMode);
        }
    }
}