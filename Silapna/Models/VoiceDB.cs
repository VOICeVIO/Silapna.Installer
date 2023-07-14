using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Silapna.Models
{
    public class VoiceInfo
    {
        public Ppkg Package { get; set; }
        public string BasePath { get; set; }

        [JsonConstructor]
        public VoiceInfo()
        {
        }
    }

    public class VoiceDbInfo
    {
        public Dictionary<string, string> NameMap { get; set; } = new();
        public List<VoiceInfo> Voices { get; set; } = new();
        public Dictionary<string, List<string>> NarratorComponents { get; set; } = new();
        
        [JsonConstructor]
        public VoiceDbInfo(){}
    }

    public class VoiceDb
    {
        public const string BasePath = "Vanarana";
        public const string FileName = "voices.json";
        public const string VoiceMapName = "voice_map.json";
        public const string Env = "VOICEPEAK_EDITOR_CONFIG_HOME";

        public VoiceDbInfo Db { get; set; } = new VoiceDbInfo();
        public string DefaultPath => Path.Combine(AppContext.BaseDirectory, BasePath, FileName);
        public string DefaultBasePath => Path.Combine(AppContext.BaseDirectory, BasePath);

        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.Indented
        };

        public async Task Save(string? path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = DefaultPath;

                var dir = DefaultBasePath;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            var json = JsonConvert.SerializeObject(Db, _settings);
            await File.WriteAllTextAsync(path, json);

            var mapPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, VoiceMapName);
            var mapJson = JsonConvert.SerializeObject(Db.NameMap, _settings);
            await File.WriteAllTextAsync(mapPath, mapJson);
        }

        public async Task Load(string? path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = DefaultPath;
            }

            if (!File.Exists(path))
            {
                return;
            }
            
            Db = JsonConvert.DeserializeObject<VoiceDbInfo>(await File.ReadAllTextAsync(path)) ?? new VoiceDbInfo();
        }

        public async Task<List<VoiceInfo>> CollectVoices(string storagePath, bool clear = true)
        {
            if (clear)
            {
                Db = new VoiceDbInfo();
            }
            List<VoiceInfo> installedProducts = new();
            Dictionary<string, List<string>> entranceComponents = new();

            foreach (var idcFile in Directory.GetFiles(storagePath, "*.idc"))
            {
                try
                {
                    var idc = JsonConvert.DeserializeObject<EntranceComponent>(await File.ReadAllTextAsync(idcFile));
                    if (idc != null)
                    {
                        if (entranceComponents.ContainsKey(idc.narrator_id))
                        {
                            entranceComponents[idc.narrator_id].Add(idc.entrance_component);
                        }
                        else
                        {
                            entranceComponents.Add(idc.narrator_id, new List<string>() {idc.entrance_component});
                        }
                    }
                }
                catch
                {
                    //ignored
                }
            }

            foreach (var entranceComponentsValue in entranceComponents.Values)
            {
                entranceComponentsValue.Sort();
            }
            Db.NarratorComponents = entranceComponents;

            foreach (var ppkgFile in Directory.GetFiles(storagePath, "*.ppkg"))
            {
                try
                {
                    var ppkg = JsonConvert.DeserializeObject<Ppkg>(await File.ReadAllTextAsync(ppkgFile));
                    if (ppkg != null)
                    {
                        var info = new VoiceInfo() {BasePath = storagePath, Package = ppkg};
                        installedProducts.Add(info);

                        if (ppkg.narrator_list.Length == 1)
                        {
                            var productName = ppkg.prod_name;
                            var idcName = ppkg.narrator_list[0].ndc_name;
                            if (entranceComponents.TryGetValue(idcName, out var components))
                            {
                                var entrance = components.FirstOrDefault();
                                if (!string.IsNullOrEmpty(entrance))
                                {
                                    Db.NameMap[productName] = entrance;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    //ignored
                }
            }

            foreach (var kv in entranceComponents)
            {
                var entrance = kv.Value.FirstOrDefault();
                if (!string.IsNullOrEmpty(entrance))
                {
                    Db.NameMap[kv.Key] = entrance;
                }
            }

            Db.Voices = installedProducts;

            return installedProducts;
        }

        public async Task<bool> BuildVirtualStorage(string storagePath)
        {
            if (!Directory.Exists(storagePath))
            { 
                return false;
            }

            var voices = await CollectVoices(storagePath);
            if (voices.Count == 0)
            {
                return false;
            }

            var basePath = Path.Combine(AppContext.BaseDirectory, BasePath);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            foreach (var voiceInfo in voices)
            {
                foreach (var narrator in voiceInfo.Package.narrator_list)
                {
                    var dir = Path.Combine(basePath, narrator.ndc_name);
                }

            }

            return true;
        }

        public async Task<bool> DeleteVirtualStorage()
        {
            return true;
        }
    }
}
