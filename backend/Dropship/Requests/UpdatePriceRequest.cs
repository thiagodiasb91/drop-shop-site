namespace Dropship.Requests;

/// <summary>
/// DTO para atualizar preço de um item/modelo na Shopee
/// </summary>
public class PriceListDto
{
    /// <summary>
    /// ID do modelo/variação (opcional)
    /// Se não fornecido, atualiza o preço do item inteiro
    /// </summary>
    public long? ModelId { get; set; }

    /// <summary>
    /// Preço original do produto
    /// Valor em decimal (ex: 79.90)
    /// </summary>
    public decimal OriginalPrice { get; set; }
}

/// <summary>
/// Request para atualizar preço de item/modelos
/// </summary>
public class UpdatePriceRequest
{
    /// <summary>
    /// Lista de preços para atualizar
    /// 
    /// Exemplos:
    /// 1. Item sem variações:
    ///    [{ "original_price": 100.00 }]
    /// 
    /// 2. Item com variações:
    ///    [
    ///      { "model_id": 111, "original_price": 100.00 },
    ///      { "model_id": 222, "original_price": 150.00 }
    ///    ]
    /// </summary>
    public List<PriceListDto> PriceList { get; set; } = new();
}

