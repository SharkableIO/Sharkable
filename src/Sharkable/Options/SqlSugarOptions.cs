using System.Text.Json.Serialization;

namespace Sharkable;
public enum InitKeyType
{
    [Obsolete("Look at the sqlsugar document entity configuration,This method relies on the database and is abandoned")]
    SystemTable,
    Attribute
}

public class ConnMoreSettings
{
    public bool IsAutoRemoveDataCache { get; set; }

    public bool IsWithNoLockQuery { get; set; }

    public bool DisableWithNoLockWithTran { get; set; }

    public bool IsWithNoLockSubquery { get; set; }

    public bool DisableNvarchar { get; set; }

    public bool DisableMillisecond { get; set; }

    public bool PgSqlIsAutoToLower { get; set; } = true;


    public bool PgSqlIsAutoToLowerCodeFirst { get; set; } = true;


    public bool EnableILike { get; set; }

    public bool IsAutoToUpper { get; set; } = true;


    public int DefaultCacheDurationInSeconds { get; set; }

    public bool? TableEnumIsString { get; set; }

    public DateTime? DbMinDate { get; set; } = DateTime.MinValue.Date.AddYears(1899);


    public bool IsNoReadXmlDescription { get; set; }

    public bool SqlServerCodeFirstNvarchar { get; set; }

    public bool OracleCodeFirstNvarchar2 { get; set; }

    public bool SqliteCodeFirstEnableDefaultValue { get; set; }

    public bool SqliteCodeFirstEnableDescription { get; set; }

    public bool IsAutoUpdateQueryFilter { get; set; }

    public bool IsAutoDeleteQueryFilter { get; set; }

    public bool EnableModelFuncMappingColumn { get; set; }

    public bool EnableOracleIdentity { get; set; }

    public bool EnableCodeFirstUpdatePrecision { get; set; }

    public bool SqliteCodeFirstEnableDropColumn { get; set; }

    public bool IsCorrectErrorSqlParameterName { get; set; }

    public int MaxParameterNameLength { get; set; }

    public bool DisableQueryWhereColumnRemoveTrim { get; set; }

    public DbType? DatabaseModel { get; set; }
}
public class SlaveConnectionConfig
{
    public int HitRate = 1;

    public string ConnectionString { get; set; }
}
internal static class ErrorMessage
{
    internal static string OperatorError => GetThrowMessage("Lambda parsing error: {0} does not support the operator to find!", "拉姆达解析出错：不支持{0}此种运算符查找！");

    internal static string ExpFileldError => GetThrowMessage("Expression format error, correct format: it=>it.fieldName", "表达式格式错误，正确格式： it=>it.fieldName");

    internal static string MethodError => GetThrowMessage("Expression parsing does not support the current function {0}. There are many functions available in the SqlFunc class, for example, it=>SqlFunc.HasValue(it.Id)", "拉姆达解析不支持当前函数{0}，SqlFunc这个类里面有大量函数可用,也许有你想要的，例如： it=>SqlFunc.HasValue(it.Id)");

    public static string ConnnectionOpen => GetThrowMessage("Connection open error . {0} ", " 连接数据库过程中发生错误，检查服务器是否正常连接字符串是否正确，错误信息：{0}.");

    public static string ExpressionCheck => GetThrowMessage("Join {0} needs to be the same as {1} {2}", "多表查询存在别名不一致,请把{1}中的{2}改成{0}就可以了，特殊需求可以使用.Select((x,y)=>new{{ id=x.id,name=y.name}}).MergeTable().Orderby(xxx=>xxx.Id)功能将Select中的多表结果集变成单表，这样就可以不限制别名一样");

    public static string WhereIFCheck => GetThrowMessage("Subquery.WhereIF.IsWhere {0} not supported", "Subquery.WhereIF 第一个参数不支持表达式中的变量，只支持外部变量");

    internal static LanguageType SugarLanguageType { get; set; }

    internal static string ObjNotExist => GetThrowMessage("{0} does not exist.", "{0}不存在。");

    internal static string EntityMappingError => GetThrowMessage("Entity mapping error.{0}", "Select 实体与表映射出错,可以注释实体类中的字段排查具体哪一个字段。【注意：如果用CodeFirt先配置禁止删列或更新】 {0}");

    public static string NotSupportedDictionary => GetThrowMessage("This type of Dictionary is not supported for the time being. You can try Dictionary<string, string>, or contact the author!!", "暂时不支持该类型的Dictionary 你可以试试 Dictionary<string ,string>或者联系作者！！");

    public static string NotSupportedArray => GetThrowMessage("This type of Array is not supported for the time being. You can try object[] or contact the author!!", "暂时不支持该类型的Array 你可以试试 object[] 或者联系作者！！");

    internal static string GetThrowMessage(string enMessage, string cnMessage, params string[] args)
    {
        if (SugarLanguageType == LanguageType.Default)
        {
            List<string> list = new List<string> { enMessage, cnMessage };
            list.AddRange(args);
            object[] args2 = list.ToArray();
            return string.Format("中文提示 : {1}\r\nEnglish Message : {0}", args2);
        }

        if (SugarLanguageType == LanguageType.English)
        {
            return enMessage;
        }

        return cnMessage;
    }
}
public enum LanguageType
{
    Default,
    Chinese,
    English
}

public enum DbType
{
    MySql = 0,
    SqlServer = 1,
    Sqlite = 2,
    Oracle = 3,
    PostgreSQL = 4,
    Dm = 5,
    Kdbndp = 6,
    Oscar = 7,
    MySqlConnector = 8,
    Access = 9,
    OpenGauss = 10,
    QuestDB = 11,
    HG = 12,
    ClickHouse = 13,
    GBase = 14,
    Odbc = 15,
    OceanBaseForOracle = 16,
    TDengine = 17,
    GaussDB = 18,
    OceanBase = 19,
    Tidb = 20,
    Vastbase = 21,
    PolarDB = 22,
    Doris = 23,
    Xugu = 24,
    GoldenDB = 25,
    Custom = 900
}

public class SqlSugarOptions
{
    public InitKeyType InitKeyType = InitKeyType.Attribute;

    public object ConfigId { get; set; }

    public DbType DbType { get; set; }

    public string ConnectionString { get; set; }

    public string DbLinkName { get; set; }

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

    public List<SlaveConnectionConfig> SlaveConnectionConfigs { get; set; }

    public ConnMoreSettings MoreSettings { get; set; }

    public string IndexSuffix { get; set; }
}
