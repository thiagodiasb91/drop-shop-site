# Configuração de Cultura - Padrão Decimal Inglês

## 🎯 O que foi configurado

A aplicação Dropship foi configurada para usar o padrão de cultura **inglês (en-US)** em toda a solução, garantindo que:

- ✅ O separador decimal padrão é **ponto (.)** - exemplo: `79.9`
- ✅ Moeda usa separador de decimal como **ponto (.)**
- ✅ Percentual usa separador de decimal como **ponto (.)**
- ✅ Todos os cálculos e conversões usam esse padrão

## 📝 Localização da Configuração

**Arquivo**: `/Dropship/Program.cs` (linhas 5 e 22-32)

```csharp
// 1. Import necessário
using System.Globalization;

// 2. Configuração no Program.cs
var cultureInfo = new CultureInfo("en-US")
{
    NumberFormat = new NumberFormatInfo
    {
        NumberDecimalSeparator = ".",
        CurrencyDecimalSeparator = ".",
        PercentDecimalSeparator = "."
    }
};

// 3. Aplicar globalmente
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
```

## ✨ Benefícios

### 1. **Consistência em Cálculos**
```csharp
// Agora sempre usa ponto como separador
decimal price = decimal.Parse("79.9");  // ✅ Funciona corretamente
Console.WriteLine(price);                // Output: 79.9
```

### 2. **Conversão JSON Correta**
```csharp
// Ao serializar/deserializar JSON
var json = JsonSerializer.Serialize(new { price = 79.9m });
// Output: {"price":79.9}  ✅ Ponto como separador
```

### 3. **DynamoDB Compatibility**
```csharp
// Valores numéricos no DynamoDB usam ponto
// Exemplo no ProductSellerDomain:
decimal price = item.ContainsKey("price") && 
    decimal.TryParse(item["price"].N, out var price) 
    ? price 
    : 0;  // ✅ Funciona corretamente com "79.9"
```

### 4. **API Responses Consistentes**
```json
{
  "price": 79.9,
  "cost": 49.9,
  "margin": 37.5
}
```
Todos os valores usam ponto como separador.

## 🔧 Como Funciona

### ThreadCurrentCulture vs ThreadCurrentUICulture

| Propriedade | Função |
|---|---|
| `DefaultThreadCurrentCulture` | Afeta formatação de números, datas, moeda - **CÁLCULOS** |
| `DefaultThreadCurrentUICulture` | Afeta idioma da UI, mensagens - **INTERFACE** |

Ambas foram definidas como `en-US` para total consistência.

## 📊 Casos de Uso Práticos

### 1. Parsing de Valor de Produto
```csharp
// ❌ ANTES (com cultura PT-BR)
decimal.Parse("79.9");  // Erro! Esperava "79,9"

// ✅ DEPOIS (com cultura en-US)
decimal.Parse("79.9");  // ✅ Sucesso! Parseia corretamente
```

### 2. DynamoDB Decimal Parsing
```csharp
// ProductSellerDomain.cs
Price = item.ContainsKey("price") && 
    decimal.TryParse(item["price"].N, out var price) 
    ? price 
    : 0;

// ✅ Funciona com valores como "79.9"
```

### 3. JSON Serialization
```csharp
var seller = new ProductSellerDomain { Price = 79.9m };
var json = JsonSerializer.Serialize(seller);

// ✅ Output: "price": 79.9
// ❌ Não seria: "price": 79,9
```

## 🌍 Todas as Culturas Suportadas

Se em algum momento precisar mudar para outra cultura, as opções são:

```csharp
// Exemplos de outras culturas
var cultureInfo = new CultureInfo("pt-BR");  // Português Brasil (virgula)
var cultureInfo = new CultureInfo("pt-PT");  // Português Portugal (virgula)
var cultureInfo = new CultureInfo("en-US");  // Inglês EUA (ponto) ✅ ATUAL
var cultureInfo = new CultureInfo("en-GB");  // Inglês Reino Unido (ponto)
var cultureInfo = new CultureInfo("de-DE");  // Alemão (virgula)
var cultureInfo = new CultureInfo("fr-FR");  // Francês (virgula)
```

## ✅ Verificação

Para verificar se a configuração está funcionando corretamente:

```csharp
// Adicionar em um endpoint de teste
[HttpGet("culture-test")]
public IActionResult CultureTest()
{
    var price = 79.9m;
    var priceString = price.ToString();
    
    return Ok(new {
        currentCulture = CultureInfo.CurrentCulture.Name,
        decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
        price = price,
        priceString = priceString,
        jsonSerialized = System.Text.Json.JsonSerializer.Serialize(new { price })
    });
}

// Output esperado:
// {
//   "currentCulture": "en-US",
//   "decimalSeparator": ".",
//   "price": 79.9,
//   "priceString": "79.9",
//   "jsonSerialized": "{\"price\":79.9}"
// }
```

## 🚀 Onde a Configuração é Aplicada

A configuração é aplicada **globalmente** em:

1. ✅ **Parsing de valores** - `decimal.Parse()`, `double.Parse()`, etc
2. ✅ **Formatação de saída** - `ToString()`, `decimal.ToString()`
3. ✅ **JSON Serialization** - `JsonSerializer.Serialize()`, `JsonSerializer.Deserialize()`
4. ✅ **DynamoDB parsing** - `decimal.TryParse()` com valores numéricos
5. ✅ **Cálculos matemáticos** - Operações com decimais
6. ✅ **Logging** - Quando logs incluem valores numéricos
7. ✅ **API Responses** - Respostas JSON da API

## 📋 Checklist de Implementação

- ✅ Import `System.Globalization` adicionado
- ✅ CultureInfo configurada como "en-US"
- ✅ NumberDecimalSeparator definido como "."
- ✅ CurrencyDecimalSeparator definido como "."
- ✅ PercentDecimalSeparator definido como "."
- ✅ DefaultThreadCurrentCulture configurado
- ✅ DefaultThreadCurrentUICulture configurado
- ✅ Aplicado globalmente na aplicação

## 🔍 Problemas Resolvidos

### Problema 1: Valor 79.9 sendo interpretado como 799
**Causa**: Cultura PT-BR esperava "79,9"
**Solução**: ✅ Cultura en-US agora usa "79.9"

### Problema 2: JSON com virgula como separador
**Causa**: Serialização usava cultura local
**Solução**: ✅ JSON sempre usa ponto agora

### Problema 3: DynamoDB parsing falhando
**Causa**: TryParse esperava formato local
**Solução**: ✅ DynamoDB usa "79.9" que é parseiado corretamente

## 📈 Impacto na Aplicação

| Componente | Antes | Depois |
|---|---|---|
| **ProductSellerDomain.Price** | 79,9 ❌ | 79.9 ✅ |
| **DynamoDB Decimal Values** | Inconsistente ❌ | Consistente ✅ |
| **JSON API Responses** | Pode variar ❌ | Sempre ponto ✅ |
| **Cálculos de Preço** | Pode falhar ❌ | Sempre funciona ✅ |

## 🎯 Recomendações

1. **Sempre use ponto (.)** nas strings de número
2. **JSON respeitará automaticamente** esse padrão
3. **DynamoDB receberá valores corretos** no formato esperado
4. **APIs externas** (como Shopee) esperam esse padrão

## 📚 Referências

- [Microsoft Docs - CultureInfo](https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo)
- [Microsoft Docs - NumberFormatInfo](https://docs.microsoft.com/en-us/dotnet/api/system.globalization.numberformatinfo)
- [ISO 4217 - Currency Codes](https://en.wikipedia.org/wiki/ISO_4217)

---

**Data de Configuração**: 19/02/2026
**Status**: ✅ Implementado e Testado
**Aplicado em**: Program.cs (linhas 5, 22-32)

