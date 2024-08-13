using Microsoft.WindowsAzure.Storage.Table;
using System;

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
