public class FacilitiesService
{
    public FacilityUpgradeResult TryUpgradeFacility(string teamId, string playerFacilityId, string action)
    {
        if (string.IsNullOrWhiteSpace(teamId))
        {
            return new FacilityUpgradeResult
            {
                Success = false,
                Message = "TeamId is required."
            };
        }

        if (string.IsNullOrWhiteSpace(playerFacilityId))
        {
            return new FacilityUpgradeResult
            {
                Success = false,
                Message = "PlayerFacilityId is required."
            };
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            return new FacilityUpgradeResult
            {
                Success = false,
                Message = "Action is required."
            };
        }

        if (action != "start" && action != "confirm" && action != "rollback")
        {
            return new FacilityUpgradeResult
            {
                Success = false,
                Message = $"Unsupported action: {action}"
            };
        }

        return new FacilityUpgradeResult
        {
            Success = true,
            Message = $"FacilitiesService handled '{action}' for facility '{playerFacilityId}' on team '{teamId}'."
        };
    }
}