using Sharkable.Sample;
using Microsoft.AspNetCore.Mvc;
using Sharkable;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateSlimBuilder(args);//.Sharkable([typeof(App).Assembly]);

builder.Services.AddShark( opt=>{
    opt.Format = EndpointFormat.SnakeCase;
});
builder.Services.AddSampleDataService();

var app = builder.Build();

var sampleTodos = new Todo[] {
            new(1, "Walk the dog",DateTime.Now),
            new(2, "Do the dishes", DateTime.Now),
            new(3, "Do the laundry", DateTime.Now.AddDays(1)),
            new(4, "Clean the bathroom",DateTime.Now),
            new(5, "Clean the car", DateTime.Now)
        };
app.AddMongoGroup();
app.UseShark();

var sopt = Shark.Services.GetService<IOptions<SharkOption>>();
Console.WriteLine(sopt?.Value.AotMode);
var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/init", async ([FromServices] IMonitor monitor) =>
{
    await monitor.InitUser();
});
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id, [FromServices] IMonitor monitor) =>
{
    var todo = monitor.GetTodo(id);
    return todo;
});

todosApi.MapGet("/love", ([FromServices]IMonitor monitor)=>
{
    monitor.Show();
});

app.Run();