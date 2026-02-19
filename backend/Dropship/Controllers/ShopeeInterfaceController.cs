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

