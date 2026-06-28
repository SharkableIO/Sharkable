namespace Sharkable;

/// <summary>
/// Non-generic marker used by the framework to discover allowed operations
/// at runtime without reflection on the generic parameter.
/// </summary>
public interface IAutoCrudEntityMarker
{
    CrudOperations GetOperations();
}

/// <summary>
/// Marker interface for <see cref="ISharkEndpoint"/> classes that want
/// automatic CRUD route generation for entity type <typeparamref name="T"/>.
/// Implement <see cref="AllowedOperations"/> to suppress specific operations.
/// Override individual operations by defining manual routes in
/// <see cref="ISharkEndpoint.AddRoutes"/> with the same pattern.
/// </summary>
/// <typeparam name="T">The entity type (must have a parameterless constructor).</typeparam>
public interface IAutoCrudEntity<T> : IAutoCrudEntityMarker where T : class, new()
{
    /// <summary>
    /// Which CRUD operations to auto-generate. Default is <see cref="CrudOperations.All"/>.
    /// Return a restricted set to suppress unwanted operations.
    /// </summary>
    CrudOperations AllowedOperations => CrudOperations.All;

    CrudOperations IAutoCrudEntityMarker.GetOperations() => AllowedOperations;
}
