using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

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


    public class Ppkg
    {
        public string prod_name { get; set; }
        public string vendor_name { get; set; }
        public EulaList[]? eula_list { get; set; }
        public NarratorList[] narrator_list { get; set; }
        
        [JsonConstructor]
        public Ppkg(){}
    }

    public class EulaList
    {
        public string language { get; set; }
        public string url { get; set; }
        
        [JsonConstructor]
        public EulaList() {}
    }

    public class NarratorList
    {
        public string ndc_name { get; set; }
        public int latest_version { get; set; }
        
        [JsonConstructor]
        public NarratorList(){}
    }

}
