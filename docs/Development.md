# 🛠️ Guia de Desenvolvimento Local

## 📋 Requisitos

- **.NET 8.0** ou superior
- **AWS CLI** configurado
- **Docker** (opcional para DynamoDB local)
- **Git**
- **Editor**: VS Code, Visual Studio 2022 ou Rider

### Verificar Versão do .NET
```bash
dotnet --version
# Deve retornar 8.0.x ou superior
```

## 🚀 Configuração Inicial

### 1. Clone o Repositório
```bash
git clone https://github.com/seu-usuario/dropship.git
cd Dropship/Dropship
```

### 2. Restaure Dependências
```bash
dotnet restore
```

### 3. Configure Variáveis de Ambiente
```bash
# Copie o arquivo de exemplo
cp .env.example .env.local

# Edite com suas credenciais
nano .env.local

# Ou no Windows
copy .env.example .env.local
notepad .env.local
```

**Variáveis Mínimas para Começar:**
```bash
# AWS
AWS_REGION=us-east-1
AWS_ACCESS_KEY_ID=sua-access-key
AWS_SECRET_ACCESS_KEY=sua-secret-key

# Shopee
SHOPEE_PARTNER_ID=1203628
SHOPEE_PARTNER_KEY=sua-partner-key

# JWT
JWT_SECRET=sua-chave-secreta-super-longa-minimo-32-chars
```

### 4. Build do Projeto
```bash
dotnet build
```

### 5. Execute a Aplicação
```bash
dotnet run
```

A aplicação estará disponível em:
- 🌐 `http://localhost:5000`
- 📚 Swagger: `http://localhost:5000/swagger`

## 🗄️ DynamoDB Local (Opcional)

### Com Docker
```bash
# Inicie DynamoDB local
docker run -p 8000:8000 amazon/dynamodb-local

# Variável de ambiente
export DYNAMODB_ENDPOINT=http://localhost:8000
```

### Criar Tabelas Localmente
```bash
# Instale AWS CLI local
pip install awscli-local

# Crie a tabela
awslocal dynamodb create-table \
  --table-name catalog-core \
  --attribute-definitions \
    AttributeName=PK,AttributeType=S \
    AttributeName=SK,AttributeType=S \
  --key-schema \
    AttributeName=PK,KeyType=HASH \
    AttributeName=SK,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --region us-east-1

# Crie o GSI
awslocal dynamodb update-table \
  --table-name catalog-core \
  --attribute-definitions AttributeName=shop_id,AttributeType=N \
  --global-secondary-indexes \
    '[{"IndexName":"GSI_SHOPID_LOOKUP","Keys":[{"AttributeName":"shop_id","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"},"ProvisionedThroughput":{"ReadCapacityUnits":5,"WriteCapacityUnits":5}}]'
```

## 🧪 Executar Testes

### Testes Unitários
```bash
# Todos os testes
dotnet test

# Com verbosidade
dotnet test --verbosity=detailed

# Teste específico
dotnet test --filter "ShopeeApiServiceTests"

# Com coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### Exemplo de Teste
```csharp
[TestFixture]
public class SellerRepositoryTests
{
    private SellerRepository _repository;
    private Mock<IDynamoDBContext> _contextMock;

    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<IDynamoDBContext>();
        _repository = new SellerRepository(_contextMock.Object, new Mock<ILogger<SellerRepository>>().Object);
    }

    [Test]
    public async Task GetSellerByIdAsync_WithValidId_ReturnsSeller()
    {
        // Arrange
        var sellerId = "test-id";
        var seller = new SellerDomain { SellerId = sellerId, SellerName = "Test" };
        
        _contextMock
            .Setup(x => x.LoadAsync<SellerDomain>(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(seller);

        // Act
        var result = await _repository.GetSellerByIdAsync(sellerId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.SellerId, Is.EqualTo(sellerId));
    }
}
```

## 🔍 Debug

### Visual Studio Code
**Criar `.vscode/launch.json`:**
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/Dropship/bin/Debug/net8.0/Dropship.dll",
            "args": [],
            "cwd": "${workspaceFolder}/Dropship",
            "stopAtEntry": false,
            "serverReadyAction": {
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
                "uriFormat": "{0}",
                "action": "openExternally"
            }
        }
    ]
}
```

**Adicione breakpoints e pressione F5 para debug**

### Visual Studio 2022
1. Abra o projeto em Visual Studio 2022
2. Configure a startup: Debug → Properties
3. Pressione F5 para iniciar com debug

### Rider
1. Abra o projeto em Rider
2. Configure run configuration
3. Clique em Debug (Shift+F9)

## 📝 Logging e Observabilidade

### Estrutura de Log
Cada log inclui:
- **CorrelationId**: ID único da requisição
- **Timestamp**: Data e hora
- **Level**: Information, Warning, Error
- **Message**: Mensagem estruturada
- **Parameters**: Valores interpolados

### Exemplo de Log
```
2026-02-04 10:15:23.456 Information [Dropship.Services.ShopeeService]
CorrelationId: 1230498a-sd09f81234 - 
Shop authenticated successfully - ShopId: 226289035, Email: user@example.com
```

### Visualizar Logs
```bash
# Logs com filtro
dotnet run | grep "ShopId"

# Com jq (JSON)
dotnet run 2>&1 | jq 'select(.message | contains("ShopId"))'

# Arquivo
dotnet run > logs.txt 2>&1
tail -f logs.txt
```

## 🚀 Publicar Mudanças

### Workflow Recomendado
```bash
# 1. Crie uma branch
git checkout -b feature/minha-feature

# 2. Faça mudanças
nano Dropship/Services/MyService.cs

# 3. Teste localmente
dotnet build
dotnet test

# 4. Commit
git add .
git commit -m "[FEAT] adiciona minha feature"

# 5. Push
git push origin feature/minha-feature

# 6. Abra Pull Request no GitHub
```

## 🐛 Troubleshooting

### Erro: "Unable to find the required services"
**Causa**: Serviço não foi registrado em `Program.cs`

**Solução**:
```csharp
// Program.cs
builder.Services.AddScoped<MinhaService>();
```

### Erro: "Cannot resolve symbol 'IDynamoDBContext'"
**Causa**: Import faltando

**Solução**:
```csharp
using Amazon.DynamoDBv2.DataModel;
```

### Erro: "Connection refused" ao conectar DynamoDB
**Causa**: DynamoDB local não está rodando

**Solução**:
```bash
# Inicie Docker
docker run -p 8000:8000 amazon/dynamodb-local

# Ou use AWS DynamoDB na nuvem
# Remova DYNAMODB_ENDPOINT do .env
```

### Erro: "Obsolete constructor" DynamoDBContext
**Causa**: Biblioteca desatualizada

**Solução**:
```bash
dotnet restore
dotnet build
```

## 📊 Performance Local

### Ferramentas de Teste
```bash
# Instale ferramentas HTTP
# macOS
brew install httpie

# Windows (chocolatey)
choco install httpie

# Linux
sudo apt-get install httpie
```

### Testar Endpoints
```bash
# GET com parâmetros
http GET http://localhost:5000/shopee/webhook/auth \
  code==auth_code_123 \
  shopId==226289035 \
  email==user@example.com

# POST com body
http POST http://localhost:5000/shopee/webhook \
  msg_id="1234" \
  shop_id=226289035 \
  code=3 \
  timestamp=1639234899 \
  data:='{"ordersn":"123","status":"UNPAID"}'
```

### Postman/Insomnia
1. Importe a collection: `postman_collection.json`
2. Configure environment: `postman_environment.json`
3. Execute requests

## 🔐 Credenciais Seguras

### Evite Commitar Credenciais
```bash
# Adicione ao .gitignore
echo ".env" >> .gitignore
echo ".env.local" >> .gitignore
echo "appsettings.local.json" >> .gitignore

# Nunca faça commit de credenciais
git rm --cached .env
git commit -m "Remove .env from version control"
```

### Use User Secrets
```bash
# Inicialize user secrets
dotnet user-secrets init

# Defina secret
dotnet user-secrets set "JWT_SECRET" "sua-chave-secreta"

# Liste secrets
dotnet user-secrets list
```

## 📦 Adicionar Dependências

```bash
# Pesquise pacote
dotnet package search "package-name"

# Adicione dependência
dotnet add package "NomePackage"

# Restaure
dotnet restore
```

### Pacotes Úteis
```bash
# Testes
dotnet add package NUnit
dotnet add package Moq

# Utilitários
dotnet add package Serilog
dotnet add package FluentValidation
```

## 💾 Banco de Dados Local

### Exportar/Importar Dados
```bash
# Exportar tabela
aws dynamodb scan \
  --table-name catalog-core \
  --output json > table_backup.json

# Importar dados
aws dynamodb batch-write-item \
  --request-items file://table_backup.json
```

## 🔄 Workflow CI/CD Local

### Simular Pipeline
```bash
#!/bin/bash
set -e

echo "🔨 Building..."
dotnet build

echo "🧪 Testing..."
dotnet test

echo "📝 Linting..."
dotnet format --verify-no-changes

echo "✅ All checks passed!"
```

Salve como `ci.sh` e execute:
```bash
chmod +x ci.sh
./ci.sh
```

## 📚 Recursos Úteis

- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/)
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/)
- [DynamoDB Local Guide](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DynamoDBLocal.html)
- [NUnit Testing Framework](https://docs.nunit.org/)

## 🆘 Precisa de Ajuda?

1. Verifique o `ARCHITECTURE.md` para entender a estrutura
2. Consulte os comentários XML nas classes
3. Abra uma issue: `[DEV] nome do problema`
4. Contacte um mantenedor

---

**Happy coding! 🎉**
