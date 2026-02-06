# 🤝 Guia de Contribuição - Dropship API

Obrigado por considerar contribuir para o Dropship API! Este documento fornece diretrizes e informações para ajudar você a contribuir de forma eficaz.

## 📋 Sumário

- [Código de Conduta](#código-de-conduta)
- [Como Começar](#como-começar)
- [Processo de Pull Request](#processo-de-pull-request)
- [Padrões de Código](#padrões-de-código)
- [Commits](#commits)
- [Testes](#testes)
- [Documentação](#documentação)

## 📜 Código de Conduta

### Nossa Visão
Nós, como contribuidores e mantenedores, nos comprometemos a tornar a participação neste projeto e nossa comunidade uma experiência livre de assédio para todos.

### Comportamento Esperado
- Ser respeitoso e inclusivo
- Aceitar críticas construtivas
- Focar no que é melhor para a comunidade
- Mostrar empatia com outros membros

### Comportamento Inaceitável
- Discriminação de qualquer tipo
- Assédio ou intimidação
- Comentários ofensivos ou insultos
- Ataques pessoais

## 🚀 Como Começar

### 1. Fork o Repositório
```bash
# Visite https://github.com/seu-usuario/dropship
# Clique em "Fork"
```

### 2. Clone Seu Fork Localmente
```bash
git clone https://github.com/seu-usuario/dropship.git
cd Dropship
```

### 3. Configure o Upstream
```bash
git remote add upstream https://github.com/original-usuario/dropship.git
git fetch upstream
```

### 4. Crie uma Branch de Feature
```bash
git checkout -b feature/minha-nova-feature
```

### 5. Configure Seu Ambiente
```bash
cd Dropship
dotnet restore
dotnet build
```

## 📝 Processo de Pull Request

### Antes de Submeter
1. ✅ Atualize sua branch com a última versão do upstream:
```bash
git fetch upstream
git rebase upstream/main
```

2. ✅ Certifique-se que seu código compila:
```bash
dotnet build
```

3. ✅ Execute testes:
```bash
dotnet test
```

4. ✅ Revise suas próprias mudanças:
```bash
git diff upstream/main
```

### Criando a Pull Request

#### Template de Título
```
[TIPO] descrição breve em português
```

**Tipos aceitos:**
- `[FEAT]` - Nova feature
- `[FIX]` - Correção de bug
- `[REFACTOR]` - Refatoração de código
- `[DOCS]` - Documentação
- `[PERF]` - Melhoria de performance
- `[TEST]` - Testes
- `[CHORE]` - Manutenção

#### Exemplo
```
[FEAT] adiciona autenticação OAuth2 para Shopee
```

#### Template de Descrição
```markdown
## 📝 Descrição
Descreva as mudanças de forma clara e concisa.

## 🎯 Tipo de Mudança
- [ ] Bug fix
- [ ] Nova feature
- [ ] Breaking change
- [ ] Mudança de documentação

## 🧪 Como Testar
1. Passo 1
2. Passo 2
3. Passo 3

## ✅ Checklist
- [ ] Meu código segue os padrões do projeto
- [ ] Executei lint e formatter localmente
- [ ] Adicionei testes para novas features
- [ ] Todos os testes passam (`dotnet test`)
- [ ] Atualizei a documentação se necessário
- [ ] Não há problemas novos de CI/CD

## 🔗 Issues Relacionadas
Fecha #(numero da issue)

## 📸 Screenshots (se aplicável)
```

### Processo de Review
1. Um mantenedor será atribuído
2. Mudanças podem ser solicitadas
3. Seu código será testado
4. Após aprovação, será feito o merge

## 💻 Padrões de Código

### Convenção de Nomenclatura

#### C# Classes e Métodos
```csharp
// Classes - PascalCase
public class ShopeeApiService { }
public class SellerRepository { }

// Métodos - PascalCase
public async Task CreateSellerAsync(SellerDomain seller)
public string GetAuthUrl()

// Propriedades - PascalCase
public string SellerId { get; set; }
public long ShopId { get; set; }

// Variáveis locais - camelCase
var sellerId = Guid.NewGuid().ToString();
var shopExists = await _repository.ExistsAsync(shopId);

// Constantes - UPPER_CASE ou PascalCase
private const string DefaultHost = "https://...";
private const int DefaultTimeout = 5000;
```

### Padrões de Código

#### Logging Estruturado
```csharp
// ✅ Bom - com parâmetros nomeados
_logger.LogInformation("Seller created successfully - SellerId: {SellerId}, ShopId: {ShopId}", 
    sellerId, shopId);

// ❌ Evitar - sem estrutura
_logger.LogInformation($"Seller created: {sellerId}");
```

#### Tratamento de Exceções
```csharp
// ✅ Bom - logging e re-throw
try
{
    await _repository.CreateAsync(entity);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error creating entity - EntityId: {EntityId}", entity.Id);
    throw;
}

// ❌ Evitar - swallowing exceptions silenciosamente
try { await _repository.CreateAsync(entity); } catch { }
```

#### Async/Await
```csharp
// ✅ Bom
public async Task<SellerDomain> GetSellerAsync(string sellerId)
{
    return await _repository.GetSellerByIdAsync(sellerId);
}

// ❌ Evitar - operações bloqueantes
public SellerDomain GetSeller(string sellerId)
{
    return _repository.GetSellerByIdAsync(sellerId).Result;
}
```

#### Validação de Entrada
```csharp
// ✅ Bom - validação clara
if (string.IsNullOrWhiteSpace(email))
{
    throw new ArgumentException("Email is required");
}

if (shopId <= 0)
{
    throw new ArgumentException("ShopId must be greater than 0");
}
```

### Comentários
```csharp
// ✅ Bom - XML documentation
/// <summary>
/// Cria um novo seller no sistema e atualiza o usuário
/// </summary>
/// <param name="sellerId">ID único do seller</param>
/// <param name="shopId">ID da loja no marketplace</param>
/// <returns>Seller criado com timestamp</returns>
public async Task<SellerDomain> CreateSellerAsync(string sellerId, long shopId)

// ✅ Bom - comentários explicativos
// Tenta usar token em cache primeiro para evitar chamadas à API
var cachedToken = await _cacheService.GetAsync(cacheKey);

// ❌ Evitar - comentários óbvios
var name = seller.Name; // Obtém o nome
```

## 📌 Commits

### Mensagens de Commit
Siga o padrão:
```
[TIPO] descrição concisa

Descrição detalhada do que foi mudado e por quê.
Inclua motivação e contexto.

Relacionado à issue #123
```

### Exemplos
```
[FEAT] adiciona endpoint de autenticação Shopee

Implementa autenticação OAuth2 com Shopee incluindo:
- Geração de assinatura HMAC SHA256
- Refresh automático de tokens
- Armazenamento em cache

Fecha #42

---

[FIX] corrige parsing de expires_in na resposta Shopee

A resposta da Shopee pode incluir diferentes nomes de propriedade
para o tempo de expiração (expires_in, expire_in, expire).
Agora o código tenta múltiplas opções com fallback para 3600.

Relacionado à issue #156

---

[REFACTOR] extrai lógica de autenticação para ShopeeApiService

Move responsabilidade de chamadas HTTP da classe monolítica
ShopeeService para ShopeeApiService dedicado.

Melhora testes e reutilização de código.
```

### Boas Práticas
- ✅ Commits pequenos e focados
- ✅ Uma feature ou fix por commit
- ✅ Mensagens claras em português
- ✅ Referência a issues quando aplicável

## 🧪 Testes

### Adicione Testes Para
- ✅ Novas features
- ✅ Bug fixes (regressão)
- ✅ Mudanças em lógica crítica

### Estrutura de Testes
```
Dropship.Tests/
├── Services/
│   ├── ShopeeApiServiceTests.cs
│   ├── ShopeeServiceTests.cs
│   └── AuthenticationServiceTests.cs
├── Repository/
│   ├── SellerRepositoryTests.cs
│   └── UserRepositoryTests.cs
└── Controllers/
    └── ShopeeWebhookControllerTests.cs
```

### Exemplo de Teste
```csharp
[TestFixture]
public class ShopeeApiServiceTests
{
    private ShopeeApiService _service;
    private Mock<HttpClient> _httpClientMock;

    [SetUp]
    public void Setup()
    {
        _httpClientMock = new Mock<HttpClient>();
        _service = new ShopeeApiService(_httpClientMock.Object, new Mock<ILogger<ShopeeApiService>>().Object);
    }

    [Test]
    public void GenerateSign_ShouldReturnValidHmacSignature()
    {
        // Arrange
        var path = "/api/v2/auth/token/get";
        var timestamp = 1609459200;

        // Act
        var sign = _service.GenerateSign(path, timestamp);

        // Assert
        Assert.That(sign, Is.Not.Null);
        Assert.That(sign, Has.Length.EqualTo(64)); // SHA256 hex
    }
}
```

### Executar Testes
```bash
# Todos os testes
dotnet test

# Com coverage
dotnet test /p:CollectCoverage=true

# Teste específico
dotnet test --filter "TestName"
```

## 📚 Documentação

### Atualize Documentação Para
- ✅ Novas features (endpoints, configurações)
- ✅ Mudanças em comportamento existente
- ✅ Novos padrões ou convenções

### Tipos de Documentação

#### README.md
Atualize a seção de endpoints quando adicionar/modificar rotas:
```markdown
### 🛍️ Shopee Webhook & Auth
```
GET    /shopee/webhook/auth       - Autenticação OAuth2
```
```

#### XML Documentation
Adicione comentários em classes públicas:
```csharp
/// <summary>
/// Serviço para autenticação e conexão com API da Shopee
/// </summary>
public class ShopeeApiService
{
    /// <summary>
    /// Obtém token em nível de loja usando código de autorização
    /// </summary>
    /// <param name="code">Código de autorização da Shopee</param>
    /// <param name="shopId">ID da loja</param>
    /// <returns>Tupla com (AccessToken, RefreshToken, ExpiresIn)</returns>
    public async Task<(string, string, long)> GetTokenShopLevelAsync(string code, string shopId)
}
```

#### Arquivos de Configuração
Se adicionar novas variáveis de ambiente, atualize `.env.example`:
```bash
# Novo serviço externo
MY_SERVICE_API_KEY=sua-chave-aqui
MY_SERVICE_TIMEOUT=30000
```

## ⚙️ CI/CD

Nosso pipeline automático:
1. Build (.NET 8.0)
2. Testes unitários
3. Análise de código (StyleCop)
4. Coverage de testes
5. Build da imagem Docker

Todos devem passar antes do merge.

## 🎓 Recursos Úteis

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
- [Logging in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)

## 🆘 Precisa de Ajuda?

- 💬 Abra uma issue com a tag `[HELP]`
- 📧 Contacte um mantenedor
- 💡 Veja discussions abertas

---

**Obrigado por contribuir! Sua ajuda torna este projeto melhor! ❤️**
