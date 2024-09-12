using SqlSugar;
using System.Diagnostics.CodeAnalysis;

namespace Sharkable.Sample;

[ScopedService]
public class Monitor(ILogger<Monitor> logger, ISqlSugarClient sqlSugarClient) : IMonitor, ISingleton
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public void Show()
    {
        logger.LogInformation("{a}",DateTime.Now.ToLongTimeString());
    }

    public async Task<Todo?> GetTodo(int id)
    {
        var find = await sqlSugarClient.Queryable<Todo>().Where(x=>x.Id == id).FirstAsync();
        return find;
    }

    public async Task InitUser()
    {
        var sampleTodos = new Todo[] {
            new(1, "Walk the dog",DateTime.Now),
            new(2, "Do the dishes", DateTime.Now),
            new(3, "Do the laundry", DateTime.Now.AddDays(1)),
            new(4, "Clean the bathroom",DateTime.Now),
            new(5, "Clean the car", DateTime.Now)
        };
        sqlSugarClient.CodeFirst.InitTables<Todo>();
        await sqlSugarClient.Insertable(sampleTodos.ToList()).ExecuteCommandAsync();
    }

    public async Task<IResult> InitTask()
    {
        var tasks = GetRandData(6);
        sqlSugarClient.CodeFirst.InitTables<TaskInfo>();
        await sqlSugarClient.Insertable(tasks).ExecuteCommandAsync();
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
            Id = Guid.NewGuid().ToString(),
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