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
    /// Azure Function triggered by an HTTP GET request to retrieve weather data from Azure Blob Storage.
    /// This function fetches weather data for a specific GUID and returns it as a JSON object.
    /// </summary>
    /// <param name="req">The HTTP request object used to retrieve the query parameter (GUID) from the request.</param>
    /// <param name="blobClient">The BlobClient used to interact with Azure Blob Storage where the weather data is stored.</param>
    /// <param name="log">The ILogger instance used for logging information, warnings, and errors during function execution.</param>
    /// <returns>
    /// An IActionResult that represents the result of the HTTP request:
    /// - 200 OK with weather data if the blob exists and is successfully retrieved.
    /// - 400 Bad Request if the GUID parameter is invalid or missing.
    /// - 404 Not Found if the blob with the specified GUID does not exist.
    /// - 500 Internal Server Error if an exception occurs during processing.
    /// </returns>
    /// <remarks>
    /// This function performs the following steps:
    /// 1. Extracts the GUID parameter from the query string of the HTTP request.
    /// 2. Validates the GUID to ensure it is not null and properly formatted.
    /// 3. Checks if a blob with the given GUID exists in Azure Blob Storage.
    /// 4. If the blob exists, downloads its content, deserializes it into a WeatherResponse object, and returns it in the HTTP response.
    /// 5. If the blob does not exist, returns a 404 Not Found response.
    /// 6. If there is any error during processing, logs the error and returns a 500 Internal Server Error response.
    /// </remarks>
    [FunctionName("GetWeatherDataByGuid")]
    public static async Task<IActionResult> GetWeatherDataByGuid(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queryWeatherData")] HttpRequest req,
        [Blob("weatherdata/{sys.utcnow}", Connection = "AzureWebJobsStorage")] BlobClient blobClient,
        ILogger log)
    {
        // Extract the GUID from query string
        string guid = req.Query["guid"];

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