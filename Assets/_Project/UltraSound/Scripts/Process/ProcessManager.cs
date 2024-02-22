using UnityEngine;

namespace NUHS.UltraSound.Process
{
    public abstract class ProcessManager : MonoBehaviour
    {
        public abstract void StartProcess(AppManager appManager);
        public abstract void StopProcess();
    }
}