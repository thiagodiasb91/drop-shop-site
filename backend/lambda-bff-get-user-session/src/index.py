import json

def get_headers(origin):
    ALLOWED_ORIGINS = {
        "http://localhost:5173",
        "https://admin.drop-shop.com"
    }

    if origin in ALLOWED_ORIGINS:
        return {
            "Content-Type": "application/json",
            "Access-Control-Allow-Origin": origin,
            "Access-Control-Allow-Credentials": "true",
            "Access-Control-Allow-Headers": "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token",
            "Access-Control-Allow-Methods": "GET,POST,OPTIONS"
        }

    return {}

def lambda_handler(event, context):
    print("lambda_handler.request", event)
    requestContext = event.get("requestContext", {})
    authorizer = requestContext.get("authorizer", {})
    origin = event.get("headers", {}).get("origin", "")
    print("origin", origin)
    print("authorizer", authorizer)

    principal_id = authorizer.get("principalId")
    email = authorizer.get("email")
  
    body_response = {
        "user_id": principal_id,
        "email": email,
    }
    print("body_response", body_response)

    return {
        "statusCode": 200,
        "headers": get_headers(origin),
        "body": json.dumps(body_response),
    }