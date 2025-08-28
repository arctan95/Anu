using System;
using System.Threading.Tasks;
using Anu.Core.Services;
using Anu.Core.ViewModels;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using SharpHook;
using SharpHook.Native;

namespace Anu.Core.Tools;

public static class InputTools
{
    private static readonly EventSimulator Simulator = new();
    private static readonly ChatWindowViewModel? ChatWindowViewModel = ServiceProviderBuilder.ServiceProvider?.GetRequiredService<ChatWindowViewModel>();

    public static readonly ChatTool TakeScreenshotTool = ChatTool.CreateFunctionTool(
        functionName: nameof(TakeScreenshot),
        functionDescription: "Take a screenshot of the screen."
    );

    public static readonly ChatTool GetCurrentCursorPositionTool = ChatTool.CreateFunctionTool(
        functionName: nameof(GetCurrentCursorPosition),
        functionDescription: "Get the current cursor position"
    );

    public static readonly ChatTool GetScreenSizeTool = ChatTool.CreateFunctionTool(
        functionName: nameof(GetScreenSize),
        functionDescription: "Get the screen size"
    );

    public static readonly ChatTool InputTextTool = ChatTool.CreateFunctionTool(
        functionName: nameof(InputText),
        functionDescription: "Simulate text input. Prefer using this method for entering text instead of press keys.",
        functionParameters: BinaryData.FromString("""
          {
              "type": "object",
              "properties": {
                  "text": { "type": "string", "description": "The text to input" }
                },
              "required": ["text"]
          }
          """)
    );


    public static readonly ChatTool GetAllKeyNamesTool = ChatTool.CreateFunctionTool(
        functionName: nameof(GetAllKeyNames),
        functionDescription: "Get all available key names from SharpHook KeyCode"
    );

    public static readonly ChatTool LongPressKeyTool = ChatTool.CreateFunctionTool(
        functionName: nameof(LongPressKey),
        functionDescription: "Simulate a key press and release after a specified duration (long press)",
        functionParameters: BinaryData.FromString("""
          {
              "type": "object",
              "properties": {
                  "key": {
                      "type": "string",
                      "description": "The key code to press (Use GetAllKeyNames to get SharpHook KeyCode)"
                  },
                  "duration_ms": {
                      "type": "integer",
                      "description": "The duration in milliseconds to hold the key before releasing"
                  }
                },
              "required": ["key", "duration_ms"]
          }
        """)
    );

    public static readonly ChatTool PressKeyTool = ChatTool.CreateFunctionTool(
        functionName: nameof(PressKey),
        functionDescription: "Simulate a key press and release",
        functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "key": {
                        "type": "string",
                        "description": "The key code to press (Use GetAllKeyNames to get SharpHook KeyCode)"
                            }
                        },
                "required": ["key"]
            }
        """)
    );

    public static readonly ChatTool PressKeyCombinationTool = ChatTool.CreateFunctionTool(
        functionName: nameof(PressKeyCombination),
        functionDescription: "Simulate a key combination (modifier + key)",
        functionParameters: BinaryData.FromString("""
        {
            "type": "object",
            "properties": {
                "modifier": { "type": "string", "description": "Modifier key (Use GetAllKeyNames to get SharpHook KeyCode)" },
                        "key": { "type": "string", "description": "Main key to press (Use GetAllKeyNames to get SharpHook KeyCode)" }
                },
            "required": ["modifier","key"]
        }
        """)
    );

    public static readonly ChatTool DoubleClickTool = ChatTool.CreateFunctionTool(
        functionName: nameof(DoubleClick),
        functionDescription: "Perform a double click"
    );

    public static readonly ChatTool TripleClickTool = ChatTool.CreateFunctionTool(
        functionName: nameof(TripleClick),
        functionDescription: "Perform a triple click"
    );

    public static readonly ChatTool LeftClickTool = ChatTool.CreateFunctionTool(
        functionName: nameof(LeftClick),
        functionDescription: "Simulate left mouse click"
    );

    public static readonly ChatTool RightClickTool = ChatTool.CreateFunctionTool(
        functionName: nameof(RightClick),
        functionDescription: "Simulate right mouse click"
    );

    public static readonly ChatTool MiddleClickTool = ChatTool.CreateFunctionTool(
        functionName: nameof(MiddleClick),
        functionDescription: "Simulate middle mouse click"
    );

    public static readonly ChatTool ClickAtTool = ChatTool.CreateFunctionTool(
        functionName: nameof(ClickAt),
        functionDescription: "Simulate a mouse left-click at the given coordinates",
        functionParameters: BinaryData.FromString("""
          {
              "type": "object",
              "properties": {
                  "x": { "type": "integer", "description": "X coordinate" },
                      "y": { "type": "integer", "description": "Y coordinate" }
                  },
              "required": ["x", "y"]
          }
          """)
    );


    public static readonly ChatTool ClickAndDragTool = ChatTool.CreateFunctionTool(
        functionName: nameof(ClickAndDrag),
        functionDescription: "Click and drag the mouse from a start coordinate to an end coordinate",
        functionParameters: BinaryData.FromString("""
          {
              "type": "object",
              "properties": {
                  "startX": { "type": "integer", "description": "Starting X coordinate" },
                    "startY": { "type": "integer", "description": "Starting Y coordinate" },
                    "endX": { "type": "integer", "description": "Target X coordinate" },
                    "endY": { "type": "integer", "description": "Target Y coordinate" }
                  },
              "required": ["startX", "startY", "endX", "endY"]
          }
        """)
    );

    public static readonly ChatTool MoveMouseTool = ChatTool.CreateFunctionTool(
        functionName: nameof(MoveMouse),
        functionDescription: "Move mouse pointer to absolute coordinates",
        functionParameters: BinaryData.FromString("""
        {
            "type": "object",
            "properties": {

                    "x": { "type": "integer", "description": "X coordinate" },
                        "y": { "type": "integer", "description": "Y coordinate" }
                    },
            "required": ["x","y"]
        }
        """)
    );

    public static readonly ChatTool MoveMouseRelativeTool = ChatTool.CreateFunctionTool(
        functionName: nameof(MoveMouseRelative),
        functionDescription: "Move mouse pointer relative to current position",
        functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "dx": { "type": "integer", "description": "Delta X" },
                            "dy": { "type": "integer", "description": "Delta Y" }
                },
                "required": ["dx","dy"]
            }
        """)
    );

    public static readonly ChatTool WheelMouseTool = ChatTool.CreateFunctionTool(
        functionName: nameof(WheelMouse),
        functionDescription: "Scroll mouse wheel",
        functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                "rotation": { "type": "integer", "description": "Rotation amount" },
                        "direction": { "type": "string", "description": "Vertical or Horizontal" }
                    },
                "required": ["rotation","direction"]
            }
        """)
    );

    public static string GetCurrentCursorPosition()
    {
        if (ChatWindowViewModel != null)
        {
            return $"X:{ChatWindowViewModel.CursorPositionX}, Y:{ChatWindowViewModel.CursorPositionY}";
        }

        return "Can't get cursor position";
    }

    public static string GetScreenSize()
    {
        if (ChatWindowViewModel != null)
        {
            return $"Screen:{ChatWindowViewModel.ScreenWidth}×{ChatWindowViewModel.ScreenHeight}";
        }

        return "Can't get screen size";
    }

    public static string GetAllKeyNames()
    {
        string[] keyNames = Enum.GetNames(typeof(KeyCode));

        return string.Join(",", keyNames);
    }

    public static string InputText(string? text)
    {
        if (text != null)
        {
            Simulator.SimulateTextEntry(text);
        }
        return $"{text}: {nameof(UioHookResult.Success)}";
    }

    public static string PressKey(KeyCode key)
    {
        Simulator.SimulateKeyPress(key);
        Simulator.SimulateKeyRelease(key);
        return $"{nameof(PressKey)}: {key}: {nameof(UioHookResult.Success)}";
    }

    public static string LongPressKey(KeyCode key, int durationMs)
    {
        _ = Task.Run(async () =>
        {
            Simulator.SimulateKeyPress(key);
            await Task.Delay(durationMs);
            Simulator.SimulateKeyRelease(key);
        });
        return $"{nameof(LongPressKey)}: {key}: {nameof(UioHookResult.Success)}";
    }

    public static string PressKeyCombination(KeyCode modifier, KeyCode key)
    {
        Simulator.SimulateKeyPress(modifier);
        Simulator.SimulateKeyPress(key);
        Simulator.SimulateKeyRelease(key);
        Simulator.SimulateKeyRelease(modifier);
        return $"{nameof(PressKeyCombination)}: {modifier}-{modifier}: {nameof(UioHookResult.Success)}";
    }

    public static string DoubleClick()
    {
        LeftClick();
        LeftClick();
        return $"{nameof(DoubleClick)}: {nameof(UioHookResult.Success)}";
    }

    public static string TripleClick()
    {
        LeftClick();
        LeftClick();
        LeftClick();
        return $"{nameof(TripleClick)}: {nameof(UioHookResult.Success)}";
    }

    public static string LeftClick()
    {
        Simulator.SimulateMousePress(MouseButton.Button1);
        Simulator.SimulateMouseRelease(MouseButton.Button1);
        return $"{nameof(LeftClick)}: {nameof(UioHookResult.Success)}";
    }

    public static string RightClick()
    {
        Simulator.SimulateMousePress(MouseButton.Button2);
        Simulator.SimulateMouseRelease(MouseButton.Button2);
        return $"{nameof(RightClick)}: {nameof(UioHookResult.Success)}";
    }

    public static string MiddleClick()
    {
        Simulator.SimulateMousePress(MouseButton.Button3);
        Simulator.SimulateMouseRelease(MouseButton.Button3);
        return $"{nameof(MiddleClick)}: {nameof(UioHookResult.Success)}";
    }

    public static string ClickAndDrag(short startX, short startY, short endX, short endY)
    {
        Simulator.SimulateMousePress(startX, startY, MouseButton.Button1);
        MoveMouse(endX, endY);
        Simulator.SimulateMouseRelease(endX, endY, MouseButton.Button1);
        return $"{nameof(ClickAndDrag)}: {nameof(UioHookResult.Success)}";
    }

    public static string ClickAt(short x, short y)
    {
        Simulator.SimulateMousePress(x, y, MouseButton.Button1);
        Simulator.SimulateMouseRelease(x, y, MouseButton.Button1);
        return $"{nameof(ClickAt)}: ({x},{y}): {nameof(UioHookResult.Success)}";
    }

    public static string MoveMouse(short x, short y)
    {
        Simulator.SimulateMouseMovement(x, y);
        return $"{nameof(MoveMouse)}: ({x},{y}): {nameof(UioHookResult.Success)}";
    }

    public static string MoveMouseRelative(short dx, short dy)
    {
        Simulator.SimulateMouseMovementRelative(dx, dy);
        return $"{nameof(MoveMouseRelative)}: ({dx},{dy}): {nameof(UioHookResult.Success)}";
    }

    public static string WheelMouse(short rotation, MouseWheelScrollDirection direction)
    {
        Simulator.SimulateMouseWheel(rotation, direction);
        return $"{nameof(WheelMouse)}: ({nameof(direction)}-{rotation}): {nameof(UioHookResult.Success)}";
    }

    public static async Task<string> TakeScreenshot()
    {
        if (Application.Current is App app)
        {
            if (ChatWindowViewModel != null)
            {
                ChatWindowViewModel.ImageSource = await app.TakeScreenshotAsync();
            }
        }
        return $"{nameof(TakeScreenshot)}: {nameof(UioHookResult.Success)}";
    }
}