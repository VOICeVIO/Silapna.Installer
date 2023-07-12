using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silapna.Models
{
    public class VoiceDB
    {
        public const string BasePath = "Vanarana";
        public const string Name = "voices.json";
        public Dictionary<string, List<string>> IdcNameMap { get; set; }

        public async Task<bool> BuildVirtualStorage()
        {
            return true;
        }

        public async Task<bool> DeleteVirtualStorage()
        {
            return true;
        }
    }
}
