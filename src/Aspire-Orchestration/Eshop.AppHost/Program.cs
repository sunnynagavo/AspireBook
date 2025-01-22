using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add a SQL Server container
var sqlPassword = builder.AddParameter("sql-password");
var sqlServer = builder
    .AddSqlServer("sql", sqlPassword);

// uncomment this line to persist the database between runs
sqlServer.WithLifetime(ContainerLifetime.Persistent);

var sqlDatabase = sqlServer.AddDatabase("WarehouseDB");

// Populate the database with the schema and data
sqlServer
    .WithBindMount("./sql-server", target: "/usr/config")
    .WithBindMount("../../../db-scripts", target: "/docker-entrypoint-initdb.d")
    .WithEntrypoint("/usr/config/entrypoint.sh");

var dab = builder.AddDataAPIBuilder("dab", "../../../dab/dab-config.json")
    .WithReference(sqlDatabase)
    .WaitFor(sqlServer);

var warehouseApi = builder.AddProject<Projects.WarehouseAPI>("warehouseapi")
    .WithReference(dab)
    .WaitFor(dab);

builder.AddGolangApp("create-order", "../../create-order-api")
    .WithHttpEndpoint(port: 5001, env: "PORT")
    .WithReference(dab)
    .WaitFor(dab);

builder.AddUvApp("process-payment", "../../process-payment-api", "process-payment-api")
    .WithHttpEndpoint(port: 5002, env: "PORT")

var createorderApi = builder.AddGolangApp("create-order", "../../create-order-api")
    .WithHttpEndpoint(env: "PORT")
    .WithReference(dab)
    .WaitFor(dab);

// Add the React front-end project
builder.AddNpmApp("FrontendWithReact", "../../FrontendWithReact/frontend-react-app")
    .WithNpmPackageInstallation()
    .WithReference(warehouseApi)
    .WaitFor(warehouseApi)
    .WithReference(createorderApi)
    .WaitFor(createorderApi)
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("BROWSER", "none") // Disable opening browser on npm start
    .WithExternalHttpEndpoints()
    .WaitFor(dab)
    .PublishAsDockerFile();

builder.Build().Run();
