# 📖 Shopee Sandbox Authorization - Developer Guide (Seller In-House)

## 📋 Referência Oficial
Documentação: https://open.shopee.com/developer-guide/20

## 🎯 Tipo: Seller In-House

Para autorização de **Seller In-House** em Sandbox, o fluxo é:

### URL Base
```
https://account.sandbox.test-stable.shopee.com
```

### Endpoint de Autorização
```
GET /api/v2/auth/authorize
```

### Parâmetros Obrigatórios

| Parâmetro | Tipo | Descrição | Exemplo |
|-----------|------|-----------|---------|
| `response_type` | string | Tipo de resposta | code |
| `client_id` | string | Partner ID | 1203628 |
| `redirect_uri` | string | URI de callback | https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=... |
| `state` | string | Estado para CSRF | Gerado aleatoriamente |

### Parâmetros Opcionais
| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `sign` | string | HMAC SHA256 da requisição |

## 🔐 Assinatura HMAC SHA256

**Base String:**
```
{partner_id}/api/v2/auth/authorize{timestamp}
```

**Exemplo:**
```
1203628/api/v2/auth/authorize1706901234
```

**Cálculo:**
```csharp
var baseString = $"{partnerId}/api/v2/auth/authorize{timestamp}";
var sign = HMAC-SHA256(partnerKey, baseString);
```

## 📝 URL Completa de Autorização

```
https://account.sandbox.test-stable.shopee.com/api/v2/auth/authorize?
response_type=code
&client_id=1203628
&redirect_uri=https%3A%2F%2Finv6sa4cb0.execute-api.us-east-1.amazonaws.com%2Fdev%2Fshopee%2Fauth%3Femail%3Duser%40example.com
&state=eyJub25jZSI6Ijk0NTM3ZjRlMTA0NDBkMDciLCJpZCI6MTIwMzYyOCwiYXV0aF9zaG9wIjoxfQ==
&sign=abc123...
```

## 🔄 Fluxo Completo para Seller In-House

```
┌─────────────────────────────────────────────────────┐
│ 1. Seu Backend Gera URL                             │
│    GET /shopee/webhook/auth-url?email=...           │
└────────────┬────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────┐
│ 2. Sistema Monta URL de Autorização                 │
│    Base: account.sandbox.test-stable.shopee.com     │
│    Path: /api/v2/auth/authorize                     │
│    Params: response_type=code&client_id=...         │
│             redirect_uri=...&state=...&sign=...     │
└────────────┬────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────┐
│ 3. Seu Frontend Envia URL ao Cliente                │
│    Cliente clica no link de autorização             │
└────────────┬────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────┐
│ 4. Cliente Autentica no Shopee Sandbox              │
│    Acessa: account.sandbox.test-stable...           │
│    Faz login com credenciais sandbox                │
│    Autoriza app                                     │
└────────────┬────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────┐
│ 5. Shopee Redireciona com Authorization Code        │
│    Para: https://inv6sa4cb0.execute-api.us-east-1...│
│           /dev/shopee/auth?email=...&code=...       │
│    Inclui: state parameter para validação           │
└────────────┬────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────┐
│ 6. Seu Backend Recebe Code                          │
│    Extrai: code, state, email da URL                │
│    Valida: state para prevenir CSRF                 │
└────────────┬────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────┐
│ 7. Sistema Troca Code por Token                     │
│    POST /api/v2/auth/token/get                      │
│    Body: { code, shop_id, partner_id }              │
│    Resposta: { access_token, refresh_token, ...}    │
└────────────┬────────────────────────────────────────┘
             │
        ✅ SELLER AUTORIZADO!
```

## 📊 Parâmetros Detalhados

### response_type
- **Valor**: `code`
- **Descrição**: Tipo de OAuth2 flow
- **Obrigatório**: Sim

### client_id
- **Valor**: seu Partner ID (1203628)
- **Descrição**: Identifica sua aplicação
- **Obrigatório**: Sim

### redirect_uri
- **Valor**: `https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email={email}`
- **Descrição**: URL onde Shopee redireciona após autorização
- **Obrigatório**: Sim
- **Nota**: URL-encode necessário

### state
- **Tipo**: string (base64 recomendado)
- **Descrição**: Token para validar retorno (CSRF protection)
- **Obrigatório**: Recomendado
- **Valor exemplo**: `eyJub25jZSI6Ijk0NTM3ZjRlMTA0NDBkMDciLCJpZCI6MTIwMzYyOH0=`

### sign
- **Tipo**: string (HMAC SHA256)
- **Descrição**: Assinatura da requisição
- **Base String**: `{partnerId}/api/v2/auth/authorize{timestamp}`
- **Obrigatório**: Recomendado para segurança

## 🧮 Cálculo da Assinatura

```csharp
// Base string
var baseString = $"{_partnerId}/api/v2/auth/authorize{timestamp}";

// Converter para bytes
var baseBytes = Encoding.UTF8.GetBytes(baseString);
var keyBytes = Encoding.UTF8.GetBytes(_partnerKey);

// HMAC SHA256
using (var hmac = new HMACSHA256(keyBytes))
{
    var hashBytes = hmac.ComputeHash(baseBytes);
    var sign = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    return sign;
}
```

## 🔗 Resposta de Callback

Shopee redireciona com:
```
https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?
email=user@example.com
&code=AUTHORIZATION_CODE_FROM_SHOPEE
&state=eyJub25jZSI6Ijk0NTM3ZjRlMTA0NDBkMDciLCJpZCI6MTIwMzYyOH0=
```

### Validações Necessárias
1. ✅ Validar `state` para prevenir CSRF
2. ✅ Extrair `code` para trocar por token
3. ✅ Armazenar `email` do callback

## 💾 Troca Code por Token

```
POST https://openplatform.sandbox.test-stable.shopee.sg/api/v2/auth/token/get
?partner_id=1203628&timestamp={timestamp}&sign={hmac}

Body: {
  "code": "AUTHORIZATION_CODE",
  "shop_id": "SHOP_ID_FROM_CALLBACK",
  "partner_id": "1203628"
}

Response: {
  "access_token": "...",
  "refresh_token": "...",
  "expires_in": 3600,
  "refresh_token_expires_in": 2592000
}
```

## 🧪 Testando em Sandbox

### 1. Gerar URL
```bash
GET http://localhost:5000/shopee/webhook/auth-url?email=seller@example.com
```

### 2. Copiar authUrl
```
https://account.sandbox.test-stable.shopee.com/api/v2/auth/authorize?response_type=code&client_id=1203628&redirect_uri=...&state=...&sign=...
```

### 3. Colar em Navegador
- Cliente acessa a URL
- Faz login em sandbox Shopee
- Autoriza app

### 4. Shopee Redireciona
- System captura `code`
- Valida `state`
- Extrai `email`

### 5. Trocar Code
```bash
GET /shopee/webhook/auth?code=SANDBOX_CODE&shopId=123&email=seller@example.com
```

### 6. Sucesso!
- Tokens armazenados
- Seller criado
- Pronto para usar APIs

## ⚠️ Diferenças: /signin/oauth/identifier vs /api/v2/auth/authorize

| Aspecto | /signin/oauth/identifier | /api/v2/auth/authorize |
|---------|--------------------------|------------------------|
| Type | Legacy/Older | Recomendado |
| Parâmetros | Muitos (client_id, lang, login_types, etc) | Simples (response_type, client_id, redirect_uri, state) |
| Segurança | Básica | HMAC sign recomendado |
| Fluxo | Mais complexo | Padrão OAuth2 |
| Documentação | Menos claro | Bem documentado |

**Recomendação**: Usar `/api/v2/auth/authorize` para novo desenvolvimento!

## 🔑 Credenciais Sandbox

### Partner ID
```
1203628
```

### Partner Key
Obtido no Shopee Developer Center (sandpox)

### Shop ID (Sandbox)
Criado ao autorizar em sandbox

### Email (Seu)
Use um email sandbox fornecido pela Shopee

## 📱 Exemplo Completo de URL

```
https://account.sandbox.test-stable.shopee.com/api/v2/auth/authorize?
response_type=code&
client_id=1203628&
redirect_uri=https%3A%2F%2Finv6sa4cb0.execute-api.us-east-1.amazonaws.com%2Fdev%2Fshopee%2Fauth%3Femail%3Dseller%40example.com&
state=eyJub25jZSI6IjEyMzQ1NiIsImlkIjoxMjAzNjI4fQ%3D%3D&
sign=abc123def456...
```

## ✅ Checklist de Implementação

- [ ] Atualizar `GetAuthUrl()` para usar `/api/v2/auth/authorize`
- [ ] Adicionar parâmetro `response_type=code`
- [ ] Adicionar parâmetro `state` (gerado aleatoriamente)
- [ ] Recalcular `sign` com novo path
- [ ] Validar `state` no callback
- [ ] Testar com email sandbox
- [ ] Verificar tokens recebidos

---

**Fonte**: https://open.shopee.com/developer-guide/20
**Data**: February 4, 2026
**Tipo**: Seller In-House
**Ambiente**: Sandbox
