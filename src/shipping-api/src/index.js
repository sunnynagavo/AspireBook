const { DaprServer, CommunicationProtocolEnum } = require("@dapr/dapr");
const express = require('express');
const axios = require('axios');
const bodyParser = require('body-parser');

const app = express();
app.use(bodyParser.json());

app.post('/api/ship-product', async (req, res) => {
    const { OrderID } = req.body;
    if (!OrderID) {
        return res.status(400).json({ detail: "Payload must contain 'OrderID'" });
    }

    try {
        await UpdateOrderStatus(OrderID);

        res.json(response.data);
    } catch (error) {
        res.status(500).json({ detail: `Error processing request: ${error.message}` });
    }
});

(async () => {
    const daprServer = new DaprServer({
      serverHost: "127.0.0.1",
      serverPort: process.env.PORT || 8000,
      serverHttp: app,
      clientOptions: {
        daprPort: process.env.DAPR_HTTP_PORT || 3500,
      },
    });
  
    await daprServer.pubsub.subscribe("pubsub", "ship-order", async (data) => {
        const OrderID = data.OrderID;
        console.log("Received event data:", JSON.stringify(OrderID, null, 2));
        await UpdateOrderStatus(OrderID);
        console.log(`Order ${OrderID} status updated.`);
    });
  
    await daprServer.start();
  })().catch(console.error);

async function UpdateOrderStatus(orderID) {
    const apiEndpoint = process.env.services__dab__http__0;

    // Read the order with OrderID from the same endpoint
    const url = `${apiEndpoint}/api/Orders/OrderID/${orderID}`;
    let response = await axios.get(url);
    if (response.status !== 200) {
        throw new Error(`Failed to retrieve order: ${response.data}`);
    }

    let orderData = response.data.value[0];

    // Remove the OrderID field from the order data
    delete orderData.OrderID;

    // Update the order data
    orderData.Status = "completed";
    orderData.LastUpdated = new Date().toISOString();

    // Print the updated order data for debug purposes
    console.log("Updated order data:", JSON.stringify(orderData, null, 2));

    // Update the order data
    const config = {
        headers: {
            'Content-Type': 'application/json'
        }
    };
    response = await axios.put(url, JSON.stringify(orderData, null, 2), config);

    // Update the warehouse stock based on the order details
    const warehouseUrl = `${apiEndpoint}/api/WarehouseItems/ItemID/${orderData.ItemID}`;
    let warehouseResponse = await axios.get(warehouseUrl);
    let warehouseItem = warehouseResponse.data.value[0];
    warehouseItem.Stock -= orderData.Quantity;
    delete warehouseItem.ItemID;

    // Print the updated warehouse quantity for debug purposes
    console.log("Updated warehouse data:", JSON.stringify(warehouseItem, null, 2));

    await axios.put(warehouseUrl, JSON.stringify(warehouseItem, null, 2), config);
}
