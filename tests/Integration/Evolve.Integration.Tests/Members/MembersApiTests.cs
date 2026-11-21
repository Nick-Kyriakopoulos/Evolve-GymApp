using System.Net;
using Aspire.Hosting.Testing;
using Xunit;

namespace Evolve.Integration.Tests.Members;

public sealed class MembersApiTests
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);

    [Fact]
    public async Task Members_Api_Accepts_Requests_When_System_Starts()
    {
        // Arrange
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(StartupTimeout);
        var cancellationToken = cts.Token;
        
        await using var builder = await DistributedApplicationTestingBuilder.CreateAsync<Program>(cancellationToken);
        await using var app = await builder.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("members-api", cancellationToken);
        
        // Act
        using var client = app.CreateHttpClient("members-api");
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), cancellationToken);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
