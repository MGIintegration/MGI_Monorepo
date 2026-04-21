# SalaryEngineApi

Implements salary contracts, weekly cost calculation and deductions.  
All testing was done through Swagger.

WalletApi and SalaryEngineApi are hardcoded locally to run on different ports during testing.

Register / Details / Calculate endpoints only compute values and do not change balance.  
Trigger endpoints deduct coins and return new_balance.

Before testing bulk or weekly trigger, first seed wallet coins in WalletApi and register contracts for the players.

### Sample inputs and recommended test flow

**1. Seed wallet** (WalletApi)
```json
{
  "player_id": "1",
  "currency": 0,
  "operation": 0,
  "amount": 5000
}
```

**2. Register contract**
```json
{
  "player_id": "1",
  "base_salary": 4000,
  "bonus_multiplier": 1,
  "performance_threshold": 0.5,
  "max_bonus_percentage": 0.5
}
```

**3. Calculate weekly** (preview only)

player_id: 1
```json
{
  "leads_generated": 60,
  "conversion_rate": 0.5,
  "quality_score": 80,
  "team_performance": 70
}
```

**4. Trigger weekly deduction**

(same input as calculate weekly)

Check WalletApi balance before and after to verify deduction.

**5. Trigger bulk**

Contracts must exist for both players beforehand.

```json
[
  {
    "player_id": "1",
    "leads_generated": 60,
    "conversion_rate": 0.5,
    "quality_score": 80,
    "team_performance": 70
  },
  {
    "player_id": "2",
    "leads_generated": 20,
    "conversion_rate": 0.2,
    "quality_score": 40,
    "team_performance": 30
  }
]
```

- Salary deductions are applied in coins only.
- Contracts and balances reset on API restart.
