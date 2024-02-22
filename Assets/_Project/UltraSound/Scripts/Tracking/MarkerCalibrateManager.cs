using System.Collections;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using NUHS.Common.UI;
using NUHS.UltraSound.Process;
using NUHS.UltraSound.UI;
using NUHS.UltraSound.Config;

namespace NUHS.UltraSound.Tracking
{
    public enum ButtonState { IDLE = 0, CONFIRM, RESTART }
    public class MarkerCalibrateManager : ProcessManager
    {
        [Header("Offsets")]
        [SerializeField] private Vector3 backplateOffset;
        [SerializeField] private Vector3 placementOffset;
        [SerializeField] private Vector3 uiOffset;
        [Header("WarningPrompt")]
        [SerializeField] private TransactionalPrompt warningPrompt;
        [Header("Detection")]
        [SerializeField] private float timeOutDuration;
        [SerializeField] private GameObject backPlate;
        [SerializeField] private GameObject placementObj;
        [SerializeField] private GameObject uiGuide;
        [Header("Stabilization")]
        [SerializeField] private GameObject guideText;
        [SerializeField] private float angleLimit;
        [SerializeField] private float distLimit;
        [Header("Count Down")]
        [SerializeField] private float countDownDuration;
        [SerializeField] private ProgressBar progressBar;
        [Header("Confirmation")]
        [SerializeField] private Renderer guidePlaneRenderer;
        [SerializeField] private Color calibGrey;
        [SerializeField] private Color calibGreen;
        [SerializeField] private PressableButton confirmButton;
        [SerializeField] private PressableButton restartButton;

        private Vector3 _prevMarkerPos;
        private Quaternion _prevMarkerRot;
        private ButtonState _confirmValue;
        private Coroutine _coroutine;
        private bool _isRunning = false;
        private AppManager _appManager;

        public enum State { Idle, Warning, MarkerDetection, Stabilization, CountDown, Confirmation, Confirm, Restart, TimeOut }
        public State CurrentCalibrateState;
        // Start is called before the first frame update
        private void OnEnable()
        {
            SwitchState(State.Idle);
        }

        private void Start()
        {
            warningPrompt.SetupButtonAction(OnWarningConfirm, OnWarningCancel);
        }

        public void OnWarningConfirm()
        {
            _appManager.PromptIPAddress(StartCalibrate, true);
        }

        public void OnWarningCancel()
        {
            CancelProcess();
        }

        private void CancelProcess()
        {
            SwitchState(State.Idle);
            _appManager.TransitToHome();
        }

        public override void StartProcess(AppManager appManager)
        {
            Debug.Log("Start Calibrate Process");
            if (_isRunning) return;
            _isRunning = true;

            _appManager = appManager;
            SwitchState(State.Warning);
        }

        public override void StopProcess()
        {
            if (!_isRunning) return;
            SwitchState(State.Idle);
            EndCalibrate();
            Debug.Log("End Calibrate Process");
        }

        private void StartCalibrate()
        {
            _appManager.StartMarkerDetect();

            var newVec3 = Camera.main.transform.position + Camera.main.transform.forward * backplateOffset.z;
            newVec3.y += backplateOffset.y;
            backPlate.transform.position = newVec3;
            newVec3 = Camera.main.transform.position + Camera.main.transform.forward * placementOffset.z;
            newVec3.y += placementOffset.y;
            placementObj.transform.position = newVec3;
            var lookPos = Camera.main.transform.position;
            lookPos.y += placementOffset.y;
            lookPos = newVec3 - lookPos;
            placementObj.transform.rotation = Quaternion.LookRotation(lookPos, Vector3.up);
            newVec3 = Camera.main.transform.position + Camera.main.transform.forward * uiOffset.z;
            newVec3.y += uiOffset.y;
            uiGuide.transform.position = newVec3;

            distLimit = UltraSoundAppConfigManager.Instance.AppConfig.positionDiff;
            angleLimit = UltraSoundAppConfigManager.Instance.AppConfig.angleDiff;

            SwitchState(State.MarkerDetection);
            Debug.Log("Enter Marker Detection mode for Calibration!");
        }

        private void EndCalibrate()
        {
            _isRunning = false;
            _appManager.StopMarkerDetect();
            _appManager = null;
        }

        private bool IsMarkerStable()
        {
            float positionDiff = Vector3.Distance(_appManager.GetMarkerPos(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId), _prevMarkerPos);
            float rotationDiff = Quaternion.Angle(_appManager.GetMarkerRot(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId), _prevMarkerRot);
            bool result = Mathf.Abs(positionDiff) < distLimit && Mathf.Abs(rotationDiff) < angleLimit;
            _prevMarkerPos = _appManager.GetMarkerPos(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId);
            _prevMarkerRot = _appManager.GetMarkerRot(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId);
            return result;
        }

        public void OnConfirmClicked()
        {
            _confirmValue = ButtonState.CONFIRM;
        }

        public void OnRestartClicked()
        {
            _confirmValue = ButtonState.RESTART;
        }

        // Idle state execution
        IEnumerator ExecuteMarkerDetection()
        {
            float timeElapsed = 0;
            guidePlaneRenderer.material.color = calibGrey;
            guidePlaneRenderer.gameObject.SetActive(true);
            while (!_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId) && timeElapsed < timeOutDuration)
            {
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            // Check if the condition is met or timed out
            if (_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId))
            {
                Debug.Log("Marker detected within 2 minutes!");
                SwitchState(State.Stabilization);
            }
            else
            {
                Debug.Log("TimeOut! No marker detected");
                SwitchState(State.TimeOut);
            }
        }

        IEnumerator ExecuteStabilization()
        {
            float timeElapsed = 0;
            while (_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId) && timeElapsed < timeOutDuration)
            {
                if (IsMarkerStable())
                {
                    Debug.Log("Marker is stable, switch to the countdown state");
                    SwitchState(State.CountDown);
                    yield break;
                }
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            if (!_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId))
            {
                Debug.Log("Lost Marker tracking!");
                SwitchState(State.MarkerDetection);
            }
            else
            {
                Debug.Log("TimeOut! Marker not stable");
                SwitchState(State.TimeOut);
            }
        }

        // show countdown
        IEnumerator ExecuteCountDown()
        {
            float timeElapsed = 0;
            progressBar.Show(countDownDuration);
            while (_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId) && IsMarkerStable())
            {
                if(timeElapsed > countDownDuration)
                {
                    progressBar.Hide();
                    Debug.Log("Marker stable for " + countDownDuration + "s");
                    SwitchState(State.Confirmation);
                    yield break;
                }
                timeElapsed += Time.deltaTime;
                progressBar.SetValue(timeElapsed);
                yield return null;
            }
            progressBar.Hide();
            if (!_appManager.IsMarkerDetected(UltraSoundAppConfigManager.Instance.AppConfig.TrackerProbeMarkerId))
            {
                Debug.Log("Lost Marker tracking!");
                SwitchState(State.MarkerDetection);
            }
            else
            {
                Debug.Log("Marker not stable, back to stablization detection");
                SwitchState(State.Stabilization);
            }
        }

        IEnumerator ExecuteConfirmation()
        {
            if (_confirmValue != ButtonState.IDLE) yield break;

            guidePlaneRenderer.material.color = calibGreen;
            confirmButton.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(true);
            Debug.Log("Waiting for buttons");
            
            while(_confirmValue == ButtonState.IDLE)
            {
                yield return null;
            }

            confirmButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(false);
            if (_confirmValue == ButtonState.RESTART)
            {
                //reset
                SwitchState(State.MarkerDetection);
            }
            else
            {
                //confirm
                SwitchState(State.Confirm);
            }
        }

        IEnumerator ExecuteTimeOut()
        {
            Debug.LogWarning("Not Implement yet !");
            yield return new WaitForSeconds(0.1f);
            CancelProcess();
        }

        private void ExecuteConfirmPressed()
        {
            Matrix4x4 m = Matrix4x4.identity;
            m.SetTRS(_prevMarkerPos, _prevMarkerRot, Vector3.one);
            Vector3 posOffset = m.inverse.MultiplyPoint3x4(guidePlaneRenderer.transform.position);
            Quaternion rotOffset = Quaternion.Inverse(_prevMarkerRot) * guidePlaneRenderer.transform.rotation;

            _appManager.SetProbeOffset(posOffset, rotOffset.eulerAngles, true);

            CancelProcess(); // use cancel process to exit
        }

        private void SwitchState(State newState)
        {
            if (CurrentCalibrateState == newState) return;
            CurrentCalibrateState = newState;
            if(_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
            switch (CurrentCalibrateState)
            {
                case State.Idle:
                    backPlate.SetActive(false);
                    placementObj.SetActive(false);
                    guideText.SetActive(false);
                    progressBar.Hide();
                    confirmButton.gameObject.SetActive(false);
                    restartButton.gameObject.SetActive(false);
                    warningPrompt.gameObject.SetActive(false);
                    break;
                case State.Warning:
                    backPlate.SetActive(false);
                    placementObj.SetActive(false);
                    guideText.SetActive(false);
                    progressBar.Hide();
                    confirmButton.gameObject.SetActive(false);
                    restartButton.gameObject.SetActive(false);
                    warningPrompt.gameObject.SetActive(true);
                    break;
                case State.MarkerDetection:
                    backPlate.SetActive(true);
                    placementObj.SetActive(true);
                    guideText.SetActive(false);
                    progressBar.Hide();
                    confirmButton.gameObject.SetActive(false);
                    restartButton.gameObject.SetActive(false);
                    warningPrompt.gameObject.SetActive(false);
                    _confirmValue = ButtonState.IDLE;
                    _coroutine = StartCoroutine(ExecuteMarkerDetection());
                    break;
                case State.Stabilization:
                    backPlate.SetActive(true);
                    placementObj.SetActive(true);
                    guideText.SetActive(true);
                    _coroutine = StartCoroutine(ExecuteStabilization());
                    break;
                case State.CountDown:
                    backPlate.SetActive(true);
                    placementObj.SetActive(true);
                    guideText.SetActive(true);
                    _coroutine = StartCoroutine(ExecuteCountDown());
                    break;
                case State.Confirmation:
                    backPlate.SetActive(true);
                    placementObj.SetActive(true);
                    guideText.SetActive(false);
                    _coroutine = StartCoroutine(ExecuteConfirmation());
                    break;
                case State.TimeOut:
                    backPlate.SetActive(true);
                    placementObj.SetActive(true);
                    guideText.SetActive(false);
                    _coroutine = StartCoroutine(ExecuteTimeOut());
                    break;
                case State.Confirm:
                    backPlate.SetActive(false);
                    placementObj.SetActive(false);
                    guideText.SetActive(false);
                    ExecuteConfirmPressed();
                    break;
                default:
                    Debug.Log("Not Implemented yet for State: " + CurrentCalibrateState.ToString());
                    break;
            }
        }
    }

}
