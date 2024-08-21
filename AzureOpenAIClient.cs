using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;

class AzureOpenAIClient
{
    private readonly string azureEndpoint;
    private readonly string apiKey;
    private readonly string apiVersion;
    private readonly string model;
    public string AssistantId { get; private set; }

    private static readonly HttpClient client = new HttpClient();

    public AzureOpenAIClient(IConfiguration? configuration, string assistantId = null)
    {
        azureEndpoint = configuration["AzureOpenAI:Endpoint"];
        apiKey = configuration["AzureOpenAI:ApiKey"];
        apiVersion = configuration["AzureOpenAI:ApiVersion"];
        model = configuration["AzureOpenAI:Model"];
        this.AssistantId = assistantId;

        client.DefaultRequestHeaders.Add("api-key", apiKey);
    }

    public async Task<string> NewThreadAsync()
    {
        Console.WriteLine("=== Creating Open AI Thread ===");
        var threadApi = $"{azureEndpoint}/openai/threads?api-version={apiVersion}";

        var response = await client.PostAsync(threadApi, null);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JObject.Parse(responseBody);

        Console.WriteLine($"=== Thread Created at {jsonResponse["id"]} ===");
        return jsonResponse["id"].ToString();
    }

    public async Task<string> AddMessagesAsync(string messages, string threadId)
    {
        Console.WriteLine("=== Processing Open AI Messages API ===");
        var messageApi = $"{azureEndpoint}/openai/threads/{threadId}/messages?api-version={apiVersion}";

        var body = new
        {
            role = "user",
            content = messages
        };

        var jsonContent = JsonConvert.SerializeObject(body);
        var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(messageApi, httpContent);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
        else
        {
            return $"Error: {response.StatusCode.ToString()}";
        }
    }

    public async Task<JObject> RunsThreadAsync(string threadId, string assistantId)
    {
        Console.WriteLine("=== Running Open AI Thread ===");
        var runApi = $"{azureEndpoint}/openai/threads/{threadId}/runs?api-version={apiVersion}";

        var body = new
        {
            assistant_id = assistantId,
            model = this.model
        };

        var jsonContent = JsonConvert.SerializeObject(body);
        var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync(runApi, httpContent);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseBody);
        }
        else
        {
            //Console.WriteLine($"Request failed with status code {response.StatusCode}");
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }
    }

    public async Task<string> GetChatHistoryAsync(string threadId)
    {
        Console.WriteLine("=== Processing Open AI Chat History API ===");
        var messageApi = $"{azureEndpoint}/openai/threads/{threadId}/messages?api-version={apiVersion}";
        var response = await client.GetAsync(messageApi);

        if (response.IsSuccessStatusCode)
        {
            var data = JObject.Parse(await response.Content.ReadAsStringAsync());
            if (data["data"]?[0]?["role"]?.ToString() != "assistant" || data["data"]?[0]?["content"]?.HasValues == false)
            {
                await Task.Delay(5000);  // Wait for 5 seconds if the assistant hasn't responded yet
                return await GetChatHistoryAsync(threadId);  // Recursively call until an assistant response is found
            }
            else
            {
                return data.ToString();
            }
        }
        else
        {
            //Console.WriteLine($"Request failed with status code {response.StatusCode}");
            throw new Exception($"Request failed with status code {response.StatusCode}");
        }
    }

    public string ExtractJson(string response)
    {
        string result = response.Split("```")[1].Replace("\n", "").Replace("json", "");

        return result;
    }

}