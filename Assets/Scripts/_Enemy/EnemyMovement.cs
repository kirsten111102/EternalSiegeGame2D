
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Animator animator;

    [Header("Player Avoidance")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float detectionDistance = 0.8f;
    [SerializeField] private float sidestepDuration = 0.5f;
    [SerializeField] private float sidestepSpeedMultiplier = 1f;
    [SerializeField] private float sideCheckDistance = 0.7f;
    [SerializeField] private float avoidanceCooldown = 0.3f;

    [Header("Enemy Separation")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float separationRadius = 0.8f;
    [SerializeField] private float separationStrength = 1.5f;

    private Rigidbody2D rb;
    private Transform target;
    private bool canMove;

    private Vector2 lastDirection = Vector2.down;

    // Sidestep state
    private Vector2 sidestepDirection;
    private float sidestepTimer;
    private float avoidanceCooldownTimer;
    private bool isSidestepping;
    private int preferredSide = 1;

    public float MoveSpeed => moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void UpdateAnimation(Vector2 movement)
    {
        if (animator == null) return;

        if (movement.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                lastDirection = movement.x > 0
                    ? Vector2.right
                    : Vector2.left;
            }
            else
            {
                lastDirection = movement.y > 0
                    ? Vector2.up
                    : Vector2.down;
            }
        }

        animator.SetFloat("DirectionX", lastDirection.x);
        animator.SetFloat("DirectionY", lastDirection.y);
        animator.SetFloat("Speed", movement.sqrMagnitude > 0.01f ? 1f : 0f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        // Reset sidestep when the target changes
        isSidestepping = false;
        sidestepTimer = 0f;
    }

    public void SetMovementEnabled(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            isSidestepping = false;
            sidestepTimer = 0f;
            UpdateAnimation(Vector2.zero);
        }
    }

    private void FixedUpdate()
    {
        if (!canMove || target == null)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(Vector2.zero);
            return;
        }

        Vector2 direction = (Vector2)target.position - rb.position;

        if (direction.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            UpdateAnimation(Vector2.zero);
            return;
        }

        direction.Normalize();

        // Countdown cooldown
        if (avoidanceCooldownTimer > 0f)
            avoidanceCooldownTimer -= Time.fixedDeltaTime;

        // Continue current sidestep
        if (isSidestepping)
        {
            sidestepTimer -= Time.fixedDeltaTime;

            if (sidestepTimer > 0f)
            {
                Move(sidestepDirection * sidestepSpeedMultiplier);
                return;
            }

            isSidestepping = false;
            avoidanceCooldownTimer = avoidanceCooldown;
        }

        // Don't avoid the Player when the Player is our actual target
        bool targetIsPlayer = target.GetComponent<PlayerHealth>() != null;

        // Check if Player is blocking our path
        if (!targetIsPlayer &&
            avoidanceCooldownTimer <= 0f &&
            IsPlayerBlocking(direction))
        {
            StartSidestep(direction);
        }

        // Normal movement toward target
        if (isSidestepping)
        {
            Move(sidestepDirection * sidestepSpeedMultiplier);
        }
        else
        {
            Move(direction);
        }
    }

    private bool IsPlayerBlocking(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            0.2f,
            direction,
            detectionDistance,
            playerLayer
        );

        return hit.collider != null;
    }

    private void StartSidestep(Vector2 forwardDirection)
    {
        Vector2 left = new Vector2(
            -forwardDirection.y,
            forwardDirection.x
        );

        Vector2 right = -left;

        bool leftBlocked = IsSideBlocked(left);
        bool rightBlocked = IsSideBlocked(right);

        // Both sides are blocked, so keep moving toward the target
        if (leftBlocked && rightBlocked)
            return;

        // Choose an available direction
        if (leftBlocked)
        {
            sidestepDirection = right;
            preferredSide = -1;
        }
        else if (rightBlocked)
        {
            sidestepDirection = left;
            preferredSide = 1;
        }
        else
        {
            // Both sides are clear; alternate preferred sides
            sidestepDirection = preferredSide == 1 ? left : right;
            preferredSide *= -1;
        }

        isSidestepping = true;
        sidestepTimer = sidestepDuration;
    }

    private bool IsSideBlocked(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            0.2f,
            direction,
            sideCheckDistance,
            playerLayer
        );

        return hit.collider != null;
    }

    private void Move(Vector2 direction)
    {
        direction = ApplyEnemySeparation(direction);

        rb.linearVelocity = direction * moveSpeed;

        UpdateAnimation(direction);
    }

    private Vector2 ApplyEnemySeparation(Vector2 movementDirection)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(
            rb.position,
            separationRadius,
            enemyLayer
        );

        Vector2 separation = Vector2.zero;

        foreach (Collider2D enemyCollider in nearbyEnemies)
        {
            if (enemyCollider.gameObject == gameObject)
                continue;

            Vector2 awayDirection =
                rb.position - (Vector2)enemyCollider.transform.position;

            float distance = awayDirection.magnitude;

            if (distance > 0.001f)
            {
                // Nearby enemies create a stronger repulsion
                float weight = 1f - (distance / separationRadius);

                separation += awayDirection.normalized * weight;
            }
        }

        if (separation.sqrMagnitude > 0.001f)
        {
            movementDirection += separation.normalized * separationStrength;
        }

        return movementDirection.normalized;
    }

    public void Stop()
    {
        canMove = false;
        isSidestepping = false;
        sidestepTimer = 0f;

        rb.linearVelocity = Vector2.zero;
        UpdateAnimation(Vector2.zero);
    }
}
