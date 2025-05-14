using System;
using SharpHook.Native;

namespace Anu.Core.Models;

/// <summary>
/// A hotkey containing a key combination (a key and modifier key) and a
/// callback function that gets called if the right keys are down. Also contains a
/// boolean stating if the callback method can be called or not.
/// </summary>
public class GlobalHotkey
{
    /// <summary>
    /// The modifier key required to be pressed for the hotkey to be 
    /// </summary>
    public ModifierMask Modifier { get; set; }

    /// <summary>
    /// The key required to be pressed for the hotkey to be fired
    /// </summary>
    public KeyCode Key { get; set; }

    // You could change this to a list of actions if you want
    // multiple things to be fired when the hotkey fires.
    /// <summary>
    /// The method to be called when the hotkey is pressed
    /// </summary>
    public Action? Callback { get; set; }

    /// <summary>
    /// States if the method can be executed.
    /// </summary>
    public bool CanExecute { get; set; }

    /// <summary>
    /// Initiates a new hotkey with the given modifier, key, callback method, 
    /// and also a boolean stating if the callback can be run (can be changed, see <see cref="CanExecute"/>)
    /// </summary>
    /// <param name="modifier">The modifier key required to be pressed</param>
    /// <param name="key">The key required to be pressed</param>
    /// <param name="callbackMethod">The method that gets called when the hotkey is fired</param>
    /// <param name="canExecute">
    /// States whether the callback can be run 
    /// (can be changed, see <see cref="CanExecute"/>)
    /// </param>
    public GlobalHotkey(ModifierMask modifier, KeyCode key, Action? callbackMethod, bool canExecute = true)
    {
        Modifier = modifier;
        Key = key;
        Callback = callbackMethod;
        CanExecute = canExecute;
    }

    public void SetFunctionBinding(Action? callbackMethod)
    {
        Callback = callbackMethod;
        CanExecute = true;
    }

    public GlobalHotkey(ModifierMask modifier, KeyCode key)
    {
        Modifier = modifier;
        Key = key;
    }
}