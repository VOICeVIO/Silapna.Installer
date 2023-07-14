using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using Silapna.Models;

namespace Silapna.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const string DRAG_VPPK = "Drag vppk here...";
    private VoicePack? _currentVoicePack;
    private string _hintText = DRAG_VPPK;
    private string? _vpPath;
    private string? _storagePath;
    private readonly byte[] _vppkHeader = { (byte)'V', (byte)'p', (byte)'p', (byte)'k', 0, 0, 0, 0 };

    private readonly string _defaultEulaEn = """
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
                ButtonEnum.Ok, Icon.Error);

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

    private async Task<MemoryStream> ConvertToFreePack(ZipArchive archive)
    {
        var zipStream = new MemoryStream();
        var newArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
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
            stream.Close();
            newEntry = newArchive.CreateEntry("eula_jp.txt");
            await using var stream2 = newEntry.Open();
            ms.WriteTo(stream2);
            stream2.Close();
            newEntry = newArchive.CreateEntry("eula_cn.txt");
            await using var stream3 = newEntry.Open();
            ms.WriteTo(stream3);
            stream3.Close();
            newEntry = newArchive.CreateEntry("eula_zh.txt");
            await using var stream4 = newEntry.Open();
            ms.WriteTo(stream4);
            stream4.Close();
        }

        return zipStream;
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

    public async Task RepackCurrent()
    {
        if (CurrentVoicePack == null || !File.Exists(CurrentVoicePack.Path))
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Repack Failed", "Cannot find vppk. Please drag vppk to this window.",
                    ButtonEnum.Ok, Icon.Error);

            var result = await box.ShowAsync();
            return;
        }
        
        await Repack(CurrentVoicePack.Path);
    }

    [DependsOn(nameof(CurrentVoicePack))]
    public bool CanRepackCurrent(object p)
    {
        return CurrentVoicePack != null && File.Exists(CurrentVoicePack.Path);
    }

    [DependsOn(nameof(StoragePath))]
    [DependsOn(nameof(CurrentVoicePack))]
    public bool CanInstallCurrent(object p)
    {
        return CurrentVoicePack != null && File.Exists(CurrentVoicePack.Path) &&
               !string.IsNullOrWhiteSpace(StoragePath) && Directory.Exists(StoragePath);
    }

    public async Task InstallCurrent()
    {
        if (CurrentVoicePack == null || !File.Exists(CurrentVoicePack.Path))
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Install Failed", "Cannot find vppk. Please drag vppk to this window.",
                    ButtonEnum.Ok, Icon.Error);

            var result = await box.ShowAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(StoragePath) || !Directory.Exists(StoragePath))
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Install Failed", "Cannot find storage folder. Please select storage folder.",
                    ButtonEnum.Ok, Icon.Error);

            var result = await box.ShowAsync();
            return;
        }

        await Install(CurrentVoicePack.Path, StoragePath);
    }

    public async Task Install(string path, string storagePath)
    {
        async Task<bool> CopyFile(string dstPath, ZipArchiveEntry? entry)
        {
            if (entry == null)
            {
                return false;
            }

            try
            {
                if (File.Exists(dstPath))
                {
                    File.Delete(dstPath);
                }

                await using var stream = entry.Open();
                await using var fs = File.OpenWrite(dstPath);
                await stream.CopyToAsync(fs);
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
            
        if (!File.Exists(path))
        {
            HintText = "[Install] File not found";
            return;
        }

        await using var fs = File.OpenRead(path);
        var header = new byte[8];
        var c = fs.Read(header);
        if (c < 8)
        {
            HintText = "[Install] File is not valid";
            return;
        }
            
        bool isVppk = true;
        if (header[0] == 'P' && header[1] == 'K')
        {
            fs.Seek(0, SeekOrigin.Begin);
            isVppk = false;
        }
        
        try
        {
            using var mms = new MemoryStream((int)fs.Length);
            await fs.CopyToAsync(mms);
            var archive = new ZipArchive(mms, ZipArchiveMode.Read);
            var ppkgName = string.Empty;
            foreach (var entry in archive.Entries)
            {
                var name = entry.Name;
                if (name.EndsWith(".ppkg"))
                {
                    await using var entryStream = entry.Open();
                    using var contentMs = new MemoryStream();
                    await entryStream.CopyToAsync(contentMs);
                    //contentMs json deserialize
                    var content = contentMs.ToArray();
                    var ppkg = JsonConvert.DeserializeObject<Ppkg>(Encoding.UTF8.GetString(content));

                    if (ppkg == null)
                    {
                        throw new FormatException($"Cannot deserialize ppkg: {name}");
                    }
                    
                    var ppkgPath = Path.Combine(storagePath, $"{ppkg.prod_name}.ppkg");
                    ppkgName = ppkgPath;
                    try
                    {
                        await File.WriteAllBytesAsync(ppkgPath, content);
                    }
                    catch (Exception e)
                    {
                        throw new IOException($"Failed to install {ppkgPath}");
                    }
                }
                else if (name.EndsWith(".sylapack"))
                {
                    var sylapackPath = Path.Combine(storagePath, name);
                    var r = await CopyFile(sylapackPath, entry);
                    if (!r)
                    {
                        throw new IOException($"Failed to install {sylapackPath}");
                    }
                }
                else if (name.EndsWith(".idc"))
                {
                    var idcPath = Path.Combine(storagePath, name);
                    var r = await CopyFile(idcPath, entry);
                    if (!r)
                    {
                        try
                        {
                            File.Delete(idcPath);
                        }
                        catch
                        {
                            //ignore
                        }
                        
                        throw new IOException($"Failed to install {idcPath}");
                    }
                }
                else if (name.EndsWith(".json"))
                {
                    var filePath = Path.Combine(storagePath, name);
                    var r = await CopyFile(filePath, entry);
                    if (!r)
                    {
                        throw new IOException($"Failed to install {filePath}");
                    }
                }
            }
            
            HintText = $"[Install] Saved to {ppkgName}";
            var box = Helper
                .GetMessageBoxStandardIcon("Install Success", $"Voice installed: {Environment.NewLine}{ppkgName}",
                    ButtonEnum.Ok, Icon.Success);

            var result = await box.ShowAsync();
        }
        catch (Exception e)
        {
            HintText = "[Install] error: " + e.Message;
        }
    }
    
    public async Task Repack(string path)
    {
        if (!File.Exists(path))
        {
            HintText = "[Repack] File not found";
            return;
        }

        await using var fs = File.OpenRead(path);
        var header = new byte[8];
        var c = fs.Read(header);
        if (c < 8)
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
            using var mms = new MemoryStream((int)fs.Length);
            await fs.CopyToAsync(mms);
            var archive = new ZipArchive(mms, ZipArchiveMode.Read);
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
            else //not free pack
            {
                var z = await ConvertToFreePack(archive);
                await using var newFs = File.OpenWrite(newPath);
                newFs.Write(_vppkHeader);
                z.WriteTo(newFs);
                await newFs.FlushAsync();
                HintText = $"[Repack] Saved to {newPath}";
            }
            
            var box = Helper
                .GetMessageBoxStandardIcon("Repack Success", $"Voice repacked: {Environment.NewLine}{newPath}",
                    ButtonEnum.Ok, Icon.Success);
            
            var result = await box.ShowWindowDialogAsync(Window as Window);
        }
        catch (Exception e)
        {
            HintText = "[Repack] error: " + e.Message;
        }

    }
}
