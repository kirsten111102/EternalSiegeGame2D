
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Knockback2D : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField, Min(0f)] private float knockbackForce = 3f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 direction)
    {
        if (rb == null || direction == Vector2.zero)
            return;

        direction.Normalize();

        rb.AddForce(
            direction * knockbackForce,
            ForceMode2D.Impulse
        );
    }
}
