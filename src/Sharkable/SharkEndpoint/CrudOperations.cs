namespace Sharkable;

/// <summary>
/// Flags enum controlling which CRUD operations are auto-generated for
/// an <see cref="IAutoCrudEntity{T}"/> endpoint.
/// </summary>
/// <remarks>
/// <see cref="List"/> defaults to paginated queries (safe). Use
/// <see cref="ListAll"/> to expose an unrestricted full-dump endpoint —
/// this flag is intentionally excluded from <see cref="All"/>.
/// </remarks>
[Flags]
public enum CrudOperations
{
    /// <summary>No operations.</summary>
    None = 0,

    /// <summary>
    /// <c>GET /{group}?page=1&amp;pageSize=20</c> — paginated list.
    /// Query parameters: <c>page</c> (default 1), <c>pageSize</c> (default 20, max 100).
    /// Response: <c>{ items, total, page, pageSize, totalPages }</c>.
    /// This is the safe default included in <see cref="All"/>.
    /// </summary>
    List = 1 << 0,

    /// <summary><c>GET /{group}/{id}</c> — get by primary key.</summary>
    Get = 1 << 1,

    /// <summary><c>POST /{group}</c> — create new entity.</summary>
    Create = 1 << 2,

    /// <summary><c>PUT /{group}/{id}</c> — update existing entity.</summary>
    Update = 1 << 3,

    /// <summary><c>DELETE /{group}/{id}</c> — delete by primary key.</summary>
    Delete = 1 << 4,

    /// <summary>
    /// <c>GET /{group}?all=true</c> — unrestricted full-dump of the entire
    /// table. <b>Potentially dangerous on large tables.</b> Not included in
    /// <see cref="All"/>. Must be explicitly opted into.
    /// </summary>
    ListAll = 1 << 5,

    /// <summary>
    /// All safe operations (List + Get + Create + Update + Delete).
    /// <see cref="ListAll"/> is intentionally excluded — opt in manually
    /// when full-table dumps are needed.
    /// </summary>
    All = List | Get | Create | Update | Delete,
}
