variable "project_name" {
  description = "Name of the project"
  type = string
}

variable "environment" {
  description = "Deployment environment (e.g., dev, staging, prod)"
  type = string
}

variable "callback_urls" {
  description = "List of callback URLs"
  type = list(string)
}

variable "logout_urls" {
  description = "List of logout URLs"
  type = list(string)
}

variable "google_client_id" {
  description = "Google client ID"
  type = string
}
variable "google_client_secret" {
  description = "Google client secret"
  type = string
}

variable "facebook_client_id" {
  description = "Facebook client ID"
  type = string
}
variable "facebook_client_secret" {
  description = "Facebook client secret"
  type = string
}