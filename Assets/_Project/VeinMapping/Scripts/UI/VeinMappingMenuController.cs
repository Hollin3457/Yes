using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using NUHS.Common.UI;
using UnityEngine;

namespace NUHS.VeinMapping
{
    public class VeinMappingMenuController : MonoBehaviour
    {
        [Header("Sending Events")] 
        [SerializeField] private SimpleEvent applicationQuitEvent;
        
        [Header("UI Dependency")]
        [SerializeField] private GameObject buttonGroup;
        [SerializeField] private PressableButton cubeButton;
        [SerializeField] private SolverHandler solverHandler;
        [SerializeField] private CommonButton quitApplicationButton;

        private Vector3 grabStartPosition;

        private void Awake()
        {
            solverHandler = GetComponent<SolverHandler>();
            cubeButton.ButtonReleased.AddListener(ToggleMenu);
            quitApplicationButton.AddButtonAction(()=>applicationQuitEvent?.Send());
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

        private void ToggleMenu()
        {
            buttonGroup.SetActive(!buttonGroup.activeSelf);
        }
    }
}
