using UnityEngine;

public class TypeSpawner : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private EnemyAI enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private CrystalCoreHealth crystalCore;

    public bool IsConfigured
    {
        get
        {
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
                return false;

            foreach (Transform spawnPoint in spawnPoints)
                if (spawnPoint != null) return true;

            return false;
        }
    }

    // Chi de thong bao/kiem tra gia tri XP trong Prefab. 
    // Gia tri thuc te duoc EnemyHealth trao khi Enemy bi tieu diet.
    public int BaseXPReward
    {
        get
        {
            if (enemyPrefab == null) return 0;
            EnemyHealth health = enemyPrefab.GetComponent<EnemyHealth>();
            return health != null ? health.XpReward : 0;
        }
    }

    public void Initialize(CrystalCoreHealth core)
    {
        crystalCore = core;
    }

    public void SpawnSingleEnemy()
    {
        if (!IsConfigured || crystalCore == null || crystalCore.IsDead)
            return;

        // Chi chon spawn point hop le (khong null).
        int validPointCount = 0;
        foreach (Transform spawnPoint in spawnPoints)
            if (spawnPoint != null) validPointCount++;

        int selectedIndex = Random.Range(0, validPointCount);
        Transform selectedPoint = null;
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;
            if (selectedIndex-- == 0)
            {
                selectedPoint = spawnPoint;
                break;
            }
        }

        EnemyAI enemy = Instantiate(enemyPrefab, selectedPoint.position, Quaternion.identity);
        enemy.Initialize(crystalCore);
    }
}
