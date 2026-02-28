# 🔐 Fluxo de Login e Autorização - Guia Completo

## 📋 Visão Geral

Documentação completa do fluxo de login, cadastro de usuário e autorização de sellers com integração Shopee.

---

## 🎯 1. Primeiro Login - Cadastro no Cognito

### Fluxo Visual

```
┌─────────────────────────────────────────┐
│ 1. Usuário Acessa Aplicação             │
│    (Sem estar autenticado)              │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│ 2. Sistema Redireciona para Cognito     │
│    GET /auth/login                      │
│    ↓                                    │
│    Cognito: Fazer cadastro/login        │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│ 3. Cognito Redireciona para Back        │
│    /auth/callback?code=...&state=...    │
│                                         │
│    Backend valida code e cria JWT       │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│ 4. Verificar se Usuário Existe no BD    │
│                                         │
│    SELECT * FROM users                  │
│    WHERE cognito_id = ?                 │
│                                         │
│    Usuário NÃO existe?                  │
│    └─ role = "new-user"  ✅            │
│                                         │
│    Usuário existe?                      │
│    └─ role = "admin"|"seller"|"vendor"  │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│ 5. Retornar ao Front                    │
│    {                                    │
│      "jwt_token": "...",               │
│      "role": "new-user" ou outro,      │
│      "email": "user@example.com",      │
│      "cognito_id": "12345678"          │
│    }                                    │
└────────────┬────────────────────────────┘
             │
┌────────────▼────────────────────────────┐
│ 6. Front: Exibir Mensagem               │
│    if (role === "new-user") {           │
│      "Você ainda não tem acesso        │
│       definido"                         │
│                                         │
│      Exibir opções:                    │
│      - Admin                            │
│      - Fornecedor (Seller)             │
│      - Vendedor                         │
│    }                                    │
└─────────────────────────────────────────┘
```

### Endpoints Envolvidos

#### Backend: GET /auth/login
**Descrição**: Iniciar fluxo de login com Cognito

```http
GET /auth/login HTTP/1.1
Host: api.dropship.com
```

**Redirecionamento**:
```
→ https://cognito-domain/oauth2/authorize?
  client_id=YOUR_CLIENT_ID&
  response_type=code&
  scope=openid profile email&
  redirect_uri=https://api.dropship.com/auth/callback&
  state=STATE_TOKEN
```

---

#### Backend: GET /auth/callback
**Descrição**: Receber callback do Cognito e criar sessão

```http
GET /auth/callback?code=AUTH_CODE&state=STATE_TOKEN HTTP/1.1
Host: api.dropship.com
```

**Processo**:
1. ✅ Validar `state` token (CSRF protection)
2. ✅ Trocar `code` por `id_token` + `access_token` com Cognito
3. ✅ Extrair `cognito_id` e `email` do token
4. ✅ Verificar se usuário existe no BD
5. ✅ Se não existe: criar usuário com `role="new-user"`
6. ✅ Gerar JWT interno
7. ✅ Redirecionar para front com token

**Resposta (Redirect)**:
```
→ https://frontend.com/auth/callback?
  token=JWT_TOKEN&
  role=new-user&
  email=user@example.com&
  cognito_id=12345678
```

---

#### Backend: POST /users/set-role
**Descrição**: Usuário escolhe seu papel (admin, seller, vendor)

```http
POST /users/set-role HTTP/1.1
Host: api.dropship.com
Authorization: Bearer JWT_TOKEN
Content-Type: application/json

{
  "role": "seller"
}
```

**Processo**:
1. ✅ Autenticar JWT
2. ✅ Validar role (seller, admin, vendor)
3. ✅ Criar registro `users#meta` no DynamoDB
   ```json
   {
     "PK": "User#{cognito_id}",
     "SK": "META",
     "cognito_id": "12345678",
     "email": "user@example.com",
     "role": "seller",
     "created_at": "2026-02-05T10:30:00Z",
     "updated_at": "2026-02-05T10:30:00Z"
   }
   ```
4. ✅ Retornar novo JWT com role atualizado

**Resposta (200 OK)**:
```json
{
  "status": "success",
  "message": "Role definido com sucesso",
  "jwt_token": "NEW_JWT_WITH_ROLE",
  "role": "seller"
}
```

---

## 👨‍💼 2. Seller - Setup Inicial

### 2.1 Verificar Status do Seller

#### Frontend: Chamar GET /me
**Descrição**: Obter informações do usuário logado

```javascript
// Frontend
const response = await fetch('/api/me', {
  headers: {
    'Authorization': 'Bearer JWT_TOKEN'
  }
});

const data = await response.json();
console.log(data);
// {
//   "cognito_id": "12345678",
//   "email": "seller@example.com",
//   "role": "seller",
//   "seller_id": null,        // ← Null = novo seller!
//   "shop_id": null,
//   "access_token": null
// }
```

#### Backend: GET /me
**Descrição**: Retornar dados do usuário autenticado

```http
GET /me HTTP/1.1
Host: api.dropship.com
Authorization: Bearer JWT_TOKEN
```

**Resposta (200 OK)**:
```json
{
  "cognito_id": "12345678",
  "email": "seller@example.com",
  "role": "seller",
  "seller_id": null,
  "shop_id": null,
  "access_token": null,
  "created_at": "2026-02-05T10:30:00Z"
}
```

### 2.2 Frontend Redireciona para Setup

```javascript
// Frontend - useEffect no App.tsx
useEffect(() => {
  const checkUserStatus = async () => {
    const response = await fetch('/api/me', {
      headers: { 'Authorization': `Bearer ${jwtToken}` }
    });
    
    const user = await response.json();
    
    // Se seller sem seller_id, redirecionar para setup
    if (user.role === 'seller' && !user.seller_id) {
      navigate(`/sellers/${user.cognito_id}/store/setup`);
    }
  };
  
  checkUserStatus();
}, [jwtToken]);
```

**URL**: `https://frontend.com/sellers/{cognito_id}/store/setup`

### 2.3 Página Setup - Conectar Shopee

```javascript
// Frontend - /sellers/{id}/store/setup
import { useParams, useNavigate } from 'react-router-dom';

export function StoreSetup() {
  const { sellerId } = useParams();
  const navigate = useNavigate();
  
  const handleConnectShopee = async () => {
    // Chamar backend para gerar URL de autorização
    const response = await fetch('/api/shopee/webhook/auth-url', {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${jwtToken}`,
        'X-Seller-Id': sellerId
      }
    });
    
    const { authUrl } = await response.json();
    
    // Redirecionar para Shopee
    window.location.href = authUrl;
  };
  
  return (
    <div>
      <h1>Configurar Loja Shopee</h1>
      <button onClick={handleConnectShopee}>
        Conectar com Shopee
      </button>
    </div>
  );
}
```

---

## 🛍️ 3. Autorização Shopee

### 3.1 Shopee Redireciona com Code

#### Fluxo Detalhado

```
┌──────────────────────────────────────┐
│ 1. Frontend Redireciona para Shopee  │
│    URL: authUrl com sign HMAC        │
└────────────┬─────────────────────────┘
             │
┌────────────▼─────────────────────────┐
│ 2. Usuário Faz Login Shopee          │
│    └─ Autoriza aplicação             │
└────────────┬─────────────────────────┘
             │
┌────────────▼─────────────────────────┐
│ 3. Shopee Redireciona                │
│    Para: /sellers/{email}/store/code │
│    Com: code={code}&shop_id={shopId} │
└────────────┬─────────────────────────┘
             │
┌────────────▼─────────────────────────┐
│ 4. Frontend Captura Parâmetros       │
│    ├─ code (authorization code)      │
│    ├─ shop_id (loja Shopee)         │
│    ├─ email (extrair da URL)         │
│    └─ Enviar ao backend              │
└─────────────────────────────────────┘
```

### 3.2 Frontend - Página de Callback

```javascript
// Frontend - /sellers/{email}/store/code
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useEffect } from 'react';

export function ShopeeCallback() {
  const { email } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  
  useEffect(() => {
    const code = searchParams.get('code');
    const shopId = searchParams.get('shop_id');
    
    if (code && shopId) {
      // Chamar backend para salvar code e shop_id
      handleShopeeAuth(email, code, shopId);
    }
  }, [searchParams]);
  
  const handleShopeeAuth = async (email, code, shopId) => {
    try {
      const response = await fetch('/api/shopee/webhook/auth', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${jwtToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email,
          code,
          shop_id: shopId
        })
      });
      
      if (response.ok) {
        // Sucesso! Atualizar session
        const updatedUser = await fetch('/api/me', {
          headers: { 'Authorization': `Bearer ${jwtToken}` }
        }).then(r => r.json());
        
        // Salvar seller_id na sessão
        setUserSession({
          ...userSession,
          seller_id: updatedUser.seller_id
        });
        
        // Redirecionar para Home
        navigate('/home');
      }
    } catch (error) {
      console.error('Erro ao autenticar Shopee:', error);
    }
  };
  
  return (
    <div>
      <h1>Autenticando com Shopee...</h1>
      <p>Por favor, aguarde...</p>
    </div>
  );
}
```

**URL**: `https://frontend.com/sellers/{email}/store/code?code=ABC123&shop_id=123456`

### 3.3 Backend - Receber Code e Shop ID

#### Backend: POST /shopee/webhook/auth
**Descrição**: Trocar code por token e salvar credenciais

```http
POST /shopee/webhook/auth HTTP/1.1
Host: api.dropship.com
Authorization: Bearer JWT_TOKEN
Content-Type: application/json

{
  "email": "seller@example.com",
  "code": "AUTH_CODE_FROM_SHOPEE",
  "shop_id": "226289035"
}
```

**Processo**:
1. ✅ Validar JWT e email
2. ✅ Trocar `code` por `access_token` com Shopee
   ```
   POST https://openplatform.sandbox.test-stable.shopee.sg/api/v2/auth/token/get
   Body: { code, shop_id, partner_id }
   ```
3. ✅ Receber `access_token` e `refresh_token`
4. ✅ Criar Seller no DynamoDB
   ```json
   {
     "PK": "Seller#{seller_id}",
     "SK": "META",
     "seller_id": "94d52c7b-8f45-4a07-b2b2-65ca9a18537b",
     "email": "seller@example.com",
     "shop_id": "226289035",
     "access_token": "ENCRYPTED_TOKEN",
     "refresh_token": "ENCRYPTED_TOKEN",
     "created_at": "2026-02-05T10:30:00Z"
   }
   ```
5. ✅ Atualizar User
   ```json
   {
     "PK": "User#{cognito_id}",
     "SK": "META",
     "seller_id": "94d52c7b-8f45-4a07-b2b2-65ca9a18537b",
     "shop_id": "226289035",
     "updated_at": "2026-02-05T10:30:00Z"
   }
   ```
6. ✅ Retornar resposta com seller_id

**Resposta (200 OK)**:
```json
{
  "status": "success",
  "message": "Loja conectada com sucesso",
  "seller_id": "94d52c7b-8f45-4a07-b2b2-65ca9a18537b",
  "shop_id": "226289035",
  "shop_name": "Minha Loja"
}
```

---

## 🏠 4. Redirecionamento Final - Home

### Frontend Atualiza Session e Redireciona

```javascript
// Frontend - após sucesso no auth
const updateSessionAndRedirect = async () => {
  // 1. Buscar dados atualizados do usuário
  const user = await fetch('/api/me', {
    headers: { 'Authorization': `Bearer ${jwtToken}` }
  }).then(r => r.json());
  
  // 2. Atualizar session/context
  setUserSession({
    cognito_id: user.cognito_id,
    email: user.email,
    role: user.role,
    seller_id: user.seller_id,      // ✅ Agora tem valor!
    shop_id: user.shop_id,
    access_token: user.access_token  // Token do Shopee
  });
  
  // 3. Salvar no localStorage/sessionStorage
  localStorage.setItem('user', JSON.stringify(user));
  
  // 4. Redirecionar para Home
  navigate('/home');
};
```

---

## 📊 Tabela Resumida de Rotas

| Etapa | Método | Rota | Autenticado | Descrição |
|-------|--------|------|-------------|-----------|
| 1 | GET | `/auth/login` | Não | Iniciar login Cognito |
| 2 | GET | `/auth/callback` | Não | Callback Cognito |
| 3 | POST | `/users/set-role` | Sim | Definir role do usuário |
| 4 | GET | `/me` | Sim | Obter dados do usuário |
| 5 | GET | `/shopee/webhook/auth-url` | Sim | Gerar URL Shopee |
| 6 | POST | `/shopee/webhook/auth` | Sim | Receber code e salvar |

---

## 🔄 Estados do Usuário

### Estado 1: Novo Usuário (new-user)
```json
{
  "cognito_id": "12345678",
  "email": "user@example.com",
  "role": "new-user",
  "seller_id": null,
  "shop_id": null,
  "access_token": null
}
```
**Ação**: Exibir seleção de role

### Estado 2: Seller Sem Loja
```json
{
  "cognito_id": "12345678",
  "email": "seller@example.com",
  "role": "seller",
  "seller_id": null,
  "shop_id": null,
  "access_token": null
}
```
**Ação**: Redirecionar para `/sellers/{id}/store/setup`

### Estado 3: Seller com Loja
```json
{
  "cognito_id": "12345678",
  "email": "seller@example.com",
  "role": "seller",
  "seller_id": "94d52c7b-8f45-4a07-b2b2-65ca9a18537b",
  "shop_id": "226289035",
  "access_token": "ENCRYPTED_TOKEN"
}
```
**Ação**: Permitir acesso ao dashboard

---

## 🔐 Fluxo Completo Visual

```
┌─────────────────────────────────────────────────────────────┐
│                    FLUXO COMPLETO                           │
└─────────────────────────────────────────────────────────────┘

1️⃣  Login
    └─ Frontend → GET /auth/login
       └─ Redireciona para Cognito

2️⃣  Callback Cognito
    └─ Cognito → GET /auth/callback?code=...
       └─ Backend cria usuário com role=new-user
       └─ Redireciona para Frontend com JWT

3️⃣  Definir Role
    └─ Frontend → POST /users/set-role
       └─ Backend cria user#meta
       └─ Retorna JWT com role

4️⃣  Verificar Status
    └─ Frontend → GET /me
       └─ se role=seller && seller_id=null
       └─ Redirecionar para Setup

5️⃣  Setup Loja
    └─ Frontend → GET /shopee/webhook/auth-url
       └─ Backend gera URL com assinatura HMAC
       └─ Frontend redireciona para Shopee

6️⃣  Autorização Shopee
    └─ Usuário faz login em Shopee
       └─ Shopee redireciona com code + shop_id
       └─ Para: /sellers/{email}/store/code?code=...&shop_id=...

7️⃣  Salvar Credenciais
    └─ Frontend → POST /shopee/webhook/auth
       └─ Backend troca code por token
       └─ Cria Seller no BD
       └─ Atualiza User com seller_id
       └─ Retorna seller_id

8️⃣  Home
    └─ Frontend atualiza session
       └─ Armazena seller_id
       └─ Redireciona para /home ✅
```

---

## 🧪 Teste Manual - Passo a Passo

### 1. Novo Usuário
```bash
# 1.1 Acessar
curl "http://localhost:5000/auth/login"
# Será redirecionado para Cognito

# 1.2 Após fazer login no Cognito
# Sistema chama: GET /auth/callback?code=...
# Usuário é criado com role=new-user
```

### 2. Definir Role
```bash
# 2.1 Definir como Seller
curl -X POST "http://localhost:5000/users/set-role" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"role":"seller"}'

# Resposta: { role: "seller", jwt_token: "..." }
```

### 3. Verificar Status
```bash
# 3.1 Obter dados
curl "http://localhost:5000/api/me" \
  -H "Authorization: Bearer JWT_TOKEN"

# Resposta: { role: "seller", seller_id: null, ... }
```

### 4. Conectar Shopee
```bash
# 4.1 Gerar URL
curl "http://localhost:5000/api/shopee/webhook/auth-url?email=seller@example.com" \
  -H "Authorization: Bearer JWT_TOKEN"

# Resposta: { authUrl: "https://partner.test-stable.shopeemobile.com/..." }
# Clicar no link...

# 4.2 Shopee redireciona para:
# https://frontend.com/sellers/seller@example.com/store/code?code=ABC123&shop_id=123456

# 4.3 Frontend chama
curl -X POST "http://localhost:5000/api/shopee/webhook/auth" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"email":"seller@example.com","code":"ABC123","shop_id":"123456"}'

# Resposta: { seller_id: "...", shop_id: "123456" }
```

### 5. Verificar Novamente
```bash
# 5.1 Verificar status atualizado
curl "http://localhost:5000/api/me" \
  -H "Authorization: Bearer JWT_TOKEN"

# Resposta: { seller_id: "...", shop_id: "123456", ... }
# Agora pode acessar dashboard! ✅
```

---

**Data**: February 5, 2026
**Versão**: 1.0
**Status**: ✅ Completo
