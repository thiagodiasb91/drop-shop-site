# ShopeeInterfaceController - Documentação

## 📋 Visão Geral

O `ShopeeInterfaceController` expõe todos os métodos do serviço `ShopeeApiService` através de endpoints REST. O objetivo é permitir testes diretos das chamadas à API do Shopee sem necessidade de debugging.

**Rota Base:** `/shopee-interface`

---

## 🔌 Endpoints

### 1. Gerar URL de Autenticação

**GET** `/shopee-interface/auth-url`

Gera uma URL para redirecionar o usuário ao Shopee para autenticação (OAuth2).

#### Parâmetros:
| Nome | Tipo | Obrigatório | Descrição |
|------|------|-------------|-----------|
| `email` | string | ✅ | Email do seller |
| `requestUri` | string | ✅ | URI base da aplicação (ex: http://localhost:3000) |

#### Response (200 OK):
```json
{
  "authUrl": "https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=1203628&redirect=http%3A%2F%2Flocalhost%3A3000%2Fsellers%2Fseller%40example.com%2Fstore%2Fcode&timestamp=1707391234&sign=abc123..."
}
```

#### Exemplo cURL:
```bash
curl "http://localhost:5000/shopee-interface/auth-url?email=seller@example.com&requestUri=http://localhost:3000"
```

---

### 2. Obter Token de Loja

**POST** `/shopee-interface/get-token`

Obtém o token de acesso e refresh usando o authorization code retornado pelo Shopee (etapa 2 do OAuth2).

#### Parâmetros:
| Nome | Tipo | Obrigatório | Descrição |
|------|------|-------------|-----------|
| `code` | string | ✅ | Authorization code do Shopee |
| `shopId` | long | ✅ | ID da loja no Shopee |

#### Response (200 OK):
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "expiresIn": 28800
}
```

#### Exemplo cURL:
```bash
curl -X POST "http://localhost:5000/shopee-interface/get-token?code=AUTH_CODE&shopId=123456"
```

---

### 3. Obter Informações da Loja

**GET** `/shopee-interface/shop-info`

Obtém informações detalhadas da loja Shopee usando um token de acesso válido.

#### Parâmetros:
| Nome | Tipo | Obrigatório | Descrição |
|------|------|-------------|-----------|
| `accessToken` | string | ✅ | Token de acesso da loja |
| `shopId` | long | ✅ | ID da loja no Shopee |

#### Response (200 OK):
```json
{
  "shop_id": 123456,
  "shop_name": "Meu Shop",
  "country": "BR",
  "status": 1,
  "is_official": false,
  "rating_star": 4.8,
  "response_rate": 0.95,
  "response_time": 3600,
  "is_one_awb": true,
  "is_mart_shop": false,
  "is_outlet_shop": false,
  // ... outros campos
}
```

#### Exemplo cURL:
```bash
curl "http://localhost:5000/shopee-interface/shop-info?accessToken=TOKEN_AQUI&shopId=123456"
```

---

### 4. Health Check

**GET** `/shopee-interface/health-check`

Simples verificação de saúde do serviço.

#### Response (200 OK):
```json
{
  "status": "healthy",
  "timestamp": "2026-02-07T12:00:00Z",
  "message": "ShopeeInterface controller is running"
}
```

#### Exemplo cURL:
```bash
curl "http://localhost:5000/shopee-interface/health-check"
```

---

### 5. Listar Endpoints

**GET** `/shopee-interface/endpoints`

Retorna informações sobre todos os endpoints disponíveis.

#### Response (200 OK):
```json
{
  "version": "1.0",
  "baseUrl": "http://localhost:5000/shopee-interface",
  "service": "ShopeeApiService",
  "endpoints": [
    {
      "method": "GET",
      "path": "/auth-url",
      "description": "Gera URL de autenticação com Shopee",
      "parameters": ["email (string)", "requestUri (string)"],
      "example": "/auth-url?email=seller@example.com&requestUri=http://localhost:3000"
    },
    // ... outros endpoints
  ]
}
```

---

## 🧪 Fluxo de Teste Completo

### Passo 1: Gerar URL de Autenticação

```bash
curl "http://localhost:5000/shopee-interface/auth-url?email=seller@example.com&requestUri=http://localhost:3000"
```

Copie a URL retornada (`authUrl`) e abra no navegador. Você será redirecionado ao Shopee para autorizar. Após autorizar, será redirecionado para:
```
http://localhost:3000/sellers/seller@example.com/store/code?code=AUTH_CODE&shop_id=123456
```

### Passo 2: Obter Token

Com o `AUTH_CODE` obtido, faça:

```bash
curl -X POST "http://localhost:5000/shopee-interface/get-token?code=AUTH_CODE&shopId=123456"
```

Resposta:
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "eyJ...",
  "expiresIn": 28800
}
```

### Passo 3: Obter Informações da Loja

Com o `accessToken`, faça:

```bash
curl "http://localhost:5000/shopee-interface/shop-info?accessToken=eyJ...&shopId=123456"
```

---

## 📊 Métodos Expostos do ShopeeApiService

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| `GetAuthUrl()` | GET `/auth-url` | Gera URL de autenticação OAuth2 |
| `GetTokenShopLevelAsync()` | POST `/get-token` | Obtém token de loja |
| `GetShopInfoAsync()` | GET `/shop-info` | Obtém informações da loja |

---

## ⚠️ Códigos de Status HTTP

| Status | Descrição |
|--------|-----------|
| 200 OK | Requisição bem-sucedida |
| 400 Bad Request | Parâmetros inválidos ou faltando |
| 500 Internal Server Error | Erro na API do Shopee ou no servidor |

---

## 🔐 Configuração Necessária

O controller usa as seguintes variáveis de ambiente:

```bash
SHOPEE_PARTNER_ID=1203628
```

A chave do parceiro é definida hardcoded no `ShopeeApiService` (considere mover para variáveis de ambiente ou AWS Secrets Manager).

---

## 🎯 Use Cases

### Caso 1: Testar fluxo de OAuth2
Use `/auth-url` e `/get-token` para validar o fluxo de autenticação sem precisar ter o frontend conectado.

### Caso 2: Validar credenciais
Use `/shop-info` com um token conhecido para confirmar que as credenciais estão corretas.

### Caso 3: Debug de erros
Todos os endpoints retornam mensagens de erro detalhadas que ajudam a identificar problemas.

---

## 📝 Logging

Todos os endpoints incluem logging detalhado com o prefixo `[SHOPEE-TEST]`:

```
[INF] [SHOPEE-TEST] GetAuthUrl - Email: seller@example.com, RequestUri: http://localhost:3000
[INF] [SHOPEE-TEST] Auth URL generated successfully
```

---

## 🚀 Como Usar no Swagger

O controller está totalmente integrado com o Swagger. Acesse:

```
http://localhost:5000/swagger
```

E procure por "shopee-interface" para ver todos os endpoints com documentação automática.
