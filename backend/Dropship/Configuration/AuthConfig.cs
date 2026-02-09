namespace Dropship.Configuration;

public static class AuthConfig
{
    public static readonly string COGNITO_DOMAIN = Environment.GetEnvironmentVariable("COGNITO_DOMAIN") ?? "drop-shop-admin-auth.auth.us-east-1.amazoncognito.com";
    public static readonly string CLIENT_ID = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID") ?? "1ovcuufeei9bbg5vkf059obj4p";
    public static readonly string USER_POOL_ID = Environment.GetEnvironmentVariable("USER_POOL_ID") ?? "us-east-1_OUGR0GKR6";
    public static readonly string SESSION_SECRET = Environment.GetEnvironmentVariable("SESSION_SECRET") ?? "session-secret-bff";
    public static readonly int SESSION_TTL = int.TryParse(Environment.GetEnvironmentVariable("SESSION_TTL"), out var ttl) ? ttl : 1800;
    
    public static readonly string TOKEN_URL = $"https://{COGNITO_DOMAIN}/oauth2/token";
    
    public static readonly Dictionary<string, string> RedirectMap = new()
    {
        { "http://localhost:5173", "http://localhost:5173/callback" },
        { "https://d35nbs4n8cbsw3.cloudfront.net", "https://d35nbs4n8cbsw3.cloudfront.net/callback" },
        {"https://duz838qu40buj.cloudfront.net", "https://duz838qu40buj.cloudfront.net"}
    };
}