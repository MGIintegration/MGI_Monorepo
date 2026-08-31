using UnityEngine;
using TMPro;

public class CoachComparePopulator : MonoBehaviour
{
    public TextMeshProUGUI slottedNameText;
    public TextMeshProUGUI slottedRatingText;
    public TextMeshProUGUI slottedSalaryText;
    public TextMeshProUGUI slottedContractText;
    public TextMeshProUGUI slottedStat1Text;
    public TextMeshProUGUI slottedStat2Text;
    public TextMeshProUGUI slottedStat3Text;
    public TextMeshProUGUI slottedStat4Text;
    public TextMeshProUGUI slottedSpecialtyText;

    public TextMeshProUGUI candidateNameText;
    public TextMeshProUGUI candidateRatingText;
    public TextMeshProUGUI candidateSalaryText;
    public TextMeshProUGUI candidateContractText;
    public TextMeshProUGUI candidateStat1Text;
    public TextMeshProUGUI candidateStat2Text;
    public TextMeshProUGUI candidateStat3Text;
    public TextMeshProUGUI candidateStat4Text;
    public TextMeshProUGUI candidateSpecialtyText;

    public TextMeshProUGUI stat1Label;
    public TextMeshProUGUI stat2Label;
    public TextMeshProUGUI stat3Label;
    public TextMeshProUGUI stat4Label;

    private CoachData candidateDerived;
    private CoachData slottedDerived;

    public void Populate(CoachDatabaseRecord candidate, CoachDatabaseRecord slotted)
    {
        ReleaseDerivedInstances();

        SetStatLabels(candidate.coach_type);

        candidateDerived = PopulateColumn(candidate, candidateNameText, candidateRatingText, candidateSalaryText,
            candidateContractText, candidateStat1Text, candidateStat2Text, candidateStat3Text,
            candidateStat4Text, candidateSpecialtyText);

        if (slotted != null)
        {
            slottedDerived = PopulateColumn(slotted, slottedNameText, slottedRatingText, slottedSalaryText,
                slottedContractText, slottedStat1Text, slottedStat2Text, slottedStat3Text,
                slottedStat4Text, slottedSpecialtyText);
        }
        else
        {
            slottedNameText.text = "No coach slotted";
            slottedRatingText.text = "";
            slottedSalaryText.text = "";
            slottedContractText.text = "";
            slottedStat1Text.text = "";
            slottedStat2Text.text = "";
            slottedStat3Text.text = "";
            slottedStat4Text.text = "";
            slottedSpecialtyText.text = "";
        }
    }

    private void SetStatLabels(string coachType)
    {
        switch (coachType)
        {
            case "D":
                stat1Label.text = "RUN DEFENSE";
                stat2Label.text = "PRESSURE CONTROL";
                stat3Label.text = "COVERAGE DISCIPLINE";
                stat4Label.text = "TURNOVER";
                break;
            case "O":
                stat1Label.text = "PASSING EFFICIENCY";
                stat2Label.text = "RUSH";
                stat3Label.text = "RED ZONE CONVERSION";
                stat4Label.text = "PLAY VARIATION";
                break;
            case "S":
                stat1Label.text = "FIELD GOAL ACCURACY";
                stat2Label.text = "KICKOFF DISTANCE";
                stat3Label.text = "RETURN SPEED";
                stat4Label.text = "RETURN COVERAGE";
                break;
        }
    }

    private int CalculateBonus(float statValue) =>
        Mathf.RoundToInt(Mathf.Clamp(statValue * 5f, 0f, 50f));

    private CoachData PopulateColumn(CoachDatabaseRecord record, TextMeshProUGUI nameText,
        TextMeshProUGUI ratingText, TextMeshProUGUI salaryText, TextMeshProUGUI contractText,
        TextMeshProUGUI stat1Text, TextMeshProUGUI stat2Text, TextMeshProUGUI stat3Text,
        TextMeshProUGUI stat4Text, TextMeshProUGUI specialtyText)
    {
        nameText.text = record.coach_name;
        int stars = Mathf.RoundToInt(Mathf.Clamp(record.overall_rating, 1f, 5f));
        ratingText.text = $"Rating: {stars} Stars";
        float weeklySalary = (record.salary * 1000000f) / 52f;
        salaryText.text = $"${weeklySalary:N0}/wk";
        contractText.text = $"{record.contract_length} games minimum";

        switch (record.coach_type)
        {
            case "D":
                stat1Text.text = $"{CalculateBonus(record.run_defence)}%";
                stat2Text.text = $"{CalculateBonus(record.pressure_control)}%";
                stat3Text.text = $"{CalculateBonus(record.coverage_discipline)}%";
                stat4Text.text = $"{CalculateBonus(record.turnover)}%";
                break;
            case "O":
                stat1Text.text = $"{CalculateBonus(record.passing_efficiency)}%";
                stat2Text.text = $"{CalculateBonus(record.rush)}%";
                stat3Text.text = $"{CalculateBonus(record.red_zone_conversion)}%";
                stat4Text.text = $"{CalculateBonus(record.play_variation)}%";
                break;
            case "S":
                stat1Text.text = $"{CalculateBonus(record.field_goal_accuracy)}%";
                stat2Text.text = $"{CalculateBonus(record.kickoff_instance)}%";
                stat3Text.text = $"{CalculateBonus(record.return_speed)}%";
                stat4Text.text = $"{CalculateBonus(record.return_coverage)}%";
                break;
        }

        var derived = CoachData.CreateFromDatabaseRecord(record);
        specialtyText.text = derived.IsSpecialist
            ? $"Specialist (+{derived.TotalBonus}%)"
            : $"+{derived.TotalBonus}% total bonus";

        return derived;
    }

    private void ReleaseDerivedInstances()
    {
        if (candidateDerived != null) Destroy(candidateDerived);
        if (slottedDerived != null) Destroy(slottedDerived);
        candidateDerived = null;
        slottedDerived = null;
    }

    private void OnDisable() => ReleaseDerivedInstances();
}