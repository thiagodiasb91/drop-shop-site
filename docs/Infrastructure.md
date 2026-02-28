# Arquitetura de Infraestrutura (IaC com Terraform)

A infraestrutura da plataforma Drop Shop é gerenciada como código (Infrastructure as Code - IaC) utilizando [Terraform](https://www.terraform.io/). Essa abordagem garante que a criação e manutenção dos recursos na nuvem (AWS) sejam automatizadas, versionadas e consistentes entre os ambientes.

## Visão Geral

O código Terraform está localizado na pasta `infra/` e é dividido em duas partes principais: `modules` e `stacks`.

-   **`modules/`**: Contém módulos Terraform reutilizáveis. Cada módulo é projetado para criar um conjunto específico de recursos, como um bucket S3 ou uma função Lambda, com configurações padronizadas. O uso de módulos evita a repetição de código.
-   **`stacks/`**: Representa os diferentes conjuntos de recursos que compõem a aplicação. Cada "stack" é uma coleção de recursos que são implantados e gerenciados juntos.

## Estrutura das Stacks

A plataforma é dividida nas seguintes stacks, o que permite gerenciar o ciclo de vida de cada parte de forma independente:

### `core_stack`

-   **Propósito:** Define os recursos fundamentais e compartilhados que são usados por outras stacks.
-   **Exemplos de Recursos:** Redes (VPC, subnets), configurações de segurança (Security Groups) e possivelmente clusters de base de dados ou políticas de IAM.

### `cognito_stack`

-   **Propósito:** Gerencia exclusivamente os recursos de autenticação e autorização de usuários.
-   **Recursos Principais:** AWS Cognito User Pool, App Clients, e configurações de identidade. Isolar o Cognito em sua própria stack é uma boa prática de segurança.

### `backend_stack`

-   **Propósito:** Implanta todos os recursos necessários para a API do backend .NET.
-   **Exemplos de Recursos:** Serviço de hospedagem da API (ex: AWS App Runner, ECS Fargate, ou Elastic Beanstalk), tabelas DynamoDB, e permissões de IAM para a API acessar outros serviços.

### `admin_stack`

-   **Propósito:** Implanta os recursos para o painel de administração (frontend).
-   **Recursos Principais:** Bucket S3 para hospedar os arquivos estáticos do site e uma distribuição CloudFront (CDN) para garantir a entrega de conteúdo com baixa latência e segurança (HTTPS).

## Workflow de Deploy

O deploy da infraestrutura é automatizado e provavelmente executado por um pipeline de CI/CD (como GitHub Actions, definido em `.github/workflows/`):

1.  Um desenvolvedor faz alterações no código Terraform em uma branch.
2.  Ao abrir um Pull Request, um plano do Terraform (`terraform plan`) é executado para validar as mudanças.
3.  Após o merge na branch principal (ex: `main` ou `develop`), o pipeline executa o `terraform apply` para provisionar ou atualizar os recursos no ambiente da AWS correspondente.
