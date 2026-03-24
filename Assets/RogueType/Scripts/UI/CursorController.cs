using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CursorController : MonoBehaviour
{
    [System.Serializable]
    private struct CursorStyle
    {
        public Texture2D texture;
        public Vector2 hotspot;
        public CursorMode mode;
    }

    private enum CursorState
    {
        Default,
        Hover,
        Blocked
    }

    [Header("Cursor Styles")]
    [SerializeField] private CursorStyle defaultStyle = new CursorStyle { mode = CursorMode.Auto };
    [SerializeField] private CursorStyle hoverStyle = new CursorStyle { mode = CursorMode.Auto };
    [SerializeField] private CursorStyle blockedStyle = new CursorStyle { mode = CursorMode.Auto };

    [Header("Behavior")]
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private bool onlyWhenCursorVisible = true;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(16);
    private PointerEventData pointerEventData;
    private EventSystem cachedEventSystem;
    private CursorState appliedState = (CursorState)(-1);

    private void Awake()
    {
        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        ApplyState(CursorState.Default, force: true);
    }

    private void OnDisable()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        appliedState = (CursorState)(-1);
    }

    private void Update()
    {
        if (onlyWhenCursorVisible && !Cursor.visible)
            return;

        ApplyState(ResolveState(), force: false);
    }

    private CursorState ResolveState()
    {
        EventSystem currentEventSystem = EventSystem.current;
        if (currentEventSystem == null)
            return CursorState.Default;

        if (cachedEventSystem != currentEventSystem || pointerEventData == null)
        {
            cachedEventSystem = currentEventSystem;
            pointerEventData = new PointerEventData(cachedEventSystem);
        }

        pointerEventData.position = Input.mousePosition;
        raycastResults.Clear();
        cachedEventSystem.RaycastAll(pointerEventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GameObject hitObject = raycastResults[i].gameObject;
            if (hitObject == null || !hitObject.activeInHierarchy)
                continue;

            Selectable selectable = hitObject.GetComponentInParent<Selectable>();
            if (selectable != null)
                return selectable.IsInteractable() ? CursorState.Hover : CursorState.Blocked;

            IPointerEnterHandler pointerEnterHandler = hitObject.GetComponentInParent<IPointerEnterHandler>();
            if (pointerEnterHandler != null)
                return CursorState.Hover;
        }

        return CursorState.Default;
    }

    private void ApplyState(CursorState state, bool force)
    {
        if (!force && state == appliedState)
            return;

        CursorStyle style = defaultStyle;
        if (state == CursorState.Hover)
            style = hoverStyle;
        else if (state == CursorState.Blocked)
            style = blockedStyle;

        Cursor.SetCursor(style.texture, style.hotspot, style.mode);
        appliedState = state;
    }
}
