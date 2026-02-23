using Amazon.DynamoDBv2.Model;

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

    // 🔗 Informações do Checkout
    public string CheckoutUrl { get; set; } = default!; // URL gerada para InfinityPay
    public string? WebhookOrderNsu { get; set; } // orderNsu que virá no webhook
}

/// <summary>
/// Builder para criar InfinityPayLinkDomain
/// </summary>
public static class InfinityPayLinkBuilder
{
    public static InfinityPayLinkDomain Create(
        string sellerId,
        List<string> paymentIds,
        decimal totalAmount)
    {
        var linkId = Ulid.NewUlid().ToString();
        
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
            EntityType = "infinitypay_link",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            // orderNsu será: paymentId1-paymentId2-...-paymentIdN
            WebhookOrderNsu = string.Join("-", paymentIds),
            CheckoutUrl = string.Empty // Será preenchido após retornar da API
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
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",
            LinkId = item.ContainsKey("link_id") ? item["link_id"].S : "",
            SellerId = item.ContainsKey("seller_id") ? item["seller_id"].S : "",
            PaymentIds = item.ContainsKey("payment_ids") && item["payment_ids"].L != null
                ? item["payment_ids"].L
                    .Where(p => p.S != null)
                    .Select(p => p.S)
                    .ToList()
                : new List<string>(),
            Amount = item.ContainsKey("amount") ? decimal.Parse(item["amount"].N) : 0,
            PaymentCount = item.ContainsKey("payment_count") ? int.Parse(item["payment_count"].N) : 0,
            Status = item.ContainsKey("status") ? item["status"].S : "pending",
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "infinitypay_link",
            CreatedAt = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O"),
            CompletedAt = item.ContainsKey("completed_at") ? item["completed_at"].S : null,
            CheckoutUrl = item.ContainsKey("checkout_url") ? item["checkout_url"].S : "",
            WebhookOrderNsu = item.ContainsKey("webhook_order_nsu") ? item["webhook_order_nsu"].S : ""
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
            { "amount", new AttributeValue { N = domain.Amount.ToString("F2") } },
            { "payment_count", new AttributeValue { N = domain.PaymentCount.ToString() } },
            { "status", new AttributeValue { S = domain.Status } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "created_at", new AttributeValue { S = domain.CreatedAt } },
            { "checkout_url", new AttributeValue { S = domain.CheckoutUrl } },
            { "webhook_order_nsu", new AttributeValue { S = domain.WebhookOrderNsu ?? "" } }
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

