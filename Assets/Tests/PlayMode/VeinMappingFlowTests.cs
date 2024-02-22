using System.Collections;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Tests;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace NUHS.Tests.PlayMode
{
    public class VeinMappingFlowTests
    {
        private InputSimulationService simulationService;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            AsyncOperation loadOp = EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/VeinMapping/Scenes/VeinMapping.unity", new LoadSceneParameters(LoadSceneMode.Single));
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
            Scene scene = EditorSceneManager.GetSceneByName("VeinMapping");
            if (scene.isLoaded)
            {
                var op = EditorSceneManager.UnloadSceneAsync(scene);
                while (op != null && !op.isDone)
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Tests the basic flow of the "Vein Mapping" feature.
        /// </summary>
        [UnityTest]
        public IEnumerator ShouldNotThrowErrors()
        {
            var cameraTransform = CameraCache.Main.transform;

            var veinsToggle = GameObject.Find("MixedRealitySceneContent/VeinVisibilityToggle");
            Assert.NotNull(veinsToggle);
            Assert.True(veinsToggle.activeInHierarchy);

            var veinGuidePrompt = GameObject.Find("MixedRealitySceneContent/VeinGuidePrompt");
            Assert.NotNull(veinGuidePrompt);
            Assert.True(veinGuidePrompt.activeInHierarchy);

            var cuboid = GameObject.Find("MixedRealitySceneContent/Cuboid");
            Assert.NotNull(cuboid);
            Assert.True(cuboid.activeInHierarchy);

            var veinTexture = GameObject.Find("MixedRealitySceneContent/Cuboid/VeinTexture");
            Assert.NotNull(veinTexture);
            Assert.True(veinTexture.activeInHierarchy);

            // Show right hand in front of the button.
            var rightHandPos = veinsToggle.transform.position;
            rightHandPos -= veinsToggle.transform.forward * 0.1f;
            var rightHand = new TestHand(Handedness.Right);
            yield return rightHand.Show(rightHandPos);
            yield return new WaitForSeconds(1);

            // Move right hand forward to press the button then back.
            rightHandPos += veinsToggle.transform.forward * 0.13f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(0.5f);
            rightHandPos -= veinsToggle.transform.forward * 0.13f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(0.5f);
            Assert.False(veinTexture.activeInHierarchy);

            // Move right hand forward to press the button then back.
            rightHandPos += veinsToggle.transform.forward * 0.13f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(0.5f);
            rightHandPos -= veinsToggle.transform.forward * 0.13f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            Assert.True(veinTexture.activeInHierarchy);

            // Move right hand to the front face of the cuboid and pinch.
            rightHandPos = cuboid.transform.position;
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

            // Verify that the prompt is hidden. By design it should hide when the user starts interacting with the cuboid.
            Assert.NotNull(veinGuidePrompt);
            Assert.False(veinGuidePrompt.activeInHierarchy);

            var expectedCuboidPos = cuboidStartPos - cameraTransform.forward * 0.1f;
            TestUtilities.AssertAboutEqual(cuboid.transform.position, expectedCuboidPos, "cuboid doesn't match expected position");

            // Move right hand to the top-front corner of the cuboid and pinch.
            // The extra 0.02 serves as 2cm padding. Pinching on the cuboid surface
            // makes it translate instead of scale/rotate.
            rightHandPos += cuboid.transform.up * cuboid.transform.localScale.y * 0.5f - cuboid.transform.forward * 0.02f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.Pinch);
            yield return new WaitForSeconds(1);

            // Increase height of the cuboid by dragging in the cuboid's down direction and release pinch.
            var cuboidStartRotation = cuboid.transform.rotation;
            rightHandPos -= cuboid.transform.up * cuboid.transform.localScale.y * 0.5f;
            yield return rightHand.MoveTo(rightHandPos);
            yield return new WaitForSeconds(1);
            yield return rightHand.SetGesture(ArticulatedHandPose.GestureId.Open);

            TestUtilities.AssertNotAboutEqual(cuboid.transform.rotation, cuboidStartRotation, "cuboid didn't rotate", tolerance: 0.05f);

            yield return new WaitForSeconds(2);
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
