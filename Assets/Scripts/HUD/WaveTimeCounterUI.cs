using UnityEngine;
using TMPro;

public class WaveTimeCounterUI : MonoBehaviour
{
    [Header("Wave References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TMP_Text waveCounterText;

    [Header("Player Penalty References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject penaltyPanel;
    [SerializeField] private TMP_Text penaltyCounterText;

    private void Update()
    {
        UpdateWaveCounter();
        UpdatePenaltyCounter();
    }

    // IsWaveActive == true (Hien thi thoi gian wave con lai)
    // IsBetweenWaves == true (Hien thi thoi gian cho wave tiep theo)
    // Campaign hoan thanh: Completed
    // Chua bat dau: Ready
    private void UpdateWaveCounter()
    {
        if (waveManager == null || waveCounterText == null)
            return;

        // Current wave is running
        if (waveManager.IsWaveActive)
        {
            int seconds = Mathf.CeilToInt(
                waveManager.WaveSecondsRemaining
            );

            waveCounterText.text = $"{seconds}s";
        }

        // Waiting for next wave
        else if (waveManager.IsBetweenWaves)
        {
            int seconds = Mathf.CeilToInt(
                waveManager.BetweenWaveSecondsRemaining
            );

            waveCounterText.text = $"Ready in {seconds}s";
        }

        // All waves completed
        else if (waveManager.NextWaveNumber >
                 waveManager.TotalWaves)
        {
            waveCounterText.text = "Completed";
        }

        // Wave has not started
        else
        {
            waveCounterText.text = "Ready";
        }
    }

    private void UpdatePenaltyCounter()
    {
        if (penaltyPanel == null)
            return;

        // Check if Player is dead >> Hien Penalty Panel
        bool isPlayerDead = 
            playerHealth != null && playerHealth.IsDead;
        if (penaltyPanel.activeSelf != isPlayerDead)
            penaltyPanel.SetActive(isPlayerDead);

        // Check if
        if (!isPlayerDead || penaltyCounterText == null)
            return;

        int seconds = Mathf.CeilToInt(
            playerHealth.RespawnSecondsRemaining
        );
        penaltyCounterText.text = $"Respawn in {seconds}s";
    }
}
