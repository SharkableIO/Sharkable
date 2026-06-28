namespace Sharkable;

/// <summary>
/// Pluggable CRUD generator called by Sharkable when an
/// <see cref="ISharkEndpoint"/> also implements <see cref="IAutoCrudEntity{T}"/>.
/// Register an implementation via DI (typically from a NuGet plugin like
/// <c>Sharkable.AutoCrud.SqlSugar</c>).
/// </summary>
public interface IAutoCrudGenerator
{
    /// <summary>
    /// Generates CRUD routes on the given route builder for the entity type.
    /// Only operations specified in <paramref name="operations"/> are generated.
    /// </summary>
    /// <param name="routes">The endpoint route builder for the group.</param>
    /// <param name="entityType">The entity type from <c>IAutoCrudEntity&lt;T&gt;</c>.</param>
    /// <param name="endpointType">The <see cref="ISharkEndpoint"/> class type.</param>
    /// <param name="operations">The allowed operations, as declared by the endpoint.</param>
    void GenerateRoutes(IEndpointRouteBuilder routes, Type entityType, Type endpointType, CrudOperations operations);
}
