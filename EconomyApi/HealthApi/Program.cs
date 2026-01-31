using HealthApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Health API", Version = "v1" });
});

builder.Services.AddSingleton<HealthMonitoringService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Health API v1"));

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "health-api" }));

app.Logger.LogInformation("Health API started.");
app.Logger.LogInformation("  GET  /api/v1/wallet/health - Economy health monitoring");
app.Logger.LogInformation("  GET  /api/v1/wallet/health/history - Health analysis history");
app.Logger.LogInformation("  GET  /api/v1/wallet/health/summary - Health status summary");
app.Logger.LogInformation("  GET  /health - API health check");
app.Logger.LogInformation("Docs: http://localhost:8002/swagger");

app.Run();
