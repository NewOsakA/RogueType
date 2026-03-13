using UnityEngine;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance;

    [Header("Skill Data")]
    public List<ActiveSkillData> skills = new List<ActiveSkillData>();

    [Header("UI Slots")]
    public List<SkillSlotUI> slots = new List<SkillSlotUI>();

    private float[] cooldownRemaining;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        cooldownRemaining = new float[skills.Count];
    }

    void Start()
    {
        ResetAllSkills();

        if (EssenceManager.Instance != null)
            EssenceManager.Instance.OnEssenceChanged += _ => UpdateCooldowns();
    }

    void Update()
    {
        HandleInput();
        UpdateCooldowns();
    }

    public ActiveSkillData GetSkill(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return skills[index];
    }

    public void UseSkillByIndex(int index)
    {
        TryUse(index);
    }

    void HandleInput()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsWavePhase())
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) TryUse(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryUse(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TryUse(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TryUse(3);
    }

void UpdateCooldowns()
{
    for (int i = 0; i < skills.Count; i++)
    {
        var skill = skills[i];

        if (skill.currentLevel == 0)
        {
            slots[i].SetLocked();
            continue;
        }

        if (cooldownRemaining[i] > 0)
        {
            cooldownRemaining[i] -= Time.deltaTime;
            slots[i].SetCooldown(cooldownRemaining[i]);
            continue;
        }

        if (!EssenceManager.Instance.HasEnoughEssence(skill.essenceCost))
        {
            slots[i].SetNoEssence(skill.essenceCost);
        }
        else
        {
            slots[i].SetReady(skill.essenceCost);
        }
    }
}


    void ResetAllSkills()
    {
        for (int i = 0; i < skills.Count; i++)
        {
            skills[i].currentLevel = 0;
            cooldownRemaining[i] = 0f;

            if (i < slots.Count)
                slots[i].SetLocked();
        }
    }

    void TryUse(int index)
    {
        if (!GameManager.Instance.IsWavePhase())
            return;
            
        if (!IsValidIndex(index))
            return;

        var skill = skills[index];
        Debug.Log($"TryUse {index} | {skill.skillName} | level={skill.currentLevel}");

        if (skill.currentLevel == 0)
        {
            Debug.Log("Skill not unlocked");
            return;
        }

        if (skill.currentLevel <= 0)
            return;

        if (cooldownRemaining[index] > 0f)
            return;

        if (!EssenceManager.Instance.TryConsumeEssence(skill.essenceCost))
        {
            Debug.Log("Not enough Essence!");
            return;
        }

        if (!Activate(skill))
            return;

        cooldownRemaining[index] = skill.cooldown;
    }

    bool Activate(ActiveSkillData skill)
    {
        int lvl = skill.currentLevel - 1;

        switch (skill.skillType)
        {
            case ActiveSkillType.ForcePush:
                if (lvl >= skill.pushDistances.Length) return false;
                SkillEffects.ForcePush(skill.pushDistances[lvl]);
                break;

            case ActiveSkillType.MassSlow:
                if (lvl >= skill.durations.Length) return false;
                SkillEffects.MassSlow(skill.durations[lvl]);
                break;

            case ActiveSkillType.ShockwaveBurst:
                if (lvl >= skill.damages.Length) return false;
                SkillEffects.Shockwave(skill.damages[lvl]);
                break;

            case ActiveSkillType.TimeBreak:
                if (lvl >= skill.durations.Length) return false;
                SkillEffects.TimeBreak(skill.durations[lvl]);
                break;

            default:
                return false;
        }

        return true;
    }

    bool IsValidIndex(int index)
    {
        return index >= 0 && index < skills.Count;
    }
}