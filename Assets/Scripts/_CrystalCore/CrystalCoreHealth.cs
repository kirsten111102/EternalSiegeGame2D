using System;
using UnityEngine;

public class CrystalCoreHealth : MonoBehaviour
{
    [Header("Core Stats")]
    [SerializeField] private float maxHealth = 2000f;
    [SerializeField] private GameManager gameManager;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    // Health UI update event
    public event Action<float, float> OnHealthChanged;

    // Core destroyed event
    public event Action OnCoreDestroyed;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(
            0f,
            CurrentHealth - damage
        );

        Debug.Log($"Crystal Core HP: {CurrentHealth}");

        // Update Health Bar
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
            Die();
    }

    public void RestoreHealth(float savedHealth)
    {
        CurrentHealth = Mathf.Clamp(
            savedHealth,
            0f,
            maxHealth
        );

        // Update Health Bar
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        IsDead = true;

        Debug.Log("GAME OVER - Crystal Core destroyed!");

        OnCoreDestroyed?.Invoke();

        if (gameManager != null)
            gameManager.GameOver();

        gameObject.SetActive(false);

        Time.timeScale = 0f;
    }
}