namespace functions.Entities
{
    using System;
    using functions.Models;

    public static class TodoItemExtensions
    {
        public static TodoTableEntity ToEntity(this TodoItem model) => new TodoTableEntity
        {
            RowKey = model.Id,
            TaskDescription = model.TaskDescription,
            CreatedTime = model.CreatedTime,
            IsCompleted = model.IsCompleted
        };

        public static TodoItem ToModel(this TodoTableEntity entity) => new TodoItem
        {
            Id = entity.RowKey,
            TaskDescription = entity.TaskDescription,
            CreatedTime = entity.CreatedTime,
            IsCompleted = entity.IsCompleted
        };       
    }
}