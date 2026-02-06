# 📚 Índice da Documentação - Dropship API

## 🚀 Para Começar Rápido

1. **Novo no projeto?** → Leia [README.md](README.md) (5 min)
2. **Quer entender a arquitetura?** → Leia [ARCHITECTURE.md](ARCHITECTURE.md) (15 min)
3. **Pronto para codar?** → Leia [DEVELOPMENT.md](DEVELOPMENT.md) (10 min)
4. **Vai contribuir?** → Leia [CONTRIBUTING.md](CONTRIBUTING.md) (10 min)

## 📖 Documentação Completa

### 📄 README.md
**O que é:** Visão geral do projeto
**Para quem:** Qualquer pessoa
**Quanto tempo:** 5 minutos

**Contém:**
- ✅ Descrição do projeto
- ✅ Características principais
- ✅ Pré-requisitos
- ✅ Instalação rápida
- ✅ Endpoints da API
- ✅ Exemplos de requisições
- ✅ Fluxo de autenticação
- ✅ Logging e monitoramento

**Leia quando:**
- 🆕 Primeira vez no projeto
- 🔍 Precisa de overview rápido
- 📚 Quer conhecer endpoints disponíveis

---

### 🏗️ ARCHITECTURE.md
**O que é:** Explicação detalhada da arquitetura
**Para quem:** Arquitetos, Leads, Devs sênior
**Quanto tempo:** 15 minutos

**Contém:**
- ✅ Diagrama de arquitetura
- ✅ Explicação de cada camada
- ✅ Repository Pattern
- ✅ Domain Models
- ✅ Fluxos principais
- ✅ Padrões utilizados
- ✅ Considerações de performance
- ✅ Segurança
- ✅ Escalabilidade

**Leia quando:**
- 🏛️ Quer entender design decisions
- 🔧 Vai adicionar nova feature
- 👥 Precisa revisar PR complexa
- 🎓 Quer aprender padrões

---

### 🛠️ DEVELOPMENT.md
**O que é:** Guia prático de desenvolvimento local
**Para quem:** Desenvolvedores
**Quanto tempo:** 10 minutos

**Contém:**
- ✅ Configuração do ambiente
- ✅ Setup com Docker
- ✅ DynamoDB local
- ✅ Como rodar testes
- ✅ Debug em diferentes IDEs
- ✅ Logging local
- ✅ Troubleshooting comum
- ✅ Publicação de mudanças

**Leia quando:**
- 💻 Configurando ambiente local
- 🐛 Debugando um problema
- ✅ Rodando testes
- 🚀 Publicando uma feature

---

### 🤝 CONTRIBUTING.md
**O que é:** Guia de contribuição ao projeto
**Para quem:** Contribuidores, novos devs
**Quanto tempo:** 10 minutos

**Contém:**
- ✅ Código de conduta
- ✅ Fork e setup
- ✅ Processo de PR
- ✅ Padrões de código
- ✅ Convenção de commits
- ✅ Testes
- ✅ Documentação
- ✅ CI/CD

**Leia quando:**
- 🆕 Primeira PR no projeto
- 📝 Quer saber padrões de código
- 🧪 Vai adicionar testes
- 💬 Precisa fazer commit

---

### 📊 PROJECT_STRUCTURE.md
**O que é:** Visualização da estrutura do projeto
**Para quém:** Todos
**Quanto tempo:** 5 minutos

**Contém:**
- ✅ Árvore completa do projeto
- ✅ Descrição de cada pasta
- ✅ Fluxos de dados visuais
- ✅ Padrões de código
- ✅ Estrutura de dados
- ✅ Layers de segurança
- ✅ Arquitetura AWS

**Leia quando:**
- 🗺️ Quer navegar o projeto
- 🔄 Quer ver fluxos de dados
- 📊 Quer entender estrutura visual

---

## 🎯 Guia Rápido por Tarefa

### Tarefa: "Adicionar um novo endpoint"
```
1. Leia ARCHITECTURE.md seção "Services Layer"
2. Leia DEVELOPMENT.md seção "Configuração Inicial"
3. Crie Controller, Service, Request, Response
4. Leia CONTRIBUTING.md seção "Padrões de Código"
5. Leia CONTRIBUTING.md seção "Testes"
6. Leia DEVELOPMENT.md seção "Debug"
```

### Tarefa: "Corrigir um bug"
```
1. Leia DEVELOPMENT.md seção "Debug"
2. Leia DEVELOPMENT.md seção "Troubleshooting"
3. Leia ARCHITECTURE.md para entender fluxo
4. Leia CONTRIBUTING.md seção "Commits"
5. Publique a correção
```

### Tarefa: "Adicionar integração com novo serviço"
```
1. Leia ARCHITECTURE.md seção "Services Layer"
2. Leia ARCHITECTURE.md seção "Infrastructure Layer"
3. Leia DEVELOPMENT.md seção "Adicionar Dependências"
4. Crie novo Service (como ShopeeApiService)
5. Registre em Program.cs
6. Integre em Service principal
```

### Tarefa: "Configurar ambiente local"
```
1. Leia README.md seção "Pré-requisitos"
2. Leia DEVELOPMENT.md seção "Configuração Inicial"
3. Leia DEVELOPMENT.md seção "DynamoDB Local"
4. Leia DEVELOPMENT.md seção "Executar Testes"
```

### Tarefa: "Fazer primeiro PR"
```
1. Leia CONTRIBUTING.md seção "Como Começar"
2. Leia CONTRIBUTING.md seção "Processo de Pull Request"
3. Leia CONTRIBUTING.md seção "Padrões de Código"
4. Leia CONTRIBUTING.md seção "Commits"
5. Submeta PR com confiança!
```

---

## 🔍 Buscar por Tema

### Autenticação & Segurança
- README.md → "Autenticação"
- ARCHITECTURE.md → "Segurança"
- CONTRIBUTING.md → "Variáveis de Ambiente"

### DynamoDB & Dados
- ARCHITECTURE.md → "Data Access Layer"
- ARCHITECTURE.md → "Domain Model Layer"
- DEVELOPMENT.md → "DynamoDB Local"
- PROJECT_STRUCTURE.md → "Estrutura de Dados"

### Shopee Integration
- README.md → "Integração Shopee"
- ARCHITECTURE.md → "Fluxo 1: Autenticação Shopee"
- ARCHITECTURE.md → "Fluxo 2: Webhook de Pedido"

### Logging & Monitoring
- README.md → "Logs e Monitoramento"
- DEVELOPMENT.md → "Logging e Observabilidade"
- PROJECT_STRUCTURE.md → "Layers de Segurança"

### Performance & Escalabilidade
- ARCHITECTURE.md → "Considerações de Performance"
- ARCHITECTURE.md → "Escalabilidade"
- DEVELOPMENT.md → "Performance Local"

### Testes
- CONTRIBUTING.md → "Testes"
- DEVELOPMENT.md → "Executar Testes"

### Deploy
- README.md → "Deployment"
- DEVELOPMENT.md → "Workflow CI/CD Local"
- PROJECT_STRUCTURE.md → "Deploy - Arquitetura AWS"

---

## 📚 Recursos por Nível

### 🟢 Iniciante
Comece com:
1. README.md
2. PROJECT_STRUCTURE.md
3. DEVELOPMENT.md (até "Executar Testes")

Depois:
4. CONTRIBUTING.md (apenas "Padrões de Código")

### 🟡 Intermediário
Leia tudo:
1. README.md (completo)
2. ARCHITECTURE.md (Camadas 1-3)
3. DEVELOPMENT.md (completo)
4. CONTRIBUTING.md (completo)
5. PROJECT_STRUCTURE.md (completo)

### 🔴 Avançado
Estude profundamente:
1. ARCHITECTURE.md (completo, várias vezes)
2. Código-fonte (Services, Repositories)
3. DynamoDB indices e design
4. AWS services integration
5. Security patterns

---

## 🆘 FAQ Rápido

**P: Por onde começo?**
R: Leia README.md (5 min), depois DEVELOPMENT.md

**P: Como configuro o ambiente?**
R: DEVELOPMENT.md → "Configuração Inicial"

**P: Como entendo a arquitetura?**
R: ARCHITECTURE.md → Comece pelo diagrama

**P: Qual padrão devo usar?**
R: CONTRIBUTING.md → "Padrões de Código"

**P: Como faço meu primeiro PR?**
R: CONTRIBUTING.md → "Processo de Pull Request"

**P: Onde está o código do Shopee?**
R: Services/ShopeeApiService.cs + ShopeeService.cs

**P: Como debugo localmente?**
R: DEVELOPMENT.md → "Debug"

**P: Qual é a estrutura do banco?**
R: PROJECT_STRUCTURE.md → "Estrutura de Dados"

**P: Onde vejo os endpoints?**
R: README.md → "API Endpoints"

**P: Como adiciono uma dependência?**
R: DEVELOPMENT.md → "Adicionar Dependências"

---

## 🔗 Links Úteis

### Documentação Interna
- [README.md](README.md) - Overview do projeto
- [ARCHITECTURE.md](ARCHITECTURE.md) - Design e padrões
- [DEVELOPMENT.md](DEVELOPMENT.md) - Setup local
- [CONTRIBUTING.md](CONTRIBUTING.md) - Padrões de código
- [PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) - Estrutura visual

### Documentação Externa
- [.NET 8 Docs](https://docs.microsoft.com/dotnet/)
- [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/)
- [DynamoDB Developer Guide](https://docs.aws.amazon.com/amazondynamodb/)
- [Shopee Open Platform](https://open.shopee.com/documents)

### Ferramentas
- [.NET Runtime](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Postman](https://www.postman.com/)
- [AWS CLI](https://aws.amazon.com/cli/)

---

## 📋 Checklist de Leitura

### Para Novo Desenvolvedor
- [ ] README.md
- [ ] PROJECT_STRUCTURE.md
- [ ] DEVELOPMENT.md (até "Executar Testes")
- [ ] CONTRIBUTING.md ("Padrões de Código")
- [ ] ARCHITECTURE.md ("Camadas 1-3")

### Para Revisor de PR
- [ ] CONTRIBUTING.md (completo)
- [ ] ARCHITECTURE.md (relevante ao PR)
- [ ] Código-fonte da feature

### Para Líder Técnico
- [ ] Tudo acima
- [ ] ARCHITECTURE.md (2-3 vezes)
- [ ] Código-fonte completo
- [ ] Design decisions

---

## 🎯 Navegação Rápida

```
Preciso de...                          Vá para...
─────────────────────────────────────────────────
Visão geral rápida                     README.md
Entender arquitetura                   ARCHITECTURE.md
Configurar ambiente                    DEVELOPMENT.md
Padrões de código                      CONTRIBUTING.md
Estrutura visual                       PROJECT_STRUCTURE.md
Como fazer PR                          CONTRIBUTING.md
Como fazer commit                      CONTRIBUTING.md
Como debugar                           DEVELOPMENT.md
Como adicionar feature                 ARCHITECTURE.md + CONTRIBUTING.md
Endpoints disponíveis                  README.md
Como rodar testes                      DEVELOPMENT.md
Como fazer deploy                      README.md
Troubleshooting                        DEVELOPMENT.md
```

---

**Última atualização:** February 4, 2026
**Versão:** 1.0
**Mantido por:** Time de Desenvolvimento

**Precisa de ajuda?** Abra uma issue com `[DOCS]` no título!
