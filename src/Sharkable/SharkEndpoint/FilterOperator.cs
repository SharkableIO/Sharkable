namespace Sharkable;

/// <summary>
/// Standard comparison operators for AutoCrud query filtering.
/// Used with the <c>filter[field][op]=value</c> URL convention.
/// </summary>
public enum FilterOperator
{
    /// <summary>Exact match (default when no operator is specified).</summary>
    Eq,

    /// <summary>Not equal: <c>filter[price][ne]=0</c></summary>
    Ne,

    /// <summary>Greater than: <c>filter[price][gt]=100</c></summary>
    Gt,

    /// <summary>Greater than or equal: <c>filter[price][gte]=100</c></summary>
    Gte,

    /// <summary>Less than: <c>filter[price][lt]=500</c></summary>
    Lt,

    /// <summary>Less than or equal: <c>filter[price][lte]=500</c></summary>
    Lte,

    /// <summary>LIKE match: <c>filter[name][like]=Widget%</c></summary>
    Like,

    /// <summary>IN list (comma-separated): <c>filter[status][in]=active,pending</c></summary>
    In,

    /// <summary>NOT IN list: <c>filter[status][nin]=deleted</c></summary>
    Nin,

    /// <summary>
    /// IS NULL (set value to <c>true</c>):
    /// <c>filter[deleted][null]=true</c>. To check IS NOT NULL, set to
    /// <c>false</c>: <c>filter[deleted][null]=false</c>.
    /// </summary>
    Null,
}
