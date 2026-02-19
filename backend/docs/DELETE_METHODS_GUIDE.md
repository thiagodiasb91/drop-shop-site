# 📋 Guia Completo - Métodos de Deletação Shopee

## Resumo de Implementação

Foram implementados **2 métodos de deletação** na Shopee API:

| # | Método | Endpoint | O que deleta | Referência |
|---|--------|----------|--------------|-----------|
| 1 | `DeleteModelAsync` | `DELETE /shopee-interface/items/{itemId}/models/{modelId}` | Uma variação (cor/tamanho) | [Docs Shopee](https://open.shopee.com/documents/v2/v2.product.delete_model?module=89&type=1) |
| 2 | `DeleteItemAsync` | `DELETE /shopee-interface/items/{itemId}` | Produto inteiro com variações | [Docs Shopee](https://open.shopee.com/documents/v2/v2.product.delete_item?module=89&type=1) |

## 🏗️ Arquitetura Implementada

```
ShopeeApiService.cs
├── DeleteModelAsync(shopId, itemId, modelId)
│   └── DELETE /api/v2/product/delete_model
│
└── DeleteItemAsync(shopId, itemId)
    └── DELETE /api/v2/product/delete_item

ShopeeInterfaceController.cs
├── DeleteModel([FromQuery] shopId, [FromRoute] itemId, [FromRoute] modelId)
│   └── DELETE /shopee-interface/items/{itemId}/models/{modelId}?shopId={shopId}
│
└── DeleteItem([FromQuery] shopId, [FromRoute] itemId)
    └── DELETE /shopee-interface/items/{itemId}?shopId={shopId}
```

## 🎯 Casos de Uso

### Caso 1: Remover uma Variação Específica

**Cenário**: Você tem um produto com cores: Azul, Vermelho, Amarelo
Deseja remover apenas a cor Vermelho

```bash
# 1. Listar modelos do item
GET /shopee-interface/items/885176298/models?shopId=226289035

# Resposta: 
# - Model ID 1: Azul
# - Model ID 2: Vermelho  <- Vamos deletar este
# - Model ID 3: Amarelo

# 2. Deletar apenas o modelo vermelho
DELETE /shopee-interface/items/885176298/models/9250789027?shopId=226289035

# Resultado: Produto ainda existe com Azul e Amarelo
```

### Caso 2: Remover Produto Inteiro

**Cenário**: Você quer descontinuar um produto completamente
Retirar da venda junto com todas suas variações

```bash
# Deletar todo o produto
DELETE /shopee-interface/items/885176298?shopId=226289035

# Resultado: Produto e todas variações (Azul, Vermelho, Amarelo) são removidos
```

## 📊 Matriz Comparativa

### Funcionalidades

| Funcionalidade | DeleteModel | DeleteItem |
|---|---|---|
| **Autentica automaticamente** | ✅ Sim | ✅ Sim |
| **Obtém token do cache** | ✅ Sim | ✅ Sim |
| **Gera assinatura HMAC** | ✅ Sim | ✅ Sim |
| **Registra logs** | ✅ Sim | ✅ Sim |
| **Valida parâmetros** | ✅ Sim | ✅ Sim |
| **Tratamento de erro** | ✅ Sim | ✅ Sim |

### Limitações

| Limitação | DeleteModel | DeleteItem |
|---|---|---|
| **Não pode deletar último modelo** | ⚠️ Sim | ❌ Não |
| **Operação irreversível** | ✅ Sim | ✅ Sim |
| **Restrições por status** | ⚠️ Sim | ⚠️ Sim |
| **Rate limiting** | ⚠️ Sim | ⚠️ Sim |

## 🔄 Fluxo Completo de Deletação

### Passo 1: Verificar acesso
```bash
# Obter informações da loja para confirmar autenticação
GET /shopee-interface/shop-info?shopId=226289035
```

### Passo 2: Listar produtos (se necessário)
```bash
# Obter lista de itens
GET /shopee-interface/items?shopId=226289035
```

### Passo 3: Deletar variação ou produto
```bash
# Opção A: Deletar apenas uma variação
DELETE /shopee-interface/items/{itemId}/models/{modelId}?shopId=226289035

# Opção B: Deletar produto inteiro
DELETE /shopee-interface/items/{itemId}?shopId=226289035
```

### Passo 4: Validar resultado
```bash
# Tentar obter informações do item deletado (deve retornar 404)
GET /shopee-interface/items/{itemId}?shopId=226289035
```

## 📈 Exemplo Prático Passo-a-Passo

### Cenário Real: Descontinuar Camiseta Azul (apenas a cor)

```bash
# 1. Listar todas as variações da camiseta
curl -X GET 'http://localhost:5000/shopee-interface/items/885176298/models?shopId=226289035'

# Resposta:
# {
#   "response": [
#     { "model_id": 9250789027, "model_sku": "CAMISETA-AZUL", "price": 50.00 },
#     { "model_id": 9250789028, "model_sku": "CAMISETA-VERMELHO", "price": 50.00 },
#     { "model_id": 9250789029, "model_sku": "CAMISETA-AMARELO", "price": 50.00 }
#   ]
# }

# 2. Deletar apenas a camiseta azul
curl -X DELETE 'http://localhost:5000/shopee-interface/items/885176298/models/9250789027?shopId=226289035'

# Resposta:
# {
#   "error": "",
#   "message": "",
#   "request_id": "abc123..."
# }

# 3. Verificar resultado (listar novamente)
curl -X GET 'http://localhost:5000/shopee-interface/items/885176298/models?shopId=226289035'

# Resposta agora só tem vermelho e amarelo:
# {
#   "response": [
#     { "model_id": 9250789028, "model_sku": "CAMISETA-VERMELHO", "price": 50.00 },
#     { "model_id": 9250789029, "model_sku": "CAMISETA-AMARELO", "price": 50.00 }
#   ]
# }
```

### Cenário Real: Descontinuar Camiseta Inteira

```bash
# 1. Deletar produto inteiro (com TODAS as cores)
curl -X DELETE 'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035'

# Resposta:
# {
#   "error": "",
#   "message": "",
#   "request_id": "def456..."
# }

# 2. Verificar resultado (tentar obter item deletado)
curl -X GET 'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035'

# Resposta: 404 - Item not found
# {
#   "error": "error_code",
#   "message": "Item not found"
# }
```

## 🛡️ Boas Práticas

### 1. Sempre Fazer Backup Antes

```bash
# Salvar dados do item antes de deletar
curl -s 'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035' \
  | jq '.' > backup_item_885176298.json
```

### 2. Confirmar Duplo no Frontend

```javascript
const deleteItem = async (itemId) => {
  // Primeira confirmação
  if (!confirm(`Deletar item ${itemId}?`)) return;
  
  // Segunda confirmação
  if (!confirm('Esta ação é IRREVERSÍVEL. Tem certeza?')) return;
  
  // Apenas então deletar
  await fetch(`/shopee-interface/items/${itemId}`, { method: 'DELETE' });
};
```

### 3. Registrar Auditoria

```csharp
// Em uma classe de serviço
public async Task<JsonDocument> DeleteItemWithAuditAsync(long shopId, long itemId, string userId)
{
    // Registrar tentativa de deleção
    _auditLogger.Log(new AuditLog {
        Action = "DELETE_ITEM",
        ItemId = itemId,
        UserId = userId,
        Timestamp = DateTime.UtcNow,
        Status = "INITIATED"
    });
    
    try 
    {
        var result = await _shopeeApiService.DeleteItemAsync(shopId, itemId);
        
        // Registrar sucesso
        _auditLogger.Log(new AuditLog {
            Action = "DELETE_ITEM",
            ItemId = itemId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Status = "SUCCESS"
        });
        
        return result;
    }
    catch (Exception ex)
    {
        // Registrar falha
        _auditLogger.Log(new AuditLog {
            Action = "DELETE_ITEM",
            ItemId = itemId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Status = "FAILED",
            ErrorMessage = ex.Message
        });
        throw;
    }
}
```

### 4. Usar Transações em Lote

```csharp
public async Task<List<JsonDocument>> DeleteMultipleItemsAsync(
    long shopId, 
    List<long> itemIds, 
    Func<long, Task> onItemDeleted)
{
    var results = new List<JsonDocument>();
    
    foreach (var itemId in itemIds)
    {
        try
        {
            var result = await _shopeeApiService.DeleteItemAsync(shopId, itemId);
            results.Add(result);
            
            // Callback para notificar sucesso
            await onItemDeleted(itemId);
            
            // Aguardar entre requisições (respeitar rate limit)
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar item {ItemId}", itemId);
        }
    }
    
    return results;
}
```

## 📞 Suporte a Tipos de Erro

### Erros Comuns e Soluções

| Erro | Causa | Solução |
|---|---|---|
| `400 Bad Request` | Parâmetros inválidos | Verificar shopId, itemId, modelId |
| `401 Unauthorized` | Token expirado | Fazer nova autenticação |
| `404 Not Found` | Item/modelo não existe | Verificar se existe antes de deletar |
| `Item cannot be deleted in current status` | Item em status restrito | Aguardar ou modificar status |
| `Cannot delete the last model` | Deletando único modelo | Deletar item inteiro em vez disso |

## 🚀 Próximas Implementações

- [ ] Soft delete (marcar como deletado sem remover)
- [ ] Undelete (restaurar item deletado - se Shopee suportar)
- [ ] Batch delete com retry automático
- [ ] Webhook notifications para deleções
- [ ] Relacionamento com pedidos/estoque

## 📚 Referência Rápida

### DeleteModel
```
DELETE /shopee-interface/items/{itemId}/models/{modelId}?shopId={shopId}

Exemplo:
DELETE /shopee-interface/items/885176298/models/9250789027?shopId=226289035
```

### DeleteItem
```
DELETE /shopee-interface/items/{itemId}?shopId={shopId}

Exemplo:
DELETE /shopee-interface/items/885176298?shopId=226289035
```

## ✅ Checklist de Implementação

- ✅ Método DeleteModelAsync implementado
- ✅ Método DeleteItemAsync implementado
- ✅ Endpoint DeleteModel exposto
- ✅ Endpoint DeleteItem exposto
- ✅ Validações implementadas
- ✅ Logging implementado
- ✅ Tratamento de erro implementado
- ✅ Documentação completa
- ✅ Exemplos de teste
- ✅ Guias de boas práticas

---

**Status**: ✅ Implementação Completa e Pronta para Uso
**Data**: 18/02/2026
**Versão**: 1.0

