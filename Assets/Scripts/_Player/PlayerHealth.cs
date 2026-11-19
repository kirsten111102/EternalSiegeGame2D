using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Base health stat (Level 1)")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private PlayerHealthUI healthBarUI;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField, Min(0f)] private float secondsPenaltyPerDeath = 10f;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private bool hideSpritesWhileDead = true;

    private float currentHealth;
    private float effectiveMaxHealth;
    private bool initialized;
    private bool isDead;
    // Runtime-only; resets when entering Play mode (no persistence).
    private int deathCount;
    private float respawnSecondsRemaining;
    private Vector3 initialPosition;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerInteraction playerInteraction;
    private SpriteRenderer[] spriteRenderers;
    private bool[] spriteEnabledBeforeDeath;
    private bool? movementEnabledBeforeDeath;
    private bool? combatEnabledBeforeDeath;
    private bool? interactionEnabledBeforeDeath;
    private bool? colliderEnabledBeforeDeath;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => initialized || effectiveMaxHealth > 0f ? effectiveMaxHealth : maxHealth;
    public float BaseMaxHealth => maxHealth;
    public bool IsDead => isDead;
    public int DeathCount => deathCount;
    public float RespawnSecondsRemaining => respawnSecondsRemaining;

    // UI can subscribe later; no timer UI required yet.
    public event Action<int> DeathCountChanged;
    public event Action<float> RespawnTimeChanged;

    private void Awake()
    {
        initialPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerInteraction = GetComponent<PlayerInteraction>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        spriteEnabledBeforeDeath = new bool[spriteRenderers.Length];
        if (effectiveMaxHealth <= 0f) effectiveMaxHealth = Mathf.Max(1f, maxHealth);
    }

    private void Start()
    {
        // Test mode: start each Play session with full HP.
        currentHealth = effectiveMaxHealth;
        initialized = true;
        RefreshHealth();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || !initialized || isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RefreshHealth();
        if (currentHealth <= 0f) Die();
    }

    // public void Heal(float amount)
    // {
    //     if (amount <= 0f || !initialized || isDead) return;

    //     currentHealth = Mathf.Min(effectiveMaxHealth, currentHealth + amount);
    //     RefreshHealth();
    // }

    public void SetHealth(float health)
    {
        if (!initialized || isDead) return;

        currentHealth = Mathf.Clamp(health, 0f, effectiveMaxHealth);
        RefreshHealth();
        if (currentHealth <= 0f) Die();
    }

    // Called by PlayerLevelStats. +1 Maximum HP per level.
    public void ApplyLevelBonus(float bonus)
    {
        effectiveMaxHealth = Mathf.Max(1f, maxHealth + Mathf.Max(0f, bonus));
        if (!initialized) return; // Start() will initialize using the new maximum.

        // Increasing Max HP does not automatically heal current HP.
        currentHealth = Mathf.Clamp(currentHealth, 0f, effectiveMaxHealth);
        RefreshHealth();
    }

    private void BeginDeath()
    {
        if (isDead) return;

        isDead = true;

        DeathCountChanged?.Invoke(deathCount);
        RespawnTimeChanged?.Invoke(respawnSecondsRemaining);

        if (playerMovement != null)
        {
            movementEnabledBeforeDeath = playerMovement.enabled;
            playerMovement.StopMovement();
            playerMovement.enabled = false;
        }
        if (playerCombat != null)
        {
            combatEnabledBeforeDeath = playerCombat.enabled;
            playerCombat.enabled = false;
        }
        if (playerInteraction != null)
        {
            interactionEnabledBeforeDeath = playerInteraction.enabled;
            playerInteraction.enabled = false;
        }
        if (bodyCollider != null)
        {
            colliderEnabledBeforeDeath = bodyCollider.enabled;
            bodyCollider.enabled = false;
        }
        if (hideSpritesWhileDead)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteEnabledBeforeDeath[i] = spriteRenderers[i].enabled;
                spriteRenderers[i].enabled = false;
            }
        }

        Debug.Log($"Player died #{deathCount}. Respawn after {respawnSecondsRemaining:0} seconds.");

        // Keep Player GameObject active so its respawn coroutine can run.
        StartCoroutine(RespawnAfterPenalty());
    }

    private void Die()
    {
        if (isDead) return;

        deathCount++;
        respawnSecondsRemaining = secondsPenaltyPerDeath * deathCount;

        BeginDeath();
    }

    // Chỉ gọi sau Start(), trên Player trong scene mới.
    public void RestoreState(
        float savedHealth,
        int savedDeathCount,
        float savedRespawnSeconds)
    {
        currentHealth = Mathf.Clamp(savedHealth, 0f, effectiveMaxHealth);
        deathCount = Mathf.Max(0, savedDeathCount);
        respawnSecondsRemaining = currentHealth <= 0f
            ? Mathf.Max(0f, savedRespawnSeconds)
            : 0f;

        RefreshHealth();

        if (currentHealth <= 0f)
        {
            // Không tăng deathCount, không tính lại thời gian phạt.
            BeginDeath();
        }
        else
        {
            DeathCountChanged?.Invoke(deathCount);
            RespawnTimeChanged?.Invoke(respawnSecondsRemaining);
        }
    }

    private IEnumerator RespawnAfterPenalty()
    {
        while (respawnSecondsRemaining > 0f)
        {
            respawnSecondsRemaining = Mathf.Max(
                0f, respawnSecondsRemaining - Time.deltaTime);
            RespawnTimeChanged?.Invoke(respawnSecondsRemaining);
            yield return null;
        }

        Vector3 targetPosition = 
            respawnPoint != null ? respawnPoint.position : initialPosition;
            
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = targetPosition;
        }
        else
        {
            transform.position = targetPosition;
        }

        currentHealth = effectiveMaxHealth;
        isDead = false;
        RefreshHealth();

        if (hideSpritesWhileDead)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
                spriteRenderers[i].enabled = spriteEnabledBeforeDeath[i];
        }

        if (bodyCollider != null && colliderEnabledBeforeDeath.HasValue)
            bodyCollider.enabled = colliderEnabledBeforeDeath.Value;
        if (playerMovement != null && movementEnabledBeforeDeath.HasValue)
            playerMovement.enabled = movementEnabledBeforeDeath.Value;
        if (playerCombat != null && combatEnabledBeforeDeath.HasValue)
            playerCombat.enabled = combatEnabledBeforeDeath.Value;
        if (playerInteraction != null && interactionEnabledBeforeDeath.HasValue)
            playerInteraction.enabled = interactionEnabledBeforeDeath.Value;

        Debug.Log($"Player respawned: HP {currentHealth}/{effectiveMaxHealth}, deaths: {deathCount}.");
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG / Take 999 Damage")]
    private void DebugKillPlayer()
    {
        TakeDamage(999f);
    }
#endif

#if UNITY_EDITOR
    [ContextMenu("DEBUG / Take 20 Damage")]
    private void DebugTakeDamage()
    {
        TakeDamage(20f);
    }
#endif

    private void RefreshHealth()
    {
        // UI is optional while testing in a Scene.
        if (healthBarUI != null)
            healthBarUI.SetHealth(currentHealth, effectiveMaxHealth);
    }
}
