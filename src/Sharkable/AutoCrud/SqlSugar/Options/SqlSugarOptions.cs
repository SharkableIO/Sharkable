
namespace Sharkable;

public class SqlSugarOptions
{
    public InitKeyType InitKeyType = InitKeyType.Attribute;

    public object? ConfigId { get; set; }

    public DbType DbType { get; set; }

    public string? ConnectionString { get; set; }

    public string? DbLinkName { get; set; }

    public bool IsAutoCloseConnection { get; set; }

    public LanguageType LanguageType
    {
        get
        {
            return ErrorMessage.SugarLanguageType;
        }
        set
        {
            ErrorMessage.SugarLanguageType = value;
        }
    }
    public string? IndexSuffix { get; set; }
}
