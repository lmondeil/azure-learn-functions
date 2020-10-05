using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using Newtonsoft.Json;
using functions.Models;
using System.Collections.Generic;
using System.Linq;
using functions.Entities;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

namespace functions
{
    public static class TodoApi
    {
        public const string TodoTableName = "todos";
        public const string TodoConnectionString = "AzureWebJobsStorage";
        [FunctionName("CreateTodo")]
        public static async Task<ActionResult<TodoItem>> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<TodoTableEntity> todoTable,
            [Queue("todos", Connection = "AzureWebJobsStorage")]IAsyncCollector<TodoItem> todoQueue,
            ILogger log)
        {
            log.LogInformation("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new TodoItem { TaskDescription = input.TaskDescription };
            await todoTable.AddAsync(todo.ToEntity());
            await todoQueue.AddAsync(todo);
            return new OkObjectResult(todo);
        }

        [FunctionName("GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")] HttpRequest req,
            [Table(TodoApi.TodoTableName, Connection = TodoApi.TodoConnectionString)] CloudTable cloudTable,
            ILogger log
        )
        {
            log.LogInformation("Getting todo list items");
            var query = new TableQuery<TodoTableEntity>();
            var segments = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            return new OkObjectResult(segments.Select(TodoItemExtensions.ToModel));
        }

        [FunctionName("GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")] HttpRequest req,
            [Table(TodoApi.TodoTableName, TodoTableEntity.PARTITION_KEY, "{id}", Connection = TodoApi.TodoConnectionString)] TodoTableEntity todoEntity,
            ILogger log,
            string id)
        {
            log.LogInformation($"Getting todo item with id {id}");
            if (todoEntity is null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(todoEntity.ToModel());
        }

        [FunctionName("UpdateTodo")]
        public static async Task<ActionResult<TodoItem>> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")] HttpRequest req,
            [Table(TodoApi.TodoTableName, Connection = TodoApi.TodoConnectionString)] CloudTable cloudTable,
            ILogger log,
            string id)
        {
            // Cloud treatments
            var findOperation = TableOperation.Retrieve<TodoTableEntity>(TodoTableEntity.PARTITION_KEY, id);
            var findResult = await cloudTable.ExecuteAsync(findOperation);
            if (findResult.Result is null)
            {
                return new NotFoundResult();
            }

            TodoTableEntity toUpdate = findResult.Result as TodoTableEntity;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            toUpdate.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrWhiteSpace(updated.TaskDescription))
            {
                toUpdate.TaskDescription = updated.TaskDescription;
            }

            // Cloud treatments
            var replaceOperation = TableOperation.Replace(toUpdate);
            await cloudTable.ExecuteAsync(replaceOperation);

            return new OkObjectResult(toUpdate.ToModel());
        }

        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")] HttpRequest req,
            [Table(TodoApi.TodoTableName, Connection = TodoApi.TodoConnectionString)] CloudTable cloudTable,
            ILogger log,
            string id)
        {
            log.LogInformation($"Deleting Todo item with id {id}");

            var delteOperation = TableOperation.Delete(new TableEntity
            {
                PartitionKey = TodoTableEntity.PARTITION_KEY,
                RowKey = id,
                ETag = "*"
            });
            try
            {
                await cloudTable.ExecuteAsync(delteOperation);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }
    }
}
