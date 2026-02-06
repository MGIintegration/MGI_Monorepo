# Forecast and Transaction API (C#)

C# port of the Python Forecast and Transaction APIs. These are separate microservices that run independently.

## Requirements

- .NET 10 SDK

## Project structure

| Project | Port | Description |
|---------|------|-------------|
| **ForecastApi** | 8003 | Economy forecasting (POST forecast) |
| **TransactionApi** | 8004 | Transaction history (GET transactions) |

## Run the separate APIs

### Forecast Api – economy forecasting (port 8003)

```bash
cd EconomyApi/ForecastApi
dotnet run
```

- API: http://localhost:8003  
- Swagger: http://localhost:8003/swagger  
- Health: http://localhost:8003/health  

**Endpoints:**

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/forecast` | Economy forecast based on income and expenses (JSON body) |
| GET | `/health` | API health check |

### Transaction Api – transaction history (port 8004)

```bash
cd EconomyApi/TransactionApi
dotnet run
```

- API: http://localhost:8004  
- Swagger: http://localhost:8004/swagger  
- Health: http://localhost:8004/health  

**Endpoints:**

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/transactions` | Transaction history (query: `player_id`) |
| GET | `/health` | API health check |

## Build

You can build each project individually:

```bash
cd EconomyApi/ForecastApi
dotnet build
```

```bash
cd EconomyApi/TransactionApi
dotnet build
```

## Layout

- **ForecastApi/** – Economy forecasting: Models, Services, Controllers, Program.cs
- **TransactionApi/** – Transaction history: Models, Services, Controllers, Program.cs
