using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Silapna.Models
{
    public class VoicePack : ObservableObject
    {
        private string? _path;
        private Ppkg? _packageInfo;

        public string? Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public string? ProductName => PackageInfo?.prod_name;

        public Ppkg? PackageInfo
        {
            get => _packageInfo;
            set
            {
                if (SetProperty(ref _packageInfo, value)) OnPropertyChanged(nameof(ProductName));
            }
        }
    }

    [Serializable]
    public class Ppkg
    {
        public string prod_name { get; set; }
        public string vendor_name { get; set; }
        public Eula[]? eula_list { get; set; }
        public Narrator[] narrator_list { get; set; }

        [JsonConstructor]
        public Ppkg() { }
    }

    [Serializable]
    public class Eula
    {
        public string language { get; set; }
        public string url { get; set; }

        [JsonConstructor]
        public Eula() { }
    }

    [Serializable]
    public class Narrator
    {
        public string ndc_name { get; set; }
        public int latest_version { get; set; }

        [JsonConstructor]
        public Narrator() { }
    }

    [Serializable]
    public class EntranceComponent
    {
        public string narrator_id { get; set; }
        public string entrance_component { get; set; }

        [JsonConstructor]
        public EntranceComponent() { }
    }

}
