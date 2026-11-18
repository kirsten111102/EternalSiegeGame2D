using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 30f;
    // Gan trong Prefab: Melee=1, Ranged=3, Tank=8.
    [SerializeField, Min(0)] private int xpReward = 5;
    [SerializeField] private int scoreValue = 10;

    [Header("References")]
    [SerializeField] private GameXP gameXP;
    [SerializeField] private EnemyHealthUI healthBar;
    [SerializeField] private GameScore gameScore;

    private EnemyAI enemyAI;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public int XpReward => xpReward;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();

        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();

        if (gameScore == null)
            gameScore = FindFirstObjectByType<GameScore>();

        if (healthBar == null)
            healthBar = GetComponentInChildren<EnemyHealthUI>(true);

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(float damage, TurretHealth attacker = null)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        if (healthBar != null)
            healthBar.SetHealth(CurrentHealth);

        if (CurrentHealth <= 0f)
        {
            Die();
            return;
        }

        if (attacker != null && enemyAI != null)
            enemyAI.OnAttackedByTurret(attacker);
    }

    private void Die()
    {
        if (IsDead) return;

        IsDead = true;

        if (enemyAI != null) enemyAI.OnDead();
        if (gameScore != null) gameScore.AddScore(scoreValue);

        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();

        if (gameXP != null)
            gameXP.AddEnemyXP(xpReward); // Co Bonus XP, chi thuong mot lan.
        else
            Debug.LogWarning($"{name}: No GameXP found; XP was not awarded.");

        Destroy(gameObject);
    }
}
