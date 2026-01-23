variable "DEFAULT_LAMBDA_ROLE_ARN" {
    type = string  
}

variable "environment_variables" {
    type = map(string)
    default = {}

}

variable "tags" {
    type = map(string)
    default = {}
}

variable "lambda_layers" {
    type = list(string)
    default = []
}