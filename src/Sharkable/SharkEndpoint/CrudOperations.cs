namespace Sharkable;

/// <summary>
/// Flags enum controlling which CRUD operations are auto-generated for
/// an <see cref="IAutoCrudEntity{T}"/> endpoint.
/// </summary>
[Flags]
public enum CrudOperations
{
    /// <summary>No operations.</summary>
    None = 0,

    /// <summary><c>GET /{group}</c> — list all entities.</summary>
    List = 1 << 0,

    /// <summary><c>GET /{group}/{id}</c> — get by primary key.</summary>
    Get = 1 << 1,

    /// <summary><c>POST /{group}</c> — create new entity.</summary>
    Create = 1 << 2,

    /// <summary><c>PUT /{group}/{id}</c> — update existing entity.</summary>
    Update = 1 << 3,

    /// <summary><c>DELETE /{group}/{id}</c> — delete by primary key.</summary>
    Delete = 1 << 4,

    /// <summary>All operations.</summary>
    All = List | Get | Create | Update | Delete,
}
