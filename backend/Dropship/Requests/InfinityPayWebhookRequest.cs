namespace Dropship.Requests;

/// <summary>
/// Request para webhook de pagamento do InfinityPay
/// </summary>
public class InfinityPayWebhookRequest
{
    /// <summary>
    /// Identificador único da fatura no InfinityPay
    /// </summary>
    public string InvoiceSlug { get; set; } = string.Empty;

    /// <summary>
    /// Valor total da transação em centavos
    /// Exemplo: 9990 = R$ 99.90
    /// </summary>
    public decimal Amount { get; set; } = 0;

    /// <summary>
    /// Valor pago em centavos
    /// Pode ser diferente de Amount se houver desconto ou taxa
    /// </summary>
    public long PaidAmount { get; set; } = 0;

    /// <summary>
    /// Quantidade de parcelas
    /// 1 = à vista, 2+ = parcelado
    /// </summary>
    public int Installments { get; set; } = 1;

    /// <summary>
    /// Método de captura/pagamento
    /// Exemplos: credit_card, debit_card, pix, boleto, bank_transfer
    /// </summary>
    public string CaptureMethod { get; set; } = string.Empty;

    /// <summary>
    /// Número NSU da transação no gateway
    /// </summary>
    public string TransactionNsu { get; set; } = string.Empty;

    /// <summary>
    /// NSU do pedido no nosso sistema
    /// Formato esperado: {sellerId}:{paymentId}
    /// Exemplo: 69611396-ee23-4a96-9161-7c9928679056:abc123def456
    /// </summary>
    public string OrderNsu { get; set; } = string.Empty;

    /// <summary>
    /// URL do comprovante de pagamento
    /// </summary>
    public string ReceiptUrl { get; set; } = string.Empty;

    /// <summary>
    /// Itens pagos (opcional, para logging/auditoria)
    /// </summary>
    public List<InfinityPayItem>? Items { get; set; }
}

/// <summary>
/// Item de um pagamento InfinityPay
/// </summary>
public class InfinityPayItem
{
    /// <summary>
    /// Identificador do produto
    /// </summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>
    /// SKU do produto
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade comprada
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Preço unitário em centavos
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// URL da imagem do produto
    /// </summary>
    public string? Image { get; set; }
}



