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
    public string Name { get; set; }
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
    public static PaymentQueueDomain ToDomain(this Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> item)
    {
        return new PaymentQueueDomain
        {
            PK = item.ContainsKey("PK") ? item["PK"].S : "",
            SK = item.ContainsKey("SK") ? item["SK"].S : "",

            PaymentId = item.ContainsKey("payment_id") ? item["payment_id"].S : "",
            SellerId = item.ContainsKey("seller_id") ? item["seller_id"].S : "",
            SupplierId = item.ContainsKey("supplier_id") ? item["supplier_id"].S : "",
            OrderSn = item.ContainsKey("ordersn") ? item["ordersn"].S : "",
            ShopId = item.ContainsKey("shop_id") && item["shop_id"].N != null ? long.Parse(item["shop_id"].N) : 0,
            Status = item.ContainsKey("status") ? item["status"].S : "pending",
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "payment_queue",
            CreatedAt = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O"),
            CompletedAt = item.ContainsKey("completed_at") ? item["completed_at"].S : null,
            TotalItems = item.ContainsKey("total_items") ? int.Parse(item["total_items"].N) : 0,
            TotalAmount = item.ContainsKey("total_amount") ? decimal.Parse(item["total_amount"].N) : 0,
            InfinityPayUrl = item.ContainsKey("infinity_pay_url") ? item["infinity_pay_url"].S : "",
            PaymentLinkId =  item.ContainsKey("payment_link_id") ? item["payment_link_id"].S : "",
            PaymentProducts = item.ContainsKey("payment_products") && item["payment_products"].L != null
                ? item["payment_products"].L.Select(p => new PaymentProduct
                {
                    ProductId = p.M.ContainsKey("product_id") && p.M["product_id"].S != null ? p.M["product_id"].S : "",
                    Sku = p.M.ContainsKey("sku") && p.M["sku"].S != null ? p.M["sku"].S : "",
                    Quantity = p.M.ContainsKey("quantity") && p.M["quantity"].N != null ? int.Parse(p.M["quantity"].N) : 0,
                    UnitPrice = p.M.ContainsKey("unit_price") && p.M["unit_price"].N != null ? decimal.Parse(p.M["unit_price"].N) : 0,
                    Image = p.M.ContainsKey("image") && p.M["image"].S != null ? p.M["image"].S : "",
                    Name = p.M.ContainsKey("name") && p.M["name"].S != null ? p.M["name"].S : ""
                }).ToList() 
                : new List<PaymentProduct>()   
        };
    }

    /// <summary>
    /// Converte PaymentQueueDomain para dicionário pronto para DynamoDB PutItem
    /// </summary>
    public static Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> ToDynamoDb(this PaymentQueueDomain domain)
    {
        var item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
        {
            { "PK", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.PK } },
            { "SK", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.SK } },
            { "payment_id", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.PaymentId } },
            { "seller_id", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.SellerId } },
            { "supplier_id", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.SupplierId } },
            { "ordersn", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.OrderSn } },
            { "shop_id", new Amazon.DynamoDBv2.Model.AttributeValue { N = domain.ShopId.ToString() } },
            { "status", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.Status } },
            { "entity_type", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.EntityType } },
            { "created_at", new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.CreatedAt } },
            { "total_items", new Amazon.DynamoDBv2.Model.AttributeValue { N = domain.TotalItems.ToString() } },
            { "total_amount", new Amazon.DynamoDBv2.Model.AttributeValue { N = domain.TotalAmount.ToString("F2") } }
        };

        // Adicionar campos opcionais
        if (!string.IsNullOrWhiteSpace(domain.CompletedAt))
        {
            item["completed_at"] = new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.CompletedAt };
        }

        if (!string.IsNullOrWhiteSpace(domain.InfinityPayUrl))
        {
            item["infinity_pay_url"] = new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.InfinityPayUrl };
        }
        
        if(!string.IsNullOrWhiteSpace(domain.PaymentLinkId))
        {
            item["payment_link_id"] = new Amazon.DynamoDBv2.Model.AttributeValue { S = domain.PaymentLinkId };
        }

        // Adicionar lista de produtos se houver
        if (domain.PaymentProducts != null && domain.PaymentProducts.Count > 0)
        {
            var productsList = new List<Amazon.DynamoDBv2.Model.AttributeValue>();
            
            foreach (var product in domain.PaymentProducts)
            {
                var productMap = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                {
                    { "product_id", new Amazon.DynamoDBv2.Model.AttributeValue { S = product.ProductId } },
                    { "sku", new Amazon.DynamoDBv2.Model.AttributeValue { S = product.Sku } },
                    { "quantity", new Amazon.DynamoDBv2.Model.AttributeValue { N = product.Quantity.ToString() } },
                    { "unit_price", new Amazon.DynamoDBv2.Model.AttributeValue { N = product.UnitPrice.ToString("F2") } },
                    { "name", new Amazon.DynamoDBv2.Model.AttributeValue { S = product.Name } },
                    { "image", new Amazon.DynamoDBv2.Model.AttributeValue { S = product.Image } }
                };
                
                productsList.Add(new Amazon.DynamoDBv2.Model.AttributeValue { M = productMap });
            }
            
            item["payment_products"] = new Amazon.DynamoDBv2.Model.AttributeValue { L = productsList };
        }

        return item;
    }
}