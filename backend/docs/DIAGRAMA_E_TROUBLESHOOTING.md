# 📊 Diagrama Detalhado + Troubleshooting

## 🎨 Diagrama de Sequência (UML Style)

```
Cliente                 Frontend                Backend              Cognito              Shopee
  │                        │                       │                   │                   │
  │─────────────────────→   │                       │                   │                   │
  │  Acessar App            │                       │                   │                   │
  │                        ├──────────────────────→ │                   │                   │
  │                        │  GET /auth/login      │                   │                   │
  │                        │                       ├──────────────────→ │                   │
  │                        │                       │ Redirecionar       │                   │
  │                        │←──────────────────────┤ para login         │                   │
  │                        │ Cognito URL           │                   │                   │
  │                        │                       │                   │                   │
  │←───────────────────────┤                       │                   │                   │
  │  Redireciona           │                       │                   │                   │
  │                        │                       │                   │                   │
  ├───────────────────────────────────────────────────────────────────→ │                   │
  │  Acessar URL                                    │                   │                   │
  │  Fazer login                                    │                   │                   │
  │                        │                       │                   │                   │
  │                        │                       │                   │←──────────────────┤
  │                        │                       │                   │ code, state       │
  │                        │                       │                   │                   │
  │←────────────────────────────────────────────────────────────────────────────────────────┤
  │  Redireciona com code  │                       │                   │                   │
  │                        │                       │                   │                   │
  │───────────────────────→ │                       │                   │                   │
  │  /auth/callback        │                       │                   │                   │
  │                        ├──────────────────────→ │                   │                   │
  │                        │  GET /auth/callback   │                   │                   │
  │                        │  code, state          │                   │                   │
  │                        │                       │                   │                   │
  │                        │                       ├──────────────────→ │                   │
  │                        │                       │ Validar code       │                   │
  │                        │                       │                   │                   │
  │                        │                       │←──────────────────┤                   │
  │                        │                       │ tokens             │                   │
  │                        │                       │                   │                   │
  │                        │←──────────────────────┤                   │                   │
  │                        │ JWT + role            │                   │                   │
  │                        │                       │                   │                   │
  │←───────────────────────┤                       │                   │                   │
  │  Redirect com JWT      │                       │                   │                   │
  │                        │                       │                   │                   │
  │  [Se role=new-user]                           │                   │                   │
  │  → Exibir seleção role                        │                   │                   │
  │  [Se role=seller]                             │                   │                   │
  │  → Verificar seller_id                        │                   │                   │
  │                        │                       │                   │                   │
  ├───────────────────────→ │                       │                   │                   │
  │  POST /users/set-role   │                       │                   │                   │
  │                        ├──────────────────────→ │                   │                   │
  │                        │ {"role": "seller"}   │                   │                   │
  │                        │                       │ [Criar user#meta] │                   │
  │                        │←──────────────────────┤                   │                   │
  │                        │ JWT com role          │                   │                   │
  │←───────────────────────┤                       │                   │                   │
  │  JWT atualizado        │                       │                   │                   │
  │                        │                       │                   │                   │
  │  [Verificar status]                           │                   │                   │
  │                        │                       │                   │                   │
  ├───────────────────────→ │                       │                   │                   │
  │  GET /me               │                       │                   │                   │
  │                        ├──────────────────────→ │                   │                   │
  │                        │                       │ [seller_id = null]│                   │
  │                        │←──────────────────────┤                   │                   │
  │                        │ user data             │                   │                   │
  │←───────────────────────┤                       │                   │                   │
  │  role=seller           │                       │                   │                   │
  │  seller_id=null        │                       │                   │                   │
  │                        │                       │                   │                   │
  │  [Redirecionar para setup]                    │                   │                   │
  │                        │                       │                   │                   │
  ├───────────────────────→ │                       │                   │                   │
  │  GET /shopee/auth-url  │                       │                   │                   │
  │                        ├──────────────────────→ │                   │                   │
  │                        │                       │ [Gerar authUrl]   │                   │
  │                        │←──────────────────────┤                   │                   │
  │                        │ authUrl               │                   │                   │
  │←───────────────────────┤                       │                   │                   │
  │  authUrl               │                       │                   │                   │
  │                        │                       │                   │                   │
  │  [Clicar no link]                             │                   │                   │
  ├───────────────────────────────────────────────────────────────────────────────────────→ │
  │  Fazer login Shopee    │                       │                   │                   │
  │  Autorizar app        │                       │                   │                   │
  │                        │                       │                   │                   │
  │←────────────────────────────────────────────────────────────────────────────────────────┤
  │  Redireciona           │                       │                   │                   │
  │  com code, shop_id     │                       │                   │                   │
  │                        │                       │                   │                   │
  │───────────────────────→ │                       │                   │                   │
  │  /shopee/callback      │                       │                   │                   │
  │                        ├──────────────────────→ │                   │                   │
  │                        │ POST /shopee/auth     │                   │                   │
  │                        │ {code, shop_id, email}│                   │                   │
  │                        │                       │                   │                   │
  │                        │                       ├──────────────────────────────────────→ │
  │                        │                       │ Trocar code por token               │
  │                        │                       │                   │                   │
  │                        │                       │←──────────────────────────────────────┤
  │                        │                       │ access_token, refresh_token          │
  │                        │                       │                   │                   │
  │                        │                       │ [Criar Seller]                       │
  │                        │                       │ [Atualizar User]                     │
  │                        │←──────────────────────┤                   │                   │
  │                        │ seller_id             │                   │                   │
  │←───────────────────────┤                       │                   │                   │
  │  seller_id             │                       │                   │                   │
  │  [Atualizar session]   │                       │                   │                   │
  │  [Redirecionar home]   │                       │                   │                   │
  │                        │                       │                   │                   │
  ▼                        ▼                       ▼                   ▼                   ▼
```

---

## 🔍 Estados e Transições

### Máquina de Estados

```
                    ┌──────────────────────────┐
                    │   Não Autenticado        │
                    │   (Sem JWT)              │
                    └───────────┬──────────────┘
                                │
                                │ Login com Cognito
                                │
                    ┌───────────▼──────────────┐
                    │   new-user               │
                    │   (role=new-user)        │
                    │   seller_id=null         │
                    └───────────┬──────────────┘
                                │
                                │ Escolher role
                                │ POST /users/set-role
                                │
                    ┌───────────▼──────────────┐
                    │   Seller Sem Loja        │
                    │   (role=seller)          │
                    │   seller_id=null         │
                    └───────────┬──────────────┘
                                │
                                │ Conectar Shopee
                                │ POST /shopee/auth
                                │
                    ┌───────────▼──────────────┐
                    │   Seller Com Loja        │
                    │   (role=seller)          │
                    │   seller_id != null      │
                    │   shop_id != null        │
                    │   access_token != null   │
                    └──────────────────────────┘
                                │
                                │ [ACESSO AO DASHBOARD]
                                │
                                ▼
```

---

## 🐛 Troubleshooting

### Problema 1: JWT Token Inválido/Expirado

**Sintoma**: 
```
GET /api/me
Response: 401 Unauthorized
```

**Causas**:
- Token expirou
- Token foi revogado
- Token malformado

**Solução**:
```bash
# 1. Verificar se token existe
echo $JWT_TOKEN

# 2. Decodificar token online
# Cole em: https://jwt.io

# 3. Verificar exp (data expiração)
# Se passado, fazer novo login

# 4. Se recente, verificar assinatura
```

---

### Problema 2: Usuário com role=new-user não consegue definir role

**Sintoma**:
```
POST /api/users/set-role
Response: 400 Bad Request
"role is already set"
```

**Causas**:
- Usuário já tem role definido
- Dados inconsistentes no BD

**Solução**:
```bash
# 1. Verificar status atual
curl -H "Authorization: Bearer $JWT" http://localhost:5000/api/me

# 2. Se role já está definido
# O /api/me retorna o role correto
# Frontend deve se adaptar

# 3. Se discrepância
# Limpar cache (localStorage)
# Fazer novo login
```

---

### Problema 3: Seller com seller_id=null mas role=seller

**Sintoma**:
```
GET /api/me
Response: {
  role: "seller",
  seller_id: null,
  shop_id: null
}
```

**Causas**:
- Usuário ainda está em setup
- Shopee auth não completou

**Solução**:
```bash
# 1. Verificar se está em setup
if (role === 'seller' && !seller_id) {
  navigate('/seller-setup');
}

# 2. Completar processo Shopee
# GET /shopee/auth-url
# Cliente clica no link
# Autoriza em Shopee
# POST /shopee/auth com code

# 3. Após sucesso
# seller_id será preenchido
```

---

### Problema 4: Shopee redireciona com erro de sign

**Sintoma**:
```
https://partner.test-stable.shopeemobile.com/...
Response: 404 Not Found
```

**Causas**:
- SHOPEE_PARTNER_KEY vazio
- Base string incorreta
- HMAC mal calculado

**Solução**:
Veja documento: `TROUBLESHOOTING_SIGN_ERROR.md` e `SHOPEE_PARTNER_KEY_CONFIG.md`

---

### Problema 5: Frontend não consegue capturar code do Shopee

**Sintoma**:
```
URL: /sellers/{email}/store/code
code e shop_id sempre null
```

**Causas**:
- Redirect_uri incorreta
- QueryString não está sendo capturada

**Solução**:
```javascript
// Verificar URL
console.log(window.location.search);
// Deve mostrar: ?code=ABC&shop_id=123

// Usar useSearchParams corretamente
import { useSearchParams } from 'react-router-dom';

const [searchParams] = useSearchParams();
const code = searchParams.get('code');
const shopId = searchParams.get('shop_id');

console.log('Code:', code);
console.log('ShopId:', shopId);

// Se null, verificar redirect_uri em GetAuthUrl
// Deve ser: https://frontend.com/sellers/{email}/store/code
```

---

### Problema 6: POST /shopee/auth retorna 400

**Sintoma**:
```
POST /api/shopee/webhook/auth
Response: 400 Bad Request
"Invalid request parameters"
```

**Causas**:
- Email vazio
- Code vazio
- ShopId inválido

**Solução**:
```bash
# Verificar request
curl -X POST "http://localhost:5000/api/shopee/webhook/auth" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "seller@example.com",
    "code": "SHOPEE_CODE",
    "shop_id": "226289035"
  }'

# Verificar cada campo:
# - email: string válida
# - code: não vazio
# - shop_id: número válido
```

---

### Problema 7: Seller criado mas não aparece em /me

**Sintoma**:
```
GET /me
seller_id continua null
```

**Causas**:
- Transação incompleta
- Cache não foi invalidado
- BD desincronizado

**Solução**:
```javascript
// 1. Limpar cache
localStorage.removeItem('user');
sessionStorage.clear();

// 2. Fazer novo GET /me
const user = await fetch('/api/me', {
  headers: { 'Authorization': `Bearer ${JWT}` }
}).then(r => r.json());

// 3. Se ainda null
// Verificar no DynamoDB
// Table: catalog-core
// PK: User#{cognito_id}
// SK: META
```

---

## ✅ Checklist de Debug

- [ ] JWT token válido (não expirado)
- [ ] Token contém claims corretos (cognito_id, email)
- [ ] Usuário existe no BD (User#meta)
- [ ] Role está definido e correto
- [ ] Seller_id preenchido (se já conectado)
- [ ] Shopee auth URL sendo gerada corretamente
- [ ] Sign HMAC válido (64 caracteres)
- [ ] Redirect URI correto em Shopee
- [ ] Code sendo capturado do callback
- [ ] POST /shopee/auth recebendo dados corretos
- [ ] Seller sendo criado no BD
- [ ] User sendo atualizado com seller_id
- [ ] GET /me retornando dados atualizados

---

## 📞 Logs Esperados

### Login com Sucesso
```
INFO: Login request initiated
INFO: Cognito callback received
INFO: User created with role=new-user
INFO: JWT generated
```

### Set Role com Sucesso
```
INFO: SetRole request - Role: seller
INFO: User#meta created
INFO: JWT updated with role
```

### Shopee Auth com Sucesso
```
INFO: Generating auth URL - PartnerId: 1203628, Email: seller@example.com
HMAC Input - PartnerId: 1203628, Path: /api/v2/shop/auth_partner, Timestamp: 1706901234, BaseString: 1203628/api/v2/shop/auth_partner1706901234
HMAC PartnerKey length: 32 bytes
HMAC Sign generated: abc123def456...
INFO: Auth URL generated successfully
```

### Shopee Callback com Sucesso
```
INFO: Shopee authentication request - Code: ABC123, ShopId: 226289035, Email: seller@example.com
INFO: Exchanging code for token
INFO: Seller created successfully
INFO: User updated with seller_id
INFO: Shop authenticated successfully
```

---

**Data**: February 5, 2026
**Versão**: 1.0
**Status**: ✅ Completo
