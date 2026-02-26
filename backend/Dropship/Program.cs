using System.Diagnostics;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Globalization;
using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using Amazon.SQS;
using Dropship.Configuration;
using Dropship.Repository;
using Dropship.Services;
using Dropship.Middlewares;
using Dropship.Logging;
using Dropship.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

// Configure culture to use English format (dot as decimal separator)
var cultureInfo = new CultureInfo("en-US")
{
    NumberFormat = new NumberFormatInfo
    {
        NumberDecimalSeparator = ".",
        CurrencyDecimalSeparator = ".",
        PercentDecimalSeparator = "."
    }
};
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configure built-in logging with CorrelationId at the start of each message
builder.Logging.ClearProviders();

// Register custom formatter options
builder.Services.Configure<CorrelationIdFormatterOptions>(options =>
{
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    options.IncludeScopes = true;
    options.UseUtcTimestamp = false;
});

// Register custom console formatter
builder.Services.AddSingleton<ConsoleFormatter, CorrelationIdConsoleFormatter>();

// Add console logging with custom formatter
builder.Logging.AddConsole(options =>
{
    options.FormatterName = nameof(CorrelationIdConsoleFormatter);
});

// Add CORS policy for the frontend origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins( AuthConfig.RedirectMap.Select( x=> x.Key).ToArray())
              .AllowCredentials()
              .WithHeaders("Content-Type", "X-Amz-Date", "Authorization", "X-Api-Key", "X-Amz-Security-Token")
              .WithMethods("GET", "POST", "PUT","DELETE", "OPTIONS");
    });
});

builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(AuthConfig.SESSION_SECRET)
                        ),
                        ClockSkew = TimeSpan.Zero
                    };

                    // 👇 ESSENCIAL para não quebrar CORS
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.HttpContext.Request.Method == "OPTIONS")
                                context.NoResult();
                            
                            return Task.CompletedTask;
                        }
                    };
                });

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Dropship API",
        Version = "v1",
        Description = "Dropship Backend API - Manage products, orders, and suppliers"
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Read AWS credentials from configuration (user secrets) or environment variables
var accessKey = builder.Configuration["AWS:ACCESS_KEY_ID"]
                ?? builder.Configuration["AWS:ACCESS_KEY"]
                ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");

var secretKey = builder.Configuration["AWS:SECRET_ACCESS_KEY"]
                ?? builder.Configuration["AWS:SECRET_KEY"]
                ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("AWS credentials not configured. Set AWS:ACCESS_KEY_ID and AWS:SECRET_ACCESS_KEY in user secrets or environment variables.");
}

var awsCredentials = new BasicAWSCredentials(accessKey, secretKey);

builder.Services.AddScoped<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(
    awsCredentials,
    Amazon.RegionEndpoint.USEast1)
);

builder.Services.AddScoped<IDynamoDBContext>(provider => 
{
    var dynamoDbClient = provider.GetRequiredService<IAmazonDynamoDB>();
    return new DynamoDBContext(dynamoDbClient);
});


builder.Services.AddScoped<IAmazonCognitoIdentityProvider>(_ => new AmazonCognitoIdentityProviderClient(
    awsCredentials,
    Amazon.RegionEndpoint.USEast1)
);

builder.Services.AddScoped<IAmazonSQS>(_ => new AmazonSQSClient(
    awsCredentials,
    Amazon.RegionEndpoint.USEast1)
);


ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
// Configure HttpClient with SSL certificate validation bypass for development (macOS compatibility)
builder.Services.AddHttpClient("default")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };
    });

// Registrar HttpClient factory
builder.Services.AddTransient(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("default");
});

// Registrar CacheService usando DynamoDB para persistir tokens
builder.Services.AddScoped<ICacheService, DynamoDbCacheService>();

builder.Services.AddScoped<ShopeeApiService>();
builder.Services.AddScoped<DynamoDbRepository>();
builder.Services.AddScoped<SupplierRepository>();
builder.Services.AddScoped<SellerRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<KardexRepository>();
builder.Services.AddScoped<StockRepository>();
builder.Services.AddScoped<ProductRepository>();
builder.Services.AddScoped<SkuRepository>();
builder.Services.AddScoped<ProductSkuSupplierRepository>();
builder.Services.AddScoped<ProductSupplierRepository>();
builder.Services.AddScoped<ProductSkuSellerRepository>();
builder.Services.AddScoped<ProductSellerRepository>();
builder.Services.AddScoped<KardexRepository>();
builder.Services.AddScoped<KardexService>();
builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<SupplierShipmentRepository>();
builder.Services.AddScoped<InfinityPayLinkRepository>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<ShopeeService>();
builder.Services.AddScoped<StockServices>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<InfinityPayService>();
builder.Services.AddMemoryCache();
builder.Services.AddAuthorization();

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear(); 

var app = builder.Build();

// Enable Swagger/OpenAPI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var prefix = !Debugger.IsAttached ? "/dev" : ""; // o /dev é quando está hospedado na AWS
        options.SwaggerEndpoint($"{prefix}/swagger/v1/swagger.json", "Dropship API v1");
        options.RoutePrefix = "swagger";
    });
}

// Enable CORS middleware

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<HttpBodyLoggingMiddleware>();
app.UseMiddleware<RouteDebugMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCors("DefaultCorsPolicy");

app.MapGet("/", () => $"Dropship working! {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
app.Run();