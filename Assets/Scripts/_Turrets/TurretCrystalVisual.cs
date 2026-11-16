using UnityEngine;

[RequireComponent(typeof(TurretCombat))]
[RequireComponent(typeof(TurretHealth))]
public class TurretCrystalVisual : MonoBehaviour
{
    [Header("Crystal References")]
    [SerializeField] private Animator crystalAnimator;
    [SerializeField] private SpriteRenderer crystalRenderer;

    [Header("Animation Speed")]
    [SerializeField] private float idleSpeed = 1f;
    [SerializeField] private float attackSpeed = 2.5f;

    [Header("Color Transition")]
    [SerializeField] private float colorChangeSpeed = 5f;

    private TurretCombat turretCombat;
    private TurretHealth turretHealth;

    private MaterialPropertyBlock propertyBlock;
    private float currentBlend;

    private static readonly int AttackBlendID =
        Shader.PropertyToID("_AttackBlend");

    private void Awake()
    {
        turretCombat = GetComponent<TurretCombat>();
        turretHealth = GetComponent<TurretHealth>();

        if (crystalAnimator == null || crystalRenderer == null)
        {
            Debug.LogError(
                "Missing Crystal Animator or SpriteRenderer!",
                this
            );

            enabled = false;
            return;
        }

        propertyBlock = new MaterialPropertyBlock();

        currentBlend = 0f;
        crystalAnimator.speed = idleSpeed;

        ApplyCrystalColor();
    }

    private void LateUpdate()
    {
        EnemyHealth target = turretCombat.CurrentTarget;

        bool isAttacking =
            !turretHealth.IsDead &&
            target != null &&
            !target.IsDead;

        float targetBlend = isAttacking ? 1f : 0f;

        // Chuyển màu từ từ
        currentBlend = Mathf.MoveTowards(
            currentBlend,
            targetBlend,
            colorChangeSpeed * Time.deltaTime
        );

        ApplyCrystalColor();

        // Tăng hoặc giảm tốc độ Idle Animation
        crystalAnimator.speed = Mathf.Lerp(
            idleSpeed,
            attackSpeed,
            currentBlend
        );
    }

    private void ApplyCrystalColor()
    {
        crystalRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetFloat(
            AttackBlendID,
            currentBlend
        );

        crystalRenderer.SetPropertyBlock(propertyBlock);
    }
}
