using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

public class UserDomain
{
    public string PK { get; set; }
    public string SK { get; set; }
    
    public string Email { get; set; }
    public string Id { get; set; }
    public string Role { get; set; }
    public string? ResourceId { get; set; }
}

public static class UserMapper
{
    public static UserDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new UserDomain
        {
            PK         = item.GetS("PK"),
            SK         = item.GetS("SK"),
            Email      = item.GetS("email"),
            Id         = item.GetS("id"),
            Role       = item.GetS("role"),
            ResourceId = item.GetSNullable("resource_id"),
        };
    }
}