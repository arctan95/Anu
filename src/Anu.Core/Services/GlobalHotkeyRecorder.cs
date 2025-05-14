using System;
using Anu.Core.Models;
using SharpHook;
using SharpHook.Native;

namespace Anu.Core.Services;

public class GlobalHotkeyRecorder
{
    private static volatile bool _isRecording;
    private static volatile Action<GlobalHotkey>? _recordCallback;

    public static void StartRecording(Action<GlobalHotkey> callback)
    {
        _isRecording = true;
        _recordCallback = callback;
    }

    public static void StopRecording()
    {
        _isRecording = false;
        _recordCallback = null;
    }

    public static void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!_isRecording)
            return;

        if (e.Data.KeyCode is KeyCode.VcLeftControl or KeyCode.VcRightControl or
            KeyCode.VcLeftAlt or KeyCode.VcRightAlt or
            KeyCode.VcLeftShift or KeyCode.VcRightShift or
            KeyCode.VcLeftMeta or KeyCode.VcRightMeta)
        {
            return;
        }

        var hotkey = new GlobalHotkey(e.RawEvent.Mask, e.Data.KeyCode);
        _recordCallback?.Invoke(hotkey);

    }
}