using System.IO;
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
}
