var builder = DistributedApplication.CreateBuilder(args);

// Add a SQL Server container
var sqlPassword = builder.AddParameter("sql-password");
var sqlServer = builder
    .AddSqlServer("sql", sqlPassword);

// uncomment this line to persist the database between runs
// sqlServer.WithLifetime(ContainerLifetime.Persistent);

var sqlDatabase = sqlServer.AddDatabase("WarehouseDB");

// Populate the database with the schema and data
sqlServer
    .WithBindMount("./sql-server", target: "/usr/config")
    .WithBindMount("../../../db-scripts", target: "/docker-entrypoint-initdb.d")
    .WithEntrypoint("/usr/config/entrypoint.sh");

// Add Data API Builder using dab-config.json 
var dab = builder.AddContainer("dab", "mcr.microsoft.com/azure-databases/data-api-builder", "latest")
    .WithHttpEndpoint(targetPort: 5000, name: "http")
    .WithOtlpExporter()
    .WithBindMount("../../../dab/dab-config.json", target: "/App/dab-config.json")
    .WithReference(sqlDatabase)
    .WaitFor(sqlServer);
var dabServiceEndpoint = dab.GetEndpoint("http");

builder.AddProject<Projects.WarehouseAPI>("warehouseapi")
    .WithReference(dabServiceEndpoint)
    .WaitFor(dab);

builder.Build().Run();
