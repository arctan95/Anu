using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anu.Core.Models;

public class McpServer
{
    [JsonPropertyName("name")]
    public string Command { get; set; }
    
    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = new();
    
    [JsonPropertyName("env")]
    public Dictionary<string, string> Env { get; set; } = new();
}