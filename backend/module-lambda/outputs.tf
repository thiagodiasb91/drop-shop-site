output "lambda_role_arn" {
    value = aws_lambda_function.lambda_module.role
}

output "function_name" {
    value = aws_lambda_function.lambda_module.function_name
}

output "handler" {
    value = aws_lambda_function.lambda_module.handler
}

output "runtime" {
    value = aws_lambda_function.lambda_module.runtime
}

output "filename" {
    value = aws_lambda_function.lambda_module.filename
}

output "environment_variables" {
    value = aws_lambda_function.lambda_module.environment.variables
}

output "tags" {
    value = aws_lambda_function.lambda_module.tags
}