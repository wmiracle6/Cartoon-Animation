using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StopMotionManager : MonoBehaviour
{
    [Header("UI элементы просмотра")]
    public Image activeFrameDisplay;
    public Image onionSkinDisplay;

    [Header("Лента кадров (Timeline)")]
    public RectTransform timelineContent; // Перетащите сюда Content Ленты кадров
    public GameObject framePrefab;       // Префаб UI_Frame_Template

    [Header("Библиотека импорта (Library)")]
    public RectTransform libraryContent;  // Перетащите сюда Content Галереи слева

    [Header("Настройки")]
    public Slider fpsSlider;
    public TMP_Text fpsText;
    public Toggle onionToggle;
    public Slider onionOpacitySlider;

    // Внутренние списки
    private List<Sprite> frames = new List<Sprite>();
    private int currentFrameIndex = -1;
    private List<Sprite> importedLibraryImages = new List<Sprite>();

    // Состояние воспроизведения
    private bool isPlaying = false;
    private float playbackTimer = 0f;

    void Start()
    {
        // Настройка дефолтных значений слайдеров
        if (fpsSlider != null)
        {
            fpsSlider.value = 6f;
            fpsSlider.onValueChanged.AddListener(OnFpsChanged);
            UpdateFpsText(fpsSlider.value);
        }

        if (onionOpacitySlider != null)
        {
            onionOpacitySlider.value = 0.5f;
            onionOpacitySlider.onValueChanged.AddListener(OnOnionOpacityChanged);
            UpdateOnionOpacity(onionOpacitySlider.value);
        }

        if (onionToggle != null)
        {
            onionToggle.isOn = true;
            onionToggle.onValueChanged.AddListener(OnOnionToggleChanged);
        }

        UpdatePreview();
    }

    void Update()
    {
        if (isPlaying && frames.Count > 0)
        {
            playbackTimer += Time.deltaTime;
            float interval = 1f / (fpsSlider != null ? fpsSlider.value : 6f);
            if (playbackTimer >= interval)
            {
                playbackTimer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % frames.Count;
                UpdatePreview();
                HighlightTimelineFrame(currentFrameIndex);
            }
        }
    }

    // --- ФУНКЦИЯ ИМПОРТА КАРТИНКИ С ДИСКА ---
    public void ImportImageFromDisk()
    {
        string path = "";

#if UNITY_EDITOR
        // Открывает стандартный проводник OS в редакторе Unity
        path = EditorUtility.OpenFilePanel("Выберите изображение для анимации", "", "png,jpg,jpeg");
#else
        Debug.Log("Импорт файлов в сборке настроен через NativeGallery или системный медиа-проигрыватель.");
#endif

        if (!string.IsNullOrEmpty(path))
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
            {
                Sprite newSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                AddImageToLibrary(newSprite);
            }
        }
    }

    // Добавление в галерею слева
    private void AddImageToLibrary(Sprite sprite)
    {
        importedLibraryImages.Add(sprite);

        // Спавним ячейку в левой галерее
        if (libraryContent != null && framePrefab != null)
        {
            GameObject newItem = Instantiate(framePrefab, libraryContent, false);
            FrameItem itemScript = newItem.GetComponent<FrameItem>();
            if (itemScript != null)
            {
                int index = importedLibraryImages.Count - 1;
                // При клике на фото в библиотеке — оно отправляется на таймлайн!
                itemScript.Setup(sprite, index + 1, false, () => {
                    AddFrameToTimeline(sprite);
                });
            }
        }

        // Также автоматически добавляем эту картинку в таймлайн в качестве нового кадра
        AddFrameToTimeline(sprite);
    }

    // Добавление кадра в ленту снизу
    public void AddFrameToTimeline(Sprite sprite)
    {
        frames.Add(sprite);
        currentFrameIndex = frames.Count - 1;

        RebuildTimelineUI();
        UpdatePreview();
    }

    // Полное обновление UI ленты
    private void RebuildTimelineUI()
    {
        foreach (Transform child in timelineContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < frames.Count; i++)
        {
            GameObject newFrameObj = Instantiate(framePrefab, timelineContent, false);
            newFrameObj.name = "Frame_" + i;

            FrameItem itemScript = newFrameObj.GetComponent<FrameItem>();
            if (itemScript != null)
            {
                int frameIdx = i;
                bool isSelected = (frameIdx == currentFrameIndex);
                itemScript.Setup(frames[frameIdx], frameIdx + 1, isSelected, () => {
                    SelectFrame(frameIdx);
                });
            }
        }
    }

    public void SelectFrame(int index)
    {
        if (index >= 0 && index < frames.Count)
        {
            currentFrameIndex = index;
            isPlaying = false; // Останавливаем воспроизведение при ручном выборе
            UpdatePreview();
            HighlightTimelineFrame(currentFrameIndex);
        }
    }

    private void HighlightTimelineFrame(int index)
    {
        for (int i = 0; i < timelineContent.childCount; i++)
        {
            Transform child = timelineContent.GetChild(i);
            FrameItem itemScript = child.GetComponent<FrameItem>();
            if (itemScript != null)
            {
                itemScript.SetHighlight(i == index);
            }
        }
    }

    // --- КНОПКИ ПАНЕЛИ КАДРОВ ---

    public void TogglePlayback()
    {
        if (frames.Count == 0) return;
        isPlaying = !isPlaying;
        playbackTimer = 0f;
    }

    public void DuplicateActiveFrame()
    {
        if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
        {
            Sprite activeSprite = frames[currentFrameIndex];
            frames.Insert(currentFrameIndex + 1, activeSprite);
            currentFrameIndex++;
            RebuildTimelineUI();
            UpdatePreview();
        }
    }

    public void DeleteActiveFrame()
    {
        if (frames.Count == 0) return;
        if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
        {
            frames.RemoveAt(currentFrameIndex);
            if (currentFrameIndex >= frames.Count)
            {
                currentFrameIndex = frames.Count - 1;
            }
            RebuildTimelineUI();
            UpdatePreview();
        }
    }

    public void ReverseTimeline()
    {
        if (frames.Count < 2) return;
        frames.Reverse();
        currentFrameIndex = 0;
        RebuildTimelineUI();
        UpdatePreview();
    }

    // --- ОБНОВЛЕНИЕ ЭКРАНА И ONION SKIN ---
    private void UpdatePreview()
    {
        if (frames.Count == 0)
        {
            if (activeFrameDisplay != null) activeFrameDisplay.gameObject.SetActive(false);
            if (onionSkinDisplay != null) onionSkinDisplay.gameObject.SetActive(false);
            return;
        }

        if (activeFrameDisplay != null && currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
        {
            activeFrameDisplay.sprite = frames[currentFrameIndex];
            activeFrameDisplay.gameObject.SetActive(true);
        }

        if (onionSkinDisplay != null)
        {
            bool showOnion = onionToggle != null ? onionToggle.isOn : true;
            int prevIdx = currentFrameIndex - 1;

            if (showOnion && prevIdx >= 0 && prevIdx < frames.Count)
            {
                onionSkinDisplay.sprite = frames[prevIdx];
                onionSkinDisplay.gameObject.SetActive(true);
            }
            else
            {
                onionSkinDisplay.gameObject.SetActive(false);
            }
        }
    }

    // --- НАСТРОЙКИ СЛАЙДЕРОВ ---
    private void OnFpsChanged(float value)
    {
        UpdateFpsText(value);
    }

    private void UpdateFpsText(float value)
    {
        if (fpsText != null)
        {
            fpsText.text = Mathf.RoundToInt(value) + " кадр/сек";
        }
    }

    private void OnOnionToggleChanged(bool isOn)
    {
        UpdatePreview();
    }

    private void OnOnionOpacityChanged(float value)
    {
        UpdateOnionOpacity(value);
    }

    private void UpdateOnionOpacity(float value)
    {
        if (onionSkinDisplay != null)
        {
            Color col = onionSkinDisplay.color;
            col.a = value;
            onionSkinDisplay.color = col;
        }
    }
}