using UnityEngine;

public class ItemUseManager : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData emergencyRepair;
    public ItemData structuralReinforcement;
    public ItemData executionCredit;
    public ItemData escentGain;

    private Wall wall;

    void Start()
    {
        wall = Object.FindFirstObjectByType<Wall>();

        // if (wall == null)
        // {
        //     Debug.LogError("Wall not found");
        // }
    }

    void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.currentPhase != GamePhase.WaveDefense)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha7))
            Use(emergencyRepair);

        if (Input.GetKeyDown(KeyCode.Alpha8))
            Use(structuralReinforcement);

        if (Input.GetKeyDown(KeyCode.Alpha9))
            Use(executionCredit);

        if (Input.GetKeyDown(KeyCode.Alpha0))
            Use(escentGain);
    }

    void Use(ItemData data)
    {
        if (data == null)
            return;

        if (ItemInventory.Instance == null)
            return;

        if (!ItemInventory.Instance.Consume(data.itemType))
            return;

        ApplyEffect(data);
    }

    void ApplyEffect(ItemData data)
    {
        if (data == null)
            return;

        switch (data.itemType)
        {
            case UsableItemType.EmergencyRepair:
                if (wall != null)
                {
                    wall.Heal(data.healAmount);
                }
                break;

            case UsableItemType.StructuralReinforcement:
                if (wall != null)
                {
                    wall.ActivateDamageReduction(
                        data.damageReduction,
                        data.duration
                    );
                }
                break;

            case UsableItemType.ExecutionCredit:
                ExecutionCreditSystem.Instance.Activate(
                    data.duration,
                    data.bonusPercent
                );
                break;

            case UsableItemType.EscentGain:
                if (EssenceManager.Instance != null)
                {
                    EssenceManager.Instance.AddEssence(data.escentGain);
                }
                break;
        }
    }
}
