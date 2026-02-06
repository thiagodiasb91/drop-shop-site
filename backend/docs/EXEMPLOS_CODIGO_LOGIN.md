# 💻 Exemplos de Código - Fluxo de Login

## Frontend - Implementação React

### 1. AuthContext - Gerenciar Sessão

```typescript
// src/context/AuthContext.tsx
import React, { createContext, useContext, useState, useEffect } from 'react';

interface User {
  cognito_id: string;
  email: string;
  role: 'new-user' | 'admin' | 'seller' | 'vendor';
  seller_id?: string;
  shop_id?: string;
  access_token?: string;
}

interface AuthContextType {
  user: User | null;
  jwtToken: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  setRole: (role: string) => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [jwtToken, setJwtToken] = useState<string | null>(
    localStorage.getItem('jwt_token')
  );

  useEffect(() => {
    // Verificar se já tem token salvo
    const token = localStorage.getItem('jwt_token');
    if (token) {
      setJwtToken(token);
      refreshUser();
    }
  }, []);

  const login = async (email: string, password: string) => {
    // Redirecionar para /auth/login (Cognito)
    window.location.href = '/auth/login';
  };

  const logout = () => {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user');
    setJwtToken(null);
    setUser(null);
  };

  const setRole = async (role: string) => {
    const response = await fetch('/api/users/set-role', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${jwtToken}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ role })
    });

    if (response.ok) {
      const data = await response.json();
      setJwtToken(data.jwt_token);
      localStorage.setItem('jwt_token', data.jwt_token);
      await refreshUser();
    }
  };

  const refreshUser = async () => {
    if (!jwtToken) return;

    const response = await fetch('/api/me', {
      headers: { 'Authorization': `Bearer ${jwtToken}` }
    });

    if (response.ok) {
      const data = await response.json();
      setUser(data);
      localStorage.setItem('user', JSON.stringify(data));
    }
  };

  return (
    <AuthContext.Provider value={{ user, jwtToken, login, logout, setRole, refreshUser }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
```

### 2. Página Login

```typescript
// src/pages/Login.tsx
import React from 'react';
import { useNavigate } from 'react-router-dom';

export function LoginPage() {
  const navigate = useNavigate();

  const handleLogin = () => {
    // Redirecionar para Cognito via backend
    window.location.href = '/api/auth/login';
  };

  return (
    <div className="login-container">
      <h1>Login</h1>
      <button onClick={handleLogin} className="btn-primary">
        Fazer Login com Cognito
      </button>
    </div>
  );
}
```

### 3. Callback do Cognito

```typescript
// src/pages/AuthCallback.tsx
import React, { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function AuthCallbackPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { refreshUser, setRole } = useAuth();

  useEffect(() => {
    const processCallback = async () => {
      const token = searchParams.get('token');
      const role = searchParams.get('role');

      if (token) {
        // Salvar token
        localStorage.setItem('jwt_token', token);

        // Atualizar user
        await refreshUser();

        // Se novo usuário, redirecionar para seleção de role
        if (role === 'new-user') {
          navigate('/select-role');
        } else if (role === 'seller') {
          navigate('/dashboard');
        }
      }
    };

    processCallback();
  }, [searchParams]);

  return (
    <div>
      <h1>Autenticando...</h1>
      <p>Por favor, aguarde.</p>
    </div>
  );
}
```

### 4. Seleção de Role

```typescript
// src/pages/SelectRole.tsx
import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function SelectRolePage() {
  const navigate = useNavigate();
  const { setRole } = useAuth();
  const [loading, setLoading] = useState(false);

  const handleSelectRole = async (role: string) => {
    setLoading(true);
    try {
      await setRole(role);

      if (role === 'seller') {
        navigate('/seller-setup');
      } else if (role === 'admin') {
        navigate('/admin-dashboard');
      }
    } catch (error) {
      console.error('Erro ao definir role:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="select-role-container">
      <h1>Defina seu tipo de conta</h1>
      <p>Você ainda não tem acesso definido</p>

      <button
        onClick={() => handleSelectRole('seller')}
        disabled={loading}
        className="btn-role"
      >
        Sou um Seller (Fornecedor)
      </button>

      <button
        onClick={() => handleSelectRole('admin')}
        disabled={loading}
        className="btn-role"
      >
        Sou um Admin
      </button>

      <button
        onClick={() => handleSelectRole('vendor')}
        disabled={loading}
        className="btn-role"
      >
        Sou um Vendor (Vendedor)
      </button>
    </div>
  );
}
```

### 5. Setup da Loja Shopee

```typescript
// src/pages/StoreSetup.tsx
import React, { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function StoreSetupPage() {
  const { sellerId } = useParams();
  const navigate = useNavigate();
  const { jwtToken } = useAuth();
  const [loading, setLoading] = useState(false);

  const handleConnectShopee = async () => {
    setLoading(true);
    try {
      // Gerar URL de autorização
      const response = await fetch(
        `/api/shopee/webhook/auth-url?email=${encodeURIComponent(
          localStorage.getItem('user')
            ? JSON.parse(localStorage.getItem('user')!).email
            : ''
        )}`,
        {
          headers: { 'Authorization': `Bearer ${jwtToken}` }
        }
      );

      if (response.ok) {
        const { authUrl } = await response.json();
        // Redirecionar para Shopee
        window.location.href = authUrl;
      }
    } catch (error) {
      console.error('Erro ao gerar URL Shopee:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="store-setup-container">
      <h1>Configurar Loja Shopee</h1>
      <p>Conecte sua loja Shopee para começar</p>

      <button
        onClick={handleConnectShopee}
        disabled={loading}
        className="btn-primary"
      >
        {loading ? 'Carregando...' : 'Conectar com Shopee'}
      </button>
    </div>
  );
}
```

### 6. Callback Shopee

```typescript
// src/pages/ShopeeCallback.tsx
import React, { useEffect } from 'react';
import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function ShopeeCallbackPage() {
  const { email } = useParams();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { jwtToken, refreshUser } = useAuth();

  useEffect(() => {
    const processCallback = async () => {
      const code = searchParams.get('code');
      const shopId = searchParams.get('shop_id');

      if (!code || !shopId || !email) {
        console.error('Missing parameters');
        return;
      }

      try {
        // Chamar backend para salvar code e shop_id
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
          // Atualizar user
          await refreshUser();

          // Redirecionar para home
          navigate('/home');
        } else {
          console.error('Erro ao autenticar Shopee');
        }
      } catch (error) {
        console.error('Erro:', error);
      }
    };

    processCallback();
  }, [searchParams, email, jwtToken]);

  return (
    <div>
      <h1>Conectando com Shopee...</h1>
      <p>Por favor, aguarde.</p>
    </div>
  );
}
```

---

## Backend - Implementação C# (Controllers e Services)

### 1. AuthController

```csharp
// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dropship.Services;

namespace Dropship.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Iniciar login com Cognito
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        _logger.LogInformation("Login request initiated");
        var loginUrl = _authService.GetCognitoLoginUrl();
        return Redirect(loginUrl);
    }

    /// <summary>
    /// Callback do Cognito
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        _logger.LogInformation("Cognito callback received");

        try
        {
            // Validar state
            if (!_authService.ValidateState(state))
            {
                _logger.LogWarning("Invalid state token");
                return BadRequest("Invalid state");
            }

            // Trocar code por tokens
            var tokens = await _authService.ExchangeCodeForTokenAsync(code);

            // Extrair informações
            var cognitoId = tokens.CognitoId;
            var email = tokens.Email;

            // Criar ou obter usuário
            var user = await _authService.GetOrCreateUserAsync(cognitoId, email);

            // Gerar JWT
            var jwtToken = _authService.GenerateJWT(user);

            // Redirecionar para frontend com token
            var callbackUrl = $"https://frontend.com/auth/callback?token={jwtToken}&role={user.Role}&email={email}&cognito_id={cognitoId}";

            return Redirect(callbackUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in callback");
            return StatusCode(500, "Internal server error");
        }
    }
}
```

### 2. UserController

```csharp
// Controllers/UserController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dropship.Services;
using Dropship.Requests;

namespace Dropship.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Obter dados do usuário autenticado
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        _logger.LogInformation("GetMe request");

        try
        {
            var cognitoId = User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(cognitoId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByCognitoIdAsync(cognitoId);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                cognito_id = user.CognitoId,
                email = user.Email,
                role = user.Role,
                seller_id = user.SellerId,
                shop_id = user.ShopId,
                access_token = user.AccessToken,
                created_at = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Definir role do usuário
    /// </summary>
    [HttpPost("set-role")]
    public async Task<IActionResult> SetRole([FromBody] SetRoleRequest request)
    {
        _logger.LogInformation("SetRole request - Role: {Role}", request.Role);

        try
        {
            var cognitoId = User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(cognitoId))
            {
                return Unauthorized();
            }

            // Validar role
            if (!new[] { "seller", "admin", "vendor" }.Contains(request.Role))
            {
                return BadRequest("Invalid role");
            }

            // Atualizar role
            var user = await _userService.SetUserRoleAsync(cognitoId, request.Role);

            // Gerar novo JWT
            var jwtToken = _userService.GenerateJWT(user);

            return Ok(new
            {
                status = "success",
                message = "Role definido com sucesso",
                jwt_token = jwtToken,
                role = user.Role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting role");
            return StatusCode(500, "Internal server error");
        }
    }
}
```

### 3. Request Classes

```csharp
// Requests/SetRoleRequest.cs
namespace Dropship.Requests;

public class SetRoleRequest
{
    public string Role { get; set; }
}

// Requests/ShopeeAuthRequest.cs
public class ShopeeAuthRequest
{
    public string Email { get; set; }
    public string Code { get; set; }
    public string ShopId { get; set; }
}
```

### 4. Service Interface

```csharp
// Services/IUserService.cs
namespace Dropship.Services;

public interface IUserService
{
    Task<UserDto> GetUserByCognitoIdAsync(string cognitoId);
    Task<UserDto> SetUserRoleAsync(string cognitoId, string role);
    string GenerateJWT(UserDto user);
}

public class UserDto
{
    public string CognitoId { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public string SellerId { get; set; }
    public string ShopId { get; set; }
    public string AccessToken { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 🧪 Testes API - Exemplos com cURL

### 1. Teste Login

```bash
# 1. Iniciar login
curl -X GET "http://localhost:5000/api/auth/login"
# Redireciona para Cognito

# 2. Após login, Cognito faz callback
# (Simulado com token válido)
curl -X GET "http://localhost:5000/api/auth/callback?code=AUTH_CODE&state=STATE_TOKEN"
```

### 2. Teste Set Role

```bash
# Definir role como seller
curl -X POST "http://localhost:5000/api/users/set-role" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "role": "seller"
  }'

# Resposta:
# {
#   "status": "success",
#   "message": "Role definido com sucesso",
#   "jwt_token": "NEW_JWT",
#   "role": "seller"
# }
```

### 3. Teste Get Me

```bash
curl -X GET "http://localhost:5000/api/users/me" \
  -H "Authorization: Bearer JWT_TOKEN"

# Resposta:
# {
#   "cognito_id": "12345678",
#   "email": "seller@example.com",
#   "role": "seller",
#   "seller_id": null,
#   "shop_id": null,
#   "created_at": "2026-02-05T10:30:00Z"
# }
```

### 4. Teste Shopee Auth URL

```bash
curl -X GET "http://localhost:5000/api/shopee/webhook/auth-url?email=seller@example.com" \
  -H "Authorization: Bearer JWT_TOKEN"

# Resposta:
# {
#   "authUrl": "https://partner.test-stable.shopeemobile.com/api/v2/shop/auth_partner?partner_id=...&redirect=...&timestamp=...&sign=..."
# }
```

### 5. Teste Shopee Callback

```bash
curl -X POST "http://localhost:5000/api/shopee/webhook/auth" \
  -H "Authorization: Bearer JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "seller@example.com",
    "code": "SHOPEE_AUTH_CODE",
    "shop_id": "226289035"
  }'

# Resposta:
# {
#   "status": "success",
#   "message": "Loja conectada com sucesso",
#   "seller_id": "94d52c7b-8f45-4a07-b2b2-65ca9a18537b",
#   "shop_id": "226289035"
# }
```

---

## 📱 Postman Collection

Salvar como `postman_login_flow.json`:

```json
{
  "info": {
    "name": "Login Flow - Dropship",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "1. Login",
      "request": {
        "method": "GET",
        "url": "{{base_url}}/api/auth/login"
      }
    },
    {
      "name": "2. Set Role - Seller",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": "{{base_url}}/api/users/set-role",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"role\": \"seller\"\n}"
        }
      }
    },
    {
      "name": "3. Get Me",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": "{{base_url}}/api/users/me"
      }
    },
    {
      "name": "4. Shopee Auth URL",
      "request": {
        "method": "GET",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          }
        ],
        "url": "{{base_url}}/api/shopee/webhook/auth-url?email=seller@example.com"
      }
    },
    {
      "name": "5. Shopee Callback",
      "request": {
        "method": "POST",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{jwt_token}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": "{{base_url}}/api/shopee/webhook/auth",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"email\": \"seller@example.com\",\n  \"code\": \"SHOPEE_CODE\",\n  \"shop_id\": \"226289035\"\n}"
        }
      }
    }
  ]
}
```

---

**Data**: February 5, 2026
**Versão**: 1.0
**Status**: ✅ Completo
