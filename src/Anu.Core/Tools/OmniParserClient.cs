using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Anu.Core.Models;

namespace Anu.Core.Tools;

public class OmniParserClient
{
    private static readonly OmniParserResponse DefaultResponse = new();
    private readonly HttpClient _httpClient = new();
    private string _apiUrl = "http://127.0.0.1:8000/parse/";

    public OmniParserClient(){}
    
    public OmniParserClient(string apiUrl)
    {
        _apiUrl = apiUrl;
    }
    
    public async Task<OmniParserResponse> ParseImageAsync(byte[] inputImageBytes)
    {
        try
        {
            string base64Image = Convert.ToBase64String(inputImageBytes);

            var requestBody = new { base64_image = base64Image };
            string json = JsonSerializer.Serialize(requestBody);

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<OmniParserResponse>(responseBody, options);

            return result ?? DefaultResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
            return DefaultResponse;
        }
    }
    
    public string GetParsedScreenInfo(OmniParserResponse response)
    {
        var screenInfoBuilder = new StringBuilder();

        for (int idx = 0; idx < response.ParsedContentList.Count; idx++)
        {
            var element = response.ParsedContentList[idx];

            if (element.Type == "text")
            {
                screenInfoBuilder.AppendLine($"ID: {idx}, Text: {element.Content}");
            }
            else if (element.Type == "icon")
            {
                screenInfoBuilder.AppendLine($"ID: {idx}, Icon: {element.Content}");
            }
        }

        return screenInfoBuilder.ToString();
    }

}