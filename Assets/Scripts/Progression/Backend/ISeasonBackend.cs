using System;
using System.Collections.Generic;


public interface ISeasonBackend
{
    void CreateSeason(List<string> teamNames, string playerTeamName, Action<SeasonSaveData> onSuccess, Action<string> onError = null);
   
    void SimulateWeek(SeasonSaveData currentSeason, Action<SeasonSaveData> onSuccess, Action<string> onError = null);

    void GetPlayerProgression(string playerId, Action<PlayerProgressionSaveData> onSuccess, Action<string> onError = null);

    void GetLocalProgression(string playerId, Action<PlayerProgressionState> onSuccess, Action<string> onError = null);

    void AddPlayerXp(string playerId, int xpAmount, string source, Action<PlayerProgressionState> onSuccess, Action<string> onError = null);
}
