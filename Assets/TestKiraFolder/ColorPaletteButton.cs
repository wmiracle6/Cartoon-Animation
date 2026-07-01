using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ColorPaletteButton : MonoBehaviour
{
    [Header("=== КНОПКА ЦВЕТА ПАЛИТРЫ ===")]
    public DrawingCanvas drawingCanvas;

    [Tooltip("Выберите цвет этой кнопки")]
    public Color color = Color.black;

    private void Start()
    {
        // Если ссылка на рисовалку пустая, найдем ее автоматически
        if (drawingCanvas == null)
        {
            drawingCanvas = FindAnyObjectByType<DrawingCanvas>();
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            // Очищаем и добавляем слушатель автоматически
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnPaletteClick);
        }
    }

    private void OnPaletteClick()
    {
        if (drawingCanvas != null)
        {
            drawingCanvas.brushColor = color;
            drawingCanvas.isEraser = false; // Отключаем ластик, включаем рисование выбранным цветом
            Debug.Log($"Цвет кисти изменен на: {color}");
        }
        else
        {
            Debug.LogError("Скрипт DrawingCanvas не найден! Убедитесь, что он висит на ActiveFrameView.");
        }
    }
}