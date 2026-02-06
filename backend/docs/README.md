# 🚀 Dropship API

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com)

Plataforma backend robusta para gerenciamento de e-commerce com integração Shopee, sistema de pagamentos e gestão de inventário em DynamoDB.

## 📋 Sumário

- [Características](#características)
- [Arquitetura](#arquitetura)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Pré-requisitos](#pré-requisitos)
- [Instalação](#instalação)
- [Configuração](#configuração)
- [API Endpoints](#api-endpoints)
- [Autenticação](#autenticação)
- [Logs e Monitoramento](#logs-e-monitoramento)
- [Deployment](#deployment)
- [Contribuindo](#contribuindo)

## ✨ Características

### 🛍️ Integração Shopee
- ✅ Autenticação OAuth2 com Shopee
- ✅ Gerenciamento de tokens com refresh automático
- ✅ Webhooks para eventos de pedidos
- ✅ Suporte a shop-level e account-level authentication

### 💳 Gestão de Pagamentos
- ✅ Processamento de pagamentos
- ✅ Histórico de transações
- ✅ Suporte a múltiplas formas de pagamento

### 📦 Controle de Inventário
- ✅ Kardex detalhado de movimentações
- ✅ Gestão de estoque em tempo real
- ✅ Rastreamento de fornecedores (Suppliers)
- ✅ Gestão de vendedores (Sellers)

### 🔐 Segurança
- ✅ Autenticação JWT
- ✅ Integração AWS Cognito
- ✅ CORS configurável
- ✅ Credenciais via AWS Secrets Manager

### 📊 Observabilidade
- ✅ Logging estruturado com CorrelationId
- ✅ Rastreamento de requisições completo
- ✅ Logs de request/response body
- ✅ Formatação customizada para CloudWatch

### 📚 Documentação
- ✅ Swagger/OpenAPI integrado
- ✅ Endpoints totalmente documentados

## 🏗️ Arquitetura

```
┌─────────────────────────────────────────┐
│         Frontend (Next.js/React)        │
└──────────────────┬──────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────┐
│    API Gateway (AWS Lambda/Http Api)    │
└──────────────────┬──────────────────────┘
                   │
        ┌──────────▼──────────┐
        │   Controllers       │
        │  (REST Endpoints)   │
        └──────────┬──────────┘
        ▼          ▼          ▼
┌──────────────┬──────────────┬──────────────┐
│ Middlewares  │  Services    │ Repositories │
│ (Logging,    │ (Business    │ (Data Access)│
│  CORS,       │  Logic)      │              │
│  Auth)       │              │              │
└──────────────┴──────────────┴──────────────┘
        │          │          │
        ▼          ▼          ▼
┌─────────────────────────────────────────┐
│         Data Layer (DynamoDB)           │
│  - Catalogs, Orders, Users, Tokens      │
└─────────────────────────────────────────┘
        │
        ▼
┌──────────────────────────────┐
│  External APIs               │
│  - Shopee API                │
│  - AWS SQS (Event Queue)     │
│  - Cognito (Auth)            │
└──────────────────────────────┘
```

### Padrões Arquiteturais
- **MVC com Separação de Responsabilidades**: Controllers → Services → Repositories
- **Injeção de Dependências**: ASP.NET Core DI Container
- **Repositories Pattern**: Abstração de acesso a dados
- **Domain-Driven Design**: Modelos de domínio bem definidos

## 📁 Estrutura do Projeto

```
Dropship/
│
├── Controllers/                 # Endpoints REST
│   ├── AuthenticateController.cs
│   ├── ShopeeWebhookController.cs
│   ├── PaymentsController.cs
│   ├── StockController.cs
│   ├── KardexController.cs
│   ├── SupplierController.cs
│   └── UserController.cs
│
├── Services/                    # Lógica de negócios
│   ├── ShopeeApiService.cs      # 🔹 Autenticação e APIs Shopee
│   ├── ShopeeService.cs         # 🔹 Processamento eventos Shopee
│   ├── AuthenticationService.cs
│   ├── PaymentService.cs
│   ├── KardexService.cs
│   └── DeprecatedService.cs (deprecated)
│
├── Repository/                  # Acesso a dados (DynamoDB)
│   ├── DynamoDbRepository.cs    # Base abstrata
│   ├── ShopeeRepository.cs      # 🔹 Queries Shopee
│   ├── SellerRepository.cs      # 🔹 CRUD Sellers
│   ├── SupplierRepository.cs
│   ├── UserRepository.cs
│   ├── KardexRepository.cs
│   └── StockRepository.cs
│
├── Domain/                      # Modelos de domínio
│   ├── SellerDomain.cs          # 🔹 Seller (Vendedor)
│   ├── UserDomain.cs
│   ├── SupplierDomain.cs
│   ├── PaymentDomain.cs
│   ├── KardexDomain.cs
│   └── StockDomain.cs
│
├── Requests/                    # DTO para entrada
│   ├── ShopeeWebhookRequest.cs
│   ├── CreateSupplierRequest.cs
│   ├── UpdateSupplierRequest.cs
│   ├── UpdateStockRequest.cs
│   ├── UpdateUserRequest.cs
│   └── CallbackRequest.cs
│
├── Responses/                   # DTO para saída
│   ├── ShopeeWebhookResponse.cs
│   ├── SupplierResponse.cs
│   ├── SupplierListResponse.cs
│   └── PaymentResponse.cs
│
├── Middlewares/                 # Pipeline de requisição
│   ├── CorrelationIdMiddleware.cs     # Geração de ID único
│   ├── RequestBodyLoggingMiddleware.cs # 🔹 Log completo do body
│   ├── ResponseBodyLoggingMiddleware.cs # 🔹 Log resposta
│   └── RouteDebugMiddleware.cs
│
├── Logging/                     # Logging customizado
│   └── CorrelationIdConsoleFormatter.cs # 🔹 Formatter com ID
│
├── Configuration/               # Configurações
│   └── AuthConfig.cs
│
├── Properties/
│   └── launchSettings.json
│
├── appsettings.json             # Configurações base
├── appsettings.Development.json
├── appsettings.dynamodb.json
│
├── Program.cs                   # 🔹 Configuração DI e startup
├── Dropship.csproj
└── README.md (este arquivo)
```

### Explicação das Partes-Chave

#### 🔹 Camada de Controllers
Recebem requisições HTTP e delegam para Services. Responsáveis por validação básica e formato de resposta.

#### 🔹 Camada de Services
Contêm a lógica de negócio:
- **ShopeeApiService**: Autenticação OAuth2, geração de assinatura HMAC SHA256, refresh de tokens
- **ShopeeService**: Processamento de webhooks, criação de Sellers, atualização de usuários
- **AuthenticationService**: Validação JWT, geração de session tokens

#### 🔹 Camada de Repository
Acesso a dados em DynamoDB:
- `DynamoDbRepository`: Classe base com métodos comuns
- Repositories específicas: Seller, Supplier, User, Payment, etc.
- Suporte a Query, Scan, GSI (Global Secondary Index)

#### 🔹 Camada de Domain
Modelos de dados que representam entidades de negócio:
```csharp
SellerDomain {
  PK: "Seller#{SellerId}",      // Partition Key
  SK: "META",                    // Sort Key
  SellerId: "uuid",
  SellerName: "Nome",
  ShopId: 123,
  Marketplace: "shopee",
  CreatedAt: timestamp,
  UpdatedAt: timestamp
}
```

#### 🔹 Middlewares
Processam todas as requisições:
1. **CorrelationIdMiddleware**: Gera UUID único por requisição
2. **RequestBodyLoggingMiddleware**: Registra todo o body recebido
3. **ResponseBodyLoggingMiddleware**: Registra resposta enviada
4. **RouteDebugMiddleware**: Debug de rotas

## 🚀 Pré-requisitos

### Obrigatório
- **.NET 8.0** ou superior
- **AWS Account** (DynamoDB, SQS, Cognito)
- **AWS CLI** configurado com credenciais

### Opcional para Desenvolvimento Local
- **Docker** (para DynamoDB local)
- **Postman/Insomnia** (para testar APIs)
- **AWS DynamoDB Local**

## 📦 Instalação

### 1. Clone o repositório
```bash
git clone https://github.com/seu-usuario/dropship.git
cd Dropship
```

### 2. Restaure as dependências
```bash
cd Dropship
dotnet restore
```

### 3. Configure as variáveis de ambiente
```bash
# Copie o arquivo de exemplo
cp .env.example .env

# Edite com suas credenciais AWS
# AWS_ACCESS_KEY_ID=seu-access-key
# AWS_SECRET_ACCESS_KEY=sua-secret-key
# SHOPEE_PARTNER_ID=seu-partner-id
# SHOPEE_PARTNER_KEY=sua-partner-key
```

### 4. Build do projeto
```bash
dotnet build
```

## ⚙️ Configuração

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Shopee": {
    "Host": "https://openplatform.sandbox.test-stable.shopee.sg",
    "PartnerID": "${SHOPEE_PARTNER_ID}",
    "PartnerKey": "${SHOPEE_PARTNER_KEY}"
  },
  "Jwt": {
    "Secret": "${JWT_SECRET}",
    "ExpirationHours": 24
  }
}
```

### Variáveis de Ambiente Necessárias
```bash
# AWS
AWS_REGION=us-east-1
AWS_ACCESS_KEY_ID=AKIA***
AWS_SECRET_ACCESS_KEY=***

# Shopee
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=***
SHOPEE_REDIRECT_URL=https://open.shopee.com

# JWT
JWT_SECRET=sua-chave-secreta-super-longa

# Logging
LOG_LEVEL=Information
```

## 🔌 API Endpoints

### 🔐 Autenticação
```
POST   /authenticate/callback     - Callback de autenticação
GET    /authenticate/user         - Dados do usuário autenticado
```

### 🛍️ Shopee Webhook & Auth
```
POST   /shopee/webhook            - Webhook de eventos (Orders)
GET    /shopee/webhook/auth       - Autenticação OAuth2
```

**Exemplo - Autenticação Shopee:**
```bash
GET /shopee/webhook/auth?code=AUTH_CODE&shopId=226289035&email=user@example.com

Resposta:
{
  "statusCode": 200,
  "message": "Tokens saved for shop 226289035"
}
```

**Exemplo - Webhook de Pedido:**
```bash
POST /shopee/webhook
Content-Type: application/json

{
  "msg_id": "85bb37f009e143af84852e17d50b572d",
  "shop_id": 226289035,
  "code": 3,
  "timestamp": 1736323998,
  "data": {
    "ordersn": "2501080NKAMXA8",
    "status": "UNPAID",
    "update_time": 1736323997,
    "completed_scenario": "",
    "items": []
  }
}

Resposta:
{
  "statusCode": 200,
  "message": "New order accepted"
}
```

### 👥 Vendedores (Sellers)
```
GET    /sellers/{sellerId}        - Obter seller por ID
GET    /sellers/shop/{shopId}     - Obter seller por Shop ID
POST   /sellers                   - Criar novo seller
PUT    /sellers/{sellerId}        - Atualizar seller
DELETE /sellers/{sellerId}        - Deletar seller
```

### 🏢 Fornecedores (Suppliers)
```
GET    /suppliers                 - Listar todos (usa GSI_RELATIONS_LOOKUP)
GET    /suppliers/{supplierId}    - Obter por ID
POST   /suppliers                 - Criar novo
PUT    /suppliers/{supplierId}    - Atualizar
DELETE /suppliers/{supplierId}    - Deletar
```

### 👤 Usuários
```
GET    /users/{email}             - Obter por email
POST   /users                     - Criar novo usuário
PUT    /users/{email}             - Atualizar usuário
```

### 💳 Pagamentos
```
GET    /payments/{orderId}        - Obter histórico de pagamentos
POST   /payments                  - Registrar novo pagamento
```

### 📦 Estoque
```
GET    /stock/{productId}         - Obter estoque
PUT    /stock/{productId}         - Atualizar estoque
```

### 📊 Kardex (Movimento de Estoque)
```
GET    /kardex/{productId}        - Histórico de movimentações
POST   /kardex                    - Registrar movimentação
```

## 🔐 Autenticação

### JWT Bearer Token
Todos os endpoints (exceto públicos) requerem:
```
Authorization: Bearer <token>
```

### Fluxo de Autenticação
```
1. Usuário clica em "Login com Cognito"
2. Cognito retorna Authorization Code
3. Callback: POST /authenticate/callback?code=...
4. Sistema valida e retorna JWT Session Token
5. Cliente armazena token
6. Usa token em requisições subsequentes
```

### Endpoints Públicos (sem autenticação)
- `POST /shopee/webhook` - Webhooks da Shopee
- `GET /shopee/webhook/auth` - Autenticação OAuth2
- `GET /` - Health check

## 📊 Logs e Monitoramento

### Estrutura de Log com CorrelationId
```
CorrelationId: 1230498a-sd09f81234 - Request Body - Method: POST, Path: /shopee/webhook, ContentType: application/json, Body: {"msg_id":"...", "shop_id":341431138}

CorrelationId: 1230498a-sd09f81234 - Seller created successfully - SellerId: 27f6e005-8719-421a-b2dc-7c09ccdb0b13, ShopId: 226289035

CorrelationId: 1230498a-sd09f81234 - Response Body - StatusCode: 200, ContentType: application/json, Body: {"statusCode":200,"message":"..."}
```

### Níveis de Log
- **Information**: Eventos normais (autenticação, CRUD)
- **Warning**: Situações inesperadas (usuário não encontrado)
- **Error**: Erros que precisam de ação (falha em API externa)

### CloudWatch Integration
Logs são enviados automaticamente para AWS CloudWatch com:
- Timestamp
- CorrelationId
- Nível de severidade
- Mensagem estruturada

## 🚀 Deployment

### AWS Lambda
```bash
# Build para publicação
dotnet publish -c Release

# Empacotar para Lambda
cd bin/Release/net8.0/publish
zip -r lambda-function.zip .

# Upload via AWS Console ou CLI
aws lambda update-function-code \
  --function-name dropship-api \
  --zip-file fileb://lambda-function.zip
```

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Dropship.dll"]
```

```bash
# Build e push
docker build -t dropship-api:latest .
docker push seu-registry/dropship-api:latest
```

## 📚 Dokumentação da API

A documentação Swagger/OpenAPI está disponível em:
```
http://localhost:5000/swagger
https://seu-dominio.com/swagger
```

## 🤝 Contribuindo

### Processo
1. Fork o repositório
2. Crie uma branch: `git checkout -b feature/nova-feature`
3. Commit: `git commit -am 'Adiciona nova feature'`
4. Push: `git push origin feature/nova-feature`
5. Abra um Pull Request

### Padrões de Código
- ✅ Naming em C# (PascalCase para classes, camelCase para variáveis)
- ✅ Comentários em português para lógica complexa
- ✅ Logs estruturados com CorrelationId
- ✅ Tratamento de exceções adequado
- ✅ Testes unitários para novos features

## 📝 Licença

MIT License - Veja o arquivo [LICENSE](LICENSE) para detalhes

## 🆘 Support

Para suporte, abra uma issue no GitHub ou contacte a equipe de desenvolvimento.

---

**Desenvolvido com ❤️ para gerenciamento eficiente de e-commerce**

Last Updated: February 4, 2026
