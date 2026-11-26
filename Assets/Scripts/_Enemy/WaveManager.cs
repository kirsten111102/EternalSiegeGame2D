using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// Chien dich 70 Wave. Moi Wave muc tieu 330 giay (5 phut 30 giay).
/// Thoi gian thuc te co the dai hon neu Enemy chua bi diet het.

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CrystalCoreHealth crystalCore;
    [SerializeField] private TypeSpawner meleeSpawner;
    [SerializeField] private TypeSpawner rangedSpawner;
    [SerializeField] private TypeSpawner tankSpawner;
    [SerializeField] private GameXP gameXP;
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Campaign Settings")]
    [SerializeField, Min(1)] private int totalWaves = 70;
    [SerializeField, Min(1)] private int nextWaveNumber = 1;

    [Header("Wave Timing (seconds)")]
    [SerializeField, Min(60f)] private float targetWaveDuration = 330f; // 5m30s
    [SerializeField, Min(1f)] private float spawnPhaseDuration = 300f; // 5m
    [SerializeField, Min(0f)] private float timeBetweenWaves = 10f;

    [Header("EXP Distribution (before Bonus XP)")]
    [SerializeField, Range(60, 70)] private int enemyXPSharePercent = 65;

    public int NextWaveNumber => nextWaveNumber;
    public int TotalWaves => totalWaves;
    public bool IsWaveActive { get; private set; }

    // wave vua hoan thanh, EXP thuong cua Wave (khong bao gom Bonus XP)
    public event Action<int, int> OnWaveCompleted;

    private int currentWaveBaseEnemyXP;

    private void Awake()
    {
        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();
    }

    private void OnEnable()
    {
        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();

        if (gameXP != null)
            gameXP.OnEnemyBaseXPGranted += HandleEnemyBaseXPGranted;
    }

    private void OnDisable()
    {
        if (gameXP != null)
            gameXP.OnEnemyBaseXPGranted -= HandleEnemyBaseXPGranted;
    }

    private void Start()
    {
        if (crystalCore == null || meleeSpawner == null ||
            rangedSpawner == null || tankSpawner == null ||
            gameXP == null || !meleeSpawner.IsConfigured ||
            !rangedSpawner.IsConfigured || !tankSpawner.IsConfigured)
        {
            Debug.LogError(
                "WaveManager: Missing CrystalCore, GameXP, or configured Spawners.");
            return;
        }

        // Bao loi neu XP prefab khong dung 1 / 3 / 8.
        if (meleeSpawner.BaseXPReward != 1 || rangedSpawner.BaseXPReward != 3 ||
            tankSpawner.BaseXPReward != 8)
        {
            Debug.LogWarning("WaveManager: Check Enemy prefab xpReward in Inspector: " +
                "Melee=1, Ranged=3, Tank=8. 70-wave balance assumes these values.");
        }

        if (spawnPhaseDuration > targetWaveDuration)
        {
            Debug.LogWarning("WaveManager: spawnPhaseDuration exceeds targetWaveDuration; " +
                "the wave cannot finish in the target time.");
        }

        meleeSpawner.Initialize(crystalCore);
        rangedSpawner.Initialize(crystalCore);
        tankSpawner.Initialize(crystalCore);

        nextWaveNumber = Mathf.Clamp(nextWaveNumber, 1, totalWaves + 1);
    }

    // Goi khi Load JSON, truoc Start() de WaveManager tiep tuc dung Wave.
    public void RestoreNextWaveNumber(int savedNextWaveNumber)
    {
        nextWaveNumber = Mathf.Clamp(savedNextWaveNumber, 1, totalWaves + 1);
    }

    private void HandleEnemyBaseXPGranted(int amount)
    {
        if (IsWaveActive && amount > 0)
            currentWaveBaseEnemyXP += amount;
    }

    private IEnumerator WaveRoutine()
    {
        while (nextWaveNumber <= totalWaves &&
                crystalCore != null && !crystalCore.IsDead)
        {
            int waveNumber = nextWaveNumber;
            float waveStartTime = Time.time;
            IsWaveActive = true;
            currentWaveBaseEnemyXP = 0;

            int meleeCount, rangedCount, tankCount;
            GetWaveComposition(waveNumber, out meleeCount, out rangedCount, out tankCount);

            List<TypeSpawner> spawnQueue = new List<TypeSpawner>(meleeCount + rangedCount + tankCount);
            for (int i = 0; i < meleeCount; i++) spawnQueue.Add(meleeSpawner);
            for (int i = 0; i < rangedCount; i++) spawnQueue.Add(rangedSpawner);
            for (int i = 0; i < tankCount; i++) spawnQueue.Add(tankSpawner);
            ShuffleList(spawnQueue);

            // Phan bo spawn deu ra 5 phut, thay vi 0.3s/enemy nhu truoc.
            float spawnInterval = spawnPhaseDuration / Mathf.Max(1, spawnQueue.Count);
            foreach (TypeSpawner spawner in spawnQueue)
            {
                if (crystalCore == null || crystalCore.IsDead) yield break;
                spawner.SpawnSingleEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }

            // Wave chi xong khi: (1) qua 5m30s, (2) khong con Enemy song.
            // Neu Enemy con song sau 5m30s thi Wave se keo dai hon.
            while (crystalCore != null && !crystalCore.IsDead &&
                   (Time.time - waveStartTime < targetWaveDuration || HasLivingEnemies()))
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (crystalCore == null || crystalCore.IsDead)
                yield break;

            IsWaveActive = false;

            // Dat muc tieu Enemy ~65%, Wave ~35% TRUOC Bonus XP.
            // Tinh theo EXP goc cua Enemy DA BI GIET, khong tinh Enemy chi da spawn.
            // VD: Enemy 65 EXP => Wave 35 EXP => tong 100.
            int waveReward = Mathf.RoundToInt(currentWaveBaseEnemyXP *
                (100f - enemyXPSharePercent) / enemyXPSharePercent);

            if (waveReward > 0)
                gameXP.AddWaveXP(waveReward); // KHONG nhan Bonus XP.

            Debug.Log($"--- Wave {waveNumber}/{totalWaves} Complete --- " +
                $"Enemy base XP: {currentWaveBaseEnemyXP}, Wave XP: {waveReward}");

            if (waveText != null)
            {
                waveText.text = $"Wave {waveNumber}/{totalWaves}";
            }

            // Chuyen sang Wave ke tiep truoc khi bao Save System de khong lap thuong.
            nextWaveNumber = waveNumber + 1;
            OnWaveCompleted?.Invoke(waveNumber, waveReward);

            if (nextWaveNumber <= totalWaves)
                yield return new WaitForSeconds(timeBetweenWaves);
        }

        if (nextWaveNumber > totalWaves)
            Debug.Log("Campaign finished: all 70 waves completed.");
    }

    /// So Enemy tang vua phai de co the rai spawn trong 300s.
    /// Wave 1: 19 Melee / 7 Ranged / 1 Tank (voi cong thuc nay).
    /// Wave 70: 88 Melee / 48 Ranged / 12 Tank.
    /// 
    public void StartWaves()
    {
        if (IsWaveActive)
            return;

        if (crystalCore == null ||
            crystalCore.IsDead ||
            gameXP == null ||
            meleeSpawner == null ||
            rangedSpawner == null ||
            tankSpawner == null ||
            !meleeSpawner.IsConfigured ||
            !rangedSpawner.IsConfigured ||
            !tankSpawner.IsConfigured)
        {
            Debug.LogError("Cannot start waves: Invalid references.");
            return;
        }
        StartCoroutine(WaveRoutine());
    }

    private void GetWaveComposition(
        int wave,
        out int melee,
        out int ranged,
        out int tank)
    {
        switch (wave)
        {
            case 1: melee = 19; ranged = 7; tank = 1; return;
            case 2: melee = 20; ranged = 8; tank = 1; return;
            case 3: melee = 28; ranged = 12; tank = 2; return;
            case 4: melee = 32; ranged = 14; tank = 2; return;
            case 5: melee = 35; ranged = 16; tank = 2; return;
            case 6: melee = 38; ranged = 18; tank = 3; return;
            case 7: melee = 41; ranged = 20; tank = 3; return;
            case 8: melee = 44; ranged = 22; tank = 3; return;
            case 9: melee = 47; ranged = 24; tank = 4; return;
            case 10: melee = 50; ranged = 26; tank = 4; return;

            default:
                int extraWaves = Mathf.Max(0, wave - 10);

                melee = 50 + extraWaves * 7 / 10;
                ranged = 26 + extraWaves * 7 / 20;
                tank = 4 + extraWaves / 8;
                return;
        }
    }

    private static bool HasLivingEnemies()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
                return true;
        }
        return false;
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
