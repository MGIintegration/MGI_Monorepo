public class BudgetHudMiddleware
{
    private readonly EconomyService economyService;
    private readonly FacilitiesService facilitiesService;

    public BudgetHudMiddleware()
    {
        economyService = new EconomyService();
        facilitiesService = new FacilitiesService();
    }

    public BudgetHudResult TryGetBudget(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            playerId = FacilitiesService.DefaultPlayerId;
        }

        var wallet = economyService.GetWallet(playerId);
        var rehabEffects = facilitiesService.GetFacilityEffects(playerId, "rehab_center");

        float recoveryBoostPercent = 0f;
        if (rehabEffects.TryGetValue("InjuryRecoveryMultiplier", out var multiplier))
        {
            recoveryBoostPercent = (multiplier - 1f) * 100f;
        }

        return new BudgetHudResult
        {
            Success = true,
            Budget = wallet.coins,
            RecoveryBoostPercent = recoveryBoostPercent,
            Message = "Budget loaded from EconomyService/FacilitiesService."
        };
    }
}
