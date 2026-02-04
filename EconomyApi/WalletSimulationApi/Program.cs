using WalletSimulationApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Wallet Simulation API", Version = "v1" });
});

builder.Services.AddSingleton<WalletService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Wallet Simulation API v1"));

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "wallet-simulation-api" }));

app.Logger.LogInformation("Wallet Simulation API started.");
app.Logger.LogInformation("  POST /api/v1/wallet/simulate - One-week wallet simulation");
app.Logger.LogInformation("  GET  /api/v1/wallet/simulate/browser - Browser-friendly wallet simulation");
app.Logger.LogInformation("  GET  /health - API health check");
app.Logger.LogInformation("Docs: http://localhost:8001/swagger");

app.Run();
