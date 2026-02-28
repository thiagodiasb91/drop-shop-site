terraform {
  backend "s3" {
    bucket         = "drop-shop-core"
    key            = "tf-state/backend/lambda-logs-front.tfstate"
    region         = "us-east-1"
    encrypt        = true
    use_lockfile   = true
  }
}
