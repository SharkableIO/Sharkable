namespace Sharkable;

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
