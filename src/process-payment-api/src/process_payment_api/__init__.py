from fastapi import FastAPI
import uvicorn
import os
import requests

app = FastAPI()

@app.get("/api/process-payment")
def process_payment(payload):
    order_id = payload.get('OrderID')
    if not order_id:
        raise ValueError("Payload must contain 'order_id'")

    api_endpoint = os.environ.get("services__dab__http__0") + "/api/Orders"
    url = f"{api_endpoint}/{order_id}"
    data = {"Status": "processing"}

    response = requests.put(url, json=data)
    if response.status_code != 200:
        raise Exception(f"Failed to update order status: {response.text}")

    return response.json()

def main() -> None:
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run(app, host="127.0.0.1", port=port)