using Amazon.DynamoDBv2.Model;
using System.Globalization;

namespace Dropship.Domain;

public class PaymentDomain
{
    // 🔑 Chaves
    public string PK { get; set; } = default!;
    public string SK { get; set; } = default!;

    // 📌 Metadados
    public string EntityType { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    // 🧾 Pedido / Produto
    public string OrderSn { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public int Quantity { get; set; }

    // 👥 Relacionamentos
    public string SellerId { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    public string SupplierName { get; set; } = default!;
    public long ShopId { get; set; }

    // 💰 Pagamento
    public string Status { get; set; } = default!;
    public decimal Value { get; set; }
}

public static class PaymentMapper
{
    public static PaymentDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new PaymentDomain
        {
            PK = item["PK"].S,
            SK = item["SK"].S,
            EntityType = item["entityType"].S,
            CreatedAt = DateTime.Parse(item["created_at"].S),
            OrderSn = item["ordersn"].S,
            ProductId = item["product_id"].S,
            Sku = item["sku"].S,
            Quantity = int.Parse(item["quantity"].N),
            SellerId = item["seller_id"].S,
            SupplierId = item["supplier_id"].S,
            ShopId = long.Parse(item["shop_id"].N),
            Status = item["status"].S,
            Value = decimal.Parse(item["value"].N,CultureInfo.InvariantCulture)
        };
    }
}