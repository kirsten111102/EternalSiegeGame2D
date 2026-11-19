
using UnityEngine;

public enum EnemyState
{
    MovingToCore,
    ChasingTurret,
    Attacking,
    NoTarget,
    Dead
}

[RequireComponent(
    typeof(EnemyMovement),
    typeof(EnemyHealth)
)]
public class EnemyAI : MonoBehaviour
{
    private EnemyMovement movement;
    private IEnemyCombat combat;
    private EnemyHealth health;

    private CrystalCoreHealth crystalCore;
    private TurretHealth aggroTurret;
    private Transform currentTarget;

    public EnemyState CurrentState { get; private set; }

    public bool IsDead => health != null && health.IsDead;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();

        // Detect which combat component this enemy uses.
        RangedEnemyCombat rangedCombat =
            GetComponent<RangedEnemyCombat>();

        EnemyCombat normalCombat =
            GetComponent<EnemyCombat>();

        combat = rangedCombat != null
            ? (IEnemyCombat)rangedCombat
            : normalCombat;

        if (combat == null)
        {
            Debug.LogError(
                $"{name} has no EnemyCombat or RangedEnemyCombat!",
                this
            );

            enabled = false;
        }
    }

    public void Initialize(CrystalCoreHealth core)
    {
        crystalCore = core;
        CurrentState = EnemyState.MovingToCore;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;
            
        if (health.IsDead)
            return;

        UpdateTarget();

        if (currentTarget == null)
        {
            movement.Stop();
            CurrentState = EnemyState.NoTarget;
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            currentTarget.position
        );

        if (distance <= combat.AttackRange)
        {
            movement.SetMovementEnabled(false);

            CurrentState = EnemyState.Attacking;

            combat.TryAttack(currentTarget);
        }
        else
        {
            movement.SetMovementEnabled(true);

            CurrentState = aggroTurret != null
                ? EnemyState.ChasingTurret
                : EnemyState.MovingToCore;
        }
    }

    // Update the current target.
    private void UpdateTarget()
    {
        Transform newTarget = null;

        // Prioritize the turret that attacked this enemy.
        if (aggroTurret != null && !aggroTurret.IsDead)
        {
            newTarget = aggroTurret.transform;
        }
        else
        {
            aggroTurret = null;

            // Return to the Crystal Core if the turret is dead.
            if (crystalCore != null && !crystalCore.IsDead)
            {
                newTarget = crystalCore.transform;
            }
        }

        // Only update movement when the target changes.
        if (currentTarget != newTarget)
        {
            currentTarget = newTarget;
            movement.SetTarget(currentTarget);
        }
    }

    // Called when a turret attacks this enemy.
    public void OnAttackedByTurret(TurretHealth attacker)
    {
        if (health.IsDead || attacker == null || attacker.IsDead)
            return;

        // Keep the first active turret as the aggro target.
        if (aggroTurret != null && !aggroTurret.IsDead)
            return;

        aggroTurret = attacker;

        Debug.Log($"{name} aggro: {attacker.name}");
    }

    public void OnDead()
    {
        CurrentState = EnemyState.Dead;

        currentTarget = null;

        movement.Stop();

        enabled = false;
    }
}
