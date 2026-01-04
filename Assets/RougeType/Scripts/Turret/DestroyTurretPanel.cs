using UnityEngine;

public class DestroyTurretPanel : MonoBehaviour
{
    private TurretSlot slot;

    public void Init(TurretSlot slotRef)
    {
        slot = slotRef;
    }

    public void OnDestroyClicked()
    {
        slot.DestroyTurret();
        slot.ClosePanel();
        Destroy(gameObject);
    }

    public void OnCancelClicked()
    {
        slot?.ClosePanel();
        Destroy(gameObject);
    }
}
