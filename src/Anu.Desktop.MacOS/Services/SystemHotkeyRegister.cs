using Anu.Core.Models;
using Anu.Core.Services;

namespace Anu.Desktop.MacOS.Services;

public class SystemHotkeyRegister: ISystemHotKeyRegister
{
    public bool RegisterHotkey(GlobalHotkey hotkey)
    {
        return true;
    }
}