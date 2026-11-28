using System;
using UnityEngine;

public class TurretHealth : MonoBehaviour
{
    [Header("Health Stats")]
    [SerializeField] private float maxHealth = 150f;

    [Header("Save Identity")]
    [SerializeField] private string turretId;

    public string TurretId => turretId;
    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    [Header("Destroyed Animation")]
    [SerializeField] private Animator crystalAnimator;
    [SerializeField] private SpriteRenderer crystalRenderer;
    [SerializeField] private Material destroyedCrystalMaterial;

    [Header("Attack Visual Systems")]
    [SerializeField] private TurretCrystalVisual crystalVisual;
    [SerializeField] private TurretBeamVisual beamVisual;

    private TurretCombat turretCombat;

    private static readonly int DestroyedHash =
        Animator.StringToHash("Destroyed");

    // Health Bar UI
    public event Action<float, float> OnHealthChanged; // Show current HP and max HP
    public event Action OnDied; // Turret Died >> Hide Health Bar UI

    private void Awake()
    {
        CurrentHealth = maxHealth;
        IsDead = false;

        turretCombat = GetComponent<TurretCombat>();

        if (crystalVisual == null)
        {
            crystalVisual =
                GetComponent<TurretCrystalVisual>();
        }

        if (beamVisual == null)
        {
            beamVisual =
                GetComponent<TurretBeamVisual>();
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        // Invoke: Health Bar UI cap nhat
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        Debug.Log($"{name} HP: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    // Hoi mau theo phan tram maxHealth
    public void HealPercent(float percent)
    {
        if (IsDead || percent <= 0f)
            return;

        float healAmount = maxHealth * percent;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healAmount);

        // Invoke: Health Bar UI cap nhat
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        Debug.Log(
            $"{name} healed {healAmount} HP. " +
            $"HP: {CurrentHealth}/{maxHealth}"
        );
    }

    // Gọi trên Turret vừa được khởi tạo trong scene mới.
    public void RestoreHealth(float savedHealth)
    {
        CurrentHealth = Mathf.Clamp(savedHealth, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        if (IsDead) return;

        // Invoke: Turret Destroyed >> Hide Health Bar UI
        OnDied?.Invoke();

        // 1. Đánh dấu Turret đã chết
        IsDead = true;

        // 2. Ngừng hoạt động chiến đấu
        if (turretCombat != null)
        {
            turretCombat.StopAllCoroutines();
            turretCombat.enabled = false;
        }

        // 3. Tắt Beam và Beam Impact VFX
        if (beamVisual != null)
        {
            beamVisual.enabled = false;
        }

        // 4. Dừng đổi màu và tăng tốc Idle
        if (crystalVisual != null)
        {
            crystalVisual.enabled = false;
        }

        // 5. Chuyển Crystal sang Material đã chết
        if (crystalRenderer != null &&
            destroyedCrystalMaterial != null)
        {
            // Xóa override AttackBlend cũ
            crystalRenderer.SetPropertyBlock(null);

            crystalRenderer.sharedMaterial =
                destroyedCrystalMaterial;
        }

        // 6. Kích hoạt Crystal Destroyed Animation
        if (crystalAnimator != null)
        {
            crystalAnimator.speed = 1f;
            crystalAnimator.SetTrigger(DestroyedHash);
        }

        Debug.Log($"{name} destroyed!");

        // Không Destroy(gameObject)
        // Turret sẽ tồn tại như một phế tích.
    }
}
