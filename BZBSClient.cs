using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

class BZBSClient
{
    private readonly string bzbsEndpoint;
    private readonly string bzbsBlob;
    private static readonly HttpClient client = new HttpClient();

    public BZBSClient(IConfiguration configuration)
    {
        // Load values from configuration
        bzbsEndpoint = configuration["BZBS:Endpoint"];
        bzbsBlob = configuration["BZBS:BlobUrl"];
    }

    public async Task<string> AddFileAsync(string fileName)
    {
        Console.WriteLine("=== Processing Add File API ===");

        var body = new
        {
            FileUrl = $"{bzbsBlob}{fileName}"
        };

        var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(body);
        var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{bzbsEndpoint}demo/add", httpContent);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(responseBody);
            string fileId = data["Data"]["DocumentReferenceId"].ToString();
            return fileId;
        }
        else
        {
            Console.WriteLine($"Request failed with status code {response.StatusCode}");
            return response.ReasonPhrase;
        }
    }

    public async Task<string> GetFileDetailAsync(string fileId)
    {
        Console.WriteLine("=== Retrieving File Details API ===");

        var response = await client.GetAsync($"{bzbsEndpoint}demo/detail?DocumentRefernceId={fileId}");

        if (response.IsSuccessStatusCode)
            {
                // Parse the JSON response
                var responseData = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(responseData);

                if (data["Data"]["DocumentStatus"].ToString() == "pending")
                {
                    await Task.Delay(1500);
                    return await GetFileDetailAsync(fileId);
                }
                else
                {
                    return data["Data"]["Content"].ToString();
                }
            }
            else
            {
                // Print the error status and return the response text
                Console.WriteLine($"Request failed with status code {response.StatusCode}");
                return await response.Content.ReadAsStringAsync();
            }
    }
}