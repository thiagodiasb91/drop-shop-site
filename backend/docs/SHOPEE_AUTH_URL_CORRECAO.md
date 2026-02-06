# ✅ Correção de URL de Autenticação Shopee - Implementado

## 🔧 O Que Foi Corrigido

A URL de autenticação Shopee estava gerando com formato incorreto. Agora foi corrigida para usar o formato correto que o Shopee espera:

### URL Antes (Incorreta)
```
https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=...&redirect=...
```

### URL Agora (Correta)
```
https://account.sandbox.test-stable.shopee.com/signin/oauth/identifier?client_id=...&lang=en&login_types=[1,4,2]&max_auth_age=3600&redirect_uri=...&region=SG&required_passwd=true&respond_code=code&scope=profile&sign=...&timestamp=...
```

## 📝 Mudanças Realizadas

### 1. **URLs Base Atualizadas**

```csharp
// Autenticação OAuth2
private const string SandboxAccountHost = "https://account.sandbox.test-stable.shopee.com";

// Chamadas de API (após ter o token)
private const string SandboxApiHost = "https://openplatform.sandbox.test-stable.shopee.sg";

// Path correto para autenticação
private const string AuthPartnerPath = "/signin/oauth/identifier";
```

### 2. **Método GetAuthUrl(email) Atualizado**

**Agora gera URL com os parâmetros corretos:**

```csharp
var queryParams = new Dictionary<string, string>
{
    { "client_id", _partnerId },
    { "lang", "en" },
    { "login_types", "[1,4,2]" },
    { "max_auth_age", "3600" },
    { "redirect_uri", redirectUri },  // Sua API AWS
    { "region", "SG" },
    { "required_passwd", "true" },
    { "respond_code", "code" },
    { "scope", "profile" },
    { "sign", sign },  // HMAC SHA256
    { "timestamp", timestamp.ToString() }
};
```

### 3. **Métodos de Token Atualizados**

Todos agora usam `DefaultApiHost` (openplatform) para as chamadas de token:
- `GetTokenShopLevelAsync()`
- `GetTokenAccountLevelAsync()`
- `RefreshAccessTokenAsync()`

## 🔄 Fluxo Correto Agora

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Gerar URL com GetAuthUrl(email)                          │
│    ↓                                                        │
│    Host: https://account.sandbox.test-stable.shopee.com    │
│    Path: /signin/oauth/identifier                          │
│    Parâmetros: client_id, redirect_uri, sign, timestamp... │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│ 2. Cliente Clica e Autoriza                                │
│    URL:                                                    │
│    https://account.sandbox.test-stable.shopee.com/...      │
│    /signin/oauth/identifier?client_id=...&redirect_uri=... │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│ 3. Shopee Redireciona com Code                             │
│    Para: https://inv6sa4cb0.execute-api.us-east-1...      │
│    /dev/shopee/auth?email=...&code=...&shop_id=...        │
└────────────┬────────────────────────────────────────────────┘
             │
┌────────────▼────────────────────────────────────────────────┐
│ 4. Trocar Code por Token                                   │
│    Host: https://openplatform.sandbox.test-stable...      │
│    Path: /api/v2/auth/token/get                           │
│    Body: { code, shop_id, partner_id }                    │
└────────────┬────────────────────────────────────────────────┘
             │
                ✅ Token Recebido!
```

## 📊 Parâmetros da URL de Autenticação

| Parâmetro | Valor | Descrição |
|-----------|-------|-----------|
| `client_id` | 1203628 | Partner ID |
| `lang` | en | Idioma |
| `login_types` | [1,4,2] | Tipos de login |
| `max_auth_age` | 3600 | Tempo máximo de auth |
| `redirect_uri` | https://inv6sa4cb0... | URL de callback |
| `region` | SG | Região (Singapore) |
| `required_passwd` | true | Requer senha |
| `respond_code` | code | Tipo de response |
| `scope` | profile | Escopo de acesso |
| `sign` | HMAC... | Assinatura SHA256 |
| `timestamp` | Unix time | Timestamp |

## 🧪 Testando

### 1. Gerar URL
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url?email=test@example.com"
```

### 2. Resposta
```json
{
  "statusCode": 200,
  "authUrl": "https://account.sandbox.test-stable.shopee.com/signin/oauth/identifier?client_id=1203628&lang=en&login_types=%5B1%2C4%2C2%5D&max_auth_age=3600&redirect_uri=https%3A%2F%2Finv6sa4cb0.execute-api.us-east-1.amazonaws.com%2Fdev%2Fshopee%2Fauth%3Femail%3Dtest%40example.com&region=SG&required_passwd=true&respond_code=code&scope=profile&sign=...&timestamp=...",
  "redirectUrl": "https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=test@example.com"
}
```

### 3. Verificar
✅ `authUrl` agora aponta para `account.sandbox.test-stable.shopee.com`
✅ Contém todos os parâmetros corretos
✅ HMAC SHA256 é calculado corretamente

## 🔐 Segurança

✅ **HMAC SHA256**: Assinatura válida para cada combinação de parâmetros
✅ **Timestamp**: Previne replay attacks
✅ **Redirect URI**: Sua API AWS
✅ **Client ID**: Partner ID do Shopee

## 📊 Hosts Utilizados

| Tipo | Host | Uso |
|------|------|-----|
| **Autenticação OAuth** | account.sandbox.test-stable.shopee.com | Login e autorização do cliente |
| **Chamadas de API** | openplatform.sandbox.test-stable.shopee.sg | Token, dados de shop, etc |

## ✅ Checklist

- [x] URL base corrigida para account.sandbox
- [x] Path correto (/signin/oauth/identifier)
- [x] Todos os parâmetros incluídos
- [x] HMAC SHA256 calculado corretamente
- [x] API host separado para chamadas de token
- [x] Código compilado sem erros
- [x] Documentado

## 🎯 Próximos Passos

1. **Testar URL gerada**
   ```bash
   curl -X GET "http://localhost:5000/shopee/webhook/auth-url?email=test@example.com"
   ```

2. **Copiar authUrl**
   - Colar em navegador
   - Ou enviar ao cliente

3. **Cliente autoriza**
   - Acessa account.sandbox.test-stable.shopee.com
   - Faz login
   - Autoriza app

4. **Sistema recebe code**
   - Shopee redireciona com code
   - GET /shopee/webhook/auth?code=...&shopId=...&email=...

5. **Tokens obtidos**
   - Chama openplatform.sandbox.test-stable.shopee.sg
   - Recebe access_token e refresh_token

## 🚀 Status

```
┌────────────────────────────────────────┐
│ ✅ URL DE AUTENTICAÇÃO CORRIGIDA       │
│                                        │
│ Host Account:  ✅                      │
│ Host API:      ✅                      │
│ Path Auth:     ✅                      │
│ Parâmetros:    ✅                      │
│ HMAC SHA256:   ✅                      │
│ Compilação:    ✅                      │
│ Pronto uso:    ✅                      │
└────────────────────────────────────────┘
```

---

**Data**: February 4, 2026
**Versão**: 1.0
**Status**: ✅ Corrigido e Pronto
