using SalaryEngineApi.Repos;
using SalaryEngineApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Wallet API client (your WalletApi is on 5002)
builder.Services.AddHttpClient<IWalletClient, WalletClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5002");
});

// Register the missing repository (THIS FIXES YOUR 500)
builder.Services.AddSingleton<ISalaryContractRepository, InMemorySalaryContractRepository>();

// Salary engine service
builder.Services.AddSingleton<ISalaryEngineService, SalaryEngineService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Salary Engine API v1"));

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "salary-engine-api" }));

app.Run();