using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 70-wave campaign. Each wave has a target duration of 330 seconds (5m30s).
// Actual duration may be longer if living enemies remain.

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
    [SerializeField, Min(60f)] private float targetWaveDuration = 100f; // 5m30s
    [SerializeField, Min(1f)] private float spawnPhaseDuration = 90f; // 5m
    [SerializeField, Min(0f)] private float timeBetweenWaves = 5f;

    [Header("EXP Distribution (before Bonus XP)")]
    [SerializeField, Range(60, 70)] private int enemyXPSharePercent = 65;

    public int NextWaveNumber => nextWaveNumber;
    public int TotalWaves => totalWaves;
    public bool IsWaveActive { get; private set; }
    public bool IsBetweenWaves { get; private set; }

    private float waveStartTime;
    private float nextWaveStartTime;

    // Hien Thi TimeLeft trong current wave
    public float WaveSecondsRemaining
    {
        get
        {
            if (!IsWaveActive)
                return 0f;

            float elapsed = Time.time - waveStartTime;

            return Mathf.Max(0f, targetWaveDuration - elapsed);
        }
    }

    // Hien thi TimeLeft trong TimeBetweenWaves
    public float BetweenWaveSecondsRemaining
    {
        get
        {
            if (!IsBetweenWaves)
                return 0f;

            return Mathf.Max(0f, nextWaveStartTime - Time.time);
        }
    }

    // The wave that just completed and its wave XP reward (excluding Bonus XP).
    public event Action<int, int> OnWaveCompleted;

    private int currentWaveBaseEnemyXP;
    private Coroutine waveCoroutine;

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
                "WaveManager: Missing CrystalCore, GameXP, or configured Spawners.",
                this);
            enabled = false;
            return;
        }

        // Report an error rather than silently leaving the wave UI blank.
        if (waveText == null)
        {
            Debug.LogError(
                "WaveManager: Wave Text is not assigned in the Inspector.",
                this);
        }

        // Warn if enemy prefab XP values do not match the campaign balance.
        if (meleeSpawner.BaseXPReward != 1 || rangedSpawner.BaseXPReward != 3 ||
            tankSpawner.BaseXPReward != 8)
        {
            Debug.LogWarning(
                "WaveManager: Check Enemy prefab xpReward in Inspector: " +
                "Melee=1, Ranged=3, Tank=8. 70-wave balance assumes these values.",
                this);
        }

        if (spawnPhaseDuration > targetWaveDuration)
        {
            Debug.LogWarning(
                "WaveManager: spawnPhaseDuration exceeds targetWaveDuration; " +
                "the wave cannot finish in the target time.",
                this);
        }

        meleeSpawner.Initialize(crystalCore);
        rangedSpawner.Initialize(crystalCore);
        tankSpawner.Initialize(crystalCore);

        nextWaveNumber = Mathf.Clamp(nextWaveNumber, 1, totalWaves + 1);
        UpdateWaveText();
    }

    // Call when loading JSON, before StartWaves(), to restore the next wave.
    public void RestoreNextWaveNumber(int savedNextWaveNumber)
    {
        nextWaveNumber = Mathf.Clamp(savedNextWaveNumber, 1, totalWaves + 1);
        UpdateWaveText();
    }

    private void UpdateWaveText()
    {
        if (waveText == null)
            return;

        if (nextWaveNumber > totalWaves)
            waveText.text = "Campaign Completed!";
        else
            waveText.text = $"Wave {nextWaveNumber}/{totalWaves}";
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

            IsBetweenWaves = false;
            IsWaveActive = true;
            currentWaveBaseEnemyXP = 0;

            UpdateWaveText();

            // Record wave start time
            waveStartTime = Time.time;

            int meleeCount, rangedCount, tankCount;
            GetWaveComposition(
                waveNumber, out meleeCount, out rangedCount, out tankCount);

            List<TypeSpawner> spawnQueue =
                new List<TypeSpawner>(meleeCount + rangedCount + tankCount);

            for (int i = 0; i < meleeCount; i++)
                spawnQueue.Add(meleeSpawner);

            for (int i = 0; i < rangedCount; i++)
                spawnQueue.Add(rangedSpawner);

            for (int i = 0; i < tankCount; i++)
                spawnQueue.Add(tankSpawner);

            ShuffleList(spawnQueue);

            // Spread spawning evenly across the spawn phase.
            float spawnInterval =
                spawnPhaseDuration / Mathf.Max(1, spawnQueue.Count);

            foreach (TypeSpawner spawner in spawnQueue)
            {
                if (crystalCore == null || crystalCore.IsDead)
                {
                    IsWaveActive = false;
                    yield break;
                }

                spawner.SpawnSingleEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }

            // A wave completes after its target duration AND when no living
            // enemies remain. If enemies remain, the wave lasts longer.
            while (crystalCore != null && !crystalCore.IsDead &&
                   (Time.time - waveStartTime < targetWaveDuration ||
                    HasLivingEnemies()))
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (crystalCore == null || crystalCore.IsDead)
            {
                IsWaveActive = false;
                yield break;
            }

            IsWaveActive = false;

            // Target enemy XP share is 65%, wave XP is 35%, before Bonus XP.
            int waveReward = Mathf.RoundToInt(
                currentWaveBaseEnemyXP *
                (100f - enemyXPSharePercent) / enemyXPSharePercent);

            if (waveReward > 0)
                gameXP.AddWaveXP(waveReward); // Does not receive Bonus XP.

            Debug.Log(
                $"--- Wave {waveNumber}/{totalWaves} Complete --- " +
                $"Enemy base XP: {currentWaveBaseEnemyXP}, Wave XP: {waveReward}");

            // Advance the next-wave value before notifying the save system.
            nextWaveNumber = waveNumber + 1;
            UpdateWaveText();

            OnWaveCompleted?.Invoke(waveNumber, waveReward);

            if (nextWaveNumber <= totalWaves)
            {
                IsBetweenWaves = true;
                nextWaveStartTime = Time.time + timeBetweenWaves;
                yield return new WaitForSeconds(timeBetweenWaves);
                IsBetweenWaves = false;
            }
        }

        IsWaveActive = false;

        if (nextWaveNumber > totalWaves)
        {
            UpdateWaveText();
            Debug.Log($"Campaign finished: all {totalWaves} waves completed.");
        }

        // Dam bao trang thai dc reset
        IsWaveActive = false;
        IsBetweenWaves = false;
        
        waveCoroutine = null;
    }

    // Wave 1: 19 Melee / 7 Ranged / 1 Tank.
    // Wave 70: 88 Melee / 48 Ranged / 12 Tank.
    public void StartWaves()
    {
        if (waveCoroutine != null || IsWaveActive)
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
            Debug.LogError("Cannot start waves: Invalid references.", this);
            return;
        }

        if (nextWaveNumber > totalWaves)
        {
            UpdateWaveText();
            Debug.Log("Cannot start waves: campaign already completed.", this);
            return;
        }

        waveCoroutine = StartCoroutine(WaveRoutine());
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
        EnemyAI[] enemies =
            FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

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
