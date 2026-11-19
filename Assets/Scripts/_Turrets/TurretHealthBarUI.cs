using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurretHealth turretHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        if (turretHealth == null)
        {
            turretHealth =
                GetComponentInParent<TurretHealth>();
        }
    }

    private void OnEnable()
    {
        if (turretHealth == null)
            return;

        // Register Update HP
        turretHealth.OnHealthChanged += UpdateHealthUI;

        // Register Destroyed Event
        turretHealth.OnDied += HideHealthBar;
    }

    private void Start()
    {
        if (turretHealth == null || healthSlider == null)
        {
            Debug.LogError(
                $"{name}: Missing Health Bar references!",
                this
            );
            return;
        }

        healthSlider.interactable = false;
        healthSlider.wholeNumbers = false;

        // Start game: Dong Bo voi data HP
        UpdateHealthUI(
            turretHealth.CurrentHealth,
            turretHealth.MaxHealth
        );

        if (turretHealth.IsDead)
        {
            HideHealthBar();
        }
    }

    private void UpdateHealthUI(float currentHealth,float maxHealth)
    {
        if (healthSlider == null)
            return;

        // Gioi han HP hien thi
        float safeMax = Mathf.Max(1f, maxHealth);

        healthSlider.minValue = 0f;
        healthSlider.maxValue = safeMax;

        healthSlider.SetValueWithoutNotify(
            Mathf.Clamp(currentHealth, 0f, safeMax)
        );

        if (healthText != null)
        {
            healthText.text =
                $"{Mathf.CeilToInt(currentHealth)} / " +
                $"{Mathf.CeilToInt(maxHealth)}";
        }
    }

    private void HideHealthBar()
    {
        // Tat canvas Health Bar
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (turretHealth == null)
            return;

        // Hủy đăng ký Event
        turretHealth.OnHealthChanged -= UpdateHealthUI;
        turretHealth.OnDied -= HideHealthBar;
    }
}
