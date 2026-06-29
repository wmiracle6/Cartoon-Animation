using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SimpleTabManager : MonoBehaviour
{
    [Header("=== НАСТРОЙКА ===")]
    public List<GameObject> windows = new List<GameObject>();  // Окна (панели)
    public List<Button> buttons = new List<Button>();          // Кнопки для окон
    public int defaultTab = 0;                                  // Какая вкладка открыта по умолчанию

    [Header("=== ЦВЕТА ТЕКСТА КНОПОК ===")]
    public Color normalTextColor = Color.white;    // Цвет текста когда кнопка НЕ выбрана
    public Color selectedTextColor = Color.yellow; // Цвет текста когда кнопка ВЫБРАНА

    private int currentTab = -1;
    private List<TMP_Text> buttonTexts = new List<TMP_Text>(); // Список текстов кнопок

    private void Start()
    {
        // Если кнопки не назначены вручную - ищем их в дочерних объектах
        if (buttons.Count == 0)
        {
            foreach (Transform child in transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null)
                    buttons.Add(btn);
            }
        }

        // Сохраняем ссылки на тексты кнопок
        foreach (Button btn in buttons)
        {
            if (btn != null)
            {
                TMP_Text text = btn.GetComponentInChildren<TMP_Text>();
                buttonTexts.Add(text);
            }
        }

        // Привязываем кнопки к окнам
        int count = Mathf.Min(windows.Count, buttons.Count);
        for (int i = 0; i < count; i++)
        {
            int index = i; // Замыкание
            buttons[i].onClick.AddListener(() => SwitchTab(index));
        }

        // Открываем вкладку по умолчанию
        SwitchTab(defaultTab);
    }

    public void SwitchTab(int index)
    {
        if (index < 0 || index >= windows.Count) return;
        if (currentTab == index) return;

        // Закрываем все окна
        foreach (GameObject window in windows)
        {
            if (window != null)
                window.SetActive(false);
        }

        // Открываем нужное
        if (windows[index] != null)
            windows[index].SetActive(true);

        // Обновляем кнопки и их тексты
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] != null)
            {
                // Кнопка активна (кликабельна) если НЕ выбрана
                buttons[i].interactable = (i != index);
            }

            // Меняем цвет текста
            if (i < buttonTexts.Count && buttonTexts[i] != null)
            {
                if (i == index)
                    buttonTexts[i].color = selectedTextColor;
                else
                    buttonTexts[i].color = normalTextColor;
            }
        }

        currentTab = index;
    }
}