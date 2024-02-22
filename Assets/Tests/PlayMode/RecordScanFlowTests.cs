using System.Collections;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Tests;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NUHS.Tests.PlayMode
{
    public class RecordScanFlowTests
    {
        private InputSimulationService simulationService;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            AsyncOperation loadOp = SceneManager.LoadSceneAsync("UltraSound", LoadSceneMode.Single);
            loadOp.allowSceneActivation = true;
            while (!loadOp.isDone)
            {
                yield return null;
            }

            // Disable user input during tests
            simulationService = GetInputSimulationService();
            simulationService.UserInputEnabled = false;
            yield return true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Scene scene = SceneManager.GetSceneByName("UltraSound");
            if (scene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(scene.buildIndex);
            }
            yield return null;
        }

        /// <summary>
        /// Tests the overall flow of the "Record Scan" feature.
        /// </summary>
        /// <remarks>
        /// If the sidecar isn't accessible, the 'Dev' scripting define symbol needs to be set in PlayerSettings
        /// in order to satisfy the IP address prompt.
        /// </remarks>
        [UnityTest]
        public IEnumerator RecordScanFlowShouldNotThrowErrors()
        {
            var cameraTransform = CameraCache.Main.transform;
            var leftHandPos = cameraTransform.position + cameraTransform.forward * 0.5f - (cameraTransform.right * 0.01f);
            var camToLeftHand = (leftHandPos - cameraTransform.position).normalized;

            // Generate hand rotation with hand palm facing camera
            var handRotation = Quaternion.LookRotation(-camToLeftHand, Vector3.up);

            // Do palm-up gesture with left hand to show main menu.
            var leftHand = new TestHand(Handedness.Left);
            yield return leftHand.SetGesture(ArticulatedHandPose.GestureId.Flat);
            yield return leftHand.Show(leftHandPos);
            yield return new WaitForSeconds(1);
            yield return leftHand.SetRotation(handRotation);
            yield return new WaitForSeconds(1);

            var recordScanButton = GameObject.Find("MixedRealitySceneContent/MainNavMenuController/MainNavMenu/ExpandedMainNavButtonGroup/RecordScanButton");
            Assert.NotNull(recordScanButton);
            Assert.True(recordScanButton.activeInHierarchy);

            // Show right hand in front of the button.
            var rightHandPos = recordScanButton.transform.position;
            rightHandPos -= recordScanButton.transform.forward * 0.1f;
            var rightHand = new TestHand(Handedness.Right);
            yield return rightHand.Show(rightHandPos);
            yield return new WaitForSeconds(1);

            // Move right hand forward to press the button then back.
            rightHandPos += recordScanButton.transform.forward * 0.13f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            rightHandPos -= recordScanButton.transform.forward * 0.13f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);

            // Dismiss main menu by hiding the left hand.
            yield return leftHand.Hide();
            Assert.False(recordScanButton.activeInHierarchy);
            yield return new WaitForSeconds(1);

            // ---------------------

            var ipConfirmButton = GameObject.Find("MixedRealitySceneContent/IPAddressPromptController/IPAddressPrompt/Canvas/Panel/ButtonGroupPlaceHolder/ConfirmButtonHolder/ConfirmButton");
            Assert.NotNull(ipConfirmButton);

            // IP address prompt only shows if value is not saved in PlayerPrefs.
            if (ipConfirmButton.activeInHierarchy)
            {
                rightHandPos = ipConfirmButton.transform.position;
                rightHandPos -= ipConfirmButton.transform.forward * 0.1f;
                yield return rightHand.MoveTo(rightHandPos);

                yield return new WaitForSeconds(2);

                // Move right hand forward to press the button then back.
                rightHandPos += ipConfirmButton.transform.forward * 0.1f;
                yield return rightHand.MoveTo(rightHandPos);
                yield return new WaitForSeconds(1);
                rightHandPos -= ipConfirmButton.transform.forward * 0.1f;
                yield return rightHand.MoveTo(rightHandPos);
                yield return new WaitForSeconds(1);
            }

            // ---------------------

            var cuboid = GameObject.Find("MixedRealitySceneContent/RecordScanManager/Cuboid");
            Assert.NotNull(cuboid);

            // Wait for guide prompts to finish.
            var guidePrompt = GameObject.Find("MixedRealitySceneContent/RecordScanManager/PassiveGuidePrompts/PassiveGuidePrompt");
            Assert.NotNull(guidePrompt);
            yield return new WaitUntil(() => guidePrompt.activeSelf);
            yield return new WaitUntil(() => !guidePrompt.activeSelf);

            // Start scan prompt shows after guidePrompt2 disappears, so just wait briefly before interacting with the cuboid.
            yield return new WaitForSeconds(0.5f);

            // Move right hand to the front face of the cuboid and pinch.
            rightHandPos = cuboid.transform.position;
            rightHandPos -= cuboid.transform.forward * cuboid.transform.localScale.z * 0.5f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.Pinch);
            yield return new WaitForSeconds(1);

            // Move cuboid toward camera (via hand) and release pinch.
            var cuboidStartPos = cuboid.transform.position;
            rightHandPos -= cameraTransform.forward * 0.1f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.Open);

            var expectedCuboidPos = cuboidStartPos - cameraTransform.forward * 0.1f;
            TestUtilities.AssertAboutEqual(cuboid.transform.position, expectedCuboidPos, "cuboid doesn't match expected position");

            // Move right hand to the top-right-front corner of the cuboid and pinch.
            // The extra 0.02 serves as 2cm padding. Pinching on the cuboid surface
            // makes it translate instead of scale/rotate.
            rightHandPos +=
                cuboid.transform.up * (cuboid.transform.localScale.y * 0.5f + 0.02f) +
                cuboid.transform.right * (cuboid.transform.localScale.x * 0.5f + 0.02f) -
                cuboid.transform.forward * 0.02f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.Pinch);
            yield return new WaitForSeconds(1);

            // Increase height of the cuboid by dragging in the cuboid's up direction and release pinch.
            var cuboidStartScale = cuboid.transform.localScale;
            rightHandPos += cuboid.transform.up * 0.1f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.Open);

            var expectedCuboidScale = cuboidStartScale + Vector3.up * 0.1f;
            TestUtilities.AssertAboutEqual(cuboid.transform.localScale, expectedCuboidScale, "cuboid doesn't match expected scale", tolerance: 0.05f);

            yield return new WaitForSeconds(3);

            // TODO: More assertions and continue where we left off. Next step is pressing "start scan" button.
        }

        /// <summary>
        /// Utility function to simplify code for getting access to the running InputSimulationService
        /// </summary>
        /// <returns>Returns InputSimulationService registered for playmode test scene</returns>
        public static InputSimulationService GetInputSimulationService()
        {
            var inputSimulationService = CoreServices.GetInputSystemDataProvider<InputSimulationService>();
            Debug.Assert((inputSimulationService != null), "InputSimulationService is null!");
            return inputSimulationService;
        }
    }
}
