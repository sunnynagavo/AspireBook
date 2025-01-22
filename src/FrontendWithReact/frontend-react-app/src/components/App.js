import { useEffect, useState } from "react";
import "./App.css";

function App() {
  const [forecasts, setForecasts] = useState([]);

  const requestWarehouse = async () => {
    const warehouse = await fetch("/api/warehousestatus");
      console.log(warehouse);

      const warehouseJson = await warehouse.json();
      console.log(warehouseJson);

      setForecasts(warehouseJson);
  };

  useEffect(() => {
      requestWarehouse();
  }, []);

  return (
    <div className="App">
      <header className="App-header">
        <h1>Warehouse Status</h1>
        <table>
          <thead>
            <tr>
              <th>ItemId</th>
              <th>ItemName</th>
              <th>Stock</th>
              <th>Last Updated</th>
            </tr>
          </thead>
          <tbody>
            {(
              forecasts ?? [
                {
                  ItemId: "N/A",
                  ItemName: "",
                  Stock: "",
                  LastUpdated: "No forecasts",
                },
              ]
            ).map((w) => {
              return (
                <tr key={w.ItemId}>
                  <td>{w.ItemName}</td>
                  <td>{w.Stock}</td>
                  <td>{w.LastUpdated}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </header>
    </div>
  );
}

export default App;
