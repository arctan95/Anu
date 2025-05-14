using Anu.Core.Models;
using Anu.Core.Services;

namespace Anu.Desktop.Windows.Services;

public class SystemHotkeyRegister: ISystemHotKeyRegister
{
    public bool RegisterHotkey(GlobalHotkey hotkey)
    {
        return true;
    }
}