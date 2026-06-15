using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FrameItem : MonoBehaviour
{
    [Header("Элементы UI кадра")]
    [Tooltip("Ссылка на изображение миниатюры")]
    public Image thumbnailDisplay;

    [Tooltip("Ссылка на текстовое поле с номером кадра")]
    public TMP_Text frameNumberText;

    [Tooltip("Рамочка выделения кадра")]
    public Image outlineHighlight;

    // Событие клика на кадр для передачи его индекса менеджеру
    private Action<int> onClickCallback;
    private int myIndex;

    /// <summary>
    /// Инициализация элемента кадра в ленте
    /// </summary>
    public void Setup(int index, Sprite sprite, bool isSelected, Action<int> clickCallback)
    {
        myIndex = index;
        onClickCallback = clickCallback;

        // Устанавливаем номер кадра (делаем человеческий индекс с 1)
        if (frameNumberText != null)
        {
            frameNumberText.text = (index + 1).ToString();
        }

        // Настраиваем миниатюру
        if (thumbnailDisplay != null)
        {
            if (sprite != null)
            {
                thumbnailDisplay.sprite = sprite;
                thumbnailDisplay.color = Color.white; // Возвращаем полную видимость
            }
            else
            {
                thumbnailDisplay.sprite = null;
                thumbnailDisplay.color = new Color(0.7f, 0.7f, 0.7f, 0.5f); // Серый цвет, если пусто
            }
        }

        // Показываем или скрываем рамочку выделения активного кадра
        if (outlineHighlight != null)
        {
            outlineHighlight.enabled = isSelected;
        }

        // Добавляем или настраиваем кнопку клика
        Button btn = GetComponent<Button>();
        if (btn == null)
        {
            btn = gameObject.AddComponent<Button>();
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnItemClicked);
    }

    private void OnItemClicked()
    {
        onClickCallback?.Invoke(myIndex);
    }
}