
# Drop Shop - Painel Administrativo

## Visão Geral

Este é o projeto frontend do painel administrativo da plataforma Drop Shop. Ele fornece uma interface de usuário para gerenciar as operações de dropshipping, incluindo produtos, fornecedores, vendedores e integrações com a Shopee.

Esta aplicação é construída com Vite.js, utilizando JavaScript puro para a lógica e Tailwind CSS para a estilização, garantindo uma experiência de desenvolvimento rápida e um design moderno e responsivo.

## ✨ Features

*   **Dashboard Principal:** Visão geral das operações e estatísticas.
*   **Gerenciamento de Produtos:** Adicionar, editar e remover produtos.
*   **Gerenciamento de Fornecedores:** Controle de fornecedores e seus produtos.
*   **Gerenciamento de Vendedores:** Administração de vendedores da plataforma.
*   **Integração com Shopee:** Monitoramento e gerenciamento da integração com a API da Shopee.
*   **Autenticação Segura:** Login e gerenciamento de sessão para administradores.

## 💻 Tech Stack

*   **Build Tool:** [Vite](https://vitejs.dev/)
*   **Framework de Estilização:** [Tailwind CSS](https://tailwindcss.com/)
*   **Componentes Interativos:** [Alpine.js](https://alpinejs.dev/)
*   **Linting:** [ESLint](https://eslint.org/)
*   **Linguagem:** JavaScript (ES6+)

## 🚀 Primeiros Passos

Siga as instruções abaixo para configurar o ambiente de desenvolvimento local.

### Pré-requisitos

*   [Node.js](https://nodejs.org/) (versão 18.x ou superior)
*   [NPM](https://www.npmjs.com/) (geralmente instalado com o Node.js)

### Instalação

1.  **Clone o repositório:**
    ```bash
    git clone <URL_DO_REPOSITORIO>
    ```

2.  **Navegue até o diretório do projeto:**
    ```bash
    cd frontend/admin
    ```

3.  **Instale as dependências:**
    ```bash
    npm install
    ```

4.  **Configure as variáveis de ambiente:**
    Crie um arquivo `.env.local` na raiz do diretório `frontend/admin`. Você pode copiar o arquivo `.env.development` como base.

    ```bash
    cp .env.development .env.local
    ```

    Edite o arquivo `.env.local` com as URLs corretas para o seu ambiente de desenvolvimento.

    ```plaintext
    # .env.local
    VITE_API_BASE_URL=https://sua-api.exemplo.com/dev
    VITE_SITE_URL=http://localhost:5173
    ```

    *   `VITE_API_BASE_URL`: A URL base da API do backend.
    *   `VITE_SITE_URL`: A URL do site para o ambiente local.

### Executando a Aplicação

Com as dependências instaladas e as variáveis de ambiente configuradas, inicie o servidor de desenvolvimento:

```bash
npm run dev
```

A aplicação estará disponível em [http://localhost:5173](http://localhost:5173).

## 📜 Scripts Disponíveis

*   `npm run dev`: Inicia o servidor de desenvolvimento com hot-reload.
*   `npm run build`: Compila e otimiza a aplicação para produção na pasta `dist/`.
*   `npm run lint`: Executa o linter para verificar a qualidade do código JavaScript.
*   `npm run preview`: Compila a aplicação e inicia um servidor local para visualizar a build de produção.

## 📦 Build e Deploy

O processo de deploy é automatizado e gerenciado pelas seguintes etapas:

1.  **Build:** O script `npm run build` cria a versão de produção do site na pasta `dist/`.
2.  **Deploy para S3:** O script `npm run deploy:s3` sincroniza o conteúdo da pasta `dist/` com um bucket S3 da AWS.
3.  **Invalidação de Cache do CDN:** O script `npm run deploy:cdn` invalida o cache do CloudFront para garantir que os usuários recebam a versão mais recente.

O script `npm run deploy` executa todas as etapas em sequência. A configuração dos buckets e distribuição de CDN deve ser feita através das variáveis de ambiente no ambiente de CI/CD.

## 🤝 Contribuição

Contribuições são bem-vindas! Por favor, siga as diretrizes de contribuição e o código de conduta do projeto. Para alterações significativas, abra uma issue para discutir o que você gostaria de mudar.
