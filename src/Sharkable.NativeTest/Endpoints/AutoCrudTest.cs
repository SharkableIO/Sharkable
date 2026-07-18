using Sharkable;
using Sharkable.AutoCrud.SqlSugar;
using SqlSugar;

namespace Sharkable.NativeTest;

[SugarTable("test_items")]
public class TestItem
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [CrudAllow]
    public string Name { get; set; } = "";
}

public class AutoCrudTestEndpoint : ISharkEndpoint, IAutoCrudEntity<TestItem>
{
    // No AddRoutes needed — AutoCrud generates all routes
    // Table is created via CodeFirst in Program.cs
}
