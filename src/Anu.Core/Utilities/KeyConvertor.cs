using System.Collections.Generic;
using Avalonia.Input;
using SharpHook.Native;

namespace Anu.Core.Utilities;

public static class KeyConvertor
{
    public static KeyModifiers ToKeyModifier(ModifierMask mask)
    {
        KeyModifiers result = KeyModifiers.None;

        if ((mask & ModifierMask.Ctrl) != 0)
            result |= KeyModifiers.Control;

        if ((mask & ModifierMask.Shift) != 0)
            result |= KeyModifiers.Shift;

        if ((mask & ModifierMask.Alt) != 0)
            result |= KeyModifiers.Alt;

        if ((mask & ModifierMask.Meta) != 0)
            result |= KeyModifiers.Meta;

        return result;
    }

    public static Key ToKey(KeyCode keyCode)
    {
        return KeyCodeMap.Map.GetValueOrDefault(keyCode, Key.None);
    }
}