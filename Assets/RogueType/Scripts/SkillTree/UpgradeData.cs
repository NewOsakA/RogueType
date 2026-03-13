// UpgradeData.cs

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
    // public UpgradeEffectType effectType;
    public List<UpgradeEffect> effects = new List<UpgradeEffect>();

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
    Rare,
    Epic,
    Ally_upg_1,
    Ally_upg_2,
}

public enum UpgradeEffectType
{
    // POWER
    IncreaseDamage,
    IncreaseDamagePercentage,
    DecreaseDamage,
    DecreaseDamagePercentage,
    Burn,
    Execution,

    // DEFENSE
    IncreaseWallHP,
    IncreaseWallHPPercentage,
    DecreaseWallHP,
    DecreaseWallHPPercentage,
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
    GlassCannon,

    // Upgrade
    AOEBoost,
    AOEDamage,
    MultiShot, 
    MultiShotPenalty,  
    SetWallHPToOne,
    AutoRepairUpgrade,


}

[System.Serializable]
public class UpgradeEffect
{
    public UpgradeEffectType type;
    public int intValue;
    public float floatValue;
}

