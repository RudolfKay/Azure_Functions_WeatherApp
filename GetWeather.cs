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
        /// <summary>
        /// Azure Function triggered by a Timer to fetch weather data from OpenWeatherMap API every minute.
        /// The retrieved data is stored in Azure Blob Storage and logs the result in Azure Table Storage.
        /// </summary>
        /// <param name="myTimer">The TimerInfo object used by the TimerTrigger to determine the schedule for the function.</param>
        /// <param name="blobContainerClient">The BlobContainerClient used to interact with Azure Blob Storage, where the weather data is saved.</param>
        /// <param name="log">The ILogger instance used for logging information, warnings, and errors during function execution.</param>
        /// <returns>A task that represents the asynchronous operation of the function.</returns>
        /// <remarks>
        /// This function performs the following steps:
        /// 1. Fetches weather data for London from the OpenWeatherMap API.
        /// 2. Saves the retrieved weather data in Azure Blob Storage with a unique name based on a GUID.
        /// 3. Logs the result of the operation in Azure Table Storage, including success or failure status and any error messages.
        /// The function uses a timer trigger to execute every minute. If the operation is successful, a success message is logged; otherwise, the error message is logged.
        /// </remarks>
        [FunctionName("FetchAndStoreWeatherDataFunction")]
        public static async Task FetchAndStoreWeatherDataFunction(
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Blob("weatherdata/{sys.utcnow}.txt", FileAccess.Write, Connection = "AzureWebJobsStorage")] BlobContainerClient blobContainerClient,
            ILogger log)
        {
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
            }
        }
    } 
}
