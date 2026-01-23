# modulo para criar a funcao lambda
resource "aws_lambda_function" "lambda_module" {
  function_name = var.function_name
  role          = var.lambda_role_arn
  handler       = var.handler
  runtime       = var.runtime
  source_path      = var.source_path
  source_code_hash = filebase64sha256(var.filename)

  environment {
    variables = var.environment_variables
  }

  tags = var.tags
}
