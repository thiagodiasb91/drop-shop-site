# SKU Controller - Documentação de Endpoints

## Visão Geral

O `SkuController` gerencia Stock Keeping Units (SKUs) - variações de produtos com atributos como tamanho, cor e quantidade em estoque.

## Estrutura de Dados DynamoDB

```json
{
  "PK": "Product#{productId}",
  "SK": "Sku#{skuCode}",
  "productId": "3a60aa94111c491c97c293f990c0eddb",
  "sku": "CROSS_P",
  "size": "P",
  "color": "Azul",
  "quantity": 80,
  "entityType": "sku",
  "created_at": "2026-02-06T10:30:00.000Z",
  "updated_at": null
}
```

## Endpoints

### 1. Obter SKU Específico
**GET** `/products/{productId}/skus/{sku}`

- **Parâmetros:**
  - `productId` (path): ID do produto
  - `sku` (path): Código do SKU

- **Response (200 OK):**
```json
{
  "sku": "CROSS_P",
  "product_id": "3a60aa94111c491c97c293f990c0eddb",
  "entity_type": "sku",
  "size": "P",
  "color": "Azul",
  "quantity": 80,
  "created_at": "2026-02-06T10:30:00Z",
  "updated_at": null
}
```

---

### 2. Listar SKUs de um Produto
**GET** `/products/{productId}/skus`

- **Parâmetros:**
  - `productId` (path): ID do produto

- **Response (200 OK):**
```json
{
  "total": 2,
  "items": [
    {
      "sku": "CROSS_P",
      "size": "P",
      "color": "Azul",
      "quantity": 80,
      "created_at": "2026-02-06T10:30:00Z"
    },
    {
      "sku": "CROSS_M",
      "size": "M",
      "color": "Azul",
      "quantity": 120,
      "created_at": "2026-02-06T10:32:00Z"
    }
  ]
}
```

---

### 3. Criar SKU
**POST** `/products/{productId}/skus`

- **Parâmetros:**
  - `productId` (path): ID do produto

- **Request Body:**
```json
{
  "productId": "3a60aa94111c491c97c293f990c0eddb",
  "sku": "CROSS_P",
  "size": "P",
  "color": "Azul",
  "quantity": 80
}
```

- **Response (201 Created):**
```json
{
  "sku": "CROSS_P",
  "product_id": "3a60aa94111c491c97c293f990c0eddb",
  "entity_type": "sku",
  "size": "P",
  "color": "Azul",
  "quantity": 80,
  "created_at": "2026-02-06T10:30:00Z",
  "updated_at": null
}
```

---

### 4. Atualizar SKU
**PUT** `/products/{productId}/skus/{sku}`

- **Parâmetros:**
  - `productId` (path): ID do produto
  - `sku` (path): Código do SKU

- **Request Body (campos opcionais):**
```json
{
  "size": "M",
  "color": "Vermelho",
  "quantity": 100
}
```

- **Response (200 OK):**
```json
{
  "sku": "CROSS_P",
  "product_id": "3a60aa94111c491c97c293f990c0eddb",
  "entity_type": "sku",
  "size": "M",
  "color": "Vermelho",
  "quantity": 100,
  "created_at": "2026-02-06T10:30:00Z",
  "updated_at": "2026-02-06T11:00:00Z"
}
```

---

### 5. Deletar SKU
**DELETE** `/products/{productId}/skus/{sku}`

- **Parâmetros:**
  - `productId` (path): ID do produto
  - `sku` (path): Código do SKU

- **Response (204 No Content):**
  Sem body

---

### 6. Atualizar Quantidade (Patch)
**PATCH** `/products/{productId}/skus/{sku}/quantity`

- **Parâmetros:**
  - `productId` (path): ID do produto
  - `sku` (path): Código do SKU
  - `quantity` (query): Nova quantidade

- **Response (200 OK):**
```json
{
  "sku": "CROSS_P",
  "product_id": "3a60aa94111c491c97c293f990c0eddb",
  "entity_type": "sku",
  "size": "P",
  "color": "Azul",
  "quantity": 60,
  "created_at": "2026-02-06T10:30:00Z",
  "updated_at": "2026-02-06T11:05:00Z"
}
```

---

## Códigos de Status HTTP

| Status | Descrição |
|--------|-----------|
| 200 OK | Operação bem-sucedida |
| 201 Created | SKU criado com sucesso |
| 204 No Content | Exclusão bem-sucedida |
| 400 Bad Request | Validação falhou |
| 404 Not Found | SKU ou Produto não encontrado |
| 500 Internal Server Error | Erro no servidor |

---

## Componentes Criados

### Domain
- **SkuDomain.cs**: Entidade de domínio para SKU
- **SkuMapper**: Mapper para converter DynamoDB items para SkuDomain

### Repository
- **SkuRepository.cs**: Repositório com operações CRUD e queries

### Controller
- **SkuController.cs**: Endpoints REST da API

### Requests
- **CreateSkuRequest.cs**: DTO para criação de SKU
- **UpdateSkuRequest.cs**: DTO para atualização de SKU

### Responses
- **SkuResponse.cs**: DTO de resposta completa
- **SkuItemResponse.cs**: DTO de resposta simplificada
- **SkuListResponse.cs**: DTO com paginação
- **SkuResponseMapper**: Mapper para converter SkuDomain em responses

### Configuration
- Registrado no `Program.cs` como serviço scoped: `builder.Services.AddScoped<SkuRepository>();`

---

## Exemplo de Uso cURL

### Criar SKU
```bash
curl -X POST http://localhost:5000/products/3a60aa94111c491c97c293f990c0eddb/skus \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "3a60aa94111c491c97c293f990c0eddb",
    "sku": "CROSS_P",
    "size": "P",
    "color": "Azul",
    "quantity": 80
  }'
```

### Obter SKU
```bash
curl http://localhost:5000/products/3a60aa94111c491c97c293f990c0eddb/skus/CROSS_P
```

### Listar SKUs do Produto
```bash
curl http://localhost:5000/products/3a60aa94111c491c97c293f990c0eddb/skus
```

### Atualizar Quantidade
```bash
curl -X PATCH "http://localhost:5000/products/3a60aa94111c491c97c293f990c0eddb/skus/CROSS_P/quantity?quantity=60"
```

### Deletar SKU
```bash
curl -X DELETE http://localhost:5000/products/3a60aa94111c491c97c293f990c0eddb/skus/CROSS_P
```
