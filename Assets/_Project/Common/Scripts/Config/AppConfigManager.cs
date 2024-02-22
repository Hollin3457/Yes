using UnityEngine;
using UnityEngine.Events;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Collections;

namespace NUHS.Common
{
    /// <summary>
    /// User Attributes to send to the RemoteConfig service.
    /// This is usually used so that rules can be applied to send different configs.
    /// e.g. user or device-specific configurations.
    /// </summary>
    public struct UserAttributes { }

    /// <summary>
    /// App Attributes to send to the Remote Config service.
    /// This is usually used so that rules can be applied to send different configs.
    /// e.g. for A/B testing.
    /// </summary>
    public struct AppAttributes { }

    public class AppConfigManager<T> : MonoBehaviour where T : new()
    {
        /// <summary>
        /// Global instance of this AppConfigManager 
        /// </summary>
        public static AppConfigManager<T> Instance { get; private set; }

        /// <summary>
        /// Application Config Model
        /// </summary>
        public T AppConfig { get; private set; } = new T();

        /// <summary>
        /// Config file name
        /// </summary>
        public string configFile = "config.json";

        /// <summary>
        /// User Attributes to send to the RemoteConfig service.
        /// NOTE: Commented out as RemoteConfig is not working
        /// </summary>
        //public UserAttributes userAttributes = new UserAttributes();

        /// <summary>
        /// App Attributes to send to the RemoteConfig service.
        /// NOTE: Commented out as RemoteConfig is not working
        /// </summary>
        //public AppAttributes appAttributes = new AppAttributes();

        /// <summary>
        /// Register this event to be notified when configs are successfully loaded
        /// </summary>
        public UnityEvent OnReady;

        /// <summary>
        /// Full file path of the config file
        /// </summary>
        private string _filePath;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }

            // Original code to use Unity Remote Config
            // if (Utilities.CheckForInternetConnection())
            // {
            //     await UnityServices.InitializeAsync();
            //
            //     if (!AuthenticationService.Instance.IsSignedIn)
            //     {
            //         await AuthenticationService.Instance.SignInAnonymouslyAsync();
            //     }
            // }
            //
            // RemoteConfigService.Instance.FetchCompleted += FetchCompleted;
            // RemoteConfigService.Instance.FetchConfigs(userAttributes, appAttributes);
        }

        private void Start()
        {
            _filePath = Path.Combine(Application.persistentDataPath, configFile);
            StartCoroutine(TryLoadConfig());
        }

        private IEnumerator TryLoadConfig()
        {
            Debug.Log("Loading Config: " + _filePath);
            if (!File.Exists(_filePath))
            {
                SaveConfig();
                yield return null;
            }
            else
            {
                yield return LoadConfig();
            }

            OnReady.Invoke();
        }

        private IEnumerator LoadConfig()
        {
            // For HoloLens, use UnityWebRequest to read the file
            UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(_filePath);
            yield return www.SendWebRequest();

            if (!www.isNetworkError && !www.isHttpError)
            {
                string json = www.downloadHandler.text;

                try
                {
                    // Process the JSON data
                    AppConfig = JsonUtility.FromJson<T>(json);
                    Debug.Log($"Loaded AppConfig: {json}");
                }
                catch (Exception ex)
                {
                    Debug.Log($"Failed to parse AppConfig: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"Failed to read AppConfig: {www.error}");
            }
        }

        public void SaveConfig()
        {
            Debug.Log($"Saving AppConfig to file: {_filePath}");
            try
            {
                string jsonString = JsonUtility.ToJson(AppConfig, true);
                File.WriteAllText(_filePath, jsonString);
            }
            catch (Exception ex)
            {
                Debug.Log($"Unable to write AppConfig to file: {_filePath}");
                Debug.Log(ex.Message);
            }
        }

        // void FetchCompleted(ConfigResponse response)
        // {
        //     switch (response.requestOrigin)
        //     {
        //         case ConfigOrigin.Default:
        //             Debug.Log($"[AppConfigManager] No settings loaded this session; using default values.");
        //             return;
        //         case ConfigOrigin.Cached:
        //             Debug.Log($"[AppConfigManager] No settings loaded this session; using cached values from a previous session.");
        //             AppConfig = RemoteConfigService.Instance.appConfig.config.ToObject<T>();
        //             break;
        //         case ConfigOrigin.Remote:
        //             Debug.Log($"[AppConfigManager] New settings loaded this session; updating values accordingly.");
        //             break;
        //     }
        //
        //     AppConfig = RemoteConfigService.Instance.appConfig.config.ToObject<T>();
        //     Debug.Log($"[AppConfigManager] AppConfig = {JsonUtility.ToJson(AppConfig)}");
        //
        //     OnReady.Invoke();
        // }

        private void OnDestroy()
        {
            //RemoteConfigService.Instance.FetchCompleted -= FetchCompleted;
        }
    }
}
