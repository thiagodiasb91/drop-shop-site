# 🏗️ Arquitetura do Dropship API

## Visão Geral da Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                      API Clients                                 │
│         (Web, Mobile, Third-party Integrations)                  │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    API Gateway / Load Balancer                  │
│              (AWS API Gateway / Application Load Balancer)       │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Middleware Pipeline                          │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ 1. CorrelationIdMiddleware    - ID único por requisição │    │
│  │ 2. RequestBodyLoggingMiddleware - Log entrada completa  │    │
│  │ 3. Authentication Middleware     - Validação JWT        │    │
│  │ 4. ResponseBodyLoggingMiddleware - Log saída completa   │    │
│  │ 5. RouteDebugMiddleware          - Debug de rotas       │    │
│  └─────────────────────────────────────────────────────────┘    │
└────────────────────────────┬────────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        ▼                    ▼                    ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│  Controllers     │ │  Controllers     │ │  Controllers     │
│  ShopeeWebhook  │ │  Auth            │ │  Stock/Payment   │
└────────┬─────────┘ └────────┬─────────┘ └────────┬─────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌──────────────────────────────────────────────────────────────┐
│                    Services Layer                            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ ShopeeApiService        - API Shopee                │   │
│  │ ShopeeService           - Processamento webhooks    │   │
│  │ AuthenticationService   - JWT & Cognito             │   │
│  │ PaymentService          - Pagamentos                │   │
│  │ KardexService           - Movimento estoque         │   │
│  └──────────────────────────────────────────────────────┘   │
└────────────────┬──────────────────────────────────────────────┘
                 │
        ┌────────┼────────┐
        ▼        ▼        ▼
┌──────────────────┐ ┌──────────────────┐
│  Repository      │ │  External APIs   │
│  Pattern         │ │  (Shopee, AWS)   │
└────────┬─────────┘ └────────┬─────────┘
         │                    │
         ▼                    ▼
┌──────────────────────────────────────────┐
│         Data Persistence Layer           │
│  ┌──────────────────────────────────┐    │
│  │ DynamoDB (Primary Data Store)    │    │
│  │  - Users                         │    │
│  │  - Sellers                       │    │
│  │  - Suppliers                     │    │
│  │  - Orders & Payments             │    │
│  │  - Stock & Kardex                │    │
│  │  - Tokens & Cache                │    │
│  └──────────────────────────────────┘    │
└──────────────────────────────────────────┘
                 │
        ┌────────┴────────┐
        ▼                 ▼
    ┌────────┐        ┌────────┐
    │  SQS   │        │  Cache │
    │ Queues │        │ Service│
    └────────┘        └────────┘
```

## Camadas da Aplicação

### 1️⃣ Presentation Layer (Apresentação)

#### Controllers
Responsáveis por:
- Receber requisições HTTP
- Validação básica de entrada
- Delegação para Services
- Formatação de resposta

**Exemplo: ShopeeWebhookController**
```csharp
[ApiController]
[Route("shopee/webhook")]
public class ShopeeWebhookController : ControllerBase
{
    private readonly ShopeeService _shopeeService;
    private readonly ILogger<ShopeeWebhookController> _logger;

    [HttpGet("auth")]
    [AllowAnonymous]
    public async Task<IActionResult> AuthenticateShop(
        [FromQuery] string code,
        [FromQuery] long shopId,
        [FromQuery] string email)
    {
        // Validação
        if (string.IsNullOrWhiteSpace(code) || shopId <= 0 || string.IsNullOrWhiteSpace(email))
            return BadRequest("Invalid parameters");

        try
        {
            // Delegação para Service
            await _shopeeService.AuthenticateShopAsync(code, shopId.ToString(), email);
            
            // Resposta formatada
            return Ok(new ShopeeWebhookResponse
            {
                StatusCode = 200,
                Message = $"Tokens saved for shop {shopId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating shop");
            return StatusCode(500, new ShopeeWebhookResponse
            {
                StatusCode = 500,
                Message = "Internal server error"
            });
        }
    }
}
```

### 2️⃣ Application/Services Layer

#### Responsabilidades
- Lógica de negócio complexa
- Orquestração de repositórios
- Chamadas a APIs externas
- Validação de domínio
- Transações entre múltiplas entidades

#### Exemplo: ShopeeService
```csharp
public class ShopeeService
{
    private readonly ShopeeApiService _shopeeApiService;      // API externa
    private readonly SellerRepository _sellerRepository;      // Dados
    private readonly UserRepository _userRepository;          // Dados
    private readonly IAmazonSQS _sqsClient;                   // Evento
    private readonly ILogger<ShopeeService> _logger;

    public async Task AuthenticateShopAsync(string code, string shopId, string email)
    {
        // 1. Validação de negócio
        var user = await _userRepository.GetUser(email);
        if (user == null)
            throw new InvalidOperationException($"User {email} not found");

        // 2. Integração com API externa
        var (accessToken, refreshToken, expiresIn) = 
            await _shopeeApiService.GetTokenShopLevelAsync(code, shopId);

        // 3. Criar nova entidade
        var seller = new SellerDomain
        {
            SellerId = Guid.NewGuid().ToString(),
            SellerName = $"Shop_{shopId}",
            ShopId = long.Parse(shopId),
            Marketplace = "shopee"
        };

        // 4. Persistir dados
        var createdSeller = await _sellerRepository.CreateSellerAsync(seller);

        // 5. Atualizar entidade relacionada
        user.ResourceId = createdSeller.SellerId;
        await _userRepository.UpdateUserAsync(user);

        // 6. Armazenar tokens em cache
        await CacheTokensAsync(shopId, accessToken, refreshToken, expiresIn);

        _logger.LogInformation("Shop authenticated - ShopId: {ShopId}", shopId);
    }
}
```

#### Exemplo: ShopeeApiService
```csharp
public class ShopeeApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopeeApiService> _logger;
    private readonly string _partnerId;
    private readonly string _partnerKey;

    // OAuth2 com HMAC SHA256
    public async Task<(string AccessToken, string RefreshToken, long ExpiresIn)> 
        GetTokenShopLevelAsync(string code, string shopId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sign = GenerateSign("/api/v2/auth/token/get", timestamp);

        var url = $"{_host}/api/v2/auth/token/get?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}";

        var body = new { code, shop_id = shopId, partner_id = _partnerId };
        var response = await _httpClient.PostAsync(url, 
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

        // Parse resposta
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        
        return (
            GetJsonProperty(json, "access_token"),
            GetJsonProperty(json, "refresh_token"),
            ParseExpiresIn(json)
        );
    }

    private string GenerateSign(string path, long timestamp)
    {
        var baseString = $"{_partnerId}{path}{timestamp}";
        var key = Encoding.UTF8.GetBytes(_partnerKey);
        
        using (var hmac = new HMACSHA256(key))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
```

### 3️⃣ Data Access Layer (Repository Pattern)

#### Características
- Abstração de acesso a dados
- Separação entre lógica de negócio e dados
- Facilita testes (mock)
- DynamoDB como persistência

#### Arquitetura Repository
```csharp
// Classe base
public abstract class DynamoDbRepository
{
    protected IDynamoDBContext _context;

    public async Task<T> GetItemAsync<T>(
        object hashKey,
        object rangeKey = null) => /* implementação */

    public async Task<List<T>> QueryAsync<T>(
        string keyExpression,
        Dictionary<string, DynamoDBEntry> values) => /* implementação */

    public async Task PutItemAsync<T>(T item) => /* implementação */
}

// Implementação específica
public class SellerRepository : DynamoDbRepository
{
    public async Task<SellerDomain?> GetSellerByIdAsync(string sellerId)
    {
        var pk = $"Seller#{sellerId}";
        return await _context.LoadAsync<SellerDomain>(pk, "META");
    }

    public async Task<List<SellerDomain>> GetAllSellersAsync()
    {
        // Usa GSI_RELATIONS_LOOKUP
        var search = _context.QueryAsync<SellerDomain>(new QueryOperationConfig
        {
            IndexName = "GSI_RELATIONS_LOOKUP",
            // PK e SK invertidos no índice
        });

        return await search.GetRemainingAsync();
    }

    public async Task<SellerDomain> CreateSellerAsync(SellerDomain seller)
    {
        seller.PK = $"Seller#{seller.SellerId}";
        seller.SK = "META";
        seller.CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        await _context.SaveAsync(seller);
        return seller;
    }
}
```

### 4️⃣ Domain Model Layer

#### SellerDomain
```csharp
[DynamoDBTable("catalog-core")]
public class SellerDomain
{
    [DynamoDBHashKey("PK")]
    public string PK { get; set; }  // "Seller#{SellerId}"

    [DynamoDBRangeKey("SK")]
    public string SK { get; set; }  // "META"

    [DynamoDBProperty("sellerId")]
    public string SellerId { get; set; }

    [DynamoDBProperty("sellerName")]
    public string SellerName { get; set; }

    [DynamoDBProperty("shop_id")]
    public long ShopId { get; set; }

    [DynamoDBProperty("marketplace")]
    public string Marketplace { get; set; }

    [DynamoDBProperty("createdAt")]
    public long? CreatedAt { get; set; }

    [DynamoDBProperty("updatedAt")]
    public long? UpdatedAt { get; set; }
}
```

### 5️⃣ Infrastructure Layer

#### DynamoDB
```
Tabela: catalog-core

Chaves Primárias:
  - PK (Partition Key): Entity type + ID
  - SK (Sort Key): Version/Type (ex: META, V1, V2)

Índices Secundários Globais (GSI):
  - GSI_SHOPID_LOOKUP
    PK: shop_id
    SK: entity_type
    → Busca rápida por loja

  - GSI_RELATIONS_LOOKUP
    PK: SK (invertido)
    SK: PK (invertido)
    → Busca reversa

Estrutura de Items:
{
  "PK": {"S": "Seller#uuid"},
  "SK": {"S": "META"},
  "sellerId": {"S": "uuid"},
  "sellerName": {"S": "nome"},
  "shop_id": {"N": "123"},
  "marketplace": {"S": "shopee"},
  "entityType": {"S": "seller"},
  "createdAt": {"N": "1639234899"},
  "updatedAt": {"N": "1639234899"}
}
```

#### SQS Queue
```
Queue: shoppe-new-order-received-queue.fifo

Propósito: Processar pedidos de forma assíncrona

Mensagem:
{
  "ordersn": "2501080NKAMXA8",
  "status": "UNPAID",
  "shop_id": "123",
  "update_time": 1639234899
}

MessageGroupId: "{shop_id}-{ordersn}"
```

## Fluxos Principais

### 🔄 Fluxo 1: Autenticação Shopee

```
1. Cliente acessa: GET /shopee/webhook/auth?code=XXX&shopId=123&email=user@email.com

2. ShopeeWebhookController.AuthenticateShop()
   ├── Valida parâmetros
   └── Chama ShopeeService.AuthenticateShopAsync()

3. ShopeeService.AuthenticateShopAsync()
   ├── Busca usuário em UserRepository
   ├── Chama ShopeeApiService.GetTokenShopLevelAsync()
   │   └── Gera HMAC SHA256 com timestamp
   │   └── POST /api/v2/auth/token/get à API Shopee
   ├── Cria novo Seller em SellerRepository
   ├── Atualiza usuário com resource_id
   ├── Armazena tokens em cache
   └── Retorna sucesso

4. Resposta: 200 OK
   {
     "statusCode": 200,
     "message": "Tokens saved for shop 123"
   }
```

### 🔄 Fluxo 2: Webhook de Pedido

```
1. Shopee POST /shopee/webhook
   {
     "msg_id": "...",
     "shop_id": 123,
     "code": 3,
     "data": {"ordersn": "...", "status": "..."}
   }

2. ShopeeWebhookController.ReceiveWebhook()
   ├── Valida código do evento
   └── Chama ShopeeService.ProcessOrderReceivedAsync()

3. ShopeeService.ProcessOrderReceivedAsync()
   ├── Verifica se loja existe em ShopeeRepository
   ├── Envia mensagem para SQS
   └── Retorna 200 OK

4. SQS Consumer (Lambda)
   ├── Lê mensagem da fila
   ├── Atualiza status do pedido
   └── Notifica cliente
```

## Padrões Utilizados

### 🎯 Repository Pattern
```
Service → Repository → DynamoDB

Vantagens:
- Facilita testes com mocks
- Centraliza lógica de acesso a dados
- Abstrai detalhes do banco
```

### 🎯 Dependency Injection
```csharp
// Program.cs
builder.Services.AddScoped<IAmazonDynamoDB>(...);
builder.Services.AddScoped<IDynamoDBContext>(...);
builder.Services.AddScoped<SellerRepository>();
builder.Services.AddScoped<ShopeeApiService>();
builder.Services.AddScoped<ShopeeService>();

// Resolução automática
public class ShopeeService
{
    public ShopeeService(
        SellerRepository sellerRepository,
        ShopeeApiService shopeeApiService,
        ...)
    {
        // Dependências injetadas automaticamente
    }
}
```

### 🎯 Middleware Pipeline
```
Request →
  CorrelationId →
    RequestBodyLogging →
      Authentication →
        ResponseBodyLogging →
          Response
```

### 🎯 Async/Await
```csharp
public async Task AuthenticateShopAsync(string code, string shopId, string email)
{
    // Operações assíncronas não bloqueantes
    var user = await _userRepository.GetUser(email);
    var tokens = await _shopeeApiService.GetTokenShopLevelAsync(code, shopId);
    var seller = await _sellerRepository.CreateSellerAsync(sellerDomain);
}
```

## Considerações de Performance

### Caching
- Tokens armazenados em cache por 24h
- Reduz chamadas à API Shopee
- Fallback para refresh se expirado

### DynamoDB Optimization
- Índices GSI para queries frequentes
- Partition key bem distribuída
- TTL em items temporários

### Logging
- CorrelationId para rastreamento completo
- Logs estruturados para análise
- CloudWatch integration para monitoramento

## Segurança

### Autenticação
- JWT Bearer tokens
- AWS Cognito integrado
- Session tokens customizados

### Autorização
- Validação de escopos
- Verificação de resource ownership
- Rate limiting (implementar)

### Dados Sensíveis
- Tokens em cache com TTL
- Credenciais em Secrets Manager
- HTTPS obrigatório em produção
- CORS configurável

## Escalabilidade

### Design Stateless
- Nenhum estado em memória
- Sessões armazenadas em DynamoDB
- Escalável horizontalmente

### Async First
- HTTP cliente não-bloqueante
- Queue de eventos (SQS)
- Background jobs (Lambda)

### Infrastructure
- AWS Lambda (serverless)
- DynamoDB (auto-scale)
- CloudFront (CDN)
- SQS (async processing)
