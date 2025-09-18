using System.Collections.Generic;

namespace Anu.Core.Models;

public class McpConfig
{
    public Dictionary<string, McpServer> McpServers { get; set; } = new();
}