using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrystalCoreHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CrystalCoreHealth crystalCoreHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        if (crystalCoreHealth == null)
        {
            crystalCoreHealth =
                GetComponentInParent<CrystalCoreHealth>();
        }
    }

    private void OnEnable()
    {
        if (crystalCoreHealth == null)
            return;

        // Register Health Update Event
        crystalCoreHealth.OnHealthChanged += UpdateHealthUI;

        // Register Core Destroyed Event
        crystalCoreHealth.OnCoreDestroyed += HideHealthBar;
    }

    private void Start()
    {
        if (crystalCoreHealth == null || healthSlider == null)
        {
            Debug.LogError(
                $"{name}: Missing Health Bar references!",
                this
            );
            return;
        }

        healthSlider.interactable = false;
        healthSlider.wholeNumbers = false;

        // Initialize Health Bar
        UpdateHealthUI(
            crystalCoreHealth.CurrentHealth,
            crystalCoreHealth.MaxHealth
        );

        if (crystalCoreHealth.IsDead)
        {
            HideHealthBar();
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider == null)
            return;

        // Prevent invalid maximum HP
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
        // Disable Health Bar Canvas
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (crystalCoreHealth == null)
            return;

        // Unregister Events
        crystalCoreHealth.OnHealthChanged -= UpdateHealthUI;
        crystalCoreHealth.OnCoreDestroyed -= HideHealthBar;
    }
}