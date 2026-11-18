using UnityEngine;

public class RangedEnemyCombat : MonoBehaviour, IEnemyCombat
{
    [Header("Combat Stats")]
    [SerializeField] private float damage = 5f;
    [SerializeField] private float attackInterval = 1.5f;
    [SerializeField] private float attackRange = 5f;

    [Header("Projectile")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private EnemyHealth enemyHealth;
    private float nextAttackTime;
    private Transform currentTarget;
    private bool isAttacking;

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

        if (target == null || isAttacking)
            return;

        if (Time.time < nextAttackTime)
            return;

        if (Vector2.Distance(transform.position, target.position)
            > attackRange)
            return;

        currentTarget = target;
        isAttacking = true;
        nextAttackTime = Time.time + attackInterval;

        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            animator.SetTrigger("Attack");
        }
    }

    // Animation Event: Called when the bow releases the arrow.
    public void ShootArrow()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (!isAttacking || currentTarget == null ||
            arrowPrefab == null || firePoint == null)
            return;

        Vector2 direction =
            (currentTarget.position - firePoint.position).normalized;

        GameObject arrow = Instantiate(
            arrowPrefab,
            firePoint.position,
            Quaternion.identity
        );

        ArrowProjectile projectile =
            arrow.GetComponent<ArrowProjectile>();

        if (projectile != null)
            projectile.Launch(direction, damage);
    }

    // Animation Event: Called at the end of the attack animation.
    public void EndAttackAnimation()
    {
        isAttacking = false;

        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
