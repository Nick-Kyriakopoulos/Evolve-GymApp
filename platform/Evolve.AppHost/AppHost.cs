var builder = DistributedApplication.CreateBuilder(args);
var postgres = builder.AddPostgres("postgres");
var membersDb = postgres.AddDatabase("members-db", databaseName: "members");

builder.AddDotnetProject("members-api", "../../services/Members/Evolve.Members.Api/Evolve.Members.Api.csproj")
    .WithHttpEndpoint()
    .WithReference(membersDb)
    .WaitFor(membersDb);

builder.Build().Run();

// Κάνει public την κλάση Program που φτιάχνει ο compiler από τα top-level statements,
// ώστε το Evolve.Integration.Tests να ξεκινά το AppHost με CreateAsync<Program>()
public partial class Program;
