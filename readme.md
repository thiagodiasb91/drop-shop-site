# Projeto Drop Shop Admin - Readme

## Arquitetura
Admin SPA (React/Vite)
  └─ CloudFront + S3
        ↓
Cognito User Pool
        ↓ (JWT)
API Gateway (Authorizer Cognito)
        ↓
Lambda (Python)

---

Front só consome userPoolId, clientId, domain
