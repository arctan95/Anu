using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Avalonia.Platform;
using Anu.Core.Models;
using Anu.Core.Utilities;

namespace Anu.Core.Services;

public class AppConfigService
{
    private readonly string _configFilePath;
    private readonly JsonObject _configRoot;

    public AppConfigService()
    {
        var configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".anu");
        Directory.CreateDirectory(configDirectory);

        _configFilePath = Path.Combine(configDirectory, "settings.json");

        if (!File.Exists(_configFilePath))
        {
            using var stream = AssetLoader.Open(new Uri("avares://Anu.Core/Assets/settings.json"));
            using var fileStream = File.Create(_configFilePath);
            stream.CopyTo(fileStream);
        }

        try
        {
            var json = File.ReadAllText(_configFilePath);
            _configRoot = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            _configRoot = new JsonObject();
        }
    }

    public T? Get<T>(string path)
    {
        var keys = path.Split('.');
        JsonNode? current = _configRoot;

        foreach (var key in keys)
        {
            current = current is JsonObject obj && obj.TryGetPropertyValue(key, out var next) ? next : null;
            if (current == null) return default;
        }

        var typeInfo = GetJsonTypeInfo(typeof(T));
        if (typeInfo is JsonTypeInfo<T> typedInfo)
        {
            return current.Deserialize(typedInfo);
        }

        throw new NotSupportedException($"Type {typeof(T)} is not supported for deserialization.");
    }

    private static JsonTypeInfo? GetJsonTypeInfo(Type type)
    {
        if (type == typeof(string)) return JsonContext.Default.String;
        if (type == typeof(bool)) return JsonContext.Default.Boolean;
        if (type == typeof(ushort[])) return JsonContext.Default.UInt16Array;

        return null;
    }

    public void Set(string path, object? value)
    {
        var keys = path.Split('.');
        JsonObject current = _configRoot;

        for (int i = 0; i < keys.Length - 1; i++)
        {
            var key = keys[i];
            if (!current.TryGetPropertyValue(key, out var next) || next is not JsonObject nextObj)
            {
                nextObj = new JsonObject();
                current[key] = nextObj;
            }

            current = nextObj;
        }

        JsonTypeInfo? typeInfo = GetJsonTypeInfo(value!.GetType());
        if (typeInfo != null)
            current[keys[^1]] = JsonSerializer.SerializeToNode(value, typeInfo);
        else
            throw new NotSupportedException($"Type {value.GetType()} is not supported for serialization.");
        SaveConfig();
    }

    public void SaveConfig()
    {
        var json = _configRoot.ToJsonString(JsonContext.Default.Options);
        _ = FileUtil.SaveFileAsync(_configFilePath, json);
    }
}