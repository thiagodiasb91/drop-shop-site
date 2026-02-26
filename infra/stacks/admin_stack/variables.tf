variable "project_name" {
    description = "Name of the project"
    type = string
}

variable "environment" {
    description = "Deployment environment (e.g., dev, staging, prod)"
    type = string
}

variable "common_tags" {
    description = "Common tags to apply to all resources"
    type = map(string)  
}

variable "aws_profile" {
  description = "AWS Profile to use for local deployment"
  type        = string
  default     = null
}