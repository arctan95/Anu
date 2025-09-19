using System.Text.Json.Serialization;

namespace Anu.Core.Models;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(ushort[]))]
[JsonSerializable(typeof(McpServer))]
[JsonSerializable(typeof(McpConfig))]
public partial class JsonContext : JsonSerializerContext
{
}