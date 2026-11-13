using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;

    public void SetMaxHealth(float maxHealth)
    {
        healthSlider.minValue = 0;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
    }

    public void SetHealth(float currentHealth, float effectiveMaxHealth)
    {
        healthSlider.maxValue = Mathf.Max(1f, effectiveMaxHealth);

        healthSlider.value = Mathf.Clamp(
            currentHealth,
            0f,
            healthSlider.maxValue
        );
    }

    
}