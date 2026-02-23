using Amazon.DynamoDBv2.Model;

namespace Dropship.Domain;

/// <summary>
/// Domain para registros de romaneio/envio de fornecedor
/// Vinculado a um pagamento processado
/// </summary>
public class SupplierShipmentDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!; // Supplier#{supplierId}
    public string Sk { get; set; } = default!; // Shipment#{shipmentId}#Payment#{paymentId}

    // 📋 Identificadores
    public string ShipmentId { get; set; } = default!; // ULID único
    public string PaymentId { get; set; } = default!;
    public string SupplierId { get; set; } = default!;
    public string SellerId { get; set; } = default!;
    public string OrderSn { get; set; } = default!;

    // 💰 Informações de Pagamento
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public int Installments { get; set; }

    // 📦 Informações de Transação
    public string TransactionNsu { get; set; } = default!;
    public string OrderNsu { get; set; } = default!;
    public string CaptureMethod { get; set; } = default!; // credit_card, bank_transfer, etc
    public string ReceiptUrl { get; set; } = default!;

    // 📊 Status e Datas
    public string Status { get; set; } = "paid"; // paid, shipped, delivered
    public string EntityType { get; set; } = "supplier_shipment";
    public string CreatedAt { get; set; } = default!;
    public string? ShippedAt { get; set; }

    // 📍 Itens do envio (mesmo formato do PaymentQueue)
    public List<PaymentProduct> Items { get; set; } = default!;
    public int TotalItems { get; set; }
}

/// <summary>
/// Builder para criar SupplierShipmentDomain a partir de PaymentQueue
/// </summary>
public static class SupplierShipmentBuilder
{
    public static SupplierShipmentDomain CreateFromPayment(
        PaymentQueueDomain paymentQueue,
        string transactionNsu,
        string orderNsu,
        string captureMethod,
        string receiptUrl,
        decimal paidAmount,
        int installments)
    {
        var shipmentId = Ulid.NewUlid().ToString();

        return new SupplierShipmentDomain
        {
            Pk = $"Supplier#{paymentQueue.SupplierId}",
            Sk = $"Shipment#{shipmentId}#Payment#{paymentQueue.PaymentId}",
            ShipmentId = shipmentId,
            PaymentId = paymentQueue.PaymentId,
            SupplierId = paymentQueue.SupplierId,
            SellerId = paymentQueue.SellerId,
            OrderSn = paymentQueue.OrderSn,
            Amount = paymentQueue.TotalAmount,
            PaidAmount = paidAmount,
            Installments = installments,
            TransactionNsu = transactionNsu,
            OrderNsu = orderNsu,
            CaptureMethod = captureMethod,
            ReceiptUrl = receiptUrl,
            Status = "paid",
            EntityType = "supplier_shipment",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            Items = paymentQueue.PaymentProducts,
            TotalItems = paymentQueue.PaymentProducts.Count
        };
    }
}

/// <summary>
/// Mapper para converter Dictionary do DynamoDB em Domain
/// </summary>
public static class SupplierShipmentMapper
{
    public static SupplierShipmentDomain ToDomain(this Dictionary<string, AttributeValue> item)
    {
        return new SupplierShipmentDomain
        {
            Pk = item.ContainsKey("PK") ? item["PK"].S : "",
            Sk = item.ContainsKey("SK") ? item["SK"].S : "",
            ShipmentId = item.ContainsKey("shipment_id") ? item["shipment_id"].S : "",
            PaymentId = item.ContainsKey("payment_id") ? item["payment_id"].S : "",
            SupplierId = item.ContainsKey("supplier_id") ? item["supplier_id"].S : "",
            SellerId = item.ContainsKey("seller_id") ? item["seller_id"].S : "",
            OrderSn = item.ContainsKey("ordersn") ? item["ordersn"].S : "",
            Amount = item.ContainsKey("amount") ? decimal.Parse(item["amount"].N) : 0,
            PaidAmount = item.ContainsKey("paid_amount") ? decimal.Parse(item["paid_amount"].N) : 0,
            Installments = item.ContainsKey("installments") ? int.Parse(item["installments"].N) : 0,
            TransactionNsu = item.ContainsKey("transaction_nsu") ? item["transaction_nsu"].S : "",
            OrderNsu = item.ContainsKey("order_nsu") ? item["order_nsu"].S : "",
            CaptureMethod = item.ContainsKey("capture_method") ? item["capture_method"].S : "",
            ReceiptUrl = item.ContainsKey("receipt_url") ? item["receipt_url"].S : "",
            Status = item.ContainsKey("status") ? item["status"].S : "paid",
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "supplier_shipment",
            CreatedAt = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O"),
            ShippedAt = item.ContainsKey("shipped_at") ? item["shipped_at"].S : null,
            TotalItems = item.ContainsKey("total_items") ? int.Parse(item["total_items"].N) : 0,
            Items = item.ContainsKey("items") && item["items"].L != null
                ? item["items"].L.Select(p => new PaymentProduct
                {
                    ProductId = p.M.ContainsKey("product_id") && p.M["product_id"].S != null ? p.M["product_id"].S : "",
                    Sku = p.M.ContainsKey("sku") && p.M["sku"].S != null ? p.M["sku"].S : "",
                    Quantity = p.M.ContainsKey("quantity") && p.M["quantity"].N != null ? int.Parse(p.M["quantity"].N) : 0,
                    UnitPrice = p.M.ContainsKey("unit_price") && p.M["unit_price"].N != null ? decimal.Parse(p.M["unit_price"].N) : 0,
                    Image = p.M.ContainsKey("image") && p.M["image"].S != null ? p.M["image"].S : ""
                }).ToList()
                : new List<PaymentProduct>()
        };
    }

    /// <summary>
    /// Converte SupplierShipmentDomain para dicionário pronto para DynamoDB PutItem
    /// </summary>
    public static Dictionary<string, AttributeValue> ToDynamoDb(this SupplierShipmentDomain domain)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            { "PK", new AttributeValue { S = domain.Pk } },
            { "SK", new AttributeValue { S = domain.Sk } },
            { "shipment_id", new AttributeValue { S = domain.ShipmentId } },
            { "payment_id", new AttributeValue { S = domain.PaymentId } },
            { "supplier_id", new AttributeValue { S = domain.SupplierId } },
            { "seller_id", new AttributeValue { S = domain.SellerId } },
            { "ordersn", new AttributeValue { S = domain.OrderSn } },
            { "amount", new AttributeValue { N = domain.Amount.ToString("F2") } },
            { "paid_amount", new AttributeValue { N = domain.PaidAmount.ToString("F2") } },
            { "installments", new AttributeValue { N = domain.Installments.ToString() } },
            { "transaction_nsu", new AttributeValue { S = domain.TransactionNsu } },
            { "order_nsu", new AttributeValue { S = domain.OrderNsu } },
            { "capture_method", new AttributeValue { S = domain.CaptureMethod } },
            { "receipt_url", new AttributeValue { S = domain.ReceiptUrl } },
            { "status", new AttributeValue { S = domain.Status } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "created_at", new AttributeValue { S = domain.CreatedAt } },
            { "total_items", new AttributeValue { N = domain.TotalItems.ToString() } }
        };

        // Adicionar campos opcionais
        if (!string.IsNullOrWhiteSpace(domain.ShippedAt))
        {
            item["shipped_at"] = new AttributeValue { S = domain.ShippedAt };
        }

        // Adicionar lista de itens se houver
        if (domain.Items != null && domain.Items.Count > 0)
        {
            var itemsList = new List<AttributeValue>();

            foreach (var product in domain.Items)
            {
                var productMap = new Dictionary<string, AttributeValue>
                {
                    { "product_id", new AttributeValue { S = product.ProductId } },
                    { "sku", new AttributeValue { S = product.Sku } },
                    { "quantity", new AttributeValue { N = product.Quantity.ToString() } },
                    { "unit_price", new AttributeValue { N = product.UnitPrice.ToString("F2") } },
                    { "image", new AttributeValue { S = product.Image } }
                };

                itemsList.Add(new AttributeValue { M = productMap });
            }

            item["items"] = new AttributeValue { L = itemsList };
        }

        return item;
    }
}





