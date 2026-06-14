using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FrameItem : MonoBehaviour
{
    [Header("Элементы UI кадра")]
    public Image thumbnailDisplay;       // Ваше ThumbnailImage (Image)
    public TMP_Text frameNumberText;    // Ваше IndexText (TextMeshPro)
    public Image outlineHighlight;      // Фоновая рамка выделения (по желанию, необязательно)

    private Action onClickAction;

    public void Setup(Sprite sprite, int number, bool isSelected, Action clickAction)
    {
        if (thumbnailDisplay != null)
        {
            thumbnailDisplay.sprite = sprite;
        }

        if (frameNumberText != null)
        {
            frameNumberText.text = number.ToString();
        }

        onClickAction = clickAction;

        SetHighlight(isSelected);

        // Автоматически подвязываем клик, если на префабе есть компонент Button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnItemClicked);
        }
    }

    public void SetHighlight(bool isSelected)
    {
        if (outlineHighlight != null)
        {
            outlineHighlight.gameObject.SetActive(isSelected);
        }
    }

    private void OnItemClicked()
    {
        if (onClickAction != null)
        {
            onClickAction.Invoke();
        }
    }
}