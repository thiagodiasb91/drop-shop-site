output "admin_cloudfront_domain" {
  value = aws_cloudfront_distribution.admin_frontend.domain_name
}

output "admin_cloudfront_id" {
  value = aws_cloudfront_distribution.admin_frontend.id
}