namespace Dropship.Requests;

/// <summary>
/// Request para criar um link de pagamento InfinityPay
/// </summary>
public class CreateInfinityPayLinkRequest
{
    /// <summary>
    /// Array de IDs de pagamento que serão processados juntos
    /// </summary>
    public List<string> PaymentIds { get; set; } = new();

    /// <summary>
    /// Valor total em centavos
    /// Exemplo: 9990 = R$ 99.90
    /// </summary>
    public decimal Amount { get; set; }
}

/// <summary>
/// Response ao criar link de pagamento
/// </summary>
public class InfinityPayLinkResponse
{
    /// <summary>
    /// ID único do link (ULID)
    /// </summary>
    public string LinkId { get; set; } = string.Empty;

    /// <summary>
    /// URL para enviar ao InfinityPay para checkout
    /// Contém o linkId como parâmetro
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;

    /// <summary>
    /// OrderNsu que será retornado no webhook
    /// Formato: paymentId1-paymentId2-...-paymentIdN
    /// </summary>
    public string OrderNsu { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade de pagamentos neste link
    /// </summary>
    public int PaymentCount { get; set; }

    /// <summary>
    /// Valor total em centavos
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Data de criação do link
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
}

