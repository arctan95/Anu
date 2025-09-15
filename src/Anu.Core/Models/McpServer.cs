using System.Collections.Generic;

namespace Anu.Core.Models;

public class McpServer
{
    public string Command { get; set; }
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string> Env { get; set; } = new();
}