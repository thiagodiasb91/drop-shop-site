using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;
using Dropship.Repository;
using Dropship.Domain;

namespace Dropship.Services;

/// <summary>
/// Serviço para processar eventos de Shopee
/// </summary>
public class ShopeeService
{
    private readonly SellerRepository _sellerRepository;
    private readonly UserRepository _userRepository;
    private readonly ShopeeApiService _shopeeApiService;
    private readonly HttpClient _httpClient;
    private readonly IAmazonSQS _sqsClient;
    private readonly ILogger<ShopeeService> _logger;
    private const string SqsQueueUrl = "https://sqs.us-east-1.amazonaws.com/511758682977/shoppe-new-order-received-queue.fifo";
    private const string CacheServiceUrl = "https://c069zuj7g8.execute-api.us-east-1.amazonaws.com/test/cache";

    public ShopeeService(
        SellerRepository sellerRepository,
        UserRepository userRepository,
        ShopeeApiService shopeeApiService,
        HttpClient httpClient,
        IAmazonSQS sqsClient,
        ILogger<ShopeeService> logger)
    {
        _sellerRepository = sellerRepository;
        _userRepository = userRepository;
        _shopeeApiService = shopeeApiService;
        _httpClient = httpClient;
        _sqsClient = sqsClient;
        _logger = logger;
    }

    /// <summary>
    /// Autentica com Shopee usando o authorization code e armazena os tokens
    /// Verifica se o Seller já existe pelo shop_id, se não existir cria um novo
    /// Atualiza o usuário com o resource_id do Seller
    /// </summary>
    public async Task AuthenticateShopAsync(string code, long shopId, string email)
    {
        _logger.LogInformation("Authenticating shop with Shopee - ShopId: {ShopId}, Email: {Email}", shopId, email);

        try
        {
            // Validar se o usuário existe
            var user = await _userRepository.GetUser(email);
            if (user == null)
            {
                _logger.LogWarning("User not found - Email: {Email}", email);
                throw new InvalidOperationException($"User with email {email} not found");
            }

            // Obter tokens da API Shopee
            var (accessToken, refreshToken, expiresIn) = await _shopeeApiService.GetTokenShopLevelAsync(code, shopId);
            
            var existingSeller = await _sellerRepository.GetSellerByShopIdAsync(shopId);
            
            SellerDomain seller;
            if (existingSeller != null)
            {
                _logger.LogInformation("Seller already exists - ShopId: {ShopId}, SellerId: {SellerId}", 
                    shopId, existingSeller.SellerId);
                seller = existingSeller;
            }
            else
            {
                // Criar novo Seller
                var sellerId = Guid.NewGuid().ToString();
                seller = new SellerDomain
                {
                    SellerId = sellerId,
                    SellerName = $"Shop_{shopId}",
                    ShopId = shopId,
                    Marketplace = "shopee"
                };

                var createdSeller = await _sellerRepository.CreateSellerAsync(seller);
                _logger.LogInformation("Seller created successfully - SellerId: {SellerId}, ShopId: {ShopId}", 
                    createdSeller.SellerId, shopId);
                
                seller = createdSeller;
            }

            // Atualizar usuário com o resource_id (sellerId)
            user.ResourceId = seller.SellerId;
            await _userRepository.UpdateUserAsync(user);
            _logger.LogInformation("User updated with resource_id - Email: {Email}, ResourceId: {ResourceId}", 
                email, seller.SellerId);

            // Armazenar tokens no cache
            await CacheTokensAsync(shopId.ToString(), accessToken, refreshToken, expiresIn);

            _logger.LogInformation("Shop authenticated successfully - ShopId: {ShopId}, Email: {Email}, SellerId: {SellerId}", 
                shopId, email, seller.SellerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating shop - ShopId: {ShopId}, Email: {Email}", shopId, email);
            throw;
        }
    }

    /// <summary>
    /// Armazena tokens no cache
    /// </summary>
    private async Task CacheTokensAsync(string shopId, string accessToken, string refreshToken, long expiresIn)
    {
        _logger.LogInformation("Caching tokens for shop - ShopId: {ShopId}", shopId);

        try
        {
            var expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn;
            
            var tokenData = new
            {
                items = new object[]
                {
                    new { key = $"{shopId}_access_token", value = accessToken },
                    new { key = $"{shopId}_refresh_token", value = refreshToken },
                    new { key = $"{shopId}_access_token_expires_at", value = expiresAt }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(tokenData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(CacheServiceUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to cache tokens - ShopId: {ShopId}, StatusCode: {StatusCode}",
                    shopId, response.StatusCode);
                // Não lançar exceção pois os tokens foram obtidos com sucesso
            }
            else
            {
                _logger.LogInformation("Tokens cached successfully - ShopId: {ShopId}", shopId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching tokens - ShopId: {ShopId}", shopId);
            // Não lançar exceção pois os tokens foram obtidos com sucesso
        }
    }

    /// <summary>
    /// Processa ordem recebida do Shopee
    /// Verifica se a loja existe e envia para fila SQS
    /// </summary>
    public async Task<bool> ProcessOrderReceivedAsync(long shopId, string orderSn, string status)
    {
        _logger.LogInformation("Processing order received - OrderSn: {OrderSn}, Status: {Status}, ShopId: {ShopId}", 
            orderSn, status, shopId);

        try
        {
            // Verificar se a loja existe
            var shopExists = await _sellerRepository.VerifyIfShopExistsAsync(shopId);
            if (!shopExists)
            {
                _logger.LogWarning("Shop not found: {ShopId}", shopId);
                throw new InvalidOperationException($"Shop {shopId} not found");
            }

            // Enviar mensagem para SQS
            await SendOrderToSqsAsync(orderSn, status, shopId.ToString());

            _logger.LogInformation("Order processed successfully - OrderSn: {OrderSn}", orderSn);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order: {OrderSn}", orderSn);
            throw;
        }
    }

    /// <summary>
    /// Envia mensagem de ordem para fila SQS FIFO
    /// </summary>
    private async Task SendOrderToSqsAsync(string orderSn, string status, string shopId)
    {
        _logger.LogInformation("Sending order to SQS - OrderSn: {OrderSn}, ShopId: {ShopId}", orderSn, shopId);

        try
        {
            var message = new
            {
                ordersn = orderSn,
                status = status,
                shop_id = shopId
            };

            var messageBody = JsonSerializer.Serialize(message);
            var messageGroupId = $"{shopId}-{orderSn}";

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = SqsQueueUrl,
                MessageBody = messageBody,
                MessageGroupId = messageGroupId
            };

            var response = await _sqsClient.SendMessageAsync(sendMessageRequest);

            _logger.LogInformation("Message sent to SQS - MessageId: {MessageId}, GroupId: {GroupId}", 
                response.MessageId, messageGroupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to SQS - OrderSn: {OrderSn}", orderSn);
            throw;
        }
    }
}
