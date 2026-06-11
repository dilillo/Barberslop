var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("barberslop");

builder.AddProject<Projects.Barberslop_Web>("web")
    .WithReference(postgres)
    .WaitFor(postgres);

builder.Build().Run();
