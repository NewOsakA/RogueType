using UnityEngine;

[CreateAssetMenu(menuName = "RougeType/ActiveSkill")]
public class ActiveSkillData : ScriptableObject
{
    public ActiveSkillType skillType;
    [Header("Display")]
    public string skillName;

    [Header("Level")]
    public int currentLevel = 0;
    public int maxLevel = 5;

    [Header("Cooldown")]
    public float cooldown;

    [Header("Values Per Level")]
    public float[] durations;      
    public float[] pushDistances;  
    public int[] damages;          

    [Header("Cost")]
    public int buyCost;
    public int[] upgradeCosts;

    [Header("Essence Cost")]
    public int essenceCost;

    [Header("Descriptions (Manual)")]
    [TextArea] public string buyDescription;
    [TextArea] public string upgradeDescription;
    [TextArea] public string maxDescription;

    public string GetDescription()
    {
        if (currentLevel == 0)
            return buyDescription;

        if (currentLevel < maxLevel)
            return upgradeDescription;

        return maxDescription;
    }
}


public enum ActiveSkillType
{
    ForcePush,
    MassSlow,
    ShockwaveBurst,
    TimeBreak
}
