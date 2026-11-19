using UnityEngine;

public class EnemyCombat : MonoBehaviour, IEnemyCombat
{
    [Header("Combat Stats")]
    [SerializeField] private float damage = 5f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Attack Animation")]
    [SerializeField] private Animator animator;

    private EnemyHealth enemyHealth;
    private float nextAttackTime;

    public float Damage => damage;
    public float AttackInterval => attackInterval;
    public float AttackRange => attackRange;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        enemyHealth = GetComponent<EnemyHealth>();
    }


    public void TryAttack(Transform target)
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (target == null)
            return;

        if (Time.time < nextAttackTime)
            return;

        bool successfullyAttacked = false;

        // Attack Turret
        if (target.TryGetComponent<TurretHealth>(out var turretHealth))
        {
            if (turretHealth.IsDead)
                return;

            turretHealth.TakeDamage(damage);
            successfullyAttacked = true;
        }
        // Attack Crystal Core
        else if (target.TryGetComponent<CrystalCoreHealth>(out var coreHealth))
        {
            if (coreHealth.IsDead)
                return;

            coreHealth.TakeDamage(damage);
            successfullyAttacked = true;
        }
        // Attack Player
        else if (target.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            if (playerHealth.IsDead)
                return;

            // Deal damage to Player
            playerHealth.TakeDamage(damage);
            successfullyAttacked = true;

            // Apply knockback to Player
            Knockback2D playerKnockback =
                target.GetComponent<Knockback2D>();

            if (playerKnockback != null)
            {
                Vector2 direction =
                    ((Vector2)target.position -
                     (Vector2)transform.position).normalized;

                playerKnockback.ApplyKnockback(direction);
            }
        }

        if (!successfullyAttacked)
            return;

        nextAttackTime = Time.time + attackInterval;

        // Play attack animation
        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            animator.SetTrigger("Attack");
        }
    }


    // Animation Event: Called at the end of the attack animation.
    public void EndAttackAnimation()
    {
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
