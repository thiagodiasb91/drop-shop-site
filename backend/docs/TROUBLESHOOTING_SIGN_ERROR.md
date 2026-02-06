# 🔧 Troubleshooting: Erro de Sign (HMAC SHA256)

## 🚨 Problema

Shopee está retornando erro de `sign` na URL de autorização.

## 🔍 Causas Possíveis

### 1. Partner Key Vazio ou Incorreto
**Sintoma**: Sign é calculado mas Shopee rejeita
**Causa**: `SHOPEE_PARTNER_KEY` não configurado corretamente

**Solução**:
```bash
# Verificar se a variável está configurada
echo $SHOPEE_PARTNER_KEY

# Deve ter um valor real (não vazio e não "seu-partner-key-aqui")
```

### 2. Base String Incorreta
**Sintoma**: Sign diferente a cada execução (ou sempre o mesmo)
**Causa**: Ordem dos parâmetros ou formatação incorreta

**Base String Correta**:
```
{partner_id}{path}{timestamp}

Exemplo:
1203628/api/v2/shop/auth_partner1706901234
```

### 3. Encoding Incorreto
**Sintoma**: Sign gerado mas não bate com o esperado
**Causa**: UTF-8 vs outro encoding

**Verificar**:
- ✅ Partner Key: UTF-8
- ✅ Base String: UTF-8
- ✅ Output: hexadecimal lowercase

### 4. Timestamp Desincronizado
**Sintoma**: Sign válido mas Shopee rejeita como "expirado"
**Causa**: Relógio do servidor desincronizado

**Solução**:
```bash
# Sincronizar relógio
sudo ntpdate -s time.nist.gov

# Ou no Windows
w32tm /resync
```

## 📋 Checklist de Verificação

### 1. Validar Variáveis de Ambiente

```bash
# Verificar PARTNER ID
echo "Partner ID: $SHOPEE_PARTNER_ID"
# Deve retornar: Partner ID: 1203628

# Verificar PARTNER KEY
echo "Partner Key: ${SHOPEE_PARTNER_KEY:0:10}..." 
# Deve retornar os primeiros 10 caracteres (não vazio!)

# Verificar se as variáveis existem
printenv | grep SHOPEE
```

### 2. Validar Base String

Execute uma requisição e procure nos logs:

```
HMAC Input - PartnerId: 1203628, Path: /api/v2/shop/auth_partner, Timestamp: 1706901234, BaseString: 1203628/api/v2/shop/auth_partner1706901234
```

✅ Base String deve estar no formato: `{partnerId}{path}{timestamp}`

### 3. Validar Partner Key

Nos logs você verá:
```
HMAC PartnerKey length: 32 bytes
```

✅ Key length deve ser > 0 (não vazio)

### 4. Validar Sign Gerado

Nos logs:
```
HMAC Sign generated: abc123def456...
```

✅ Sign deve ser 64 caracteres (SHA256 em hexadecimal)

### 5. Validar URL Final

```
https://partner.test-stable.shopeemobile.com/api/v2/shop/auth_partner?
partner_id=1203628&
redirect=https%3A%2F%2Finv6sa4cb0.execute-api.us-east-1.amazonaws.com%2F...&
timestamp=1706901234&
sign=abc123def456...
```

✅ Verifique:
- Host correto
- Path correto
- Parâmetros na ordem
- Sign com 64 caracteres

## 🧪 Teste o HMAC Localmente

### C# Test

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        // Valores de teste (mude para seus valores reais)
        var partnerId = "1203628";
        var path = "/api/v2/shop/auth_partner";
        var timestamp = 1706901234L;
        var partnerKey = "seu-partner-key-real";
        
        // Base string
        var baseString = $"{partnerId}{path}{timestamp}";
        Console.WriteLine($"Base String: {baseString}");
        
        // HMAC SHA256
        var baseBytes = Encoding.UTF8.GetBytes(baseString);
        var keyBytes = Encoding.UTF8.GetBytes(partnerKey);
        
        using (var hmac = new HMACSHA256(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(baseBytes);
            var sign = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            Console.WriteLine($"Sign: {sign}");
            Console.WriteLine($"Sign Length: {sign.Length}");
        }
    }
}
```

### Python Test (para comparar)

```python
import hmac
import hashlib

partner_id = "1203628"
path = "/api/v2/shop/auth_partner"
timestamp = 1706901234
partner_key = "seu-partner-key-real"

base_string = f"{partner_id}{path}{timestamp}"
print(f"Base String: {base_string}")

sign = hmac.new(
    partner_key.encode(),
    base_string.encode(),
    hashlib.sha256
).hexdigest()

print(f"Sign: {sign}")
print(f"Sign Length: {len(sign)}")
```

## 📝 Passos para Resolver

### 1. Verificar Configuração
```bash
# Abrir .env.local ou variáveis de ambiente
# Garantir que SHOPEE_PARTNER_KEY tem um valor real

# Exemplo correto:
# SHOPEE_PARTNER_KEY=abc123def456xyz789...

# Exemplo INCORRETO:
# SHOPEE_PARTNER_KEY=seu-partner-key-aqui  ❌
# SHOPEE_PARTNER_KEY=                       ❌
```

### 2. Obter Partner Key Correta
- Acesse: https://partner.shopeemobile.com
- Vá para Developer Center
- Copie a Partner Key (não o Partner ID!)
- Coloque em `SHOPEE_PARTNER_KEY`

### 3. Recompilar e Testar
```bash
dotnet build
dotnet run
```

### 4. Verificar Logs
Procure por:
```
HMAC Input - PartnerId: ..., BaseString: ...
HMAC PartnerKey length: ...
HMAC Sign generated: ...
```

### 5. Testar URL
Copie a URL gerada do log e teste em navegador ou curl:
```bash
curl -v "https://partner.test-stable.shopeemobile.com/api/v2/shop/auth_partner?partner_id=1203628&redirect=...&timestamp=...&sign=..."
```

Se retornar status 200 ou redirecionar → Sign está correto! ✅

## 🔐 Validação HMAC Online

Para validar se o HMAC está correto:

1. Acesse: https://www.freeformatter.com/hmac-generator.html
2. Selecione: SHA256
3. Preencha:
   - **Secret Key**: seu SHOPEE_PARTNER_KEY
   - **Data**: sua Base String (ex: 1203628/api/v2/shop/auth_partner1706901234)
4. Compare o resultado com o `sign` gerado

## ✅ Checklist Final

- [ ] SHOPEE_PARTNER_KEY está configurado (não vazio)
- [ ] Partner Key contém o valor real (não "seu-partner-key-aqui")
- [ ] Base String está no formato: `{pid}{path}{ts}`
- [ ] HMAC está usando SHA256
- [ ] Sign tem 64 caracteres
- [ ] Relógio do servidor está sincronizado
- [ ] URL tem parâmetros na ordem: partner_id, redirect, timestamp, sign
- [ ] Testou URL em navegador ou curl

## 🚨 Se o Problema Persistir

1. Verifique logs detalhados (LogLevel: Debug)
2. Compare base string com código Python
3. Valide partner key no Shopee Developer Center
4. Sincronize relógio do servidor
5. Teste HMAC em ferramenta online (https://www.freeformatter.com/hmac-generator.html)

---

**Data**: February 5, 2026
**Versão**: 1.0
