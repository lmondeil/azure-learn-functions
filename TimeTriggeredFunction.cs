using System;
using System.Threading.Tasks;
using functions;
using functions.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace azure_learn_functions
{
    public static class TimeTriggeredFunction
    {
        [FunctionName("TimeTriggeredFunction")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer,
        [Table(TodoApi.TodoTableName, Connection = TodoApi.TodoConnectionString)] CloudTable cloudTable, ILogger log)
        {
            var query = new TableQuery<TodoTableEntity>();
            var items = await cloudTable.ExecuteQuerySegmentedAsync(query, null);

            var deleted = 0;
            foreach (var item in items)
            {
                if(item.IsCompleted)
                {
                    await cloudTable.ExecuteAsync(TableOperation.Delete(item));
                    deleted++;
                }
            }

            log.LogInformation($"{deleted} items deleted at {DateTime.UtcNow}");
        }
    }
}
