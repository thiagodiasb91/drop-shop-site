using Amazon.DynamoDBv2.Model;
using Dropship.Helpers;

namespace Dropship.Domain;

/// <summary>
/// Registra pagamentos pendentes de fornecedores
/// </summary>
public class PaymentQueueDomain
{
    // 🔑 Chaves DynamoDB
    public string PK { get; set; } = default!; 
    public string SK { get; set; } = default!; 

    // 📋 Identificadores
    public string PaymentId { get; set; } = default!; // ULID único
    public string SellerId { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    public string? SupplierName { get; set; } // Nome do fornecedor (enriquecido via service)
    public string OrderSn { get; set; } = default!;

    // 🏪 Shop Info
    public long ShopId { get; set; }

    // 📊 Status e Datas
    public string Status { get; set; } = "pending"; // pending, waiting-payment, paid, failed
    public string EntityType { get; set; } = "payment_queue";
    public string CreatedAt { get; set; } = default!;
    public string? CompletedAt { get; set; }

    public List<PaymentProduct> PaymentProducts { get; set; } = default!;
    public int TotalItems { get; set; }
    public decimal TotalAmount { get; set; }
    public string InfinityPayUrl { get; set; } = default!;
    public string PaymentLinkId { get; set; } = default!;
}

public class PaymentProduct
{
    public string ProductId { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Image { get; set; } = default!;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
}

/// <summary>
/// Builder para criar PaymentQueueDomain
/// </summary>
public static class PaymentQueueBuilder
{
    public static PaymentQueueDomain Create(
        string sellerId,
        string supplierId,
        string orderSn,
        long shopId,
        List<PaymentProduct> products)
    {
        var paymentId = Ulid.NewUlid().ToString();
        
        return new PaymentQueueDomain
        {
            PK = $"Seller#{sellerId}",
            SK = $"Payment#{paymentId}",
            PaymentId = paymentId,
            SellerId = sellerId,
            SupplierId = supplierId,
            OrderSn = orderSn,
            ShopId = shopId,
            TotalItems = products.Count,
            TotalAmount = products.Sum( x=> x.UnitPrice * x.Quantity),
            Status = "pending",
            EntityType = "payment_queue",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            PaymentProducts = products
        };
    }
}

/// <summary>
/// Mapper para converter Dictionary do DynamoDB em Domain
/// </summary>
public static class PaymentQueueMapper
{
    public static PaymentQueueDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new PaymentQueueDomain
        {
            PK             = item.GetS("PK"),
            SK             = item.GetS("SK"),
            PaymentId      = item.GetS("payment_id"),
            SellerId       = item.GetS("seller_id"),
            SupplierId     = item.GetS("supplier_id"),
            OrderSn        = item.GetS("ordersn"),
            ShopId         = item.GetN<long>("shop_id"),
            Status         = item.GetS("status", "pending"),
            EntityType     = item.GetS("entity_type", "payment_queue"),
            CreatedAt      = item.GetS("created_at", DateTime.UtcNow.ToString("O")),
            CompletedAt    = item.GetSNullable("completed_at"),
            TotalItems     = item.GetN<int>("total_items"),
            TotalAmount    = item.GetDecimal("total_amount"),
            InfinityPayUrl = item.GetS("infinity_pay_url"),
            PaymentLinkId  = item.GetS("payment_link_id"),
            PaymentProducts = item.GetList("payment_products")
                .Select(p => new PaymentProduct
                {
                    ProductId = p.M.GetS("product_id"),
                    Sku       = p.M.GetS("sku"),
                    Quantity  = p.M.GetN<int>("quantity"),
                    UnitPrice = p.M.GetDecimal("unit_price"),
                    Image     = p.M.GetS("image"),
                    Name      = p.M.GetS("name"),
                    Color     = p.M.GetS("color"),
                    Size      = p.M.GetS("size"),
                }).ToList()
        };
    }

    /// <summary>
    /// Converte PaymentQueueDomain para dicionário pronto para DynamoDB PutItem
    /// </summary>
    public static Dictionary<string, AttributeValue> ToDynamoDb(this PaymentQueueDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.PK } },
            { "SK", new AttributeValue { S = domain.SK } },
            { "payment_id", new AttributeValue { S = domain.PaymentId } },
            { "seller_id", new AttributeValue { S = domain.SellerId } },
            { "supplier_id", new AttributeValue { S = domain.SupplierId } },
            { "ordersn", new AttributeValue { S = domain.OrderSn } },
            { "shop_id", new AttributeValue { N = domain.ShopId.ToString() } },
            { "status", new AttributeValue { S = domain.Status } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "created_at", new AttributeValue { S = domain.CreatedAt } },
            { "total_items", new AttributeValue { N = domain.TotalItems.ToString() } },
            { "total_amount", new AttributeValue { N = domain.TotalAmount.ToString("F2") } }
        };

        // Adicionar campos opcionais
        if (!string.IsNullOrWhiteSpace(domain.CompletedAt))
        {
            item["completed_at"] = new AttributeValue { S = domain.CompletedAt };
        }

        if (!string.IsNullOrWhiteSpace(domain.InfinityPayUrl))
        {
            item["infinity_pay_url"] = new AttributeValue { S = domain.InfinityPayUrl };
        }
        
        if(!string.IsNullOrWhiteSpace(domain.PaymentLinkId))
        {
            item["payment_link_id"] = new AttributeValue { S = domain.PaymentLinkId };
        }

        // Adicionar lista de produtos se houver
        if (domain.PaymentProducts != null && domain.PaymentProducts.Count > 0)
        {
            var productsList = new List<AttributeValue>();
            
            foreach (var product in domain.PaymentProducts)
            {
                var productMap = new Dictionary<string, AttributeValue>
                {
                    { "product_id", new AttributeValue { S = product.ProductId } },
                    { "sku", new AttributeValue { S = product.Sku } },
                    { "quantity", new AttributeValue { N = product.Quantity.ToString() } },
                    { "unit_price", new AttributeValue { N = product.UnitPrice.ToString("F2") } },
                    { "name", new AttributeValue { S = product.Name } },
                    { "image", new AttributeValue { S = product.Image } },
                    { "color", new AttributeValue { S = product.Color } },
                    { "size", new AttributeValue { S = product.Size } }
                };
                
                productsList.Add(new AttributeValue { M = productMap });
            }
            
            item["payment_products"] = new AttributeValue { L = productsList };
        }

        return item;
    }
}