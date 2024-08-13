using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Newtonsoft.Json;
using Atea.Task1.Models;

public static class QueryWeatherData
{
    [FunctionName("QueryWeatherData")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queryWeatherData")] HttpRequest req,
        [Blob("weatherdata/{sys.utcnow}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        ILogger log)
    {
        // Extract the GUID from query string
        string guid = req.Query["guid"];

        log.LogInformation($"QueryWeatherData function processed a request for GUID: {guid}");

        // Validate the GUID
        if (string.IsNullOrEmpty(guid) || !Guid.TryParse(guid, out _))
        {
            return new BadRequestObjectResult("Invalid or missing GUID parameter.");
        }

        // Get the blob client for the given GUID
        blobClient = new BlobClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "weatherdata", $"{guid}.json");

        // Check if the blob exists
        if (!await blobClient.ExistsAsync())
        {
            return new NotFoundObjectResult($"Blob with GUID '{guid}' does not exist.");
        }

        try
        {
            // Download the blob content
            var downloadResponse = await blobClient.DownloadAsync();
            using (var stream = downloadResponse.Value.Content)
            using (var reader = new StreamReader(stream))
            {
                var jsonContent = await reader.ReadToEndAsync();
                WeatherResponse weatherData = JsonConvert.DeserializeObject<WeatherResponse>(jsonContent);

                // Return the weather data
                return new OkObjectResult(weatherData);
            }
        }
        catch (Exception ex)
        {
            log.LogError($"Error processing blob '{guid}': {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}