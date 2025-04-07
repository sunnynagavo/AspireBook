import React from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import WarehouseStatus from "./components/Warehousestatus";

const container = document.getElementById("root");
const root = createRoot(container);
root.render(
  <React.StrictMode>
    <WarehouseStatus />
  </React.StrictMode>
);