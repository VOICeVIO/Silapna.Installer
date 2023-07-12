using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Silapna.Models;

namespace Silapna.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private VoicePack? _currentVoicePack;
    private string _hintText = "Drag vppk here...";
    private string? _vpPath;
    private string? _storagePath;

    public VoicePack? CurrentVoicePack
    {
        get => _currentVoicePack;
        set => SetProperty(ref _currentVoicePack, value);
    }

    public string HintText
    {
        get => _hintText;
        set => SetProperty(ref _hintText, value);
    }

    public string? VpPath
    {
        get => _vpPath;
        set => SetProperty(ref _vpPath, value);
    }

    public string? StoragePath
    {
        get => _storagePath;
        set => SetProperty(ref _storagePath, value);
    }

    bool CanLoadVoices(object p)
    {
        return Directory.Exists(StoragePath);
    }

    public void LoadVoices()
    {

    }

    private bool CheckIsFreePack(ZipArchive archive)
    {
        bool hasPpkg = false;
        bool hasIdc = false;
        bool hasSylapack = false;

        foreach (var entry in archive.Entries)
        {
            var n = entry.FullName;
            if (n == "voice.ppkg")
            {
                hasPpkg = true;
            }
            else if (entry.Name.EndsWith(".sylapack"))
            {
                if (!entry.FullName.StartsWith("sylapack"))
                {
                    return false;
                }
                hasSylapack = true;
            }
            else if (entry.Name.EndsWith(".idc"))
            {
                if (!entry.FullName.StartsWith("sylapack"))
                {
                    return false;
                }
                hasIdc = true;
            }
        }

        return hasPpkg && hasIdc && hasSylapack;
    }

    private async Task ConvertToFreePack(ZipArchive archive)
    {

    }

    public async Task Repack(string path)
    {
        if (!File.Exists(path))
        {
            HintText = "[Repack] File not found";
            return;
        }

        using var fs = File.OpenRead(path);
        var header = new byte[4];
        var c = fs.Read(header);
        if (c < 4)
        {
            HintText = "[Repack] File is not valid";
            return;
        }

        bool isVppk = true;
        var newPath = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + ".silapna.vppk");
        
        if (header[0] == 'P' && header[1] == 'K')
        {
            fs.Seek(0, SeekOrigin.Begin);
            isVppk = false;
        }
        try
        {
            var archive = new ZipArchive(fs);
            if (CheckIsFreePack(archive))
            {
                if (!isVppk)
                {
                    
                }
            }
        }
        catch (Exception e)
        {
            HintText = "[Repack] error: " + e.Message;
        }

    }
}
