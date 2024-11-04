var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WarehouseAPI>("warehouseapi");

builder.Build().Run();
