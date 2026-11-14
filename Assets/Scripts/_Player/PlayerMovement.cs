using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Base movement stat (Level 1)")]
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Vector2 direction;
    private Vector2 lastDirection = Vector2.down;
    private float currentMoveSpeed;
    private bool statsApplied;

    public float CurrentMoveSpeed => statsApplied ? currentMoveSpeed : moveSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Do not overwrite a level bonus that another component applied in Awake.
        if (!statsApplied) ApplyLevelBonus(0);
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        direction = new Vector2(horizontal, vertical);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 targetPosition =
            rb.position + direction * CurrentMoveSpeed * Time.fixedDeltaTime;
        
        rb.MovePosition(targetPosition);
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        if (direction.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                lastDirection = direction.x > 0 ? Vector2.right : Vector2.left;
            else
                lastDirection = direction.y > 0 ? Vector2.up : Vector2.down;
        }

        animator.SetFloat("DirectionX", lastDirection.x);
        animator.SetFloat("DirectionY", lastDirection.y);
        animator.SetFloat("Speed", direction.magnitude);
    }

    // A level adds exactly +1 to Movement Speed.
    public void ApplyLevelBonus(int bonus)
    {
        currentMoveSpeed = moveSpeed + Mathf.Max(0, bonus);
        statsApplied = true;
    }

    public void StopMovement()
    {
        direction = Vector2.zero;
        if (animator != null) animator.SetFloat("Speed", 0f);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private void OnDisable()
    {
        StopMovement();
    }

    
}
