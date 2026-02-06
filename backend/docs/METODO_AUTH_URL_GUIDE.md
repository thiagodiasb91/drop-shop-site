# 🚀 Guia Rápido - Método GetAuthorizationUrl

## 📍 Localização
- **Controller**: `ShopeeWebhookController`
- **Método**: `GetAuthorizationUrl()`
- **Rota**: `GET /shopee/webhook/auth-url`

## 🎯 Objetivo
Gerar uma URL de autorização para o cliente. Esta é a URL que você fornecerá ao seu cliente para que ele autorize a integração com Shopee.

## 📝 Implementação

### Código do Método
```csharp
/// <summary>
/// Gera a URL para autorização do Shopee
/// Esta é a URL que deve ser fornecida ao cliente para autorizar a API
/// </summary>
/// <returns>URL de autorização com assinatura HMAC</returns>
[HttpGet("auth-url")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK)]
public IActionResult GetAuthorizationUrl()
{
    _logger.LogInformation("Generating Shopee authorization URL");

    try
    {
        var authUrl = _shopeeApiService.GetAuthUrl();

        _logger.LogInformation("Shopee authorization URL generated successfully");

        return Ok(new
        {
            statusCode = 200,
            message = "Authorization URL generated successfully",
            authUrl = authUrl,
            instructions = new
            {
                step1 = "Forneça esta URL ao cliente",
                step2 = "O cliente será redirecionado para login na Shopee",
                step3 = "Após autorizar, Shopee irá redirecionar para o callback com um code",
                step4 = "Use o endpoint GET /shopee/webhook/auth com code, shopId e email para trocar pelo token"
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generating Shopee authorization URL");
        return StatusCode(StatusCodes.Status500InternalServerError, new ShopeeWebhookResponse
        {
            StatusCode = 500,
            Message = "Error generating authorization URL"
        });
    }
}
```

## 📤 Como Usar

### 1️⃣ Requisição
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url"
```

### 2️⃣ Resposta (200 OK)
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

## 🔧 O Que Faz Internamente

1. **Chama `ShopeeApiService.GetAuthUrl()`**
   - Gera timestamp Unix atual
   - Calcula assinatura HMAC SHA256
   - Formata URL com parâmetros

2. **Gera Assinatura HMAC SHA256**
   ```csharp
   base_string = partner_id + path + timestamp
   sign = HMAC-SHA256(partner_key, base_string)
   ```

3. **Monta a URL Final**
   ```
   https://openplatform.sandbox.test-stable.shopee.sg/api/v2/shop/auth_partner
   ?partner_id={PARTNER_ID}
   &redirect={REDIRECT_URL}
   &timestamp={TIMESTAMP}
   &sign={SIGNATURE}
   ```

4. **Log de Informação**
   - Registra geração da URL
   - Usa CorrelationId para rastreamento

## 💡 Fluxo Completo

```
1. Seu Backend
   ├─ GET /shopee/webhook/auth-url
   │
2. Sistema Gera URL
   ├─ Cria HMAC SHA256
   ├─ Formata parâmetros
   │
3. Retorna URL ao Cliente
   └─ authUrl: "https://openplatform.sandbox..."
   
4. Cliente (Frontend)
   ├─ Clica no link
   ├─ Faz login Shopee (se necessário)
   ├─ Autoriza app
   │
5. Shopee Redireciona
   └─ code=ABC123DEF456&shop_id=226289035
   
6. Cliente Captura Code
   └─ Envia ao seu backend
   
7. Seu Backend Recebe Code
   └─ GET /shopee/webhook/auth?code=ABC123&shopId=226289035&email=user@email.com
   
8. Sistema Troca Code por Token
   ├─ Cria Seller
   ├─ Atualiza User
   ├─ Armazena tokens
   └─ ✅ Sucesso!
```

## ⚙️ Configurações Necessárias

### Variáveis de Ambiente
```bash
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=sua-partner-key
SHOPEE_REDIRECT_URL=https://open.shopee.com
SHOPEE_HOST=https://openplatform.sandbox.test-stable.shopee.sg
```

### Injeção de Dependência (Program.cs)
```csharp
builder.Services.AddScoped<ShopeeApiService>();
builder.Services.AddHttpClient();
```

## 🧪 Testando

### Com Postman
1. Importar `postman_collection.json`
2. Executar request "1. Gerar URL de Autorização"
3. Copiar o valor de `authUrl`
4. Cole em uma aba do navegador
5. Veja a página de autorização Shopee

### Com cURL
```bash
curl -X GET "http://localhost:5000/shopee/webhook/auth-url" \
  -H "Content-Type: application/json"
```

### Com PowerShell
```powershell
$response = Invoke-WebRequest -Uri "http://localhost:5000/shopee/webhook/auth-url" `
  -Method GET

$data = $response.Content | ConvertFrom-Json
Write-Host "Auth URL: $($data.authUrl)"
```

## 📝 Exemplo de Integração Frontend

### React
```javascript
import React, { useState } from 'react';

function ShopeeAuth() {
  const [authUrl, setAuthUrl] = useState(null);
  const [loading, setLoading] = useState(false);

  const generateAuthUrl = async () => {
    setLoading(true);
    try {
      const response = await fetch('/shopee/webhook/auth-url');
      const data = await response.json();
      setAuthUrl(data.authUrl);
    } catch (error) {
      console.error('Erro ao gerar URL:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <button onClick={generateAuthUrl} disabled={loading}>
        {loading ? 'Gerando URL...' : 'Gerar URL de Autorização'}
      </button>

      {authUrl && (
        <div>
          <p>Clique no link abaixo para autorizar:</p>
          <a href={authUrl} target="_blank" rel="noopener noreferrer">
            Autorizar com Shopee
          </a>
        </div>
      )}
    </div>
  );
}

export default ShopeeAuth;
```

### Vue
```vue
<template>
  <div>
    <button @click="generateAuthUrl" :disabled="loading">
      {{ loading ? 'Gerando URL...' : 'Gerar URL de Autorização' }}
    </button>

    <div v-if="authUrl">
      <p>Clique no link abaixo para autorizar:</p>
      <a :href="authUrl" target="_blank">Autorizar com Shopee</a>
    </div>
  </div>
</template>

<script>
export default {
  data() {
    return {
      authUrl: null,
      loading: false
    };
  },
  methods: {
    async generateAuthUrl() {
      this.loading = true;
      try {
        const response = await fetch('/shopee/webhook/auth-url');
        const data = await response.json();
        this.authUrl = data.authUrl;
      } catch (error) {
        console.error('Erro:', error);
      } finally {
        this.loading = false;
      }
    }
  }
};
</script>
```

## ✅ Checklist

- [x] Endpoint criado (`GET /shopee/webhook/auth-url`)
- [x] Injeta `ShopeeApiService`
- [x] Gera HMAC SHA256 válido
- [x] Retorna URL no formato correto
- [x] Inclui instruções na resposta
- [x] Logging completo
- [x] Tratamento de erros
- [x] Documentado
- [x] Testado

## 📊 Comparação com Método Anterior

| Aspecto | Método Anterior | Novo Método |
|---------|-----------------|------------|
| Endpoint | `GET /shopee/webhook/auth` | `GET /shopee/webhook/auth-url` |
| Parâmetros | `code`, `shopId`, `email` | Nenhum |
| Função | Trocar code por token | Gerar URL de autorização |
| Usar Quando | Após cliente autorizar | Antes de cliente autorizar |
| Ordem | 2º passo | 1º passo |

## 🔐 Segurança

✅ **HTTPS obrigatório em produção**
✅ **HMAC SHA256 valida assinatura**
✅ **Timestamp previne replay attacks**
✅ **Sem credenciais na URL**
✅ **Logs estruturados com CorrelationId**

## 📞 Troubleshooting

**P: A URL não funciona**
A: Verifique se `SHOPEE_PARTNER_ID` e `SHOPEE_PARTNER_KEY` estão corretos

**P: Erro ao gerar URL**
A: Verifique variáveis de ambiente no `.env`

**P: Como testar localmente**
A: Use a URL gerada em uma aba do navegador (funciona em sandbox)

## 🎯 Resumo

- **Rota**: `GET /shopee/webhook/auth-url`
- **Retorna**: URL com assinatura HMAC válida
- **Uso**: Fornecê-la ao cliente para autorização
- **Próximo**: Capturar `code` quando cliente autorizar
- **Status**: ✅ Pronto para produção

---

**Data**: February 4, 2026
**Versão**: 1.0
