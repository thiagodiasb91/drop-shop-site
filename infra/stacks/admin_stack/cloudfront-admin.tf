# 🔐 OAC para S3
resource "aws_cloudfront_origin_access_control" "admin_oac" {
  name                              = "${var.project_name}-admin-oac"
  origin_access_control_origin_type = "s3"
  signing_behavior                  = "always"
  signing_protocol                  = "sigv4"
}

# 🌍 CloudFront - FRONTEND
resource "aws_cloudfront_distribution" "admin_frontend" {
  comment = "Drop Shop Admin Frontend (${var.environment})"
  enabled             = true
  default_root_object = "index.html"

  origin {
    domain_name              = aws_s3_bucket.admin.bucket_regional_domain_name
    origin_id                = "admin-s3-origin"
    origin_access_control_id = aws_cloudfront_origin_access_control.admin_oac.id
  }

  default_cache_behavior {
    target_origin_id       = "admin-s3-origin"
    viewer_protocol_policy = "redirect-to-https"

    allowed_methods = ["GET", "HEAD"]
    cached_methods  = ["GET", "HEAD"]

    cache_policy_id = aws_cloudfront_cache_policy.static.id
  }

  # ⚠️ SPA fallback (APENAS AQUI)
  custom_error_response {
    error_code         = 403
    response_code      = 200
    response_page_path = "/index.html"
  }

  custom_error_response {
    error_code         = 404
    response_code      = 200
    response_page_path = "/index.html"
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }
}

resource "aws_cloudfront_cache_policy" "static" {
  name = "static-assets"

  default_ttl = 86400
  max_ttl     = 31536000
  min_ttl     = 0

  parameters_in_cache_key_and_forwarded_to_origin {
    headers_config {
      header_behavior = "none"
    }

    cookies_config {
      cookie_behavior = "none"
    }

    query_strings_config {
      query_string_behavior = "none"
    }
  }
}
