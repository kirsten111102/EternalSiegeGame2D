using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// EXP dai han: chi TotalXPHundredths la du lieu EXP can luu.
/// Level, XP trong Level va MaxXP duoc tinh lai tu tong EXP.
/// 1 EXP = 100 don vi, nen 1.05 EXP = 105 don vi (khong mat phan le).

public class GameXP : MonoBehaviour
{
    public const int MaxLevel = 99;
    private const long XPScale = 100L;

    [Header("XP UI")]
    [SerializeField] private TextMeshProUGUI LevelText;
    [SerializeField] private Slider XPSlider;

    public int XPCount => (int)(XPIntoLevelHundredths / XPScale);
    public int TotalXPEarned => (int)(TotalXPHundredths / XPScale);
    public int CurrentLevel { get; private set; } = 1;
    public int MaxXP { get; private set; }
    public int BonusXPPercent { get; private set; }

    // Nguon EXP duy nhat. Khong luu CurrentLevel/XPCount/MaxXP trong JSON.
    public long TotalXPHundredths { get; private set; }
    public long XPIntoLevelHundredths { get; private set; }

    public event Action<int> OnLevelUp;

    // Bao cho WaveManager biet EXP GOC cua Enemy da bi giet trong wave.
    // Khong bao gom BonusXP; phan thuong Wave duoc tinh theo EXP goc.
    public event Action<int> OnEnemyBaseXPGranted;

    private void Awake()
    {
        RecalculateLevelFromTotalXP();
    }

    private void Start()
    {
        UpdateXPUI();
    }

    /// Tuong thich voi nhung cho dang goi AddXP() de thuong EXP Enemy.
    /// WaveManager phai goi AddWaveXP(), khong dung ham nay.
    
    public void AddXP(int amount)
    {
        AddEnemyXP(amount);
    }

    /// EXP Enemy co ap dung Bonus XP: 1 EXP, +5% => +105 don vi.
    
    public void AddEnemyXP(int baseAmount)
    {
        if (baseAmount <= 0 || CurrentLevel >= MaxLevel)
            return;

        long scaledAmount = (long)baseAmount * (XPScale + BonusXPPercent);
        if (GrantScaledXP(scaledAmount))
            OnEnemyBaseXPGranted?.Invoke(baseAmount);
    }

    /// EXP Wave KHONG nhan he so Bonus XP.
    
    public void AddWaveXP(int amount)
    {
        if (amount <= 0 || CurrentLevel >= MaxLevel)
            return;

        GrantScaledXP((long)amount * XPScale);
    }

    public void AddXPBonus(int percentagePoints)
    {
        if (percentagePoints <= 0 || CurrentLevel >= MaxLevel)
            return;

        // Tranh overflow so nguyen; khong dat gioi han gameplay 
        // co dinh cho bonus.
        BonusXPPercent = (int)Math.Min((long)int.MaxValue,
            (long)BonusXPPercent + percentagePoints);
        Debug.Log($"Bonus Enemy XP: +{BonusXPPercent}%");
    }

    /// Load JSON: KHONG goi OnLevelUp: Level phai duoc tinh tu EXP da luu.
    /// BonusXPPercent la trang thai PowerUp, khong phai Level.
    
    public void RestoreProgress(long savedTotalXPHundredths, int savedBonusXPPercent)
    {
        TotalXPHundredths = Math.Max(0L,
            Math.Min(savedTotalXPHundredths, GetXPNeededForLevel99Hundredths()));
        BonusXPPercent = Mathf.Max(0, savedBonusXPPercent);
        RecalculateLevelFromTotalXP();
        UpdateXPUI();
    }

    /// EXP can de len cap tiep theo TU Level hien tai (khong phai EXP tong).
    
    public static int GetXPRequiredForLevel(int level)
    {
        if (level < 1 || level >= MaxLevel)
            return 0;

        if (level <= 10) return 28 + 4 * (level - 1);    // 28..64
        if (level <= 30) return 68 + 5 * (level - 11);   // 68..163
        if (level <= 60) return 164 + 3 * (level - 31);  // 164..251
        return 253 + 2 * (level - 61);                   // 253..327
    }

    public static long GetXPNeededForLevel99Hundredths()
    {
        long sum = 0;
        for (int level = 1; level < MaxLevel; level++)
            sum += (long)GetXPRequiredForLevel(level) * XPScale;
        return sum;
    }

    private bool GrantScaledXP(long scaledAmount)
    {
        if (scaledAmount <= 0 || CurrentLevel >= MaxLevel)
            return false;

        long remainingCapacity = 
            GetXPNeededForLevel99Hundredths() - TotalXPHundredths;
        if (remainingCapacity <= 0)
            return false;

        long acceptedXP = Math.Min(scaledAmount, remainingCapacity);
        int previousLevel = CurrentLevel;
        TotalXPHundredths += acceptedXP;
        RecalculateLevelFromTotalXP();

        // Co the len nhieu Level tu 1 lan nhan EXP.
        for (int level = previousLevel + 1; level <= CurrentLevel; level++)
        {
            Debug.Log($"Level Up! Current Level: {level}");
            OnLevelUp?.Invoke(level);
        }

        UpdateXPUI();
        return acceptedXP > 0;
    }

    private void RecalculateLevelFromTotalXP()
    {
        TotalXPHundredths = Math.Max(0L,
            Math.Min(TotalXPHundredths, GetXPNeededForLevel99Hundredths()));

        long remainingXP = TotalXPHundredths;
        CurrentLevel = 1;

        while (CurrentLevel < MaxLevel)
        {
            long requirement = (long)GetXPRequiredForLevel(CurrentLevel) * XPScale;
            if (remainingXP < requirement)
                break;

            remainingXP -= requirement;
            CurrentLevel++;
        }

        if (CurrentLevel == MaxLevel)
        {
            MaxXP = 0;
            XPIntoLevelHundredths = 0;
        }
        else
        {
            MaxXP = GetXPRequiredForLevel(CurrentLevel);
            XPIntoLevelHundredths = remainingXP;
        }
    }

    private void UpdateXPUI()
    {
        if (XPSlider != null)
        {
            XPSlider.minValue = 0f;
            XPSlider.maxValue = CurrentLevel >= MaxLevel ? 1f : MaxXP * XPScale;
            XPSlider.value = CurrentLevel >= MaxLevel ? 1f : XPIntoLevelHundredths;
        }

        if (LevelText != null)
        {
            LevelText.text = CurrentLevel >= MaxLevel
                ? "Lv. 99 (MAX)"
                : $"Lv. {CurrentLevel} | XP: {XPIntoLevelHundredths / 100.0:0.##}/{MaxXP}";
        }
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG/Level Up Once")]
    private void DebugLevelUpOnce()
    {
        if (CurrentLevel < MaxLevel)
        {
            // Chi dung khi test; debug XP nay duoc xem nhu EXP Enemy.
            int amount = Mathf.Max(1, MaxXP - XPCount);
            AddEnemyXP(amount);
        }
    }

    [ContextMenu("DEBUG/Add 1000 Enemy XP")]
    private void DebugAdd1000XP()
    {
        AddEnemyXP(1000);
    }
#endif
}
