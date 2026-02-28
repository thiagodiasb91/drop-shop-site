using System.Text.Json.Serialization;
using Dropship.Requests;
using Microsoft.AspNetCore.Mvc;
using Dropship.Services;
using Dropship.Responses;

namespace Dropship.Controllers;

/// <summary>
/// Controller de Interface Shopee para testes diretos de API
/// Expõe métodos do serviço ShopeeApiService
/// Útil para testar chamadas à API da Shopee sem necessidade de debug
/// </summary>
[ApiController]
[Route("shopee-interface")]
public class ShopeeInterfaceController : ControllerBase
{
    private readonly ShopeeApiService _shopeeApiService;
    private readonly ILogger<ShopeeInterfaceController> _logger;

    public ShopeeInterfaceController(
        ShopeeApiService shopeeApiService,
        ILogger<ShopeeInterfaceController> logger)
    {
        _shopeeApiService = shopeeApiService;
        _logger = logger;
    }

    /// <summary>
    /// Gera URL de autenticação com Shopee
    /// Útil para testes iniciais de auth flow
    /// </summary>
    /// <param name="email">Email do seller</param>
    /// <param name="requestUri">URI base da aplicação (ex: http://localhost:5000)</param>
    /// <returns>URL de autenticação para redirecionar o usuário ao Shopee</returns>
    [HttpGet("auth-url")]
    [ProducesResponseType(typeof(AuthUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAuthUrl([FromQuery] string email, [FromQuery] string requestUri)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetAuthUrl - Email: {Email}, RequestUri: {RequestUri}", email, requestUri);

        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(requestUri))
            {
                _logger.LogWarning("[SHOPEE-TEST] Missing email or requestUri");
                return BadRequest(new { error = "Email and requestUri are required" });
            }

            var authUrl = _shopeeApiService.GetAuthUrl(email, requestUri);
            _logger.LogInformation("[SHOPEE-TEST] Auth URL generated successfully");

            return Ok(new AuthUrlResponse { AuthUrl = authUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error generating auth URL");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém token de loja (shop-level) usando authorization code
    /// Etapa 2 do fluxo OAuth2
    /// </summary>
    /// <param name="code">Authorization code retornado pelo Shopee</param>
    /// <param name="shopId">ID da loja no Shopee</param>
    /// <returns>Tokens de acesso e refresh</returns>
    [HttpPost("get-token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetToken([FromQuery] string code, [FromQuery] long shopId)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetToken - Code: {Code}, ShopId: {ShopId}", code, shopId);

        try
        {
            var accessToken = await _shopeeApiService.GetCachedAccessTokenAsync(shopId, code);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting token - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém informações detalhadas da loja
    /// O access token é obtido automaticamente do cache usando shopId
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <returns>Informações da loja Shopee</returns>
    [HttpGet("shop-info")]
    [ProducesResponseType(typeof(ShopeeShopInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetShopInfo([FromQuery] long shopId)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetShopInfo - ShopId: {ShopId}", shopId);

        try
        {
            if (shopId <= 0)
            {
                _logger.LogWarning("[SHOPEE-TEST] Invalid shopId");
                return BadRequest(new { error = "Valid shopId is required" });
            }

            var shopInfo = await _shopeeApiService.GetShopInfoAsync(shopId);

            _logger.LogInformation("[SHOPEE-TEST] Shop info obtained successfully - ShopId: {ShopId}",
                shopId);

            return Ok(shopInfo);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[SHOPEE-TEST] Invalid operation - ShopId: {ShopId}", shopId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting shop info - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    #region Product Endpoints

    /// <summary>
    /// Obtém lista de categorias disponíveis na Shopee
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="language">Idioma das categorias: pt, en, zh-Hans (default: pt)</param>
    /// <returns>Lista de categorias com IDs para uso no AddItem</returns>
    /// <remarks>
    /// Use este endpoint para obter os category_id válidos para criar produtos.
    /// 
    /// Exemplo de resposta:
    /// 
    ///     {
    ///         "error": "",
    ///         "message": "",
    ///         "response": {
    ///             "category_list": [
    ///                 {
    ///                     "category_id": 100629,
    ///                     "parent_category_id": 100017,
    ///                     "original_category_name": "Camisetas",
    ///                     "display_category_name": "Camisetas",
    ///                     "has_children": false
    ///                 }
    ///             ]
    ///         }
    ///     }
    /// </remarks>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] long shopId,
        [FromQuery] string language = "pt")
    {
        _logger.LogInformation("[SHOPEE-TEST] GetCategories - ShopId: {ShopId}, Language: {Language}", shopId, language);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            var result = await _shopeeApiService.GetCategoryListAsync(shopId, language);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting categories - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém lista de itens/produtos da loja
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="offset">Offset para paginação (default: 0)</param>
    /// <param name="pageSize">Tamanho da página (default: 20, max: 100)</param>
    /// <param name="itemStatus">Status dos itens: NORMAL, BANNED, DELETED, UNLIST (default: NORMAL)</param>
    [HttpGet("items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetItemList(
        [FromQuery] long shopId,
        [FromQuery] int offset = 0,
        [FromQuery] int pageSize = 20,
        [FromQuery] string itemStatus = "NORMAL")
    {
        _logger.LogInformation("[SHOPEE-TEST] GetItemList - ShopId: {ShopId}, Offset: {Offset}, PageSize: {PageSize}, Status: {Status}",
            shopId, offset, pageSize, itemStatus);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            var result = await _shopeeApiService.GetItemListAsync(shopId, offset, pageSize, itemStatus);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting item list - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém informações básicas de um ou mais itens
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemIds">Lista de IDs dos itens (separados por vírgula, max: 50)</param>
    [HttpGet("items/base-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetItemBaseInfo(
        [FromQuery] long shopId,
        [FromQuery] string itemIds)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetItemBaseInfo - ShopId: {ShopId}, ItemIds: {ItemIds}", shopId, itemIds);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (string.IsNullOrWhiteSpace(itemIds))
            {
                return BadRequest(new { error = "itemIds is required" });
            }

            var itemIdArray = itemIds.Split(',').Select(id => long.Parse(id.Trim())).ToArray();
            var result = await _shopeeApiService.GetItemBaseInfoAsync(shopId, itemIdArray);
            return Ok(result.RootElement);
        }
        catch (FormatException)
        {
            return BadRequest(new { error = "itemIds must be a comma-separated list of numbers" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting item base info - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adiciona um novo item/produto à loja
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemData">Dados do item em formato JSON</param>
    [HttpPost("items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(
        [FromQuery] long shopId,
        [FromBody] object itemData)
    {
        _logger.LogInformation("[SHOPEE-TEST] AddItem - ShopId: {ShopId}", shopId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (itemData == null)
            {
                return BadRequest(new { error = "itemData is required" });
            }

            var result = await _shopeeApiService.AddItemAsync(shopId, itemData);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error adding item - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um item/produto existente
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item a ser atualizado</param>
    /// <param name="itemData">Dados do item para atualização</param>
    [HttpPut("items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateItem(
        [FromQuery] long shopId,
        [FromRoute] long itemId,
        [FromBody] object itemData)
    {
        _logger.LogInformation("[SHOPEE-TEST] UpdateItem - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (itemData == null)
            {
                return BadRequest(new { error = "itemData is required" });
            }

            var result = await _shopeeApiService.UpdateItemAsync(shopId, itemId, itemData);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error updating item - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deleta um item/produto existente
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item a ser deletado</param>
    /// <remarks>
    /// Nota: Esta operação é irreversível. O item será completamente removido da loja.
    /// Certifique-se de que deseja deletar o item antes de fazer a requisição.
    /// </remarks>
    [HttpDelete("items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteItem(
        [FromQuery] long shopId,
        [FromRoute] long itemId)
    {
        _logger.LogInformation("[SHOPEE-TEST] DeleteItem - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (itemId <= 0)
            {
                return BadRequest(new { error = "Valid itemId is required" });
            }

            var result = await _shopeeApiService.DeleteItemAsync(shopId, itemId);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error deleting item - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    #endregion

    #region Model/Variation Endpoints

    /// <summary>
    /// Inicializa variações de tier para um item existente (sem variações)
    /// Use este endpoint para adicionar variações a um produto que foi criado sem elas
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="request">Dados das variações e modelos</param>
    /// <remarks>
    /// Exemplo de requisição:
    /// 
    ///     POST /shopee-interface/items/43464493923/init-tier-variation?shopId=226289035
    ///     {
    ///         "standardise_tier_variation": [
    ///             {
    ///                 "variation_id": 0,
    ///                 "variation_group_id": 0,
    ///                 "variation_name": "Cor",
    ///                 "variation_option_list": [
    ///                     { "variation_option_id": 0, "variation_option_name": "Azul", "image_id": "sg-xxx-azul" },
    ///                     { "variation_option_id": 0, "variation_option_name": "Amarelo", "image_id": "sg-xxx-amarelo" },
    ///                     { "variation_option_id": 0, "variation_option_name": "Vermelho", "image_id": "sg-xxx-vermelho" }
    ///                 ]
    ///             },
    ///             {
    ///                 "variation_id": 0,
    ///                 "variation_group_id": 0,
    ///                 "variation_name": "Tamanho",
    ///                 "variation_option_list": [
    ///                     { "variation_option_id": 0, "variation_option_name": "P" },
    ///                     { "variation_option_id": 0, "variation_option_name": "M" },
    ///                     { "variation_option_id": 0, "variation_option_name": "G" }
    ///                 ]
    ///             }
    ///         ],
    ///         "model": [
    ///             { "tier_index": [0, 0], "model_sku": "CAM-AZUL-P", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [0, 1], "model_sku": "CAM-AZUL-M", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [0, 2], "model_sku": "CAM-AZUL-G", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [1, 0], "model_sku": "CAM-AMARELO-P", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [1, 1], "model_sku": "CAM-AMARELO-M", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [1, 2], "model_sku": "CAM-AMARELO-G", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [2, 0], "model_sku": "CAM-VERMELHO-P", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [2, 1], "model_sku": "CAM-VERMELHO-M", "original_price": 10000, "seller_stock": [{ "stock": 10 }] },
    ///             { "tier_index": [2, 2], "model_sku": "CAM-VERMELHO-G", "original_price": 10000, "seller_stock": [{ "stock": 10 }] }
    ///         ]
    ///     }
    /// 
    /// Notas:
    /// - tier_index[0] = índice da Cor (0=Azul, 1=Amarelo, 2=Vermelho)
    /// - tier_index[1] = índice do Tamanho (0=P, 1=M, 2=G)
    /// - image_id é opcional e só necessário para variações visuais (como cor)
    /// - original_price pode estar em centavos dependendo da região
    /// </remarks>
    [HttpPost("items/{itemId}/init-tier-variation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitTierVariation(
        [FromQuery] long shopId,
        [FromRoute] long itemId,
        [FromBody] InitTierVariationRequest request)
    {
        _logger.LogInformation("[SHOPEE-TEST] InitTierVariation - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (request == null || request.StandardiseTierVariation == null || request.Model == null)
            {
                return BadRequest(new { error = "standardise_tier_variation and model are required" });
            }

            var result = await _shopeeApiService.InitTierVariationAsync(shopId, itemId, request.StandardiseTierVariation, request.Model);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error initializing tier variation - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém lista de modelos/variações de um item
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    [HttpGet("items/{itemId}/models")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetModelList(
        [FromQuery] long shopId,
        [FromRoute] long itemId)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetModelList - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            var result = await _shopeeApiService.GetModelListAsync(shopId, itemId);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting model list - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adiciona modelos/variações a um item existente
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="modelData">Dados dos modelos a serem adicionados</param>
    [HttpPost("items/{itemId}/models")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddModel(
        [FromQuery] long shopId,
        [FromRoute] long itemId,
        [FromBody] object modelData)
    {
        _logger.LogInformation("[SHOPEE-TEST] AddModel - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (modelData == null)
            {
                return BadRequest(new { error = "modelData is required" });
            }

            var result = await _shopeeApiService.AddModelAsync(shopId, itemId, modelData);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error adding model - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza modelos/variações de um item
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="modelData">Dados dos modelos para atualização</param>
    [HttpPut("items/{itemId}/models")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateModel(
        [FromQuery] long shopId,
        [FromRoute] long itemId,
        [FromBody] object modelData)
    {
        _logger.LogInformation("[SHOPEE-TEST] UpdateModel - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (modelData == null)
            {
                return BadRequest(new { error = "modelData is required" });
            }

            var result = await _shopeeApiService.UpdateModelAsync(shopId, itemId, modelData);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error updating model - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deleta um modelo/variação de um item
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="modelId">ID do modelo a ser deletado</param>
    /// <remarks>
    /// Nota: Se o item possui apenas um modelo, ele não poderá ser deletado.
    /// A API retornará um erro neste caso.
    /// </remarks>
    [HttpDelete("items/{itemId}/models/{modelId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteModel(
        [FromQuery] long shopId,
        [FromRoute] long itemId,
        [FromRoute] long modelId)
    {
        _logger.LogInformation("[SHOPEE-TEST] DeleteModel - ShopId: {ShopId}, ItemId: {ItemId}, ModelId: {ModelId}", shopId, itemId, modelId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (itemId <= 0)
            {
                return BadRequest(new { error = "Valid itemId is required" });
            }

            if (modelId <= 0)
            {
                return BadRequest(new { error = "Valid modelId is required" });
            }

            var result = await _shopeeApiService.DeleteModelAsync(shopId, itemId, modelId);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error deleting model - ShopId: {ShopId}, ItemId: {ItemId}, ModelId: {ModelId}", shopId, itemId, modelId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza o preço de um item/produto ou de modelos específicos
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="priceList">Lista de preços com original_price (e opcionalmente model_id para variações)</param>
    /// <remarks>
    /// Exemplos de priceList:
    /// 
    /// 1. Item sem variações:
    /// [
    ///   { "original_price": 100.00 }
    /// ]
    /// 
    /// 2. Item com variações (múltiplos modelos):
    /// [
    ///   { "model_id": 111, "original_price": 100.00 },
    ///   { "model_id": 222, "original_price": 150.00 }
    /// ]
    /// </remarks>
    [HttpPut("items/{itemId}/price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePrice(
        [FromQuery] long shopId,
        [FromRoute] long itemId,
        [FromBody] UpdatePriceRequest request)
    {
        _logger.LogInformation("[SHOPEE-TEST] UpdatePrice - ShopId: {ShopId}, ItemId: {ItemId}, PriceCount: {PriceCount}",
            shopId, itemId, request.PriceList?.Count ?? 0);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (itemId <= 0)
            {
                return BadRequest(new { error = "Valid itemId is required" });
            }

            if (request?.PriceList == null || request.PriceList.Count == 0)
            {
                return BadRequest(new { error = "priceList is required and cannot be empty" });
            }

            // Validar que cada item em priceList tem original_price válido
            foreach (var price in request.PriceList)
            {
                if (price.OriginalPrice <= 0)
                {
                    return BadRequest(new { error = "Each price in priceList must have 'original_price' greater than 0" });
                }
            }

            var result = await _shopeeApiService.UpdatePriceAsync(shopId, itemId, request.PriceList);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error updating price - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza estoque de um item/produto ou de modelos específicos
    /// Endpoint: POST /api/v2/product/update_stock
    /// Ref: https://open.shopee.com/documents/v2/v2.product.update_stock
    /// 
    /// Pode ser usado para:
    /// 1. Atualizar estoque do item inteiro (sem modelos)
    /// 2. Atualizar estoque de modelos específicos (variações)
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="itemId">ID do item</param>
    /// <param name="modelId"></param>
    /// <param name="request">Request contendo lista de estoques</param>
    /// <remarks>
    /// Exemplo 1 - Item sem variações:
    /// {
    ///   "stock_list": [
    ///     { "stock": 100 }
    ///   ]
    /// }
    /// 
    /// Exemplo 2 - Item com variações:
    /// {
    ///   "stock_list": [
    ///     { "model_id": 111, "stock": 100 },
    ///     { "model_id": 222, "stock": 150 }
    ///   ]
    /// }
    /// </remarks>
    [HttpPut("items/{itemId}/models/{modelId}/stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStock(
        [FromQuery] long shopId,
        [FromRoute] long itemId,
        [FromRoute] long modelId,
        [FromBody] UpdateStockRequest request)
    {
        _logger.LogInformation("[SHOPEE-TEST] UpdateStock - ShopId: {ShopId}, ItemId: {ItemId}, ModelId: {ModelId}", shopId, itemId, modelId);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (itemId <= 0)
            {
                return BadRequest(new { error = "Valid itemId is required" });
            }

            if (modelId <= 0)
            {
                return BadRequest(new { error = "ModelId is required and cannot be empty" });
            }

            var result = await _shopeeApiService.UpdateStockAsync(shopId, itemId, modelId, request.Quantity);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error updating stock - ShopId: {ShopId}, ItemId: {ItemId}", shopId, itemId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    #endregion

    #region Order Endpoints

    /// <summary>
    /// Obtém lista de pedidos da loja
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="timeRangeField">Campo de tempo para filtro: create_time, update_time</param>
    /// <param name="timeFrom">Timestamp inicial do período</param>
    /// <param name="timeTo">Timestamp final do período</param>
    /// <param name="pageSize">Tamanho da página (default: 20, max: 100)</param>
    /// <param name="cursor">Cursor para paginação</param>
    /// <param name="orderStatus">Status do pedido</param>
    [HttpGet("orders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrderList(
        [FromQuery] long shopId,
        [FromQuery] string timeRangeField,
        [FromQuery] long timeFrom,
        [FromQuery] long timeTo,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? cursor = null,
        [FromQuery] string? orderStatus = null)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetOrderList - ShopId: {ShopId}, TimeRangeField: {TimeRangeField}, TimeFrom: {TimeFrom}, TimeTo: {TimeTo}",
            shopId, timeRangeField, timeFrom, timeTo);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (string.IsNullOrWhiteSpace(timeRangeField))
            {
                return BadRequest(new { error = "timeRangeField is required (create_time or update_time)" });
            }

            var result = await _shopeeApiService.GetOrderListAsync(shopId, timeRangeField, timeFrom, timeTo, pageSize, cursor, orderStatus);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting order list - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém detalhes de um ou mais pedidos
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="orderSnList">Lista de order_sn (números dos pedidos), separados por vírgula</param>
    /// <param name="responseOptionalFields">Campos opcionais a serem retornados, separados por vírgula</param>
    [HttpGet("orders/detail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrderDetail(
        [FromQuery] long shopId,
        [FromQuery] string orderSnList)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetOrderDetail - ShopId: {ShopId}, OrderSnList: {OrderSnList}", shopId, orderSnList);

        try
        {
            if (shopId <= 0)
            {
                return BadRequest(new { error = "Valid shopId is required" });
            }

            if (string.IsNullOrWhiteSpace(orderSnList))
            {
                return BadRequest(new { error = "orderSnList is required" });
            }

            var orderSnArray = orderSnList.Split(',').Select(s => s.Trim()).ToArray();
            
            var result = await _shopeeApiService.GetOrderDetailAsync(shopId, orderSnArray);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting order detail - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    #endregion

    #region Logistics Endpoints

    /// <summary>
    /// Obtém os parâmetros de logística de um pedido antes de confirmar o envio.
    /// Retorna endereço de coleta, time slots, transportadora e número de rastreio.
    /// Ref: https://open.shopee.com/documents/v2/v2.logistics.get_shipping_parameter
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="orderSn">Número do pedido</param>
    /// <param name="packageNumber">Número do pacote (opcional — usar quando o pedido tem split de pacotes)</param>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     GET /shopee-interface/logistics/shipping-parameter?shopId=226289035&amp;orderSn=260227RU6TQ98E
    ///
    /// Com package_number:
    ///
    ///     GET /shopee-interface/logistics/shipping-parameter?shopId=226289035&amp;orderSn=260227RU6TQ98E&amp;packageNumber=PKG-001
    /// </remarks>
    [HttpGet("logistics/shipping-parameter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetShippingParameter(
        [FromQuery] long shopId,
        [FromQuery] string orderSn,
        [FromQuery] string? packageNumber = null)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetShippingParameter - ShopId: {ShopId}, OrderSn: {OrderSn}", shopId, orderSn);

        try
        {
            if (shopId <= 0)
                return BadRequest(new { error = "Valid shopId is required" });

            if (string.IsNullOrWhiteSpace(orderSn))
                return BadRequest(new { error = "orderSn is required" });

            var result = await _shopeeApiService.GetShippingParameterAsync(shopId, orderSn, packageNumber);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting shipping parameter - ShopId: {ShopId}, OrderSn: {OrderSn}", shopId, orderSn);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém o número de rastreio de um pedido.
    /// Ref: https://open.shopee.com/documents/v2/v2.logistics.get_tracking_number
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="orderSn">Número do pedido</param>
    /// <param name="packageNumber">Número do pacote (opcional — usar quando o pedido tem split de pacotes)</param>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     GET /shopee-interface/logistics/tracking-number?shopId=226289035&amp;orderSn=260227RU6TQ98E
    ///
    /// Com package_number:
    ///
    ///     GET /shopee-interface/logistics/tracking-number?shopId=226289035&amp;orderSn=260227RU6TQ98E&amp;packageNumber=PKG-001
    ///
    /// Resposta esperada:
    ///
    ///     {
    ///       "response": {
    ///         "tracking_number": "BR123456789",
    ///         "pltnm": false,
    ///         "hint": ""
    ///       }
    ///     }
    /// </remarks>
    [HttpGet("logistics/tracking-number")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrackingNumber(
        [FromQuery] long shopId,
        [FromQuery] string orderSn,
        [FromQuery] string? packageNumber = null)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetTrackingNumber - ShopId: {ShopId}, OrderSn: {OrderSn}", shopId, orderSn);

        try
        {
            if (shopId <= 0)
                return BadRequest(new { error = "Valid shopId is required" });

            if (string.IsNullOrWhiteSpace(orderSn))
                return BadRequest(new { error = "orderSn is required" });

            var result = await _shopeeApiService.GetTrackingNumberAsync(shopId, orderSn, packageNumber);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting tracking number - ShopId: {ShopId}, OrderSn: {OrderSn}", shopId, orderSn);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém o histórico detalhado de rastreio de um pedido (todas as movimentações).
    /// Ref: https://open.shopee.com/documents/v2/v2.logistics.get_tracking_info
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="orderSn">Número do pedido</param>
    /// <param name="packageNumber">Número do pacote (opcional — usar quando o pedido tem split de pacotes)</param>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     GET /shopee-interface/logistics/tracking-info?shopId=226289035&amp;orderSn=260227RU6TQ98E
    ///
    /// Com package_number:
    ///
    ///     GET /shopee-interface/logistics/tracking-info?shopId=226289035&amp;orderSn=260227RU6TQ98E&amp;packageNumber=PKG-001
    ///
    /// Resposta esperada:
    ///
    ///     {
    ///       "response": {
    ///         "tracking_number": "BR123456789",
    ///         "shipping_carrier": "Correios",
    ///         "tracking_info": [
    ///           {
    ///             "update_time": 1706901234,
    ///             "description": "Objeto postado",
    ///             "status": "SHIPPED"
    ///           }
    ///         ]
    ///       }
    ///     }
    /// </remarks>
    [HttpGet("logistics/tracking-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrackingInfo(
        [FromQuery] long shopId,
        [FromQuery] string orderSn,
        [FromQuery] string? packageNumber = null)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetTrackingInfo - ShopId: {ShopId}, OrderSn: {OrderSn}", shopId, orderSn);

        try
        {
            if (shopId <= 0)
                return BadRequest(new { error = "Valid shopId is required" });

            if (string.IsNullOrWhiteSpace(orderSn))
                return BadRequest(new { error = "orderSn is required" });

            var result = await _shopeeApiService.GetTrackingInfoAsync(shopId, orderSn, packageNumber);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting tracking info - ShopId: {ShopId}, OrderSn: {OrderSn}", shopId, orderSn);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém os parâmetros disponíveis para geração do documento de envio (etiqueta).
    /// Retorna os tipos de documento disponíveis e informações necessárias antes de chamar create_shipping_document.
    /// Ref: https://open.shopee.com/documents/v2/v2.logistics.get_shipping_document_parameter
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="request">Lista de pedidos com order_sn e opcionalmente package_number</param>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     POST /shopee-interface/logistics/shipping-document-parameter?shopId=226289035
    ///     {
    ///         "order_list": [
    ///             { "order_sn": "201201E81SYYKE", "package_number": "60489687088750" },
    ///             { "order_sn": "201201V81SYYDG" }
    ///         ]
    ///     }
    ///
    /// Fluxo recomendado:
    /// 1. POST logistics/shipping-document-parameter → verifica tipos disponíveis
    /// 2. POST logistics/shipping-document/create    → cria o documento
    /// 3. POST logistics/shipping-document/result    → aguarda status READY
    /// 4. POST logistics/shipping-document/download  → baixa o PDF
    /// </remarks>
    [HttpPost("logistics/shipping-document-parameter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetShippingDocumentParameter(
        [FromQuery] long shopId,
        [FromBody] ShippingDocumentRequest request)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetShippingDocumentParameter - ShopId: {ShopId}, Orders: {Count}",
            shopId, request?.OrderList?.Count ?? 0);

        try
        {
            if (shopId <= 0)
                return BadRequest(new { error = "Valid shopId is required" });

            if (request?.OrderList == null || request.OrderList.Count == 0)
                return BadRequest(new { error = "order_list is required and cannot be empty" });

            var result = await _shopeeApiService.GetShippingDocumentParameterAsync(shopId, request.OrderList.Cast<object>());
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting shipping document parameter - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cria o documento de envio (etiqueta) para um ou mais pedidos.
    /// Deve ser chamado antes de get-shipping-document e download-shipping-document.
    /// Ref: https://open.shopee.com/documents/v2/v2.logistics.create_shipping_document
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="request">Lista de pedidos para os quais criar o documento</param>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     POST /shopee-interface/logistics/shipping-document/create?shopId=226289035
    ///     {
    ///         "order_list": [
    ///             { "order_sn": "260227RU6TQ98E" }
    ///         ]
    ///     }
    ///
    /// O campo "package_number" é opcional e necessário apenas quando o pedido possui split de pacotes.
    /// </remarks>
    [HttpPost("logistics/shipping-document/create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateShippingDocument(
        [FromQuery] long shopId,
        [FromBody] ShippingDocumentRequest request)
    {
        _logger.LogInformation("[SHOPEE-TEST] CreateShippingDocument - ShopId: {ShopId}, Orders: {Count}",
            shopId, request?.OrderList?.Count ?? 0);

        try
        {
            if (shopId <= 0)
                return BadRequest(new { error = "Valid shopId is required" });

            if (request?.OrderList == null || request.OrderList.Count == 0)
                return BadRequest(new { error = "order_list is required and cannot be empty" });

            var result = await _shopeeApiService.CreateShippingDocumentAsync(shopId, request.OrderList);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error creating shipping document - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtém o resultado do documento de envio, incluindo a URL do PDF da etiqueta.
    /// Endpoint Shopee: POST /api/v2/logistics/get_shipping_document_result
    /// Ref: https://open.shopee.com/documents/v2/v2.logistics.get_shipping_document_result
    ///
    /// Deve ser chamado após create_shipping_document.
    /// Repita a chamada até o status retornar READY antes de tentar o download direto.
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="request">Lista de pedidos e tipo do documento</param>
    /// <remarks>
    /// Exemplo de requisição:
    ///
    ///     POST /shopee-interface/logistics/shipping-document/result?shopId=226289035
    ///     {
    ///         "order_list": [
    ///             { "order_sn": "260227RU6TQ98E" }
    ///         ],
    ///         "shipping_document_type": "THERMAL_AIR_WAYBILL"
    ///     }
    ///
    /// Status possíveis na resposta: PROCESSING, READY, FAILED.
    /// Quando READY, a resposta conterá a URL do PDF da etiqueta.
    /// </remarks>
    [HttpPost("logistics/shipping-document/result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetShippingDocumentResult(
        [FromQuery] long shopId,
        [FromBody] DownloadShippingDocumentRequest request)
    {
        _logger.LogInformation("[SHOPEE-TEST] GetShippingDocumentResult - ShopId: {ShopId}, Orders: {Count}, DocumentType: {DocumentType}",
            shopId, request?.OrderList?.Count ?? 0, request?.ShippingDocumentType);

        try
        {
            if (shopId <= 0)
                return BadRequest(new { error = "Valid shopId is required" });

            if (request?.OrderList == null || request.OrderList.Count == 0)
                return BadRequest(new { error = "order_list is required and cannot be empty" });

            var docType = string.IsNullOrWhiteSpace(request.ShippingDocumentType)
                ? "THERMAL_AIR_WAYBILL"
                : request.ShippingDocumentType;

            var result = await _shopeeApiService.GetShippingDocumentResultAsync(shopId, request.OrderList.Cast<object>(), docType);
            return Ok(result.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error getting shipping document result - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Faz o download da etiqueta de envio.
    /// Somente deve ser chamado após create e quando get-shipping-document retornar status READY.
    /// Ref: https://open.shopee.com/documents/v2/v2.logistics.download_shipping_document
    /// </summary>
    /// <param name="shopId">ID da loja</param>
    /// <param name="request">Lista de pedidos e tipo de documento</param>
    /// <remarks>
    /// Fluxo correto:
    /// 1. POST logistics/shipping-document/create  → cria o documento
    /// 2. POST logistics/shipping-document/status  → aguarda status READY
    /// 3. POST logistics/shipping-document/download → baixa a etiqueta (URL do PDF)
    ///
    /// Exemplo de requisição:
    ///
    ///     POST /shopee-interface/logistics/shipping-document/download?shopId=226289035
    ///     {
    ///         "order_list": [
    ///             { "order_sn": "260227RU6TQ98E" }
    ///         ],
    ///         "shipping_document_type": "THERMAL_AIR_WAYBILL"
    ///     }
    ///
    /// Valores possíveis para shipping_document_type:
    /// - THERMAL_AIR_WAYBILL  (etiqueta térmica - mais comum)
    /// - A4_AIR_WAYBILL       (formato A4)
    /// - OFFICIAL_AIR_WAYBILL (etiqueta oficial da transportadora)
    /// </remarks>
    [HttpPost("logistics/shipping-document/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadShippingDocument(
        [FromQuery] long shopId,
        [FromBody] DownloadShippingDocumentRequest request)
    {
        _logger.LogInformation("[SHOPEE-TEST] DownloadShippingDocument - ShopId: {ShopId}, Orders: {Count}, DocumentType: {DocumentType}",
            shopId, request?.OrderList?.Count ?? 0, request?.ShippingDocumentType);

        try
        {
            if (shopId <= 0)
                return BadRequest(new { error = "Valid shopId is required" });

            if (request?.OrderList == null || request.OrderList.Count == 0)
                return BadRequest(new { error = "order_list is required and cannot be empty" });

            var docType = string.IsNullOrWhiteSpace(request.ShippingDocumentType)
                ? "THERMAL_AIR_WAYBILL"
                : request.ShippingDocumentType;

            var (fileBytes, contentType) = await _shopeeApiService.DownloadShippingDocumentAsync(
                shopId, request.OrderList.Cast<object>(), docType);

            var fileName = docType == "THERMAL_AIR_WAYBILL"
                ? $"shipping_label_{shopId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"
                : $"shipping_document_{shopId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

            _logger.LogInformation("[SHOPEE-TEST] DownloadShippingDocument - returning file: {FileName}, Bytes: {Bytes}",
                fileName, fileBytes.Length);

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SHOPEE-TEST] Error downloading shipping document - ShopId: {ShopId}", shopId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    #endregion
}

/// <summary>
/// Response com URL de autenticação
/// </summary>
public class AuthUrlResponse
{
    public string AuthUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response com tokens de acesso
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long ExpiresIn { get; set; }
}

/// <summary>
/// Response com token de acesso do cache
/// </summary>
public class CachedTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
}

/// <summary>
/// Request para criar ou verificar status do documento de envio
/// </summary>
public class ShippingDocumentRequest
{
    /// <summary>
    /// Lista de pedidos. Cada item deve ter ao menos order_sn.
    /// O campo package_number é opcional (usado quando o pedido tem split de pacotes).
    /// Exemplo: [{ "order_sn": "260227RU6TQ98E" }]
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("order_list")]
    public List<ShippingOrderItem> OrderList { get; set; } = new();
}

/// <summary>
/// Request para download do documento de envio
/// </summary>
public class DownloadShippingDocumentRequest
{
    /// <summary>
    /// Lista de pedidos. Cada item deve ter ao menos order_sn.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("order_list")]
    public List<ShippingOrderItem> OrderList { get; set; } = new();

    /// <summary>
    /// Tipo do documento de envio.
    /// Valores: THERMAL_AIR_WAYBILL (default), A4_AIR_WAYBILL, OFFICIAL_AIR_WAYBILL
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("shipping_document_type")]
    public string ShippingDocumentType { get; set; } = "THERMAL_AIR_WAYBILL";
}

/// <summary>
/// Item de pedido para requisições de documento de envio
/// </summary>
public class ShippingOrderItem
{
    /// <summary>
    /// Número do pedido na Shopee
    /// </summary>
    [JsonPropertyName("order_sn")]
    public string OrderSn { get; set; } = string.Empty;

    /// <summary>
    /// Número do pacote (opcional - usado quando o pedido tem split de pacotes)
    /// </summary>
    [JsonPropertyName("package_number")]
    public string? PackageNumber { get; set; }
    /// <summary>
    /// Número do pacote (opcional - usado quando o pedido tem split de pacotes)
    /// </summary>
    [JsonPropertyName("tracking_number")]
    public string? TrackingNumber { get; set; }
    
    [JsonPropertyName("shipping_document_type")]
    public string ShippingDocumentType { get; set; } = "NORMAL_AIR_WAYBILL";
}

