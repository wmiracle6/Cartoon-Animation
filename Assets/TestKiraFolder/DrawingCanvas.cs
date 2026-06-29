using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DrawingCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Ссылки")]
    public StopMotionWithAudio stopMotionController;
    public Image drawingImage; // Изображение холста (ActiveFrameView)

    [Header("Настройки кисти")]
    public Color brushColor = Color.black;
    public float brushSize = 10f;
    public bool isEraser = false;

    private Texture2D activeTexture;
    private Vector2 lastPixelPos;
    private bool isDrawingActive = false;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (drawingImage == null)
            drawingImage = GetComponent<Image>();

        if (stopMotionController == null)
            stopMotionController = FindAnyObjectByType<StopMotionWithAudio>();
    }

    // Подготовка и проверка текстуры перед рисованием
    private bool EnsureActiveTexture()
    {
        if (stopMotionController == null) return false;

        // Проверяем, есть ли кадры на таймлайне
        if (stopMotionController.timelineFrames.Count == 0) return false;

        int index = stopMotionController.currentFrameIndex;
        var frame = stopMotionController.timelineFrames[index];

        // Если спрайта нет — создаем чистый белый холст 1024x768
        if (frame.frameSprite == null)
        {
            int w = 1024;
            int h = 768;
            Texture2D newTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] whitePixels = new Color[w * h];
            for (int i = 0; i < whitePixels.Length; i++) whitePixels[i] = Color.white;
            newTex.SetPixels(whitePixels);
            newTex.Apply();

            frame.frameSprite = Sprite.Create(newTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            stopMotionController.UpdateUI();
        }

        activeTexture = frame.frameSprite.texture;
        return activeTexture != null;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!EnsureActiveTexture()) return;

        isDrawingActive = true;
        Vector2 pixelPos = GetPixelPosition(eventData);
        DrawDot(pixelPos);
        lastPixelPos = pixelPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawingActive || activeTexture == null) return;

        Vector2 pixelPos = GetPixelPosition(eventData);
        DrawLine(lastPixelPos, pixelPos);
        lastPixelPos = pixelPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDrawingActive)
        {
            isDrawingActive = false;
            // Обновляем UI и миниатюры на таймлайне при завершении штриха
            if (stopMotionController != null)
            {
                stopMotionController.UpdateUI();
            }
        }
    }

    // Конвертация экранных координат клика мыши в пиксели текстуры
    private Vector2 GetPixelPosition(PointerEventData eventData)
    {
        if (rectTransform == null) return Vector2.zero;

        // Конвертируем экранную позицию в локальную точку внутри RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        float rectWidth = rectTransform.rect.width;
        float rectHeight = rectTransform.rect.height;

        // Переводим локальную точку в нормализованные координаты (0..1)
        float normX = (localPoint.x - rectTransform.rect.x) / rectWidth;
        float normY = (localPoint.y - rectTransform.rect.y) / rectHeight;

        // Переводим в координаты пикселей активной текстуры
        float pixelX = normX * activeTexture.width;
        float pixelY = normY * activeTexture.height;

        return new Vector2(pixelX, pixelY);
    }

    // Рисование круглой точки на текстуре
    private void DrawDot(Vector2 pos)
    {
        int centerX = Mathf.RoundToInt(pos.x);
        int centerY = Mathf.RoundToInt(pos.y);
        int radius = Mathf.RoundToInt(brushSize / 2f);

        Color paintColor = isEraser ? Color.white : brushColor;

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int targetX = centerX + x;
                int targetY = centerY + y;

                if (targetX >= 0 && targetX < activeTexture.width && targetY >= 0 && targetY < activeTexture.height)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        activeTexture.SetPixel(targetX, targetY, paintColor);
                    }
                }
            }
        }

        activeTexture.Apply();
    }

    // Интерполяция для рисования сплошных линий при быстром движении мыши
    private void DrawLine(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        if (distance == 0) return;

        float step = Mathf.Max(1f, brushSize / 4f);
        int stepsCount = Mathf.CeilToInt(distance / step);

        for (int i = 0; i <= stepsCount; i++)
        {
            float t = (float)i / stepsCount;
            Vector2 point = Vector2.Lerp(start, end, t);
            DrawDot(point);
        }
    }

    // =========================================================
    // ПАБЛИК МЕТОДЫ ДЛЯ КНОПОК ПАЛИТРЫ (Будут видны в OnClick)
    // =========================================================

    // Универсальный метод установки цвета через HEX-строку (например, #FF0000)
    public void SetBrushColorHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color newColor))
        {
            brushColor = newColor;
            isEraser = false;
        }
        else
        {
            Debug.LogError($"Не удалось распознать цвет: {hex}");
        }
    }

    // Готовые заготовки популярных цветов
    public void SetColorRed() { brushColor = Color.red; isEraser = false; }
    public void SetColorGreen() { brushColor = Color.green; isEraser = false; }
    public void SetColorBlue() { brushColor = Color.blue; isEraser = false; }
    public void SetColorBlack() { brushColor = Color.black; isEraser = false; }
    public void SetColorWhite() { brushColor = Color.white; isEraser = false; }
    public void SetColorYellow() { brushColor = Color.yellow; isEraser = false; }
    public void SetColorPurple() { brushColor = new Color(0.66f, 0.33f, 0.97f); isEraser = false; }
    public void SetColorOrange() { brushColor = new Color(1f, 0.6f, 0f); isEraser = false; }

    public void SetBrushSize(float size)
    {
        brushSize = size;
    }

    public void SetDrawMode()
    {
        isEraser = false;
    }

    public void SetEraseMode()
    {
        isEraser = true;
    }
}