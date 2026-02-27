using Amazon.DynamoDBv2.Model;
using System.Globalization;
using Newtonsoft.Json;

namespace Dropship.Domain;

/// <summary>
/// Domain para registros de romaneio/envio de fornecedor
/// Vinculado a um pagamento processado
/// </summary>
public class SupplierShipmentDomain
{
    // 🔑 Chaves DynamoDB
    public string Pk { get; set; } = default!; // Supplier#{supplierId}
    public string Sk { get; set; } = default!; // Shipment#{shipmentId}

    // 📋 Identificadores
    public string ShipmentId { get; set; } = default!;
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
    public string CaptureMethod { get; set; } = default!;
    public string ReceiptUrl { get; set; } = default!;

    // 📊 Status e Datas
    public string Status { get; set; } = "paid";
    public string EntityType { get; set; } = "supplier_shipment";
    public string CreatedAt { get; set; } = default!;
    public string? ShippedAt { get; set; }

    // 📍 Endereço de Entrega (vindo do Order)
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public string DeliveryZipcode { get; set; } = string.Empty;

    // 🛒 Itens do envio
    public List<PaymentProduct> Items { get; set; } = new();
    public int TotalItems { get; set; }
}

/// <summary>
/// Builder para criar SupplierShipmentDomain a partir de PaymentQueue + OrderDomain
/// </summary>
public static class SupplierShipmentBuilder
{
    public static SupplierShipmentDomain CreateFromPayment(
        PaymentQueueDomain paymentQueue,
        string transactionNsu,
        string captureMethod,
        string receiptUrl,
        decimal paidAmount,
        int installments,
        OrderDomain? order = null)
    {
        var shipmentId = Ulid.NewUlid().ToString();

        return new SupplierShipmentDomain
        {
            Pk = $"Supplier#{paymentQueue.SupplierId}",
            Sk = $"Shipment#{shipmentId}",
            ShipmentId = shipmentId,
            PaymentId = paymentQueue.PaymentId,
            SupplierId = paymentQueue.SupplierId,
            SellerId = paymentQueue.SellerId,
            OrderSn = paymentQueue.OrderSn,
            Amount = paymentQueue.TotalAmount,
            PaidAmount = paidAmount,
            Installments = installments,
            TransactionNsu = transactionNsu,
            CaptureMethod = captureMethod,
            ReceiptUrl = receiptUrl,
            Status = "paid",
            EntityType = "supplier_shipment",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            Items = paymentQueue.PaymentProducts,
            TotalItems = paymentQueue.PaymentProducts.Count,

            // Endereço do pedido
            RecipientName = order?.RecipientName ?? "",
            RecipientPhone = order?.RecipientPhone ?? "",
            DeliveryAddress = order?.DeliveryAddress ?? "",
            DeliveryCity = order?.DeliveryCity ?? "",
            DeliveryState = order?.DeliveryState ?? "",
            DeliveryZipcode = order?.DeliveryZipcode ?? ""
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
            Amount = item.ContainsKey("amount") ? decimal.Parse(item["amount"].N, CultureInfo.InvariantCulture) : 0,
            PaidAmount = item.ContainsKey("paid_amount") ? decimal.Parse(item["paid_amount"].N, CultureInfo.InvariantCulture) : 0,
            Installments = item.ContainsKey("installments") ? int.Parse(item["installments"].N) : 0,
            TransactionNsu = item.ContainsKey("transaction_nsu") ? item["transaction_nsu"].S : "",
            CaptureMethod = item.ContainsKey("capture_method") ? item["capture_method"].S : "",
            ReceiptUrl = item.ContainsKey("receipt_url") ? item["receipt_url"].S : "",
            Status = item.ContainsKey("status") ? item["status"].S : "paid",
            EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "supplier_shipment",
            CreatedAt = item.ContainsKey("created_at") ? item["created_at"].S : DateTime.UtcNow.ToString("O"),
            ShippedAt = item.ContainsKey("shipped_at") ? item["shipped_at"].S : null,
            TotalItems = item.ContainsKey("total_items") ? int.Parse(item["total_items"].N) : 0,

            // Endereço de entrega
            RecipientName = item.ContainsKey("recipient_name") ? item["recipient_name"].S : "",
            RecipientPhone = item.ContainsKey("recipient_phone") ? item["recipient_phone"].S : "",
            DeliveryAddress = item.ContainsKey("delivery_address") ? item["delivery_address"].S : "",
            DeliveryCity = item.ContainsKey("delivery_city") ? item["delivery_city"].S : "",
            DeliveryState = item.ContainsKey("delivery_state") ? item["delivery_state"].S : "",
            DeliveryZipcode = item.ContainsKey("delivery_zipcode") ? item["delivery_zipcode"].S : "",

            Items = item.ContainsKey("items") && item["items"].L != null
                ? item["items"].L.Select(p => new PaymentProduct
                {
                    ProductId = p.M.ContainsKey("product_id") ? p.M["product_id"].S : "",
                    Sku = p.M.ContainsKey("sku") ? p.M["sku"].S : "",
                    Name = p.M.ContainsKey("name") ? p.M["name"].S : "",
                    Quantity = p.M.ContainsKey("quantity") ? int.Parse(p.M["quantity"].N) : 0,
                    UnitPrice = p.M.ContainsKey("unit_price") ? decimal.Parse(p.M["unit_price"].N, CultureInfo.InvariantCulture) : 0,
                    Image = p.M.ContainsKey("image") ? p.M["image"].S : "",
                    Color = p.M.ContainsKey("color") ? p.M["color"].S : "",
                    Size = p.M.ContainsKey("size") ? p.M["size"].S : ""
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
            { "amount", new AttributeValue { N = domain.Amount.ToString("F2", CultureInfo.InvariantCulture) } },
            { "paid_amount", new AttributeValue { N = domain.PaidAmount.ToString("F2", CultureInfo.InvariantCulture) } },
            { "installments", new AttributeValue { N = domain.Installments.ToString() } },
            { "transaction_nsu", new AttributeValue { S = domain.TransactionNsu } },
            { "capture_method", new AttributeValue { S = domain.CaptureMethod } },
            { "receipt_url", new AttributeValue { S = domain.ReceiptUrl } },
            { "status", new AttributeValue { S = domain.Status } },
            { "entity_type", new AttributeValue { S = domain.EntityType } },
            { "created_at", new AttributeValue { S = domain.CreatedAt } },
            { "total_items", new AttributeValue { N = domain.TotalItems.ToString() } },
            { "recipient_name", new AttributeValue { S = domain.RecipientName } },
            { "recipient_phone", new AttributeValue { S = domain.RecipientPhone } },
            { "delivery_address", new AttributeValue { S = domain.DeliveryAddress } },
            { "delivery_city", new AttributeValue { S = domain.DeliveryCity } },
            { "delivery_state", new AttributeValue { S = domain.DeliveryState } },
            { "delivery_zipcode", new AttributeValue { S = domain.DeliveryZipcode } }
        };

        if (!string.IsNullOrWhiteSpace(domain.ShippedAt))
            item["shipped_at"] = new AttributeValue { S = domain.ShippedAt };

        if (domain.Items.Count > 0)
        {
            item["items"] = new AttributeValue
            {
                L = domain.Items.Select(p => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        { "product_id", new AttributeValue { S = p.ProductId } },
                        { "sku", new AttributeValue { S = p.Sku } },
                        { "name", new AttributeValue { S = p.Name } },
                        { "quantity", new AttributeValue { N = p.Quantity.ToString() } },
                        { "unit_price", new AttributeValue { N = p.UnitPrice.ToString("F2", CultureInfo.InvariantCulture) } },
                        { "image", new AttributeValue { S = p.Image } },
                        { "color", new AttributeValue { S = p.Color } },
                        { "size", new AttributeValue { S = p.Size } }
                    }
                }).ToList()
            };
        }

        return item;
    }
}


