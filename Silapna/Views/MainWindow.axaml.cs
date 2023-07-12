using Avalonia.Controls;
using Avalonia.Input;
using Silapna.Models;
using Silapna.ViewModels;
using System.Linq;

namespace Silapna.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DropEvent, Drop);
        this.AddHandler(DragDrop.DragOverEvent, DragOver);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Copy;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        var viewModel = (DataContext as MainViewModel)!;
        if (e.Data.Contains(DataFormats.Files))
        {
            var fileNames = e.Data.GetFiles();
            var first = fileNames?.FirstOrDefault();
            if (first == null)
            {
                return;
            }

            viewModel.CurrentVoicePack = new VoicePack() { Path = first.Path.LocalPath };
        }
    }
}
