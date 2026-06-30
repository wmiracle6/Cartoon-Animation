using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExportProgressUI : MonoBehaviour
{
    [Header("=== UI ЭЛЕМЕНТЫ ===")]
    public GameObject progressPanel;
    public TMP_Text statusText;

    private void Start()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);
    }

    /// <summary>
    /// Показывает окно прогресса
    /// </summary>
    public void ShowProgress()
    {
        if (progressPanel != null)
            progressPanel.SetActive(true);

        if (statusText != null)
            statusText.text = "Идет экспорт видео...\nПожалуйста, подождите";
    }

    /// <summary>
    /// Скрывает панель
    /// </summary>
    public void HidePanel()
    {
        if (progressPanel != null)
            progressPanel.SetActive(false);
    }

    /// <summary>
    /// Обновляет текст статуса
    /// </summary>
    public void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    /// <summary>
    /// Вызывается когда экспорт завершен
    /// </summary>
    public void OnExportComplete(bool success)
    {
        if (statusText != null)
        {
            if (success)
                statusText.text = "Экспорт завершен!";
            else
                statusText.text = "Ошибка экспорта!";
        }

        // Закрываем окно через 2 секунды
        Invoke(nameof(HidePanel), 2f);
    }

    public void UpdateProcess(Process process)
    {
        // Ничего не делаем, просто заглушка для совместимости
    }
}