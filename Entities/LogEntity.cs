using Microsoft.WindowsAzure.Storage.Table;
using System;

/// <summary>
/// Represents a log entry in Azure Table Storage for weather data operations.
/// </summary>
/// <remarks>
/// This entity is used to store information about the status and details of weather data retrieval operations. 
/// It extends the <see cref="TableEntity"/> class and includes properties for the status of the operation, 
/// a message with additional details, and a timestamp indicating when the log entry was created.
/// </remarks>
public class LogEntity : TableEntity
{
    public LogEntity(string partitionKey, string rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }

    public LogEntity() { }

    public string Status { get; set; }
    public string Message { get; set; }
    public DateTime TimestampUtc { get; set; }
}
