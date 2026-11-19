using System;
using UnityEngine;

public class CrystalCoreHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 2000f;
    [SerializeField] private GameManager gameManager;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action OnCoreDestroyed;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f) return;

        CurrentHealth = Mathf.Max(
            0f,
            CurrentHealth - damage
        );

        Debug.Log($"Crystal Core HP: {CurrentHealth}");

        if (CurrentHealth <= 0f) Die();
    }

    public void RestoreHealth(float savedHealth)
    {
        CurrentHealth = Mathf.Clamp(savedHealth, 0f, maxHealth);
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
