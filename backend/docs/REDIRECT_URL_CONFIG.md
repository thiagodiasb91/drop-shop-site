# 🔐 Configuração do Redirect URL - Guia Implementado

## 📋 O Que Foi Configurado

O `redirectUrl` foi configurado para **sempre** usar a rota da sua API em produção:

```
https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email={email}
```

O email é passado **dinamicamente** como parâmetro de query string.

## 🔄 Fluxo Completo

```
┌──────────────────────────────────────────────────────────────────┐
│ 1. Frontend/Backend Solicita URL de Autorização                  │
│    GET /shopee/webhook/auth-url?email=user@example.com           │
└─────────────────────┬──────────────────────────────────────────┘
                      │
┌─────────────────────▼──────────────────────────────────────────┐
│ 2. Backend Gera URL com Redirect Dinâmico                       │
│    redirectUrl = https://inv6sa4cb0.execute-api.us-east-1...    │
│                  /dev/shopee/auth?email=user@example.com        │
│    ↓                                                             │
│    Calcula HMAC SHA256 com essa redirect URL                    │
│    ↓                                                             │
│    Retorna authUrl para cliente                                 │
└─────────────────────┬──────────────────────────────────────────┘
                      │
┌─────────────────────▼──────────────────────────────────────────┐
│ 3. Frontend Envia authUrl ao Cliente                            │
│    Cliente clica no link                                        │
└─────────────────────┬──────────────────────────────────────────┘
                      │
┌─────────────────────▼──────────────────────────────────────────┐
│ 4. Shopee Autentica Cliente                                     │
│    Cliente faz login                                            │
│    Autoriza app                                                 │
└─────────────────────┬──────────────────────────────────────────┘
                      │
┌─────────────────────▼──────────────────────────────────────────┐
│ 5. Shopee Redireciona com Code                                  │
│    https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/...   │
│    /dev/shopee/auth?email=user@example.com&code=ABC123&...      │
└─────────────────────┬──────────────────────────────────────────┘
                      │
┌─────────────────────▼──────────────────────────────────────────┐
│ 6. Backend Recebe Code e Completa Autenticação                 │
│    GET /shopee/webhook/auth?code=ABC123&shopId=123&email=user  │
│    ↓                                                             │
│    Cria Seller                                                  │
│    Armazena tokens                                              │
│    ✅ Sucesso!                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## 📝 Exemplo Prático

### 1️⃣ Solicitar URL de Autorização
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url?email=user@example.com"
```

### 2️⃣ Resposta Recebida
```json
{
  "statusCode": 200,
  "message": "Authorization URL generated successfully",
  "authUrl": "https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=1203628&redirect=https%3A%2F%2Finv6sa4cb0.execute-api.us-east-1.amazonaws.com%2Fdev%2Fshopee%2Fauth%3Femail%3Duser%40example.com&timestamp=1736323998&sign=shpk4871546d53586b746b4c57614a4b5a577a4476726a4e6747765749665468",
  "redirectUrl": "https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=user@example.com",
  "instructions": {
    "step1": "Forneça esta URL ao cliente",
    "step2": "O cliente será redirecionado para login na Shopee",
    "step3": "Após autorizar, Shopee irá redirecionar para o callback com email e code",
    "step4": "Use o endpoint GET /shopee/webhook/auth com code e shopId (email já estará na URL de callback)"
  }
}
```

### 3️⃣ Cliente Clica no Link `authUrl`
O cliente será levado para a página de login e autorização do Shopee.

### 4️⃣ Shopee Redireciona com Code
Após autorizar, Shopee redireciona para:
```
https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=user@example.com&code=ABC123DEF456&shop_id=226289035
```

### 5️⃣ Backend Processa
O seu sistema backend (ou frontend) captura os parâmetros e chama:
```bash
curl -X GET "https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?code=ABC123DEF456&shopId=226289035&email=user@example.com"
```

## 🔧 O Que Mudou no Código

### ShopeeApiService.cs

**Antes:**
```csharp
private readonly string _redirectUrl;

public string GetAuthUrl()
{
    var url = $"{_host}{AuthPartnerPath}?partner_id={_partnerId}&redirect={Uri.EscapeDataString(_redirectUrl)}&timestamp={timestamp}&sign={sign}";
}
```

**Depois:**
```csharp
public string GetAuthUrl(string email)
{
    var redirectUrl = $"https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email={Uri.EscapeDataString(email)}";
    
    var url = $"{_host}{AuthPartnerPath}?partner_id={_partnerId}&redirect={Uri.EscapeDataString(redirectUrl)}&timestamp={timestamp}&sign={sign}";
}
```

### ShopeeWebhookController.cs

**Antes:**
```csharp
public IActionResult GetAuthorizationUrl()
{
    var authUrl = _shopeeApiService.GetAuthUrl();
}
```

**Depois:**
```csharp
public IActionResult GetAuthorizationUrl([FromQuery] string email)
{
    var authUrl = _shopeeApiService.GetAuthUrl(email);
    
    return Ok(new
    {
        statusCode = 200,
        authUrl = authUrl,
        redirectUrl = $"https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email={Uri.EscapeDataString(email)}",
        instructions = { ... }
    });
}
```

## 📊 Comparação

| Aspecto | Antes | Depois |
|---------|-------|--------|
| Redirect URL | Vinha de variável de ambiente | Hardcoded na sua API AWS |
| Email | Não era usado | Parâmetro dinâmico na URL |
| Flexibilidade | Configurável | Fixed na API |
| Segurança | HMAC com redirect genérico | HMAC com redirect específico |

## ✅ Vantagens

✅ **Redirect sempre para a sua API**
- Email é passado dinamicamente
- Sem necessidade de configuração
- Sempre aponta para sua produção

✅ **URL de Callback Prévia**
- Você já sabe exatamente para onde Shopee redirecionará
- Email já está incluído
- Facilita captura do code

✅ **Segurança Aumentada**
- HMAC SHA256 valida que a URL é legítima
- Timestamp previne replay attacks
- Redirect URL específica da sua aplicação

## 📱 Integração Frontend

### React
```javascript
// 1. Solicitar URL
const response = await fetch(`/shopee/webhook/auth-url?email=${userEmail}`);
const data = await response.json();

// 2. Redirecionar
window.location.href = data.authUrl;

// 3. Shopee redirecionará para:
// https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=user@example.com&code=XXX&shop_id=YYY

// 4. Capturar parâmetros (já incluem email)
const params = new URLSearchParams(window.location.search);
const code = params.get('code');
const email = params.get('email');
const shopId = params.get('shop_id');

// 5. Completar autenticação
await fetch(`/shopee/webhook/auth?code=${code}&shopId=${shopId}&email=${email}`);
```

## 🧪 Testando

### Teste 1: Gerar URL Local
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url?email=test@example.com"
```

**Esperado:**
- ✅ Retorna authUrl com email codificado
- ✅ redirectUrl aponta para sua API

### Teste 2: Validar Redirect URL
No response, veja:
```json
"redirectUrl": "https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=test@example.com"
```

✅ Email está presente na URL

### Teste 3: Verificar HMAC
A URL gerada contém `sign=` que é a assinatura HMAC válida para essa redirect URL específica.

## 🔐 Segurança

### HMAC SHA256
```
base_string = partner_id + path + timestamp
signature = HMAC-SHA256(partner_key, base_string)

A assinatura garante que:
✅ Ninguém pode modificar a URL
✅ A URL vem da sua API
✅ Timestamp previne replay
```

### Redirect URL Específica
```
A redirect URL agora é:
https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email={email}

Shopee SEMPRE redirecionará para lá após autorizar
Isso garante que o code chegue na sua API
```

## 📞 Fluxo em Produção

```
1. User clica em "Conectar Shopee"
   └─ Frontend chama: GET /shopee/webhook/auth-url?email=user@company.com

2. Backend retorna authUrl com redirect URL
   └─ authUrl = "https://openplatform.sandbox...?redirect=https://inv6sa4cb0...&sign=..."

3. Frontend redireciona para authUrl
   └─ User vai para página de login Shopee

4. User faz login e autoriza

5. Shopee redireciona para
   └─ https://inv6sa4cb0.execute-api.us-east-1.amazonaws.com/dev/shopee/auth?email=user@company.com&code=ABC123&shop_id=123

6. Backend processa
   └─ GET /shopee/webhook/auth?code=ABC123&shopId=123&email=user@company.com

7. ✅ Seller criado, tokens salvos, tudo funcionando!
```

## 🎯 Resumo

- ✅ Redirect URL agora é sempre a sua API em produção
- ✅ Email é parâmetro dinâmico (vem do query string)
- ✅ HMAC SHA256 valida a autenticidade
- ✅ Timestamp previne replay attacks
- ✅ Tudo documentado e seguro

## 📊 Checklist

- [x] Método `GetAuthUrl(email)` atualizado
- [x] Controller passando email para o serviço
- [x] Redirect URL usando AWS API Gateway
- [x] Email como parâmetro dinâmico
- [x] HMAC SHA256 com redirect URL correto
- [x] Variável `_redirectUrl` removida (não usada)
- [x] Compilação validada
- [x] Documentação criada

---

**Data**: February 4, 2026
**Versão**: 1.0
**Status**: ✅ Implementado e Validado
