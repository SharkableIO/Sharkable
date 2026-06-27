
namespace Sharkable;

/// <summary>Key initialization strategy for SqlSugar entity tables.</summary>
public enum InitKeyType
{
    /// <summary>Read key metadata from database system tables (legacy, relies on database schema).</summary>
    [Obsolete("Look at the sqlsugar document entity configuration,This method relies on the database and is abandoned")]
    SystemTable,
    /// <summary>Read key metadata from entity class attributes (recommended).</summary>
    Attribute
}
