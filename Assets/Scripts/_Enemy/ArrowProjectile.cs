using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 8f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D rb;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction, float arrowDamage)
    {
        damage = arrowDamage;

        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(
            direction.y, direction.x
        ) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.TryGetComponent<TurretHealth>(
            out var turret))
        {
            if (!turret.IsDead)
            {
                hasHit = true;
                turret.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else if (other.TryGetComponent<CrystalCoreHealth>(
            out var core))
        {
            if (!core.IsDead)
            {
                hasHit = true;
                core.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}