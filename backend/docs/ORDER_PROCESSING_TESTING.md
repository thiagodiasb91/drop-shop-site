# 🧪 Order Processing - Manual Testing Guide

## Pré-requisitos

1. ✅ API rodando em `http://localhost:5000`
2. ✅ DynamoDB acessível (ou tabela `catalog-core` em produção)
3. ✅ Dados de teste preenchidos no banco

## Dados de Teste Necessários

### 1. Criar um Seller

```sql
Insert DynamoDB Item:
{
  "PK": { "S": "Seller#69611396-ee23-4a96-9161-7c9928679056" },
  "SK": { "S": "META" },
  "entityType": { "S": "seller" },
  "sellerId": { "S": "69611396-ee23-4a96-9161-7c9928679056" },
  "sellerName": { "S": "Test Seller" },
  "shop_id": { "N": "341431138" },
  "marketplace": { "S": "shopee" },
  "createdAt": { "N": "1740000000" }
}
```

### 2. Criar um Produto

```sql
{
  "PK": { "S": "Product#3a60aa94111c491c97c293f990c0eddb" },
  "SK": { "S": "META#test-product" },
  "entityType": { "S": "product" },
  "id": { "S": "3a60aa94111c491c97c293f990c0eddb" },
  "name": { "S": "Test Product" },
  "description": { "S": "Test Description" },
  "createdAt": { "S": "2026-02-20T00:00:00Z" }
}
```

### 3. Criar um SKU

```sql
{
  "PK": { "S": "Product#3a60aa94111c491c97c293f990c0eddb" },
  "SK": { "S": "Sku#CROSS_P" },
  "entityType": { "S": "sku" },
  "productId": { "S": "3a60aa94111c491c97c293f990c0eddb" },
  "sku": { "S": "CROSS_P" },
  "color": { "S": "Azul" },
  "size": { "S": "P" },
  "quantity": { "N": "100" },
  "createdAt": { "S": "2026-02-20T00:00:00Z" }
}
```

### 4. Criar um Fornecedor

```sql
{
  "PK": { "S": "Supplier#051728cf88c143b5814ec9706ab61ddb" },
  "SK": { "S": "META" },
  "entityType": { "S": "supplier" },
  "supplierId": { "S": "051728cf88c143b5814ec9706ab61ddb" },
  "supplierName": { "S": "Test Supplier" },
  "createdAt": { "S": "2026-02-20T00:00:00Z" }
}
```

### 5. Vincular Supplier ao Produto-SKU

```sql
{
  "PK": { "S": "Product#3a60aa94111c491c97c293f990c0eddb" },
  "SK": { "S": "Sku#CROSS_P#Supplier#051728cf88c143b5814ec9706ab61ddb" },
  "entity_type": { "S": "product_sku_supplier" },
  "productId": { "S": "3a60aa94111c491c97c293f990c0eddb" },
  "product_id": { "S": "3a60aa94111c491c97c293f990c0eddb" },
  "sku": { "S": "CROSS_P" },
  "supplier_id": { "S": "051728cf88c143b5814ec9706ab61ddb" },
  "sku_supplier": { "S": "CROSS_P" },
  "price": { "N": "49.90" },
  "quantity": { "N": "80" },
  "priority": { "N": "1" },
  "created_at": { "S": "2026-02-20T00:00:00Z" }
}
```

## Teste 1: ✅ Processamento com Sucesso

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

### Expected Response (200 OK):
```json
{
    "message": "Pedido processado com sucesso",
    "orderSn": "2501080NKAMXA8",
    "status": "READY_TO_SHIP",
    "shopId": 341431138,
    "updateTime": 1736323997
}
```

### Validação:
1. ✅ Verificar se `/logs` contém: `Order processed successfully`
2. ✅ Verificar DynamoDB:
   - `Product-Sku-Supplier` quantity reduzida
   - Novo registro em `Kardex#Sku#CROSS_P`
   - Novo registro em `PaymentQueue#Seller#...`

---

## Teste 2: ❌ Status Inválido

### Request:
```bash
curl -X POST http://localhost:5000/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "ordersn": "2501080NKAMXA9",
    "status": "UNPAID",
    "update_time": 1736323998,
    "shop_id": 341431138
  }'
```

### Expected Response (200 OK - mas não processa):
```json
{
    "message": "Pedido não foi processado",
    "details": "Status deve ser 'READY_TO_SHIP'",
    "orderSn": "2501080NKAMXA9",
    "status": "UNPAID",
    "shopId": 341431138
}
```

### Validação:
1. ✅ Log contém: `Order status is not READY_TO_SHIP`
2. ✅ DynamoDB não foi alterado
3. ✅ Status é 200 (não é erro, é comportamento esperado)

---

## Teste 3: ❌ OrderSn Vazio

### Request:
```bash
curl -X POST http://localhost:5000/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "ordersn": "",
    "status": "READY_TO_SHIP",
    "update_time": 1736323997,
    "shop_id": 341431138
  }'
```

### Expected Response (400 Bad Request):
```json
{
    "error": "OrderSn e Status são obrigatórios"
}
```

### Validação:
1. ✅ Status HTTP = 400
2. ✅ Log contém: `Missing required fields`

---

## Teste 4: ❌ ShopId Inválido

### Request:
```bash
curl -X POST http://localhost:5000/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "ordersn": "2501080NKAMXA8",
    "status": "READY_TO_SHIP",
    "update_time": 1736323997,
    "shop_id": 0
  }'
```

### Expected Response (400 Bad Request):
```json
{
    "error": "ShopId válido é obrigatório"
}
```

### Validação:
1. ✅ Status HTTP = 400
2. ✅ Log contém: `Invalid ShopId`

---

## Teste 5: ❌ Seller Não Encontrado

### Setup:
1. Usar shop_id que não existe no banco
   
### Request:
```bash
curl -X POST http://localhost:5000/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "ordersn": "2501080NKAMXAB",
    "status": "READY_TO_SHIP",
    "update_time": 1736323997,
    "shop_id": 999999999
  }'
```

### Expected Response (500 Internal Server Error):
```json
{
    "error": "Seller not found for shop ID 999999999"
}
```

### Validação:
1. ✅ Status HTTP = 500
2. ✅ Log contém: `Seller not found`
3. ✅ DynamoDB não foi alterado

---

## Teste 6: ✅ Múltiplos Fornecedores

### Setup:
1. Criar 2 fornecedores para o mesmo SKU
   
```sql
-- Fornecedor 1 (prioridade 1, quantidade 5)
{
  "PK": { "S": "Product#3a60aa94111c491c97c293f990c0eddb" },
  "SK": { "S": "Sku#CROSS_P#Supplier#supplier-001" },
  "entity_type": { "S": "product_sku_supplier" },
  "quantity": { "N": "5" },
  "priority": { "N": "1" },
  "price": { "N": "49.90" }
}

-- Fornecedor 2 (prioridade 1, quantidade 50)
{
  "PK": { "S": "Product#3a60aa94111c491c97c293f990c0eddb" },
  "SK": { "S": "Sku#CROSS_P#Supplier#supplier-002" },
  "entity_type": { "S": "product_sku_supplier" },
  "quantity": { "N": "50" },
  "priority": { "N": "1" },
  "price": { "N": "45.00" }
}
```

### Request (pedir 8 unidades):
Nota: A API lê da Shopee, então você precisa ter um pedido com 8 items naquele modelo_sku

```bash
curl -X POST http://localhost:5000/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "ordersn": "2501080NKAMXAC",
    "status": "READY_TO_SHIP",
    "update_time": 1736323997,
    "shop_id": 341431138
  }'
```

### Expected Behavior:
1. ✅ Fornecedor 1: reduz 5 unidades (5 - 5 = 0)
2. ✅ Fornecedor 2: reduz 3 unidades (50 - 3 = 47)
3. ✅ 2 registros no Kardex (um para cada fornecedor)
4. ✅ 2 registros no PaymentQueue (um para cada fornecedor)

### Validação:
```sql
-- Verificar Kardex
SELECT * FROM "catalog-core" 
WHERE PK = "Kardex#Sku#CROSS_P" 
ORDER BY SK DESC 
LIMIT 2;

-- Verificar PaymentQueue
SELECT * FROM "catalog-core" 
WHERE PK = "PaymentQueue#Seller#69611396-ee23-4a96-9161-7c9928679056" 
AND begins_with(SK, "PaymentStatus#Pending#Supplier#");
```

---

## Teste 7: ✅ Quantidade Exata

### Setup:
- Fornecedor tem exatamente 3 unidades
- Pedido pede 3 unidades

### Request:
```bash
curl -X POST http://localhost:5000/orders/process \
  -H "Content-Type: application/json" \
  -d '{
    "ordersn": "2501080NKAMXAD",
    "status": "READY_TO_SHIP",
    "update_time": 1736323997,
    "shop_id": 341431138
  }'
```

### Expected Behavior:
1. ✅ Fornecedor: quantidade reduzida para 0
2. ✅ Kardex: registrado com quantity = 3
3. ✅ PaymentQueue: criado com quantity = 3

---

## Checklist de Validação

Para cada teste bem-sucedido, verificar:

### Logs
- [ ] Mensagens apropriadas no console
- [ ] Nível de severidade correto (INFO para sucesso, WARNING para validações, ERROR para exceções)
- [ ] CorrelationId presente em todas as mensagens

### DynamoDB - Product-Sku-Supplier
- [ ] `quantity` reduzida pela quantidade vendida
- [ ] Outros campos não foram alterados
- [ ] `updated_at` foi atualizado (se houver)

### DynamoDB - Kardex
- [ ] Novo registro criado com SK = ULID
- [ ] `entity_type` = "kardex"
- [ ] `operation` = "remove"
- [ ] `quantity` = quantidade vendida
- [ ] `product_id` correto
- [ ] `sku` correto
- [ ] `supplier_id` correto
- [ ] `ordersn` correto
- [ ] `shop_id` correto
- [ ] `date` = timestamp atual em ISO 8601

### DynamoDB - PaymentQueue
- [ ] PK = "PaymentQueue#Seller#{seller_id}"
- [ ] SK contém todos os componentes (Pending, Supplier, Order, Sku)
- [ ] `entity_type` = "paymentQueue"
- [ ] `status` = "pending"
- [ ] `quantity` correto
- [ ] `value` = production_price * quantity
- [ ] `product_id` correto
- [ ] `sku` correto
- [ ] `supplier_id` correto
- [ ] `seller_id` correto
- [ ] `ordersn` correto
- [ ] `shop_id` correto
- [ ] `created_at` = timestamp atual

### Response HTTP
- [ ] Status code correto (200 para sucesso, 400 para validação, 500 para erro)
- [ ] Body contém campos esperados
- [ ] Sem erros de serialização JSON

---

## Troubleshooting

### Erro: "Seller not found for shop ID"
**Causa**: Seller não existe na tabela
**Solução**: Criar um seller com o shop_id correto (use GSI_SHOPID_LOOKUP para verificar)

### Erro: "No suppliers found for SKU"
**Causa**: Product-Sku-Supplier não existe ou foi deletado
**Solução**: Verificar se o registro existe no DynamoDB com:
```sql
SELECT * FROM "catalog-core" 
WHERE PK = "Product#..." AND begins_with(SK, "Sku#...");
```

### Erro: "Failed to get order detail from Shopee API"
**Causa**: 
- API da Shopee indisponível
- ShopId incorreto
- Access token expirado
**Solução**: 
- Verificar se a Shopee API está acessível
- Validar shop_id
- Checar logs da ShopeeApiService

### Kardex não criado
**Causa**: Exception em AddToKardexAsync
**Solução**: 
- Verificar logs para exceção
- Garantir que DynamoDbRepository.PutItemAsync funcionou
- Validar estrutura do item antes de inserir

### PaymentQueue não criado
**Causa**: Geralmente seller_id não encontrado
**Solução**:
- Verificar se o Seller existe
- Validar shop_id do Seller
- Garantir que GSI_SHOPID_LOOKUP está funcional

---

## Performance Test

### Request de Volume

```bash
# Gerar 100 pedidos em paralelo
for i in {1..100}; do
  curl -X POST http://localhost:5000/orders/process \
    -H "Content-Type: application/json" \
    -d "{
      \"ordersn\": \"order-$i\",
      \"status\": \"READY_TO_SHIP\",
      \"update_time\": 1736323997,
      \"shop_id\": 341431138
    }" &
done
wait

# Verificar tempo de resposta no console
```

### Métricas Esperadas
- Tempo médio por pedido: < 2 segundos
- Taxa de sucesso: > 95% (alguns podem falhar se fornecedor não tem estoque)
- Erro de throttling: 0

---

## Cleanup

Para limpar dados de teste:

```sql
-- Deletar Kardex
DELETE FROM "catalog-core" 
WHERE PK = "Kardex#Sku#CROSS_P";

-- Deletar PaymentQueue
DELETE FROM "catalog-core" 
WHERE PK = "PaymentQueue#Seller#69611396-ee23-4a96-9161-7c9928679056";

-- Restaurar Product-Sku-Supplier
UPDATE "catalog-core" 
SET quantity = 80 
WHERE PK = "Product#3a60aa94111c491c97c293f990c0eddb" 
AND SK = "Sku#CROSS_P#Supplier#051728cf88c143b5814ec9706ab61ddb";
```

