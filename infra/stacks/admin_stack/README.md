# Como fazer deploy da lambda via terraform?

Execute os seguintes comandos a partir da pasta `infra` para fazer o deploy da lambda:

1.  **Inicialize o Terraform: **
    Este comando inicializa o diretório de trabalho do Terraform, baixando os provedores e módulos necessários.

    ```bash
    terraform init
    ```

2.  **Planeje o Deploy:**
    Este comando cria um plano de execução, que permite visualizar as alterações que o Terraform fará na sua infraestrutura.

    ```bash
    terraform plan -var-file=../../envs/dev.tfvars
    ```

3.  **Aplique as Alterações:**
    Este comando aplica as alterações planejadas na sua infraestrutura. Você precisará confirmar a aplicação digitando `yes`.

    ```bash
    terraform apply -var-file=../../envs/dev.tfvars -auto-approve
    ```

### Pré-requisitos

-   Você precisa ter o [Terraform](https://learn.hashicorp.com/tutorials/terraform/install-cli) instalado.
-   Você precisa ter as credenciais da AWS configuradas no seu ambiente.