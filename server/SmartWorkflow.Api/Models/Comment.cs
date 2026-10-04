using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWorkflow.Api.Models;

public class Comment : BaseEntity
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string RequestId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
