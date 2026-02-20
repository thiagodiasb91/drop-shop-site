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
- Admin
  - Gestão de usuários
    - GET /users
    - POST /users/{user_id}
  - Criação de produtos
    - GET /products
      - Retorna os produtos com a informação dos skus
    - POST /products/{product_id}
      - Atualiza informação de produtos e dos SKUs

- Fornecedor
  - Tela de vínculo de produtos
    - GET /suppliers/{supplier_id}/products
      - Obtém todos os produtos
      - Retornar informação de quais produtos estão vinculados com o fornecedor
    - POST /suppliers/{supplier_id}/products
      - Serviço que atualiza os vinculos, além de SKUs e Preços de custo
  - Atualização de estoques
    - Produtos pré-cadastrados
      - GET /suppliers/{supplier_id}/products
        - Os produtos que estão vinculados com o fornecedor
      - POST /suppliers/{supplier_id}/products/{product_id}/stock
        - Serviço que atualiza o preço de custo do produto do fornecedor
  - Listagem de pedidos para envio
    - GET /suppliers/{supplier_id}/orders
      - Listagem de pedidos para envio

- Vendedor
  - Tela de vínculo de produtos
    - GET /sellers/{seller_id}/products/available
      - Produtos que possuem vínculo com algum fornecedor
      - Retornar informação de quais produtos estão vinculados com o vendedor
  - Visualização de estoque
    - GET /sellers/{seller_id}/products/stock
      - Retornar informação do estoque do vendedor
  - Listagem de pagamentos por fornecedor
    - GET /sellers/{seller_id}/payments
      - Retorna listagem de pagamentos do vendedor

