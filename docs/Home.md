# Bem-vindo à Wiki da Plataforma Drop Shop

Esta wiki é a fonte central de conhecimento para a arquitetura, desenvolvimento e operação da plataforma Drop Shop. O objetivo é fornecer a todos os desenvolvedores uma compreensão clara de todos os componentes do sistema.

## Visão Geral do Projeto

A Drop Shop é uma plataforma de gerenciamento de dropshipping que integra com a Shopee. O sistema é composto por quatro áreas principais:

1.  **Backend (.NET):** A API central que orquestra toda a lógica de negócios.
2.  **Frontend (Painel Admin):** Uma SPA para administradores gerenciarem a plataforma.
3.  **Infraestrutura (IaC):** Código Terraform que define e implanta todos os recursos na AWS.
4.  **Serverless (Lambda):** Funções para tarefas desacopladas, como telemetria e gerenciamento de sessão.

## Arquitetura Central

Para entender como a plataforma funciona, comece explorando a arquitetura de cada componente principal.

| Componente                                        | Descrição                                                                      |
| ------------------------------------------------- | ------------------------------------------------------------------------------ |
| 📄 **[Backend (.NET)](./Architecture.md)**         | A arquitetura da API principal, suas camadas e responsabilidades.              |
| 🖥️ **[Frontend (Admin)](./Frontend-Architecture.md)** | A estrutura do painel de administração, tecnologias e padrões utilizados.      |
| ☁️ **[Infraestrutura (IaC)](./Infrastructure.md)**  | Como a infraestrutura é definida, organizada e implantada com Terraform.       |
| ⚡ **[Serverless (Lambda)](./Serverless.md)**       | O propósito e o funcionamento das funções Lambda do projeto.                   |

## Guias Práticos

Depois de entender a arquitetura, consulte os guias para obter informações práticas sobre desenvolvimento e fluxos específicos.

-   **[Guia de Desenvolvimento](./Development.md):** Passos essenciais para configurar seu ambiente local.
-   **[Fluxo de Autenticação da Shopee](./Shopee-Auth-Flow.md):** Descrição detalhada do processo de autenticação com a API da Shopee.
