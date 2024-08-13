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

public static class QueryWeatherLogsFunction
{
    [FunctionName("QueryWeatherLogs")]
    public static async Task<IActionResult> Run(
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

