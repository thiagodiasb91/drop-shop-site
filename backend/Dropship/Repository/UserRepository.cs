using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

public class UserRepository(IAmazonDynamoDB amazonDynamoDb) : DynamoDbRepository(amazonDynamoDb)
{
    public async Task<UserDomain?> GetUser(string email)
    {
        var key = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"User#{email}" } },
            { "SK", new AttributeValue { S = $"META" } }
        };
        var userDynamo = await GetItemAsync(key);

        if (userDynamo == null)
        {
            return null;
        }
        
        return UserMapper.ToDomain(userDynamo);
    }

    public async Task<UserDomain> CreateUserAsync(UserDomain user)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"User#{user.Email}" } },
            { "SK", new AttributeValue { S = "META" } },
            { "id", new AttributeValue { S = user.Id } },
            { "email", new AttributeValue { S = user.Email } },
            { "role", new AttributeValue { S = user.Role } },
            { "entityType", new AttributeValue { S = "user" } }
        };

        await PutItemAsync(item);
        return user;
    }

    public async Task<UserDomain> UpdateUserAsync(UserDomain user)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = $"User#{user.Email}" } },
            { "SK", new AttributeValue { S = "META" } },
            { "id", new AttributeValue { S = user.Id } },
            { "email", new AttributeValue { S = user.Email } },
            { "role", new AttributeValue { S = user.Role } },
            { "entityType", new AttributeValue { S = "user" } }
        };

        if (!string.IsNullOrWhiteSpace(user.ResourceId))
        {
            item.Add("resource_id", new AttributeValue { S = user.ResourceId });
        }

        await PutItemAsync(item);
        return user;
    }

    public async Task SetResourceId(string? userEmail, string supplierId)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            throw new ArgumentException("User email cannot be null or empty", nameof(userEmail));
        
        var user = await GetUser(userEmail);
        
        if (user == null)
            throw new InvalidOperationException($"User with email {userEmail} not found");
        
        user.ResourceId = supplierId;
        await UpdateUserAsync(user);
    }
}