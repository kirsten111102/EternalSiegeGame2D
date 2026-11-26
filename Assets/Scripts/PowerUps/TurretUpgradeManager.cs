using UnityEngine;

public class TurretUpgradeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameXP gameXP;

    private void Awake()
    {
        if (gameXP == null)
            gameXP = FindFirstObjectByType<GameXP>();
    }

    public void ApplyUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.TurretRepair:
                HealAllTurrets();
                return;

            case UpgradeType.BonusXp:
                if (gameXP != null)
                    gameXP.AddXPBonus(5);
                else
                    Debug.LogWarning("GameXP is missing; Bonus XP could not be applied.");
                return;
        }

        // Chi quet khi chon the, ko quet moi frame
        TurretCombat[] turrets = FindObjectsByType<TurretCombat>(
            FindObjectsSortMode.None);

        foreach (TurretCombat turret in turrets)
        {
            if (!turret.isActiveAndEnabled)
                continue;

            switch (type)
            {
                case UpgradeType.SharpBullet:
                    turret.AddDamage(0.5f);
                    break;

                case UpgradeType.RapidFire:
                    turret.AddAttackSpeed(15f);
                    break;

                case UpgradeType.LongRange:
                    turret.AddRange(0.5f);
                    break;
            }
        }
    }

    private void HealAllTurrets()
    {
        TurretHealth[] turrets = FindObjectsByType<TurretHealth>(
            FindObjectsSortMode.None);

        foreach (TurretHealth turret in turrets)
        {
            if (turret.IsDead || !turret.isActiveAndEnabled)
                continue;

            turret.HealPercent(0.2f);
        }
    }

    
}
