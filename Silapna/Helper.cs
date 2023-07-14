using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Silapna
{
    internal static class Helper
    {
        private static WindowIcon? _icon;

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo() {UseShellExecute = true, FileName = url});
            }
            catch
            {
                // HACK: Process.Start throws when the default browser is not set
                // correctly on macOS. Use Mac open command as a workaround
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo() {UseShellExecute = true, FileName = "open", Arguments = url});
                }
            }
        }

        private static WindowIcon GetIcon()
        {
            if (_icon != null)
            {
                return _icon;
            }

            var bitmap = new Bitmap(AssetLoader.Open(new Uri("avares://Silapna/Assets/VP.png")));
            _icon = new WindowIcon(bitmap);
            return _icon;
        }

        public static IMsBox<ButtonResult> GetMessageBoxStandardIcon(
            string title,
            string text,
            ButtonEnum @enum = ButtonEnum.Ok,
            Icon icon = Icon.None,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen)
        {
            return GetMessageBoxStandard(title, text, GetIcon(), @enum, icon, windowStartupLocation);
        }

        public static IMsBox<ButtonResult> GetMessageBoxStandard(
            string title,
            string text,
            WindowIcon windowIcon,
            ButtonEnum @enum = ButtonEnum.Ok,
            Icon icon = Icon.None,
            WindowStartupLocation windowStartupLocation = WindowStartupLocation.CenterScreen)
        {
            MessageBoxStandardParams @params = new MessageBoxStandardParams();
            @params.ContentTitle = title;
            @params.ContentMessage = text;
            @params.ButtonDefinitions = @enum;
            @params.Icon = icon;
            @params.WindowStartupLocation = windowStartupLocation;
            @params.WindowIcon = windowIcon;
            return MessageBoxManager.GetMessageBoxStandard(@params);
        }
    }
}