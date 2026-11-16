namespace Evolve.Members.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var app = builder.Build();
        
        app.MapGet("/", () => "Evolve Members API is running!");
        
        app.Run();
    }
}

