resource "aws_s3_bucket" "core" {
  bucket = "drop-shop-core"
}

resource "aws_s3_bucket_versioning" "core" {
  bucket = aws_s3_bucket.core.id
  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "core" {
  bucket = aws_s3_bucket.core.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm = "AES256"
    }
  }
}