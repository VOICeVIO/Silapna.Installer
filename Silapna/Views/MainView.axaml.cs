using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Silapna.ViewModels;

namespace Silapna.Views;

public partial class MainView : UserControl
{
    private const string VP_EXE = "voicepack.exe";

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var vm = (DataContext as MainViewModel)!;
        var vpPath1 = Path.Combine(AppContext.BaseDirectory, VP_EXE);
        if (File.Exists(vpPath1))
        {
            vm.VpPath = vpPath1;
        }
        
    }


}
