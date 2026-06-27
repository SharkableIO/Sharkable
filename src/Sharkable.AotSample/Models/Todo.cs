namespace Sharkable.AotSample;

/// <summary>
/// Represents a Todo task with priority tracking and completion status.
/// </summary>
public sealed record Todo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsComplete { get; set; }
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;
    public DateOnly? DueBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Priority level for a Todo item.
/// </summary>
public enum TodoPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Request DTO for creating a new Todo.
/// </summary>
public sealed record CreateTodoRequest(
    string Title,
    string? Description = null,
    TodoPriority Priority = TodoPriority.Medium,
    DateOnly? DueBy = null
);

/// <summary>
/// Request DTO for updating an existing Todo.
/// </summary>
public sealed record UpdateTodoRequest(
    string Title,
    string? Description,
    TodoPriority Priority,
    DateOnly? DueBy
);

/// <summary>
/// Response DTO for API version info.
/// </summary>
public sealed record VersionInfo(
    string Version,
    string Framework,
    bool AotMode,
    string[] EnabledFeatures
);

/// <summary>
/// Paginated list response for v2 API.
/// </summary>
public sealed record TodoListResponse(Todo[] Data, int Total, DateTime Timestamp);

/// <summary>
/// Feature flags response.
/// </summary>
public sealed record FeatureFlagsResponse(
    bool AotMode,
    string ApiPrefix,
    string EndpointFormat,
    bool IdempotencyEnabled,
    bool ValidationEnabled,
    bool HealthChecksEnabled
);
