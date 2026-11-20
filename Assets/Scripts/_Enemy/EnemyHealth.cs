using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] private float maxHealth = 30f;
    // Set in Prefab: Melee=1, Ranged=3, Tank=8.
    [SerializeField, Min(0)] private int xpReward = 5;
    [SerializeField] private int scoreValue = 10;

    [Header("References")]
    [SerializeField] private GameXP gameXP;
    [SerializeField] private EnemyHealthUI healthBar;
    [SerializeField] private GameScore gameScore;

    [Header("Death")]
    [SerializeField] private Animator animator;
    [SerializeField] private string dieTriggerName = "Die";
    [SerializeField, Min(0f)] private float dieAnimationDuration = 1f;
    [SerializeField] private bool hideSpritesAfterDeathAnimation = true;


    private EnemyAI enemyAI;
    private EnemyCombat enemyCombat;
    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] colliders;


    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public int XpReward => xpReward;


    private void Awake()
    {
        CurrentHealth = maxHealth;
        enemyAI = GetComponent<EnemyAI>();
        enemyCombat = GetComponent<EnemyCombat>();

        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();

        if (gameScore == null)
            gameScore = FindFirstObjectByType<GameScore>();

        if (healthBar == null)
            healthBar = GetComponentInChildren<EnemyHealthUI>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
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
        if (IsDead)
            return;

        IsDead = true;

        // Keep existing enemy death, score, and XP logic.
        if (enemyAI != null)
            enemyAI.OnDead();

        if (gameScore != null)
            gameScore.AddScore(scoreValue);

        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();

        if (gameXP != null)
            gameXP.AddEnemyXP(xpReward);
        else
            Debug.LogWarning($"{name}: No GameXP found; XP was not awarded.");

        // Stop the enemy from moving/attacking or colliding during its death sequence.
        if (enemyAI != null)
            enemyAI.enabled = false;

        if (enemyCombat != null)
            enemyCombat.enabled = false;

        foreach (Collider2D enemyCollider in colliders)
        {
            if (enemyCollider != null)
                enemyCollider.enabled = false;
        }

        // Play the death animation before hiding the sprite and destroying the enemy.
        if (animator != null)
        {
            animator.ResetTrigger(dieTriggerName);
            animator.SetTrigger(dieTriggerName);
        }

        StartCoroutine(DeathSequence());
    }


    private IEnumerator DeathSequence()
    {
        // Allow the death animation to play.
        yield return new WaitForSeconds(dieAnimationDuration);

        // Hide sprites only after the death animation finishes.
        if (hideSpritesAfterDeathAnimation)
        {
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
            }
        }

        Destroy(gameObject);
    }
}
