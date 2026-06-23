using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance;

    [Header("Настройки Drag & Drop")]
    public Color dragColor = new Color(1f, 1f, 1f, 0.6f);
    public float dragScale = 1.1f;

    private DragDropItem currentDraggedItem;
    private RectTransform currentDraggedRect;
    private Canvas canvas;
    private CanvasGroup currentCanvasGroup;
    private Vector2 originalDragOffset;
    private RectTransform dropZoneRect;
    private int dropTargetIndex = -1;
    private ScrollRect parentScrollRect;
    private bool isDragging = false;
    private Coroutine dragCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindAnyObjectByType<Canvas>();
    }

    public void StartDrag(DragDropItem item, PointerEventData eventData)
    {
        if (isDragging) return;

        isDragging = true;
        currentDraggedItem = item;
        currentDraggedRect = item.GetComponent<RectTransform>();
        currentCanvasGroup = item.GetComponent<CanvasGroup>();

        // Сохраняем ScrollRect
        parentScrollRect = currentDraggedRect.GetComponentInParent<ScrollRect>();

        // Сохраняем смещение
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        originalDragOffset = (Vector2)currentDraggedRect.localPosition - localPoint;

        // Меняем визуал
        if (currentCanvasGroup != null)
        {
            currentCanvasGroup.blocksRaycasts = false;
            currentCanvasGroup.alpha = dragColor.a;
        }

        currentDraggedRect.localScale = Vector3.one * dragScale;
        currentDraggedRect.SetAsLastSibling();

        // ОТКЛЮЧАЕМ СКРОЛЛ
        if (parentScrollRect != null)
            parentScrollRect.enabled = false;

        dropTargetIndex = -1;
        dropZoneRect = null;
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (!isDragging || currentDraggedRect == null) return;

        // Перемещаем объект
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );
        currentDraggedRect.localPosition = localPoint + originalDragOffset;

        // Проверяем пересечение с другими элементами
        CheckHover(eventData);
    }

    public void EndDrag()
    {
        if (!isDragging) return;
        if (currentDraggedItem == null)
        {
            CleanupDrag();
            return;
        }

        // Возвращаем визуал
        if (currentCanvasGroup != null)
        {
            currentCanvasGroup.blocksRaycasts = true;
            currentCanvasGroup.alpha = 1f;
        }

        if (currentDraggedRect != null)
            currentDraggedRect.localScale = Vector3.one;

        // Если есть цель - перемещаем
        if (dropTargetIndex >= 0 && currentDraggedItem != null)
        {
            int fromIndex = currentDraggedItem.GetCurrentIndex();

            // Корректируем индекс если перетаскиваем вперед
            int targetIndex = dropTargetIndex;
            if (targetIndex > fromIndex)
                targetIndex--;

            // Перемещаем
            if (fromIndex != targetIndex)
                currentDraggedItem.MoveToIndex(targetIndex);
        }

        // Снимаем подсветку
        if (dropZoneRect != null)
        {
            DragDropItem oldTarget = dropZoneRect.GetComponent<DragDropItem>();
            if (oldTarget != null)
                oldTarget.OnHover(false);
            dropZoneRect = null;
        }

        // ВКЛЮЧАЕМ СКРОЛЛ ОБРАТНО
        if (parentScrollRect != null)
        {
            // Небольшая задержка чтобы не мешать Drag
            StartCoroutine(EnableScrollRectDelayed());
        }

        CleanupDrag();
    }

    private IEnumerator EnableScrollRectDelayed()
    {
        yield return new WaitForEndOfFrame();
        if (parentScrollRect != null)
            parentScrollRect.enabled = true;
        parentScrollRect = null;
    }

    private void CleanupDrag()
    {
        isDragging = false;
        dropTargetIndex = -1;
        currentDraggedItem = null;
        currentDraggedRect = null;
        currentCanvasGroup = null;
    }

    private void CheckHover(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DragDropItem targetItem = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastResult result in results)
        {
            DragDropItem item = result.gameObject.GetComponent<DragDropItem>();
            if (item != null && item != currentDraggedItem && item.enableDrag)
            {
                float distance = Vector2.Distance(
                    currentDraggedRect.position,
                    item.GetComponent<RectTransform>().position
                );
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetItem = item;
                }
            }
        }

        // Снимаем подсветку со старой цели
        if (dropZoneRect != null)
        {
            DragDropItem oldTarget = dropZoneRect.GetComponent<DragDropItem>();
            if (oldTarget != null && oldTarget != targetItem)
                oldTarget.OnHover(false);
        }

        // Подсвечиваем новую цель
        if (targetItem != null)
        {
            targetItem.OnHover(true);
            dropZoneRect = targetItem.GetComponent<RectTransform>();
            dropTargetIndex = targetItem.GetCurrentIndex();
        }
        else
        {
            dropZoneRect = null;
            dropTargetIndex = -1;
        }
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}