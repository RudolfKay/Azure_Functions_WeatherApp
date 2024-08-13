using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.Net.Http;
using System.IO;
using System;

namespace Atea.Task1
{
    public static class GetWeather
    {
        [FunctionName("GetWeather")]
        public static async Task Run(
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Blob("weatherdata/{sys.utcnow}.txt", FileAccess.Write, Connection = "AzureWebJobsStorage")] BlobContainerClient blobContainerClient,
            ILogger log)
        {
            //WeatherResponse weatherObject = null;
            string logMessage;
            string status;
            DateTime timestamp = DateTime.UtcNow;
            string logId = Guid.NewGuid().ToString();

            // Access the Table Storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable logTable = tableClient.GetTableReference("WeatherLogs");

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync("https://api.openweathermap.org/data/2.5/weather?q=London&appid=426702e350c498554c037f99df7644ca");

                    if (response.IsSuccessStatusCode)
                    {
                        var weatherData  = await response.Content.ReadAsStringAsync();

                        // Generate a unique GUID for the blob name
                        var blobName = $"{logId}.json";
                        BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                        // Ensure the container exists
                        await blobContainerClient.CreateIfNotExistsAsync();

                        // Save the full weather data to Blob Storage
                        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(weatherData), writable: false))
                        {
                            await blobClient.UploadAsync(stream, overwrite: true);
                        }

                        status = "Success";
                        logMessage = "Weather data retrieved and stored successfully.";
                    }
                    else
                    {
                        status = "Failure";
                        logMessage = "Failed to retrieve weather data.";
                    }
                }
                catch (Exception ex)
                {
                    status = "Failure";
                    logMessage = $"Failed to retrieve and store weather data: {ex.Message}";
                    log.LogError(logMessage);
                }

                
                // Ensure the table exists
                await logTable.CreateIfNotExistsAsync();

                // Generate a unique RowKey using a GUID
                string rowKey = logId;

                // PartitionKey can be based on date (e.g., yyyyMMdd) for easier querying by day
                string partitionKey = timestamp.ToString("yyyyMMdd");

                // Log the operation result in Azure Table Storage
                var logEntity = new LogEntity(partitionKey, rowKey)
                {
                    Status = status,
                    Message = logMessage,
                    TimestampUtc = timestamp
                };

                TableOperation insertOperation = TableOperation.Insert(logEntity);
                await logTable.ExecuteAsync(insertOperation);

                // Log output to the console (useful for debugging in local dev environments)
                log.LogInformation($"Timer trigger function executed at: {DateTime.Now}");
                log.LogInformation(logMessage);
            }
        }
    } 
}
