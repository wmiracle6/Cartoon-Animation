using System;
using UnityEngine;

// Символ ":" означает, что этот скрипт ОБЯЗУЕТСЯ выполнять правила интерфейса ITimelineController
public class TimelineControllerStub : MonoBehaviour, ITimelineController
{
    // Эти настройки появятся в Инспекторе Unity, и ты сможешь ими управлять!
    [Header("Настройки времени (Заглушка)")]
    [SerializeField] private bool _isPlaying = true; // Идет ли воспроизведение сейчас?
    [SerializeField] private float _currentTime = 0f;  // Текущая секунда таймлайна

    // --- Реализация интерфейса ITimelineController ---
    // Код ниже просто отдает значения наших переменных туда, где их запрашивают
    public float CurrentTime => _currentTime;
    public bool IsPlaying => _isPlaying;
    public event Action<float> OnTimeChanged;
    // -------------------------------------------------

    void Update()
    {
        // Если в Инспекторе стоит галочка IsPlaying, мы каждую секунду прибавляем время
        if (_isPlaying)
        {
            _currentTime += Time.deltaTime;

            // Вызываем событие изменения времени (если кто-то на него подписан)
            OnTimeChanged?.Invoke(_currentTime);
        }
    }
}