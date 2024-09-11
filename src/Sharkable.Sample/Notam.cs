using System;
using System.Text.Json.Serialization;

namespace Sharkable.Sample;

/// <summary>
/// 航行通告
/// </summary>
public class Notam : SharkMongoEntity
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    [JsonPropertyName("sendAddress")]
    public string? SendAddress{ get; set; }
    [JsonPropertyName("originAddress")]
    public string? OriginAddress{ get; set; }
    [JsonPropertyName("level")]
    public string? Level { get; set; }
    [JsonPropertyName("header")]
    public Header Header { get; set; } = new Header();
    [JsonPropertyName("qualification")]
    public Qualification? Qualification { get; set; }

    [JsonPropertyName("schedule")]
    public Schedule? Schedule { get; set; }

    [JsonPropertyName("content")]
    public Content? Content { get; set; }

    [JsonPropertyName("limits")]
    public Limits? Limits { get; set; }
    public string? Source { get; set; }
}

public class Area
{
    [JsonPropertyName("point")]
    public ATCPoint? Point { get; set; }
}

public class Code
{
    [JsonPropertyName("code")]
    public string? ItemCode { get; set; }

    [JsonPropertyName("entity")]
    public string? Entity { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    [JsonPropertyName("subArea")]
    public string? SubArea { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    [JsonPropertyName("modifier")]
    public string? Modifier { get; set; }
}

public class Content
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("area")]
    public List<Area>? Area { get; set; }

    public int? PageSize { get; set; }

    public int? PageIndex { get; set; }
}

public struct ATCPoint
{
    [JsonPropertyName("lat")]
    public double? Lat { get; set; }
    [JsonPropertyName("lng")]
    public double? Lng { get; set; }
}

public class Coordinates
{
    [JsonPropertyName("point")]
    public ATCPoint? Point { get; set; }

    [JsonPropertyName("radius")]
    public double? Radius { get; set; }
}

public class Header
{
    [JsonPropertyName("notamId")]
    public string? NotamId { get; set; }

    [JsonPropertyName("series")]
    public string? Series { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("year")]
    public string? Year { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("typeDesc")]
    public string? TypeDesc { get; set; }
    
    [JsonPropertyName("refNotam")]
    public string? RefNotam { get; set; }
    [JsonPropertyName("codeA")]
    public string? CodeA{ get; set; }
}

public class Limits
{
    [JsonPropertyName("lower")]
    public string? Lower { get; set; }

    [JsonPropertyName("upper")]
    public string? Upper { get; set; }
}

public class Purpose
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class Qualification
{
    [JsonPropertyName("line")]
    public string? Line { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("code")]
    public Code? Code { get; set; }

    [JsonPropertyName("traffic")]
    public List<Traffic>? Traffic { get; set; }

    [JsonPropertyName("purpose")]
    public List<Purpose>? Purpose { get; set; }

    [JsonPropertyName("scope")]
    public List<Scope>? Scope { get; set; }

    [JsonPropertyName("coordinates")]
    public Coordinates? Coordinates { get; set; }

    [JsonPropertyName("limits")]
    public Limits? Limits { get; set; }
}

public class Schedule
{
    [JsonPropertyName("activityStart")]
    public DateTime? ActivityStart { get; set; }

    [JsonPropertyName("validityEnd")]
    public DateTime? ValidityEnd { get; set; }
    public bool Estimated { get; set; } = false;
    public bool Permanent { get; set; } = false;
    
    [JsonPropertyName("elements")]
    public List<string>? Elements { get; set; }
}

public class Scope
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class Traffic
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
