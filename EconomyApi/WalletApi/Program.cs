using WalletApi.Repos;
using WalletApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-memory store 
builder.Services.AddSingleton<IWalletRepository, InMemoryWalletRepository>();
builder.Services.AddScoped<IWalletService, WalletService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();