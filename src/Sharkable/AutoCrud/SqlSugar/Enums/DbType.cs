
namespace Sharkable;

/// <summary>Supported database types for SqlSugar connections.</summary>
public enum DbType
{
    /// <summary>MySQL</summary>
    MySql = 0,
    /// <summary>SQL Server</summary>
    SqlServer = 1,
    /// <summary>SQLite</summary>
    Sqlite = 2,
    /// <summary>Oracle</summary>
    Oracle = 3,
    /// <summary>PostgreSQL</summary>
    PostgreSQL = 4,
    /// <summary>DaMeng (DM8)</summary>
    Dm = 5,
    /// <summary>KingbaseES (Kdbndp)</summary>
    Kdbndp = 6,
    /// <summary>ShenTong (Oscar)</summary>
    Oscar = 7,
    /// <summary>MySQL Connector</summary>
    MySqlConnector = 8,
    /// <summary>Microsoft Access</summary>
    Access = 9,
    /// <summary>OpenGauss</summary>
    OpenGauss = 10,
    /// <summary>QuestDB</summary>
    QuestDB = 11,
    /// <summary>Huawei Gauss (HG)</summary>
    HG = 12,
    /// <summary>ClickHouse</summary>
    ClickHouse = 13,
    /// <summary>GBase</summary>
    GBase = 14,
    /// <summary>ODBC</summary>
    Odbc = 15,
    /// <summary>OceanBase for Oracle</summary>
    OceanBaseForOracle = 16,
    /// <summary>TDengine</summary>
    TDengine = 17,
    /// <summary>GaussDB</summary>
    GaussDB = 18,
    /// <summary>OceanBase</summary>
    OceanBase = 19,
    /// <summary>TiDB</summary>
    Tidb = 20,
    /// <summary>Vastbase (Huawei)</summary>
    Vastbase = 21,
    /// <summary>PolarDB (Alibaba)</summary>
    PolarDB = 22,
    /// <summary>Apache Doris</summary>
    Doris = 23,
    /// <summary>Xugu</summary>
    Xugu = 24,
    /// <summary>GoldenDB (ZTE)</summary>
    GoldenDB = 25,
    /// <summary>Custom database type</summary>
    Custom = 900
}
