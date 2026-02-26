namespace Dropship.Configuration;

public static class AuthConfig
{
    public static readonly string COGNITO_DOMAIN = Environment.GetEnvironmentVariable("COGNITO_DOMAIN") ??
                                                   "drop-shop-dev-admin-auth.auth.us-east-1.amazoncognito.com";

    public static readonly string CLIENT_ID =
        Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID") ?? "6jp62ofu16kilar90619g6naq0";

    public static readonly string USER_POOL_ID =
        Environment.GetEnvironmentVariable("USER_POOL_ID") ?? "us-east-1_NjIF2zIQf";

    public static readonly string SESSION_SECRET =
        Environment.GetEnvironmentVariable("SESSION_SECRET") ?? "session-secret-bff";

    public static readonly int SESSION_TTL =
        int.TryParse(Environment.GetEnvironmentVariable("SESSION_TTL"), out var ttl) ? ttl : 2880;

    public static readonly string TOKEN_URL = $"https://{COGNITO_DOMAIN}/oauth2/token";

    public static readonly Dictionary<string, string> RedirectMap = new()
    {
        { "http://localhost:5173", "http://localhost:5173/callback" },
        { "https://d34qn8nhq3y13z.cloudfront.net", "https://d34qn8nhq3y13z.cloudfront.net/callback" },
        { "https://d121i7s3p0c7di.cloudfront.net", "https://d121i7s3p0c7di.cloudfront.net/callback" },
        { "https://duz838qu40buj.cloudfront.net", "https://duz838qu40buj.cloudfront.net/callback" }
    };
}