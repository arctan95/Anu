using System;
using System.Collections.Generic;

namespace Anu.Core.Services;

public static class FunctionRegistry
{
    public static readonly Dictionary<string, Action?> FunctionBindings;

    static FunctionRegistry()
    {
        FunctionBindings = new Dictionary<string, Action?>();
    }

    public static void RegisterFunction(string functionName, Action? function)
    {
        FunctionBindings[functionName] = function;
    }

    public static Action? GetFunction(string functionName)
    {
        return FunctionBindings[functionName];
    }
}