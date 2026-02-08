sequenceDiagram
    participant User
    participant Front
    participant Cognito
    participant Backend
    participant Dynamo
    participant Shopee

    User ->> Front: Acessa app
    Front ->> Cognito: Redirect OAuth Login
    Cognito ->> Front: Callback (code)

    Front ->> Backend: POST /auth/callback
    Backend ->> Cognito: Troca code por tokens
    Backend ->> Dynamo: Consulta permissão
    Backend ->> Front: JWT { user, role, resource_id }

    Front ->> Front: Armazena JWT (sessionStorage)
    Front ->> Backend: GET /auth/me
    Backend ->> Front: { role, resource_id }

    alt role = new-user
        Front ->> User: Exibe "Sem acesso definido"

    else role = seller && resource_id == null
        Front ->> User: Redirect Setup de Loja
        User ->> Shopee: Autoriza integração
        Shopee ->> Front: Redirect com code + shop_id
        Front ->> Backend: POST /sellers/store/setup
        Backend ->> Dynamo: Vincula seller + resource_id

        Front ->> Backend: POST /auth/renew-token
        Backend ->> Front: Novo JWT { resource_id }
        Front ->> Front: Atualiza JWT (sessionStorage)

        Front ->> Backend: GET /auth/me
        Backend ->> Front: { role, resource_id }
        Front ->> User: Redirect Home

    else role = supplier
        Front ->> Backend: GET /suppliers/{id}
        Backend ->> Dynamo: Consulta Supplier

        opt setup incompleto
            Front ->> User: Redirect Setup Fornecedor
            Front ->> Backend: POST /suppliers/{id}
            Backend ->> Dynamo: Completa cadastro

            Front ->> Backend: POST /auth/renew-token
            Backend ->> Front: Novo JWT { resource_id }
            Front ->> Front: Atualiza JWT (sessionStorage)

            Front ->> Backend: GET /auth/me
            Backend ->> Front: { role, resource_id }
        end

        Front ->> User: Redirect Home

    else role = admin OR setup completo
        Front ->> User: Redirect Home
    end
