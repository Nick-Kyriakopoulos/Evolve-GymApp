namespace Evolve.Members.Api;

public class Program
{
    public static void Main(string[] args)
    {
        // Create a new web application builder
        var builder = WebApplication.CreateBuilder(args);
        
        builder.AddServiceDefaults();
        

        var app = builder.Build();
        
        app.MapDefaultEndpoints();
        app.MapGet("/", () => "Evolve Members API is running!");
        
        app.Run();
        
        
    }
}

