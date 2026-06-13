using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Этот класс управляет списком всех кадров, 
// переключением между ними, калькой (Onion Skin) и воспроизведением.
public class FlipbookManager : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    public FlipbookDrawer drawer; // Ссылка на скрипт рисования
    public RawImage onionSkinImage; // Дополнительная картинка сзади со значением прозрачности Alpha ~0.25f

    [Header("Настройки воспроизведения")]
    public float fps = 6f; // Кадров в секунду

    private List<Texture2D> framesList = new List<Texture2D>();
    private int currentFrameIndex = 0;

    private bool isPlaying = false;
    private float nextFrameTime = 0f;

    void Start()
    {
        // Создаем первый пустой кадр
        AddNewFrame();
    }

    void Update()
    {
        if (isPlaying)
        {
            if (Time.time >= nextFrameTime)
            {
                // Переходим на следующий кадр в цикле
                currentFrameIndex = (currentFrameIndex + 1) % framesList.Count;
                ShowFrame(currentFrameIndex);
                nextFrameTime = Time.time + (1f / fps);
            }
        }
    }

    // Создает новый чистый кадр
    public void AddNewFrame()
    {
        Texture2D newTex = new Texture2D(600, 450, TextureFormat.RGBA32, false);

        // Заполняем белым
        Color[] pixels = new Color[600 * 450];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        newTex.SetPixels(pixels);
        newTex.Apply();

        // Добавляем текстуру в список
        framesList.Add(newTex);
        currentFrameIndex = framesList.Count - 1;
        ShowFrame(currentFrameIndex);
    }

    // Подсветить нужный кадр на холсте
    public void ShowFrame(int index)
    {
        if (index < 0 || index >= framesList.Count) return;
        currentFrameIndex = index;

        // Рисуем активный кадр
        drawer.SetActiveTexture(framesList[index]);

        // Настраиваем кальку (Onion Skin) - просвечивание предыдущего кадра
        UpdateOnionSkin();
    }

    // Настройка кальки
    private void UpdateOnionSkin()
    {
        if (onionSkinImage == null) return;

        // Если есть предыдущий кадр, показываем его полупрозрачным на заднем фоне
        if (currentFrameIndex > 0 && !isPlaying)
        {
            onionSkinImage.gameObject.SetActive(true);
            onionSkinImage.texture = framesList[currentFrameIndex - 1];

            // Ставим полупрозрачность
            Color c = onionSkinImage.color;
            c.a = 0.25f;
            onionSkinImage.color = c;
        }
        else
        {
            onionSkinImage.gameObject.SetActive(false);
        }
    }

    // Переходы по кнопкам "Вперед / Назад"
    public void GoToNextFrame()
    {
        if (isPlaying) return;
        if (currentFrameIndex < framesList.Count - 1)
        {
            ShowFrame(currentFrameIndex + 1);
        }
    }

    public void GoToPrevFrame()
    {
        if (isPlaying) return;
        if (currentFrameIndex > 0)
        {
            ShowFrame(currentFrameIndex - 1);
        }
    }

    // Дублирование текущего кадра
    public void DuplicateCurrentFrame()
    {
        Texture2D currentTex = framesList[currentFrameIndex];
        Texture2D duplicatedTex = new Texture2D(currentTex.width, currentTex.height, currentTex.format, false);

        // Копируем пиксели оригинальной текстуры
        duplicatedTex.SetPixels(currentTex.GetPixels());
        duplicatedTex.Apply();

        framesList.Insert(currentFrameIndex + 1, duplicatedTex);
        ShowFrame(currentFrameIndex + 1);
    }

    // Удаление кадра
    public void DeleteCurrentFrame()
    {
        if (framesList.Count <= 1)
        {
            // Если кадр единственный, просто стираем его начисто
            drawer.ClearTexture();
            return;
        }

        framesList.RemoveAt(currentFrameIndex);
        currentFrameIndex = Mathf.Max(0, currentFrameIndex - 1);
        ShowFrame(currentFrameIndex);
    }

    // Старт / Стоп Анимации
    public void TogglePlayback()
    {
        isPlaying = !isPlaying;
        if (isPlaying)
        {
            nextFrameTime = Time.time + (1f / fps);
        }
        else
        {
            // Возвращаем на текущий редактируемый кадр после просмотра
            ShowFrame(currentFrameIndex);
        }
    }

    // Изменение скорости (для слайдера FPS)
    public void SetFpsValue(float value)
    {
        fps = Mathf.Clamp(value, 1f, 24f);
    }
}