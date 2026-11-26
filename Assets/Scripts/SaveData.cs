using System;

[Serializable]
public class SaveData
{
    // New Game creates a new ID; loading a checkpoint preserves it.
    public string runId;

    // Player data
    public float playerX;
    public float playerY;
    public float health;
    public int deathCount;
    public float respawnSecondsRemaining;

    // Score
    public int score;

    // Wave and EXP
    public int nextWaveNumber = 1;
    public long totalXPHundredths;
    public int bonusXPPercent;

    // Crystal Core
    public float coreHealth;

    // Turrets
    public TurretSaveData[] turrets;
}

[Serializable]
public class TurretSaveData
{
    public string turretId;
    public float currentHealth;
    public float damagePerTick;
    public float attackRange;
    public float attackSpeedPoints;
}