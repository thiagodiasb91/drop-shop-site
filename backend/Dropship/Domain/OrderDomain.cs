namespace Dropship.Domain;

using System.Globalization;
using Newtonsoft.Json;

// ...existing code...

/// <summary>
/// Domínio para representar um Pedido
/// Armazena informações básicas do pedido, endereço de entrega e data
/// </summary>
public class OrderDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!;
    public string Sk { get; set; } = default!;

    // 📌 Identificadores
    // PK = Orders#{sellerId} | SK = {orderSn}
    // OrderSn == OrderId (são o mesmo valor)
    public string OrderSn { get; set; } = default!;
    public string EntityType { get; set; } = "order";

    // 👥 Relacionamentos
    public long ShopId { get; set; }
    public string SellerId { get; set; } = default!;

    // 📦 Informações do Pedido
    public string Status { get; set; } = default!; // READY_TO_SHIP, SHIPPED, DELIVERED, etc
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }

    // 📍 Endereço de Entrega
    public string RecipientName { get; set; } = default!;
    public string RecipientPhone { get; set; } = default!;
    public string DeliveryAddress { get; set; } = default!;
    public string DeliveryCity { get; set; } = default!;
    public string DeliveryState { get; set; } = default!;
    public string DeliveryZipcode { get; set; } = default!;

    // 🧾 Fatura
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceStatus { get; set; } = string.Empty; // UNISSUED, ISSUED, VOIDED

    // 🛒 Itens do Pedido
    public List<OrderItemDomain> Items { get; set; } = new();

    // 📅 Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Representa um item dentro de um pedido
/// Dados relevantes para exibição em tela e impressão
/// </summary>
public class OrderItemDomain
{
    public string ProductId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public decimal ProductionPrice { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
}

/// <summary>
/// Mapper para converter Dictionary (DynamoDB) para OrderDomain
/// </summary>
public static class OrderMapper
{
    public static OrderDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        var createdAtString = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O");
        DateTime.TryParse(createdAtString, null, DateTimeStyles.RoundtripKind, out var createdAt);

        var updatedAtString = item.ContainsKey("updated_at") ? item["updated_at"].S : null;
        DateTime.TryParse(updatedAtString, null, DateTimeStyles.RoundtripKind, out var updatedAt);

        var pk = item.ContainsKey("PK") ? item["PK"].S : "";
        var sk = item.ContainsKey("SK") ? item["SK"].S : "";

        // Desserializar itens do JSON armazenado
        var itemsJson = item.ContainsKey("items") ? item["items"].S : null;
        var items = string.IsNullOrEmpty(itemsJson)
            ? new List<OrderItemDomain>()
            : JsonConvert.DeserializeObject<List<OrderItemDomain>>(itemsJson) ?? new List<OrderItemDomain>();

        return new OrderDomain
        {
            Pk = pk,
            Sk = sk,
            OrderSn = sk,
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "order",
            ShopId = item.ContainsKey("shop_id") ? long.Parse(item["shop_id"].N) : 0,
            SellerId = item.ContainsKey("seller_id") ? item["seller_id"].S : "",
            Status = item.ContainsKey("status") ? item["status"].S : "",
            TotalAmount = item.ContainsKey("total_amount") ? decimal.Parse(item["total_amount"].N, CultureInfo.InvariantCulture) : 0,
            TotalItems = item.ContainsKey("total_items") ? int.Parse(item["total_items"].N) : 0,
            RecipientName = item.ContainsKey("recipient_name") ? item["recipient_name"].S : "",
            RecipientPhone = item.ContainsKey("recipient_phone") ? item["recipient_phone"].S : "",
            DeliveryAddress = item.ContainsKey("delivery_address") ? item["delivery_address"].S : "",
            DeliveryCity = item.ContainsKey("delivery_city") ? item["delivery_city"].S : "",
            DeliveryState = item.ContainsKey("delivery_state") ? item["delivery_state"].S : "",
            DeliveryZipcode = item.ContainsKey("delivery_zipcode") ? item["delivery_zipcode"].S : "",
            InvoiceNumber = item.ContainsKey("invoice_number") ? item["invoice_number"].S : "",
            InvoiceStatus = item.ContainsKey("invoice_status") ? item["invoice_status"].S : "",
            Items = items,
            CreatedAt = createdAt,
            UpdatedAt = updatedAtString != null ? updatedAt : null
        };
    }

    /// <summary>
    /// Converte OrderDomain para Dictionary pronto para salvar no DynamoDB
    /// PK = Orders#{sellerId} | SK = {orderSn}
    /// Os itens são serializados como JSON no campo "items"
    /// </summary>
    public static Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> ToDynamoDb(this OrderDomain domain)
    {
        var item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
        {
            { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Pk } },
            { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Sk } },
            { "order_sn", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.OrderSn } },
            { "entity_type", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.EntityType } },
            { "shop_id", new Amazon.DynamoDBv2.Model.AttributeValue { N = domain.ShopId.ToString() } },
            { "seller_id", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.SellerId } },
            { "status", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Status } },
            { "total_amount", new Amazon.DynamoDBv2.Model.AttributeValue { N = domain.TotalAmount.ToString(CultureInfo.InvariantCulture) } },
            { "total_items", new Amazon.DynamoDBv2.Model.AttributeValue { N = domain.TotalItems.ToString() } },
            { "recipient_name", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.RecipientName } },
            { "recipient_phone", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.RecipientPhone } },
            { "delivery_address", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.DeliveryAddress } },
            { "delivery_city", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.DeliveryCity } },
            { "delivery_state", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.DeliveryState } },
            { "delivery_zipcode", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.DeliveryZipcode } },
            { "invoice_number", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.InvoiceNumber } },
            { "invoice_status", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.InvoiceStatus } },
            { "items", new Amazon.DynamoDBv2.Model.AttributeValue { S = JsonConvert.SerializeObject(domain.Items) } },
            { "created_at", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.CreatedAt.ToString("O") } }
        };

        if (domain.UpdatedAt.HasValue)
            item["updated_at"] = new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.UpdatedAt.Value.ToString("O") };

        return item;
    }
}

