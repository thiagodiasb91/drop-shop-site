# Arquitetura Serverless (AWS Lambda)

A plataforma Drop Shop utiliza funções Serverless com AWS Lambda para executar tarefas específicas, desacopladas e escaláveis. Essa abordagem permite que funcionalidades isoladas sejam desenvolvidas, implantadas e mantidas de forma independente do monólito do backend.

As funções estão localizadas na pasta `backend/` e seguem um padrão de projeto próprio, contendo código-fonte (`src/`), testes (`tests/`) e, em alguns casos, sua própria definição de infraestrutura.

## Funções Lambda

Atualmente, o projeto conta com as seguintes funções:

### 1. `lambda-bff-get-user-session`

-   **Linguagem:** Python
-   **Propósito:** Atua como um "Backend for Frontend" (BFF) minimalista, com a responsabilidade única de gerenciar e validar a sessão de um usuário.
-   **Funcionamento:**
    1.  O painel de administração (frontend) provavelmente invoca esta função (via API Gateway) no carregamento inicial da página.
    2.  A função recebe um token de sessão (ex: JWT) do frontend.
    3.  Ela se comunica com o serviço de autenticação (provavelmente o Cognito da `cognito_stack`) para validar o token.
    4.  Se o token for válido, ela retorna os dados essenciais do usuário (como nome, permissões, etc.) para o frontend.
-   **Justificativa:** Isolar a lógica de sessão em uma função Lambda dedicada simplifica o frontend e o backend principal, criando um ponto de estrangulamento de segurança claro e fácil de monitorar.

### 2. `lambda-telemetry-frontend-logs`

-   **Linguagem:** Python
-   **Propósito:** Coletar e processar logs e eventos de telemetria gerados pelo painel de administração.
-   **Funcionamento:**
    1.  O frontend envia eventos (ex: erros de JavaScript, cliques em botões, métricas de performance) para um endpoint de API que aciona esta função.
    2.  A função Lambda recebe esses dados.
    3.  Ela processa, formata e encaminha os logs para um serviço de monitoramento e observabilidade, como **AWS CloudWatch Logs**, **Datadog**, ou similar.
-   **Justificativa:** Centralizar a coleta de logs do cliente em uma função Lambda é uma maneira eficiente e escalável de monitorar a saúde da aplicação e a experiência do usuário em tempo real, sem sobrecarregar a API principal do backend com esse tipo de tráfego.
