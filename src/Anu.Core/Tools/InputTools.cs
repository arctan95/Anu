using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using SharpHook;
using SharpHook.Native;

namespace Anu.Core.Tools;

[McpServerToolType]
public static class InputTools
{
    private static readonly EventSimulator Simulator = new();

    [McpServerTool, Description("Simulate a key press and release.")]
    public static Task PressKey(
        [Description("The key code to press (SharpHook KeyCode)")]
        KeyCode key)
    {
        Simulator.SimulateKeyPress(key);
        Simulator.SimulateKeyRelease(key);
        return Task.CompletedTask;
    }

    [McpServerTool, Description("Simulate a key combination (modifier + key).")]
    public static Task PressKeyCombination(
        [Description("Modifier key (e.g., VcLeftControl)")]
        KeyCode modifier,
        [Description("Main key to press (e.g., VcC)")]
        KeyCode key)
    {
        Simulator.SimulateKeyPress(modifier);
        Simulator.SimulateKeyPress(key);
        Simulator.SimulateKeyRelease(key);
        Simulator.SimulateKeyRelease(modifier);
        return Task.CompletedTask;
    }

    [McpServerTool, Description("Simulate mouse click at the current pointer location.")]
    public static Task ClickMouse([Description("Mouse button")] MouseButton button)
    {
        Simulator.SimulateMousePress(button);
        Simulator.SimulateMouseRelease(button);
        return Task.CompletedTask;
    }

    [McpServerTool, Description("Move mouse pointer to absolute coordinates.")]
    public static Task MoveMouse(
        [Description("X coordinate")] short x,
        [Description("Y coordinate")] short y)
    {
        Simulator.SimulateMouseMovement(x, y);
        return Task.CompletedTask;
    }

    [McpServerTool, Description("Move mouse pointer relative to current position.")]
    public static Task MoveMouseRelative(
        [Description("Delta X")] short dx,
        [Description("Delta Y")] short dy)
    {
        Simulator.SimulateMouseMovementRelative(dx, dy);
        return Task.CompletedTask;
    }

    [McpServerTool, Description("Scroll mouse wheel.")]
    public static Task ScrollMouse(
        [Description("Rotation amount")] short rotation,
        [Description("Vertical or horizontal")]
        MouseWheelScrollDirection direction)
    {
        Simulator.SimulateMouseWheel(rotation, direction, MouseWheelScrollType.UnitScroll);
        return Task.CompletedTask;
    }
}