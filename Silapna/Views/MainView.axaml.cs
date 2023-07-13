using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Silapna.ViewModels;

namespace Silapna.Views;

public partial class MainView : UserControl
{
    private const string VP_EXE = "voicepack.exe";
    private const string DIR_D = "Dreamtonics";
    private const string DIR_VP = "VoicePack";

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var vm = (DataContext as MainViewModel)!;
        vm.Window = TopLevel.GetTopLevel(this);
        var vpPath1 = Path.Combine(AppContext.BaseDirectory, VP_EXE);
        if (File.Exists(vpPath1))
        {
            vm.VpPath = vpPath1;
        }
        
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var storagePath = Path.Combine(programDataPath, DIR_D, DIR_VP);
        var hasDir1 = Directory.Exists(storagePath);
        var hasVoice1 = false;
        if (hasDir1)
        {
            var files = Directory.GetFiles(storagePath, "*.ppkg");
            hasVoice1 = files.Length > 0;
        }

        if (hasDir1 && hasVoice1)
        {
            vm.StoragePath = storagePath;
        }
        else
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var storagePath2 = Path.Combine(appDataPath, DIR_D, DIR_VP);
            var hasDir2 = Directory.Exists(storagePath2);
            var hasVoice2 = false;
            if (hasDir2)
            {
                var files = Directory.GetFiles(storagePath2, "*.ppkg");
                hasVoice2 = files.Length > 0;
            }

            if (hasDir2 && hasVoice2)
            {
                vm.StoragePath = storagePath2;
            }
            else if (hasDir1)
            {
                vm.StoragePath = storagePath;
            }
            else if (hasDir2)
            {
                vm.StoragePath = storagePath2;
            }
            else
            {
                vm.StoragePath = null;
                vm.HintText = "Cannot find Storage. Please set by yourself.";
            }
        }
    }


}
