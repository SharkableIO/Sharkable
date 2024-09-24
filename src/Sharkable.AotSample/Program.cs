using Microsoft.Extensions.Options;
using Sharkable;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
//builder.Services.AddDynamicShark();
builder.Services.AddShark([typeof(Program).Assembly, typeof(Sharkable.AutoCrudSqlSugar).Assembly], opt=>{
    opt.Format = Sharkable.EndpointFormat.Tolower;
    opt.ConfigureAutoCrud(s =>
    {
        s.IsAutoCloseConnection = true;
        s.DbType = DbType.Sqlite;
        s.ConnectionString = "DataSource=testsample.db";
    });
});
var app = builder.Build();

app.UseShark(opt=>{
    opt.ConfigureSwaggerOptions(s =>
    {
        
    });
});

var sopt = Shark.Services.GetService<IOptions<SharkOption>>();
Console.WriteLine(sopt?.Value.AotMode);
var sampleTodos = new Todo[] {
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
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
