using System.Text.Json.Serialization;
using Sharkable;

var builder = WebApplication.CreateSlimBuilder(args);


builder.Services.AddShark(opt =>
{
    opt.Format = EndpointFormat.Tolower;
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(1, AppJsonSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(2, AppJsonSerializerContext2.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(3, MyUnifiedResultContext.Default);
});
var app = builder.Build();
app.UseShark();
var sampleTodos = new Todo[]
{
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.Run();


public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(Task<Todo[]>))]
[JsonSerializable(typeof(string))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
[JsonSerializable(typeof(Todo))]
internal partial class AppJsonSerializerContext2 : JsonSerializerContext
{
}

[JsonSerializable(typeof(UnifiedResult<Todo[]>))]
[JsonSerializable(typeof(UnifiedResult<Todo>))]
public partial class MyUnifiedResultContext : JsonSerializerContext
{
    
}