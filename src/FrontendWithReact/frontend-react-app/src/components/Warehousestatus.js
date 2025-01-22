import React, { useEffect, useState } from "react";
import "./App.css";

function WarehouseStatus() {
    const [warehouseItems, setWarehouseItems] = useState([]);

    const fetchWarehouseStatus = async () => {
        try {
            const response = await fetch("https://localhost:7262/api/warehousestatus", {
                headers: {
                    Accept: "application/json",
                    //enable cors
                    "Access-Control-Allow-Origin": "*",
                },
            });
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            const data = await response.json();
            setWarehouseItems(data);
        } catch (error) {
            console.error("Failed to fetch warehouse status:", error);
        }
    };

    useEffect(() => {
        fetchWarehouseStatus();
    }, []);

    return (
        <div className="App">
            <header className="App-header">
            <h1>Warehouse Status</h1>
            <table>
                <thead>
                    <tr>
                        <th>Item ID</th>
                        <th>Item Name</th>
                        <th>Stock</th>
                        <th>Last Updated</th>
                    </tr>
                </thead>
                <tbody>
                    {warehouseItems.map((item) => (
                        <tr key={item.ItemID}>
                            <td>{item.ItemID}</td>
                            <td>{item.ItemName}</td>
                            <td>{item.Stock}</td>
                            <td>{new Date(item.LastUpdated).toLocaleString()}</td>
                        </tr>
                    ))}
                </tbody>
                </table>
            </header>
        </div>
    );
}

export default WarehouseStatus;