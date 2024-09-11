using System;
//using MongoDB.Bson;
//using MongoDB.Bson.Serialization.Attributes;

namespace Sharkable.Sample;

public abstract class MongoEntity
{
    //[BsonId]
    //[BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
}

public abstract class SharkMongoEntity : MongoEntity
{
    /// <summary>
    /// for time series usage
    /// </summary>
    public DateTimeOffset? CreatedTime { get; set; }

    public DateTimeOffset? UpdatedTime { get; set; }

    public bool IsDeleted { get; set; } = false;
}