using Anu.Core.Models;

namespace Anu.Core.Services;

public interface ISystemHotKeyRegister
{
    bool RegisterHotkey(GlobalHotkey hotkey);
}