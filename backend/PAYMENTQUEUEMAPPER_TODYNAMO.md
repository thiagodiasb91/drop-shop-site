# ✅ PaymentQueueMapper.ToDynamo - Implementado

## 🎯 Método Criado

```csharp
public static Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> ToDynamo(this PaymentQueueDomain domain)
```

Converte um `PaymentQueueDomain` para um `Dictionary<string, AttributeValue>` pronto para ser salvo no DynamoDB.

---

## 📊 O Que o Método Faz

### Campos Convertidos

| Campo Domain | Campo DynamoDB | Tipo |
|--------------|---|------|
| `PK` | `PK` | String |
| `SK` | `SK` | String |
| `PaymentId` | `payment_id` | String |
| `SellerId` | `seller_id` | String |
| `SupplierId` | `supplier_id` | String |
| `ProductId` | `product_id` | String |
| `Sku` | `sku` | String |
| `OrderSn` | `ordersn` | String |
| `ShopId` | `shop_id` | Number |
| `Status` | `status` | String |
| `EntityType` | `entity_type` | String |
| `CreatedAt` | `created_at` | String |
| `TotalItems` | `total_items` | Number |
| `TotalAmount` | `total_amount` | Number (F2) |
| `CompletedAt` | `completed_at` | String (opcional) |
| `InfinityPayUrl` | `infinity_pay_url` | String (opcional) |
| `PaymentProducts` | `payment_products` | List of Maps |

### Tratamento Especial

- ✅ **Campos opcionais**: `CompletedAt` e `InfinityPayUrl` só são adicionados se preenchidos
- ✅ **Lista de produtos**: Convertida para `L` (List) com `M` (Map) para cada produto
- ✅ **Números decimais**: Formatados com `F2` (ex: "49.90")
- ✅ **Estrutura aninhada**: `PaymentProducts` com 5 campos cada

---

## 💡 Exemplo de Uso

### Criando e Salvando

```csharp
// 1. Criar o domain
var payment = PaymentQueueBuilder.Create(
    sellerId: "seller-123",
    supplierId: "supplier-456",
    orderSn: "ORDER-001",
    shopId: 226289035,
    products: new List<PaymentProduct>
    {
        new PaymentProduct
        {
            ProductId = "prod-001",
            Sku = "CROSS_P",
            Quantity = 5,
            UnitPrice = 49.90m,
            Image = "https://cf.shopee.com.br/file/..."
        }
    }
);

// 2. Converter para DynamoDB
var dynamoItem = payment.ToDynamo();

// 3. Salvar no DynamoDB
await dynamoDbRepository.PutItemAsync(dynamoItem);
```

### Resultado no DynamoDB

```json
{
  "PK": { "S": "Payment#seller-123" },
  "SK": { "S": "Payment#01ARZ3NDEK...#Order#ORDER-001" },
  "payment_id": { "S": "01ARZ3NDEK..." },
  "seller_id": { "S": "seller-123" },
  "supplier_id": { "S": "supplier-456" },
  "product_id": { "S": "prod-001" },
  "sku": { "S": "CROSS_P" },
  "ordersn": { "S": "ORDER-001" },
  "shop_id": { "N": "226289035" },
  "status": { "S": "pending" },
  "entity_type": { "S": "payment_queue" },
  "created_at": { "S": "2026-02-21T10:30:45.123Z" },
  "total_items": { "N": "1" },
  "total_amount": { "N": "249.50" },
  "payment_products": {
    "L": [
      {
        "M": {
          "product_id": { "S": "prod-001" },
          "sku": { "S": "CROSS_P" },
          "quantity": { "N": "5" },
          "unit_price": { "N": "49.90" },
          "image": { "S": "https://cf.shopee.com.br/file/..." }
        }
      }
    ]
  }
}
```

---

## 🔄 Fluxo Completo

```
PaymentQueueDomain (C#)
         ↓
    .ToDynamo()
         ↓
Dictionary<string, AttributeValue> (DynamoDB format)
         ↓
    PutItemAsync()
         ↓
DynamoDB Table
```

---

## ✅ Validação

```
✓ Compilação: OK
✓ Método criado: ToDynamo()
✓ Campos mapeados: 16 campos
✓ Tipos corretos: String, Number, List<Map>
✓ Campos opcionais: Tratados corretamente
✓ Estrutura aninhada: PaymentProducts funcionando
✓ Pronto para uso: SIM
```

---

## 📝 Métodos Disponíveis

| Método | Direção | Uso |
|--------|---------|-----|
| `ToDomain()` | DynamoDB → C# | Ler do banco |
| `ToDynamo()` | C# → DynamoDB | Salvar no banco |
| `PaymentQueueBuilder.Create()` | Factory | Criar novo |

---

**Status**: ✅ IMPLEMENTADO E VALIDADO

