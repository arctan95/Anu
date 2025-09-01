using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anu.Core.Models;

public class OmniParserResponse
{
    [JsonPropertyName("som_image_base64")]
    public string SomImageBase64 { get; set; }

    [JsonPropertyName("parsed_content_list")]
    public List<ParsedContent> ParsedContentList { get; set; }

    [JsonPropertyName("latency")]
    public double Latency { get; set; }
    
    public class ParsedContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } // "text" or "icon"

        [JsonPropertyName("bbox")]
        public List<double> Bbox { get; set; } // [x1, y1, x2, y2]

        [JsonPropertyName("interactivity")]
        public bool Interactivity { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }
    }
}