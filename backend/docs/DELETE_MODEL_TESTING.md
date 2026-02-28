# Delete Model - Exemplos de Teste

## Testando via cURL

### Deletar um modelo específico

```bash
curl -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298/models/9250789027?shopId=226289035' \
  -H 'Content-Type: application/json'
```

### Com mais detalhes de resposta

```bash
curl -v -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298/models/9250789027?shopId=226289035' \
  -H 'Content-Type: application/json'
```

### Em produção (HTTPS)

```bash
curl -X DELETE \
  'https://api.dropshop.com/shopee-interface/items/885176298/models/9250789027?shopId=226289035' \
  -H 'Content-Type: application/json'
```

## Testando via Postman

### 1. Criar uma nova requisição
- **Método**: DELETE
- **URL**: `{{base_url}}/shopee-interface/items/885176298/models/9250789027`
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
var modelId = 9250789027;

var url = $"http://localhost:5000/shopee-interface/items/{itemId}/models/{modelId}?shopId={shopId}";

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
$modelId = 9250789027

$uri = "http://localhost:5000/shopee-interface/items/$itemId/models/$modelId?shopId=$shopId"

$response = Invoke-WebRequest -Uri $uri -Method Delete -ContentType "application/json"

Write-Host "Status Code: $($response.StatusCode)"
Write-Host "Response: $($response.Content)"
```

## Cenários de Teste

### 1. ✅ Sucesso - Deletar modelo válido

**Entrada**:
```
DELETE /shopee-interface/items/885176298/models/9250789027?shopId=226289035
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
DELETE /shopee-interface/items/885176298/models/9250789027?shopId=0
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
DELETE /shopee-interface/items/0/models/9250789027?shopId=226289035
```

**Resposta esperada** (400 Bad Request):
```json
{
    "error": "Valid itemId is required"
}
```

### 4. ❌ Erro - ModelId inválido

**Entrada**:
```
DELETE /shopee-interface/items/885176298/models/0?shopId=226289035
```

**Resposta esperada** (400 Bad Request):
```json
{
    "error": "Valid modelId is required"
}
```

### 5. ❌ Erro - Modelo não encontrado

**Entrada**:
```
DELETE /shopee-interface/items/885176298/models/999999999?shopId=226289035
```

**Resposta esperada** (500 Internal Server Error):
```json
{
    "error": "Failed to delete model: 404 - {\"error\":\"error_code\",\"message\":\"Model not found\"}"
}
```

### 6. ❌ Erro - Último modelo do item

**Cenário**: Item com apenas um modelo

**Entrada**:
```
DELETE /shopee-interface/items/885176298/models/9250789027?shopId=226289035
```

**Resposta esperada** (500 Internal Server Error):
```json
{
    "error": "Failed to delete model: 400 - {\"error\":\"error_code\",\"message\":\"Cannot delete the last model of an item\"}"
}
```

### 7. ❌ Erro - Token expirado

**Cenário**: Access token do cache expirou e refresh token também expirou

**Resposta esperada** (500 Internal Server Error):
```json
{
    "error": "Failed to delete model: 401 - {\"error\":\"error_code\",\"message\":\"Invalid access token\"}"
}
```

## Testando com diferentes ShopIds

### Shop 1
```bash
curl -X DELETE 'http://localhost:5000/shopee-interface/items/885176298/models/9250789027?shopId=226289035'
```

### Shop 2
```bash
curl -X DELETE 'http://localhost:5000/shopee-interface/items/123456789/models/987654321?shopId=987654321'
```

## Monitoramento de Logs

### Buscar logs de delete model

```bash
# Usando grep
docker logs dropship-backend | grep "DeleteModel"

# Usando jq (se logs forem JSON)
docker logs dropship-backend | jq 'select(.message | contains("DeleteModel"))'
```

### Exemplo de logs esperados

```
[2026-02-18 10:30:45.123] [INFORMATION] Deleting model - ShopId: 226289035, ItemId: 885176298, ModelId: 9250789027
[2026-02-18 10:30:45.234] [DEBUG] DeleteModel URL - ShopId: 226289035, ItemId: 885176298, ModelId: 9250789027
[2026-02-18 10:30:45.567] [DEBUG] DeleteModel Response - StatusCode: 200, Content: {"error":"","message":"","request_id":"...","response":{}}
[2026-02-18 10:30:45.678] [INFORMATION] Model deleted successfully - ShopId: 226289035, ItemId: 885176298, ModelId: 9250789027
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

### Problema: 400 - Modelo não encontrado

**Causa**: O modelId passado não existe no item

**Solução**:
1. Liste os modelos do item: `GET /shopee-interface/items/885176298/models?shopId=226289035`
2. Use um modelId válido da lista

## Variações de Teste com jq

```bash
# Deletar e analisar resposta
curl -s -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298/models/9250789027?shopId=226289035' | \
  jq '.'

# Verificar se houve erro
curl -s -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298/models/9250789027?shopId=226989035' | \
  jq '.error'

# Extrair request_id
curl -s -X DELETE \
  'http://localhost:5000/shopee-interface/items/885176298/models/9250789027?shopId=226289035' | \
  jq '.request_id'
```

## Checklist de Teste Completo

- [ ] Validar parâmetros (shopId, itemId, modelId)
- [ ] Testar deletar modelo válido
- [ ] Testar deletar último modelo (deve falhar)
- [ ] Testar com modelo inexistente
- [ ] Testar com item inexistente
- [ ] Testar com token expirado
- [ ] Testar com shop não autenticado
- [ ] Validar logging
- [ ] Validar response format
- [ ] Testar rate limiting
- [ ] Testar concorrência (múltiplas requisições)
- [ ] Testar timeout

