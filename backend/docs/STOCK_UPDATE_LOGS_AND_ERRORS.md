# Stock Update - Logs e Tratativa de Erros

## Visão Geral

O método `UpdateStockSupplier` na classe `StockServices` foi implementado com logging abrangente e tratativa de erros robusta para garantir rastreabilidade e recuperação de falhas.

## Estrutura de Logs

### 1. Log Inicial - Information
```csharp
[STOCK-UPDATE] Iniciando atualização de estoque - SupplierId: {SupplierId}, ProductId: {ProductId}, SKU: {SKU}, Quantity: {Quantity}
```
- **Nível**: Information
- **Quando**: No início do método
- **Informação**: Marca o início da operação com todos os parâmetros

---

### 2. Logs de Validação - Warning
```csharp
[STOCK-UPDATE] Parâmetro supplierId inválido ou vazio
[STOCK-UPDATE] Parâmetro productId inválido ou vazio
[STOCK-UPDATE] Parâmetro sku inválido ou vazio
[STOCK-UPDATE] Quantidade inválida: {Quantity}
```
- **Nível**: Warning
- **Quando**: Se algum parâmetro de entrada é inválido
- **Ação**: Lança `ArgumentException` com mensagem descritiva

---

### 3. Log de Busca - Debug
```csharp
[STOCK-UPDATE] Buscando produtos do vendedor vinculados ao fornecedor...
```
- **Nível**: Debug
- **Quando**: Antes de fazer a busca no repositório
- **Informação**: Marca ponto de execução na busca

---

### 4. Log de Busca Vazia - Warning
```csharp
[STOCK-UPDATE] Nenhum vínculo de vendedor encontrado - SupplierId: {SupplierId}, ProductId: {ProductId}, SKU: {SKU}
```
- **Nível**: Warning
- **Quando**: Se nenhum vínculo é encontrado
- **Ação**: Lança `InvalidOperationException`

---

### 5. Log de Vínculos Encontrados - Information
```csharp
[STOCK-UPDATE] Encontrados {Count} vínculo(s) de vendedor para atualização
```
- **Nível**: Information
- **Quando**: Após buscar com sucesso os vínculos
- **Informação**: Quantidade de produtos para atualizar

---

### 6. Log de Atualização - Information
```csharp
[STOCK-UPDATE] Atualizando estoque na Shopee - StoreId: {StoreId}, ItemId: {ItemId}, ModelId: {ModelId}, Quantity: {Quantity}
```
- **Nível**: Information
- **Quando**: Antes de chamar a API da Shopee para cada vínculo
- **Informação**: Detalhes da requisição que será feita

---

### 7. Log de Sucesso - Information
```csharp
[STOCK-UPDATE] Estoque atualizado com sucesso - StoreId: {StoreId}, ItemId: {ItemId}, ModelId: {ModelId}
```
- **Nível**: Information
- **Quando**: Após atualização bem-sucedida na Shopee
- **Informação**: Confirma sucesso com detalhes

---

### 8. Log de Erro na Resposta - Warning
```csharp
[STOCK-UPDATE] Erro ao atualizar estoque na Shopee - StoreId: {StoreId}, ItemId: {ItemId}, Error: {Error}
```
- **Nível**: Warning
- **Quando**: A API da Shopee retorna com erro
- **Informação**: Detalhe do erro retornado

---

### 9. Logs de Erro por Tipo - Error

#### Parse de IDs
```csharp
[STOCK-UPDATE] Erro ao fazer parse de IDs - MarketplaceProductId: {ProductId}, MarketplaceModelId: {ModelId}
```
- **Tipo**: FormatException
- **Causa**: IDs inválidos que não podem ser convertidos para long

#### Dados de Vínculo Inválidos
```csharp
[STOCK-UPDATE] Dados do vínculo de vendedor inválidos - SellerId: {SellerId}
```
- **Tipo**: ArgumentException
- **Causa**: Validação interna falhou

#### Erro na API Shopee
```csharp
[STOCK-UPDATE] Erro ao atualizar estoque na API Shopee - StoreId: {StoreId}
```
- **Tipo**: InvalidOperationException
- **Causa**: Erro durante chamada à API

#### Erro Inesperado
```csharp
[STOCK-UPDATE] Erro inesperado ao atualizar estoque - SellerId: {SellerId}
```
- **Tipo**: Qualquer outra Exception
- **Causa**: Erro não previsto durante processamento

---

### 10. Log de Resumo - Information
```csharp
[STOCK-UPDATE] Atualização de estoque concluída - Sucesso: {SuccessCount}, Falhas: {FailureCount}, Total: {Total}
```
- **Nível**: Information
- **Quando**: Após processar todos os vínculos
- **Informação**: Estatísticas de sucesso/falha

---

### 11. Logs de Falha Total - Error
```csharp
[STOCK-UPDATE] Todas as atualizações de estoque falharam
```
- **Nível**: Error
- **Quando**: Se todas as atualizações falharem
- **Ação**: Lança `AggregateException` com todas as exceções

---

### 12. Logs de Falha Parcial - Warning
```csharp
[STOCK-UPDATE] Algumas atualizações de estoque falharam ({FailureCount} de {Total})
```
- **Nível**: Warning
- **Quando**: Se algumas atualizações falharem
- **Ação**: Lança `AggregateException` com detalhes

---

### 13. Logs de Exceção Geral - Error
```csharp
[STOCK-UPDATE] Erro ao atualizar estoque (AggregateException) - SupplierId: {SupplierId}, ProductId: {ProductId}, SKU: {SKU}
[STOCK-UPDATE] Erro de validação de parâmetros - SupplierId: {SupplierId}, ProductId: {ProductId}, SKU: {SKU}
[STOCK-UPDATE] Operação inválida - SupplierId: {SupplierId}, ProductId: {ProductId}, SKU: {SKU}
[STOCK-UPDATE] Erro inesperado ao atualizar estoque - SupplierId: {SupplierId}, ProductId: {ProductId}, SKU: {SKU}
```
- **Nível**: Error
- **Quando**: No bloco catch externo para cada tipo de exceção
- **Informação**: Contexto completo da operação

---

## Tratativa de Erros

### 1. Validação de Parâmetros (Entrada)

**Exemplos de validação:**
- `supplierId`: Não pode ser nulo ou vazio
- `productId`: Não pode ser nulo ou vazio
- `sku`: Não pode ser nulo ou vazio
- `quantity`: Não pode ser negativo

**Exceção lançada**: `ArgumentException`

```csharp
if (string.IsNullOrWhiteSpace(supplierId))
{
    _logger.LogWarning("[STOCK-UPDATE] Parâmetro supplierId inválido ou vazio");
    throw new ArgumentException("supplierId não pode ser nulo ou vazio", nameof(supplierId));
}
```

---

### 2. Validação de Dados Recuperados (Repositório)

**Validação:**
- Resultado não pode ser nulo
- Resultado não pode estar vazio

**Exceção lançada**: `InvalidOperationException`

```csharp
if (productsToUpdate == null || !productsToUpdate.Any())
{
    _logger.LogWarning("[STOCK-UPDATE] Nenhum vínculo de vendedor encontrado...");
    throw new InvalidOperationException($"Nenhum vínculo de vendedor encontrado...");
}
```

---

### 3. Validação de Dados do Vínculo

Método separado: `ValidateProductSkuSellerData()`

**Validações:**
- Objeto não pode ser nulo
- `MarketplaceProductId` não pode ser nulo/vazio
- `MarketplaceModelId` não pode ser nulo/vazio
- `StoreId` deve ser maior que 0
- `MarketplaceProductId` deve ser long válido
- `MarketplaceModelId` deve ser long válido

**Exceção lançada**: `ArgumentException`

---

### 4. Tratativa em Loop (Por Vínculo)

O método processa cada vínculo individualmente com:
- Try-catch interno para capturar erros específicos
- Incrementa `failureCount` em caso de erro
- Adiciona exceção à lista para agregação
- **Continua processando** os próximos vínculos (não para)

**Tipos de erro tratados:**

#### A. FormatException
- **Causa**: Parse de IDs para long falha
- **Log**: Error com detalhes dos IDs
- **Ação**: Incrementa falha e continua

#### B. ArgumentException
- **Causa**: Validação de dados falha
- **Log**: Error com ID do vendedor
- **Ação**: Incrementa falha e continua

#### C. InvalidOperationException
- **Causa**: API Shopee retorna erro
- **Log**: Error com detalhes
- **Ação**: Incrementa falha e continua

#### D. Exception (genérica)
- **Causa**: Qualquer outro erro não previsto
- **Log**: Error com contexto
- **Ação**: Incrementa falha e continua

---

### 5. Agregação de Erros

Após processar todos os vínculos:

```
Se (failureCount == total):
    Lança AggregateException com TODAS as exceções
    
Se (failureCount > 0 E failureCount < total):
    Lança AggregateException com todas as exceções
    Mensagem indica sucesso parcial
    
Se (failureCount == 0):
    Sem exceção, operação bem-sucedida
```

---

### 6. Tratativa Externa (Try-Catch Externo)

Captura tipos específicos de exceção:

```csharp
catch (AggregateException ex)
catch (ArgumentException ex)
catch (InvalidOperationException ex)
catch (Exception ex)
```

Cada um faz log específico antes de relançar a exceção.

---

## Exemplo de Fluxo de Log Completo

### Sucesso Total
```
[INFO] Iniciando atualização de estoque - SupplierId: 123, ProductId: abc, SKU: CROSS_P, Quantity: 50
[DEBUG] Buscando produtos do vendedor vinculados ao fornecedor...
[INFO] Encontrados 2 vínculo(s) de vendedor para atualização
[INFO] Atualizando estoque na Shopee - StoreId: 226289035, ItemId: 123456789, ModelId: 9250789027, Quantity: 50
[INFO] Estoque atualizado com sucesso - StoreId: 226289035, ItemId: 123456789, ModelId: 9250789027
[INFO] Atualizando estoque na Shopee - StoreId: 226289035, ItemId: 987654321, ModelId: 9250789028, Quantity: 50
[INFO] Estoque atualizado com sucesso - StoreId: 226289035, ItemId: 987654321, ModelId: 9250789028
[INFO] Atualização de estoque concluída - Sucesso: 2, Falhas: 0, Total: 2
```

### Sucesso Parcial
```
[INFO] Iniciando atualização de estoque - SupplierId: 123, ProductId: abc, SKU: CROSS_P, Quantity: 50
[DEBUG] Buscando produtos do vendedor vinculados ao fornecedor...
[INFO] Encontrados 2 vínculo(s) de vendedor para atualização
[INFO] Atualizando estoque na Shopee - StoreId: 226289035, ItemId: 123456789, ModelId: 9250789027, Quantity: 50
[INFO] Estoque atualizado com sucesso - StoreId: 226289035, ItemId: 123456789, ModelId: 9250789027
[INFO] Atualizando estoque na Shopee - StoreId: 226289035, ItemId: 987654321, ModelId: 9250789028, Quantity: 50
[ERROR] Erro ao fazer parse de IDs - MarketplaceProductId: invalid, MarketplaceModelId: 9250789028
[INFO] Atualização de estoque concluída - Sucesso: 1, Falhas: 1, Total: 2
[WARNING] Algumas atualizações de estoque falharam (1 de 2)
[ERROR] Erro ao atualizar estoque (AggregateException) - SupplierId: 123, ProductId: abc, SKU: CROSS_P
```

### Falha Total
```
[INFO] Iniciando atualização de estoque - SupplierId: 123, ProductId: abc, SKU: CROSS_P, Quantity: 50
[DEBUG] Buscando produtos do vendedor vinculados ao fornecedor...
[WARNING] Nenhum vínculo de vendedor encontrado - SupplierId: 123, ProductId: abc, SKU: CROSS_P
[ERROR] Operação inválida - SupplierId: 123, ProductId: abc, SKU: CROSS_P
```

---

## Níveis de Log Utilizados

| Nível | Uso | Frequência |
|-------|-----|-----------|
| Debug | Pontos de execução, preparação de requisição | Baixa |
| Information | Início, sucesso, estatísticas | Alta |
| Warning | Validação falha, nenhum resultado, falha parcial | Média |
| Error | Exceções, erros na API, falha total | Média |

---

## Código de Integração

```csharp
// Registrar no DI
services.AddScoped<StockServices>();

// Usar no controller
[HttpPost("supplier/stock")]
public async Task<IActionResult> UpdateStockSupplier(
    [FromBody] UpdateStockSupplierRequest request)
{
    try
    {
        await _stockServices.UpdateStockSupplier(
            request.SupplierId,
            request.ProductId,
            request.SKU,
            request.Quantity);
        
        return Ok(new { message = "Stock updated successfully" });
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning($"Validation error: {ex.Message}");
        return BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning($"Operation error: {ex.Message}");
        return NotFound(new { error = ex.Message });
    }
    catch (AggregateException ex)
    {
        _logger.LogError($"Update failed: {ex.Message}");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

---

## Monitoramento e Alertas Recomendados

### Alertas de Erro
- Qualquer log com `[ERROR]` deve gerar alerta
- Falha total deve notificar administrador imediatamente

### Métricas
- Contabilizar sucessos e falhas por período
- Rastrear taxa de sucesso
- Monitorar tempo de execução

### Auditoria
- Manter histórico de todas as operações
- Registrar quem iniciou a operação
- Rastrear mudanças de estoque

---

## Tratativas Recomendadas no Controller

```csharp
[HttpPost("update-stock")]
public async Task<IActionResult> UpdateStock(
    [FromQuery] string supplierId,
    [FromQuery] string productId,
    [FromQuery] string sku,
    [FromQuery] int quantity)
{
    try
    {
        await _stockServices.UpdateStockSupplier(supplierId, productId, sku, quantity);
        return Ok(new { success = true, message = "Stock updated successfully" });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { success = false, error = "Invalid parameters", details = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return NotFound(new { success = false, error = "No sellers found", details = ex.Message });
    }
    catch (AggregateException ex)
    {
        var failureCount = ex.InnerExceptions.Count;
        return StatusCode(207, new { 
            success = false, 
            error = "Partial failure", 
            failures = failureCount,
            details = ex.InnerExceptions.Select(e => e.Message).ToList()
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, error = "Unexpected error", details = ex.Message });
    }
}
```

---

## Resumo

✅ **Logs Abrangentes**: 13 pontos de log diferentes
✅ **Validação Rigorosa**: Entrada, repositório, dados, API
✅ **Tratativa Granular**: Por tipo de erro
✅ **Rastreabilidade**: Contexto completo em todos os logs
✅ **Recuperação Graceful**: Continua processamento mesmo com falhas
✅ **Agregação**: Relata todas as falhas ao final
✅ **Auditoria**: Facilita investigação de problemas

