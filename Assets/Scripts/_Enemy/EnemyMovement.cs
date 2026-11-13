using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Transform target;
    private bool canMove;

    private Vector2 lastDirection = Vector2.down;

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
    }

    public void SetMovementEnabled(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
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

        rb.linearVelocity = direction * moveSpeed;

        UpdateAnimation(direction);
    }

    public void Stop()
    {
        canMove = false;
        rb.linearVelocity = Vector2.zero;

        UpdateAnimation(Vector2.zero);
    }
}
