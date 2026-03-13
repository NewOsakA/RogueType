// // UpgradeNode.cs

// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.EventSystems;

// public class UpgradeNode : MonoBehaviour, IPointerEnterHandler
// {
//     private enum VisualState
//     {
//         Available,
//         Unavailable,
//         Bought,
//         RequirementNotMet
//     }

//     public UpgradeData data;
//     public Button button;
//     [SerializeField] private Animator nodeAnimator;
//     [SerializeField] private string availableStateName = "Available";
//     [SerializeField] private string unavailableStateName = "Unavailable";
//     [SerializeField] private string boughtStateName = "Bought";
//     [SerializeField] private string requirementNotMetStateName = "RequirementNotMet";

//     void Awake()
//     {
//         button = GetComponent<Button>();
//         if (nodeAnimator == null)
//             nodeAnimator = GetComponent<Animator>();

//         if (button != null)
//             button.transition = Selectable.Transition.None;
//     }

//     public void Refresh()
//     {
//         SkillTreeUpgradeManager manager = SkillTreeUpgradeManager.Instance;

//         if (manager == null)
//             return;

//         if (button == null)
//         {
//             Debug.LogWarning($"UpgradeNode Missing Button on {gameObject.name}");
//             return;
//         }

//         if (data == null)
//         {
//             button.interactable = false;
//             ApplyVisualState(VisualState.Unavailable);
//             return;
//         }

//         VisualState visualState;
//         bool shouldBeInteractable;

//         if (manager.IsUnlocked(data))
//         {
//             visualState = VisualState.Bought;
//             shouldBeInteractable = false;
//         }
//         else if (!manager.IsPrerequisiteMet(data) || manager.IsGroupLockedFor(data))
//         {
//             visualState = VisualState.Unavailable;
//             shouldBeInteractable = false;
//         }
//         else if (!manager.CanAfford(data))
//         {
//             visualState = VisualState.RequirementNotMet;
//             shouldBeInteractable = false;
//         }
//         else
//         {
//             visualState = VisualState.Available;
//             shouldBeInteractable = true;
//         }

//         button.interactable = shouldBeInteractable;
//         ApplyVisualState(visualState);
//     }

//     private void ApplyVisualState(VisualState visualState)
//     {
//         if (nodeAnimator == null)
//             return;

//         string[] candidateStateNames = visualState switch
//         {
//             VisualState.Available => new[] { availableStateName, "NormalState" },
//             VisualState.Bought => new[] { boughtStateName, "BoughtState" },
//             VisualState.RequirementNotMet => new[] { requirementNotMetStateName },
//             _ => new[] { unavailableStateName }
//         };

//         foreach (string stateName in candidateStateNames)
//         {
//             if (string.IsNullOrWhiteSpace(stateName))
//                 continue;

//             int stateHash = Animator.StringToHash(stateName);
//             if (!nodeAnimator.HasState(0, stateHash))
//                 continue;

//             nodeAnimator.Play(stateHash, 0, 0f);
//             nodeAnimator.Update(0f);
//             return;
//         }
//     }

//     public void Buy()
//     {
//         SkillTreeUpgradeManager manager = SkillTreeUpgradeManager.Instance;
//         if (manager != null)
//         {
//             manager.TryBuy(this);
//         }
//     }

//     public void OnPointerEnter(PointerEventData eventData)
//     {
//         SkillTreeUpgradeManager manager = SkillTreeUpgradeManager.Instance;
//         if (manager != null)
//         {
//             manager.ShowInfo(data);
//         }
//     }
// }
