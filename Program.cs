using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;

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
        SaveToExcel("output/match_result.xlsx", result);
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

    // private static void SaveToCsv(string filePath, string data)
    // {

    // JArray jsonArray = JArray.Parse(data);

    // // Get the headers from the first object
    // var headers = jsonArray.First?.Children<JProperty>().Select(p => p.Name).ToArray();

    // // Create the CSV lines
    // var csvLines = new List<string>
    // {
    // string.Join(",", headers!) // Add the headers as the first line
    // };

    // // Add each JSON object's values as a CSV line
    // foreach (var obj in jsonArray)
    // {
    // var values = obj.Children<JProperty>().Select(p => p.Value.ToString()).ToArray();
    // csvLines.Add(string.Join(",", values));
    // }

    // // Combine all lines into a single CSV string
    // string csv = string.Join(Environment.NewLine, csvLines);

    // // Output the CSV file
    // File.WriteAllText(filePath, csv);
    // }
    // }

    private static void SaveToExcel(string filePath, string data)
    {
        // Set the EPPlus license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        try
        {
            // Parse the input string data to JArray
            JArray jsonArray = JArray.Parse(data);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // Add headers
                worksheet.Cells[1, 1].Value = "Vendor SKU";
                worksheet.Cells[1, 2].Value = "Vendor Product";
                worksheet.Cells[1, 3].Value = "BZBS Product";
                worksheet.Cells[1, 4].Value = "BZBS SKU";
                worksheet.Cells[1, 5].Value = "Probability";
                worksheet.Cells[1, 6].Value = "Quantity";

                int row = 2;
                foreach (var item in jsonArray)
                {
                    worksheet.Cells[row, 1].Value = item["vendor_sku"]?.ToString();
                    worksheet.Cells[row, 2].Value = item["vendor_product"]?.ToString();
                    worksheet.Cells[row, 3].Value = item["bzbs_product"]?.ToString();
                    worksheet.Cells[row, 4].Value = item["bzbs_sku"]?.ToString();
                    double probability = item["probability"]?.ToObject<double>() ?? 0;
                    worksheet.Cells[row, 5].Value = probability;
                    worksheet.Cells[row, 6].Value = item["quantity"]?.ToObject<int>();

                    // Highlight the Probability cell based on the criteria
                    if (probability < 0.21)
                    {
                        worksheet.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.Red);
                    }
                    else if (probability >= 0.21 && probability <= 0.41)
                    {
                        worksheet.Cells[row, 5].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 5].Style.Fill.BackgroundColor.SetColor(Color.Yellow);
                    }

                    row++;
                }

                // AutoFit columns for better display
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Save the Excel package to the specified file path
                FileInfo excelFile = new FileInfo(filePath);
                package.SaveAs(excelFile);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting JSON to Excel: {ex.Message}");
        }
    }

}