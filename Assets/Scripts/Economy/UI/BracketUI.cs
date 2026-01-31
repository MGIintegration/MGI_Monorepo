using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class BracketUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Transform tableParent;      
    public GameObject rowPrefab;       

    void OnEnable()
    {
        RefreshBracket();
    }

    public void RefreshBracket()
    {
        var sm = SeasonManager.Instance;
        
        // Safety Checks
        if (sm == null || sm.Teams == null || sm.Teams.Count == 0)
        {
            Debug.LogWarning("BracketUI: No season data available yet.");
            return;
        }

        if (titleText != null)
            titleText.text = $"WEEK {sm.CurrentWeek} — TEAM STANDINGS";

        for (int i = tableParent.childCount - 1; i >= 0; i--)
        {
            Transform child = tableParent.GetChild(i);
            child.SetParent(null); 
            Destroy(child.gameObject);
        }

        // 2. Sort Teams (Points -> Wins)
        var standings = sm.Teams
            .OrderByDescending(t => t.stats.points)
            .ThenByDescending(t => t.stats.wins)
            .ToList();

        int rank = 1;

        foreach (var team in standings)
        {
            var row = Instantiate(rowPrefab, tableParent);
            
            var fields = row.GetComponentsInChildren<TextMeshProUGUI>();

            if (fields.Length >= 6)
            {
                fields[0].text = rank.ToString();
                
                fields[1].text = team.team_name;

                fields[2].text = team.stats.wins.ToString();

                fields[3].text = team.stats.losses.ToString();

                fields[4].text = team.stats.points.ToString();

                if (team.is_player_team)
                {
                    fields[5].text = sm.PlayerXP.ToString(); // Or use a variable from team.stats if you added one
                    
                    // Highlight the Player's row in Yellow
                    foreach(var txt in fields) txt.color = new Color(1f, 0.8f, 0f); // Gold/Yellow
                }
                else
                {
                    fields[5].text = "-";

                    foreach(var txt in fields) txt.color = Color.white;
                }
                Debug.Log($"Added Row: Rank {rank}, Team {team.team_name}, Wins {team.stats.wins}, Losses {team.stats.losses}, Points {team.stats.points}, XP {(team.is_player_team ? sm.PlayerXP.ToString() : "-")}");
            }
            else
            {
                Debug.LogError($"Bracket Row Prefab only has {fields.Length} text fields. Needed 6.");
            }

            rank++;
        }
    }
}