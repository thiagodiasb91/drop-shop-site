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
    public static UserDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        var resourceId = item.ContainsKey("resource_id") ? item["resource_id"].S : null;
        
        return new UserDomain 
        {
            PK = item["PK"].S,
            SK = item["SK"].S,
            Email = item["email"].S,
            Id = item["id"].S,
            Role = item["role"].S,
            ResourceId = resourceId
        };
    }
}