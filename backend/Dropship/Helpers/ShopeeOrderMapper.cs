using Newtonsoft.Json.Linq;
using Dropship.Domain;

namespace Dropship.Helpers;

/// <summary>
/// Helper para mapear dados de pedidos da API Shopee para domínios da aplicação
/// Centraliza toda a lógica de extração e transformação de dados
/// </summary>
public static class ShopeeOrderMapper
{
    /// <summary>
    /// Extrai dados do endereço de entrega a partir do objeto do pedido Shopee
    /// </summary>
    public static DeliveryAddressData ExtractDeliveryAddress(JToken order)
    {
        var buyerAddress = order["buyer_address"]?.Value<string>() ?? "No address";
        var buyerName = order["buyer_username"]?.Value<string>() ?? "Unknown";
        
        return new DeliveryAddressData
        {
            RecipientName = order["recipient_address"]?["name"]?.Value<string>() ?? buyerName,
            RecipientPhone = order["recipient_address"]?["phone"]?.Value<string>() ?? "",
            DeliveryAddress = order["recipient_address"]?["full_address"]?.Value<string>() ?? buyerAddress,
            DeliveryCity = order["recipient_address"]?["city"]?.Value<string>() ?? "",
            DeliveryState = order["recipient_address"]?["state"]?.Value<string>() ?? "",
            DeliveryZipcode = order["recipient_address"]?["zipcode"]?.Value<string>() ?? ""
        };
    }

    /// <summary>
    /// Extrai dados da fatura a partir do objeto do pedido Shopee
    /// </summary>
    public static InvoiceData ExtractInvoice(JToken order)
    {
        return new InvoiceData
        {
            InvoiceNumber = order["invoice"]?[0]?["invoice_number"]?.Value<string>() ?? "",
            InvoiceStatus = order["invoice"]?[0]?["invoice_status"]?.Value<string>() ?? ""
        };
    }

    /// <summary>
    /// Calcula o valor total e quantidade de itens do pedido
    /// </summary>
    public static OrderTotals CalculateOrderTotals(JToken itemList)
    {
        return new OrderTotals
        {
            TotalAmount = itemList.Sum(i => 
                decimal.Parse(i["model_original_price"]?.Value<string>() ?? "0")),
            TotalItems = itemList.Sum(i => 
                int.TryParse(i["model_quantity_purchased"]?.Value<string>(), out var qty) ? qty : 0)
        };
    }

    /// <summary>
    /// Mapeia um item do pedido Shopee para dados internos de processamento
    /// color e size vêm do SkuDomain buscado no banco de dados
    /// </summary>
    public static OrderItemMapped? MapOrderItem(
        JToken item,
        string? productId,
        string supplierId,
        SellerDomain seller,
        decimal productionPrice,
        string color = "",
        string size = "")
    {
        var modelSku = item["model_sku"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(modelSku))
            return null;

        if (!int.TryParse(item["model_quantity_purchased"]?.Value<string>(), out var quantityPurchased))
            return null;

        if (string.IsNullOrEmpty(productId))
            return null;

        var price = item["model_original_price"]?.Value<decimal>() ?? 0;

        return new OrderItemMapped
        {
            ProductId = productId,
            Sku = modelSku,
            Quantity = quantityPurchased,
            Price = price,
            ProductionPrice = productionPrice,
            SupplierId = supplierId,
            Seller = seller,
            Name = item["item_name"]?.Value<string>() ?? "Unknown Product",
            Image = item["image_info"]?["image_url"]?.Value<string>() ?? "",
            Color = color,
            Size = size
        };
    }

    /// <summary>
    /// Converte a lista de OrderItemMapped para OrderItemDomain
    /// já com todos os dados relevantes para exibição e impressão
    /// </summary>
    public static List<OrderItemDomain> MapOrderItems(IEnumerable<OrderItemMapped> mappedItems)
    {
        return mappedItems.Select(i => new OrderItemDomain
        {
            ProductId = i.ProductId,
            Sku = i.Sku,
            Name = i.Name,
            ImageUrl = i.Image,
            Quantity = i.Quantity,
            UnitPrice = i.Price,
            TotalPrice = i.Price * i.Quantity,
            SupplierId = i.SupplierId,
            ProductionPrice = i.ProductionPrice,
            Color = i.Color,
            Size = i.Size
        }).ToList();
    }

    /// <summary>
    /// Cria um OrderDomain a partir dos dados extraídos
    /// PK = Orders#{sellerId} | SK = {orderSn}
    /// OrderSn == OrderId (são o mesmo valor)
    /// </summary>
    public static OrderDomain CreateOrderDomain(
        string orderSn,
        long shopId,
        string sellerId,
        string status,
        OrderTotals totals,
        DeliveryAddressData deliveryAddress,
        InvoiceData invoice,
        List<OrderItemDomain>? items = null)
    {
        return new OrderDomain
        {
            Pk = $"Orders#{sellerId}",
            Sk = orderSn,
            OrderSn = orderSn,
            EntityType = "order",
            ShopId = shopId,
            SellerId = sellerId,
            Status = status,
            TotalAmount = totals.TotalAmount,
            TotalItems = totals.TotalItems,
            RecipientName = deliveryAddress.RecipientName,
            RecipientPhone = deliveryAddress.RecipientPhone,
            DeliveryAddress = deliveryAddress.DeliveryAddress,
            DeliveryCity = deliveryAddress.DeliveryCity,
            DeliveryState = deliveryAddress.DeliveryState,
            DeliveryZipcode = deliveryAddress.DeliveryZipcode,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceStatus = invoice.InvoiceStatus,
            Items = items ?? new List<OrderItemDomain>(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Dados do endereço de entrega extraídos do pedido Shopee
/// </summary>
public class DeliveryAddressData
{
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public string DeliveryZipcode { get; set; } = string.Empty;
}

/// <summary>
/// Dados da fatura extraídos do pedido Shopee
/// </summary>
public class InvoiceData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceStatus { get; set; } = string.Empty;
}

/// <summary>
/// Totais calculados do pedido
/// </summary>
public class OrderTotals
{
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
}

/// <summary>
/// Item mapeado do pedido para processamento interno
/// </summary>
public class OrderItemMapped
{
    public string ProductId { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal ProductionPrice { get; set; }
    public string SupplierId { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public SellerDomain Seller { get; set; } = default!;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
}


