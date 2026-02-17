# WalletApi

Wallet service used for testing economy + salary engine flows.  
All testing was done through Swagger.

The service stores data in memory, so balances reset when the API restarts.

Important: Wallet uses numeric enums

**currency**
0 = coins
1 = gems
2 = credits

**operation**
0 = add
1 = spend

### Sample inputs

Add coins (seed player)
{
  "player_id": "1",
  "currency": 0,
  "operation": 0,
  "amount": 5000
}

Spend coins
{
  "player_id": "1",
  "currency": 0,
  "operation": 1,
  "amount": 1500
}

Display wallet example response
{
  "player_id": "1",
  "coins": 3500,
  "gems": 0,
  "credits": 0
}

This API is used by SalaryEngineApi for deductions.  
To sanity check salary deductions, check wallet balance before and after triggering salary endpoints.