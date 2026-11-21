var builder = DistributedApplication.CreateBuilder(args);

builder.AddDotnetProject("members-api", "../../services/Members/Evolve.Members.Api/Evolve.Members.Api.csproj")
    .WithHttpEndpoint();

builder.Build().Run();

// Κάνει public την κλάση Program που φτιάχνει ο compiler από τα top-level statements,
// ώστε το Evolve.Integration.Tests να ξεκινά το AppHost με CreateAsync<Program>()
public partial class Program;
