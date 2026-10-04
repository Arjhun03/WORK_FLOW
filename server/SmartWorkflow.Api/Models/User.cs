using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartWorkflow.Api.Models;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    public string Department { get; set; } = string.Empty;

    private string? _managerId;

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string? ManagerId
    {
        get => string.IsNullOrWhiteSpace(_managerId) ? null : _managerId;
        set => _managerId = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool IsActive { get; set; } = true;
}
