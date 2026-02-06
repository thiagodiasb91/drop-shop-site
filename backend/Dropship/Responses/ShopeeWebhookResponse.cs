namespace Dropship.Responses;

/// <summary>
/// Response para webhook de Shopee
/// </summary>
public class ShopeeWebhookResponse
{
    /// <summary>
    /// Código de status HTTP
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Mensagem de resposta
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
