# 🌍 CloudFront - BACKEND
resource "aws_cloudfront_distribution" "admin_backend" {
  comment = "Drop Shop Admin Backend API"
  enabled = true

  origin {
    domain_name = var.api_gateway_domain_name
    origin_id   = "admin-api-origin"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "https-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  default_cache_behavior {
    target_origin_id       = "admin-api-origin"
    viewer_protocol_policy = "redirect-to-https"

    allowed_methods = ["HEAD", "GET", "OPTIONS", "POST", "PUT", "PATCH", "DELETE"]
    cached_methods  = ["GET", "HEAD"]

    cache_policy_id          = aws_cloudfront_cache_policy.no_cache.id
    origin_request_policy_id = "b689b0a8-53d0-40ab-baf2-68738e2966ac" # AllViewerExceptHostHeader

    response_headers_policy_id = aws_cloudfront_response_headers_policy.backend_cors.id
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

resource "aws_cloudfront_cache_policy" "no_cache" {
  name = "no-cache-backend"

  default_ttl = 0
  max_ttl     = 0
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


resource "aws_cloudfront_response_headers_policy" "backend_cors" {
  name = "backend-cors-auth"

  cors_config {
    access_control_allow_credentials = false

    access_control_allow_headers {
      items = [
        "Content-Type",
        "X-Amz-Date",
        "Authorization",
        "X-Api-Key",
        "X-Amz-Security-Token",
        "X-Amz-User-Agent"
      ]
    }

    access_control_allow_methods {
      items = [
        "POST", 
        "OPTIONS",
        "GET",
        "PUT",
        "PATCH",
        "DELETE"
      ]
    }

    access_control_allow_origins {
      items = [
        "https://d35nbs4n8cbsw3.cloudfront.net",
        "https://duz838qu40buj.cloudfront.net",
        "http://localhost:5173"
      ]
    }

    origin_override = false
  }
}
