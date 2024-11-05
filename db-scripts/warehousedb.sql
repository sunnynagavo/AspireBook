-- SQL script to create the Warehouse database and table
USE [master]
GO

CREATE DATABASE [WarehouseDB]
GO

USE [WarehouseDB]
GO

-- create warehouseitems table
CREATE TABLE WarehouseItems (
    ItemID INT PRIMARY KEY IDENTITY(1,1),
    ItemName NVARCHAR(100) NOT NULL,
    Stock INT NOT NULL,
    LastUpdated DATETIME DEFAULT GETDATE()
);

-- create orders table
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    CustomerName NVARCHAR(100) NOT NULL,
    ItemID INT NOT NULL,
    Quantity INT NOT NULL,
    Status NVARCHAR(50) CHECK (Status IN ('Pending', 'Processing', 'Completed')),
    OrderDate DATETIME DEFAULT GETDATE(),
    LastUpdated DATETIME DEFAULT GETDATE()
);

-- Foreign key to link to Warehouse items
ALTER TABLE Orders
ADD FOREIGN KEY (ItemID) REFERENCES WarehouseDB.dbo.WarehouseItems(ItemID);

-- Insert a row into orders table
INSERT INTO dbo.Orders (CustomerName, ItemID, LastUpdated, OrderDate, Quantity, Status)
SELECT 'John Doe', ItemID, GETDATE(), GETDATE(), 10, 'Processing'
FROM dbo.WarehouseItems
WHERE ItemName = 'Tablet'
;

-- Insert sample data into warehouseitems
INSERT INTO WarehouseItems (ItemName, Stock) VALUES 
('Laptop', 50),
('Smartphone', 100),
('Tablet', 75);