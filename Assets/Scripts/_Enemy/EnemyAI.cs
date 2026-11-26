
using UnityEngine;

public enum EnemyState
{
    MovingToCore,
    ChasingPlayer,
    ChasingTurret,
    Attacking,
    NoTarget,
    Dead
}

[RequireComponent(typeof(EnemyMovement), typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    private EnemyMovement movement;
    private IEnemyCombat combat;
    private EnemyHealth health;

    private CrystalCoreHealth crystalCore;
    private TurretHealth aggroTurret;
    private Transform aggroPlayer;
    private Transform currentTarget;

    [Header("Player Aggro")]
    [SerializeField, Min(0f)] private float extraAggroTime = 1f;
    private float playerAggroTimer;
    public EnemyState CurrentState { get; private set; }

    public bool IsDead => health != null && health.IsDead;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();

        RangedEnemyCombat rangedCombat = GetComponent<RangedEnemyCombat>();
        EnemyCombat normalCombat = GetComponent<EnemyCombat>();

        combat = rangedCombat != null
            ? (IEnemyCombat)rangedCombat
            : normalCombat;

        if (combat == null)
        {
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

        // Countdown Player aggro
        if (playerAggroTimer > 0f)
        {
            playerAggroTimer -= Time.deltaTime;

            if (playerAggroTimer <= 0f)
            {
                playerAggroTimer = 0f;
                aggroPlayer = null;
            }
        }

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

        // Attack when target is within range
        if (distance <= combat.AttackRange)
        {
            movement.SetMovementEnabled(false);
            CurrentState = EnemyState.Attacking;

            combat.TryAttack(currentTarget);
        }
        else
        {
            movement.SetMovementEnabled(true);

            if (aggroTurret != null && !aggroTurret.IsDead)
            {
                CurrentState = EnemyState.ChasingTurret;
            }
            else if (aggroPlayer != null && playerAggroTimer > 0f)
            {
                CurrentState = EnemyState.ChasingPlayer;
            }
            else
            {
                CurrentState = EnemyState.MovingToCore;
            }
        }
    }

    private void UpdateTarget()
    {
        Transform newTarget = null;

        // Priority 1: Turret
        if (aggroTurret != null && !aggroTurret.IsDead)
        {
            newTarget = aggroTurret.transform;
        }
        else
        {
            aggroTurret = null;

            // Priority 2: Player (temporary aggro)
            if (aggroPlayer != null && playerAggroTimer > 0f)
            {
                PlayerHealth playerHealth =
                    aggroPlayer.GetComponent<PlayerHealth>();

                if (playerHealth != null && !playerHealth.IsDead)
                {
                    newTarget = aggroPlayer;
                }
                else
                {
                    aggroPlayer = null;
                    playerAggroTimer = 0f;
                }
            }
            else
            {
                aggroPlayer = null;
            }

            // Priority 3: Crystal Core
            if (newTarget == null &&
                crystalCore != null &&
                !crystalCore.IsDead)
            {
                newTarget = crystalCore.transform;
            }
        }

        // Update target only when it changes
        if (currentTarget != newTarget)
        {
            currentTarget = newTarget;
            movement.SetTarget(currentTarget);
        }
    }

    public void OnAttackedByPlayer(Transform player)
    {
        if (health.IsDead || player == null)
            return;

        // A living Turret has higher priority
        if (aggroTurret != null && !aggroTurret.IsDead)
            return;

        aggroPlayer = player;

        // Aggro duration = Player attack cooldown + extra time
        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();

        float playerAttackCooldown = playerCombat != null
            ? playerCombat.CurrentAttackCooldown
            : 1f;

        playerAggroTimer = playerAttackCooldown + extraAggroTime;

        UpdateTarget();
    }

    public void OnAttackedByTurret(TurretHealth attacker)
    {
        if (health.IsDead || attacker == null || attacker.IsDead)
            return;

        aggroTurret = attacker;
        aggroPlayer = null;
        playerAggroTimer = 0f;

        UpdateTarget();
    }

    public void OnDead()
    {
        CurrentState = EnemyState.Dead;

        currentTarget = null;
        aggroPlayer = null;
        aggroTurret = null;
        playerAggroTimer = 0f;

        movement.Stop();
        enabled = false;
    }
}
