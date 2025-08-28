using System.Diagnostics;
using Anu.Core.Services;
using Microsoft.Win32;

namespace Anu.Desktop.Windows.Services;

public class AutostartManager : IAutostartManager
{

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static readonly string AppName = "Anu";
    public void EnableAutoStart()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath != null)
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true)!;
            key.SetValue(AppName, exePath);
        }
    }

    public void DisableAutoStart()
    {
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true)!;
        if (key.GetValue(AppName) != null)
            key.DeleteValue(AppName);
    }
}