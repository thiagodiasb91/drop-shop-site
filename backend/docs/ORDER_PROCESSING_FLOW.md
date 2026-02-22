# 🔄 Order Processing Flow Diagram

## Fluxo Completo de Processamento de Pedidos

```
┌─────────────────────────────────────────────────────────────────────┐
│                     POST /orders/process                            │
│                    (ProcessOrderRequest)                            │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                           ▼
           ┌───────────────────────────────┐
           │  Validações Iniciais          │
           │  - OrderSn not null/empty     │
           │  - Status not null/empty      │
           │  - ShopId > 0                 │
           └───────────────┬───────────────┘
                           │
                ┌──────────┴──────────┐
                │                     │
          ✅ Valid             ❌ Invalid
                │                     │
                ▼                     ▼
        ┌──────────────┐      ┌──────────────┐
        │  Continue    │      │  400 Error   │
        └──────┬───────┘      │  Bad Request │
               │              └──────────────┘
               ▼
    ┌──────────────────────────────┐
    │ OrderProcessingService       │
    │ ProcessOrderAsync()          │
    └──────────────┬───────────────┘
                   │
                   ▼
    ┌───────────────────────────────────────┐
    │  Valida Status == "READY_TO_SHIP"     │
    └────────────────┬──────────────────────┘
                     │
         ┌───────────┴────────────┐
         │                        │
    ✅ READY_TO_SHIP      ❌ Outro Status
         │                        │
         ▼                        ▼
    Continue            ┌──────────────────┐
         │              │  Return false    │
         │              │  (não processa)  │
         │              └──────────────────┘
         │
         ▼
    ┌──────────────────────────────────────────┐
    │ ShopeeApiService                         │
    │ GetOrderDetailAsync(shopId, [orderSn])   │
    │                                          │
    │ GET /api/v2/order/get_order_detail       │
    └──────────────────┬─────────────────────┘
                       │
         ┌─────────────┴──────────────┐
         │                            │
    ✅ Success                    ❌ Error
         │                            │
         ▼                            ▼
    Parse JSON                 ┌────────────┐
    Extract itemList           │ Throw Ex   │
         │                     └────────────┘
         ▼
    ┌──────────────────────────────────┐
    │ For Each Item in Order           │
    │ - model_sku                      │
    │ - model_quantity_purchased       │
    └──────────────┬───────────────────┘
                   │
                   ▼
    ┌──────────────────────────────────────────┐
    │ ProcessOrderItemAsync()                  │
    │ (sku, quantity, orderSn, shopId)         │
    └──────────────┬───────────────────────────┘
                   │
                   ▼
    ┌──────────────────────────────────────────┐
    │ GetSuppliersBySku(sku)                   │
    │                                          │
    │ QueryTableAsync(GSI_SKU_LOOKUP)          │
    │ Filter: entity_type = "product_sku_supplier" │
    │ Order By: Priority ASC, Quantity DESC    │
    └──────────────┬───────────────────────────┘
                   │
         ┌─────────┴──────────┐
         │                    │
    ✅ Found            ❌ No suppliers
    Suppliers                │
         │                   ▼
         │            Log warning, return
         │
         ▼
    ┌──────────────────────────────┐
    │ For Each Supplier            │
    │ (while remainingQty > 0)     │
    └──────────────┬───────────────┘
                   │
                   ▼
    ┌──────────────────────────────────┐
    │ Calculate:                       │
    │ quantityToUse = Min(             │
    │   remainingQty,                  │
    │   supplier.Quantity              │
    │ )                                │
    └──────────────┬───────────────────┘
                   │
         ┌─────────┴──────────────┐
         │                        │
         ▼                        ▼
    Process 3         Parallel operations:
    Operations
    Async:       1. UpdateSupplierStockAsync()
                    - PK: Product#{productId}
                    - SK: Sku#{sku}#Supplier#{supplierId}
                    - SET quantity = quantity - :qty
                    
                 2. AddToKardexAsync()
                    - PK: Kardex#Sku#{sku}
                    - SK: {ULID}
                    - operation: "remove"
                    - product_id, quantity, supplier_id
                    - ordersn, shop_id, date
                    
                 3. AddToPaymentQueueAsync()
                    - PK: PaymentQueue#Seller#{sellerId}
                    - SK: PaymentStatus#Pending#Supplier#{id}#Order#{ordersn}#Sku#{sku}
                    - product_id, sku, quantity
                    - value (production_price)
                    - status: "pending"
                    - supplier_id, shop_id, seller_id
         │                        │
         └────────────┬───────────┘
                      │
                      ▼
         ┌──────────────────────────────┐
         │ remainingQty -= quantityToUse│
         │ Continua para próximo        │
         │ fornecedor                   │
         └──────────────┬───────────────┘
                        │
                        ▼
                   ┌─────────────┐
                   │ Próximo Item│
                   │ ou          │
                   │ Retorn true │
                   └─────────────┘
                        │
                        ▼
    ┌──────────────────────────────────┐
    │ OrdersController                 │
    │ Retorna 200 OK                   │
    │                                  │
    │ {                                │
    │   "message": "Processado",       │
    │   "orderSn": "...",              │
    │   "status": "READY_TO_SHIP",     │
    │   "shopId": 341431138            │
    │ }                                │
    └──────────────────────────────────┘
```

## Fluxo de Tratamento de Erros

```
┌─────────────────────────────────────┐
│ Exception na ProcessOrderItemAsync   │
│ ou ProcessOrderAsync                │
└────────────────┬────────────────────┘
                 │
                 ▼
        ┌─────────────────┐
        │ Log Exception   │
        │ (LogError)      │
        └────────┬────────┘
                 │
                 ▼
        ┌─────────────────────┐
        │ Throw Exception     │
        │ para OrdersController
        └────────┬────────────┘
                 │
                 ▼
        ┌──────────────────────────┐
        │ Catch em ProcessOrder()  │
        │                          │
        │ Return 500 Error         │
        │ { "error": "message" }   │
        └──────────────────────────┘
```

## Fluxo de Validação de Seller

```
┌─────────────────────────────────────────┐
│ AddToPaymentQueueAsync()                │
│ (necessita seller_id)                   │
└────────────────┬────────────────────────┘
                 │
                 ▼
    ┌─────────────────────────────────┐
    │ SellerRepository                │
    │ GetSellerByShopIdAsync(shopId)  │
    │                                 │
    │ QueryTableAsync(GSI_SHOPID_LOOKUP)
    │ PK begins_with: "Seller#"       │
    │ shop_id = :shopid               │
    └────────────────┬────────────────┘
                     │
         ┌───────────┴──────────┐
         │                      │
    ✅ Found                ❌ Not Found
    Seller                      │
         │                      ▼
         │              ┌────────────────┐
         ▼              │ Throw Exception│
    Extract              │ "Seller not   │
    SellerId             │  found for    │
         │               │  shop #{id}"  │
         │               └────────────────┘
         ▼
    Continue with
    PaymentQueue
    creation
```

## Estrutura de Dados Resultante

### Antes do Processamento:
```
Product-Sku-Supplier:
PK: Product#prod-123
SK: Sku#SKU-001#Supplier#sup-456
Quantity: 80
Priority: 1
Price: 49.90
```

### Depois do Processamento de 3 unidades:
```
1️⃣ UPDATED Product-Sku-Supplier:
   Quantity: 77 (80 - 3)

2️⃣ NEW Kardex:
   PK: Kardex#Sku#SKU-001
   SK: 01ARZ3NDEKTSV4RRFFQ69G5FAV
   operation: "remove"
   quantity: 3
   supplier_id: sup-456
   ordersn: 2501080NKAMXA8
   shop_id: 341431138

3️⃣ NEW PaymentQueue:
   PK: PaymentQueue#Seller#sell-789
   SK: PaymentStatus#Pending#Supplier#sup-456#Order#2501080NKAMXA8#Sku#SKU-001
   status: "pending"
   quantity: 3
   value: 49.90 (production_price * qty = 49.90)
```

## Regras de Negócio Implementadas

1. ✅ **Validação de Status**: Apenas "READY_TO_SHIP" é processado
2. ✅ **Busca de Fornecedores**: Ordenada por prioridade (menor primeiro), depois quantidade (maior primeiro)
3. ✅ **Distribuição de Estoque**: Se houver múltiplos fornecedores, distribui entre eles
4. ✅ **Atualização Atômica**: Estoque, Kardex e Payment Queue são atualizados juntos
5. ✅ **Rastreabilidade**: Cada operação é registrada no Kardex
6. ✅ **Auditoria**: Payment Queue mantém histórico para auditoria financeira
7. ✅ **Seller Lookup**: Seller é obtido via GSI (lookup eficiente)

## Complexidade Computacional

- **Time Complexity**: O(N * M) onde N = número de items, M = número de fornecedores por SKU
- **Space Complexity**: O(M) para armazenar fornecedores em memória
- **DynamoDB Operations**: 2 reads + 3 writes por supplier utilizado

