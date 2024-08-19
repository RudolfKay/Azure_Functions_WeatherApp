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
    /// Retrieves weather logs from Azure Table Storage within the specified date range.
    /// </summary>
    /// <param name="req">The HTTP request containing 'from' and 'to' query parameters.</param>
    /// <param name="log">Logger to capture execution details.</param>
    /// <returns>Weather logs within the specified date range or appropriate HTTP responses.</returns>
    [FunctionName("GetWeatherLogsByDateRange")]
    public static async Task<IActionResult> GetWeatherLogsByDateRange(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "queryWeatherLogs")] HttpRequest req,
        ILogger log)
    {
        // Validate and parse 'from' and 'to' dates from the query string
        if (!TryGetDateFromQuery(req, "from", out DateTime from) || 
            !TryGetDateFromQuery(req, "to", out DateTime to))
        {
            return new BadRequestObjectResult("Please provide valid 'from' and 'to' dates.");
        }

        if (from > to)
        {
            return new BadRequestObjectResult("'from' date cannot be greater than 'to' date.");
        }

        // Access Azure Table Storage
        var logTable = await GetWeatherLogsTableAsync();
        if (logTable == null)
        {
            return new NotFoundObjectResult("WeatherLogs table does not exist.");
        }

        // Build and execute the query for the specified date range
        var logEntities = await QueryWeatherLogsAsync(logTable, from, to);

        // Return the results
        return new OkObjectResult(logEntities);
    }

    /// <summary>
    /// Attempts to parse a date from the query string.
    /// </summary>
    private static bool TryGetDateFromQuery(HttpRequest req, string key, out DateTime date)
    {
        string dateQuery = req.Query[key];
        return DateTime.TryParse(dateQuery, out date);
    }

    /// <summary>
    /// Gets the WeatherLogs table reference from Azure Table Storage.
    /// </summary>
    private static async Task<CloudTable> GetWeatherLogsTableAsync()
    {
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        CloudTable logTable = tableClient.GetTableReference("WeatherLogs");

        if (await logTable.ExistsAsync())
        {
            return logTable;
        }

        return null;
    }

    /// <summary>
    /// Queries weather logs from the table based on the provided date range.
    /// </summary>
    private static async Task<List<LogEntity>> QueryWeatherLogsAsync(CloudTable logTable, DateTime from, DateTime to)
    {
        string fromFilter = TableQuery.GenerateFilterConditionForDate("TimestampUtc", QueryComparisons.GreaterThanOrEqual, from);
        string toFilter = TableQuery.GenerateFilterConditionForDate("TimestampUtc", QueryComparisons.LessThanOrEqual, to);
        string combinedFilter = TableQuery.CombineFilters(fromFilter, TableOperators.And, toFilter);

        TableQuery<LogEntity> query = new TableQuery<LogEntity>().Where(combinedFilter);
        List<LogEntity> logEntities = new List<LogEntity>();

        TableContinuationToken token = null;
        do
        {
            var queryResult = await logTable.ExecuteQuerySegmentedAsync(query, token);
            logEntities.AddRange(queryResult.Results);
            token = queryResult.ContinuationToken;
        } while (token != null);

        return logEntities;
    }

}

