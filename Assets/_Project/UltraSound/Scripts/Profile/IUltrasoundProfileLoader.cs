using System.Collections.Generic;
using System.Threading.Tasks;

namespace NUHS.UltraSound.Profile
{
    public interface IUltrasoundProfileLoader
    {
        Task<List<UltrasoundProfile>> List(bool showHidden);
        Task<UltrasoundProfile> Get(string id);
    }
}
