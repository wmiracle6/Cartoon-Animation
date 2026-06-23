using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Íàñòğîéêè ıëåìåíòà")]
    public bool enableDrag = true;
    public Color hoverColor = new Color(0.8f, 0.9f, 1f, 1f);

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private Color originalColor;
    private StopMotionWithAudio manager;
    private int currentIndex;
    private bool isDragging = false;

    public delegate void OnMoveDelegate(int fromIndex, int toIndex);
    public event OnMoveDelegate OnMoveEvent;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
            originalColor = backgroundImage.color;

        manager = FindAnyObjectByType<StopMotionWithAudio>();
    }

    public void Initialize(int index)
    {
        currentIndex = index;
    }

    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    public void MoveToIndex(int targetIndex)
    {
        if (manager == null)
        {
            manager = FindAnyObjectByType<StopMotionWithAudio>();
            if (manager == null)
            {
                Debug.LogWarning("StopMotionWithAudio íå íàéäåí!");
                return;
            }
        }

        int fromIndex = currentIndex;

        if (fromIndex == targetIndex) return;

        // Âûçûâàåì ìåòîä MoveFrame ó ìåíåäæåğà
        manager.MoveFrame(fromIndex, targetIndex);

        // ÎÁÍÎÂËßÅÌ ÈÍÄÅÊÑ
        currentIndex = targetIndex;

        // ÎÁÍÎÂËßÅÌ ÏÎÇÈÖÈŞ Â ÈÅĞÀĞÕÈÈ
        transform.SetSiblingIndex(targetIndex);

        // Âûçûâàåì ñîáûòèå
        OnMoveEvent?.Invoke(fromIndex, targetIndex);
    }

    public void OnHover(bool isHovering)
    {
        if (backgroundImage != null)
        {
            if (isHovering)
            {
                backgroundImage.color = hoverColor;
                rectTransform.localScale = Vector3.one * 1.05f;
            }
            else
            {
                backgroundImage.color = originalColor;
                rectTransform.localScale = Vector3.one;
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDrag || isDragging) return;

        isDragging = true;
        currentIndex = transform.GetSiblingIndex();

        if (DragDropManager.Instance != null)
            DragDropManager.Instance.StartDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!enableDrag || !isDragging) return;

        if (DragDropManager.Instance != null)
            DragDropManager.Instance.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enableDrag || !isDragging) return;

        if (DragDropManager.Instance != null)
            DragDropManager.Instance.EndDrag();

        // ÎÁÍÎÂËßÅÌ ÈÍÄÅÊÑ ÏÎÑËÅ ÄĞÎÏÀ
        currentIndex = transform.GetSiblingIndex();
        isDragging = false;
    }

    private void OnDestroy()
    {
        OnMoveEvent = null;
    }
}