using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
public class GameSaveController : MonoBehaviour
{
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameScore gameScore;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameXP gameXP;
    [SerializeField] private CrystalCoreHealth crystalCore;
    [SerializeField] private LevelUpPanelUI levelUpPanelUI;

    private bool pendingAutoSave;
    private static SaveData pendingLoadData;
    public void SaveGame()
    {
        if (saveSystem == null ||
            playerTransform == null ||
            playerHealth == null ||
            gameScore == null ||
            waveManager == null ||
            gameXP == null ||
            crystalCore == null ||
            levelUpPanelUI == null)
        {
            Debug.LogWarning("Cannot save: Missing gameplay references.");
            return;
        }

        // Save khi k trong wave
        if (waveManager.IsWaveActive)
        {
            Debug.LogWarning("Cannot save during an active wave.");
            return;
        }

        // Chọn buff trước mới save
        if (levelUpPanelUI.IsOpen)
        {
            Debug.LogWarning("Finish selecting upgrades before saving.");
            return;
        }

        if (crystalCore.IsDead)
        {
            Debug.LogWarning("Cannot save after the Core is destroyed.");
            return;
        }

        // Bao gồm cả trụ inactive và trụ đã bị phá hủy.
        TurretHealth[] turrets = FindObjectsByType<TurretHealth>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        TurretSaveData[] turretData = new TurretSaveData[turrets.Length];
        HashSet<string> turretIds = new HashSet<string>();

        for (int i = 0; i < turrets.Length; i++)
        {
            TurretHealth turret = turrets[i];

            if (string.IsNullOrWhiteSpace(turret.TurretId) ||
                !turretIds.Add(turret.TurretId))
            {
                Debug.LogError(
                    $"Cannot save: Turret '{turret.name}' has an empty or duplicate ID."
                );
                return;
            }

            TurretCombat combat = turret.GetComponent<TurretCombat>();

            if (combat == null)
            {
                Debug.LogError(
                    $"Cannot save: Turret '{turret.name}' has no TurretCombat."
                );
                return;
            }

            turretData[i] = new TurretSaveData
            {
                turretId = turret.TurretId,
                currentHealth = turret.CurrentHealth,
                damagePerTick = combat.DamagePerTick,
                attackRange = combat.AttackRange,
                attackSpeedPoints = combat.AttackSpeedPoints
            };
        }

        SaveData data = new SaveData
        {
            playerX = playerTransform.position.x,
            playerY = playerTransform.position.y,
            health = playerHealth.CurrentHealth,
            deathCount = playerHealth.DeathCount,
            respawnSecondsRemaining = playerHealth.RespawnSecondsRemaining,

            score = gameScore.Score,

            nextWaveNumber = waveManager.NextWaveNumber,
            totalXPHundredths = gameXP.TotalXPHundredths,
            bonusXPPercent = gameXP.BonusXPPercent,
            coreHealth = crystalCore.CurrentHealth,
            turrets = turretData
        };

        saveSystem.SaveGame(data);
    }

    private void OnEnable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted += HandleWaveCompleted;

        if (levelUpPanelUI != null)
            levelUpPanelUI.OnAllChoicesCompleted += HandleAllChoicesCompleted;
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.OnWaveCompleted -= HandleWaveCompleted;

        if (levelUpPanelUI != null)
            levelUpPanelUI.OnAllChoicesCompleted -= HandleAllChoicesCompleted;

        pendingAutoSave = false;
    }

    private void HandleWaveCompleted(int completedWave, int waveXP)
    {
        pendingAutoSave = true;
        TryAutoSave();
    }

    private void HandleAllChoicesCompleted()
    {
        TryAutoSave();
    }

    private void TryAutoSave()
    {
        if (!pendingAutoSave)
            return;

        if (waveManager == null || levelUpPanelUI == null)
            return;

        // Không thực hiện yêu cầu lưu cũ nếu đã sang wave mới.
        if (waveManager.IsWaveActive)
        {
            pendingAutoSave = false;
            return;
        }

        // Giữ yêu cầu lưu cho đến khi chọn hết Power Up.
        if (levelUpPanelUI.IsOpen)
            return;

        pendingAutoSave = false;
        SaveGame();
    }

    public void LoadGame()
    {
        RequestLoadGame(saveSystem);
    }

    public static void RequestLoadGame(SaveSystem saveSystem)
    {
        if (saveSystem == null)
        {
            Debug.LogWarning("Cannot load: SaveSystem is missing.");
            return;
        }
        SaveData data = saveSystem.LoadGame();
        if (data == null)
        {
            Debug.LogWarning("Cannot load: No readable save data.");
            return;
        }
        // Kiểm tra cấu trúc checkpoint cơ bản trước khi đổi scene.
        if (data.turrets == null ||
            data.coreHealth <= 0f ||
            data.nextWaveNumber < 1)
        {
            Debug.LogError("Cannot load: Invalid checkpoint data.");
            return;
        }
        pendingLoadData = data;
        SceneManager.LoadScene(
            "MainMap",
            LoadSceneMode.Single
        );
    }

    private bool ApplySaveData(SaveData data)
    {
        if (playerTransform == null ||
            playerHealth == null ||
            gameScore == null ||
            gameXP == null ||
            crystalCore == null ||
            waveManager == null)
        {
            Debug.LogError("Cannot load: Missing gameplay references.");
            return false;
        }

        PlayerLevelStats levelStats =
            playerHealth.GetComponent<PlayerLevelStats>();

        if (levelStats == null)
        {
            Debug.LogError("Cannot load: PlayerLevelStats is missing.");
            return false;
        }

        if (data.turrets == null ||
            data.coreHealth <= 0f ||
            data.nextWaveNumber < 1 ||
            data.nextWaveNumber > waveManager.TotalWaves + 1)
        {
            Debug.LogError("Cannot load: Invalid checkpoint data.");
            return false;
        }

        TurretHealth[] turrets = FindObjectsByType<TurretHealth>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Dictionary<string, TurretHealth> turretMap =
            new Dictionary<string, TurretHealth>();

        // Kiểm tra toàn bộ Turret trước khi thay đổi gameplay state.
        foreach (TurretHealth turret in turrets)
        {
            if (string.IsNullOrWhiteSpace(turret.TurretId) ||
                turretMap.ContainsKey(turret.TurretId))
            {
                Debug.LogError("Cannot load: Empty or duplicate Turret ID.");
                return false;
            }

            // Luồng này cần các Turret đã chạy Awake trong scene mới.
            if (!turret.gameObject.activeInHierarchy ||
                turret.GetComponent<TurretCombat>() == null)
            {
                Debug.LogError(
                    $"Cannot load: Turret '{turret.name}' is not initialized."
                );
                return false;
            }

            turretMap.Add(turret.TurretId, turret);
        }

        if (data.turrets.Length != turretMap.Count)
        {
            Debug.LogError("Cannot load: Saved Turrets do not match this scene.");
            return false;
        }

        HashSet<string> savedIds = new HashSet<string>();

        foreach (TurretSaveData savedTurret in data.turrets)
        {
            if (savedTurret == null ||
                string.IsNullOrWhiteSpace(savedTurret.turretId) ||
                !savedIds.Add(savedTurret.turretId) ||
                !turretMap.ContainsKey(savedTurret.turretId))
            {
                Debug.LogError("Cannot load: Invalid saved Turret ID.");
                return false;
            }
        }

        // 1. EXP -> level -> chỉ số tối đa của Player.
        gameXP.RestoreProgress(
            data.totalXPHundredths,
            data.bonusXPPercent
        );

        // RestoreProgress không phát OnLevelUp nên cần gọi trực tiếp.
        levelStats.ApplyLevel(gameXP.CurrentLevel);

        // 2. Vị trí và trạng thái Player.
        Vector3 position = new Vector3(
            data.playerX,
            data.playerY,
            playerTransform.position.z
        );

        playerTransform.position = position;

        Rigidbody2D playerBody =
            playerTransform.GetComponent<Rigidbody2D>();

        if (playerBody != null)
        {
            playerBody.position = new Vector2(position.x, position.y);
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }

        playerHealth.RestoreState(
            data.health,
            data.deathCount,
            data.respawnSecondsRemaining
        );

        // 3. Điểm và Core.
        gameScore.SetScore(data.score);
        crystalCore.RestoreHealth(data.coreHealth);

        // 4. Chỉ số và máu của từng Turret.
        foreach (TurretSaveData savedTurret in data.turrets)
        {
            TurretHealth turret = turretMap[savedTurret.turretId];
            TurretCombat combat = turret.GetComponent<TurretCombat>();
            combat.RestoreStats(
                savedTurret.damagePerTick,
                savedTurret.attackRange,
                savedTurret.attackSpeedPoints
            );

            turret.RestoreHealth(savedTurret.currentHealth);
        }

        // 5. Chuẩn bị wave tiếp theo, chưa chạy tại đây.
        waveManager.RestoreNextWaveNumber(data.nextWaveNumber);
        pendingAutoSave = false;

        return true;
    }

    private IEnumerator Start()
    {
        SaveData data = pendingLoadData;
        pendingLoadData = null;

        // Đợi các Start() hiện tại hoàn tất:
        // PlayerHealth, PlayerLevelStats, GameScore, WaveManager...
        yield return null;

        if (waveManager == null)
        {
            Debug.LogError("Cannot start game: WaveManager is missing.");
            yield break;
        }

        if (data != null)
        {
            if (!ApplySaveData(data))
            {
                Debug.LogError("Load failed. Waves were not started.");
                Time.timeScale = 0f;
                yield break;
            }

            Debug.Log("Game loaded successfully.");
        }

        Time.timeScale = 1f;
        waveManager.StartWaves();
    }

}