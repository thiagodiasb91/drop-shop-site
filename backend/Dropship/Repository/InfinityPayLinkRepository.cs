using Amazon.DynamoDBv2.Model;
using Dropship.Domain;

namespace Dropship.Repository;

/// <summary>
/// Repositório para operações com InfinityPayLink no DynamoDB
/// Gerencia links de pagamento gerados para InfinityPay
/// </summary>
public class InfinityPayLinkRepository
{
    private readonly DynamoDbRepository _repository;
    private readonly ILogger<InfinityPayLinkRepository> _logger;

    public InfinityPayLinkRepository(DynamoDbRepository repository, ILogger<InfinityPayLinkRepository> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Cria um novo link de pagamento InfinityPay
    /// </summary>
    public async Task CreateLinkAsync(InfinityPayLinkDomain link)
    {
        _logger.LogInformation(
            "Creating InfinityPay link - LinkId: {LinkId}, SellerId: {SellerId}, PaymentCount: {PaymentCount}",
            link.LinkId, link.SellerId, link.PaymentCount);

        try
        {
            var item = link.ToDynamoDb();
            await _repository.PutItemAsync(item);

            _logger.LogInformation(
                "InfinityPay link created successfully - LinkId: {LinkId}, Amount: {Amount}",
                link.LinkId, link.Amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating InfinityPay link - LinkId: {LinkId}, SellerId: {SellerId}",
                link.LinkId, link.SellerId);
            throw;
        }
    }

    /// <summary>
    /// Obtém um link pelo LinkId
    /// </summary>
    public async Task<InfinityPayLinkDomain?> GetLinkByIdAsync(string linkId)
    {
        _logger.LogInformation("Getting InfinityPay link by ID - LinkId: {LinkId}", linkId);

        try
        {
            var item = await _repository.GetItemAsync(new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue { S = $"InfinityPayLink#{linkId}" } },
                { "SK", new AttributeValue { S = "META" } }
            });

            if (item == null || item.Count == 0)
            {
                _logger.LogWarning("InfinityPay link not found - LinkId: {LinkId}", linkId);
                return null;
            }

            return InfinityPayLinkMapper.ToDomain(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting InfinityPay link - LinkId: {LinkId}", linkId);
            throw;
        }
    }

    /// <summary>
    /// Obtém todos os links de um vendedor
    /// </summary>
    public async Task<List<InfinityPayLinkDomain>> GetLinksBySellerAsync(string sellerId)
    {
        _logger.LogInformation("Getting InfinityPay links by seller - SellerId: {SellerId}", sellerId);

        try
        {
            var items = await _repository.QueryTableAsync(
                keyConditionExpression: "PK = :pk AND SK = :sk",
                expressionAttributeValues: new Dictionary<string, AttributeValue>
                {
                    { ":pk", new AttributeValue { S = $"Seller#{sellerId}" } },
                    { ":sk", new AttributeValue { S = "InfinityPayLink#" } }
                },
                indexName: "GSI_RELATIONS"
            );

            var links = items.Select(InfinityPayLinkMapper.ToDomain).ToList();
            
            _logger.LogInformation("Found {Count} InfinityPay links for seller - SellerId: {SellerId}",
                links.Count, sellerId);

            return links;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting InfinityPay links by seller - SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    /// <summary>
    /// Atualiza o status de um link
    /// </summary>
    public async Task<bool> UpdateLinkStatusAsync(string linkId, string status, string? completedAt = null)
    {
        _logger.LogInformation(
            "Updating InfinityPay link status - LinkId: {LinkId}, Status: {Status}",
            linkId, status);

        try
        {
            var link = await GetLinkByIdAsync(linkId);
            if (link == null)
            {
                _logger.LogWarning("InfinityPay link not found for update - LinkId: {LinkId}", linkId);
                return false;
            }

            link.Status = status;
            link.CompletedAt = completedAt;
            
            await _repository.PutItemAsync(link.ToDynamoDb());
            _logger.LogInformation(
                "InfinityPayLink status updated successfully - LinkId: {LinkId}, Status: {Status}",
                linkId, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating InfinityPay link status - LinkId: {LinkId}, Status: {Status}",
                linkId, status);
            throw;
        }
    }
}

