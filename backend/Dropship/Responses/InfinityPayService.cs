using System.Text;
using System.Text.Json;

namespace Dropship.Responses;

/// <summary>
/// Serviço para integração com InfinityPay API
/// Cria links de checkout de pagamento
/// </summary>
public class InfinityPayService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InfinityPayService> _logger;
    private readonly string _apiBaseUrl = "https://api.infinitepay.io/invoices/public/checkout/links";

    public InfinityPayService(HttpClient httpClient, ILogger<InfinityPayService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Cria um link de checkout no InfinityPay
    /// POST: https://api.infinitepay.io/invoices/public/checkout/links
    /// </summary>
    /// <param name="handle">Username/Handle do fornecedor no InfinityPay</param>
    /// <param name="items">Lista de itens/produtos para o checkout</param>
    /// <param name="orderNsu">NSU único do pedido para rastreamento</param>
    /// <param name="webhookUrl">URL para webhook de notificação de pagamento</param>
    /// <returns>Resposta com checkout_url e invoice_id</returns>
    public async Task<string> CreateLinkAsync(
        string handle,
        List<InfinityPayItem> items,
        string orderNsu,
        string webhookUrl)
    {
        _logger.LogInformation(
            "Creating InfinityPay checkout link - Handle: {Handle}, Items: {ItemCount}, OrderNsu: {OrderNsu}",
            handle, items.Count, orderNsu);

        try
        {
            // Validar parâmetros obrigatórios
            if (string.IsNullOrWhiteSpace(handle))
                throw new ArgumentException("Handle is required", nameof(handle));

            if (items == null || items.Count == 0)
                throw new ArgumentException("Items cannot be empty", nameof(items));

            if (string.IsNullOrWhiteSpace(orderNsu))
                throw new ArgumentException("OrderNsu is required", nameof(orderNsu));

            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentException("WebhookUrl is required", nameof(webhookUrl));

            // Montar body da requisição conforme curl
            var requestBody = new
            {
                handle,
                items = items.Select(i => new
                {
                    quantity = i.Quantity,
                    price = i.Price * 100,
                    description = i.Description
                }).ToList(),
                order_nsu = orderNsu,
                webhook_url = webhookUrl
            };

            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogDebug("InfinityPay Request Body: {Body}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Fazer POST para API
            var response = await _httpClient.PostAsync(_apiBaseUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug(
                "InfinityPay Response - StatusCode: {StatusCode}, Content: {Content}",
                response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "InfinityPay API error - StatusCode: {StatusCode}, Content: {Content}",
                    response.StatusCode, responseContent);
                throw new HttpRequestException(
                    $"InfinityPay API returned {response.StatusCode}: {responseContent}");
            }

            // Deserializar resposta
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResponse = JsonSerializer.Deserialize<InfinityPayApiResponse>(responseContent, options);
            
            if (apiResponse == null || string.IsNullOrWhiteSpace(apiResponse.Url))
            {
                _logger.LogError(
                    "Invalid response from InfinityPay API - Missing URL - Content: {Content}",
                    responseContent);
                throw new InvalidOperationException("Invalid response from InfinityPay API: missing URL");
            }
            return apiResponse.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating InfinityPay checkout link - OrderNsu: {OrderNsu}", orderNsu);
            throw;
        }
    }
}

/// <summary>
/// Item/Produto para o checkout
/// </summary>
public class InfinityPayItem
{
    /// <summary>
    /// Quantidade do item
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Preço em centavos
    /// Exemplo: 1000 = R$ 10.00
    /// </summary>
    public long Price { get; set; }

    /// <summary>
    /// Descrição do item
    /// Exemplo: "Produto de Exemplo"
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Response de sucesso ao criar link no InfinityPay
/// </summary>
public class InfinityPayCheckoutLinkResponse
{
    /// <summary>
    /// URL do checkout para redirecionar o cliente
    /// </summary>
    public string CheckoutUrl { get; set; } = string.Empty;

    /// <summary>
    /// ID da fatura criada no InfinityPay
    /// </summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>
    /// Status da criação (created, failed, etc)
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response interna da API InfinityPay
/// </summary>
internal class InfinityPayApiResponse
{
    public string Url { get; set; } = default!;
}

