using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Anu.Core.Services;

public class McpConfigService
{
    private readonly string _mcpConfigFilePath;

    public string McpConfigFilePath => _mcpConfigFilePath;

    public async Task<string> ReadMcpConfigJson()
    {
        try
        {
            return await File.ReadAllTextAsync(_mcpConfigFilePath);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public McpConfigService()
    {
        var configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".anu");
        Directory.CreateDirectory(configDirectory);

        _mcpConfigFilePath = Path.Combine(configDirectory, "mcp.json");

        if (!File.Exists(_mcpConfigFilePath))
        {
            using var stream = AssetLoader.Open(new Uri("avares://Anu.Core/Assets/mcp.json"));
            using var fileStream = File.Create(_mcpConfigFilePath);
            stream.CopyToAsync(fileStream);
        }
    }
}