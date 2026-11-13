using UnityEngine;

// Test-ready Player stat upgrades: follows GameXP.OnLevelUp if GameXP exists,
// otherwise DEBUG context menus can simulate leveling without a GameXP object.
// No persistent state or external session manager is needed.
[DisallowMultipleComponent]
[RequireComponent(  typeof(PlayerHealth), 
                    typeof(PlayerCombat), 
                    typeof(PlayerMovement))]
public class PlayerLevelStats : MonoBehaviour
{
    [Header("Existing EXP system")]
    [SerializeField] private GameXP gameXP;

    private PlayerHealth playerHealth;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;

    public int CurrentLevel { get; private set; } = 1;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerCombat = GetComponent<PlayerCombat>();
        playerMovement = GetComponent<PlayerMovement>();
        ApplyStatsForLevel(1);
    }

    private void OnEnable()
    {
        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();

        if (gameXP != null)
            gameXP.OnLevelUp += HandleLevelUp;
        else
            Debug.Log("PlayerLevelStats: no GameXP in Scene. Use DEBUG/Level Up Once to test Player stats.");
    }

    private void Start()
    {
        // On Play, start at Level 1 (or use GameXP.CurrentLevel if present).
        // Start with runtime Level; no stored state is read.
        if (gameXP != null)
            ApplyLevel(gameXP.CurrentLevel);
    }

    private void OnDisable()
    {
        if (gameXP != null)
            gameXP.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        // GameXP announces the new level; don't calculate or save XP here.
        ApplyLevel(newLevel);
    }

    public void ApplyLevel(int newLevel)
    {
        int clamped = Mathf.Clamp(newLevel, 1, 99);
        if (CurrentLevel == clamped) return;

        ApplyStatsForLevel(clamped);
        Debug.Log(
            $"Player Lv.{CurrentLevel} | HP {playerHealth.CurrentHealth}/{playerHealth.MaxHealth} " +
            $"| Damage {playerCombat.CurrentAttackDamage} " +
            $"| Attack Speed {(1f / playerCombat.CurrentAttackCooldown):0.##}/s " +
            $"| Move Speed {playerMovement.CurrentMoveSpeed:0.##}");
    }

#if UNITY_EDITOR
    // In Play mode: component's three-dot menu -> DEBUG/Level Up Once.
    // This tests stats without needing GameXP in the Hierarchy.
    [ContextMenu("DEBUG/Level Up Once")]
    private void DebugLevelUpOnce()
    {
        if (!Application.isPlaying) return;
        ApplyLevel(CurrentLevel + 1);
    }

    [ContextMenu("DEBUG/Reset to Level 1")]
    private void DebugResetLevel()
    {
        if (!Application.isPlaying) return;
        ApplyLevel(1);
    }
#endif

    private void ApplyStatsForLevel(int newLevel)
    {
        CurrentLevel = Mathf.Clamp(newLevel, 1, 99);
        int bonus = CurrentLevel - 1;

        playerHealth.ApplyLevelBonus(bonus);
        playerCombat.ApplyLevelBonus(bonus);
        playerMovement.ApplyLevelBonus(bonus);
    }

    
}
