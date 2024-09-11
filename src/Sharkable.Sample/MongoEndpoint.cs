using System;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
//using MongoDB.Driver;

namespace Sharkable.Sample;

//[SharkEndpoint]
public static class TaskInfoEndpoint
{
    public static void MapTaskInfo(this WebApplication app)
    {
        var g = app.MapGroup("/api/taskinfo");
        g.MapGet("/tryme", LetGo);
        g.MapGet("/fuckme", Showme);
    }
    [SharkMethod("tryme", SharkHttpMethod.GET)]
    public static Task LetGo([FromServices]IMonitor monitor)
    {
        monitor.Show();
        Console.WriteLine("letmego");
        return Task.CompletedTask;
    }

    //[SharkMethod("fuckme", SharkHttpMethod.GET)]
    public static async void Showme([FromServices]IMonitor monitor)
    {
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
            return Results.Ok("init task");
        });
        taskapi.MapGet("/all", async ([FromServices]IMonitor monitor) =>
        {
            var data = await monitor.GetTasks();
            return Results.Ok(data);
        });
    }

    public static async Task<IResult> GetData()
    {
        /*var mongoClient = new MongoClient("mongodb://root:123321@10.10.22.162:27017/");
        var mongoBase = mongoClient.GetDatabase("atcer");
        var repo = mongoBase.GetCollection<Notam>("Notam");
        var ds = await repo.Find(x => x.IsDeleted == false && x.Header.CodeA == "ZGHA").FirstOrDefaultAsync();*/

        return Results.Ok("ok");
    }
}
