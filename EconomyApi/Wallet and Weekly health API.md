# Economy API (C#)

C# port of the Python `/wallet/simulate` and `/wallet/health` APIs. The implementation is split into **two separate APIs** that can run independently.

## Requirements

- .NET 10 SDK

## How to run the API

**Option A – Run the combined API (all endpoints on one port):**

```bash
cd EconomyApi
dotnet run
```

- API: http://localhost:8000  
- Swagger: http://localhost:8000/swagger  
- Health: http://localhost:8000/health  

**Option B – Run Wallet and Health APIs separately:**

```bash
# Terminal 1 – Wallet API (weekly simulation)
cd EconomyApi/WalletApi
dotnet run

# Terminal 2 – Weekly Health API
cd EconomyApi/HealthApi
dotnet run
```

- Wallet API: http://localhost:8001 (Swagger: http://localhost:8001/swagger)  
- Weekly Health API: http://localhost:8002 (Swagger: http://localhost:8002/swagger)  

## Project structure

| Project | Port | Description |
|---------|------|-------------|
| **WalletApi** | 8001 | Wallet simulation only (POST/GET simulate) |
| **HealthApi** | 8002 | Economy health monitoring only (health, history, summary) |
| **EconomyApi** (legacy) | 8000 | Combined API (all endpoints in one app) |

## Run the separate APIs

### Wallet API – weekly simulation (port 8001)

```bash
cd EconomyApi/WalletApi
dotnet run
```

- API: http://localhost:8001  
- Swagger: http://localhost:8001/swagger  
- Health: http://localhost:8001/health  

**Endpoints:**

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/wallet/simulate` | One-week wallet simulation (JSON body) |
| GET | `/api/v1/wallet/simulate/browser` | Wallet simulation via query params |
| GET | `/health` | API health check |

### Weekly Health API (port 8002)

```bash
cd EconomyApi/HealthApi
dotnet run
```

- API: http://localhost:8002  
- Swagger: http://localhost:8002/swagger  
- Health: http://localhost:8002/health  

**Endpoints:**

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/wallet/health` | Economy health (query: `player_id`, `analysis_period_weeks`, etc.) |
| GET | `/api/v1/wallet/health/history` | Health history for a player |
| GET | `/api/v1/wallet/health/summary` | Health summary for a player |
| GET | `/health` | API health check |

## Build all projects

From the `EconomyApi` folder:

```bash
dotnet build EconomyApi.sln
```

## Run combined API (legacy)

Same as **How to run the API → Option A**. From the repo root:

```bash
cd EconomyApi
dotnet run
```

- API: http://localhost:8000  
- Swagger: http://localhost:8000/swagger  

## Layout

- **WalletApi/** – Wallet simulation: Models, Processors, Services, Controllers, Program.cs  
- **HealthApi/** – Health monitoring: Models, Processors, Services, Controllers, Program.cs  
- **EconomyApi/** (root) – Legacy combined app: EconomyController, shared Models/Services/Processors  

Health logs from HealthApi are written to `economy_health_logs.json` in the process working directory.
