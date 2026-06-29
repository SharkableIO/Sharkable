namespace Sharkable;

/// <summary>
/// Marker interface for entities that support soft deletion.
/// AutoCrud automatically converts <see cref="CrudOperations.Delete"/>
/// to soft delete and appends <c>IsDeleted = false</c> to all read queries.
/// The entity must have a <c>bool IsDeleted</c> property (case-insensitive).
/// </summary>
public interface ISoftDeletable { }
