using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using System;

public static class QueryWeatherLogs
{
    /// <summary>
    /// Azure Function triggered by an HTTP GET request to query weather logs from Azure Table Storage.
    /// This function retrieves weather log entries within a specified date range and returns them as a JSON array.
    /// </summary>
    /// <param name="req">The HTTP request object used to retrieve query parameters (date range) from the request.</param>
    /// <param name="log">The ILogger instance used for logging information, warnings, and errors during function execution.</param>
    /// <returns>
    /// An IActionResult that represents the result of the HTTP request:
    /// - 200 OK with the list of weather log entries if the logs are successfully retrieved.
    /// - 400 Bad Request if the 'from' or 'to' date parameters are invalid or if 'from' is greater than 'to'.
    /// - 404 Not Found if the WeatherLogs table does not exist.
    /// </returns>
    /// <remarks>
    /// This function performs the following steps:
    /// 1. **Extract Date Range**: Retrieves the 'from' and 'to' date parameters from the query string and validates them.
    /// 2. **Validate Dates**: Ensures that the 'from' date is not greater than the 'to' date and that both dates are valid.
    /// 3. **Access Table Storage**: Connects to Azure Table Storage and retrieves the reference to the WeatherLogs table.
    /// 4. **Check Table Existence**: Verifies if the WeatherLogs table exists in Azure Table Storage.
    /// 5. **Build and Execute Query**: Constructs a query to filter log entries based on the provided date range and executes it, retrieving all matching entries.
    /// 6. **Return Results**: Returns the retrieved log entries as a JSON array in the HTTP response.
    /// </remarks>
    [FunctionName("GetWeatherLogsByDateRange")]
    public static async Task<IActionResult> GetWeatherLogsByDateRange(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queryWeatherLogs")] HttpRequest req,
        ILogger log)
    {
        DateTime from;
        DateTime to;

        // Retrieve 'from' and 'to' from the query string
        string fromQuery = req.Query["from"];
        string toQuery = req.Query["to"];

        // Validate and parse input parameters
        if (string.IsNullOrEmpty(fromQuery) || !DateTime.TryParse(fromQuery, out from))
        {
            return new BadRequestObjectResult("Please provide a valid 'from' date.");
        }

        if (string.IsNullOrEmpty(toQuery) || !DateTime.TryParse(toQuery, out to))
        {
            return new BadRequestObjectResult("Please provide a valid 'to' date.");
        }

        from = DateTime.Parse(fromQuery);
        to = DateTime.Parse(toQuery);

        if (from > to)
        {
            return new BadRequestObjectResult("'from' date cannot be greater than 'to' date.");
        }

        // Access the Table Storage
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        CloudTable logTable = tableClient.GetTableReference("WeatherLogs");

        // Ensure the table exists
        if (!await logTable.ExistsAsync())
        {
            return new NotFoundObjectResult("WeatherLogs table does not exist.");
        }

        // Build the query
        string fromFilter = TableQuery.GenerateFilterConditionForDate("TimestampUtc", QueryComparisons.GreaterThanOrEqual, from);
        string toFilter = TableQuery.GenerateFilterConditionForDate("TimestampUtc", QueryComparisons.LessThanOrEqual, to);
        string combinedFilter = TableQuery.CombineFilters(fromFilter, TableOperators.And, toFilter);

        TableQuery<LogEntity> query = new TableQuery<LogEntity>().Where(combinedFilter);
        List<LogEntity> logEntities = new List<LogEntity>();

        // Execute the query
        TableContinuationToken token = null;
        do
        {
            var queryResult = await logTable.ExecuteQuerySegmentedAsync(query, token);
            logEntities.AddRange(queryResult.Results);
            token = queryResult.ContinuationToken;
        } while (token != null);

        // Return the results
        return new OkObjectResult(logEntities);
    }
}

