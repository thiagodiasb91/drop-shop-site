variable "common_tags" {
    description = "Common tags to apply to all resources"
    type        = map(string)  
}

variable "admin_site_bucket_name" {
    description = "Name of the S3 bucket for the admin site"
    type        = string
}