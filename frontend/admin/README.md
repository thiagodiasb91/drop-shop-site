# Rodar front
npm run dev

# Rodar front utilizando a build
npm run build && npm run preview

# Subir front end no s3
npm run build

aws s3 sync dist/ s3://drop-shop-admin-frontend \
  --delete \
  --cache-control "max-age=31536000,public"

# Como invalidar o cache do cloudfront?
aws cloudfront create-invalidation \
  --distribution-id E2A7MA05O5ZR0P \
  --paths "/*"

aws s3 sync dist/ s3://drop-shop-admin-frontend \
  --delete \
  --cache-control "max-age=31536000,public" && \
aws cloudfront create-invalidation \
  --distribution-id E2A7MA05O5ZR0P \
  --paths "/*"

## Backend
aws cloudfront create-invalidation \
  --distribution-id E1VO270OC90WWS \
  --paths "/*"

# Stack

## Frontend
- HTML
- CSS
- JavaScript (ES Modules)
- Alpine.js (CDN)
- Fetch API

## Backend
- Cognito (Hosted UI)
- API Gateway + Lambda
- JWT no cookie

## Infra
- S3 + CloudFront