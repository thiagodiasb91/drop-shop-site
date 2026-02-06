# 🔑 Validar e Configurar SHOPEE_PARTNER_KEY

## 🚨 Principais Causas de Erro de Sign

A maioria dos erros de `sign` é causada por:

```
1️⃣ SHOPEE_PARTNER_KEY vazio
2️⃣ SHOPEE_PARTNER_KEY com valor placeholder ("seu-partner-key-aqui")
3️⃣ SHOPEE_PARTNER_KEY de outro servidor/ambiente
```

## 📝 O que é Partner Key?

- **Chave Secreta** para assinar requisições HMAC SHA256
- **Diferente** de Partner ID
- **Obtida** no Shopee Partner Center
- **Nunca deve** ser compartilhada ou commitada

## 📍 Onde Obter Partner Key

### 1. Acesse Shopee Partner Center
```
URL: https://partner.shopeemobile.com
```

### 2. Navegue para Credenciais
```
Partner Center → API Integration → Credenciais
```

### 3. Copie a Partner Key (não o Partner ID)

```
❌ Partner ID:   1203628              (não é a chave!)
✅ Partner Key:  abc123def456xyz789...  (é a chave!)
```

## ⚙️ Configurar em Seu Sistema

### Opção 1: Variável de Ambiente (Recomendado)

**No seu terminal:**
```bash
# Linux/Mac
export SHOPEE_PARTNER_KEY=abc123def456xyz789...

# Windows (PowerShell)
$env:SHOPEE_PARTNER_KEY="abc123def456xyz789..."
```

**Permanente:**
```bash
# Linux/Mac - adicionar ao ~/.bashrc ou ~/.zshrc
echo 'export SHOPEE_PARTNER_KEY=abc123def456xyz789...' >> ~/.zshrc
source ~/.zshrc
```

### Opção 2: Arquivo .env.local

```bash
# .env.local (não commitar!)
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=abc123def456xyz789...
```

### Opção 3: appsettings.json (Desenvolvimento)

```json
{
  "Shopee": {
    "PartnerId": "1203628",
    "PartnerKey": "abc123def456xyz789..."
  }
}
```

## ✅ Verificar Configuração

### Verificar se está Configurado

```bash
# Linux/Mac
echo $SHOPEE_PARTNER_KEY

# Windows (PowerShell)
$env:SHOPEE_PARTNER_KEY
```

**Resultado esperado:**
```
abc123def456xyz789...
```

**Resultado ERRADO:**
```
seu-partner-key-aqui    ❌ (placeholder)
                        ❌ (vazio)
```

### Verificar no Código C#

O código agora valida:

```csharp
if (string.IsNullOrWhiteSpace(_partnerKey))
{
    _logger.LogError("Partner Key is empty or null");
    throw new InvalidOperationException("Partner Key is required");
}
```

Você verá no log:
```
ERROR: Partner Key is empty or null
```

Se vir isso → configure a variável!

## 🔍 Debug do Partner Key

### 1. Adicionar Log de Debug

No seu projeto, pode adicionar:

```csharp
_logger.LogDebug("Partner Key (first 10 chars): {Key}...", 
    _partnerKey.Substring(0, Math.Min(10, _partnerKey.Length)));
_logger.LogDebug("Partner Key Length: {Length}", _partnerKey.Length);
```

### 2. Procurar no Log

Procure por:
```
HMAC PartnerKey length: 32 bytes
```

- Se mostrar `0 bytes` → está vazio!
- Se mostrar > 0 bytes → está configurado ✅

### 3. Validar Valor

```csharp
// Não deve ser placeholder
if (_partnerKey.Contains("seu-") || _partnerKey.Contains("aqui"))
{
    throw new InvalidOperationException("Partner Key is still a placeholder!");
}
```

## 📊 Formato Esperado

### Típico Partner Key

```
Comprimento: 20-50 caracteres
Caracteres: a-z, A-Z, 0-9, especiais

Exemplo válido:
abc123def456xyz789...

Exemplo inválido (placeholder):
seu-partner-key-aqui
```

## 🧪 Testar HMAC com Partner Key Correta

```csharp
var partnerId = "1203628";
var path = "/api/v2/shop/auth_partner";
var timestamp = 1706901234L;
var partnerKey = "abc123def456..."; // ← COLOQUE AQUI seu valor real

var baseString = $"{partnerId}{path}{timestamp}";
var baseBytes = Encoding.UTF8.GetBytes(baseString);
var keyBytes = Encoding.UTF8.GetBytes(partnerKey);

using (var hmac = new HMACSHA256(keyBytes))
{
    var hashBytes = hmac.ComputeHash(baseBytes);
    var sign = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    Console.WriteLine($"Sign: {sign}");
    Console.WriteLine($"Length: {sign.Length}"); // Deve ser 64
}
```

## 🚀 Passo a Passo para Resolver

### 1. Copiar Partner Key do Shopee
```
Shopee Partner Center
  → API Integration
    → Credenciais
      → Copiar "Partner Key"
```

### 2. Configurar no .env.local
```bash
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=abc123def456... # ← Cole aqui!
```

### 3. NÃO commitar .env.local
```bash
echo ".env.local" >> .gitignore
git add .gitignore
git commit -m "[SECURITY] Add .env.local to gitignore"
```

### 4. Recompilar
```bash
dotnet build
dotnet run
```

### 5. Testar
```bash
# Gerar URL
curl -X GET "http://localhost:5000/shopee/webhook/auth-url?email=test@example.com"

# Copiar URL e testar
curl -v "https://partner.test-stable.shopeemobile.com/api/v2/shop/auth_partner?..."
```

## ⚠️ Segurança

**NUNCA:**
- ❌ Commitar Partner Key em git
- ❌ Compartilhar Partner Key
- ❌ Colocar em código-fonte
- ❌ Expor em logs públicos

**SEMPRE:**
- ✅ Usar variáveis de ambiente
- ✅ Usar .env.local (não versionado)
- ✅ Manter em AWS Secrets Manager (produção)
- ✅ Rotacionar periodicamente

## ✅ Checklist Final

- [ ] Obtive Partner Key no Shopee Partner Center
- [ ] Partner Key não é placeholder ("seu-partner-key-aqui")
- [ ] Partner Key está configurado em SHOPEE_PARTNER_KEY
- [ ] Recompilei o projeto (dotnet build)
- [ ] Verifiquei no log: "HMAC PartnerKey length: X bytes" (X > 0)
- [ ] Testei a URL no navegador ou curl
- [ ] Shopee agora aceita o sign ✅

---

**Data**: February 5, 2026
**Versão**: 1.0
