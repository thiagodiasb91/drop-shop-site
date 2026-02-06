using Amazon.DynamoDBv2.DataModel;

namespace Dropship.Domain;

/// <summary>
/// Domínio para representar um vendedor (Seller)
/// </summary>
[DynamoDBTable("catalog-core")]
public class SellerDomain
{
    /// <summary>
    /// Chave de Partição: Seller#{SellerId}
    /// </summary>
    [DynamoDBHashKey("PK")]
    public string PK { get; set; } = string.Empty;

    /// <summary>
    /// Chave de Classificação: META
    /// </summary>
    [DynamoDBRangeKey("SK")]
    public string SK { get; set; } = string.Empty;

    /// <summary>
    /// Tipo da entidade: "seller"
    /// </summary>
    [DynamoDBProperty("entityType")]
    public string EntityType { get; set; } = "seller";

    /// <summary>
    /// Marketplace associado (ex: "shopee")
    /// </summary>
    [DynamoDBProperty("marketplace")]
    public string Marketplace { get; set; } = string.Empty;

    /// <summary>
    /// ID único do vendedor (UUID)
    /// </summary>
    [DynamoDBProperty("sellerId")]
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Nome do vendedor
    /// </summary>
    [DynamoDBProperty("sellerName")]
    public string SellerName { get; set; } = string.Empty;

    /// <summary>
    /// ID da loja no marketplace (ex: shop_id do Shopee)
    /// </summary>
    [DynamoDBProperty("shop_id")]
    public long ShopId { get; set; }

    /// <summary>
    /// Data de criação do registro
    /// </summary>
    [DynamoDBProperty("createdAt")]
    public long? CreatedAt { get; set; }

    /// <summary>
    /// Data de atualização do registro
    /// </summary>
    [DynamoDBProperty("updatedAt")]
    public long? UpdatedAt { get; set; }
}
