using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
//using MongoDB.Driver;

namespace Sharkable.Sample;

[SharkEndpoint]
public class TaskInfoVlosiEndpointV2 : ISharkEndpoint
{
    public static void MapTaskInfo( WebApplication app) 
    {
        // var g = app.MapGroup("/api/taskinfo");
        // g.MapGet("/tryme", LetGo);
        // g.MapGet("/fuckme", Showme);
    }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/hello", async ([FromServices]IMonitor monitor) => 
        {
            using var scope = Shark.ServiceScopeFactory.CreateScope();
            var sw = scope.ServiceProvider.GetService<IOptions<SharkOption>>();
            Console.WriteLine(sw?.Value.ApiPrefix);
            return Monitor.GetRandData(6);
        });

        app.MapGet("/init", async ([FromServices]IMonitor monitor) => 
        {
            await monitor.InitTask();
            Console.WriteLine("ok");
        });
    }

    [SharkMethod("tryme/{jl}", SharkHttpMethod.GET)]
    public  void LetGo([FromServices]IMonitor monitor, string jl)
    {
        monitor.Show();
        Console.WriteLine(jl);
        
    }

    [SharkMethod("fuckme/{a}", SharkHttpMethod.GET)]
    public async void Showme([FromServices]IMonitor monitor, int a)
    {
        if(a == 5)
        {
            Console.WriteLine(a);
        }
        Console.WriteLine("fuckme");
        await monitor.InitUser();
    }
}
public static class MongoEndpoint
{
    public static void AddMongoGroup(this WebApplication app)
    {
        app.MapGroup("/api/mongo");
        app.MapGet("/", GetData);
        app.MapGet("/monitor", ([FromServices] IMonitor monitor) =>
        {
            monitor.Show();
        });

        var taskapi = app.MapGroup("/task");
        taskapi.MapGet("/init", async ([FromServices]IMonitor monitor) =>
        {
            await monitor.InitTask();
        });
        taskapi.MapGet("/all", async ([FromServices]IMonitor monitor) =>
        {
            var data = await monitor.GetTasks();
        });
    }

    public static async void GetData()
    {
        
    }
}
