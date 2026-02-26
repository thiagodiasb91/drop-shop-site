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

