using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Media;
#endif

[System.Serializable]
public class FrameData
{
    public Sprite frameSprite;
    public float duration = 1f;
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

    private List<FrameData> timelineFrames = new List<FrameData>();
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private float fps = 6f;

    private Coroutine playCoroutine;

    private void Start()
    {
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

        if (playButton != null) playButton.onClick.AddListener(TogglePlayback);
        if (duplicateButton != null) duplicateButton.onClick.AddListener(DuplicateCurrentFrame);
        if (deleteButton != null) deleteButton.onClick.AddListener(DeleteCurrentFrame);
        if (reverseButton != null) reverseButton.onClick.AddListener(ReverseTimeline);
        if (importButton != null) importButton.onClick.AddListener(ImportImages);
        if (exportButton != null) exportButton.onClick.AddListener(ExportAnimation);

        CreateInitialScene();
        UpdateUI();
    }

    private void CreateInitialScene()
    {
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

    public void SelectFrame(int index)
    {
        if (isPlaying) StopAnimation();
        if (index >= 0 && index < timelineFrames.Count)
        {
            currentFrameIndex = index;
            UpdateUI();
        }
    }

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

    public void ReverseTimeline()
    {
        if (timelineFrames.Count <= 1) return;
        if (isPlaying) StopAnimation();

        timelineFrames.Reverse();
        currentFrameIndex = 0;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (timelineFrames.Count == 0) return;

        FrameData activeFrame = timelineFrames[currentFrameIndex];

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
                activeFrameDisplay.color = Color.white;
            }
        }

        UpdateOnionSkin();
        RedrawTimeline();
        UpdateStatistics();
    }

    private void UpdateOnionSkin()
    {
        if (onionSkinDisplay == null) return;

        bool showOnion = onionToggle != null && onionToggle.isOn;

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

        onionSkinDisplay.gameObject.SetActive(false);
    }

    private void RedrawTimeline()
    {
        if (timelineContent == null || framePrefab == null) return;

        // Отсоединяем элементы, чтобы разметка UI прекратила их учитывать мгновенно
        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in timelineContent)
        {
            childrenToDestroy.Add(child);
        }

        foreach (Transform child in childrenToDestroy)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        // Спавним префабы
        for (int i = 0; i < timelineFrames.Count; i++)
        {
            GameObject frameObj = Instantiate(framePrefab, timelineContent);

            // Защита от багов масштабирования UI и отключенных префабов:
            frameObj.SetActive(true);
            RectTransform rectTrans = frameObj.GetComponent<RectTransform>();
            if (rectTrans != null)
            {
                rectTrans.localScale = Vector3.one;       // Принудительно ставим масштаб 1:1
                rectTrans.anchoredPosition3D = Vector3.zero; // Сбрасываем позицию по Z в плоскость UI
            }

            FrameItem itemComponent = frameObj.GetComponent<FrameItem>();
            if (itemComponent != null)
            {
                bool isSelected = (i == currentFrameIndex);
                itemComponent.Setup(i, timelineFrames[i].frameSprite, isSelected, SelectFrame);
            }
        }

        // Заставляем Canvas немедленно пересчитать размеры внутри ScrollView
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(timelineContent);
    }

    private void UpdateStatistics()
    {
        if (totalFramesText != null)
        {
            totalFramesText.text = $"{timelineFrames.Count} кадров";
        }

        if (movieLengthText != null)
        {
            float lengthInSeconds = (float)timelineFrames.Count / fps;
            movieLengthText.text = $"{lengthInSeconds:F2} сек";
        }
    }

    public void ImportImages()
    {
        if (isPlaying) StopAnimation();

#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Выберите картинку для импорта", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            LoadImageFromPath(path);
        }
#else
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        Debug.Log($"Путь для импорта по умолчанию: {desktopPath}");
#endif
    }

    private void LoadImageFromPath(string path)
    {
        if (!File.Exists(path)) return;

        byte[] fileData = File.ReadAllBytes(path);

        // Создаем текстуру С поддержкой Mip-карт (четвертый параметр = true)
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);

        if (texture.LoadImage(fileData))
        {
            // Настраиваем максимальное качество сглаживания
            texture.filterMode = FilterMode.Trilinear; // Плавная фильтрация между Mip-уровнями
            texture.anisoLevel = 8;                    // Анизотропная фильтрация для четкости деталей
            texture.Apply(true, false);                // Принудительно генерируем Mip-карты

            Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

            // Принудительно заставляем экраны сохранять пропорции исходного фото, чтобы избежать растягивания
            if (activeFrameDisplay != null) activeFrameDisplay.preserveAspect = true;
            if (onionSkinDisplay != null) onionSkinDisplay.preserveAspect = true;

            if (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null)
            {
                timelineFrames[0].frameSprite = newSprite;
            }
            else
            {
                FrameData newFrame = new FrameData { frameSprite = newSprite };
                timelineFrames.Add(newFrame);
                currentFrameIndex = timelineFrames.Count - 1;
            }

            UpdateUI();
        }
    }

    public void ExportAnimation()
    {
        if (timelineFrames.Count == 0 || (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null))
        {
            Debug.LogError("Нечего экспортировать!");
            return;
        }

        if (isPlaying) StopAnimation();

#if UNITY_EDITOR
        string targetPath = EditorUtility.SaveFilePanel("Сохранить видео как MP4", "", "StopMotionMovie", "mp4");
        if (!string.IsNullOrEmpty(targetPath))
        {
            StartCoroutine(ExportMP4Coroutine(targetPath));
        }
#else
        string targetFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "StopMotionFilm_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(targetFolder);
        StartCoroutine(ExportFramesCoroutine(targetFolder));
#endif
    }

#if UNITY_EDITOR
    private IEnumerator ExportMP4Coroutine(string filePath)
    {
        Debug.Log($"Начало экспорта MP4 в файл: {filePath}");

        int width = 1024;
        int height = 768;
        foreach (var frame in timelineFrames)
        {
            if (frame.frameSprite != null)
            {
                width = frame.frameSprite.texture.width;
                height = frame.frameSprite.texture.height;
                break;
            }
        }

        if (width % 2 != 0) width++;
        if (height % 2 != 0) height++;

        VideoTrackAttributes videoAttr = new VideoTrackAttributes
        {
            frameRate = new MediaRational((int)fps),
            width = (uint)width,
            height = (uint)height,
            bitRateMode = VideoBitrateMode.High //нужна ли она тут хз
        };

        using (MediaEncoder encoder = new MediaEncoder(filePath, videoAttr))
        {
            for (int i = 0; i < timelineFrames.Count; i++)
            {
                Sprite sprite = timelineFrames[i].frameSprite;
                if (sprite != null)
                {
                    Texture2D tex = sprite.texture;
                    Texture2D readableTex = CreateReadableTexture(tex, width, height);

                    encoder.AddFrame(readableTex);
                    Destroy(readableTex);
                }
                yield return null;
            }
        }

        Debug.Log($"Экспорт MP4 успешно завершен!");
        EditorUtility.RevealInFinder(filePath);
    }

    private Texture2D CreateReadableTexture(Texture2D source, int width, int height)
    {
        // RenderTextureFormat.ARGB32 гарантирует отсутствие цветовых искажений и потерь при сжатии
        RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        source.filterMode = FilterMode.Bilinear;
        tempRT.filterMode = FilterMode.Bilinear;

        Graphics.Blit(source, tempRT);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = tempRT;

        Texture2D readableTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(tempRT);

        return readableTex;
    }
#endif

    private IEnumerator ExportFramesCoroutine(string folderPath)
    {
        Debug.Log($"Режим сборки: Экспорт кадров в папку: {folderPath}");

        for (int i = 0; i < timelineFrames.Count; i++)
        {
            Sprite sprite = timelineFrames[i].frameSprite;
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;
                Texture2D readableTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);

                RenderTexture tempRT = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(tex, tempRT);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = tempRT;

                readableTex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
                readableTex.Apply();

                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(tempRT);

                byte[] bytes = readableTex.EncodeToPNG();
                Destroy(readableTex);

                string fileName = $"kadr_{i + 1:D3}.png";
                string fullPath = Path.Combine(folderPath, fileName);

                File.WriteAllBytes(fullPath, bytes);
            }
            yield return null;
        }

        string readmePath = Path.Combine(folderPath, "readme.txt");
        string readmeContent = $"Экспортировано кадров: {timelineFrames.Count}\n" +
                               $"Выбранный FPS: {fps}\n" +
                               $"Для склейки используйте: ffmpeg -r {fps} -i kadr_%03d.png -c:v libx264 -pix_fmt yuv420p video.mp4";

        File.WriteAllText(readmePath, readmeContent);
        Debug.Log($"Сохранение последовательности завершено!");
    }
}