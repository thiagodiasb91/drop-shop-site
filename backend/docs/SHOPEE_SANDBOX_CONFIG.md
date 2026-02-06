# 🔧 Configuração Shopee Sandbox - Implementado

## ✅ O Que Foi Configurado

A autenticação Shopee foi totalmente configurada para usar o **ambiente Sandbox** com URLs específicas:

### URLs Sandbox Implementadas

```
🔹 API OpenPlatform (Autenticação e APIs):
   https://openplatform.sandbox.test-stable.shopee.sg

🔹 Account Service (Serviço de Conta):
   https://account.sandbox.test-stable.shopee.com
```

## 🔐 Endpoints Sandbox

### 1. Autenticação (OAuth2)
```
GET https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner
   ?partner_id=1203628
   &redirect={redirect_url}
   &timestamp={timestamp}
   &sign={hmac_sha256}
```

### 2. Trocar Code por Token
```
POST https://openplatform.sandbox.test-stable.shopee.sg/api/v2/auth/token/get
   Body: { code, shop_id, partner_id }
```

### 3. Renovar Token
```
POST https://openplatform.sandbox.test-stable.shopee.sg/api/v2/auth/access_token/get
   Body: { refresh_token, shop_id, partner_id }
```

### 4. Serviço de Conta
```
Acesso em: https://account.sandbox.test-stable.shopee.com
```

## 📝 Configuração no Código

### ShopeeApiService.cs

```csharp
// Constantes Sandbox
private const string SandboxHost = "https://openplatform.sandbox.test-stable.shopee.sg";
private const string SandboxAccountHost = "https://account.sandbox.test-stable.shopee.com";
private const string DefaultHost = SandboxHost;

// Constructor
public ShopeeApiService(HttpClient httpClient, ILogger<ShopeeApiService> logger)
{
    _host = Environment.GetEnvironmentVariable("SHOPEE_HOST") ?? DefaultHost;
    // Usa sandbox por padrão, pode ser sobrescrito com variável de ambiente
}
```

## 🎯 Variáveis de Ambiente

### .env.example Atualizado
```bash
# Shopee API Configuration (Sandbox Environment)
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=seu-partner-key-aqui
SHOPEE_HOST=https://openplatform.sandbox.test-stable.shopee.sg
SHOPEE_ACCOUNT_HOST=https://account.sandbox.test-stable.shopee.com
```

### .env Local (para desenvolvimento)
```bash
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=sua-chave-sandbox
SHOPEE_HOST=https://openplatform.sandbox.test-stable.shopee.sg
SHOPEE_ACCOUNT_HOST=https://account.sandbox.test-stable.shopee.com
```

## 🔄 Fluxo de Autenticação Sandbox

```
┌─────────────────────────────────────────────────────────────┐
│ 1. Gerar URL de Autorização                                 │
│    GET /shopee/webhook/auth-url?email=user@example.com      │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│ 2. Sistema Gera URL para Sandbox                            │
│    Host: https://openplatform.sandbox.test-stable...        │
│    Path: /api/v2/shop/auth_partner                          │
│    HMAC: Assinado com partner_key                           │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│ 3. Cliente Clica e Vai para Sandbox Shopee                  │
│    https://openplatform.sandbox.test-stable.shopee.sg/...   │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│ 4. Faz Login em Sandbox                                     │
│    Email/Senha de sandbox                                   │
│    Autoriza app                                             │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│ 5. Shopee Sandbox Redireciona com Code                      │
│    Para: https://inv6sa4cb0.execute-api.us-east-1...        │
│    ?email=user@example.com&code=SANDBOX_CODE&shop_id=123    │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│ 6. Sistema Troca Code por Token (Sandbox)                   │
│    POST https://openplatform.sandbox.test-stable.shopee.sg/ │
│        /api/v2/auth/token/get                               │
│    Body: { code, shop_id, partner_id }                      │
└────────────────┬────────────────────────────────────────────┘
                 │
┌────────────────▼────────────────────────────────────────────┐
│ 7. Recebe Tokens (Sandbox)                                  │
│    access_token (sandbox)                                   │
│    refresh_token (sandbox)                                  │
│    ✅ Pronto para usar                                      │
└─────────────────────────────────────────────────────────────┘
```

## 🧪 Testando em Sandbox

### 1. Gerar URL de Autorização
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url?email=test@sandbox.com"
```

**Resposta:**
```json
{
  "statusCode": 200,
  "authUrl": "https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=1203628&redirect=https%3A%2F%2Finv6sa4cb0.execute-api.us-east-1.amazonaws.com%2Fdev%2Fshopee%2Fauth%3Femail%3Dtest%40sandbox.com&timestamp=1736323998&sign=...",
  "redirectUrl": "https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=test@sandbox.com"
}
```

### 2. Verificar URL Sandbox
✅ A URL contém: `https://openplatform.sandbox.test-stable.shopee.sg/`

### 3. Cliente Acessa Sandbox
- Clica no link `authUrl`
- Faz login com credenciais **sandbox** da Shopee
- Não usa credenciais de produção

### 4. Receber Code e Autenticar
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth?code=SANDBOX_CODE&shopId=123&email=test@sandbox.com"
```

## 🔑 Dados de Teste Sandbox

### Conta Sandbox Shopee
Para testar, você precisa de:
- Partner ID: `1203628` (fornecido pela Shopee)
- Partner Key: Seu partner key sandbox
- Shop ID: ID da loja de teste (será gerado ao autorizar)

### Login Sandbox
```
URL: https://account.sandbox.test-stable.shopee.com
Username: Suas credenciais de teste
Password: Suas credenciais de teste
```

## 📊 Comparação: Sandbox vs Produção

| Aspecto | Sandbox | Produção |
|---------|---------|----------|
| Host | openplatform.**sandbox**.test-stable.shopee.sg | openplatform.shopee.com |
| Account | account.**sandbox**.test-stable.shopee.com | account.shopee.com |
| Dados | Fictícios/Testes | Reais |
| Transações | Sem custo | Com custo |
| Impacto | Nenhum | Real |
| Uso | Desenvolvimento | Live |

## 🔄 Mudança para Produção (Futuro)

Quando precisar mudar para produção:

### 1. Atualizar `.env`
```bash
# De Sandbox
SHOPEE_HOST=https://openplatform.sandbox.test-stable.shopee.sg

# Para Produção
SHOPEE_HOST=https://openplatform.shopee.com
```

### 2. Atualizar Partner Key
```bash
# Obter partner key de produção da Shopee
SHOPEE_PARTNER_KEY=sua-chave-produção
```

### 3. Nenhuma mudança no código!
✅ O sistema usará automaticamente a URL de produção

## ✅ Checklist

- [x] URL Sandbox para OpenPlatform configurada
- [x] URL Sandbox para Account adicionada
- [x] DefaultHost aponta para Sandbox
- [x] Variáveis de ambiente atualizadas
- [x] .env.example com URLs corretas
- [x] Constantes nomeadas e organizadas
- [x] Ambiente pode ser sobrescrito via variável
- [x] Fácil mudar para produção (só troca variável)

## 📝 Exemplos de Variáveis

### Desenvolvimento (Sandbox)
```bash
# .env.local
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=sua-partner-key-sandbox
SHOPEE_HOST=https://openplatform.sandbox.test-stable.shopee.sg
SHOPEE_ACCOUNT_HOST=https://account.sandbox.test-stable.shopee.com
```

### Produção (Futuro)
```bash
# .env.production
SHOPEE_PARTNER_ID=seu-partner-id-producao
SHOPEE_PARTNER_KEY=sua-partner-key-producao
SHOPEE_HOST=https://openplatform.shopee.com
SHOPEE_ACCOUNT_HOST=https://account.shopee.com
```

## 🌐 URLs Sandbox Completas

### OpenPlatform Sandbox
```
Base: https://openplatform.sandbox.test-stable.shopee.sg

Endpoints:
- Auth Partner:      /api/v2/shop/auth_partner
- Get Token:         /api/v2/auth/token/get
- Refresh Token:     /api/v2/auth/access_token/get
- Get Shops:         /api/v2/shop/get_partner_shop
```

### Account Sandbox
```
Base: https://account.sandbox.test-stable.shopee.com

Endpoints:
- Login:             /
- OAuth Callback:    /oauth/callback
- Account:           /account
```

## 🔐 Segurança Sandbox

✅ **Isolado**: Dados sandbox não afetam produção
✅ **Teste**: Todos os fluxos podem ser testados
✅ **Sem custo**: Nenhuma transação real
✅ **Resetável**: Dados podem ser resetados
✅ **HMAC**: Assinatura válida mesmo em sandbox

## 🚀 Status

```
┌──────────────────────────────────┐
│ ✅ SANDBOX CONFIGURADO           │
│                                  │
│ API Host:     Sandbox ✅         │
│ Account Host: Sandbox ✅         │
│ Variáveis:    Atualizadas ✅     │
│ Código:       Pronto ✅          │
│ Compilação:   OK ✅              │
│ Produção:     Fácil de mudar ✅  │
└──────────────────────────────────┘
```

## 📞 Próximos Passos

1. **Testar Localmente**
   ```bash
   dotnet build
   dotnet run
   GET http://localhost:5000/shopee/webhook/auth-url?email=test@sandbox.com
   ```

2. **Autorizar Cliente de Teste**
   - Enviar `authUrl` para cliente
   - Cliente clica e faz login em sandbox Shopee

3. **Receber Code**
   - Sistema recebe `code` de sandbox Shopee
   - Troca por token sandbox

4. **Testar APIs**
   - Use access_token sandbox
   - Faça requisições para Shopee sandbox

5. **Mudar para Produção** (quando pronto)
   - Trocar `SHOPEE_HOST` em variáveis
   - Trocar `SHOPEE_PARTNER_KEY`
   - Nenhuma mudança de código!

---

**Data**: February 4, 2026
**Versão**: 1.0
**Status**: ✅ Sandbox Implementado e Pronto
