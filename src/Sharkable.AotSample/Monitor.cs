using System;
using SqlSugar;

namespace Sharkable.AotSample;

[ScopedService]
public class Monitor(ILogger<Monitor> logger, ISqlSugarClient sqlSugarClient) : IMonitor, ISingleton
{
    public void Show()
    {
        logger.LogInformation("{a}",DateTime.Now.ToLongTimeString());
    }

    public async Task<Todo?> GetTodo(int id)
    {
        var find = await sqlSugarClient.Queryable<Todo>().Where(x=>x.Id == id).FirstAsync();
        return find;
    }

    public async Task<IResult> InitTask()
    {
        var tasks = GetRandData(6);
        try 
        {
            sqlSugarClient.CodeFirst.InitTables<TaskInfo>();
            var result = await sqlSugarClient.Insertable(tasks).ExecuteCommandAsync();
            Console.WriteLine(result);
        }
        catch(Exception ex)
        {
            return Results.Ok(ex.Message);
        }
       
        return Results.Ok("cooled");
    }

    public async Task<List<TaskInfo>> GetTasks()
    {
        var find = await sqlSugarClient.Queryable<TaskInfo>().ToListAsync();
        return find;
    }

    public static List<TaskInfo> GetRandData(int size)
    {
        return Enumerable.Range(0, size).Select(a => new TaskInfo
        {
            Topic = Guid.NewGuid().ToString(),
            Body = Guid.NewGuid().ToString(),
            Round = a,
            Interval = TaskInterval.RunOnMonth,
            IntervalArgument = Guid.NewGuid().ToString(),
            CreateTime = DateTime.Now,
            LastRunTime = DateTime.Now,
            CurrentRound = a,
            ErrorTimes = a,
            Status = TaskStatus.Completed
        }).ToList();
    }
}

public interface IMonitor
{
    void Show();
    Task<IResult> InitTask();
    Task<List<TaskInfo>> GetTasks();
}