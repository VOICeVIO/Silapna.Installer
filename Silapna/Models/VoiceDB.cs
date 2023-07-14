using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Silapna.Models
{
    class VoiceInfo
    {
        public Ppkg Package { get; set; }
        public string BasePath { get; set; }
    }

    class VoiceDbInfo
    {
        public Dictionary<string, string> NameMap { get; set; }
        public List<VoiceInfo> Voices { get; set; }
    }

    public class VoiceDb
    {
        public const string BasePath = "Vanarana";
        public const string Name = "voices.json";
        public const string Env = "VOICEPEAK_EDITOR_CONFIG_HOME";

        public Dictionary<string, List<string>> IdcNameMap { get; set; }

        public async Task CollectVoices(string storagePath)
        {

        }

        public async Task<bool> BuildVirtualStorage(string storagePath)
        {
            if (!Directory.Exists(storagePath))
            { 
                return false;
            }

            var basePath = Path.Combine(AppContext.BaseDirectory, BasePath);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            return true;
        }

        public async Task<bool> DeleteVirtualStorage()
        {
            return true;
        }
    }
}
