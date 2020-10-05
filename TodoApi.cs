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

namespace functions
{
    public static class TodoApi
    {
        static List<TodoItem> items = new List<TodoItem>();

        [FunctionName("CreateTodo")]
        public static async Task<ActionResult<TodoItem>> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")] HttpRequest req, TraceWriter log)
        {
            log.Info("Creating a new todo list item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new TodoItem { TaskDescription = input.TaskDescription };
            items.Add(todo);
            return new OkObjectResult(todo);
        }

        [FunctionName("GetTodos")]
        public static IActionResult GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")]HttpRequest req, ILogger log 
        )
        {
            log.LogInformation("Getting todo list items");
            return new OkObjectResult(items);
        }

        [FunctionName("GetTodoById")]
        public static IActionResult GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route="todo/{id}")]HttpRequest req, 
            TraceWriter log, 
            string id)
        {
            log.Info($"Getting todo item with id {id}");
            var todoItem = items.FirstOrDefault(x => x.Id == id);
            if (todoItem is null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(todoItem);
        }

        [FunctionName("UpdateTodo")]
        public static async Task<ActionResult<TodoItem>> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route="todo/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            var toUpdate = items.FirstOrDefault(x => x.Id == id);
            if (toUpdate is null)
            {
                return new NotFoundResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            toUpdate.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrWhiteSpace(updated.TaskDescription)) 
            {
                toUpdate.TaskDescription = updated.TaskDescription;
            }

            return new OkObjectResult(toUpdate);
        }

        [FunctionName("DeleteTodo")]
        public static IActionResult DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route="todo/{id}")]HttpRequest req,
            TraceWriter log,
            string id)
        {
            log.Info($"Deleting Todo item with id {id}");

            var todoItem = items.FirstOrDefault(x => x.Id == id);
            if (todoItem is null)
            {
                return new NotFoundResult();
            }

            items.Remove(todoItem);
            return new OkResult();
        }
    }
}
