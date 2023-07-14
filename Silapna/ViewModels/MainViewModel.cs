using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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

    private double _progressValue = 0;

    public TopLevel? Window { get; set; }

    public double ProgressValue
    {
        get => _progressValue;
        set => Dispatcher.UIThread.Invoke(() => SetProperty(ref _progressValue, value));
    }

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
        ProgressValue = 20;
        var zipStream = new MemoryStream();
        using var newArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true, Encoding.UTF8);
        bool hasEula = false;
        var deltaPerEntry = 50.0 / archive.Entries.Count;
        foreach (var entry in archive.Entries.OrderBy(e => e.CompressedLength))
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
            else if (entry.Name.EndsWith(".txt")) //entry.Name.EndsWith(".json")
            { 
                var newEntry = newArchive.CreateEntry(entry.Name);
                await using var stream = newEntry.Open();
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(stream);
            }
            ProgressValue += deltaPerEntry;
        }

        if (!hasEula)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(_defaultEulaEn));
            var newEntry = newArchive.CreateEntry("eula_en.txt", CompressionLevel.NoCompression);
            await using var stream = newEntry.Open();
            ms.WriteTo(stream);
            stream.Close();
            newEntry = newArchive.CreateEntry("eula_jp.txt", CompressionLevel.NoCompression);
            await using var stream2 = newEntry.Open();
            ms.WriteTo(stream2);
            stream2.Close();
            newEntry = newArchive.CreateEntry("eula_cn.txt", CompressionLevel.NoCompression);
            await using var stream3 = newEntry.Open();
            ms.WriteTo(stream3);
            stream3.Close();
            newEntry = newArchive.CreateEntry("eula_zh.txt", CompressionLevel.NoCompression);
            await using var stream4 = newEntry.Open();
            ms.WriteTo(stream4);
            stream4.Close();
        }
        ProgressValue = 80;
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

        await Task.Run((async () => await Repack(CurrentVoicePack.Path)));
        //await Repack(CurrentVoicePack.Path).ConfigureAwait(false);
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

        await Task.Run((async () => await Install(CurrentVoicePack.Path, StoragePath)));
        //await Install(CurrentVoicePack.Path, StoragePath);
    }

    public async Task Install(string path, string storagePath)
    {
        ProgressValue = 0;

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

        async Task<Ppkg?> ReadPpkg(ZipArchiveEntry entry)
        {
            try
            {
                await using var entryStream = entry.Open();
                using var contentMs = new MemoryStream();
                await entryStream.CopyToAsync(contentMs);
                //contentMs json deserialize
                var content = contentMs.ToArray();
                var ppkg = JsonConvert.DeserializeObject<Ppkg>(Encoding.UTF8.GetString(content));
                return ppkg;
            }
            catch (Exception e)
            {
            }

            return null;
        }
            
        if (!File.Exists(path))
        {
            HintText = "[Install] File not found";
            return;
        }

        if (string.IsNullOrWhiteSpace(StoragePath) || !Directory.Exists(StoragePath))
        {
            HintText = "[Install] storage path not found";
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

        List<string> installedProducts = new List<string>();
        foreach (var ppkgFile in Directory.GetFiles(StoragePath, "*.ppkg"))
        {
            try
            {
                var ppkg = JsonConvert.DeserializeObject<Ppkg>(await File.ReadAllTextAsync(ppkgFile));
                if(ppkg != null)
                    installedProducts.Add(ppkg.prod_name);
            }
            catch
            {
                //ignored
            }
        }

        try
        {
            using var mms = new MemoryStream((int)fs.Length);
            await fs.CopyToAsync(mms);
            ProgressValue = 10;
            var archive = new ZipArchive(mms, ZipArchiveMode.Read);

            //find all ppkg file in archive
            foreach (var ppkgEntry in archive.Entries.Where(e => e.Name.ToLowerInvariant().EndsWith(".ppkg")))
            {
                var p = await ReadPpkg(ppkgEntry);
                if (p != null && installedProducts.Contains(p.prod_name))
                {
                    throw new Exception($"Voice {p.prod_name} already installed");
                }
            }

            var ppkgName = string.Empty;
            var deltaPerEntry = 90.0 / archive.Entries.Count;
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

                ProgressValue += deltaPerEntry;
            }
            
            HintText = $"[Install] Saved to {ppkgName}";
            ProgressValue = 100;

            var result = Helper
                .ShowMessageBoxStandardIconAsDialog("Install Success", $"Voice installed: {Environment.NewLine}{ppkgName}", Window as Window,
                    ButtonEnum.Ok, Icon.Success);
        }
        catch (Exception e)
        {
            HintText = "[Install] error: " + e.Message;
            ProgressValue = 0;
        }
    }
    
    public async Task Repack(string path)
    {
        ProgressValue = 0;

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
            ProgressValue = 10;
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
                z.Close();
                HintText = $"[Repack] Saved to {newPath}";
            }

            ProgressValue = 100;

            var result = Helper
                .ShowMessageBoxStandardIconAsDialog("Repack Success", $"Voice repacked: {Environment.NewLine}{newPath}", Window as Window,
                    ButtonEnum.Ok, Icon.Success);
        }
        catch (Exception e)
        {
            HintText = "[Repack] error: " + e.Message;
            ProgressValue = 0;
        }

    }
}
