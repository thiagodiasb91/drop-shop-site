variable "function_name" {
    type = string  
}

variable "lambda_role_arn" {
    type = string  
}

variable "handler" {
    type = string  
}

variable "runtime" {
    type = string  
}

variable "source_path" {
    type = string  
}

variable "environment_variables" {
    type = map(string)  
}

variable "tags" {
    type = map(string)  
}