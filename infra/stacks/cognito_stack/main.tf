resource "aws_cognito_user_pool" "admin" {
  name = "${var.project_name}-admin-pool"

  # self_signup_enabled = false

  username_attributes = ["email"]
  auto_verified_attributes = ["email"]

  password_policy {
    minimum_length = 8
    require_lowercase = true
    require_uppercase = true
    require_numbers   = true
    require_symbols   = false
  }

  schema {
    name     = "email"
    attribute_data_type = "String"
    required = true
  }
}

resource "aws_cognito_identity_provider" "google" {
  user_pool_id  = aws_cognito_user_pool.admin.id
  provider_name = "Google"
  provider_type = "Google"

  provider_details = {
    client_id     = var.google_client_id
    client_secret = var.google_client_secret
    authorize_scopes = "openid email profile"
  }

  attribute_mapping = {
    email = "email"
    name  = "name"
  }
}

resource "aws_cognito_identity_provider" "facebook" {
  user_pool_id  = aws_cognito_user_pool.admin.id
  provider_name = "Facebook"
  provider_type = "Facebook"

  provider_details = {
    client_id     = var.facebook_client_id
    client_secret = var.facebook_client_secret
    authorize_scopes = "email public_profile"
  }

  attribute_mapping = {
    email = "email"
    name  = "name"
  }
}


resource "aws_cognito_user_pool_client" "admin_web" {
  name         = "${var.project_name}-admin-web"
  user_pool_id = aws_cognito_user_pool.admin.id

  generate_secret = false

  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows  = ["code"]
  allowed_oauth_scopes = ["openid", "email", "profile"]

  callback_urls = var.callback_urls
  logout_urls   = var.logout_urls

  supported_identity_providers = [
    "COGNITO",
    "Google",
    "Facebook"
  ]

  explicit_auth_flows = [
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH"
]

  depends_on = [ 
    aws_cognito_identity_provider.facebook,
    aws_cognito_identity_provider.google
  ]
}

resource "aws_cognito_user_pool_domain" "admin" {
  domain       = "${var.project_name}-admin-auth"
  user_pool_id = aws_cognito_user_pool.admin.id
}
