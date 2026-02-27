import json
import logging

# Configura o logger básico
logger = logging.getLogger()
logger.setLevel(logging.INFO)

def lambda_handler(event, context):
    try:
        body = json.loads(event.get('body', '{}'))
        
        log_entry = {
            "level": body.get("level", "ERROR"),
            "frontend_message": body.get("message"),
            "user_email": body.get("userEmail"),
            "sessionId": body.get("sessionId"),
            "page_url": body.get("url"),
            "stack_trace": body.get("stack"),
            "error_message": body.get("errorMessage"),
            "environment": body.get("environment"),
            "timestamp": body.get("timestamp"),
            "userAgent": body.get("userAgent"),
            "client_ip": event.get('requestContext', {}).get('identity', {}).get('sourceIp'),
            "source": "FRONTEND_TELEMETRY" # Tag para facilitar filtros globais
        }

        print(json.dumps(log_entry))

        return {
            'statusCode': 200,
            'headers': {
                'Access-Control-Allow-Origin': '*', # Importante para o CORS do front
                'Content-Type': 'application/json'
            },
            'body': json.dumps({'status': 'sent'})
        }
        
    except Exception as e:
        logger.error(f"Erro ao processar telemetria: {str(e)}")
        return {'statusCode': 500, 'body': 'Internal Error'}