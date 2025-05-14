using System;
using System.Collections.Generic;

namespace Anu.Core.Services;

public static class FunctionRegistry
{
    public static readonly Dictionary<string, Action?> _functionBindings;

    static FunctionRegistry()
    {
        _functionBindings = new Dictionary<string, Action?>();
    }

    public static void RegisterFunction(string functionName, Action? function)
    {
        _functionBindings[functionName] = function;
    }

    public static Action? GetFunction(string functionName)
    {
        return _functionBindings[functionName];
    }
}