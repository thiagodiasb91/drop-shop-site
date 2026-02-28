# Arquitetura do Frontend (Painel Administrativo)

O painel administrativo é o ponto de acesso para a gestão da plataforma Drop Shop. Ele é projetado como uma Single-Page Application (SPA) moderna, focada em performance e uma boa experiência de desenvolvimento.

## Visão Geral

A aplicação permite que os administradores realizem operações CRUD (Criar, Ler, Atualizar, Deletar) sobre as principais entidades do sistema, como produtos, vendedores e fornecedores, consumindo a API do backend .NET.

## Tech Stack

| Categoria             | Tecnologia                                                    |
| --------------------- | ------------------------------------------------------------- |
| **Build Tool**        | [Vite](https://vitejs.dev/)                                   |
| **Linguagem**         | JavaScript (ES6+)                                             |
| **Estilização**       | [Tailwind CSS](https://tailwindcss.com/)                      |
| **Componentes UI**    | [Alpine.js](https://alpinejs.dev/) (para interatividade leve) |
| **Linting**           | [ESLint](https://eslint.org/)                                 |
| **Package Manager**   | [NPM](https://www.npmjs.com/)                                 |

A escolha por Vite e JavaScript puro (com Alpine.js para pequenas interatividades) foi feita para manter o frontend leve e rápido, sem a complexidade de um framework pesado como React ou Vue, que não era necessário para os requisitos deste painel.

## Estrutura de Arquivos (`frontend/admin`)

A estrutura segue o padrão de projetos Vite:

-   **`public/`**: Contém arquivos estáticos que são servidos diretamente, como favicons e imagens.
-   **`src/`**: O coração do código-fonte da aplicação.
-   **`index.html`**: O ponto de entrada da aplicação.
-   **`vite.config.js`**: Arquivo de configuração do Vite.
-   **`tailwind.config.js`**: Arquivo de configuração do Tailwind CSS.
-   **`package.json`**: Define os scripts e dependências do projeto.

## Scripts Principais

Os scripts definidos no `package.json` automatizam as tarefas de desenvolvimento e build:

-   `npm run dev`: Inicia o servidor de desenvolvimento local.
-   `npm run build`: Gera a versão de produção da aplicação na pasta `dist/`.
-   `npm run lint`: Analisa o código em busca de erros e inconsistências de estilo.

## Deployment

O deploy é feito para a AWS, utilizando S3 para hospedar os arquivos estáticos e CloudFront como CDN para performance e distribuição global. O processo é automatizado via scripts (`deploy:s3`, `deploy:cdn`) que podem ser executados em um ambiente de CI/CD como o GitHub Actions.
