namespace Silapna.Models
{
    public class VoicePack
    {
        public string Path { get; set; }
        public string? ProductName => PackageInfo?.prod_name;
        public Ppkg? PackageInfo { get; set; }
    }


    public class Ppkg
    {
        public string prod_name { get; set; }
        public string vendor_name { get; set; }
        public EulaList[]? eula_list { get; set; }
        public NarratorList[] narrator_list { get; set; }
    }

    public class EulaList
    {
        public string language { get; set; }
        public string url { get; set; }
    }

    public class NarratorList
    {
        public string ndc_name { get; set; }
        public int latest_version { get; set; }
    }

}
