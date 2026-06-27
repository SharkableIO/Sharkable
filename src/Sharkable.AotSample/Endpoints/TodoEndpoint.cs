using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Sharkable.AotSample;

/// <summary>
/// CRUD endpoints for managing Todo items.
/// Demonstrates: validation, rate limiting, output cache, idempotency, OpenAPI metadata.
/// </summary>
[SharkDescription("Todo Management", "Create, read, update, and delete todo items with priority tracking")]
[SharkResponseType(StatusCodes.Status200OK, typeof(Todo), "Success")]
[SharkResponseType(StatusCodes.Status400BadRequest, typeof(UnifiedResult<object>), "Validation error")]
[SharkResponseType(StatusCodes.Status404NotFound, typeof(UnifiedResult<object>), "Todo not found")]
public sealed class TodoEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (ITodoService store) =>
        {
            var items = store.GetAll();
            return Results.Ok(items);
        })
        .SharkCacheOutput("todos")
        .WithName("GetAllTodos");

        app.MapGet("/{id}", (int id, ITodoService store) =>
        {
            var todo = store.GetById(id);
            return todo is not null
                ? Results.Ok(todo)
                : Results.NotFound(new UnifiedResult<object>(null, "Todo not found", HttpStatusCode.NotFound));
        })
        .WithName("GetTodoById")
        .AllowAnonymous();

        app.MapPost("/", ([FromBody] CreateTodoRequest request, ITodoService store) =>
        {
            var todo = store.Create(request);
            return Results.Created($"/api/todo/{todo.Id}", todo);
        })
        .SharkRequireRateLimiting("fixed")
        .WithName("CreateTodo");

        app.MapPut("/{id}", (int id, [FromBody] UpdateTodoRequest request, ITodoService store) =>
        {
            var todo = store.Update(id, request);
            return todo is not null
                ? Results.Ok(todo)
                : Results.NotFound(new UnifiedResult<object>(null, "Todo not found", HttpStatusCode.NotFound));
        })
        .WithName("UpdateTodo");

        app.MapDelete("/{id}", (int id, ITodoService store) =>
        {
            return store.Delete(id)
                ? Results.NoContent()
                : Results.NotFound(new UnifiedResult<object>(null, "Todo not found", HttpStatusCode.NotFound));
        })
        .WithName("DeleteTodo");

        app.MapPost("/{id}/complete", (int id, ITodoService store) =>
        {
            var todo = store.SetComplete(id, true);
            return todo is not null
                ? Results.Ok(todo)
                : Results.NotFound(new UnifiedResult<object>(null, "Todo not found", HttpStatusCode.NotFound));
        })
        .WithName("CompleteTodo");
    }
}

/// <summary>
/// Deprecated v1 todos endpoint. Migrate to the latest version.
/// Demonstrates: <see cref="SharkVersionAttribute"/>, <see cref="SharkDeprecatedAttribute"/>.
/// </summary>
[SharkVersion("v1")]
[EndpointGroup("todo")]
[SharkDescription("Todo Management (Legacy)", "Deprecated v1 API. Migrate to latest for current features.")]
[SharkDeprecated]
public sealed class TodoLegacyEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (ITodoService store) =>
        {
            return Results.Ok(store.GetAll());
        });
    }
}

/// <summary>
/// Enhanced v2 todos endpoint with paginated response and metadata.
/// Demonstrates: <see cref="SharkVersionAttribute"/>, enhanced response DTOs.
/// </summary>
[SharkVersion("v2")]
[EndpointGroup("todo")]
[SharkDescription("Todo Management v2", "Enhanced version with paginated response and metadata")]
[SharkResponseType(StatusCodes.Status200OK, typeof(TodoListResponse), "Success")]
public sealed class TodoEnhancedEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", (ITodoService store) =>
        {
            var items = store.GetAll();
            return Results.Ok(new TodoListResponse(items, items.Length, DateTime.UtcNow));
        })
        .SharkCacheOutput("todos")
        .WithName("GetAllTodosV2");

        app.MapGet("/{id}", (int id, ITodoService store) =>
        {
            var todo = store.GetById(id);
            return todo is not null
                ? Results.Ok(todo)
                : Results.NotFound(new UnifiedResult<object>(null, "Todo not found", HttpStatusCode.NotFound));
        })
        .WithName("GetTodoByIdV2");
    }
}
