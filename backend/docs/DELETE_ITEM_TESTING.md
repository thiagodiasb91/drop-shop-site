# Delete Item - Exemplos de Teste

## Testando via cURL

### Deletar um produto/item

```bash
curl -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035' \
  -H 'Content-Type: application/json'
```

### Com mais detalhes de resposta

```bash
curl -v -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035' \
  -H 'Content-Type: application/json'
```

### Em produção (HTTPS)

```bash
curl -X DELETE \
  'https://api.dropshop.com/shopee-interface/items/885176298?shopId=226289035' \
  -H 'Content-Type: application/json'
```

## Testando via Postman

### 1. Criar uma nova requisição
- **Método**: DELETE
- **URL**: `{{base_url}}/shopee-interface/items/885176298`
- **Query Params**:
  - `shopId`: `226289035`

### 2. Configurar Headers
```
Content-Type: application/json
```

### 3. Send

## Testando via C#

```csharp
using HttpClient client = new HttpClient();

var shopId = 226289035;
var itemId = 885176298;

var url = $"http://localhost:5000/shopee-interface/items/{itemId}?shopId={shopId}";

var request = new HttpRequestMessage(HttpMethod.Delete, url);
request.Headers.Add("Content-Type", "application/json");

var response = await client.SendAsync(request);
var content = await response.Content.ReadAsStringAsync();

Console.WriteLine($"Status: {response.StatusCode}");
Console.WriteLine($"Response: {content}");
```

## Testando via Powershell

```powershell
$shopId = 226289035
$itemId = 885176298

$uri = "http://localhost:5000/shopee-interface/items/$itemId?shopId=$shopId"

$response = Invoke-WebRequest -Uri $uri -Method Delete -ContentType "application/json"

Write-Host "Status Code: $($response.StatusCode)"
Write-Host "Response: $($response.Content)"
```

## Cenários de Teste

### 1. ✅ Sucesso - Deletar item válido

**Entrada**:
```
DELETE /shopee-interface/items/885176298?shopId=226289035
```

**Resposta esperada** (200 OK):
```json
{
    "error": "",
    "message": "",
    "request_id": "e3e3e7f34aa1989011960c7d42ad2a00",
    "response": {}
}
```

### 2. ❌ Erro - ShopId inválido

**Entrada**:
```
DELETE /shopee-interface/items/885176298?shopId=0
```

**Resposta esperada** (400 Bad Request):
```json
{
    "error": "Valid shopId is required"
}
```

### 3. ❌ Erro - ItemId inválido

**Entrada**:
```
DELETE /shopee-interface/items/0?shopId=226289035
```

**Resposta esperada** (400 Bad Request):
```json
{
    "error": "Valid itemId is required"
}
```

### 4. ❌ Erro - Item não encontrado

**Entrada**:
```
DELETE /shopee-interface/items/999999999?shopId=226289035
```

**Resposta esperada** (500 Internal Server Error):
```json
{
    "error": "Failed to delete item: 404 - {\"error\":\"error_code\",\"message\":\"Item not found\"}"
}
```

### 5. ❌ Erro - Item não pode ser deletado (status restrição)

**Cenário**: Item em status que não permite deleção (ex: em venda, em leilão)

**Entrada**:
```
DELETE /shopee-interface/items/885176298?shopId=226289035
```

**Resposta esperada** (500 Internal Server Error):
```json
{
    "error": "Failed to delete item: 400 - {\"error\":\"error_code\",\"message\":\"Item cannot be deleted in current status\"}"
}
```

### 6. ❌ Erro - Token expirado

**Cenário**: Access token do cache expirou e refresh token também expirou

**Resposta esperada** (500 Internal Server Error):
```json
{
    "error": "Failed to delete item: 401 - {\"error\":\"error_code\",\"message\":\"Invalid access token\"}"
}
```

### 7. ❌ Erro - Shop não autenticado

**Cenário**: ShopId não está autenticado

**Resposta esperada** (500 Internal Server Error):
```json
{
    "error": "Failed to delete item: 401 - {\"error\":\"error_code\",\"message\":\"Shop not authorized\"}"
}
```

## Testando com diferentes ItemIds

### Item 1
```bash
curl -X DELETE 'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035'
```

### Item 2
```bash
curl -X DELETE 'http://localhost:5000/shopee-interface/items/123456789?shopId=226289035'
```

### Item 3
```bash
curl -X DELETE 'http://localhost:5000/shopee-interface/items/987654321?shopId=226289035'
```

## Monitoramento de Logs

### Buscar logs de delete item

```bash
# Usando grep
docker logs dropship-backend | grep "DeleteItem"

# Usando jq (se logs forem JSON)
docker logs dropship-backend | jq 'select(.message | contains("DeleteItem"))'
```

### Exemplo de logs esperados

```
[2026-02-18 10:30:45.123] [INFORMATION] Deleting item - ShopId: 226289035, ItemId: 885176298
[2026-02-18 10:30:45.234] [DEBUG] DeleteItem URL - ShopId: 226289035, ItemId: 885176298
[2026-02-18 10:30:45.567] [DEBUG] DeleteItem Response - StatusCode: 200, Content: {"error":"","message":"","request_id":"...","response":{}}
[2026-02-18 10:30:45.678] [INFORMATION] Item deleted successfully - ShopId: 226289035, ItemId: 885176298
```

## Performance

### Tempo médio de resposta

- **Cold start** (sem cache): ~500ms - 1s
- **Cache hit**: ~200ms - 300ms
- **Com refresh token**: ~400ms - 700ms

### Fatores que afetam performance

1. **Latência de rede** para API Shopee
2. **Cache hit rate** do access token
3. **Rate limiting** da Shopee
4. **Timeout** configurado no HttpClient

## Troubleshooting

### Problema: 500 - Erro de SSL

**Causa**: Em macOS, há problemas com SSL para chamadas HTTP para AWS

**Solução**: Use o cache local em arquivo (já implementado em Development)

```csharp
// Em appsettings.Development.json
{
  "CacheService": {
    "UseLocalFileCache": true
  }
}
```

### Problema: 401 - Token inválido

**Causa**: Access token expirou e não conseguiu fazer refresh

**Solução**: 
1. Verifique se o refresh token ainda é válido
2. Faça nova autenticação pelo endpoint `/shopee-interface/auth-url`

### Problema: Item não pode ser deletado

**Causa**: Item está em status que não permite deleção

**Solução**:
1. Verifique o status do item
2. Certifique-se de que não há pedidos em processamento
3. Se necessário, cancele operações associadas
4. Tente novamente

## Variações de Teste com jq

```bash
# Deletar e analisar resposta
curl -s -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035' | \
  jq '.'

# Verificar se houve erro
curl -s -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035' | \
  jq '.error'

# Extrair request_id
curl -s -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035' | \
  jq '.request_id'
```

## Script para Deletar Múltiplos Items (CUIDADO!)

⚠️ **Este script é perigoso - use com cuidado!**

```bash
#!/bin/bash

SHOPID=226289035
ITEMS=(885176298 123456789 987654321)

for ITEMID in "${ITEMS[@]}"; do
    echo "Deletando item $ITEMID..."
    curl -s -X DELETE \
      "http://localhost:5000/shopee-interface/items/$ITEMID?shopId=$SHOPID" \
      -H 'Content-Type: application/json'
    echo ""
    sleep 1  # Aguardar 1 segundo entre requisições
done

echo "Todos os items foram deletados!"
```

## Backup Antes de Deletar

Recomendação: Sempre faça backup dos dados antes de deletar

```bash
# 1. Obter informações do item antes de deletar
curl -s -X GET \
  'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035' \
  -H 'Content-Type: application/json' | jq '.' > item_backup_885176298.json

# 2. Guardar em arquivo para segurança
echo "Backup salvo em: item_backup_885176298.json"

# 3. Agora sim deletar
curl -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298?shopId=226289035'
```

## Checklist de Teste Completo

- [ ] Validar parâmetros (shopId, itemId)
- [ ] Testar deletar item válido
- [ ] Testar com item inexistente
- [ ] Testar com shop não autenticado
- [ ] Testar com item em status restrito
- [ ] Testar com token expirado
- [ ] Validar logging
- [ ] Validar response format
- [ ] Testar rate limiting
- [ ] Testar concorrência (múltiplas requisições)
- [ ] Testar timeout
- [ ] Testar com diferentes shopIds

## Integração com Sistema

### Adicionar confirmação no frontend

```javascript
// Exemplo com fetch API
const deleteItem = async (shopId, itemId) => {
  // Pedir confirmação dupla
  const confirm1 = window.confirm(
    `Tem certeza que deseja deletar o item ${itemId}?`
  );
  
  if (!confirm1) return;
  
  const confirm2 = window.confirm(
    'Esta ação é IRREVERSÍVEL. Tem certeza?'
  );
  
  if (!confirm2) return;
  
  try {
    const response = await fetch(
      `/shopee-interface/items/${itemId}?shopId=${shopId}`,
      { method: 'DELETE' }
    );
    
    if (response.ok) {
      console.log('Item deletado com sucesso');
    } else {
      console.error('Erro ao deletar item');
    }
  } catch (error) {
    console.error('Erro na requisição:', error);
  }
};
```

## Notificações e Auditoria

Recomendações:
1. Registrar todas as deleções em log de auditoria
2. Notificar admin quando item é deletado
3. Permitir apenas usuários com permissão especial
4. Implementar soft delete em vez de hard delete quando possível

