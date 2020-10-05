using System;
using System.Threading.Tasks;
using functions;
using functions.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace azure_learn_functions
{
    public static class QueueTriggeredFunction
    {
        [FunctionName("QueueTriggeredFunction")]
        public static async Task Run([QueueTrigger(TodoApi.TodoTableName, Connection = TodoApi.TodoConnectionString)]TodoItem todoItem,
        [Blob("todos", Connection = TodoApi.TodoConnectionString)]CloudBlobContainer container,
        ILogger log)
        {
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference($"{todoItem.Id}.txt");
            await blob.UploadTextAsync($"New task created : {todoItem.TaskDescription}");
            log.LogInformation($"C# Queue trigger function processed: {todoItem.TaskDescription}");
        }
    }
}
