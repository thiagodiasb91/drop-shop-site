using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Dropship.Configuration;
using Dropship.Domain;
using Dropship.Repository;

namespace Dropship.Services;

public class AuthenticationService(UserRepository userRepository)
{
    public async Task<(string?, DateTime? expiresAt)> ProcessCallbackAsync(string code, string origin)
    {
        var redirectUrl = GetRedirectUrl(origin);
        if (redirectUrl == null) return (null, null);

        var tokens = await ExchangeCodeForTokens(code, redirectUrl);
        if (tokens == null) return (null,null);

        if (!tokens.Value.TryGetProperty("token_type", out var tokenType) || tokenType.GetString() != "Bearer")
            return (null,null);

        var idToken = tokens.Value.GetProperty("id_token").GetString();
        var accessToken = tokens.Value.GetProperty("access_token").GetString();

        if (string.IsNullOrWhiteSpace(idToken) || string.IsNullOrWhiteSpace(accessToken))
            return (null,null);

        var claims = await ValidateIdToken(idToken!, accessToken!);
        if (claims == null) return (null,null);

        var email = claims.TryGetValue("email", out var emailStr) ? emailStr!.GetString()! : string.Empty;
        var user = await userRepository.GetUser(email) ?? new UserDomain()
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            Role = "new-user"
        };

        return GenerateSessionToken(user);
    }

    public object? ValidateSessionToken(string token)
    {
        try
        { 
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(AuthConfig.SESSION_SECRET);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParameters, out _);
            var role = principal.Claims.FirstOrDefault(x => x.Type == "role")?.Value;
            
            long? iat = null, exp = null;
            var iatClaim = principal.FindFirst(JwtRegisteredClaimNames.Iat);
            var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp);
            if (iatClaim != null && long.TryParse(iatClaim.Value, out var iatv)) iat = iatv;
            if (expClaim != null && long.TryParse(expClaim.Value, out var expv)) exp = expv;

            var email = principal.FindFirst("email")?.Value;
            var resourceId = principal.FindFirst("resourceId")?.Value;
            return new
            {
                user = new { email },
                role,
                resourceId,
                session = new { iat, exp }
            };
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public string GenerateLoginUrl(string referer)
    {
        var map = new Dictionary<string, string>()
        {
            { "http://localhost:5173/", "http://localhost:5173/callback" },
            { "https://duz838qu410buj.cloudfront.net/", "https://duz838qu40buj.cloudfront.net/callback" },
            { "https://d2rjoik9cb60m4.cloudfront.net/", "https://d2rjoik9cb60m4.cloudfront.net/callback" }  
        };
        
        var redirectUri = map.GetValueOrDefault(referer, "");
        
        var parameters = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = AuthConfig.CLIENT_ID,
            ["redirect_uri"] = redirectUri,
            ["scope"] = "openid email profile"
        };

        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"https://{AuthConfig.COGNITO_DOMAIN}/login?{query}";
    }

    private string? GetRedirectUrl(string origin)
    {
        return AuthConfig.RedirectMap.TryGetValue(origin, out var redirect) ? redirect : null;
    }

    private async Task<JsonElement?> ExchangeCodeForTokens(string code, string redirectUri)
    {
        using var http = new HttpClient();
        var values = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "redirect_uri", redirectUri },
            { "code", code },
            { "client_id", AuthConfig.CLIENT_ID }
        };

        using var content = new FormUrlEncodedContent(values);
        using var res = await http.PostAsync(AuthConfig.TOKEN_URL, content);
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public (string token, DateTime expireAt) GenerateSessionToken(UserDomain user)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sessionClaims = new List<Claim>
        {
            new Claim("email", user.Email),
            new Claim("role", user.Role),
            new Claim("id", user.Id),
            new Claim("resourceId", user.ResourceId ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToString(), ClaimValueTypes.Integer64)
        };

        var symKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthConfig.SESSION_SECRET));
        var creds = new SigningCredentials(symKey, SecurityAlgorithms.HmacSha256);

        var expiration = DateTime.UtcNow.AddSeconds(AuthConfig.SESSION_TTL);
        
        var sessionToken = new JwtSecurityToken(
            claims: sessionClaims,
            notBefore: DateTime.UtcNow,
            expires: expiration,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(sessionToken), expiration);
    }

    private async Task<Dictionary<string, JsonElement>?> ValidateIdToken(string idToken, string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(idToken);
            var kid = jwt.Header.TryGetValue("kid", out var kidVal) ? kidVal?.ToString() : null;
            if (kid == null) return null;

            var jwks = await GetJwks();
            if (jwks == null) return null;

            if (!jwks.Value.TryGetProperty("keys", out var keys)) return null;

            JsonElement? keyElem = null;
            foreach (var k in keys.EnumerateArray())
            {
                if (k.TryGetProperty("kid", out var kKid) && kKid.GetString() == kid)
                {
                    keyElem = k;
                    break;
                }
            }

            if (keyElem == null) return null;

            var kElm = keyElem.Value;
            var n = kElm.GetProperty("n").GetString()!;
            var e = kElm.GetProperty("e").GetString()!;

            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(n),
                Exponent = Base64UrlDecode(e)
            });

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://cognito-idp.us-east-1.amazonaws.com/{AuthConfig.USER_POOL_ID}",
                ValidateAudience = true,
                ValidAudience = AuthConfig.CLIENT_ID,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),
                ValidateLifetime = true
            };

            handler.ValidateToken(idToken, validationParameters, out var validatedToken);
            var payload = new JwtSecurityToken(idToken).Payload.SerializeToJson();
            var doc = JsonDocument.Parse(payload);
            var dict = new Dictionary<string, JsonElement>();
            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = prop.Value;

            return dict;
        }
        catch
        {
            return null;
        }
    }

    private async Task<JsonElement?> GetJwks()
    {
        var url = $"https://cognito-idp.us-east-1.amazonaws.com/{AuthConfig.USER_POOL_ID}/.well-known/jwks.json";
        using var http = new HttpClient();
        var res = await http.GetAsync(url);
        
        if (!res.IsSuccessStatusCode) return null;
        
        var json = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string s = input;
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 0: break;
            case 2: s += "=="; break;
            case 3: s += "="; break;
            default: throw new FormatException("Invalid base64url string");
        }
        return Convert.FromBase64String(s);
    }

}