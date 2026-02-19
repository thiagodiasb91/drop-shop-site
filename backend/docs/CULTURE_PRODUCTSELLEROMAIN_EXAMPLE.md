# Exemplo Prático - ProductSellerDomain com Cultura en-US

## 📝 Antes e Depois da Configuração

### ❌ ANTES (com cultura PT-BR)

```csharp
// ProductSellerDomain.cs - Parsing falharia
Price = item.ContainsKey("price") && 
    decimal.TryParse(item["price"].N, out var price)  // ❌ FALHA!
    ? price 
    : 0;

// ❌ Problema:
// - DynamoDB retorna: "79.9" (com ponto)
// - Cultura PT-BR espera: "79,9" (com virgula)
// - Resultado: TryParse retorna false, Price fica 0
```

### ✅ DEPOIS (com cultura en-US)

```csharp
// ProductSellerDomain.cs - Parsing funciona!
Price = item.ContainsKey("price") && 
    decimal.TryParse(item["price"].N, out var price)  // ✅ SUCESSO!
    ? price 
    : 0;

// ✅ Agora:
// - DynamoDB retorna: "79.9" (com ponto)
// - Cultura en-US espera: "79.9" (com ponto) ✅ MATCH!
// - Resultado: TryParse retorna true, Price = 79.9
```

---

## 🔄 Fluxo Completo de Dados

```
┌─────────────────────────────────────────────────────┐
│         1. DynamoDB Armazena Valor                  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Item no DynamoDB:                                  │
│  {                                                  │
│    "price": {                                       │
│      "N": "79.9"  ← Sempre com ponto!              │
│    }                                                │
│  }                                                  │
│                                                     │
└─────────────────────────────────────────────────────┘
            ↓
┌─────────────────────────────────────────────────────┐
│  2. ProductSellerMapper.ToDomain() Faz Parse        │
├─────────────────────────────────────────────────────┤
│                                                     │
│  decimal.TryParse(item["price"].N, out var price)  │
│  ✅ Com en-US culture:                             │
│     - Input: "79.9"                                │
│     - Esperado: "79.9" (com ponto)                │
│     - Resultado: ✅ SUCESSO! price = 79.9          │
│                                                     │
│  ❌ Com PT-BR culture (antes):                     │
│     - Input: "79.9"                                │
│     - Esperado: "79,9" (com virgula)              │
│     - Resultado: ❌ FALHA! price = 0               │
│                                                     │
└─────────────────────────────────────────────────────┘
            ↓
┌─────────────────────────────────────────────────────┐
│  3. ProductSellerDomain Armazena Valor              │
├─────────────────────────────────────────────────────┤
│                                                     │
│  public decimal Price { get; set; }                │
│  Price = 79.9m  ← ✅ Valor correto!                │
│                                                     │
└─────────────────────────────────────────────────────┘
            ↓
┌─────────────────────────────────────────────────────┐
│  4. JSON Serialization para Response                │
├─────────────────────────────────────────────────────┤
│                                                     │
│  JsonSerializer.Serialize(productSeller)           │
│  ✅ Resultado JSON:                                │
│  {                                                  │
│    "price": 79.9  ← Ponto como separador!          │
│  }                                                  │
│                                                     │
└─────────────────────────────────────────────────────┘
            ↓
┌─────────────────────────────────────────────────────┐
│  5. Cliente HTTP/API Recebe                         │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Response JSON:                                     │
│  {                                                  │
│    "price": 79.9  ← Padrão internacional!         │
│  }                                                  │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 🧪 Teste Prático

### Código de Teste
```csharp
[HttpGet("test-culture")]
public IActionResult TestCulture()
{
    // 1. Simular valor do DynamoDB
    var dynamoDbValue = "79.9";
    
    // 2. Tentar fazer parse
    var parseSuccess = decimal.TryParse(dynamoDbValue, out var price);
    
    // 3. Criar objeto domain
    var seller = new ProductSellerDomain
    {
        Price = parseSuccess ? price : 0
    };
    
    // 4. Serializar para JSON
    var json = System.Text.Json.JsonSerializer.Serialize(seller);
    
    return Ok(new
    {
        culture = System.Globalization.CultureInfo.CurrentCulture.Name,
        decimalSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
        dynamoDbValue = dynamoDbValue,
        parseSuccess = parseSuccess,
        parsedValue = price,
        domainPrice = seller.Price,
        jsonOutput = json
    });
}
```

### ✅ Resposta Esperada (com en-US)
```json
{
  "culture": "en-US",
  "decimalSeparator": ".",
  "dynamoDbValue": "79.9",
  "parseSuccess": true,
  "parsedValue": 79.9,
  "domainPrice": 79.9,
  "jsonOutput": "{\"price\":79.9,\"...\":\"...\"}"
}
```

### ❌ Resposta Anterior (com PT-BR)
```json
{
  "culture": "pt-BR",
  "decimalSeparator": ",",
  "dynamoDbValue": "79.9",
  "parseSuccess": false,
  "parsedValue": 0,
  "domainPrice": 0,
  "jsonOutput": "{\"price\":0,\"...\":\"...\"}"
}
```

---

## 💾 Exemplo Real com DynamoDB

### Item no DynamoDB
```json
{
  "PK": { "S": "Product#12345" },
  "SK": { "S": "Seller#seller-1" },
  "entity_type": { "S": "product_seller" },
  "product_id": { "S": "12345" },
  "seller_id": { "S": "seller-1" },
  "price": { "N": "79.9" },
  "sku_count": { "N": "5" },
  "created_at": { "S": "2026-02-19T10:30:00Z" }
}
```

### Mapping com Culture en-US
```csharp
public static ProductSellerDomain ToDomain(this Dictionary<string, AttributeValue> item)
{
    return new ProductSellerDomain
    {
        Pk = item.ContainsKey("PK") ? item["PK"].S : "",
        Sk = item.ContainsKey("SK") ? item["SK"].S : "",
        EntityType = item.ContainsKey("entity_type") ? item["entity_type"].S : "",
        ProductId = item.ContainsKey("product_id") ? item["product_id"].S : "",
        SellerId = item.ContainsKey("seller_id") ? item["seller_id"].S : "",
        
        // ✅ COM EN-US: Funciona corretamente!
        Price = item.ContainsKey("price") && 
                decimal.TryParse(item["price"].N, out var price)  // ✅ "79.9" é parseado!
                ? price 
                : 0,
        
        SkuCount = item.ContainsKey("sku_count") && 
                   int.TryParse(item["sku_count"].N, out var count)
                   ? count 
                   : 0,
        
        CreatedAt = DateTime.UtcNow
    };
}
```

### Resultado do Domain
```csharp
var domain = item.ToDomain();

// ✅ Valores corretos:
domain.Price      = 79.9m      // ✅ Correto!
domain.SkuCount   = 5          // ✅ Correto!
domain.ProductId  = "12345"    // ✅ Correto!
domain.SellerId   = "seller-1" // ✅ Correto!
```

---

## 📊 Tabela de Comparação

| Operação | PT-BR (❌ Antes) | en-US (✅ Depois) |
|---|---|---|
| `decimal.Parse("79.9")` | ❌ Erro | ✅ 79.9 |
| `decimal.TryParse("79.9", out price)` | ❌ false | ✅ true |
| `price.ToString()` | "79,9" | "79.9" ✅ |
| `JsonSerializer.Serialize(obj)` | `"price":"79,9"` | `"price":79.9` ✅ |
| DynamoDB Parsing | ❌ Falha | ✅ Sucesso |

---

## 🎯 Impacto Específico no ProductSellerDomain

### Antes (Problema)
```csharp
var item = dynamoDbResult;  // {"price": {"N": "79.9"}}

// ❌ Falha no parsing!
decimal.TryParse(item["price"].N, out var price)  // false
var domain = new ProductSellerDomain { Price = 0 };  // Price fica 0!
```

### Depois (Solução)
```csharp
var item = dynamoDbResult;  // {"price": {"N": "79.9"}}

// ✅ Parsing sucesso!
decimal.TryParse(item["price"].N, out var price)  // true, price = 79.9
var domain = new ProductSellerDomain { Price = 79.9m };  // Price correto!
```

---

## 🔧 Aplicar Mudanças

### Passo 1: Verificar Configuração
```bash
cat /Users/afonsofernandes/Documents/Projects/drop-shop-site/backend/Dropship/Program.cs | grep -A 10 "CultureInfo"
```

### Passo 2: Rebuild da Solução
```bash
cd /Users/afonsofernandes/Documents/Projects/drop-shop-site/backend
dotnet build
dotnet run
```

### Passo 3: Testar
```bash
curl http://localhost:5000/test-culture
```

---

## ✅ Verificação de Sucesso

Quando você fizer uma requisição para buscar produtos com seller:

```json
{
  "sellerId": "seller-1",
  "price": 79.9,
  "skuCount": 5,
  "createdAt": "2026-02-19T10:30:00Z"
}
```

✅ O price mostrará **79.9** (com ponto)
❌ Não seria mais 79,9 (com virgula)
❌ E não seria mais 0 (falha de parsing)

---

**Status**: ✅ Implementado e Pronto para Uso
**Data**: 19/02/2026

