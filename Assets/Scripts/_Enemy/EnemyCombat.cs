using UnityEngine;

public class EnemyCombat : MonoBehaviour, IEnemyCombat
{
    [Header("Combat Stats")]
    [SerializeField] private float damage = 5f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Attack Animation")]
    [SerializeField] private Animator animator;

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
    }

    public void TryAttack(Transform target)
    {
        if (target == null)
            return;

        if (Time.time < nextAttackTime)
            return;

        // Check whether the target can be damaged before playing the attack animation.
        if (target.TryGetComponent<TurretHealth>(
            out var turretHealth))
        {
            if (turretHealth.IsDead)
                return;

            turretHealth.TakeDamage(damage);
        }
        else if (target.TryGetComponent<CrystalCoreHealth>(
            out var coreHealth))
        {
            if (coreHealth.IsDead)
                return;

            coreHealth.TakeDamage(damage);
        }
        else
        {
            return;
        }

        nextAttackTime = Time.time + attackInterval;

        // Play the enemy's attack animation.
        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            animator.SetTrigger("Attack");
        }
    }

    // Add this method as an Animation Event at the end of the attack animation.
    public void EndAttackAnimation()
    {
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }


}