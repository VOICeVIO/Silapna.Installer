using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Silapna.Models;

namespace Silapna.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const string DRAG_VPPK = "Drag vppk here...";
    private VoicePack? _currentVoicePack;
    private string _hintText = DRAG_VPPK;
    private string? _vpPath;
    private string? _storagePath;
    private byte[] _vppkHeader = { (byte)'V', (byte)'p', (byte)'p', (byte)'k', 0, 0, 0, 0 };

    private string _defaultEulaEn = """
        ### Repacked by Silapna
        Silapna is a free tool made by VOICeVIO, wdwxy12345@gmail.com
        """;

    public TopLevel? Window { get; set; }

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

    public async Task LoadVoices()
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard("Sorry", "Cannot find VP location.",
                ButtonEnum.Ok);

        var result = await box.ShowAsync();
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

    private async Task<ZipArchive> ConvertToFreePack(ZipArchive archive)
    {
        var newArchive = new ZipArchive(new MemoryStream(), ZipArchiveMode.Create, true);
        bool hasEula = false;
        foreach (var entry in archive.Entries)
        {
            var n = entry.FullName;
            if (n.EndsWith(".ppkg"))
            {
                var newEntry = newArchive.CreateEntry("voice.ppkg");
                await using var stream = newEntry.Open();
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(stream);
            }
            else if (entry.Name.EndsWith(".sylapack"))
            {
                var newEntry = newArchive.CreateEntry("sylapack/" + entry.Name);
                await using var stream = newEntry.Open();
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(stream);
            }
            else if (entry.Name.EndsWith(".idc"))
            {
                var newEntry = newArchive.CreateEntry("sylapack/" + entry.Name);
                await using var stream = newEntry.Open();
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(stream);
            }
            else if (entry.Name.StartsWith("eula_") && entry.Name.EndsWith(".txt"))
            { 
                var newEntry = newArchive.CreateEntry(entry.Name);
                await using var stream = newEntry.Open();
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(stream);
                hasEula = true;
            }
            else if (entry.Name.EndsWith(".json") || entry.Name.EndsWith(".txt"))
            { 
                var newEntry = newArchive.CreateEntry(entry.Name);
                await using var stream = newEntry.Open();
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(stream);
            }
        }

        if (!hasEula)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(_defaultEulaEn));
            var newEntry = newArchive.CreateEntry("eula_en.txt");
            await using var stream = newEntry.Open();
            ms.WriteTo(stream);
            newEntry = newArchive.CreateEntry("eula_jp.txt");
            await using var stream2 = newEntry.Open();
            ms.WriteTo(stream2);
            newEntry = newArchive.CreateEntry("eula_cn.txt");
            await using var stream3 = newEntry.Open();
            ms.WriteTo(stream3);
        }

        return newArchive;
    }

    public async Task OpenStorageDialog()
    {
        if (Window == null)
        {
            return;
        }
        var result = await Window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            { AllowMultiple = false, Title = "Select storage folder" });
        if (result.Count == 0)
        {
            return;
        }

        var path = result[0].Path;
        if (Directory.Exists(path.LocalPath))
        {
            StoragePath = path.LocalPath;
            HintText = DRAG_VPPK;
        }
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
                    //Append vppk header
                    await using var newFs = File.OpenWrite(newPath);
                    newFs.Write(_vppkHeader);
                    await fs.CopyToAsync(newFs);
                    HintText = $"[Repack] Saved to {newPath}";
                }
            }
            else
            {
                
            }
        }
        catch (Exception e)
        {
            HintText = "[Repack] error: " + e.Message;
        }

    }
}
