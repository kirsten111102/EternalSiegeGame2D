using System;
using System.IO;
using UnityEngine;

/// Vi du Save/Load RIENG phan EXP va tien do Wave bang JSON.
/// KHONG thay the Save System tong cua game: HP, turret upgrades, card dang cho...
/// phai duoc luu rieng neu muon tiep tuc tran cu nguyen trang.
/// Gan component len 1 GameObject dang active trong Scene.

public class EXPProgressSave : MonoBehaviour
{
    [Serializable]
    private class ProgressData
    {
        public int version = 1;
        public long totalXPHundredths;
        public int bonusXPPercent;
        public int nextWaveNumber = 1;
    }

    [Header("References")]
    [SerializeField] private GameXP gameXP;
    [SerializeField] private WaveManager waveManager;

    [Header("Save Options")]
    [SerializeField] private string saveFileName = "eternal_siege_exp.json";
    [SerializeField] private bool autoLoadOnAwake = true;
    [SerializeField] private bool autoSaveAfterWave = true;
    [SerializeField] private bool saveOnApplicationPauseAndQuit = true;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        if (gameXP == null) gameXP = FindFirstObjectByType<GameXP>();
        if (waveManager == null) waveManager = FindFirstObjectByType<WaveManager>();

        // Awake chay truoc Start cua WaveManager, nen Wave duoc khoi phuc kip.
        if (autoLoadOnAwake) LoadProgress();
    }

    private void OnEnable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted += HandleWaveCompleted;
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted -= HandleWaveCompleted;
    }

    private void HandleWaveCompleted(int completedWave, int waveXP)
    {
        if (autoSaveAfterWave) SaveProgress();
    }

    public void SaveProgress()
    {
        // Checkpoint o RANH GIOI Wave: neu save giua Wave va tai lai Wave
        // tu dau, EXP Enemy da nhan se bi tinh lai -> pha can bang 70 Wave.
        if (waveManager != null && waveManager.IsWaveActive)
        {
            Debug.LogWarning("EXPProgressSave: Cannot save during an active Wave. " +
                "Progress is checkpointed after a Wave is cleared.");
            return;
        }

        if (gameXP == null || waveManager == null)
        {
            Debug.LogWarning("EXPProgressSave: Missing GameXP/WaveManager, cannot save.");
            return;
        }

        ProgressData data = new ProgressData
        {
            totalXPHundredths = gameXP.TotalXPHundredths,
            bonusXPPercent = gameXP.BonusXPPercent,
            nextWaveNumber = waveManager.NextWaveNumber
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"EXP progress saved: {SavePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"EXPProgressSave: Save failed: {ex.Message}");
        }
    }

    public void LoadProgress()
    {
        if (gameXP == null || waveManager == null || !File.Exists(SavePath))
            return;

        try
        {
            ProgressData data = JsonUtility.FromJson<ProgressData>(File.ReadAllText(SavePath));
            if (data == null || data.version != 1)
            {
                Debug.LogWarning("EXPProgressSave: Unsupported/corrupted save data.");
                return;
            }

            // RestoreProgress KHONG goi event OnLevelUp khi Load.
            gameXP.RestoreProgress(data.totalXPHundredths, data.bonusXPPercent);
            waveManager.RestoreNextWaveNumber(data.nextWaveNumber);
            Debug.Log($"EXP loaded: Lv {gameXP.CurrentLevel}, Next Wave {waveManager.NextWaveNumber}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"EXPProgressSave: Load failed: {ex.Message}");
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused && saveOnApplicationPauseAndQuit)
            SaveProgress();
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationPauseAndQuit)
            SaveProgress();
    }
}
