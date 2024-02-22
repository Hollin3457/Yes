using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using NUHS.UltraSound.Tracking;

namespace NUHS.UltraSound.UI
{
    public class QuickViewController : MonoBehaviour
    {
        // public UltrasoundImageReceiver ultrasoundImageReceiver;
        public GameObject smallUltrasoundController;
        public GameObject bigUltrasoundController;

        private bool isRunning = false;
        [Header("Receiving Event")]
        [SerializeField] private SimpleEvent recordScanEvent;
        [SerializeField] private SimpleEvent quickViewEvent;
        [SerializeField] private SimpleEvent recalibrateEvent;
        [SerializeField] private SimpleEvent fileFinderEvent;

        private void OnEnable()
        {
            
            recordScanEvent.Register(TrigerOther);
            quickViewEvent.Register(TriggerQuickView);
            recalibrateEvent.Register(TrigerOther);
            fileFinderEvent.Register(TrigerOther);
        }

        private void OnDisable()
        {
            recordScanEvent.Unregister(TrigerOther);
            quickViewEvent.Unregister(TriggerQuickView);
            recalibrateEvent.Unregister(TrigerOther);
            fileFinderEvent.Unregister(TrigerOther);
        }

        private void TriggerQuickView()
        {
            if (isRunning) return;
            isRunning = true;
            //ultrasoundImageReceiver.Play();
            OnZoomToSmallPressed();
        }

        private async void TrigerOther()
        {
            if (!isRunning) return;
            isRunning = false;
            //await ultrasoundImageReceiver.Stop();
            OnZoomEnd();
        }

        public void OnZoomToBigPressed()
        {
            Debug.Log("Show big Ultrasound");
            smallUltrasoundController.gameObject.SetActive(false);
            bigUltrasoundController.gameObject.SetActive(true);
        }

        public void OnZoomToSmallPressed()
        {
            Debug.Log("Show small Ultrasound");
            smallUltrasoundController.gameObject.SetActive(true);
            bigUltrasoundController.gameObject.SetActive(false);
        }

        public void OnZoomEnd()
        {
            Debug.Log("Exit Ultrasound");
            smallUltrasoundController.gameObject.SetActive(false);
            bigUltrasoundController.gameObject.SetActive(false);
        }
    }
}

