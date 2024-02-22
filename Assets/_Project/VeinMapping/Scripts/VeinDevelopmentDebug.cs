using NUHS.Common.InternalDebug;
using UnityEngine;
using UnityEngine.UI;

namespace NUHS.VeinMapping
{
    public class VeinDevelopmentDebug : MonoBehaviour
    {
        public Text renderFPS;
        public Text serverFPS;
        private void LateUpdate()
        {
            DebugUtils.RenderTick();
            float renderDeltaTime = DebugUtils.GetRenderDeltaTime();
            float serverDeltaTime = DebugUtils.GetVideoDeltaTime(); //reuse the video delta time to track vein server process performance

            if (renderFPS != null)
            {
                renderFPS.text = string.Format("Render: {0:0.0} ms ({1:0.} fps)", renderDeltaTime, 1000.0f / renderDeltaTime);
            }
            if (serverFPS != null)
            {
                serverFPS.text = string.Format("Server: {0:0.0} ms ({1:0.} fps)", serverDeltaTime, 1000.0f / serverDeltaTime);
            }
        }
    }
}