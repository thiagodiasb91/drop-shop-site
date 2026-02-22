# 📋 Order Processing Implementation - Resumo

## ✅ Implementação Concluída

Foi implementado um sistema completo de processamento de pedidos integrado com a API da Shopee, seguindo a lógica do código Python fornecido.

## 📁 Arquivos Criados

### 1. **ProcessOrderRequest.cs** 
`/Dropship/Requests/ProcessOrderRequest.cs` (25 linhas)

DTO para receber dados do pedido:
- `OrderSn`: Número do pedido na Shopee
- `Status`: Status do pedido (READY_TO_SHIP, PAID, UNPAID, etc)
- `UpdateTime`: Timestamp da última atualização
- `ShopId`: ID da loja no Shopee

### 2. **OrderProcessingService.cs**
`/Dropship/Services/OrderProcessingService.cs` (350 linhas)

Serviço principal de processamento que implementa:

#### Métodos Públicos:
- `ProcessOrderAsync()` - Processa um pedido completo

#### Métodos Privados:
- `ProcessOrderItemAsync()` - Processa item individual do pedido
- `GetSuppliersBySku()` - Busca fornecedores de um SKU (ordenado por prioridade)
- `UpdateSupplierStockAsync()` - Atualiza estoque do fornecedor (subtrai quantidade)
- `AddToKardexAsync()` - Registra movimentação no Kardex
- `AddToPaymentQueueAsync()` - Cria registro na fila de pagamento

#### Fluxo de Processamento:
1. Valida se status é "READY_TO_SHIP"
2. Obtém detalhes do pedido via `ShopeeApiService.GetOrderDetailAsync()`
3. Para cada SKU no pedido:
   - Busca fornecedores ordenados por prioridade e quantidade
   - Itera pelos fornecedores até atingir a quantidade pedida
   - Para cada fornecedor usado:
     - Atualiza estoque (quantidade - vendido)
     - Registra no Kardex com entityType="kardex"
     - Cria registro na fila de pagamento com status="pending"

### 3. **OrdersController.cs** (Atualizado)
`/Dropship/Controllers/OrdersController.cs` (112 linhas)

Controller que expõe endpoint para processar pedidos:

#### Endpoint:
```
POST /orders/process
Content-Type: application/json

{
    "ordersn": "2501080NKAMXA8",
    "status": "READY_TO_SHIP",
    "update_time": 1736323997,
    "shop_id": 341431138
}
```

#### Response de Sucesso:
```json
{
    "message": "Pedido processado com sucesso",
    "orderSn": "2501080NKAMXA8",
    "status": "READY_TO_SHIP",
    "shopId": 341431138,
    "updateTime": 1736323997
}
```

#### Response quando Status != READY_TO_SHIP:
```json
{
    "message": "Pedido não foi processado",
    "details": "Status deve ser 'READY_TO_SHIP'",
    "orderSn": "2501080NKAMXA8",
    "status": "UNPAID",
    "shopId": 341431138
}
```

### 4. **ProductSkuSupplierDomain.cs** (Atualizado)
Adicionada propriedade `Priority` para ordenação de fornecedores:
- `Priority: int` - Prioridade do fornecedor (menor = maior prioridade)

### 5. **Program.cs** (Atualizado)
Registrado `OrderProcessingService` na injeção de dependências:
```csharp
builder.Services.AddScoped<OrderProcessingService>();
```

## 🔗 Integração com Repositórios Existentes

O serviço reaproveita:

1. **ShopeeApiService**
   - `GetOrderDetailAsync()` - Obtém detalhes do pedido

2. **DynamoDbRepository**
   - `QueryTableAsync()` - Busca fornecedores
   - `UpdateItemAsync()` - Atualiza estoque
   - `PutItemAsync()` - Cria registros

3. **KardexService**
   - `AddToKardexAsync()` - Registra movimentação

4. **SellerRepository**
   - `GetSellerByShopIdAsync()` - Obtém seller pelo shop_id

## 📊 Estrutura de Dados DynamoDB

### Registros Atualizados:

**1. Product-Sku-Supplier (Estoque Reduzido)**
```dynamodb
{
    "PK": "Product#3a60aa94111c491c97c293f990c0eddb",
    "SK": "Sku#CROSS_P#Supplier#051728cf88c143b5814ec9706ab61ddb",
    "quantity": 77  // Reduzido de 80 para 77
}
```

**2. Kardex (Nova Movimentação)**
```dynamodb
{
    "PK": "Kardex#Sku#CROSS_P",
    "SK": "01ARZ3NDEKTSV4RRFFQ69G5FAV",  // ULID
    "entity_type": "kardex",
    "product_id": "3a60aa94111c491c97c293f990c0eddb",
    "quantity": 3,
    "operation": "remove",
    "ordersn": "2501080NKAMXA8",
    "supplier_id": "051728cf88c143b5814ec9706ab61ddb",
    "shop_id": 341431138,
    "date": "2026-02-20T15:30:45.123Z"
}
```

**3. Payment Queue (Nova Fila de Pagamento)**
```dynamodb
{
    "PK": "PaymentQueue#Seller#69611396-ee23-4a96-9161-7c9928679056",
    "SK": "PaymentStatus#Pending#Supplier#051728cf88c143b5814ec9706ab61ddb#Order#2501080NKAMXA8#Sku#CROSS_P",
    "entity_type": "paymentQueue",
    "product_id": "3a60aa94111c491c97c293f990c0eddb",
    "sku": "CROSS_P",
    "quantity": 3,
    "value": 49.90,  // Preço de produção
    "status": "pending",
    "created_at": "2026-02-20T15:30:45.123Z",
    "shop_id": 341431138,
    "seller_id": "69611396-ee23-4a96-9161-7c9928679056",
    "ordersn": "2501080NKAMXA8",
    "supplier_id": "051728cf88c143b5814ec9706ab61ddb"
}
```

## 🔍 Validações Implementadas

1. ✅ Status deve ser "READY_TO_SHIP"
2. ✅ OrderSn obrigatório e não vazio
3. ✅ ShopId válido (> 0)
4. ✅ Seller deve existir para o shop_id
5. ✅ Fornecedores devem ter quantidade suficiente
6. ✅ Tratamento de exceções em todos os níveis
7. ✅ Logging detalhado em cada etapa

## 📝 Logging

Todo o fluxo é registrado com informações detalhadas:
- `[ORDERS]` - Log no controller
- `[ORDERS PROCESSING]` - Log no serviço (implícito via nome da classe)

Exemplo de logs:
```
[ORDERS] Processing order - OrderSn: 2501080NKAMXA8, Status: READY_TO_SHIP, ShopId: 341431138
Processing order - OrderSn: 2501080NKAMXA8, Status: READY_TO_SHIP, ShopId: 341431138
Processing item - SKU: CROSS_P, Quantity: 3, OrderSn: 2501080NKAMXA8
Getting suppliers for SKU - SKU: CROSS_P
Processing supplier - ProductId: 3a60aa94111c491c97c293f990c0eddb, SKU: CROSS_P, SupplierId: 051728cf88c143b5814ec9706ab61ddb, Quantity: 3
[ORDERS] Order processed successfully - OrderSn: 2501080NKAMXA8
```

## 🧪 Exemplo de Teste

### Request:
```bash
curl -X POST http://localhost:5000/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "ordersn": "2501080NKAMXA8",
    "status": "READY_TO_SHIP",
    "update_time": 1736323997,
    "shop_id": 341431138
  }'
```

### Response (Sucesso):
```json
{
    "message": "Pedido processado com sucesso",
    "orderSn": "2501080NKAMXA8",
    "status": "READY_TO_SHIP",
    "shopId": 341431138,
    "updateTime": 1736323997
}
```

## 🚀 Próximos Passos (Opcional)

1. Implementar webhook para receber notificações de pedidos automaticamente
2. Adicionar lógica para processar pedidos em background (SQS)
3. Implementar retry com exponential backoff em caso de falhas
4. Adicionar monitoramento de performance
5. Implementar testes unitários

## ⚠️ Notas Importantes

1. O serviço valida se o status é exatamente "READY_TO_SHIP" (case-sensitive)
2. A quantidade é obtida dos fornecedores em ordem: prioridade (menor primeiro), depois quantidade (maior primeiro)
3. Se houver múltiplos fornecedores, o serviço distribui a quantidade entre eles automaticamente
4. O preço de produção é obtido diretamente do registro Product-Sku-Supplier
5. O seller_id é obtido via GSI_SHOPID_LOOKUP a partir do shop_id

