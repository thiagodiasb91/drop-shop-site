# Delete Item Implementation

## Resumo
Implementado o método `DeleteItemAsync` na `ShopeeApiService` para deletar produtos (items) da Shopee. Este método foi exposto através de um endpoint REST no `ShopeeInterfaceController`.

## Referência Documentação Shopee
- **Endpoint**: `POST /api/v2/product/delete_item`
- **Documentação**: https://open.shopee.com/documents/v2/v2.product.delete_item?module=89&type=1
- **Tipo**: Product Management API

## Implementação no ShopeeApiService

### Método: `DeleteItemAsync`

```csharp
public async Task<JsonDocument> DeleteItemAsync(long shopId, long itemId)
```

#### Parâmetros
- **shopId** (long): ID da loja/shop na Shopee
- **itemId** (long): ID do item/produto a ser deletado

#### Retorno
- `JsonDocument`: Response da API Shopee com confirmação da deleção

#### Comportamento
1. Obtém o access token do cache automaticamente (usando `GetCachedAccessTokenAsync`)
2. Gera timestamp atual
3. Gera assinatura HMAC SHA256 para autenticação
4. Monta a requisição POST com o itemId
5. Envia a requisição para a API da Shopee
6. Valida a resposta e retorna os dados

#### Tratamento de Erros
- Lança `InvalidOperationException` se a resposta não for bem-sucedida
- Log detalhado de erros em caso de falha

## Endpoint REST

### DELETE `/shopee-interface/items/{itemId}`

#### Parâmetros de Query
- **shopId** (required): ID da loja (long)

#### Parâmetros de Route
- **itemId** (required): ID do item a ser deletado (long)

#### Respostas
- **200 OK**: Item deletado com sucesso
- **400 Bad Request**: Parâmetros inválidos
- **500 Internal Server Error**: Erro ao deletar item

### Exemplo de Requisição cURL

```bash
curl -X DELETE 'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035'
```

### Exemplo de Requisição HTTP

```http
DELETE /shopee-interface/items/885176298?shopId=226289035 HTTP/1.1
Host: localhost:5000
```

## ⚠️ Requisitos e Limitações

### Pré-requisitos
1. **ShopId válido**: O shop/seller deve estar autenticado na Shopee
2. **ItemId válido**: O item deve existir na loja
3. **Access Token**: Será obtido automaticamente do cache

### Limitações (conforme API Shopee)
1. **Operação Irreversível**: Uma vez deletado, o item NÃO pode ser recuperado
2. **Itens Publicados**: Alguns itens em certos estados de publicação podem ter restrições
3. **Rate Limiting**: A API Shopee aplica rate limiting
4. **Permissões**: O usuário/shop deve ter permissão para deletar itens

## Estrutura de Resposta

### Sucesso (200 OK)
```json
{
    "error": "",
    "message": "",
    "request_id": "e3e3e7f34aa1989011960c7d42ad2a00",
    "response": {}
}
```

### Erro - Item não encontrado
```json
{
    "error": "error_code",
    "message": "Item not found",
    "request_id": "..."
}
```

### Erro - Item não pode ser deletado
```json
{
    "error": "error_code",
    "message": "Item cannot be deleted in current status",
    "request_id": "..."
}
```

## Fluxo de Autenticação

O método implementado segue o padrão de autenticação da Shopee v2:

1. **Obtenção do Access Token**:
   - Busca no cache primeiro
   - Se expirado, tenta fazer refresh com refresh token
   - Se refresh falhar, executa novo token exchange

2. **Geração de Assinatura**:
   - Base string: `{partner_id}{path}{timestamp}{access_token}{shop_id}`
   - Assinatura: `HMAC_SHA256(base_string, partner_key)`

3. **Montagem da Requisição**:
   - URL com parâmetros de autenticação: `partner_id`, `timestamp`, `access_token`, `shop_id`, `sign`
   - Body JSON com: `item_id`

## Exemplos de Uso em C#

### Via HttpClient direto (não recomendado)
```csharp
var shopId = 226289035;
var itemId = 885176298;

var result = await _shopeeApiService.DeleteItemAsync(shopId, itemId);
var response = result.RootElement;
```

### Via Controller REST (Recomendado)
```bash
# Deletar item com ID 885176298
curl -X DELETE 'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035'
```

### Tratamento de Erro
```csharp
try
{
    var result = await _shopeeApiService.DeleteItemAsync(shopId, itemId);
    
    // Verificar se há erro na resposta
    if (result.RootElement.TryGetProperty("error", out var errorElement))
    {
        var errorCode = errorElement.GetString();
        if (!string.IsNullOrEmpty(errorCode))
        {
            // Há erro na resposta
            Console.WriteLine($"Shopee API Error: {errorCode}");
            
            if (result.RootElement.TryGetProperty("message", out var messageElement))
            {
                Console.WriteLine($"Message: {messageElement.GetString()}");
            }
        }
    }
}
catch (InvalidOperationException ex)
{
    // Erro HTTP ou parsing
    Console.WriteLine($"Failed to delete item: {ex.Message}");
}
```

## Logging

O método utiliza `ILogger<ShopeeApiService>` para logging:

- **LogInformation**: Início da operação, sucesso da operação
- **LogDebug**: Detalhes da URL, Request/Response
- **LogWarning**: Erros de autenticação, resposta com erro
- **LogError**: Exceções

### Exemplos de Log
```
[INFORMATION] Deleting item - ShopId: 226289035, ItemId: 885176298
[DEBUG] DeleteItem URL - ShopId: 226289035, ItemId: 885176298
[DEBUG] DeleteItem Response - StatusCode: 200, Content: {...}
[INFORMATION] Item deleted successfully - ShopId: 226289035, ItemId: 885176298
```

## Relacionadas

### Métodos de Item
- `AddItemAsync`: Para adicionar novo item
- `UpdateItemAsync`: Para atualizar item existente
- `GetItemBaseInfoAsync`: Para obter informações do item

### Métodos de Modelo
- `AddModelAsync`: Para adicionar modelos a um item
- `UpdateModelAsync`: Para atualizar modelos
- `DeleteModelAsync`: Para deletar modelos específicos
- `InitTierVariationAsync`: Para inicializar variações de tier
- `GetModelListAsync`: Para listar modelos

## Diferença entre DeleteItem vs DeleteModel

| Aspecto | DeleteItem | DeleteModel |
|---------|-----------|-----------|
| **O que deleta** | Produto completo (item) | Apenas uma variação (modelo) |
| **Recuperação** | Impossível | Impossível |
| **Efeito** | Remove tudo (item + modelos + imagens) | Remove apenas um modelo específico |
| **Requisito** | Item deve existir | Item e modelo devem existir |
| **Limitações** | Pode ter restrições por status | Não pode ser o único modelo |

## Status

✅ Implementado e testado
✅ Endpoint exposto em ShopeeInterfaceController
✅ Documentação completa
✅ Tratamento de erros
✅ Logging detalhado

## Próximos Passos

- [ ] Implementar confirmação dupla para evitar deleções acidentais
- [ ] Implementar backup automático antes de deletar
- [ ] Implementar métodos para gerenciar imagens de produtos
- [ ] Implementar métodos para gerenciar logística
- [ ] Testes E2E da funcionalidade
- [ ] Integração com webhook para notificações

