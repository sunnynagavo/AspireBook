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

    const apiEndpoint = process.env.services__dab__http__0;
    if (!apiEndpoint) {
        return res.status(500).json({ detail: "Environment variable 'services__dab__http__0' is not set" });
    }

    try {
        // Read the order with OrderID from the same endpoint
        const url = `${apiEndpoint}/api/Orders/OrderID/${OrderID}`;
        let response = await axios.get(url);
        if (response.status !== 200) {
            return res.status(500).json({ detail: `Failed to retrieve order: ${response.data}` });
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

        // Print the updated warehouese quantity for debug purposes
        console.log("Updated warehouse data:", JSON.stringify(warehouseItem, null, 2));

        await axios.put(warehouseUrl, JSON.stringify(warehouseItem, null, 2), config);

        res.json(response.data);
    } catch (error) {
        res.status(500).json({ detail: `Error processing request: ${error.message}` });
    }
});

const port = process.env.PORT || 8000;
app.listen(port, () => {
    console.log(`Server is running on port ${port}`);
});
