output "admin_backend_cloudfront_domain" {
  value = aws_cloudfront_distribution.admin_backend.domain_name
}

output "admin_backend_cloudfront_id" {
  value = aws_cloudfront_distribution.admin_backend.id
}