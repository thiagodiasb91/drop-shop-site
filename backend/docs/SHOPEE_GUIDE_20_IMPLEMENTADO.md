# ✅ Shopee Developer Guide 20 - Implementado

## 🎯 O Que Foi Atualizado

Baseado na documentação oficial do Shopee (Developer Guide 20), o método `GetAuthUrl()` foi atualizado para usar o endpoint correto e os parâmetros conforme especificação.

## 📋 Mudanças Principais

### 1. Endpoint Correto
```csharp
// Antes: /signin/oauth/identifier
// Agora:
private const string AuthPartnerPath = "/api/v2/auth/authorize";
```

### 2. Parâmetros Atualizados

**Antes (Legacy):**
- client_id
- lang
- login_types
- max_auth_age
- redirect_uri
- region
- required_passwd
- respond_code
- scope
- sign
- timestamp

**Agora (Standard OAuth2):**
- `response_type` = "code"
- `client_id` = Partner ID
- `redirect_uri` = Sua API AWS
- `state` = Base64(JSON com nonce, id, timestamp)
- `sign` = HMAC SHA256

### 3. Geração de State Token

```csharp
var nonce = Guid.NewGuid().ToString("N").Substring(0, 16);
var timestamp = GetCurrentTimestamp();
var stateJson = $"{{\"nonce\":\"{nonce}\",\"id\":{_partnerId},\"timestamp\":{timestamp}}}";
var stateBytes = Encoding.UTF8.GetBytes(stateJson);
var state = Convert.ToBase64String(stateBytes);
```

**Exemplo:**
```json
{
  "nonce": "1234567890abcdef",
  "id": 1203628,
  "timestamp": 1706901234
}
```

Base64 Encoded:
```
eyJub25jZSI6IjEyMzQ1Njc4OTBhYmNkZWYiLCJpZCI6MTIwMzYyOCwidGltZXN0YW1wIjoxNzA2OTAxMjM0fQ==
```

### 4. Assinatura HMAC SHA256

**Base String:**
```
{partnerId}/api/v2/auth/authorize{timestamp}
```

**Exemplo:**
```
1203628/api/v2/auth/authorize1706901234
```

## 🔄 Fluxo Completo Atualizado

```
1. GetAuthUrl(email)
   └─ Gera state token base64
   └─ Calcula HMAC SHA256
   └─ Retorna URL com parâmetros corretos

2. URL de Autorização
   Host: account.sandbox.test-stable.shopee.com
   Path: /api/v2/auth/authorize
   Params: response_type, client_id, redirect_uri, state, sign

3. Cliente Autoriza
   └─ Acessa account.sandbox.test-stable.shopee.com
   └─ Faz login
   └─ Autoriza app

4. Shopee Redireciona
   └─ Para: https://inv6sa4cb0.../dev/shopee/auth?email=...&code=...&state=...

5. Sistema Valida
   └─ Verifica state token
   └─ Extrai code
   └─ Troca por token

6. ✅ Seller Autorizado
```

## 📝 Exemplo de URL Gerada

```
https://account.sandbox.test-stable.shopee.com/api/v2/auth/authorize?
response_type=code&
client_id=1203628&
redirect_uri=https%3A%2F%2Finv6sa4cb0.execute-api.us-east-1.amazonaws.com%2Fdev%2Fshopee%2Fauth%3Femail%3Dseller%40example.com&
state=eyJub25jZSI6IjEyMzQ1Njc4OTBhYmNkZWYiLCJpZCI6MTIwMzYyOCwidGltZXN0YW1wIjoxNzA2OTAxMjM0fQ%3D%3D&
sign=abc123def456...
```

## 🔐 CSRF Protection (State Token)

### O que é State Token?
- Token aleatório gerado pelo servidor
- Armazenado na sessão do cliente
- Retornado pelo Shopee no callback
- Validado pelo servidor

### Por que é Importante?
✅ Previne ataques CSRF
✅ Valida que o callback veio do Shopee
✅ Garante segurança do fluxo OAuth2

### Como Validar
```csharp
// No callback (/shopee/webhook/auth)
var receivedState = HttpContext.Request.Query["state"];
var storedState = HttpContext.Session.GetString("auth_state");

if (receivedState != storedState)
{
    throw new InvalidOperationException("State token validation failed");
}
```

## 🧪 Testando

### 1. Gerar URL
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url?email=seller@example.com"
```

### 2. Verificar URL
✅ Host: `account.sandbox.test-stable.shopee.com`
✅ Path: `/api/v2/auth/authorize`
✅ Parâmetros: `response_type`, `client_id`, `redirect_uri`, `state`, `sign`

### 3. Exemplo de Resposta
```json
{
  "statusCode": 200,
  "authUrl": "https://account.sandbox.test-stable.shopee.com/api/v2/auth/authorize?response_type=code&client_id=1203628&redirect_uri=...&state=eyJub25j...&sign=abc123...",
  "redirectUrl": "https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=seller@example.com",
  "instructions": {...}
}
```

### 4. Cliente Autoriza
- Clica no link `authUrl`
- Faz login em Shopee Sandbox
- Autoriza app
- Shopee redireciona com code + state

### 5. Sistema Recebe
```
https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?
email=seller@example.com&
code=AUTHORIZATION_CODE_HERE&
state=eyJub25j...
```

## 📊 Comparação: Antes vs Depois

| Aspecto | Antes | Depois |
|---------|-------|--------|
| Endpoint | /signin/oauth/identifier | /api/v2/auth/authorize |
| Tipo | Legacy | Standard OAuth2 |
| Parâmetros | 11 parâmetros | 5 parâmetros essenciais |
| State Token | Não | Sim (CSRF protection) |
| Documentação | Menos claro | Developer Guide 20 |
| Segurança | Básica | Padrão OAuth2 |
| Complexidade | Maior | Menor |

## ✅ Checklist

- [x] Path atualizado para /api/v2/auth/authorize
- [x] Parâmetros simplificados (OAuth2 padrão)
- [x] State token implementado
- [x] HMAC SHA256 calculado corretamente
- [x] Base string corrigida
- [x] Compilação OK
- [x] Logging atualizado
- [x] Documentação criada

## 🔗 Referência Oficial

**Documentação**: https://open.shopee.com/developer-guide/20
**Tipo**: Seller In-House
**Ambiente**: Sandbox
**Endpoint**: /api/v2/auth/authorize

## 🚀 Status

```
┌────────────────────────────────────┐
│ ✅ SHOPEE DEVELOPER GUIDE 20       │
│    IMPLEMENTADO COM SUCESSO        │
│                                    │
│ Endpoint:  ✅                      │
│ Parâmetros: ✅                     │
│ State Token: ✅                    │
│ HMAC:      ✅                      │
│ Segurança: ✅                      │
│ Pronto:    ✅                      │
└────────────────────────────────────┘
```

---

**Fonte**: https://open.shopee.com/developer-guide/20
**Data**: February 4, 2026
**Versão**: 1.0
**Status**: ✅ Implementado
