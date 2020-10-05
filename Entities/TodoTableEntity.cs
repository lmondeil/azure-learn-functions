namespace functions.Entities
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    public class TodoTableEntity : TableEntity
    {
        public const string PARTITION_KEY = "TODO.PartitionKey";
        public DateTime CreatedTime { get; set; }
        public string TaskDescription { get; set; }
        public bool IsCompleted { get; set; }

        public TodoTableEntity()
        {
            this.PartitionKey = PARTITION_KEY;
        }
    }
}