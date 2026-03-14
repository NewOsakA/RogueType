// UpgradeData.cs

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

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
    public List<UpgradeEffect> effects = new List<UpgradeEffect>();
    [FormerlySerializedAs("effectType")]
    [SerializeField] private int legacyEffectType = -1;
    [FormerlySerializedAs("intValue")]
    [SerializeField] private int legacyIntValue = 0;
    [FormerlySerializedAs("floatValue")]
    [SerializeField] private float legacyFloatValue = 0f;

    // EXCLUSIVE
    [Header("Exclusive Rule")]
    public bool exclusive;
    public ExclusiveGroup exclusiveGroup;

    public IEnumerable<UpgradeEffect> GetEffects()
    {
        if (effects != null && effects.Count > 0)
            return effects;

        if (legacyEffectType < 0)
            return System.Array.Empty<UpgradeEffect>();

        return new[]
        {
            new UpgradeEffect
            {
                type = (UpgradeEffectType)legacyEffectType,
                intValue = legacyIntValue,
                floatValue = legacyFloatValue
            }
        };
    }
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
    UnlockAlly1,
    UnlockAlly2,
    IncreaseAlly1Damage,
    IncreaseAlly2Damage,
    ReduceAlly1Interval,
    ReduceAlly2Interval,
    EnableAlly1Burn,
    EnableAlly2Burn,
    EnableAlly1Frost,
    EnableAlly2Frost,
}

[System.Serializable]
public class UpgradeEffect
{
    public UpgradeEffectType type;
    public int intValue;
    public float floatValue;
}
