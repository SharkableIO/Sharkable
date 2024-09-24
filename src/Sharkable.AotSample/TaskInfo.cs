using System;

namespace Sharkable.AotSample;

using global::SqlSugar;
public enum TaskStatus { Running, Paused, Completed }
public enum TaskInterval { SEC = 1, RunOnDay = 11, RunOnWeek = 12, RunOnMonth = 13, Custom = 21 }
public class TaskInfo
{
    public int Id { get; set; }
    public string? Topic { get; set; }
    public string? Body { get; set; }
    public int? Round { get; set; }
    public TaskInterval Interval { get; set; }
    public string? IntervalArgument { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? LastRunTime { get; set; }

    public int? CurrentRound { get; set; }
    public int? ErrorTimes { get; set; }
    public TaskStatus Status { get; set; }

    public override string ToString() =>
        $"{Id},{Topic},{Body},{Round},{Interval},{IntervalArgument},{CreateTime?.ToString("yyyy-MM-dd HH:mm:ss")},{LastRunTime?.ToString("yyyy-MM-dd HH:mm:ss")}" +
        $",{CurrentRound},{ErrorTimes},{Status}";
}

