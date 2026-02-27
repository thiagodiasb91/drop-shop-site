module "lambda_bff_get_user_session" {
  source = "terraform-aws-modules/lambda/aws"
  function_name = "telemetry-frontend-logs"
  create_role   = false
  lambda_role   = var.DEFAULT_LAMBDA_ROLE_ARN
  description   = "Lambda to log errors from the frontend"
  handler       = "index.lambda_handler"
  runtime       = "python3.14"
  source_path   = "${path.module}/../src/"
  publish       = true
  layers = var.lambda_layers
  environment_variables = merge(var.environment_variables, {})
  tags = merge(var.tags, {
    project = "drop-shop-site",
    environment = var.environment
  })
}
