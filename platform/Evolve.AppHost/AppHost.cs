var builder = DistributedApplication.CreateBuilder(args);

builder.AddDotnetProject("members-api", "../../services/Members/Evolve.Members.Api/Evolve.Members.Api.csproj")
    .WithHttpEndpoint();

builder.Build().Run();
