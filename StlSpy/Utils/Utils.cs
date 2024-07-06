using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MsBox.Avalonia;

namespace StlSpy.Utils;

public static class Utils
{
    public static void OpenUrl(Uri uri) => OpenUrl(uri.AbsoluteUri);
    public static void OpenUrl(string url)
    {
        // hack because of this: https://github.com/dotnet/corefx/issues/10361
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
        else throw new Exception("No url 4 u");
    }

    public static void OpenFolder(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start("explorer.exe", "\"" + path.Replace("/", "\\") + "\""); // I love windows hacks
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", $"\"{path}\"");
    }

    public static bool OpenPrusaSlicer(List<string> paths)
    {
        try
        {
            string stringPaths = string.Join(" ", paths.Select(x => x.Contains(" ") ? $"\"{x}\"" : x));
                
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("C:/Program Files/Prusa3D/PrusaSlicer/prusa-slicer.exe", stringPaths);
            else
                Process.Start("/usr/bin/flatpak",
                    $"run --branch=stable --arch=x86_64 --command=entrypoint --file-forwarding com.prusa3d.PrusaSlicer @@ {stringPaths} @@");
            
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    
    public static bool OpenBambuStudio(List<string> paths)
    {
        try
        {
            string stringPaths = string.Join(" ", paths.Select(x => x.Contains(" ") ? $"\"{x}\"" : x));
                
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start("C:/Program Files/Bambu Studio/bambu-studio.exe", stringPaths);
            else
                Process.Start("/usr/bin/flatpak",
                    $"run --branch=stable --arch=x86_64 --command=entrypoint --file-forwarding com.bambulab.BambuStudio @@ {stringPaths} @@");
            
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static async Task ShowMessageBox(string title, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager
            .GetMessageBoxStandard(title, message);
        await messageBoxStandardWindow.ShowAsync();
    }
}