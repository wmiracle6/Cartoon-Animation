using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class FrameData
{
    public Sprite frameSprite;
    public float duration = 1f; // Множитель времени показа кадра
}

public class StopMotionManager : MonoBehaviour
{
    [Header("UI элементы просмотра")]
    public Image activeFrameDisplay;
    public Image onionSkinDisplay;

    [Header("Лента кадров (Timeline)")]
    public RectTransform timelineContent;
    public GameObject framePrefab;

    [Header("Настройки")]
    public Slider fpsSlider;
    public TMP_Text fpsText;
    public Toggle onionToggle;
    public Slider onionOpacitySlider;

    [Header("Статистика фильма")]
    [Tooltip("Текст для показа общего числа кадров")]
    public TMP_Text totalFramesText;
    [Tooltip("Текст для показа общей длины анимации в секундах")]
    public TMP_Text movieLengthText;

    [Header("Кнопки управления")]
    public Button playButton;
    public Button duplicateButton;
    public Button deleteButton;
    public Button reverseButton;
    public Button importButton;
    public Button exportButton;

    // Список кадров нашей анимации
    private List<FrameData> timelineFrames = new List<FrameData>();
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private float fps = 6f;

    private Coroutine playCoroutine;

    private void Start()
    {
        // Инициализация ползунков и слушателей UI
        if (fpsSlider != null)
        {
            fpsSlider.minValue = 1f;
            fpsSlider.maxValue = 30f;
            fpsSlider.value = fps;
            fpsSlider.onValueChanged.AddListener(OnFpsChanged);
            UpdateFpsText();
        }

        if (onionToggle != null)
        {
            onionToggle.onValueChanged.AddListener(OnOnionToggleChanged);
        }

        if (onionOpacitySlider != null)
        {
            onionOpacitySlider.minValue = 0f;
            onionOpacitySlider.maxValue = 1f;
            onionOpacitySlider.value = 0.35f;
            onionOpacitySlider.onValueChanged.AddListener(OnOnionOpacityChanged);
        }

        // Подключаем слушатели для кнопок
        if (playButton != null) playButton.onClick.AddListener(TogglePlayback);
        if (duplicateButton != null) duplicateButton.onClick.AddListener(DuplicateCurrentFrame);
        if (deleteButton != null) deleteButton.onClick.AddListener(DeleteCurrentFrame);
        if (reverseButton != null) reverseButton.onClick.AddListener(ReverseTimeline);
        if (importButton != null) importButton.onClick.AddListener(ImportImages);
        if (exportButton != null) exportButton.onClick.AddListener(ExportAnimation);

        // Создаем стартовую пустышку, чтобы экран не был совсем серым
        CreateInitialScene();
        UpdateUI();
    }

    private void CreateInitialScene()
    {
        // Добавляем один стартовый пустой кадр
        FrameData initialFrame = new FrameData { frameSprite = null };
        timelineFrames.Add(initialFrame);
        currentFrameIndex = 0;
    }

    private void OnFpsChanged(float value)
    {
        fps = Mathf.Round(value);
        UpdateFpsText();
        UpdateStatistics();
    }

    private void UpdateFpsText()
    {
        if (fpsText != null)
        {
            fpsText.text = $"{fps} кадр/сек";
        }
    }

    private void OnOnionToggleChanged(bool value)
    {
        UpdateOnionSkin();
    }

    private void OnOnionOpacityChanged(float value)
    {
        UpdateOnionSkin();
    }

    /// <summary>
    /// Переключение воспроизведения Анимации
    /// </summary>
    public void TogglePlayback()
    {
        if (isPlaying)
        {
            StopAnimation();
        }
        else
        {
            StartAnimation();
        }
    }

    private void StartAnimation()
    {
        if (timelineFrames.Count == 0) return;
        isPlaying = true;
        if (playButton != null)
        {
            TMP_Text btnText = playButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Пауза";
        }
        playCoroutine = StartCoroutine(PlayAnimationLoop());
    }

    private void StopAnimation()
    {
        isPlaying = false;
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
        }
        if (playButton != null)
        {
            TMP_Text btnText = playButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = "Запуск";
        }
        UpdateUI();
    }

    private IEnumerator PlayAnimationLoop()
    {
        while (isPlaying && timelineFrames.Count > 0)
        {
            currentFrameIndex = (currentFrameIndex + 1) % timelineFrames.Count;
            UpdateUI();
            yield return new WaitForSeconds(1f / fps);
        }
    }

    /// <summary>
    /// Выбор конкретного кадра
    /// </summary>
    public void SelectFrame(int index)
    {
        if (isPlaying) StopAnimation();
        if (index >= 0 && index < timelineFrames.Count)
        {
            currentFrameIndex = index;
            UpdateUI();
        }
    }

    /// <summary>
    /// Дублирование текущего кадра
    /// </summary>
    public void DuplicateCurrentFrame()
    {
        if (timelineFrames.Count == 0 || currentFrameIndex < 0) return;
        if (isPlaying) StopAnimation();

        FrameData current = timelineFrames[currentFrameIndex];
        FrameData copy = new FrameData
        {
            frameSprite = current.frameSprite,
            duration = current.duration
        };

        timelineFrames.Insert(currentFrameIndex + 1, copy);
        currentFrameIndex++;
        UpdateUI();
    }

    /// <summary>
    /// Удаление выбранного кадра
    /// </summary>
    public void DeleteCurrentFrame()
    {
        if (timelineFrames.Count <= 1)
        {
            Debug.LogWarning("Нельзя удалить единственный кадр!");
            return;
        }
        if (isPlaying) StopAnimation();

        timelineFrames.RemoveAt(currentFrameIndex);
        if (currentFrameIndex >= timelineFrames.Count)
        {
            currentFrameIndex = timelineFrames.Count - 1;
        }
        UpdateUI();
    }

    /// <summary>
    /// Реверс всей ленты кадров задом наперед
    /// </summary>
    public void ReverseTimeline()
    {
        if (timelineFrames.Count <= 1) return;
        if (isPlaying) StopAnimation();

        timelineFrames.Reverse();
        currentFrameIndex = 0;
        UpdateUI();
    }

    /// <summary>
    /// Обновление визуального представления интерфейса
    /// </summary>
    public void UpdateUI()
    {
        if (timelineFrames.Count == 0) return;

        FrameData activeFrame = timelineFrames[currentFrameIndex];

        // Отображение активного кадра на холсте
        if (activeFrameDisplay != null)
        {
            if (activeFrame.frameSprite != null)
            {
                activeFrameDisplay.sprite = activeFrame.frameSprite;
                activeFrameDisplay.color = Color.white;
            }
            else
            {
                activeFrameDisplay.sprite = null;
                activeFrameDisplay.color = Color.white; // Белое полотно-черновик
            }
        }

        UpdateOnionSkin();
        RedrawTimeline();
        UpdateStatistics();
    }

    /// <summary>
    /// Расчет Onion Skin (эффект прозрачности подложки для прошлого кадра)
    /// </summary>
    private void UpdateOnionSkin()
    {
        if (onionSkinDisplay == null) return;

        bool showOnion = onionToggle != null && onionToggle.isOn;

        // Показываем подложку только если это включено в UI, проект на паузе и есть прошлый кадр
        if (showOnion && !isPlaying && currentFrameIndex > 0)
        {
            FrameData prevFrame = timelineFrames[currentFrameIndex - 1];
            if (prevFrame.frameSprite != null)
            {
                onionSkinDisplay.sprite = prevFrame.frameSprite;
                float opacity = onionOpacitySlider != null ? onionOpacitySlider.value : 0.35f;
                onionSkinDisplay.color = new Color(1f, 1f, 1f, opacity);
                onionSkinDisplay.gameObject.SetActive(true);
                return;
            }
        }

        // Если условий нет — выключаем отображение onion skin
        onionSkinDisplay.gameObject.SetActive(false);
    }

    /// <summary>
    /// Полная перерисовка нижнего таймлайна (ленты кадров)
    /// </summary>
    private void RedrawTimeline()
    {
        if (timelineContent == null || framePrefab == null) return;

        // Чистим старые объекты в ленте
        foreach (Transform child in timelineContent)
        {
            Destroy(child.gameObject);
        }

        // Спавним префабы для выстраивания ленты
        for (int i = 0; i < timelineFrames.Count; i++)
        {
            GameObject frameObj = Instantiate(framePrefab, timelineContent);
            FrameItem itemComponent = frameObj.GetComponent<FrameItem>();

            if (itemComponent != null)
            {
                bool isSelected = (i == currentFrameIndex);
                itemComponent.Setup(i, timelineFrames[i].frameSprite, isSelected, SelectFrame);
            }
        }
    }

    /// <summary>
    /// Перерасчет статистики по фильму
    /// </summary>
    private void UpdateStatistics()
    {
        // 1. Обновляем количество кадров
        if (totalFramesText != null)
        {
            totalFramesText.text = $"{timelineFrames.Count} кадров";
        }

        // 2. Рассчитываем длину фильма
        if (movieLengthText != null)
        {
            float lengthInSeconds = (float)timelineFrames.Count / fps;
            movieLengthText.text = $"{lengthInSeconds:F2} сек";
        }
    }

    /// <summary>
    /// Функция Импорта картинок из проводника на ПК
    /// </summary>
    public void ImportImages()
    {
        if (isPlaying) StopAnimation();

#if UNITY_EDITOR
        // Запускаем нативный диалог открытия файлов в Unity Editor
        string path = EditorUtility.OpenFilePanel("Выберите картинку для импорта", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            LoadImageFromPath(path);
        }
#else
        // В Standalone билдах мы можем загружать картинку из папки проекта / С рабочего стола
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        Debug.Log($"Ожидаем файлы в папке: {desktopPath}. В билде рекомендуется использовать StandaloneFileBrowser.");
#endif
    }

    private void LoadImageFromPath(string path)
    {
        if (!File.Exists(path)) return;

        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            // Если у нас в ленте был всего один пустой стартовый кадр, заменим его
            if (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null)
            {
                timelineFrames[0].frameSprite = newSprite;
            }
            else
            {
                // Иначе добавляем новый кадр в конец очереди
                FrameData newFrame = new FrameData { frameSprite = newSprite };
                timelineFrames.Add(newFrame);
                currentFrameIndex = timelineFrames.Count - 1;
            }

            UpdateUI();
        }
    }

    /// <summary>
    /// Функция ЭКСПОРТА фильма! Открывает проводник для сохранения PNG-последовательности кадров.
    /// Это надежный, классический способ для стоп-моушна, позволяющий получить высокое качество.
    /// </summary>
    public void ExportAnimation()
    {
        if (timelineFrames.Count == 0 || (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null))
        {
            Debug.LogError("Нечего экспортировать! Добавьте кадры в ленту.");
            return;
        }

        if (isPlaying) StopAnimation();

        string targetFolder = "";

#if UNITY_EDITOR
        // Открываем проводник для выбора папки, куда сохранить кадры
        targetFolder = EditorUtility.SaveFolderPanel("Выберите папку для сохранения фильма (Кадров)", "", "StopMotionFilm");
#else
        // В собранной игре сохраняем в папку документов пользователя
        targetFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "StopMotionFilm_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(targetFolder);
#endif

        if (!string.IsNullOrEmpty(targetFolder))
        {
            StartCoroutine(ExportFramesCoroutine(targetFolder));
        }
    }

    private IEnumerator ExportFramesCoroutine(string folderPath)
    {
        Debug.Log($"Начало экспорта кадров в: {folderPath}");

        for (int i = 0; i < timelineFrames.Count; i++)
        {
            Sprite sprite = timelineFrames[i].frameSprite;
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;

                // Создаем временную текстуру для чтения/записи, чтобы избежать ошибок Read/Write Enabled на импортированных картинках
                Texture2D readableTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);

                // Чтобы скопировать пиксели, активируем рендер-текстуру
                RenderTexture tempRT = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(tex, tempRT);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = tempRT;

                readableTex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
                readableTex.Apply();

                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(tempRT);

                // Кодируем в PNG
                byte[] bytes = readableTex.EncodeToPNG();
                Destroy(readableTex);

                // Имя файла с красивым индексом (например kadr_001.png, kadr_002.png)
                string fileName = $"kadr_{i + 1:D3}.png";
                string fullPath = Path.Combine(folderPath, fileName);

                File.WriteAllBytes(fullPath, bytes);
            }
            yield return null; // Предотвращаем зависание Unity при экспорте сотен кадров
        }

        // Сохраняем текстовый файлик-инструкцию, как склеить в MP4 с помощью бесплатной утилиты ffmpeg
        string readmePath = Path.Combine(folderPath, "инструкция_по_склейке.txt");
        string readmeContent = $"Ваша стоп-моушн анимация успешно экспортирована!\n" +
                               $"Всего кадров: {timelineFrames.Count} шт.\n" +
                               $"Выбранный FPS: {fps} кадров в сек.\n" +
                               $"Длина ролика: {(float)timelineFrames.Count / fps:F2} сек.\n\n" +
                               $"Вы можете соединить эти кадры в красивое Full-HD MP4 видео с помощью любой бесплатной монтажной программы (CapCut, Premiere Pro, Vegas, DaVinci) или использовать одну команду FFMPEG:\n" +
                               $"ffmpeg -r {fps} -i kadr_%03d.png -c:v libx264 -pix_fmt yuv420p video.mp4";

        File.WriteAllText(readmePath, readmeContent);

        Debug.Log($"Экспорт успешно завершен! Создано {timelineFrames.Count} файлов кадра в папке: {folderPath}");

#if UNITY_EDITOR
        EditorUtility.RevealInFinder(folderPath); // Фокусируем проводник на папке с файлами!
#endif
    }
}