using UnityEngine;

namespace NUHS.UltraSound.Tracking
{
    [CreateAssetMenu(fileName = "MarkerConfig", menuName = "Config/Marker Config")]
    public class MarkerConfig : ScriptableObject
    {
        public int deviceId;
        public int configId;
        public Vector3 posOffset;
        public Vector3 rotationOffset;

        public void ApplyMarkerConfigurationForTargetTransform(Transform targetTransform)
        {
            targetTransform.localPosition = posOffset;
            targetTransform.localRotation = Quaternion.Euler(rotationOffset);
        }

        public void Save()
        {
            var configStr = JsonUtility.ToJson(this);
            PlayerPrefs.SetString("MarkerConfig", configStr);
            Debug.Log(configStr);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            if (PlayerPrefs.HasKey("MarkerConfig"))
            {
                string jsonData = PlayerPrefs.GetString("MarkerConfig");
                JsonUtility.FromJsonOverwrite(jsonData, this);
            }
        }
    }
}