using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Atea.Task1.Models;
using Newtonsoft.Json;
using System.IO;
using System;

public static class QueryWeatherData
{
    /// <summary>
    /// Retrieves weather data by GUID from Azure Blob Storage.
    /// </summary>
    /// <param name="req">The HTTP request containing the GUID parameter.</param>
    /// <param name="log">Logger for capturing execution details.</param>
    /// <returns>Weather data if found; appropriate HTTP responses otherwise.</returns>
    [FunctionName("GetWeatherDataByGuidFunction")]
    public static async Task<IActionResult> GetWeatherDataByGuidFunction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queryWeatherData")] HttpRequest req,
        ILogger log)
    {
        // Extract and validate the GUID parameter
        if (!TryGetGuidFromQuery(req, out Guid guid))
        {
            return new BadRequestObjectResult("Invalid or missing GUID parameter.");
        }

        // Access the BlobClient and check if the blob exists
        var blobClient = GetBlobClientForGuid(guid);
        if (!await blobClient.ExistsAsync())
        {
            return new NotFoundObjectResult($"Blob with GUID '{guid}' does not exist.");
        }

        // Retrieve and return the weather data
        try
        {
            var weatherData = await DownloadWeatherDataAsync(blobClient);
            return new OkObjectResult(weatherData);
        }
        catch (Exception ex)
        {
            log.LogError($"Error processing blob '{guid}': {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Tries to retrieve and validate the GUID parameter from the HTTP request.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="guid">The parsed GUID.</param>
    /// <returns>True if the GUID is valid, false otherwise.</returns>
    private static bool TryGetGuidFromQuery(HttpRequest req, out Guid guid)
    {
        string guidParam = req.Query["guid"];
        return Guid.TryParse(guidParam, out guid);
    }

    /// <summary>
    /// Creates a BlobClient for accessing the weather data for a specific GUID.
    /// </summary>
    /// <param name="guid">The GUID representing the specific weather data blob.</param>
    /// <returns>The BlobClient for accessing the corresponding blob.</returns>
    private static BlobClient GetBlobClientForGuid(Guid guid)
    {
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        return new BlobClient(connectionString, "weatherdata", $"{guid}.json");
    }

    /// <summary>
    /// Downloads and deserializes weather data from the blob.
    /// </summary>
    /// <param name="blobClient">The BlobClient to download the data from.</param>
    /// <returns>The deserialized WeatherResponse object.</returns>
    private static async Task<WeatherResponse> DownloadWeatherDataAsync(BlobClient blobClient)
    {
        var downloadResponse = await blobClient.DownloadAsync();
        using (var stream = downloadResponse.Value.Content)
        using (var reader = new StreamReader(stream))
        {
            string jsonContent = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<WeatherResponse>(jsonContent);
        }
    }
}