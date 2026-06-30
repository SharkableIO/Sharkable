
namespace Sharkable;

/// <summary>Configuration options for SqlSugar database connection and initialization.</summary>
public class SqlSugarOptions
{
    /// <summary>Key initialization strategy. Defaults to <see cref="InitKeyType.Attribute"/>.</summary>
    public InitKeyType InitKeyType { get; set; } = InitKeyType.Attribute;

    /// <summary>Optional configuration identifier for multi-tenant databases.</summary>
    public object? ConfigId { get; set; }

    /// <summary>The database type (MySQL, SqlServer, PostgreSQL, etc.).</summary>
    public DbType DbType { get; set; }

    /// <summary>The database connection string.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Display name for the database connection link.</summary>
    public string? DbLinkName { get; set; }

    /// <summary>Whether to automatically close the connection after each operation. Defaults to false.</summary>
    public bool IsAutoCloseConnection { get; set; }

    /// <summary>Language for error messages (Default / Chinese / English).</summary>
    public LanguageType LanguageType
    {
        get => ErrorMessage.SugarLanguageType;
        set => ErrorMessage.SugarLanguageType = value;
    }

    /// <summary>Optional suffix for index names.</summary>
    public string? IndexSuffix { get; set; }

    /// <summary>Maximum page size for AutoCrud list endpoints. Default: <c>100</c>.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Default page size for AutoCrud list endpoints. Default: <c>20</c>.</summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Field name used for soft delete filtering. Default: <c>"IsDeleted"</c>.
    /// Only used when the entity implements <see cref="ISoftDeletable"/>.
    /// </summary>
    public string SoftDeleteFieldName { get; set; } = "IsDeleted";
}
