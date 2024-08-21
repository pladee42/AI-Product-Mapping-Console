using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    public static IConfiguration? Configuration { get; private set; }

    static async Task Main(string[] args)
    {
        // Set up configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        Configuration = builder.Build();

        // Access the settings
        string? extractorAgentId = Configuration["ProductExtractorAgentId"];
        string? matchingAgentId = Configuration["ProductMatchingAgentId"];

        var stopwatch = Stopwatch.StartNew();

        BZBSClient bzbsClient = new BZBSClient(Configuration);
        AzureOpenAIClient productExtractorAgent = new AzureOpenAIClient(Configuration, extractorAgentId!);
        AzureOpenAIClient productMatchingAgent = new AzureOpenAIClient(Configuration, matchingAgentId!);

        string vendorFileName = PromptUser("Please input vendor file name: ");
        string bzbsFileName = PromptUser("Please input Buzzebees file name: ");

        string fileId = await bzbsClient.AddFileAsync(vendorFileName);
        string ocrData = await bzbsClient.GetFileDetailAsync(fileId);
        string vendorData = await AgentChatAsync(productExtractorAgent, ocrData);
        WriteLog("vendor", vendorData);

        Console.WriteLine($"1st Agent Time taken: {stopwatch.Elapsed.TotalSeconds} seconds");

        string bzbsData;
        using (var reader = new StreamReader($"input/bzbs/{bzbsFileName}"))
        {
            bzbsData = await reader.ReadToEndAsync();
        }

        string prompt = new GetPrompt(vendorData, bzbsData).GetPromptText(vendorData, bzbsData);
        WriteLog("prompt", prompt);

        var result = await AgentChatAsync(productMatchingAgent, prompt);
        WriteLog("result", result.ToString());

        // Save result to CSV (use a CSV library like CsvHelper if needed)
        SaveToCsv("output/match_result.csv", result);
    }

    private static string PromptUser(string message)
    {
        Console.Write(message);
        return Console.ReadLine()!;
    }

    private static void WriteLog(string filename, string data)
    {
        File.WriteAllText($"log/{filename}.txt", data);
    }



    private static async Task<string> AgentChatAsync(AzureOpenAIClient agent, string messages)
    {
        string threadId = await agent.NewThreadAsync();
        await agent.AddMessagesAsync(messages, threadId);
        await agent.RunsThreadAsync(threadId, agent.AssistantId);

        var jsonObject = JObject.Parse(await agent.GetChatHistoryAsync(threadId));
        string response = jsonObject["data"]?[0]?["content"]?[0]?["text"]?["value"]?.ToString() ?? string.Empty;

        if (agent.AssistantId == Configuration?["ProductMatchingAgentId"])
        {
            return agent.ExtractJson(response).ToString()!;
        }
        else
        {
            return response;
        }
    }

    private static void SaveToCsv(string filePath, string data)
    {

        JArray jsonArray = JArray.Parse(data);

        // Get the headers from the first object
        var headers = jsonArray.First?.Children<JProperty>().Select(p => p.Name).ToArray();

        // Create the CSV lines
        var csvLines = new List<string>
        {
            string.Join(",", headers!) // Add the headers as the first line
        };

        // Add each JSON object's values as a CSV line
        foreach (var obj in jsonArray)
        {
            var values = obj.Children<JProperty>().Select(p => p.Value.ToString()).ToArray();
            csvLines.Add(string.Join(",", values));
        }

        // Combine all lines into a single CSV string
        string csv = string.Join(Environment.NewLine, csvLines);

        // Output the CSV file
        File.WriteAllText(filePath, csv);
    }
}
