using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NUHS.Common
{
    /// <summary>
    /// Class that listens for and acts upon speech commands.
    /// </summary>
    public class NuhsSpeechCommand : MonoBehaviour, IMixedRealitySpeechHandler
    {
        [SerializeField] private string keyword;
        [SerializeField] private UnityEvent action;

        private bool _registeredForInput = false;

        private void OnEnable()
        {
            if (!_registeredForInput)
            {
                if (CoreServices.InputSystem != null)
                {
                    CoreServices.InputSystem.RegisterHandler<IMixedRealitySpeechHandler>(this);
                    _registeredForInput = true;
                }
            }
        }

        private void OnDisable()
        {
            if (_registeredForInput)
            {
                CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySpeechHandler>(this);
                _registeredForInput = false;
            }
        }

        /// <inheritdoc />
        void IMixedRealitySpeechHandler.OnSpeechKeywordRecognized(SpeechEventData eventData)
        {
            if (eventData.Command.Keyword.Equals(keyword, StringComparison.OrdinalIgnoreCase))
            {
                action.Invoke();
            }
        }
    }
}
