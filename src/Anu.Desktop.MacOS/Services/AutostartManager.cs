using System.Runtime.Versioning;
using Anu.Core.Services;
using ServiceManagement;

namespace Anu.Desktop.MacOS.Services;

public class AutostartManager : IAutostartManager
{
    [SupportedOSPlatform("macos13.0")]
    public void EnableAutoStart()
    {
        SMAppService.MainApp.Register();
    }

    [SupportedOSPlatform("macos13.0")]
    public void DisableAutoStart()
    {
        SMAppService.MainApp.Unregister();
    }

}