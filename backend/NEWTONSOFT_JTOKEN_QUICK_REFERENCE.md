# 🔖 Newtonsoft.Json JToken - Quick Reference Guide

## Snippets Prontos para Usar

### 1. Parse JSON String
```csharp
string jsonString = "{ \"name\": \"John\", \"age\": 30 }";
var jObject = JObject.Parse(jsonString);
```

### 2. Parse de JsonDocument
```csharp
var jObject = JObject.Parse(jsonDocument.RootElement.GetRawText());
```

### 3. Acesso Simples
```csharp
var jObject = JObject.Parse(json);

// Retorna JToken ou null
var name = jObject["name"];

// Retorna string ou null
var nameStr = jObject["name"]?.Value<string>();

// Com valor padrão
var nameStr = jObject["name"]?.Value<string>() ?? "Unknown";
```

### 4. Null-Coalescing Throw
```csharp
var required = jObject["required"] ?? 
    throw new InvalidOperationException("required field missing");

var validated = jObject["validated"]?.Value<bool>() ?? 
    throw new ArgumentException("invalid validated field");
```

### 5. Arrays
```csharp
var jArray = jObject["items"] as JArray;

if (jArray?.HasValues == true)
{
    foreach (var item in jArray)
    {
        var id = item["id"]?.Value<int>();
    }
}

// Ou verificar count
if (jArray?.Count > 0)
{
    var first = jArray.First;
    var count = jArray.Count();
}
```

### 6. Iteração de Array
```csharp
var items = jObject["items"];

// Com HasValues check
if (items?.HasValues == true)
{
    foreach (var item in items)
    {
        var name = item["name"]?.Value<string>();
    }
}

// Com FirstOrDefault
var firstItem = items?.FirstOrDefault();

// Com LINQ
var names = items?.Select(x => x["name"]?.Value<string>()).ToList();
```

### 7. Aninhamento
```csharp
// Acesso seguro em estruturas aninhadas
var city = jObject["address"]?["city"]?.Value<string>();

// Com validação
var zip = jObject["address"]?["zip"] ?? 
    throw new InvalidOperationException("zip required");

// Com múltiplos níveis
var country = jObject["address"]?["geo"]?["country"]?.Value<string>();
```

### 8. Verificação de Tipo
```csharp
var token = jObject["value"];

if (token?.Type == JTokenType.Array)
{
    foreach (var item in (JArray)token)
    {
        // Process array
    }
}

if (token?.Type == JTokenType.Object)
{
    var name = token["name"]?.Value<string>();
}

if (token?.Type == JTokenType.String)
{
    var str = token.Value<string>();
}
```

### 9. Conversão Segura
```csharp
// Com casting safe
var intValue = token?.Value<int?>();
if (intValue.HasValue)
{
    var result = intValue.Value;
}

// Com TryParse
var strValue = token?.Value<string>();
if (int.TryParse(strValue, out var number))
{
    // Use number
}

// Com try-catch
try
{
    var date = token?.Value<DateTime>();
}
catch (FormatException)
{
    // Handle invalid format
}
```

### 10. Modificação
```csharp
var jObject = JObject.Parse(json);

// Adicionar propriedade
jObject["newProp"] = "value";

// Modificar propriedade
jObject["existing"] = "newValue";

// Remover
jObject.Remove("oldProp");

// Serializar de volta
var jsonString = jObject.ToString();
```

---

## Padrões Comuns

### Validação com Throw
```csharp
public bool ValidateOrder(JObject order)
{
    var orderSn = order["ordersn"] ?? 
        throw new ArgumentException("ordersn required");
    
    var status = order["status"] ?? 
        throw new ArgumentException("status required");
    
    return status.Value<string>() == "READY_TO_SHIP";
}
```

### Processamento Seguro
```csharp
public void ProcessItems(JObject response)
{
    var items = response["items"];
    
    if (items?.HasValues != true)
    {
        _logger.LogWarning("No items found");
        return;
    }
    
    foreach (var item in items)
    {
        var sku = item["sku"]?.Value<string>();
        if (string.IsNullOrEmpty(sku)) continue;
        
        var qty = item["quantity"]?.Value<int>();
        if (!qty.HasValue) continue;
        
        ProcessItem(sku, qty.Value);
    }
}
```

### Transformação
```csharp
public List<OrderItem> ExtractItems(JObject response)
{
    return response["items"]?
        .Select(item => new OrderItem
        {
            Sku = item["sku"]?.Value<string>(),
            Quantity = item["quantity"]?.Value<int>() ?? 0,
            Price = decimal.Parse(item["price"]?.Value<string>() ?? "0")
        })
        .Where(x => !string.IsNullOrEmpty(x.Sku))
        .ToList() ?? new List<OrderItem>();
}
```

### Validação com Mensagens
```csharp
public class OrderValidator
{
    public (bool valid, string error) ValidateOrder(JObject order)
    {
        if (order["ordersn"] == null)
            return (false, "Order SN is required");
        
        if (order["status"]?.Value<string>() == null)
            return (false, "Status is required");
        
        var items = order["items"];
        if (items?.HasValues != true)
            return (false, "At least one item is required");
        
        return (true, "");
    }
}
```

---

## Comparação com System.Text.Json

```csharp
// ❌ ANTES (System.Text.Json)
if (!json.TryGetProperty("items", out var itemsElement) || 
    itemsElement.ValueKind == JsonValueKind.Null)
{
    throw new Exception("No items");
}

var count = itemsElement.GetArrayLength();
foreach (var item in itemsElement.EnumerateArray())
{
    if (!item.TryGetProperty("sku", out var skuElement))
        continue;
    
    var sku = skuElement.GetString();
}

// ✅ DEPOIS (Newtonsoft.Json)
var items = json["items"] ?? 
    throw new Exception("No items");

var count = items.Count();
foreach (var item in items)
{
    var sku = item["sku"]?.Value<string>();
    if (string.IsNullOrEmpty(sku)) continue;
}
```

---

## Performance Tips

### ✅ BOM
```csharp
// Parse uma única vez
var jObject = JObject.Parse(json);

// Reutilize em múltiplas operações
var name = jObject["name"]?.Value<string>();
var email = jObject["email"]?.Value<string>();
var phone = jObject["phone"]?.Value<string>();
```

### ❌ RUIM
```csharp
// Parse múltiplas vezes
var name = JObject.Parse(json)["name"]?.Value<string>();
var email = JObject.Parse(json)["email"]?.Value<string>();
var phone = JObject.Parse(json)["phone"]?.Value<string>();
```

---

## Debugging

### Print JObject
```csharp
var jObject = JObject.Parse(json);

// Pretty print
var prettyJson = jObject.ToString(Formatting.Indented);
_logger.LogInformation("Parsed JSON: {Json}", prettyJson);

// Compact print
var compactJson = jObject.ToString();

// Just check exists
var hasItems = jObject["items"] != null;
```

### Verify Types
```csharp
var token = jObject["value"];

_logger.LogInformation("Token type: {Type}", token?.Type);
// Outputs: JTokenType.String, Array, Object, etc.
```

---

## Tratamento de Erros

### Try-Catch Pattern
```csharp
try
{
    var jObject = JObject.Parse(json);
    var value = jObject["required"]?.Value<DateTime>();
}
catch (JsonException ex)
{
    _logger.LogError("Invalid JSON: {Message}", ex.Message);
}
catch (FormatException ex)
{
    _logger.LogError("Invalid value format: {Message}", ex.Message);
}
catch (Exception ex)
{
    _logger.LogError("Unexpected error: {Message}", ex.Message);
}
```

### Validation Pattern
```csharp
public bool TryExtractValue<T>(JToken token, string key, out T result)
{
    result = default;
    
    try
    {
        var value = token[key]?.Value<T>();
        if (value == null) return false;
        
        result = value;
        return true;
    }
    catch
    {
        return false;
    }
}

// Uso
if (TryExtractValue<string>(jObject, "name", out var name))
{
    _logger.LogInformation("Name: {Name}", name);
}
```

---

## Cheat Sheet

```csharp
var json = "{ \"name\": \"John\", \"items\": [{ \"id\": 1 }] }";
var obj = JObject.Parse(json);

// Acesso
obj["name"]                      // JToken ou null
obj["name"]?.Value<string>()     // string ou null
obj["name"]?.Value<string>() ?? "default"  // string com fallback

// Arrays
obj["items"]?.Count()            // int
obj["items"]?.HasValues          // bool
obj["items"]?.FirstOrDefault()   // JToken ou null
foreach(var x in obj["items"] ?? new JArray()) // Iteração safe

// Aninhamento
obj["a"]?["b"]?["c"]?.Value<T>() // Múltiplos níveis

// Erro/Validação
obj["required"] ?? throw new Exception("...")  // Obrigatório
obj["optional"]?.Value<string>()  // Opcional

// Tipo
obj["value"]?.Type                // JTokenType
obj["value"]?.Type == JTokenType.Array

// Modificação
obj["key"] = "value"              // Adicionar/Modificar
obj.Remove("key")                 // Remover
```

---

## Referências

- [Newtonsoft.Json Docs](https://www.newtonsoft.com/json/help/html/Introduction.htm)
- [JToken API](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JToken.htm)
- [JObject API](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_Linq_JObject.htm)

---

**Última atualização**: 2026-02-20  
**Status**: Pronto para uso ✅

