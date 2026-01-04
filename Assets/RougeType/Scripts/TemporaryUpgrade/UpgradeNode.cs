using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UpgradeNode : MonoBehaviour, IPointerEnterHandler
{
    public UpgradeData data;
    public Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Refresh()
    {
        TemporaryUpgradeManager manager = TemporaryUpgradeManager.Instance;

        if (manager == null)
            return;

        if (button == null)
        {
            Debug.LogWarning($"UpgradeNode Missing Button on {gameObject.name}");
            return;
        }

        if (data == null)
        {
            button.interactable = false;
            return;
        }

        bool shouldBeInteractable = true;

        if (manager.IsUnlocked(data))
        {
            shouldBeInteractable = false;
        }
        else if (!manager.IsPrerequisiteMet(data))
        {
            shouldBeInteractable = false;
        }
        else if (
            data.exclusiveGroup != ExclusiveGroup.None &&
            manager.IsExclusiveGroupTaken(data.exclusiveGroup)
        )
        {
            shouldBeInteractable = false;
        }

        button.interactable = shouldBeInteractable;
    }



    public void Buy()
    {
        TemporaryUpgradeManager manager = TemporaryUpgradeManager.Instance;
        if (manager != null)
        {
            manager.TryBuy(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TemporaryUpgradeManager manager = TemporaryUpgradeManager.Instance;
        if (manager != null)
        {
            manager.ShowInfo(data);
        }
    }
}
