using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Silapna.Models
{
    public class VoiceInfo
    {
        public Ppkg Package { get; set; }
        public string BasePath { get; set; }

        public string PpkgPath { get; set; }

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
        public VoiceDbInfo()
        {
        }
    }

    public class VoiceDb
    {
        public const string BaseName = "Vanarana";
        public const string FileName = "voices.json";
        public const string VoiceMapName = "voice_map.json";
        public const string Env = "VOICEPEAK_EDITOR_CONFIG_HOME";
        private const string VpDefaultPathC = "C:\\Program Files\\Voicepeak";
        private const string VpDefaultPathD = "D:\\Program Files\\Voicepeak";

        public VoiceDbInfo Db { get; set; } = new VoiceDbInfo();
        public readonly string StorageParentPath;
        public string DefaultPath => Path.Combine(StorageParentPath, BaseName, FileName);
        public string DefaultBasePath => Path.Combine(StorageParentPath, BaseName);

        public VoiceDb(string storageParent)
        { 
            StorageParentPath = storageParent;
        }

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

            var mapPath = Path.Combine(AppContext.BaseDirectory, VoiceMapName);
            var map = Db.NameMap;
            map["#Base"] = StorageParentPath;
            var mapJson = JsonConvert.SerializeObject(Db.NameMap, _settings);
            await File.WriteAllTextAsync(mapPath, mapJson);
            if (Directory.Exists(VpDefaultPathC))
            {
                try
                {
                    await File.WriteAllTextAsync(Path.Combine(VpDefaultPathC, VoiceMapName), mapJson);
                }
                catch
                {
                    // ignored
                }
            }
            if (Directory.Exists(VpDefaultPathD))
            {
                try
                {
                    await File.WriteAllTextAsync(Path.Combine(VpDefaultPathD, VoiceMapName), mapJson);
                }
                catch
                {
                    // ignored
                }
            }
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

        public async Task CollectVoices(string storagePath, bool clear = true)
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
                        var info = new VoiceInfo() {BasePath = storagePath, Package = ppkg, PpkgPath = ppkgFile};
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

            return;
        }

        public async Task<bool> BuildVirtualStorage(string storagePath, IProgress<double>? progress = null)
        {
            progress?.Report(0);

            string FindPpkgPath(string narratorName)
            {
                foreach (var voiceInfo in Db.Voices)
                {
                    if (voiceInfo.Package.narrator_list.Any(narrator => narrator.ndc_name == narratorName))
                    {
                        return voiceInfo.PpkgPath;
                    }

                    if (voiceInfo.Package.prod_name == narratorName)
                    {
                        return voiceInfo.PpkgPath;
                    }
                }

                return string.Empty;
            }

            if (!Directory.Exists(storagePath))
            {
                return false;
            }

            await CollectVoices(storagePath);
            if (Db.NarratorComponents.Count == 0)
            {
                return false;
            }

            double currentProgress = 20;
            progress?.Report(currentProgress);

            var sylapacks = Directory.GetFiles(storagePath, "*.sylapack", SearchOption.TopDirectoryOnly);

            var basePath = DefaultBasePath;
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var delta = 80.0 / sylapacks.Length;
            foreach (var kv in Db.NarratorComponents)
            {
                var narratorName = kv.Key;
                try
                {
                    foreach (var component in kv.Value)
                    {
                        if (string.IsNullOrWhiteSpace(component))
                        {
                            continue;
                        }

                        var narratorPath = Path.Combine(basePath, component, "storage");
                        if (!Directory.Exists(narratorPath))
                        {
                            Directory.CreateDirectory(narratorPath);
                        }

                        var oriIdcPath = Path.Combine(storagePath, $"{component}.idc");
                        if (File.Exists(oriIdcPath))
                        {
                            File.Copy(oriIdcPath, Path.Combine(narratorPath, $"{component}.idc"), true);
                        }

                        var oriComponentPath = Path.Combine(storagePath, $"{component}.sylapack");
                        if (File.Exists(oriComponentPath))
                        {
                            File.Copy(oriComponentPath, Path.Combine(narratorPath, $"{component}.sylapack"), true);
                        }

                        var oriPpkgPath = FindPpkgPath(narratorName);
                        if (File.Exists(oriPpkgPath))
                        {
                            File.Copy(oriPpkgPath, Path.Combine(narratorPath, Path.GetFileName(oriPpkgPath)), true);
                        }

                        foreach (var sylapack in sylapacks)
                        {
                            var dstFilePath = Path.Combine(narratorPath, Path.GetFileName(sylapack));
                            Helper.CreateHardLink(dstFilePath, sylapack, IntPtr.Zero);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }

                progress?.Report(currentProgress += delta);
            }

            progress?.Report(100);
            return true;
        }

        public static void DeleteVirtualStorage(string storageParentPath)
        {
            var basePath = Path.Combine(storageParentPath, BaseName);
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }
        }
    }
}