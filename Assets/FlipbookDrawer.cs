using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Этот скрипт вешается на UI элемент RawImage,
// на котором мы будем рисовать пальцем или мышкой.
public class FlipbookDrawer : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Настройки рисования")]
    public Color brushColor = Color.purple;
    public int brushSize = 8;
    public bool isEraser = false;

    private RawImage rawImage;
    private Texture2D drawTexture;
    private RectTransform rectTransform;

    private int textureWidth = 600;
    private int textureHeight = 450;
    private Vector2 lastCoords;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();

        // Создаем чистый кадр, если он еще не назначен
        if (rawImage.texture == null)
        {
            CreateNewEmptyTexture();
        }
        else
        {
            drawTexture = (Texture2D)rawImage.texture;
        }
    }

    // Создает белую пустую текстуру
    public void CreateNewEmptyTexture()
    {
        drawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        ClearTexture();
        rawImage.texture = drawTexture;
    }

    // Устанавливает текстуру из менеджера кадров
    public void SetActiveTexture(Texture2D newTexture)
    {
        drawTexture = newTexture;
        rawImage.texture = drawTexture;
    }

    // Заливает текстуру белым цветом
    public void ClearTexture()
    {
        Color[] whitePixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < whitePixels.Length; i++)
        {
            whitePixels[i] = Color.white;
        }
        drawTexture.SetPixels(whitePixels);
        drawTexture.Apply();
    }

    // Клик по экрану (начало рисования)
    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 localCursor;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
        {
            Vector2 pixelCoords = LocalToPixelCoords(localCursor);
            DrawCircle(pixelCoords, isEraser ? Color.white : brushColor, brushSize);
            lastCoords = pixelCoords;
        }
    }

    // Ведение пальцем или мышкой
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localCursor;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
        {
            Vector2 pixelCoords = LocalToPixelCoords(localCursor);

            // Соединяем точки линией, чтобы рисунок не прерывался при быстром движении
            DrawLine(lastCoords, pixelCoords, isEraser ? Color.white : brushColor, brushSize);
            lastCoords = pixelCoords;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Палец отпущен, можно обновить миниатюру в списке кадров
    }

    // Перевод координат UI в координаты пикселей текстуры
    private Vector2 LocalToPixelCoords(Vector2 localCursor)
    {
        float xNorm = (localCursor.x - rectTransform.rect.x) / rectTransform.rect.width;
        float yNorm = (localCursor.y - rectTransform.rect.y) / rectTransform.rect.height;

        int x = Mathf.Clamp((int)(xNorm * textureWidth), 0, textureWidth - 1);
        int y = Mathf.Clamp((int)(yNorm * textureHeight), 0, textureHeight - 1);

        return new Vector2(x, y);
    }

    // Рисование круглой кисти
    private void DrawCircle(Vector2 center, Color col, int r)
    {
        int cx = (int)center.x;
        int cy = (int)center.y;

        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    {
                        drawTexture.SetPixel(x, y, col);
                    }
                }
            }
        }
        drawTexture.Apply();
    }

    // Рисование непрерывной линии между точками
    private void DrawLine(Vector2 start, Vector2 end, Color col, int size)
    {
        float distance = Vector2.Distance(start, end);
        Vector2 direction = (end - start).normalized;

        for (float i = 0; i < distance; i += 1.0f)
        {
            Vector2 currentPoint = start + direction * i;
            DrawCircle(currentPoint, col, size);
        }
        DrawCircle(end, col, size); // Дорисовываем финальную точку
    }
}