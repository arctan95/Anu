using System;
using System.Collections.Generic;
using Anu.Core.Models;
using SharpHook;
using SharpHook.Native;

namespace Anu.Core.Services;

/// <summary>
/// A class for adding/removing global hotkeys to and from your application, 
/// meaning these hotkeys can be run even if your application isn't focused.
/// </summary>
public static class GlobalHotkeyManager
{
    
    // Events
    public delegate void HotkeyEvent(GlobalHotkey hotkey);

    /// <summary>
    /// Fired when a hotkey is fired (duh lol).
    /// </summary>
    public static event HotkeyEvent? HotkeyFired;


    // All the Hotkeys
    private static readonly Dictionary<string, GlobalHotkey> _hotkeyBindings;

    /// <summary>
    /// States whether the system hotkey is registered. 
    /// </summary>
    public static bool IsSystemHotKeyRegistered { get; private set; }

    /// <summary>
    /// States whether hotkeys require modifier keys to be scanned (and therefore
    /// have a chance for their callback method to be called). If this is disabled,
    /// the hotkeys will be checked in every key stroke/event, so pressing just 'A' could 
    /// fire a hotkey if there is one with no modifiers and just it's key set to 'A'. 
    /// <para>
    /// If enabled, a modifier key is required on hotkeys. if the hotkey
    /// has no modifiers, then it simply wont be scanned at all.
    /// </para>
    /// </summary>
    public static bool RequiresModifierKey { get; set; }

    static GlobalHotkeyManager()
    {
        _hotkeyBindings = new();
        RequiresModifierKey = false;
    }

    /// <summary>
    /// Adds a hotkey to the hotkeys list.
    /// </summary>
    public static void BindHotkey(string functionName, GlobalHotkey hotkey)
    {
        _hotkeyBindings[functionName] = hotkey;
    }

    /// <summary>
    /// Removes a hotkey from the hotkeys list.
    /// </summary>
    /// <param name="functionName">The name of the hotkey function</param>
    /// <param name="hotkey"></param>
    public static void RemoveHotkey(string functionName, GlobalHotkey hotkey)
    {
        _hotkeyBindings.Remove(functionName);
    }

    /// <summary>
    /// Checks if there are any modifiers are pressed. If so, it checks through every
    /// Hotkey and matches their Modifier/Key. If they both match, and the hotkey allows
    /// the callback method to be called, it is called.
    /// </summary>
    public static void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        foreach (var hotkey in _hotkeyBindings.Values)
        {
            if (RequiresModifierKey && hotkey.Modifier == ModifierMask.None)
                continue;

            if (e.RawEvent.Mask.HasFlag(hotkey.Modifier) && e.Data.KeyCode == hotkey.Key)
            {
                if (hotkey.CanExecute)
                {
                    hotkey.Callback?.Invoke();
                    HotkeyFired?.Invoke(hotkey);
                }
            }
        }
    }
    

    /// <summary>
    /// Creates and adds a new hotkey to the hotkeys list.
    /// </summary>
    /// <param name="functionName">The name of the hotkey function</param>
    /// <param name="modifier">The modifier key. ALT Does not work.</param>
    /// <param name="key"></param>
    /// <param name="callback"></param>
    /// <param name="canExecute"></param>
    public static void BindHotkey(string functionName, ModifierMask modifier, KeyCode key, Action? callback, bool canExecute = true)
    {
        BindHotkey(functionName, new GlobalHotkey(modifier, key, callback, canExecute));
    }
    
}