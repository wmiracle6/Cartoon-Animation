using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using System;
using Process = System.Diagnostics.Process;

#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class StopMotionAudioData
{
    public AudioClip clip;
    public string fileName;
    public float duration;
    public float trimStart = 0f;
    public float trimEnd = 0f;
}

public class StopMotionWithAudio : MonoBehaviour
{
    [Header("=== ОСНОВНЫЕ UI ЭЛЕМЕНТЫ ===")]
    public Image activeFrameDisplay;
    public Image onionSkinDisplay;
    public RectTransform timelineContent;
    public GameObject framePrefab;

    [Header("=== УПРАВЛЕНИЕ ВОСПРОИЗВЕДЕНИЕМ ===")]
    public Slider fpsSlider;
    public TMP_Text fpsText;
    public Button playButton;

    [Header("=== ЛУКОВАЯ КОЖИЦА ===")]
    public Toggle onionToggle;
    public Slider onionOpacitySlider;

    [Header("=== КНОПКИ ДЕЙСТВИЙ ===")]
    public Button duplicateButton;
    public Button deleteButton;
    public Button reverseButton;
    public Button importImageButton;
    public Button importFolderButton;
    public Button exportButton;
    public Button createEmptyCanvasButton;

    [Header("=== ПОВОРОТ ИЗОБРАЖЕНИЙ ===")]
    public Button rotateCurrentButton;
    public Button rotateAllButton;

    [Header("=== АУДИО ДОРОЖКА ===")]
    public Button importAudioButton;
    public Button removeAudioButton;
    public TMP_Text audioStatusText;
    public GameObject audioPanel;
    public Slider audioVolumeSlider;

    [Header("=== СТАТИСТИКА ===")]
    public TMP_Text totalFramesText;
    public TMP_Text movieLengthText;

    [Header("=== ПРОГРЕСС ЭКСПОРТА ===")]
    public ExportProgressUI exportProgressUI;

    // ===== ВНУТРЕННИЕ ДАННЫЕ =====
    public List<FrameItemData> timelineFrames = new List<FrameItemData>();
    public int currentFrameIndex = 0;

    private bool isPlaying = false;
    private float fps = 6f;
    private Coroutine playCoroutine;

    private StopMotionAudioData currentAudio = new StopMotionAudioData();
    private AudioSource audioSource;

    [System.Serializable]
    public class FrameItemData
    {
        public Sprite frameSprite;
        public float duration = 1f;
    }

    private void Start()
    {
        // === НАСТРОЙКА АУДИО ===
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 0.8f;

        // === НАСТРОЙКА FPS ===
        if (fpsSlider != null)
        {
            fpsSlider.minValue = 1f;
            fpsSlider.maxValue = 30f;
            fpsSlider.value = fps;
            fpsSlider.onValueChanged.AddListener(OnFpsChanged);
            UpdateFpsText();
        }

        // === НАСТРОЙКА ONION SKIN ===
        if (onionToggle != null)
            onionToggle.onValueChanged.AddListener(OnOnionToggleChanged);

        if (onionOpacitySlider != null)
        {
            onionOpacitySlider.minValue = 0f;
            onionOpacitySlider.maxValue = 1f;
            onionOpacitySlider.value = 0.35f;
            onionOpacitySlider.onValueChanged.AddListener(OnOnionOpacityChanged);
        }

        // === КНОПКИ УПРАВЛЕНИЯ ===
        if (playButton != null) playButton.onClick.AddListener(TogglePlayback);
        if (duplicateButton != null) duplicateButton.onClick.AddListener(DuplicateCurrentFrame);
        if (deleteButton != null) deleteButton.onClick.AddListener(DeleteCurrentFrame);
        if (reverseButton != null) reverseButton.onClick.AddListener(ReverseTimeline);
        if (importImageButton != null) importImageButton.onClick.AddListener(ImportSingleImage);
        if (importFolderButton != null) importFolderButton.onClick.AddListener(ImportFolder);
        if (exportButton != null) exportButton.onClick.AddListener(ExportAnimation);

        if (createEmptyCanvasButton != null)
            createEmptyCanvasButton.onClick.AddListener(CreateEmptyCanvas);

        // === КНОПКИ ПОВОРОТА ===
        if (rotateCurrentButton != null) rotateCurrentButton.onClick.AddListener(RotateCurrentFrame);
        if (rotateAllButton != null) rotateAllButton.onClick.AddListener(RotateAllFrames);

        // === АУДИО КНОПКИ ===
        if (importAudioButton != null) importAudioButton.onClick.AddListener(ImportAudio);
        if (removeAudioButton != null) removeAudioButton.onClick.AddListener(RemoveAudio);

        if (audioVolumeSlider != null)
        {
            audioVolumeSlider.minValue = 0f;
            audioVolumeSlider.maxValue = 1f;
            audioVolumeSlider.value = 0.8f;
            audioVolumeSlider.onValueChanged.AddListener(OnAudioVolumeChanged);
        }

        // === СОЗДАЕМ НАЧАЛЬНЫЙ КАДР ===
        CreateInitialScene();
        UpdateUI();
        UpdateAudioUI();
    }

    private void CreateInitialScene()
    {
        timelineFrames.Add(new FrameItemData { frameSprite = null });
        currentFrameIndex = 0;
    }

    // =========================================================
    // ============= СОЗДАНИЕ ПУСТОГО ХОЛСТА ===================
    // =========================================================
    public void CreateEmptyCanvas()
    {
        if (isPlaying) StopAnimation();

        int width = 1024;
        int height = 768;
        Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] whitePixels = new Color[width * height];
        for (int i = 0; i < whitePixels.Length; i++)
        {
            whitePixels[i] = Color.white;
        }
        newTexture.SetPixels(whitePixels);
        newTexture.Apply();

        Sprite newSprite = Sprite.Create(
            newTexture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f)
        );

        if (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null)
        {
            timelineFrames[0].frameSprite = newSprite;
        }
        else
        {
            timelineFrames.Add(new FrameItemData { frameSprite = newSprite });
            currentFrameIndex = timelineFrames.Count - 1;
        }

        UpdateUI();
        Debug.Log("Создан новый пустой холст для рисования!");
    }

    // =========================================================
    // ============= УПРАВЛЕНИЕ FPS ============================
    // =========================================================
    private void OnFpsChanged(float value)
    {
        fps = Mathf.Round(value);
        UpdateFpsText();
        UpdateStatistics();
    }

    private void UpdateFpsText()
    {
        if (fpsText != null)
            fpsText.text = $"{fps} кадр/сек";
    }

    // =========================================================
    // ============= ONION SKIN ================================
    // =========================================================
    private void OnOnionToggleChanged(bool value) => UpdateOnionSkin();
    private void OnOnionOpacityChanged(float value) => UpdateOnionSkin();

    private void UpdateOnionSkin()
    {
        if (onionSkinDisplay == null) return;

        bool showOnion = onionToggle != null && onionToggle.isOn;

        if (showOnion && !isPlaying && currentFrameIndex > 0)
        {
            FrameItemData prevFrame = timelineFrames[currentFrameIndex - 1];
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

    // =========================================================
    // ============= ВОСПРОИЗВЕДЕНИЕ ===========================
    // =========================================================
    public void TogglePlayback()
    {
        if (isPlaying)
            StopAnimation();
        else
            StartAnimation();
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

        if (currentAudio.clip != null)
        {
            audioSource.clip = currentAudio.clip;
            audioSource.Play();
        }

        playCoroutine = StartCoroutine(PlayAnimationLoop());
    }

    private void StopAnimation()
    {
        isPlaying = false;

        if (playCoroutine != null)
            StopCoroutine(playCoroutine);

        if (audioSource.isPlaying)
            audioSource.Stop();

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

    // =========================================================
    // ============= ВЫБОР КАДРА ===============================
    // =========================================================
    public void SelectFrame(int index)
    {
        if (isPlaying) StopAnimation();

        if (index >= 0 && index < timelineFrames.Count)
        {
            currentFrameIndex = index;
            UpdateUI();
        }
    }

    // =========================================================
    // ============= ДЕЙСТВИЯ С КАДРАМИ =======================
    // =========================================================
    public void DuplicateCurrentFrame()
    {
        if (timelineFrames.Count == 0 || currentFrameIndex < 0) return;
        if (isPlaying) StopAnimation();

        FrameItemData current = timelineFrames[currentFrameIndex];

        Sprite newSprite = null;
        if (current.frameSprite != null)
        {
            Texture2D originalTexture = current.frameSprite.texture;
            Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height, originalTexture.format, true);
            newTexture.SetPixels(originalTexture.GetPixels());
            newTexture.Apply(true, false);

            newSprite = Sprite.Create(
                newTexture,
                new Rect(0, 0, newTexture.width, newTexture.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        FrameItemData copy = new FrameItemData
        {
            frameSprite = newSprite,
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

        FrameItemData frameToDelete = timelineFrames[currentFrameIndex];

        if (frameToDelete.frameSprite != null)
        {
            bool isUsedElsewhere = false;
            for (int i = 0; i < timelineFrames.Count; i++)
            {
                if (i == currentFrameIndex) continue;
                if (timelineFrames[i].frameSprite == frameToDelete.frameSprite)
                {
                    isUsedElsewhere = true;
                    break;
                }
            }

            if (!isUsedElsewhere)
            {
                Destroy(frameToDelete.frameSprite.texture);
                Destroy(frameToDelete.frameSprite);
            }
        }

        timelineFrames.RemoveAt(currentFrameIndex);

        if (currentFrameIndex >= timelineFrames.Count)
            currentFrameIndex = timelineFrames.Count - 1;

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

    // =========================================================
    // ============= ПОВОРОТ ИЗОБРАЖЕНИЙ ======================
    // =========================================================

    public void RotateCurrentFrame()
    {
        if (isPlaying) StopAnimation();

        if (timelineFrames.Count == 0 || currentFrameIndex < 0)
        {
            Debug.LogWarning("Нет кадров для поворота!");
            return;
        }

        FrameItemData current = timelineFrames[currentFrameIndex];
        if (current.frameSprite == null)
        {
            Debug.LogWarning("Текущий кадр пустой!");
            return;
        }

        Sprite rotated = RotateSprite(current.frameSprite, 90);
        if (rotated != null)
        {
            Destroy(current.frameSprite.texture);
            Destroy(current.frameSprite);

            current.frameSprite = rotated;
            UpdateUI();
            Debug.Log($"Кадр {currentFrameIndex + 1} повернут на 90 градусов");
        }
    }

    public void RotateAllFrames()
    {
        if (isPlaying) StopAnimation();

        if (timelineFrames.Count == 0)
        {
            Debug.LogWarning("Нет кадров для поворота!");
            return;
        }

        int rotatedCount = 0;

        for (int i = 0; i < timelineFrames.Count; i++)
        {
            FrameItemData frame = timelineFrames[i];
            if (frame.frameSprite != null)
            {
                Sprite rotated = RotateSprite(frame.frameSprite, 90);
                if (rotated != null)
                {
                    Destroy(frame.frameSprite.texture);
                    Destroy(frame.frameSprite);
                    frame.frameSprite = rotated;
                    rotatedCount++;
                }
            }
        }

        if (rotatedCount > 0)
        {
            UpdateUI();
            Debug.Log($"Повернуто {rotatedCount} кадров на 90 градусов");
        }
        else
        {
            Debug.LogWarning("Не удалось повернуть ни одного кадра!");
        }
    }

    private Sprite RotateSprite(Sprite original, float angle)
    {
        if (original == null) return null;

        Texture2D originalTex = original.texture;
        int width = originalTex.width;
        int height = originalTex.height;

        Texture2D rotatedTex = new Texture2D(height, width, TextureFormat.RGBA32, true);

        Color[] pixels = originalTex.GetPixels();
        Color[] rotatedPixels = new Color[pixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int newX = height - 1 - y;
                int newY = x;
                rotatedPixels[newY * height + newX] = pixels[y * width + x];
            }
        }

        rotatedTex.SetPixels(rotatedPixels);
        rotatedTex.Apply(true, false);
        rotatedTex.filterMode = FilterMode.Trilinear;
        rotatedTex.anisoLevel = 8;

        Sprite newSprite = Sprite.Create(
            rotatedTex,
            new Rect(0, 0, rotatedTex.width, rotatedTex.height),
            new Vector2(0.5f, 0.5f)
        );

        return newSprite;
    }

    // =========================================================
    // ============= ОБНОВЛЕНИЕ UI =============================
    // =========================================================
    public void UpdateUI()
    {
        if (timelineFrames.Count == 0) return;

        FrameItemData activeFrame = timelineFrames[currentFrameIndex];

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

    private void RedrawTimeline()
    {
        if (timelineContent == null || framePrefab == null) return;

        List<Transform> childrenToDestroy = new List<Transform>();
        foreach (Transform child in timelineContent)
            childrenToDestroy.Add(child);

        foreach (Transform child in childrenToDestroy)
        {
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        for (int i = 0; i < timelineFrames.Count; i++)
        {
            GameObject frameObj = Instantiate(framePrefab, timelineContent);
            frameObj.SetActive(true);

            RectTransform rectTrans = frameObj.GetComponent<RectTransform>();
            if (rectTrans != null)
            {
                rectTrans.localScale = Vector3.one;
                rectTrans.anchoredPosition3D = Vector3.zero;
            }

            DragDropItem dragDrop = frameObj.GetComponent<DragDropItem>();
            if (dragDrop == null)
            {
                dragDrop = frameObj.AddComponent<DragDropItem>();
            }
            dragDrop.Initialize(i);

            FrameItem itemComponent = frameObj.GetComponent<FrameItem>();
            if (itemComponent != null)
            {
                bool isSelected = (i == currentFrameIndex);
                itemComponent.Setup(i, timelineFrames[i].frameSprite, isSelected, SelectFrame);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(timelineContent);
    }

    private void UpdateStatistics()
    {
        if (totalFramesText != null)
            totalFramesText.text = $"{timelineFrames.Count} кадров";

        if (movieLengthText != null)
        {
            float lengthInSeconds = (float)timelineFrames.Count / fps;
            movieLengthText.text = $"{lengthInSeconds:F2} сек";
        }
    }

    // =========================================================
    // ============= ИМПОРТ ИЗОБРАЖЕНИЙ =======================
    // =========================================================

    // === КНОПКА 1: ИМПОРТ ОДНОГО ИЗОБРАЖЕНИЯ ===
    public void ImportSingleImage()
    {
        if (isPlaying) StopAnimation();

#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Выберите изображение", "", "png,jpg,jpeg,bmp,tga,psd,gif,tiff");
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            Debug.Log($"Импорт одного изображения: {path}");
            LoadImageFromPath(path);
        }
        else
        {
            Debug.Log("Импорт отменён");
        }
#else
        // В БИЛДЕ ИСПОЛЬЗУЕМ NATIVE FILE PICKER
        PickImagesNative();
#endif
    }

    // === КНОПКА 2: ИМПОРТ ПАПКИ (ВСЕ КАРТИНКИ ПО ЦИФРАМ В ИМЕНИ) ===
    public void ImportFolder()
    {
        if (isPlaying) StopAnimation();

#if UNITY_EDITOR
        string folderPath = EditorUtility.OpenFolderPanel("Выберите папку с картинками", "", "");
        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
        {
            string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
            List<string> imageFiles = new List<string>();
            string[] extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".psd", ".gif", ".tiff" };

            foreach (string file in allFiles)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (Array.Exists(extensions, e => e == ext))
                {
                    imageFiles.Add(file);
                }
            }

            if (imageFiles.Count > 0)
            {
                imageFiles.Sort((a, b) =>
                {
                    string fileNameA = Path.GetFileNameWithoutExtension(a);
                    string fileNameB = Path.GetFileNameWithoutExtension(b);

                    int numA = ExtractNumber(fileNameA);
                    int numB = ExtractNumber(fileNameB);

                    if (numA != numB)
                        return numA.CompareTo(numB);

                    return string.Compare(fileNameA, fileNameB, StringComparison.OrdinalIgnoreCase);
                });

                Debug.Log($"Найдено {imageFiles.Count} изображений в папке. Начинаю загрузку...");
                StartCoroutine(LoadMultipleImages(imageFiles.ToArray()));
            }
            else
            {
                Debug.LogWarning("В выбранной папке нет поддерживаемых изображений!");
            }
        }
        else
        {
            Debug.Log("Импорт отменён");
        }
#else
        // В БИЛДЕ ИСПОЛЬЗУЕМ NATIVE FILE PICKER ДЛЯ ПАПКИ
        PickFolderNative();
#endif
    }

    // =========================================================
    // ============= NATIVE FILE PICKER (ДЛЯ БИЛДА) ===========
    // =========================================================

    private void PickImagesNative()
    {
        if (NativeFilePicker.IsFilePickerBusy()) return;

        // Разрешаем выбор нескольких файлов
        NativeFilePicker.PickMultipleFiles((paths) =>
        {
            if (paths != null && paths.Length > 0)
            {
                Debug.Log($"Выбрано {paths.Length} изображений через NativePicker");

                // Фильтруем только изображения
                List<string> imagePaths = new List<string>();
                string[] extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".psd", ".gif", ".tiff" };

                foreach (string path in paths)
                {
                    string ext = Path.GetExtension(path).ToLower();
                    if (Array.Exists(extensions, e => e == ext))
                    {
                        imagePaths.Add(path);
                    }
                }

                if (imagePaths.Count > 0)
                {
                    StartCoroutine(LoadMultipleImages(imagePaths.ToArray()));
                }
                else
                {
                    Debug.LogWarning("Выбраны файлы не являющиеся изображениями!");
                }
            }
        }, new string[] { "image/*" });
    }

    private void PickFolderNative()
    {
        // NativeFilePicker НЕ ИМЕЕТ PickFolder!
        // Вместо этого выбираем файл, и загружаем все картинки из его папки
        if (NativeFilePicker.IsFilePickerBusy()) return;

        NativeFilePicker.PickFile((path) =>
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string folderPath = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
                {
                    Debug.Log($"Выбрана папка: {folderPath}");

                    string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
                    List<string> imageFiles = new List<string>();
                    string[] extensions = { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".psd", ".gif", ".tiff" };

                    foreach (string file in allFiles)
                    {
                        string ext = Path.GetExtension(file).ToLower();
                        if (Array.Exists(extensions, e => e == ext))
                        {
                            imageFiles.Add(file);
                        }
                    }

                    if (imageFiles.Count > 0)
                    {
                        imageFiles.Sort((a, b) =>
                        {
                            string fileNameA = Path.GetFileNameWithoutExtension(a);
                            string fileNameB = Path.GetFileNameWithoutExtension(b);
                            int numA = ExtractNumber(fileNameA);
                            int numB = ExtractNumber(fileNameB);
                            if (numA != numB) return numA.CompareTo(numB);
                            return string.Compare(fileNameA, fileNameB, StringComparison.OrdinalIgnoreCase);
                        });

                        Debug.Log($"Найдено {imageFiles.Count} изображений в папке");
                        StartCoroutine(LoadMultipleImages(imageFiles.ToArray()));
                    }
                    else
                    {
                        Debug.LogWarning("В выбранной папке нет изображений!");
                    }
                }
            }
        }, new string[] { "image/*" });
    }

    // =========================================================
    // ============= ЗАГРУЗКА ИЗОБРАЖЕНИЙ =====================
    // =========================================================

    private void LoadImageFromPath(string path)
    {
        if (!File.Exists(path)) return;

        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);

        if (texture.LoadImage(fileData))
        {
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 8;
            texture.Apply(true, false);

            Sprite newSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (activeFrameDisplay != null)
                activeFrameDisplay.preserveAspect = true;

            if (onionSkinDisplay != null)
                onionSkinDisplay.preserveAspect = true;

            if (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null)
            {
                timelineFrames[0].frameSprite = newSprite;
            }
            else
            {
                timelineFrames.Add(new FrameItemData { frameSprite = newSprite });
                currentFrameIndex = timelineFrames.Count - 1;
            }

            UpdateUI();
        }
    }

    private IEnumerator LoadMultipleImages(string[] paths)
    {
        int loadedCount = 0;
        int errorCount = 0;

        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path)) continue;
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Файл не найден: {path}");
                errorCount++;
                continue;
            }

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);

            if (texture.LoadImage(fileData))
            {
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 8;
                texture.Apply(true, false);

                Sprite newSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                if (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null)
                {
                    timelineFrames[0].frameSprite = newSprite;
                }
                else
                {
                    timelineFrames.Add(new FrameItemData { frameSprite = newSprite });
                }

                loadedCount++;

                if (loadedCount % 5 == 0 || loadedCount == paths.Length)
                {
                    currentFrameIndex = timelineFrames.Count - 1;
                    UpdateUI();
                    yield return null;
                }
            }
            else
            {
                Debug.LogError($"Не удалось загрузить изображение: {path}");
                errorCount++;
                Destroy(texture);
            }
        }

        if (loadedCount > 0)
        {
            currentFrameIndex = timelineFrames.Count - 1;
            UpdateUI();
            Debug.Log($"Загружено {loadedCount} изображений. Ошибок: {errorCount}");
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить ни одного изображения!");
            if (timelineFrames.Count == 0)
            {
                CreateInitialScene();
                UpdateUI();
            }
        }

        UpdateStatistics();
        yield return null;
    }

    // =========================================================
    // ============= АУДИО ФУНКЦИИ ============================
    // =========================================================
    public void ImportAudio()
    {
        if (isPlaying) StopAnimation();

#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Выберите аудиофайл", "", "mp3,wav,ogg,aiff");
        if (!string.IsNullOrEmpty(path))
        {
            LoadAudioFromPath(path);
        }
#else
        Debug.Log("В билде импорт аудио через NativePicker");
        PickAudioNative();
#endif
    }

    private void PickAudioNative()
    {
        if (NativeFilePicker.IsFilePickerBusy()) return;

        NativeFilePicker.PickFile((path) =>
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                Debug.Log($"Выбран аудиофайл: {path}");
                LoadAudioFromPath(path);
            }
        }, new string[] { "audio/*" });
    }

    private void LoadAudioFromPath(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            if (currentAudio.clip != null)
            {
                Destroy(currentAudio.clip);
                currentAudio.clip = null;
            }

            StartCoroutine(LoadAudioCoroutine(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка импорта аудио: {e.Message}");
        }
    }

    private IEnumerator LoadAudioCoroutine(string path)
    {
        string url = "file://" + path;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

                if (clip != null)
                {
                    currentAudio.clip = clip;
                    currentAudio.fileName = Path.GetFileName(path);
                    currentAudio.duration = clip.length;
                    currentAudio.trimStart = 0f;
                    currentAudio.trimEnd = 0f;

                    float movieDuration = (float)timelineFrames.Count / fps;
                    if (clip.length > movieDuration)
                    {
                        Debug.LogWarning($"Аудио ({clip.length:F2} сек) длиннее анимации ({movieDuration:F2} сек)!");
                    }

                    if (audioVolumeSlider != null)
                    {
                        audioSource.volume = audioVolumeSlider.value;
                    }
                    else
                    {
                        audioSource.volume = 0.8f;
                    }

                    Debug.Log($"Аудио загружено: {currentAudio.fileName} ({currentAudio.duration:F2} сек)");
                    UpdateAudioUI();
                }
                else
                {
                    Debug.LogError($"Не удалось создать AudioClip из {path}");
                }
            }
            else
            {
                Debug.LogError($"Ошибка загрузки аудио: {www.error}");
            }
        }
    }

    public void RemoveAudio()
    {
        if (currentAudio.clip != null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            Destroy(currentAudio.clip);
            currentAudio.clip = null;
            currentAudio.fileName = "";
            currentAudio.duration = 0f;
            currentAudio.trimStart = 0f;
            currentAudio.trimEnd = 0f;

            Debug.Log("Аудио удалено");
            UpdateAudioUI();
        }
        else
        {
            Debug.LogWarning("Нет аудио для удаления");
        }
    }

    private void UpdateAudioUI()
    {
        bool hasAudio = currentAudio.clip != null;

        if (audioStatusText != null)
        {
            if (hasAudio)
            {
                audioStatusText.text = $"[AUDIO] {currentAudio.fileName} ({currentAudio.duration:F2} сек)";
                audioStatusText.color = Color.green;
            }
            else
            {
                audioStatusText.text = "[NO AUDIO]";
                audioStatusText.color = Color.gray;
            }
        }

        if (audioPanel != null)
            audioPanel.SetActive(hasAudio);

        if (removeAudioButton != null)
        {
            removeAudioButton.interactable = hasAudio;
        }
    }

    private void OnAudioVolumeChanged(float value)
    {
        if (audioSource != null)
        {
            float clampedValue = Mathf.Clamp01(value);
            audioSource.volume = clampedValue;
        }
    }

    // =========================================================
    // ============= ЭКСПОРТ С FFMPEG =========================
    // =========================================================
    public void ExportAnimation()
    {
        if (timelineFrames.Count == 0 ||
            (timelineFrames.Count == 1 && timelineFrames[0].frameSprite == null))
        {
            Debug.LogError("Нечего экспортировать!");
            return;
        }

        if (isPlaying) StopAnimation();

#if UNITY_EDITOR
        string targetPath = EditorUtility.SaveFilePanel(
            "Сохранить видео как MP4",
            "",
            "StopMotionMovie",
            "mp4"
        );

        if (!string.IsNullOrEmpty(targetPath))
        {
            StartCoroutine(ExportWithFFmpeg(targetPath));
        }
#else
        string folderPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
            "StopMotionFilm_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
        );
        Directory.CreateDirectory(folderPath);
        StartCoroutine(ExportFramesCoroutine(folderPath));
#endif
    }

#if UNITY_EDITOR
    private IEnumerator ExportWithFFmpeg(string filePath)
    {
        if (exportProgressUI != null)
        {
            exportProgressUI.ShowProgress();
            exportProgressUI.UpdateStatus("Подготовка к экспорту...");
        }
        yield return null;

        UnityEngine.Debug.Log($"Начало экспорта в: {filePath}");

        string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFmpeg", "ffmpeg.exe");

        if (!File.Exists(ffmpegPath))
        {
            UnityEngine.Debug.LogError($"ffmpeg.exe не найден по пути: {ffmpegPath}");

            if (exportProgressUI != null)
            {
                exportProgressUI.UpdateStatus("Ошибка: FFmpeg не найден!");
                yield return new WaitForSeconds(2f);
                exportProgressUI.HidePanel();
            }
            yield break;
        }

        if (exportProgressUI != null)
            exportProgressUI.UpdateStatus("Экспорт кадров...");

        string tempFolder = Path.Combine(Application.temporaryCachePath, "export_frames_" + DateTime.Now.Ticks);
        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);
        Directory.CreateDirectory(tempFolder);

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

        if (width % 2 != 0) width += 1;
        if (height % 2 != 0) height += 1;
        if (width % 2 != 0) width = width + 1;
        if (height % 2 != 0) height = height + 1;

        UnityEngine.Debug.Log($"Размер видео: {width}x{height}, FPS: {fps}, Кадров: {timelineFrames.Count}");

        int totalFrames = timelineFrames.Count;
        for (int i = 0; i < totalFrames; i++)
        {
            if (i % 5 == 0 && exportProgressUI != null)
            {
                exportProgressUI.UpdateStatus($"Экспорт кадров: {i + 1}/{totalFrames}");
            }

            Sprite sprite = timelineFrames[i].frameSprite;
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;
                int texWidth = tex.width;
                int texHeight = tex.height;

                if (texWidth % 2 != 0) texWidth += 1;
                if (texHeight % 2 != 0) texHeight += 1;

                Texture2D readableTex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
                RenderTexture tempRT = RenderTexture.GetTemporary(texWidth, texHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

                Graphics.Blit(tex, tempRT);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = tempRT;

                readableTex.ReadPixels(new Rect(0, 0, texWidth, texHeight), 0, 0);
                readableTex.Apply();

                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(tempRT);

                byte[] bytes = readableTex.EncodeToPNG();
                Destroy(readableTex);

                string fileName = $"frame_{i + 1:D4}.png";
                string fullPath = Path.Combine(tempFolder, fileName);
                File.WriteAllBytes(fullPath, bytes);
            }
            yield return null;
        }

        if (exportProgressUI != null)
            exportProgressUI.UpdateStatus("Сохранение аудио...");

        string audioPath = "";
        bool hasAudio = currentAudio.clip != null;

        if (hasAudio)
        {
            audioPath = Path.Combine(tempFolder, "audio.wav");
            byte[] wavData = AudioClipToWav(currentAudio.clip);
            File.WriteAllBytes(audioPath, wavData);
            UnityEngine.Debug.Log($"Аудио сохранено: {audioPath}");
        }

        if (exportProgressUI != null)
            exportProgressUI.UpdateStatus("Кодирование видео...");

        string framePattern = $"\"{Path.Combine(tempFolder, "frame_%04d.png")}\"";
        string command = $"-y -framerate {fps} -i {framePattern}";

        if (hasAudio && File.Exists(audioPath))
        {
            float movieDuration = (float)timelineFrames.Count / fps;
            float audioDuration = currentAudio.duration;

            if (audioDuration > movieDuration)
            {
                UnityEngine.Debug.LogWarning($"Аудио ({audioDuration:F2} сек) длиннее анимации ({movieDuration:F2} сек). Аудио будет обрезано.");
            }

            command += $" -i \"{audioPath}\"";
        }

        command += $" -vf \"scale={width}:{height}\"";

        if (hasAudio && File.Exists(audioPath))
        {
            command += $" -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest";
        }
        else
        {
            command += $" -c:v libx264 -pix_fmt yuv420p";
        }

        command += $" \"{filePath}\"";

        UnityEngine.Debug.Log($"Команда FFmpeg: {command}");

        Process process = null;
        bool success = false;

        try
        {
            process = new Process();
            process.StartInfo.FileName = ffmpegPath;
            process.StartInfo.Arguments = command;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            string output = process.StandardError.ReadToEnd();
            process.WaitForExit();

            UnityEngine.Debug.Log($"FFmpeg завершен с кодом: {process.ExitCode}");

            if (!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log($"FFmpeg вывод:\n{output}");

            if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
            {
                UnityEngine.Debug.Log($"Видео успешно создано: {filePath}");
                success = true;
                EditorUtility.RevealInFinder(filePath);
            }
            else
            {
                UnityEngine.Debug.LogError($"FFmpeg не создал MP4 файл! Проверь вывод выше.");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Ошибка при запуске FFmpeg: {e.Message}");
            success = false;
        }

        if (exportProgressUI != null)
        {
            exportProgressUI.OnExportComplete(success);
        }
    }

    private byte[] AudioClipToWav(AudioClip clip)
    {
        float volume = 1f;
        if (audioVolumeSlider != null)
        {
            volume = audioVolumeSlider.value;
        }
        else if (audioSource != null)
        {
            volume = audioSource.volume;
        }

        volume = Mathf.Clamp01(volume);

        if (volume <= 0.001f)
        {
            Debug.Log("Громкость = 0, экспортируем тишину");
            return CreateSilentWav(clip);
        }

        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        byte[] wavData = new byte[sampleCount * 2 + 44];

        int position = 0;
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavData, position); position += 4;
        int fileSize = sampleCount * 2 + 36;
        BitConverter.GetBytes(fileSize).CopyTo(wavData, position); position += 4;
        System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavData, position); position += 4;
        System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavData, position); position += 4;
        BitConverter.GetBytes(16).CopyTo(wavData, position); position += 4;
        BitConverter.GetBytes((short)1).CopyTo(wavData, position); position += 2;
        BitConverter.GetBytes((short)clip.channels).CopyTo(wavData, position); position += 2;
        BitConverter.GetBytes(clip.frequency).CopyTo(wavData, position); position += 4;
        int byteRate = clip.frequency * clip.channels * 2;
        BitConverter.GetBytes(byteRate).CopyTo(wavData, position); position += 4;
        short blockAlign = (short)(clip.channels * 2);
        BitConverter.GetBytes(blockAlign).CopyTo(wavData, position); position += 2;
        BitConverter.GetBytes((short)16).CopyTo(wavData, position); position += 2;
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavData, position); position += 4;
        int dataSize = sampleCount * 2;
        BitConverter.GetBytes(dataSize).CopyTo(wavData, position); position += 4;

        for (int i = 0; i < samples.Length; i++)
        {
            float adjustedSample = samples[i] * volume;
            adjustedSample = Mathf.Clamp(adjustedSample, -1f, 1f);
            short sample = (short)(adjustedSample * 32767f);
            BitConverter.GetBytes(sample).CopyTo(wavData, position);
            position += 2;
        }

        Debug.Log($"Экспорт аудио с громкостью: {volume * 100:F0}%");
        return wavData;
    }

    private byte[] CreateSilentWav(AudioClip clip)
    {
        int sampleCount = clip.samples * clip.channels;
        byte[] wavData = new byte[sampleCount * 2 + 44];

        int position = 0;
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavData, position); position += 4;
        int fileSize = sampleCount * 2 + 36;
        BitConverter.GetBytes(fileSize).CopyTo(wavData, position); position += 4;
        System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavData, position); position += 4;
        System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavData, position); position += 4;
        BitConverter.GetBytes(16).CopyTo(wavData, position); position += 4;
        BitConverter.GetBytes((short)1).CopyTo(wavData, position); position += 2;
        BitConverter.GetBytes((short)clip.channels).CopyTo(wavData, position); position += 2;
        BitConverter.GetBytes(clip.frequency).CopyTo(wavData, position); position += 4;
        int byteRate = clip.frequency * clip.channels * 2;
        BitConverter.GetBytes(byteRate).CopyTo(wavData, position); position += 4;
        short blockAlign = (short)(clip.channels * 2);
        BitConverter.GetBytes(blockAlign).CopyTo(wavData, position); position += 2;
        BitConverter.GetBytes((short)16).CopyTo(wavData, position); position += 2;
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavData, position); position += 4;
        int dataSize = sampleCount * 2;
        BitConverter.GetBytes(dataSize).CopyTo(wavData, position); position += 4;

        for (int i = 0; i < sampleCount; i++)
        {
            BitConverter.GetBytes((short)0).CopyTo(wavData, position);
            position += 2;
        }

        return wavData;
    }
#endif

    private IEnumerator ExportFramesCoroutine(string folderPath)
    {
        Debug.Log($"Export frames to: {folderPath}");

        for (int i = 0; i < timelineFrames.Count; i++)
        {
            Sprite sprite = timelineFrames[i].frameSprite;
            if (sprite != null)
            {
                Texture2D tex = sprite.texture;
                Texture2D readableTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);

                RenderTexture tempRT = RenderTexture.GetTemporary(
                    tex.width, tex.height, 0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB
                );

                Graphics.Blit(tex, tempRT);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = tempRT;

                readableTex.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
                readableTex.Apply();

                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(tempRT);

                byte[] bytes = readableTex.EncodeToPNG();
                Destroy(readableTex);

                string fileName = $"frame_{i + 1:D4}.png";
                string fullPath = Path.Combine(folderPath, fileName);
                File.WriteAllBytes(fullPath, bytes);
            }
            yield return null;
        }

        string readmePath = Path.Combine(folderPath, "readme.txt");
        string readmeContent = $"Exported frames: {timelineFrames.Count}\n" +
                               $"FPS: {fps}\n" +
                               $"Duration: {(float)timelineFrames.Count / fps:F2} sec\n\n" +
                               $"To create video:\n" +
                               $"ffmpeg -framerate {fps} -i frame_%04d.png -c:v libx264 -pix_fmt yuv420p video.mp4\n\n";

        if (currentAudio.clip != null)
        {
            readmeContent += $"Audio: {currentAudio.fileName} ({currentAudio.duration:F2} sec)\n" +
                             $"To add audio:\n" +
                             $"ffmpeg -i video.mp4 -i audio.wav -c:v copy -c:a aac -shortest output_with_audio.mp4\n";
        }

        File.WriteAllText(readmePath, readmeContent);
        Debug.Log($"Export completed! Folder: {folderPath}");
    }

    private int ExtractNumber(string text)
    {
        if (string.IsNullOrEmpty(text)) return int.MaxValue;

        int startIndex = -1;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]))
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex == -1) return int.MaxValue;

        int endIndex = startIndex;
        while (endIndex < text.Length && char.IsDigit(text[endIndex]))
        {
            endIndex++;
        }

        string numberStr = text.Substring(startIndex, endIndex - startIndex);
        if (int.TryParse(numberStr, out int result))
        {
            return result;
        }

        return int.MaxValue;
    }

    public void MoveFrame(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (fromIndex < 0 || fromIndex >= timelineFrames.Count) return;
        if (toIndex < 0 || toIndex >= timelineFrames.Count) return;

        FrameItemData frame = timelineFrames[fromIndex];
        timelineFrames.RemoveAt(fromIndex);
        timelineFrames.Insert(toIndex, frame);

        currentFrameIndex = toIndex;
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (currentAudio.clip != null)
            Destroy(currentAudio.clip);

        foreach (var frame in timelineFrames)
        {
            if (frame.frameSprite != null)
            {
                Destroy(frame.frameSprite.texture);
                Destroy(frame.frameSprite);
            }
        }

        timelineFrames.Clear();
    }

    private void OnApplicationQuit()
    {
        if (isPlaying)
            StopAnimation();
    }
}