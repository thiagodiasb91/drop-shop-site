# Update Stock Implementation Summary

## O que foi implementado

### 1. Método na ShopeeApiService
- **Classe**: `ShopeeApiService.cs`
- **Método**: `UpdateStockAsync(long shopId, long itemId, List<StockListDto> stockList)`
- **Endpoint Shopee**: POST `/api/v2/product/update_stock`
- **Responsabilidade**: 
  - Autenticar usando access_token do cache
  - Gerar assinatura HMAC SHA256
  - Fazer requisição POST para API da Shopee
  - Validar resposta e retornar dados

### 2. DTOs no UpdateStockRequest
- **StockListDto**: 
  - `ModelId` (long?, opcional) - ID da variação
  - `Stock` (int) - Quantidade de estoque
  
- **UpdateStockRequest**:
  - `StockList` (List<StockListDto>) - Lista de estoques a atualizar

### 3. Endpoint no Controller
- **Classe**: `ShopeeInterfaceController.cs`
- **Rota**: `PUT /shopee-interface/items/{itemId}/stock`
- **Parâmetros**:
  - `shopId` (query parameter)
  - `itemId` (route parameter)
  - `request` (body) - UpdateStockRequest
- **Responsabilidade**:
  - Validar parâmetros
  - Chamar ShopeeApiService.UpdateStockAsync
  - Retornar resposta formatada

### 4. Documentação
- **Arquivo**: `docs/UPDATE_STOCK_GUIDE.md`
- **Contém**:
  - Visão geral do método
  - Guias de uso completos
  - Exemplos de payloads JSON
  - Casos de uso (item sem variações, com variações)
  - Código C# de exemplo
  - Validações e fluxo de funcionamento

## Fluxo de Requisição

```
Request (Controller)
    ↓
Validação de parâmetros
    ↓
ShopeeApiService.UpdateStockAsync()
    ↓
Obter access_token do cache
    ↓
Gerar assinatura HMAC SHA256
    ↓
POST para API Shopee
    ↓
Validar resposta
    ↓
Retornar JsonDocument com resultado
    ↓
Response (Controller)
```

## Características Principais

✅ **Autenticação Automática**: Obtém token do cache usando shopId
✅ **Validação Completa**: Valida shopId, itemId, stockList e valores
✅ **Logging Detalhado**: Information, Debug e Error levels
✅ **Suporte a Múltiplas Variações**: Atualiza vários modelos em uma requisição
✅ **Tratamento de Erro**: Lança exceções informativas em caso de falha
✅ **Compatibilidade**: Segue o mesmo padrão de UpdatePriceAsync

## Como Usar

### Via Postman/Curl

```bash
PUT /shopee-interface/items/123456789/stock?shopId=226289035
Content-Type: application/json

{
  "stockList": [
    {
      "modelId": 111,
      "stock": 50
    },
    {
      "modelId": 222,
      "stock": 75
    }
  ]
}
```

### Via C# (HttpClient)

```csharp
var request = new UpdateStockRequest
{
    StockList = new List<StockListDto>
    {
        new StockListDto { ModelId = 111, Stock = 50 },
        new StockListDto { ModelId = 222, Stock = 75 }
    }
};

// GET endpoint
var httpClient = new HttpClient();
var content = new StringContent(
    JsonSerializer.Serialize(request),
    Encoding.UTF8,
    "application/json"
);

var response = await httpClient.PutAsync(
    "http://localhost:5000/shopee-interface/items/123456789/stock?shopId=226289035",
    content
);
```

### Via ShopeeApiService Diretamente

```csharp
var stockList = new List<StockListDto>
{
    new StockListDto { ModelId = 111, Stock = 50 },
    new StockListDto { ModelId = 222, Stock = 75 }
};

var result = await _shopeeApiService.UpdateStockAsync(
    shopId: 226289035,
    itemId: 123456789,
    stockList: stockList
);
```

## Status da Implementação

| Componente | Status | Detalhes |
|-----------|--------|----------|
| ShopeeApiService | ✅ Implementado | Método UpdateStockAsync criado e testável |
| DTOs | ✅ Implementado | StockListDto e UpdateStockRequest criados |
| Controller Endpoint | ✅ Implementado | PUT /shopee-interface/items/{itemId}/stock |
| Documentação | ✅ Implementado | UPDATE_STOCK_GUIDE.md criado |
| Validações | ✅ Implementado | Todas as validações no controller e service |
| Logs | ✅ Implementado | Information, Debug e Error logging |

## Próximos Passos (Opcionais)

1. Integrar com lógica de gestão de estoque do negócio
2. Criar endpoints específicos para atualizar estoque por fornecedor/vendedor
3. Implementar fila de sincronização de estoque
4. Adicionar métricas de auditoria
5. Criar testes unitários

## Referências

- Documentação Shopee: https://open.shopee.com/documents/v2/v2.product.update_stock
- Método similar (UpdatePrice): ShopeeApiService.cs linha ~1120
- Documentação local: docs/UPDATE_STOCK_GUIDE.md

