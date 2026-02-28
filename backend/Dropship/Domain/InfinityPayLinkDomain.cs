using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

/// <summary>
/// Domain para rastrear links de pagamento InfinityPay
/// Mapeia um ULID único para múltiplos paymentIds
/// </summary>
public class InfinityPayLinkDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!; // InfinityPayLink#{linkId}
    public string Sk { get; set; } = default!; // META

    // 📋 Identificadores
    public string LinkId { get; set; } = default!; // ULID único
    public string SellerId { get; set; } = default!;
    
    // 💰 Informações do Link
    public List<string> PaymentIds { get; set; } = default!; // Array de payment IDs
    public decimal Amount { get; set; } // Valor total em centavos
    public int PaymentCount { get; set; } // Quantidade de pagamentos

    // 📊 Status e Datas
    public string Status { get; set; } = "pending"; // pending, completed, expired, failed
    public string EntityType { get; set; } = "infinitypay_link";
    public string CreatedAt { get; set; } = default!;
    public string? CompletedAt { get; set; }
    public string Url { get; set; }
}

/// <summary>
/// Builder para criar InfinityPayLinkDomain
/// </summary>
public static class InfinityPayLinkBuilder
{
    public static InfinityPayLinkDomain Create(
        string sellerId,
        string linkId,
        List<string> paymentIds,
        decimal totalAmount,
        string url)
    {
        
        return new InfinityPayLinkDomain
        {
            Pk = $"InfinityPayLink#{linkId}",
            Sk = "META",
            LinkId = linkId,
            SellerId = sellerId,
            PaymentIds = paymentIds,
            Amount = totalAmount,
            PaymentCount = paymentIds.Count,
            Status = "pending",
            EntityType = "infinitypay",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            Url = url
        };
    }
}

/// <summary>
/// Mapper para converter Dictionary do DynamoDB em Domain
/// </summary>
public static class InfinityPayLinkMapper
{
    public static InfinityPayLinkDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new InfinityPayLinkDomain
        {
            Pk           = item.GetS("PK"),
            Sk           = item.GetS("SK"),
            LinkId       = item.GetS("link_id"),
            SellerId     = item.GetS("seller_id"),
            Amount       = item.GetDecimal("amount"),
            PaymentCount = item.GetN<int>("payment_count"),
            Status       = item.GetS("status", "pending"),
            EntityType   = item.GetS("entity_type", "infinitypay"),
            CreatedAt    = item.GetS("created_at", DateTime.UtcNow.ToString("O")),
            CompletedAt  = item.GetSNullable("completed_at"),
            Url          = item.GetS("url"),
            PaymentIds   = item.GetList("payment_ids")
                              .Where(p => p.S != null)
                              .Select(p => p.S!)
                              .ToList()
        };
    }

    /// <summary>
    /// Converte InfinityPayLinkDomain para dicionário pronto para DynamoDB PutItem
    /// </summary>
    public static Dictionary<string, AttributeValue> ToDynamoDb(this InfinityPayLinkDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.Pk } },
            { "SK", new AttributeValue { S = domain.Sk } },
            { "link_id", new AttributeValue { S = domain.LinkId } },
            { "seller_id", new AttributeValue { S = domain.SellerId } },
            { "amount", new AttributeValue { N = domain.Amount.ToString() } },
            { "payment_count", new AttributeValue { N = domain.PaymentCount.ToString() } },
            { "status", new AttributeValue { S = domain.Status } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "created_at", new AttributeValue { S = domain.CreatedAt } },
            { "url", new AttributeValue { S = domain.Url } }
        };

        // Adicionar lista de payment IDs
        if (domain.PaymentIds != null && domain.PaymentIds.Count > 0)
        {
            var paymentIdsList = domain.PaymentIds
                .Select(pid => new AttributeValue { S = pid })
                .ToList();
            
            item["payment_ids"] = new AttributeValue { L = paymentIdsList };
        }

        // Adicionar completed_at se fornecido
        if (!string.IsNullOrWhiteSpace(domain.CompletedAt))
        {
            item["completed_at"] = new AttributeValue { S = domain.CompletedAt };
        }

        return item;
    }
}
