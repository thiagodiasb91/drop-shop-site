using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

using System.Globalization;
using Newtonsoft.Json;

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
    public static OrderDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        var itemsJson = item.GetSNullable("items");
        List<OrderItemDomain>? orderItems = null;
        
        if (!string.IsNullOrEmpty(itemsJson))
        {
            try { orderItems = JsonConvert.DeserializeObject<List<OrderItemDomain>>(itemsJson); }
            catch { orderItems = null; }
        }

        return new OrderDomain
        {
            Pk              = item.GetS("PK"),
            Sk              = item.GetS("SK"),
            OrderSn         = item.GetS("order_sn"),
            EntityType      = item.GetS("entity_type", "order"),
            ShopId          = item.GetN<long>("shop_id"),
            SellerId        = item.GetS("seller_id"),
            Status          = item.GetS("status"),
            TotalAmount     = item.GetDecimal("total_amount"),
            TotalItems      = item.GetN<int>("total_items"),
            RecipientName   = item.GetS("recipient_name"),
            RecipientPhone  = item.GetS("recipient_phone"),
            DeliveryAddress = item.GetS("delivery_address"),
            DeliveryCity    = item.GetS("delivery_city"),
            DeliveryState   = item.GetS("delivery_state"),
            DeliveryZipcode = item.GetS("delivery_zipcode"),
            InvoiceNumber   = item.GetS("invoice_number"),
            InvoiceStatus   = item.GetS("invoice_status"),
            CreatedAt       = item.GetDateTimeS("created_at"),
            UpdatedAt       = item.GetDateTimeSNullable("updated_at"),
            Items           = orderItems,
        };
    }

    /// <summary>
    /// Converte OrderDomain para Dictionary pronto para salvar no DynamoDB
    /// PK = Orders#{sellerId} | SK = {orderSn}
    /// Os itens são serializados como JSON no campo "items"
    /// </summary>
    public static Dictionary<string, AttributeValue> ToDynamoDb(this OrderDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.Pk } },
            { "SK", new AttributeValue { S = domain.Sk } },
            { "order_sn", new AttributeValue { S = domain.OrderSn } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "shop_id", new AttributeValue { N = domain.ShopId.ToString() } },
            { "seller_id", new AttributeValue { S = domain.SellerId } },
            { "status", new AttributeValue { S = domain.Status } },
            { "total_amount", new AttributeValue { N = domain.TotalAmount.ToString(CultureInfo.InvariantCulture) } },
            { "total_items", new AttributeValue { N = domain.TotalItems.ToString() } },
            { "recipient_name", new AttributeValue { S = domain.RecipientName } },
            { "recipient_phone", new AttributeValue { S = domain.RecipientPhone } },
            { "delivery_address", new AttributeValue { S = domain.DeliveryAddress } },
            { "delivery_city", new AttributeValue { S = domain.DeliveryCity } },
            { "delivery_state", new AttributeValue { S = domain.DeliveryState } },
            { "delivery_zipcode", new AttributeValue { S = domain.DeliveryZipcode } },
            { "invoice_number", new AttributeValue { S = domain.InvoiceNumber } },
            { "invoice_status", new AttributeValue { S = domain.InvoiceStatus } },
            { "items", new AttributeValue { S = JsonConvert.SerializeObject(domain.Items) } },
            { "created_at", new AttributeValue { S = domain.CreatedAt.ToString("O") } }
        };

        if (domain.UpdatedAt.HasValue)
            item["updated_at"] = new AttributeValue { S = domain.UpdatedAt.Value.ToString("O") };

        return item;
    }
}
