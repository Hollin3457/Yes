using System.Collections.Generic;
using UnityEngine;
using Grpc.Core;
using NUHS.Backend;
using System.Threading.Tasks;

namespace NUHS.UltraSound.Profile
{
    /// <summary>
    /// A Profile Loader that connects to the Backend via a gRPC Channel
    /// </summary>
    public class BackendUltrasoundProfileLoader : IUltrasoundProfileLoader
    {
        private Channel _channel;
        private UltrasoundProfileServiceV1.UltrasoundProfileServiceV1Client _service;

        public BackendUltrasoundProfileLoader(Channel backendChannel)
        {
            _channel = backendChannel;
            _service = new UltrasoundProfileServiceV1.UltrasoundProfileServiceV1Client(_channel);
        }
        public async Task<UltrasoundProfile> Get(string id)
        {
            var response = await _service.GetAsync(new UltrasoundProfileGetRequestV1() { Id = id });
            return ConvertFromGrpcProfile(response);
        }

        public async Task<List<UltrasoundProfile>> List(bool showHidden)
        {
            var response = await _service.ListAsync(new UltrasoundProfileListRequestV1() { ShowHidden = showHidden });
            var list = new List<UltrasoundProfile>();
            foreach (var p in response.Profiles)
            {
                list.Add(ConvertFromGrpcProfileSummary(p));
            }
            return list;
        }

        private UltrasoundProfile ConvertFromGrpcProfile(UltrasoundProfileV1 p)
        {
            var profile = new UltrasoundProfile()
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Image = new Texture2D(1, 1),
                IsHidden = p.Hidden,
                DeviceSizeInCm = new Vector3(p.DeviceSize.X, p.DeviceSize.Y, p.DeviceSize.Z),
                DeviceType = p.DeviceType,
                DeviceConfig = p.DeviceConfig,
                IsSummary = false
            };

            profile.Image.LoadImage(p.Image.ToByteArray());

            return profile;
        }

        private UltrasoundProfile ConvertFromGrpcProfileSummary(UltrasoundProfileSummaryV1 p)
        {
            var profile = new UltrasoundProfile()
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Image = new Texture2D(1, 1),
                IsHidden = p.Hidden,
                IsSummary = true
            };

            profile.Image.LoadImage(p.Image.ToByteArray());

            return profile;
        }
    }
}
