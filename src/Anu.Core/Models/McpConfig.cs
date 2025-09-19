using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anu.Core.Models;

public class McpConfig
{
    [JsonPropertyName("mcpServers")]
    public Dictionary<string, McpServer> McpServers { get; set; } = new();
}