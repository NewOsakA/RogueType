using System;
using UnityEngine;

public enum GameDifficultyMode
{
    Casual = 0,
    Normal = 1,
    Hardcore = 2,
    Deathcore = 3
}

[Serializable]
public class DifficultyModeProfile
{
    [Header("Identity")]
    public GameDifficultyMode mode = GameDifficultyMode.Normal;

    [Header("Enemy Multipliers")]
    [Range(0.1f, 3f)]
    public float enemySpeedMultiplier = 1f;
    [Range(0.1f, 5f)]
    public float enemyDamageMultiplier = 1f;
    [Range(0.1f, 5f)]
    public float enemyHealthMultiplier = 1f;

    [Header("Spawn Scaling")]
    [Range(-20, 50)]
    public int enemySpawnFlatBonus = 0;
    [Range(0, 10)]
    public int enemySpawnBonusPerWave = 0;

    [Header("Wall")]
    [Range(1, 10000)]
    public int wallStartHp = 100;
    public bool allowMetaWallUpgrades = true;
    public bool lockWallHpToOne = false;

    [Header("Economy")]
    [Range(0f, 5f)]
    public float currencyRewardMultiplier = 1f;

    public static DifficultyModeProfile CreateDefault(GameDifficultyMode mode)
    {
        switch (mode)
        {
            case GameDifficultyMode.Casual:
                return new DifficultyModeProfile
                {
                    mode = mode,
                    enemySpeedMultiplier = 0.85f,
                    enemyDamageMultiplier = 0.85f,
                    enemyHealthMultiplier = 0.90f,
                    enemySpawnFlatBonus = -2,
                    enemySpawnBonusPerWave = 0,
                    wallStartHp = 140,
                    allowMetaWallUpgrades = true,
                    lockWallHpToOne = false,
                    currencyRewardMultiplier = 1.20f
                };

            case GameDifficultyMode.Hardcore:
                return new DifficultyModeProfile
                {
                    mode = mode,
                    enemySpeedMultiplier = 1.15f,
                    enemyDamageMultiplier = 1.25f,
                    enemyHealthMultiplier = 1.20f,
                    enemySpawnFlatBonus = 1,
                    enemySpawnBonusPerWave = 1,
                    wallStartHp = 90,
                    allowMetaWallUpgrades = true,
                    lockWallHpToOne = false,
                    currencyRewardMultiplier = 0.95f
                };

            case GameDifficultyMode.Deathcore:
                return new DifficultyModeProfile
                {
                    mode = mode,
                    enemySpeedMultiplier = 1.30f,
                    enemyDamageMultiplier = 1.50f,
                    enemyHealthMultiplier = 1.35f,
                    enemySpawnFlatBonus = 2,
                    enemySpawnBonusPerWave = 1,
                    wallStartHp = 1,
                    allowMetaWallUpgrades = false,
                    lockWallHpToOne = true,
                    currencyRewardMultiplier = 0.90f
                };

            default:
                return new DifficultyModeProfile
                {
                    mode = GameDifficultyMode.Normal,
                    enemySpeedMultiplier = 1f,
                    enemyDamageMultiplier = 1f,
                    enemyHealthMultiplier = 1f,
                    enemySpawnFlatBonus = 0,
                    enemySpawnBonusPerWave = 0,
                    wallStartHp = 110,
                    allowMetaWallUpgrades = true,
                    lockWallHpToOne = false,
                    currencyRewardMultiplier = 1f
                };
        }
    }
}
