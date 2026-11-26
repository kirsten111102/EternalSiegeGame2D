using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class LeaderboardRow : MonoBehaviour
{
    [Header("Leaderboard UI")]
    [SerializeField] private TMP_Text rankText;
    [FormerlySerializedAs("achievedAtText")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
     [SerializeField] private TMP_Text scoreText;

    public void SetData(int rank, LeaderboardEntry entry)
    {
        if (entry == null)
            return;

        if (rankText != null)
            rankText.text = rank.ToString("00");

        if (nameText != null)
            nameText.text = entry.name;

        if (scoreText != null)
            scoreText.text = entry.score.ToString("N0");

        if (levelText != null)
            levelText.text = "Lv. " + entry.level;
    }
}