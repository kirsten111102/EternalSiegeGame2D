using TMPro;
using UnityEngine;
using System;
using System.Globalization;

public class LeaderboardRow : MonoBehaviour
{
    [Header("Leaderboard UI")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text achievedAtText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text levelText;

    public void SetData(int rank, LeaderboardEntry entry)
    {
        // Rank
        rankText.text = rank.ToString();

        // Achieved Date
        if (DateTime.TryParse(
            entry.achievedAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime achievedAt))
        {
            achievedAtText.text = achievedAt.ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm");
        }
        else
        {
            achievedAtText.text = entry.achievedAt;
        }

        // Score
        scoreText.text = entry.score.ToString("N0");

        // Level
        levelText.text = "Lv. " + entry.level.ToString();
    }
}