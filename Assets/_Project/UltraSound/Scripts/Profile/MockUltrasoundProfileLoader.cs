using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NUHS.UltraSound.Profile
{
    /// <summary>
    /// A dummy Profile Loader which returns two static profiles
    /// </summary>
    public class MockUltrasoundProfileLoader : IUltrasoundProfileLoader
    {
        private IDictionary<string, UltrasoundProfile> _profiles;

        public MockUltrasoundProfileLoader(IEnumerable<UltrasoundProfile> profiles)
        {
            _profiles = new Dictionary<string, UltrasoundProfile>();
            foreach (var p in profiles) _profiles.Add(p.Id, p);
        }

        public MockUltrasoundProfileLoader() : this(new UltrasoundProfile[] {
            new UltrasoundProfile {
                Id = Guid.NewGuid().ToString(),
                Name = "Mock",
                Description = "This is a mock profile.",
                Image = new Texture2D(400, 400), // This is supposed to be a photo of the device that we show in the profile picker, not in use right now
                IsHidden = false,
                DeviceSizeInCm = new Vector3(5f, 15f, 1f),
                DeviceType = "mock",
                DeviceConfig = ""
            },
            new UltrasoundProfile {
                Id =  Guid.NewGuid().ToString(),
                Name = "Sidecar",
                Description = "This is a mock sidecar profile.",
                Image = new Texture2D(400, 400), // This is supposed to be a photo of the device that we show in the profile picker, not in use right now
                IsHidden = false,
                DeviceSizeInCm = new Vector3(4.5f, 15.8f, 0.8f),
                DeviceType = "sidecar",
                DeviceConfig = "{\"DeviceId\":2,\"CaptureW\":1280,\"CaptureH\":720,\"CropX\":496,\"CropY\":75,\"CropW\":374,\"CropH\":538,\"PixelsPerCm\":97,\"DetectPixelsPerCm\":false,\"DetectPixelsPerCmMethod\":0,\"FPS\":15,\"Quality\":70}"
            },
        }) { }

        public Task<UltrasoundProfile> Get(string id)
        {
            if (_profiles.ContainsKey(id)) return Task.FromResult(_profiles[id]);
            return Task.FromResult(default(UltrasoundProfile));
        }

        public Task<List<UltrasoundProfile>> List(bool showHidden)
        {
            return Task.FromResult(new List<UltrasoundProfile>(_profiles.Values));
        }
    }
}
