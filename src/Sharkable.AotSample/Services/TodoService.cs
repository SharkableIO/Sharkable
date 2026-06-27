namespace Sharkable.AotSample;

/// <summary>
/// In-memory store for Todo items. Thread-safe via <see cref="System.Threading.Lock"/>.
/// </summary>
public interface ITodoService
{
    Todo[] GetAll();
    Todo? GetById(int id);
    Todo Create(CreateTodoRequest request);
    Todo? Update(int id, UpdateTodoRequest request);
    bool Delete(int id);
    Todo? SetComplete(int id, bool complete);
}

/// <summary>
/// Default in-memory implementation of <see cref="ITodoService"/>.
/// </summary>
[SingletonService]
public sealed class TodoStore : ITodoService
{
    private readonly List<Todo> _items = [];
    private int _nextId = 1;
    private readonly Lock _lock = new();

    public Todo[] GetAll()
    {
        lock (_lock)
            return [.. _items];
    }

    public Todo? GetById(int id)
    {
        lock (_lock)
            return _items.Find(t => t.Id == id);
    }

    public Todo Create(CreateTodoRequest request)
    {
        var todo = new Todo
        {
            Id = Interlocked.Increment(ref _nextId) - 1,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueBy = request.DueBy,
            CreatedAt = DateTime.UtcNow,
        };

        lock (_lock)
            _items.Add(todo);

        return todo;
    }

    public Todo? Update(int id, UpdateTodoRequest request)
    {
        lock (_lock)
        {
            var index = _items.FindIndex(t => t.Id == id);
            if (index < 0) return null;

            _items[index] = _items[index] with
            {
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                DueBy = request.DueBy,
            };
            return _items[index];
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
            return _items.RemoveAll(t => t.Id == id) > 0;
    }

    public Todo? SetComplete(int id, bool complete)
    {
        lock (_lock)
        {
            var index = _items.FindIndex(t => t.Id == id);
            if (index < 0) return null;

            _items[index] = _items[index] with { IsComplete = complete };
            return _items[index];
        }
    }
}
