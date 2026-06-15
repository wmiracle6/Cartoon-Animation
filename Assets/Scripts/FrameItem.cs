using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FrameItem : MonoBehaviour
{
    [Header("Ёлементы UI кадра")]
    [Tooltip("—сылка на изображение миниатюры")]
    public Image thumbnailDisplay;

    [Tooltip("—сылка на текстовое поле с номером кадра")]
    public TMP_Text frameNumberText;

    [Tooltip("–амочка выделени€ кадра")]
    public Image outlineHighlight;

    private Action<int> onClickCallback;
    private int myIndex;

    public void Setup(int index, Sprite sprite, bool isSelected, Action<int> clickCallback)
    {
        myIndex = index;
        onClickCallback = clickCallback;

        if (frameNumberText != null)
        {
            frameNumberText.text = (index + 1).ToString();
        }

        if (thumbnailDisplay != null)
        {
            // ¬ключаем сохранение пропорций дл€ миниатюры
            thumbnailDisplay.preserveAspect = true;

            if (sprite != null)
            {
                thumbnailDisplay.sprite = sprite;
                thumbnailDisplay.color = Color.white;
            }
            else
            {
                thumbnailDisplay.sprite = null;
                thumbnailDisplay.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            }
        }

        if (outlineHighlight != null)
        {
            outlineHighlight.enabled = isSelected;
        }

        Button btn = GetComponent<Button>();
        if (btn == null)
        {
            btn = gameObject.AddComponent<Button>();
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnItemClicked);
    }

    // Ётот метод вызываетс€ при клике на элемент кадра в ленте
    private void OnItemClicked()
    {
        onClickCallback?.Invoke(myIndex);
    }
}