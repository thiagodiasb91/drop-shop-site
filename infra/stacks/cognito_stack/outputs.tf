output "user_pool_id" {
  value = aws_cognito_user_pool.admin.id
}

output "client_id" {
  value = aws_cognito_user_pool_client.admin_web.id
}

output "cognito_domain" {
  value = aws_cognito_user_pool_domain.admin.domain
}
