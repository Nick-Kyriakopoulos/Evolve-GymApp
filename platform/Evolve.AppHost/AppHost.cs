var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Evolve_Members_Api>("members-api")
    .WithHttpEndpoint();

builder.Build().Run();
