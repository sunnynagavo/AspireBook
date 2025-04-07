from fastapi import Body, FastAPI, HTTPException
from dapr.clients import DaprClient
from dapr.ext.fastapi import DaprApp
from pydantic import BaseModel
import uvicorn
import os
import requests
import json
from datetime import datetime

app = FastAPI()
dapr_app = DaprApp(app)

class PaymentPayload(BaseModel):
    OrderID: str
    
@dapr_app.subscribe(pubsub='pubsub', topic='process-payment')
@app.post("/api/process-payment")
def process_payment(event_data = Body()):
    print("Received event data:", event_data)
    data = event_data.get("data")
    order_id = data.get("OrderID")
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

    PUBSUB_NAME = 'pubsub'
    TOPIC_NAME = 'ship-order'
    with DaprClient() as client:
        #Using Dapr SDK to publish a topic
        result = client.publish_event(
            pubsub_name=PUBSUB_NAME,
            topic_name=TOPIC_NAME,
            data=json.dumps({"OrderID": order_id}),
            data_content_type='application/json',
        )

    return response.json()

def main() -> None:
    port = int(os.environ.get("PORT", 8000))
    uvicorn.run(app, host="0.0.0.0", port=port)