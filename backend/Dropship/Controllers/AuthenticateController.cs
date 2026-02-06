using Dropship.Repository;
using Microsoft.AspNetCore.Mvc;
using Dropship.Requests;
using Dropship.Responses;
using Dropship.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dropship.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticateController(AuthenticationService authService, ILogger<AuthenticateController> logger, ShopeeService shopeeService, UserRepository userRepository)
    : ControllerBase
{
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] CallbackRequest? request)
    {
        var origin = Request.Headers["Origin"].FirstOrDefault() ?? string.Empty;
        logger.LogInformation("[AUTH] Processing authentication callback for origin: {Origin}", origin);
            

        if (request is null || string.IsNullOrWhiteSpace(request.Code))
        {
            logger.LogWarning("[AUTH] Missing authorization code in callback request from origin: {Origin}", origin);
            return BadRequest(new { error = "Missing authorization code" });
        }

        (string? sessionToken, DateTime? expiresAt) = await authService.ProcessCallbackAsync(request.Code, origin);
        if (sessionToken == null)
        {
            logger.LogError("[AUTH] Authentication failed for origin: {Origin} with code: {Code}", origin, request.Code?.Substring(0, Math.Min(10, request.Code.Length)) + "...");
            return BadRequest(new { error = "Authentication failed" });
        }

        logger.LogInformation("[AUTH] Authentication successful for origin: {Origin}", origin);
        return Ok(new
        {
            sessionToken, 
            expiresAt
        });
    }

    [HttpPost("renew")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RenewToken()
    {
        logger.LogInformation("[AUTH] Renewing session token");

        var token = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value.ToString();
        var user = await userRepository.GetUser(token);
        
        var result = authService.GenerateSessionToken(user);
        
        if (string.IsNullOrWhiteSpace(result.token))
        {
            logger.LogWarning("[AUTH] Invalid session token provided for renewal");
            return Unauthorized(new { error = "invalid_token" });
        }

        logger.LogInformation("[AUTH] Session token renewed successfully");
        return Ok(new
        {
            sessionToken = result.token,
            expireAt = result.expireAt
        });
    }
    
    /// <summary>
    /// Endpoint para autenticar com Shopee e armazenar tokens
    /// </summary>
    /// <param name="request">Request contendo code, shop_id e email</param>
    /// <returns>Resposta com status da autenticação</returns>
    [HttpPost("confirm-shopee")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmShopee([FromBody] ShopeeAuthRequest request)
    {
        logger.LogInformation("Shopee authentication request - Code: {Code}, ShopId: {ShopId}, Email: {Email}", 
            request.Code, request.ShopId, request.Email);
        try
        {
            await shopeeService.AuthenticateShopAsync(request.Code, request.ShopId, request.Email);

            logger.LogInformation("Shop authenticated successfully - ShopId: {ShopId}, Email: {Email}", 
                request.ShopId, request.Email);

            return Ok(new
            {
                StatusCode = 200,
                Message = $"Tokens saved for shop {request.ShopId}"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error during authentication - ShopId: {ShopId}, Email: {Email}", 
                request.ShopId, request.Email);
            return BadRequest(new ShopeeWebhookResponse
            {
                StatusCode = 400,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error authenticating shop - ShopId: {ShopId}, Email: {Email}", 
                request.ShopId, request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                StatusCode = 500,
                Message = "Internal server error"
            });
        }
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        logger.LogInformation("Validating user session");
        
        var authorization = Request.Headers.Authorization.FirstOrDefault();
        var token = authorization != null && authorization.StartsWith("Bearer ") ? authorization.Substring("Bearer ".Length).Trim() : null;

        if (string.IsNullOrWhiteSpace(token) || token == "null")
        {
            logger.LogWarning("Token not found in request");
            return Unauthorized(new { error = "token_not_found" });
        }

        // Validar se o token tem o formato correto (3 segmentos para JWT)
        if (token.Split('.').Length != 3)
        {
            logger.LogWarning("Invalid JWT format - token does not have 3 segments");
            return Unauthorized(new { error = "invalid_token_format" });
        }

        var result = authService.ValidateSessionToken(token);
        if (result == null)
        {
            logger.LogWarning("Invalid session token provided");
            return Unauthorized(new { error = "invalid_token" });
        }

        logger.LogInformation("Session validation successful");
        return Ok(result);
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var referer = Request.Headers["Referer"].ToString();
        logger.LogInformation("Generating login URL for referer: {Referer}", referer);
        
        var url = authService.GenerateLoginUrl(referer);
        
        Response.Headers.Location = url;
        return StatusCode(302);
    }

}

