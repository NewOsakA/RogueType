using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "RougeType/Upgrade")]
public class UpgradeData : ScriptableObject
{
    [Header("Basic Info")]
    public string upgradeName;

    [TextArea(3, 6)]
    public string description;

    public int cost;

    [Header("Tree")]
    public List<UpgradeData> prerequisites = new List<UpgradeData>();

    public PrerequisiteMode prerequisiteMode = PrerequisiteMode.All;

    [Header("Effect")]
    public UpgradeEffectType effectType;

    [Tooltip("Used for int-based effects (damage, wall hp, combo goal, etc.)")]
    public int intValue;

    [Tooltip("Used for float-based effects (crit chance, multiplier, etc.)")]
    public float floatValue;

    // EXCLUSIVE
    [Header("Exclusive Rule")]
    public bool exclusive;
    public ExclusiveGroup exclusiveGroup;
}
public enum PrerequisiteMode
{
    Any,
    All
}
public enum ExclusiveGroup
{
    None,
    Common,
    Rare
}

public enum UpgradeEffectType
{
    // POWER
    IncreaseDamage,
    Burn,
    Execution,

    // DEFENSE
    IncreaseWallHP,
    Shield,
    AutoRepair,
    Fortress,

    // PRECISION
    Combo,
    ComboDamage,
    CritChance,
    CritBoost,

    // ECONOMY
    GoldMultiplier,
    Interest,
    DiscountShop,
    //(EXCLUSIVE)
    // Common
    ChainShot,
    ExplosiveShot,
    TypingFrenzy,

    // Rare
    PrecisionBurst,
    FocusedFire,
    GlassCannon
}

