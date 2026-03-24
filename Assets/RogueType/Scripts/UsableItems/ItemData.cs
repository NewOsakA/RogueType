using UnityEngine;

public enum UsableItemType
{
    EmergencyRepair,
    StructuralReinforcement,
    ExecutionCredit,
    EscentGain
}

[CreateAssetMenu(menuName = "RougeType/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea]
    public string description;

    public UsableItemType itemType;

    [Header("Shop")]
    public int cost;
    public int maxCarry;

    [Header("Effects")]
    public int healAmount;
    public float damageReduction;
    public float duration;
    public int escentGain;
    [Header("Execution Credit")]
    [Range(0f, 5f)]
    public float bonusPercent;
}
