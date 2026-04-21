using EconomyApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<WalletService>();
builder.Services.AddSingleton<HealthMonitoringService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Economy API v1"));

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "economy-api" }));

app.Logger.LogInformation("Economy Management API started.");
app.Logger.LogInformation("  POST /api/v1/wallet/simulate - One-week wallet simulation");
app.Logger.LogInformation("  GET  /api/v1/wallet/health - Economy health monitoring");
app.Logger.LogInformation("  GET  /api/v1/wallet/health/history - Health analysis history");
app.Logger.LogInformation("  GET  /api/v1/wallet/health/summary - Health status summary");
app.Logger.LogInformation("  GET  /api/v1/wallet/simulate/browser - Browser-friendly wallet simulation");
app.Logger.LogInformation("  GET  /health - API health check");
app.Logger.LogInformation("Docs: http://localhost:8000/swagger");

app.Run();
