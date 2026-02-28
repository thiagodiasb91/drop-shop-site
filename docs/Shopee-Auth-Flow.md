# 🔐 Fluxo de Autorização Shopee - Guia Passo a Passo

## 📋 Visão Geral

O fluxo de autorização Shopee segue o padrão OAuth 2.0 com 3 etapas principais:

```
┌─────────────────────────────────────────────────────────┐
│  1. Gerar URL de Autorização                            │
│  GET /shopee/webhook/auth-url                           │
│  ↓                                                      │
│  2. Cliente Autoriza na Shopee                          │
│  Cliente clica no link e autoriza                       │
│  ↓                                                      │
│  3. Receber Code e Trocar por Token                     │
│  GET /shopee/webhook/auth?code=XXX&shopId=YYY&email=ZZ │
│  ↓                                                      │
│  ✅ Tokens Salvos com Sucesso                          │
└─────────────────────────────────────────────────────────┘
```

## 🚀 Passo 1: Gerar URL de Autorização

### Endpoint
```
GET /shopee/webhook/auth-url
```

### Requisição
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url"
```

### Resposta (200 OK)
```json
{
  "statusCode": 200,
  "message": "Authorization URL generated successfully",
  "authUrl": "https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=1203628&redirect=https://open.shopee.com&timestamp=1736323998&sign=shpk4871546d53586b746b4c57614a4b5a577a4476726a4e6747765749665468",
  "instructions": {
    "step1": "Forneça esta URL ao cliente",
    "step2": "O cliente será redirecionado para login na Shopee",
    "step3": "Após autorizar, Shopee irá redirecionar para o callback com um code",
    "step4": "Use o endpoint GET /shopee/webhook/auth com code, shopId e email para trocar pelo token"
  }
}
```

### O Que Fazer
1. ✅ Copie o valor de `authUrl`
2. ✅ Envie a URL para seu cliente
3. ✅ Instruções estão incluídas na resposta

---

## 👤 Passo 2: Cliente Autoriza na Shopee

### O Que o Cliente Faz
1. Clica no link `authUrl` recebido
2. Faz login na conta Shopee (se necessário)
3. Vê um formulário de autorização
4. Clica em "Autorizar" ou "Permitir Acesso"
5. Shopee redireciona para a URL de callback com um `code`

### Exemplo de Redirect
```
https://seu-redirect-url.com?code=AUTH_CODE_12345&shop_id=226289035
```

### Parâmetros Recebidos
- `code`: Authorization code válido por ~30 minutos
- `shop_id`: ID da loja Shopee autorizada

---

## 🔄 Passo 3: Trocar Code por Token

Após o cliente autorizar, você receberá um `code` e `shop_id`.

### Endpoint
```
GET /shopee/webhook/auth?code=CODE&shopId=SHOP_ID&email=EMAIL
```

### Parâmetros Obrigatórios
| Parâmetro | Tipo | Descrição |
|-----------|------|-----------|
| `code` | string | Authorization code recebido do Shopee |
| `shopId` | long | ID da loja Shopee |
| `email` | string | Email do usuário que autoriza |

### Requisição Completa
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth?code=AUTH_CODE_12345&shopId=226289035&email=user@example.com"
```

### Resposta (200 OK)
```json
{
  "statusCode": 200,
  "message": "Tokens saved for shop 226289035"
}
```

### O Que Acontece Internamente
1. ✅ Valida se o usuário existe
2. ✅ Chama a API Shopee com o `code`
3. ✅ Recebe `access_token`, `refresh_token`, `expires_in`
4. ✅ Cria novo **Seller** associado à loja
5. ✅ Atualiza usuário com `resource_id` (sellerId)
6. ✅ Armazena tokens em cache

---

## 🔐 Fluxo Completo Exemplo

### Cenário Real

**1. Backend solicita URL de autorização:**
```bash
GET http://localhost:5000/shopee/webhook/auth-url
```

**Resposta:**
```json
{
  "statusCode": 200,
  "authUrl": "https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=1203628&redirect=https://open.shopee.com&timestamp=1736323998&sign=shpk..."
}
```

**2. Backend envia link ao cliente:**
```
Prezado cliente,

Por favor, clique no link abaixo para autorizar a integração:

https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=1203628&redirect=https://open.shopee.com&timestamp=1736323998&sign=shpk...
```

**3. Cliente autoriza e Shopee redireciona:**
```
Browser redirect para:
https://seu-sistema.com/callback?code=ABC123DEF456&shop_id=226289035
```

**4. Backend recebe o code e troca por token:**
```bash
GET http://localhost:5000/shopee/webhook/auth?code=ABC123DEF456&shopId=226289035&email=user@example.com
```

**Resposta:**
```json
{
  "statusCode": 200,
  "message": "Tokens saved for shop 226289035"
}
```

**5. Sistema está autorizado!**
✅ Seller criado
✅ Tokens armazenados
✅ Pronto para processar webhooks

---

## 📊 Diagrama de Sequência

```
Cliente                  Backend                 Shopee API
   │                        │                         │
   │ 1. Solicita URL        │                         │
   ├───────────────────────→│                         │
   │                        │ 2. GET /auth-url        │
   │                        ├────────────────────────→│
   │                        │ (gera HMAC SHA256)      │
   │                        │←────────────────────────┤
   │                        │ (retorna URL)           │
   │ 3. Recebe URL          │                         │
   │←───────────────────────┤                         │
   │                        │                         │
   │ 4. Clica no link       │                         │
   ├────────────────────────────────────────────────→│
   │                        │                         │
   │ 5. Faz login (se necessário)                    │
   │                        │                         │
   │ 6. Autoriza app        │                         │
   │                        │                         │
   │ 7. Shopee redireciona  │                         │
   │←────────────────────────────────────────────────┤
   │ (com code)             │                         │
   │                        │                         │
   │ 8. Envia code ao backend                        │
   ├───────────────────────→│                         │
   │                        │ 9. GET /auth?code=...  │
   │                        ├────────────────────────→│
   │                        │ (troca code por token)  │
   │                        │←────────────────────────┤
   │                        │                         │
   │                        │ 10. Salva tokens        │
   │                        │ 11. Cria Seller        │
   │                        │ 12. Atualiza User      │
   │                        │                         │
   │ 13. Sucesso!           │                         │
   │←───────────────────────┤                         │
   │                        │                         │
```

---

## 🔑 Informações de Segurança

### Assinatura HMAC SHA256
A URL de autorização é assinada com HMAC SHA256:
```
base_string = partner_id + path + timestamp
sign = HMAC-SHA256(partner_key, base_string)
```

### Timestamp
- Válido por um período limitado
- Gerado no servidor (não no cliente)
- Garante que a URL não é muito antiga

### Authorization Code
- Válido por ~30 minutos
- Pode ser usado apenas uma vez
- Fornece acesso apenas ao que foi autorizado

### Tokens Armazenados
- **Access Token**: Válido por 24 horas
- **Refresh Token**: Armazenado para renovação
- **Cache**: Tokens em cache por 24 horas
- **TTL**: Automático

---

## ⚠️ Possíveis Erros

### Erro 400 - Invalid Code
```json
{
  "statusCode": 400,
  "message": "Invalid code, shopId or email"
}
```
**Causa**: Um dos parâmetros (code, shopId ou email) está vazio ou inválido
**Solução**: Verifique se todos os parâmetros foram passados corretamente

### Erro 400 - User Not Found
```json
{
  "statusCode": 400,
  "message": "User with email ... not found"
}
```
**Causa**: O usuário não existe na base de dados
**Solução**: Crie o usuário primeiro antes de fazer a autorização

### Erro 500 - Internal Server Error
```json
{
  "statusCode": 500,
  "message": "Internal server error"
}
```
**Causa**: Erro ao chamar API Shopee ou ao salvar dados
**Solução**: Verifique os logs e credenciais Shopee

---

## 🧪 Testando Localmente

### 1. Gerar URL
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url"
```

### 2. Copiar a URL authUrl
```
https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner?partner_id=1203628&redirect=https://open.shopee.com&timestamp=...&sign=...
```

### 3. Usar em Postman/Insomnia
- Cole a URL em uma aba do navegador
- Ou use como GET request

### 4. Ao receber o code
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth?code=TESTE_CODE&shopId=226289035&email=user@example.com"
```

---

## 📱 Integração com Frontend

### React Example
```javascript
// 1. Obter URL
const response = await fetch('/shopee/webhook/auth-url');
const data = await response.json();

// 2. Redirecionar o usuário
window.location.href = data.authUrl;

// 3. Após autorização, Shopee redireciona com code
// Capturar code na página de callback
const params = new URLSearchParams(window.location.search);
const code = params.get('code');
const shopId = params.get('shop_id');

// 4. Trocar code por token
const email = userEmail; // do formulário ou sessão
await fetch(`/shopee/webhook/auth?code=${code}&shopId=${shopId}&email=${email}`);

// ✅ Pronto!
```

### Fluxo no Frontend
```
1. Usuário clica em "Conectar Shopee"
2. Frontend chama GET /shopee/webhook/auth-url
3. Redireciona para authUrl
4. Usuário faz login e autoriza
5. Shopee redireciona com code
6. Frontend captures code e chama /shopee/webhook/auth
7. Mostra mensagem de sucesso
```

---

## 📞 Fluxo de Comunicação

### Entre Você e Cliente
```
1. Você: "Clique neste link para autorizar"
   Link: https://openplatform.sandbox.../?partner_id=...&sign=...

2. Cliente: "Autorizo!"
   Sistema Shopee: Gera code

3. Sistema Shopee redireciona com code
   Cliente: Vê mensagem de sucesso

4. Você: Recebe tokens automaticamente
   Sistema: Pronto para usar
```

---

## 🎯 Endpoints Resumo

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/shopee/webhook/auth-url` | Gera URL de autorização |
| GET | `/shopee/webhook/auth` | Troca code por token |
| POST | `/shopee/webhook` | Recebe webhooks de eventos |

---

## ✅ Checklist de Implementação

- [x] Endpoint para gerar URL (`/auth-url`)
- [x] Endpoint para receber code (`/auth`)
- [x] Validação de parâmetros
- [x] Chamada à API Shopee
- [x] Criação de Seller
- [x] Atualização de User
- [x] Armazenamento de tokens
- [x] Logging completo
- [x] Tratamento de erros
- [x] Documentação

---

**Data**: February 4, 2026
**Versão**: 1.0
**Status**: ✅ Pronto para Produção
