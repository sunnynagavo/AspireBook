from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import uvicorn
import os
import requests
from datetime import datetime

app = FastAPI()

class PaymentPayload(BaseModel):
    OrderID: str

@app.post("/api/process-payment")
def process_payment(payload: PaymentPayload):
    order_id = payload.OrderID
    if not order_id:
        raise HTTPException(status_code=400, detail="Payload must contain 'OrderID'")

    api_endpoint = os.environ.get("services__dab__http__0")
    if not api_endpoint:
        raise HTTPException(status_code=500, detail="Environment variable 'services__dab__http__0' is not set")

    # Read the order with OrderID from the same endpoint
    url = f"{api_endpoint}/api/Orders/OrderID/{order_id}"
    response = requests.get(url)
    if response.status_code != 200:
        raise HTTPException(status_code=500, detail=f"Failed to retrieve order: {response.text}")

    order_data = response.json()

    # Extract the first item from the array
    order_data = order_data["value"][0]

    # Remove the OrderID field from the order data
    if "OrderID" in order_data:
        del order_data["OrderID"]
    # Update the order data
    order_data["Status"] = "processing"
    order_data["LastUpdated"] = datetime.utcnow().isoformat()

    # Print the updated order data for debug purposes
    print("Updated order data:", order_data)

    # Send the updated order data as an update request
    response = requests.put(url, json=order_data)
    if response.status_code != 200:
        raise HTTPException(status_code=500, detail=f"Failed to update order status: {response.text}")

    return response.json()

def main() -> None:
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run(app, host="0.0.0.0", port=port)