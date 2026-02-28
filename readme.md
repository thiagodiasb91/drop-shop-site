# 🛍️ Drop Shop Platform

Bem-vindo à plataforma Drop Shop! Este é um sistema de gerenciamento de dropshipping projetado para integrar com a Shopee, facilitando a administração de produtos, fornecedores e vendedores.

---

## 🏛️ Arquitetura do Projeto

A plataforma é composta por múltiplos componentes que trabalham em conjunto para fornecer uma solução completa e robusta:

*   **Backend (.NET):** O núcleo da aplicação, construído com .NET. É uma API RESTful que gerencia toda a lógica de negócios, desde a comunicação com o banco de dados até a integração com serviços externos como a Shopee.
*   **Frontend (Admin Panel):** Uma interface de administração web, construída com Vite e Tailwind CSS. Permite que os administradores gerenciem a plataforma de forma intuitiva.
*   **Infraestrutura (IaC):** Toda a infraestrutura na nuvem (AWS) é gerenciada como código, utilizando Terraform. Isso garante consistência, versionamento e automação no deploy dos recursos.
*   **Funções Serverless (Lambda):** Funções AWS Lambda para tarefas específicas e desacopladas, como gerenciamento de sessões de usuário e coleta de telemetria.

---

## 📚 Hub de Documentação

Este repositório é organizado em módulos, e cada um possui sua própria documentação detalhada. Use os links abaixo para navegar para a documentação específica de cada componente.

| Componente                                                                   | Descrição                                                                      |
| ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| 📄 **[Documentação Geral do Backend](./backend/docs/README.md)**               | Visão geral da arquitetura do backend, guias de desenvolvimento e documentação da API. |
| 🖥️ **[Painel de Administração (Frontend)](./frontend/admin/README.md)**      | Instruções de instalação, scripts e detalhes técnicos sobre o painel de administração.   |
| lambda **[BFF Get User Session](./backend/lambda-bff-get-user-session/README.md)** | Detalhes sobre a função Lambda responsável pelo gerenciamento de sessão de usuário. |
| 📡 **[Telemetry Frontend Logs](./backend/lambda-telemetry-frontend-logs/README.md)** | Informações sobre a função Lambda que coleta logs do frontend.                 |
