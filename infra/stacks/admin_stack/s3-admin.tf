resource "aws_s3_bucket" "admin" {
  bucket = "${var.project_name}-${var.environment}-admin-frontend"

  tags = merge(
    var.common_tags,
    {
      Name        = "${var.project_name}-admin-frontend"
      Environment = "${var.environment}"
    }
  )
}

resource "aws_s3_bucket_ownership_controls" "admin" {
  bucket = aws_s3_bucket.admin.id

  rule {
    object_ownership = "BucketOwnerPreferred"
  }
}

resource "aws_s3_bucket_public_access_block" "admin" {
  bucket = aws_s3_bucket.admin.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_policy" "allow_cloudfront_admin" {
  bucket = aws_s3_bucket.admin.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Sid    = "AllowCloudFrontFrontend"
      Effect = "Allow"
      Principal = {
        Service = "cloudfront.amazonaws.com"
      }
      Action   = "s3:GetObject"
      Resource = "arn:aws:s3:::${aws_s3_bucket.admin.id}/*"
      Condition = {
        StringEquals = {
          "AWS:SourceArn" = aws_cloudfront_distribution.admin_frontend.arn
        }
      }
    }]
  })
}
