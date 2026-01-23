output "admin_cloudfront_domain" {
  value = aws_cloudfront_distribution.admin_distribution.domain_name
}
