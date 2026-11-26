using System.Collections.Generic;
using UnityEngine;

// Redraws the attack circle in Scene/Edit mode as well as Play mode.
[ExecuteAlways]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Point")]
    [SerializeField] private Transform attackPoint;
    [SerializeField, Min(0f)] private float attackPointDistance = 0.7f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Animator animator;

    [Header("Base combat stats (Level 1)")]
    [SerializeField, Min(0f)] private float attackRange = 1f;
    [SerializeField, Min(1)] private int attackDamage = 20;
    [SerializeField, Min(0.01f)] private float attackCooldown = 0.5f;

    [Header("Optional range display")]
    [SerializeField] private LineRenderer attackCircle;
    [SerializeField, Min(3)] private int circleSegments = 60;
    [SerializeField] private EnemyAI enemyAI;

    private float nextAttackTime;
    private Vector2 lastMoveDirection = Vector2.right;
    private int levelBonus;

    // Prevent one EnemyHealth with multiple colliders from taking damage twice.
    private readonly HashSet<EnemyHealth> enemiesHit = new HashSet<EnemyHealth>();

    public int CurrentAttackDamage => attackDamage + levelBonus;
    public float CurrentAttackRange => attackRange;
    public float CurrentAttackCooldown =>
        1f / (1f / Mathf.Max(0.01f, attackCooldown) + levelBonus);

    private void Awake()
    {
        if (!Application.IsPlaying(gameObject)) return;

        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();
    }

    private void Update()
    {
        // Never handle input / change AttackPoint while editing a Scene or Prefab.
        if (Application.IsPlaying(gameObject) && Time.timeScale > 0f)
        {
            UpdateAttackDirection();

            if (Input.GetKeyDown(KeyCode.Space))
                Attack();
        }

        // Keep LineRenderer synced with AttackPoint and attackRange.
        DrawAttackCircle();
    }

    public void Attack()
    {
        if (!Application.IsPlaying(gameObject) ||
            !isActiveAndEnabled ||
            Time.timeScale <= 0f ||
            attackPoint == null ||
            Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + CurrentAttackCooldown;

        if (animator != null)
        {
            animator.SetBool("IsAttacking", true);
            animator.SetTrigger("Attack");
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            CurrentAttackRange,
            enemyLayer
        );

        enemiesHit.Clear();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            EnemyHealth enemyHealth =
                hit.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null || !enemiesHit.Add(enemyHealth))
                continue;

            // Apply damage
            enemyHealth.TakeDamage(CurrentAttackDamage);

            // Get the AI of the actual enemy hit
            EnemyAI targetAI =
                enemyHealth.GetComponent<EnemyAI>();

            if (targetAI != null)
            {
                // Player aggro (Turret aggro still takes priority)
                targetAI.OnAttackedByPlayer(transform);
            }

            // Apply knockback to the enemy
            Knockback2D enemyKnockback =
                enemyHealth.GetComponent<Knockback2D>();

            if (enemyKnockback != null)
            {
                Vector2 direction =
                    ((Vector2)enemyHealth.transform.position -
                     (Vector2)transform.position).normalized;

                enemyKnockback.ApplyKnockback(direction);
            }
        }
    }

    // Called by PlayerLevelStats when the player levels up.
    public void ApplyLevelBonus(int bonus)
    {
        levelBonus = Mathf.Max(0, bonus);
    }

    // Optional API for future upgrades that modify range during play.
    public void SetAttackRange(float newRange)
    {
        attackRange = Mathf.Max(0f, newRange);
        DrawAttackCircle();
    }

    private void UpdateAttackDirection()
    {
        if (attackPoint == null) return;

        Vector2 moveDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));

        if (moveDirection != Vector2.zero)
            lastMoveDirection = moveDirection.normalized;

        attackPoint.localPosition = (Vector3)(
            lastMoveDirection * attackPointDistance);
    }

    private void DrawAttackCircle()
    {
        if (attackCircle == null || attackPoint == null) return;

        attackCircle.useWorldSpace = true;
        attackCircle.loop = true;

        if (attackCircle.positionCount != circleSegments)
            attackCircle.positionCount = circleSegments;

        attackCircle.startWidth = 0.03f;
        attackCircle.endWidth = 0.03f;

        Vector3 center = attackPoint.position;
        float angleStep = Mathf.PI * 2f / circleSegments;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * angleStep;
            Vector3 point = center + new Vector3(
                Mathf.Cos(angle) * CurrentAttackRange,
                Mathf.Sin(angle) * CurrentAttackRange,
                0f);

            attackCircle.SetPosition(i, point);
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
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, CurrentAttackRange);
    }

    // Validate only serialized values; avoid LineRenderer API calls here.
    private void OnValidate()
    {
        attackRange = Mathf.Max(0f, attackRange);
        attackPointDistance = Mathf.Max(0f, attackPointDistance);
        attackDamage = Mathf.Max(1, attackDamage);
        attackCooldown = Mathf.Max(0.01f, attackCooldown);
        circleSegments = Mathf.Max(3, circleSegments);
    }
}
