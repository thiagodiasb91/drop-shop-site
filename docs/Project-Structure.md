# 📊 Estrutura Visual do Projeto

## 📁 Árvore do Projeto

```
Dropship/
│
├── 📄 README.md                          ← 📍 COMECE AQUI!
├── 📄 ARCHITECTURE.md                    ← Arquitetura detalhada
├── 📄 CONTRIBUTING.md                    ← Guia de contribuição
├── 📄 DEVELOPMENT.md                     ← Setup local
├── 📄 .gitignore                         ← Arquivos ignorados
├── 📄 .env.example                       ← Template de variáveis
│
├── 📁 Dropship/                          ← Projeto principal
│   │
│   ├── 📄 Program.cs                     ← ⚙️ Configuração DI & Startup
│   ├── 📄 Dropship.csproj                ← Arquivo de projeto
│   │
│   ├── 📁 Controllers/                   ← 🎯 Endpoints REST
│   │   ├── AuthenticateController.cs
│   │   ├── ShopeeWebhookController.cs    ← 🔹 Webhooks Shopee
│   │   ├── PaymentsController.cs
│   │   ├── StockController.cs
│   │   ├── KardexController.cs
│   │   ├── SupplierController.cs
│   │   └── UserController.cs
│   │
│   ├── 📁 Services/                      ← 💼 Lógica de negócio
│   │   ├── ShopeeApiService.cs           ← 🔹 API Shopee (HMAC, OAuth2)
│   │   ├── ShopeeService.cs              ← 🔹 Processamento webhooks
│   │   ├── AuthenticationService.cs      ← JWT & Cognito
│   │   ├── PaymentService.cs
│   │   └── KardexService.cs
│   │
│   ├── 📁 Repository/                    ← 💾 Acesso a dados (DynamoDB)
│   │   ├── DynamoDbRepository.cs         ← Base abstrata
│   │   ├── ShopeeRepository.cs           ← 🔹 Queries Shopee
│   │   ├── SellerRepository.cs           ← 🔹 CRUD Sellers
│   │   ├── SupplierRepository.cs
│   │   ├── UserRepository.cs
│   │   ├── KardexRepository.cs
│   │   └── StockRepository.cs
│   │
│   ├── 📁 Domain/                        ← 🏢 Modelos de domínio
│   │   ├── SellerDomain.cs               ← 🔹 Seller (Vendedor)
│   │   ├── UserDomain.cs
│   │   ├── SupplierDomain.cs
│   │   ├── PaymentDomain.cs
│   │   ├── KardexDomain.cs
│   │   └── StockDomain.cs
│   │
│   ├── 📁 Requests/                      ← 📥 DTOs de entrada
│   │   ├── ShopeeWebhookRequest.cs       ← 🔹 Webhook payload
│   │   ├── CreateSupplierRequest.cs
│   │   ├── UpdateSupplierRequest.cs
│   │   ├── UpdateStockRequest.cs
│   │   ├── UpdateUserRequest.cs
│   │   └── CallbackRequest.cs
│   │
│   ├── 📁 Responses/                     ← 📤 DTOs de saída
│   │   ├── ShopeeWebhookResponse.cs      ← 🔹 Webhook response
│   │   ├── SupplierResponse.cs
│   │   ├── SupplierListResponse.cs
│   │   └── PaymentResponse.cs
│   │
│   ├── 📁 Middlewares/                   ← 🔀 Pipeline HTTP
│   │   ├── CorrelationIdMiddleware.cs    ← Geração de ID único
│   │   ├── RequestBodyLoggingMiddleware.cs  ← 🔹 Log requisição
│   │   ├── ResponseBodyLoggingMiddleware.cs ← 🔹 Log resposta
│   │   └── RouteDebugMiddleware.cs
│   │
│   ├── 📁 Logging/                       ← 🎨 Formatação logs
│   │   └── CorrelationIdConsoleFormatter.cs ← 🔹 Formatter customizado
│   │
│   ├── 📁 Configuration/                 ← ⚙️ Configurações
│   │   └── AuthConfig.cs
│   │
│   ├── 📁 Properties/
│   │   └── launchSettings.json
│   │
│   ├── 📄 appsettings.json               ← Config base
│   ├── 📄 appsettings.Development.json   ← Dev-specific
│   └── 📄 appsettings.dynamodb.json      ← DynamoDB config
│
├── 📁 Dropship.Tests/                    ← 🧪 Testes unitários
│   ├── Services/
│   ├── Repository/
│   └── Controllers/
│
├── 📁 bin/                               ← Build output
├── 📁 obj/                               ← Objeto compilado
└── 📁 publish/                           ← Publicação para Lambda
```

## 🔹 Partes-Chave do Projeto

### Controllers
```
Controllers/
├── AuthenticateController     - Autenticação JWT
├── ShopeeWebhookController    - 🔥 Webhooks + OAuth2
├── PaymentsController         - Processamento de pagamentos
├── StockController            - Gestão de estoque
├── KardexController           - Histórico de movimentação
├── SupplierController         - Gerenciamento de fornecedores
└── UserController             - Gerenciamento de usuários
```

### Services
```
Services/
├── ShopeeApiService           - 🔥 API Shopee (HMAC SHA256, refresh tokens)
├── ShopeeService              - 🔥 Orquestração webhook + Seller
├── AuthenticationService      - JWT validation
├── PaymentService             - Pagamentos
└── KardexService              - Kardex
```

### Repository (Data Access)
```
Repository/
├── DynamoDbRepository         - Classe base abstrata
├── SellerRepository           - 🔥 CRUD Sellers
├── ShopeeRepository           - 🔥 Queries Shopee
├── SupplierRepository         - CRUD Suppliers
├── UserRepository             - CRUD Users
├── KardexRepository           - Kardex queries
└── StockRepository            - Stock queries
```

### Domain Models
```
Domain/
├── SellerDomain               - 🔥 Entity Seller
│   ├── PK: "Seller#{SellerId}"
│   ├── SK: "META"
│   ├── shop_id: long
│   └── marketplace: string
│
├── UserDomain                 - Entity User
├── SupplierDomain             - Entity Supplier
├── PaymentDomain              - Entity Payment
├── KardexDomain               - Entity Kardex
└── StockDomain                - Entity Stock
```

## 🔄 Fluxos de Dados

### Fluxo 1: Autenticação Shopee
```
┌─────────────────────────────────────────────────────────────┐
│ GET /shopee/webhook/auth?code=XXX&shopId=123&email=user@... │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
        ┌────────────────────────────────┐
        │ ShopeeWebhookController        │
        │ - Valida parâmetros            │
        └────────────┬───────────────────┘
                     │
                     ▼
        ┌────────────────────────────────┐
        │ ShopeeService.                 │
        │ AuthenticateShopAsync()        │
        │ 1. Busca usuário               │
        │ 2. Chama Shopee API            │
        │ 3. Cria Seller                 │
        │ 4. Atualiza usuário            │
        │ 5. Cacheia tokens              │
        └────────────┬───────────────────┘
                     │
        ┌────────────┼────────────┐
        │            │            │
        ▼            ▼            ▼
    UserRepo    ShopeeApi    SellerRepo
        │            │            │
        └────────────┼────────────┘
                     │
                     ▼
            ┌────────────────────┐
            │ DynamoDB           │
            │ SQS (tokens)       │
            │ Cache (tokens)     │
            └────────────────────┘
                     │
                     ▼
        ┌────────────────────────────────┐
        │ 200 OK                         │
        │ {                              │
        │   "statusCode": 200,           │
        │   "message": "Tokens saved"    │
        │ }                              │
        └────────────────────────────────┘
```

### Fluxo 2: Webhook de Pedido
```
┌──────────────────────────────────┐
│ POST /shopee/webhook             │
│ {                                │
│   "msg_id": "...",               │
│   "shop_id": 123,                │
│   "code": 3,                     │
│   "data": {                       │
│     "ordersn": "...",            │
│     "status": "UNPAID"           │
│   }                              │
│ }                                │
└────────────────┬─────────────────┘
                 │
                 ▼
    ┌────────────────────────────────┐
    │ ShopeeWebhookController        │
    │ - Valida estrutura             │
    │ - Valida código do evento      │
    └────────────┬───────────────────┘
                 │
                 ▼
    ┌────────────────────────────────┐
    │ ShopeeService.                 │
    │ ProcessOrderReceivedAsync()    │
    │ 1. Verifica se loja existe     │
    │ 2. Envia para SQS              │
    └────────────┬───────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
        ▼                 ▼
    ShopeeRepo          SQS
        │                │
        └────────┬───────┘
                 │
                 ▼
        ┌────────────────────┐
        │ DynamoDB           │
        │ SQS Queue          │
        └────────────────────┘
                 │
                 ▼
    ┌────────────────────────────────┐
    │ 200 OK                         │
    │ {                              │
    │   "statusCode": 200,           │
    │   "message": "Order accepted"  │
    │ }                              │
    └────────────────────────────────┘
                 │
                 ▼
        ┌────────────────────┐
        │ Lambda Consumer    │
        │ - Processa pedido  │
        │ - Atualiza status  │
        │ - Notifica cliente │
        └────────────────────┘
```

## 🧩 Padrões de Código

### Repository Pattern
```
┌─────────────────────────────┐
│      Service Layer          │
│  (Lógica de negócio)        │
└────────────┬────────────────┘
             │
    ┌────────▼────────┐
    │ Repository      │
    │ (Abstração)     │
    └────────┬────────┘
             │
    ┌────────▼──────────────┐
    │ DynamoDB              │
    │ (Persistência)        │
    └───────────────────────┘

Benefícios:
✅ Fácil testar (mock)
✅ Lógica centralizada
✅ Abstração de BD
```

### Dependency Injection
```csharp
// Program.cs
builder.Services
  .AddScoped<IAmazonDynamoDB>(...)
  .AddScoped<IDynamoDBContext>(...)
  .AddScoped<SellerRepository>()
  .AddScoped<ShopeeApiService>()
  .AddScoped<ShopeeService>();

// ShopeeService.cs
public class ShopeeService
{
    public ShopeeService(
        SellerRepository sellerRepository,      // Injetado
        ShopeeApiService shopeeApiService,      // Injetado
        UserRepository userRepository,          // Injetado
        IAmazonSQS sqsClient,                   // Injetado
        ILogger<ShopeeService> logger)          // Injetado
    {
        _sellerRepository = sellerRepository;
        // ...
    }
}
```

## 📊 Estrutura de Dados (DynamoDB)

### Seller Item
```json
{
  "PK": {"S": "Seller#27f6e005-8719-421a-b2dc-7c09ccdb0b13"},
  "SK": {"S": "META"},
  "entityType": {"S": "seller"},
  "sellerId": {"S": "27f6e005-8719-421a-b2dc-7c09ccdb0b13"},
  "sellerName": {"S": "SANDBOX.738de4c78ad25143eec4"},
  "shop_id": {"N": "226289035"},
  "marketplace": {"S": "shopee"},
  "createdAt": {"N": "1639234899"},
  "updatedAt": {"N": "1639234899"}
}
```

### User Item
```json
{
  "PK": {"S": "User#user@example.com"},
  "SK": {"S": "META"},
  "id": {"S": "user-uuid"},
  "email": {"S": "user@example.com"},
  "role": {"S": "admin"},
  "resource_id": {"S": "seller-uuid"},
  "entityType": {"S": "user"}
}
```

## 🔒 Segurança

### Layers
```
┌────────────────────────────────┐
│ HTTPS (Transporte)             │
├────────────────────────────────┤
│ JWT Bearer Token (Autenticação)│
├────────────────────────────────┤
│ CORS (Origem)                  │
├────────────────────────────────┤
│ Rate Limiting (Rate)           │
├────────────────────────────────┤
│ Validação de Input             │
├────────────────────────────────┤
│ Autorização por Scopes         │
├────────────────────────────────┤
│ Encriptação em Transit         │
├────────────────────────────────┤
│ AWS Secrets Manager (Credenciais)
├────────────────────────────────┤
│ Logging & Auditoria            │
└────────────────────────────────┘
```

## 📈 Performance

### Otimizações
```
1. Caching
   - Tokens em cache (24h)
   - Reduz chamadas à API

2. DynamoDB
   - Índices GSI bem distribuídos
   - TTL em items temporários

3. Async/Await
   - Sem bloqueio de threads
   - Suporta alta concorrência

4. Logging
   - Estruturado com CorrelationId
   - Não bloqueia pipeline
```

## 🚀 Deploy

### Arquitetura AWS
```
┌─────────────────────────────┐
│ CloudFront (CDN)            │
└────────────┬────────────────┘
             │
┌────────────▼────────────────┐
│ API Gateway                 │
└────────────┬────────────────┘
             │
┌────────────▼────────────────┐
│ AWS Lambda (Dropship API)   │
│ .NET 8.0 Runtime            │
└────────────┬────────────────┘
             │
    ┌────────┼────────┬──────────┐
    │        │        │          │
    ▼        ▼        ▼          ▼
  DynamoDB  SQS    CloudWatch  Cognito
  (dados)   (queue) (logs)     (auth)
```

---

**Estrutura pronta para crescer! 🚀**
