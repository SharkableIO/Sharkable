namespace Sharkable.Sample;

public interface IMonitor {
    void Show();
    Task<Todo?> GetTodo(int id);
    Task InitUser();
    Task<List<TaskInfo>> GetTasks();
    Task<IResult> InitTask();
}