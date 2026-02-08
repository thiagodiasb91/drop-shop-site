# Projeto Drop Shop Admin - Readme

## Arquitetura
Admin SPA (React/Vite)
  └─ CloudFront + S3
        ↓
Cognito User Pool
        ↓ (JWT)
API Gateway (Authorizer Cognito)
        ↓
Lambda (Python)

---

Front só consome userPoolId, clientId, domain


## Primeira entrega - Shopee
- Fornecedor
  - Tela de vínculo de produtos
  - Atualização de estoques
    - Produtos pré-cadastrados
    - Atualiza as 2 lojas
  - Listagem de pedidos para envio

- Vendedor
  - Tela de vínculo de produtos
    - Atualiza sua loja
  - Visualização de estoque
  - Listagem de pagamentos por fornecedor

- Admin
  - Gestão de login