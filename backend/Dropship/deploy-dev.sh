#!/bin/bash

set -e

# ===== CONFIGURAÇÕES =====
FUNCTION_NAME="bff-dotnet-test"
REGION="us-east-1"
DEPLOY_PATH="./bin/Release/net8.0/Dropship.zip"
PROFILE="default"

# ===== PACKAGE =====
echo "📦 Gerando pacote com dotnet lambda package..."
dotnet lambda package

# ===== DEPLOY =====
echo "🚀 Atualizando código da Lambda..."
aws lambda update-function-code \
  --function-name $FUNCTION_NAME \
  --zip-file fileb://$DEPLOY_PATH \
  --region $REGION \
  --profile $PROFILE

echo "✅ Deploy concluído com sucesso!"