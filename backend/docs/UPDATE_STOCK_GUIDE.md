# Update Stock Guide

## Visão Geral

O método `UpdateStockAsync` permite atualizar o estoque de produtos na Shopee através da API v2.

### Endpoint
- **POST** `/api/v2/product/update_stock`
- **Controller**: `ShopeeInterfaceController`
- **Route**: `PUT /shopee-interface/items/{itemId}/stock`

## Referência Oficial
https://open.shopee.com/documents/v2/v2.product.update_stock

## Parâmetros

### URL Parameters
| Parâmetro | Tipo | Obrigatório | Descrição |
|-----------|------|-------------|-----------|
| `shopId` | long | Sim | ID da loja Shopee |
| `itemId` | long | Sim | ID do produto/item na Shopee |

### Request Body
```json
{
  "stockList": [
    {
      "modelId": null,
      "stock": 100
    }
  ]
}
```

## Casos de Uso

### 1. Atualizar estoque de item SEM variações

**Request:**
```bash
curl -X PUT "http://localhost:5000/shopee-interface/items/123456789/stock?shopId=226289035" \
  -H "Content-Type: application/json" \
  -d '{
    "stockList": [
      {
        "modelId": null,
        "stock": 100
      }
    ]
  }'
```

**JSON Payload:**
```json
{
  "stockList": [
    {
      "stock": 100
    }
  ]
}
```

### 2. Atualizar estoque de item COM variações (múltiplos modelos)

**Request:**
```bash
curl -X PUT "http://localhost:5000/shopee-interface/items/123456789/stock?shopId=226289035" \
  -H "Content-Type: application/json" \
  -d '{
    "stockList": [
      {
        "modelId": 111,
        "stock": 50
      },
      {
        "modelId": 222,
        "stock": 75
      },
      {
        "modelId": 333,
        "stock": 100
      }
    ]
  }'
```

**JSON Payload:**
```json
{
  "stockList": [
    {
      "modelId": 111,
      "stock": 50
    },
    {
      "modelId": 222,
      "stock": 75
    },
    {
      "modelId": 333,
      "stock": 100
    }
  ]
}
```

## Exemplo Completo - Variações Cor + Tamanho

Para um produto com 9 variações (3 cores × 3 tamanhos):

**JSON Payload:**
```json
{
  "stockList": [
    {
      "modelId": 9250789027,
      "stock": 10
    },
    {
      "modelId": 9250789028,
      "stock": 15
    },
    {
      "modelId": 9250789029,
      "stock": 20
    },
    {
      "modelId": 9250789030,
      "stock": 12
    },
    {
      "modelId": 9250789031,
      "stock": 18
    },
    {
      "modelId": 9250789032,
      "stock": 25
    },
    {
      "modelId": 9250789033,
      "stock": 8
    },
    {
      "modelId": 9250789034,
      "stock": 14
    },
    {
      "modelId": 9250789035,
      "stock": 22
    }
  ]
}
```

## Classes C#

### StockListDto
```csharp
public class StockListDto
{
    /// <summary>
    /// ID do modelo/variação (opcional)
    /// Se não fornecido, atualiza o estoque do item inteiro
    /// </summary>
    public long? ModelId { get; set; }

    /// <summary>
    /// Quantidade de estoque
    /// Valor inteiro, maior ou igual a 0
    /// </summary>
    public int Stock { get; set; }
}
```

### UpdateStockRequest
```csharp
public class UpdateStockRequest
{
    /// <summary>
    /// Lista de estoques para atualizar
    /// </summary>
    public List<StockListDto> StockList { get; set; } = new();
}
```

## Resposta (Response)

A resposta será um objeto JSON com a estrutura da Shopee:

```json
{
  "error": "",
  "message": "",
  "request_id": "e3e3e7f34aa1989011960c7d42ad2a00",
  "response": {
    "item_id": 885176298,
    "model": [
      {
        "model_id": 9250789027,
        "stock": 10,
        "stock_info": [
          {
            "stock_type": 2,
            "normal_stock": 10
          }
        ]
      }
    ]
  }
}
```

## Validações

1. **shopId** deve ser um valor maior que 0
2. **itemId** deve ser um valor maior que 0
3. **stockList** não pode estar vazio
4. Cada item em **stockList** deve ter:
   - `stock` >= 0 (obrigatório)
   - `modelId` > 0 (opcional, se não fornecido atualiza item inteiro)

## Código de Exemplo - C#

```csharp
var request = new UpdateStockRequest
{
    StockList = new List<StockListDto>
    {
        new StockListDto { ModelId = 111, Stock = 50 },
        new StockListDto { ModelId = 222, Stock = 75 },
        new StockListDto { ModelId = 333, Stock = 100 }
    }
};

var result = await _shopeeApiService.UpdateStockAsync(
    shopId: 226289035,
    itemId: 123456789,
    stockList: request.StockList
);
```

## Fluxo de Funcionamento

1. **Autenticação**: O método obtém automaticamente o access_token do cache usando o `shopId`
2. **Geração da Assinatura**: Cria uma assinatura HMAC SHA256 usando partner_id, partner_key, path e timestamp
3. **Requisição à API**: Envia POST para a API da Shopee com o payload
4. **Tratamento de Erro**: Valida a resposta e retorna os dados ou lança exceção

## Logs

Os logs são registrados em diferentes níveis:
- **Information**: Início e sucesso da operação
- **Debug**: Detalhes de URL, timestamp e resposta
- **Error**: Erros durante a operação

## Notas Importantes

1. Os model_ids devem ser os IDs corretos retornados pela Shopee quando o item/variação foi criado
2. O stock é sempre um valor inteiro (não decimal)
3. O `shopId` é obtido automaticamente do cache de tokens (deve ter sido feito login anteriormente)
4. A atualização de estoque é instantânea na Shopee após sucesso da requisição
5. Não há limite de modelos que podem ser atualizados por requisição, mas é recomendado não exceder 100

