using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // Нужно для загрузки файлов с диска
using System.Collections;     // Нужно для асинхронных операций (корутин)

#if UNITY_EDITOR
using UnityEditor;            // Нужно для вызова окна проводника
#endif

public class TimelineBootstrapper : MonoBehaviour
{
    [Header("Ссылки на интерфейс Canvas")]
    public Button AddTrackButton;
    public Transform TrackContainer;
    public GameObject TrackPrefab;

    [Header("Настройки Аудио")]
    public float PixelsPerSecond = 20f;

    private AudioPlaybackManager _playbackManager;
    private int _trackCounter = 1;

    private void Start()
    {
        _playbackManager = GetComponent<AudioPlaybackManager>();

        if (AddTrackButton != null)
        {
            // Теперь при нажатии кнопки мы открываем проводник
            AddTrackButton.onClick.AddListener(OpenExplorer);
        }
    }

    private void OpenExplorer()
    {
#if UNITY_EDITOR
        // Открываем стандартное окно выбора файла (только в Unity)
        string path = EditorUtility.OpenFilePanel("Выберите аудиофайл", "", "mp3,wav,ogg");

        if (!string.IsNullOrEmpty(path))
        {
            // Если файл выбран, начинаем его загрузку
            StartCoroutine(LoadAudioFile(path));
        }
#else
        Debug.LogWarning("Окно выбора файла работает только в редакторе Unity. Для собранной программы нужен отдельный плагин.");
#endif
    }

    // Специальная функция (корутина), которая загружает файл в фоне, не подвешивая игру
    private IEnumerator LoadAudioFile(string absolutePath)
    {
        // Добавляем "file:///" чтобы Unity поняла, что это локальный файл на диске
        string url = "file:///" + absolutePath;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
        {
            // Ждем, пока файл загрузится в оперативную память
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Ошибка загрузки файла: " + www.error);
            }
            else
            {
                // Достаем готовый звук
                AudioClip downloadedClip = DownloadHandlerAudioClip.GetContent(www);

                // Называем клип так же, как называется сам файл
                downloadedClip.name = System.IO.Path.GetFileNameWithoutExtension(absolutePath);

                // Создаем дорожку
                CreateNewTrack(downloadedClip);
            }
        }
    }

    private void CreateNewTrack(AudioClip loadedClip)
    {
        GameObject newTrackUI = Instantiate(TrackPrefab, TrackContainer);
        newTrackUI.name = "TrackUI_" + _trackCounter;

        var clipData = new AudioClipData
        {
            RealClip = loadedClip,
            StartTime = 0.0f, // Ставим звук на самое начало таймлайна
            Duration = loadedClip.length
        };

        // Берем имя трека из названия аудиофайла
        var newTrackData = new AudioTrackData { TrackName = loadedClip.name };
        newTrackData.Clips.Add(clipData);

        RectTransform clipTransform = newTrackUI.transform.Find("ClipUI") as RectTransform;
        if (clipTransform != null)
        {
            float xPosition = clipData.StartTime * PixelsPerSecond;
            float clipWidth = clipData.Duration * PixelsPerSecond;

            clipTransform.anchoredPosition = new Vector2(xPosition, 0);
            clipTransform.sizeDelta = new Vector2(clipWidth, clipTransform.sizeDelta.y);
        }

        if (_playbackManager != null)
        {
            _playbackManager.RegisterTrack(newTrackData);
        }

        _trackCounter++;
    }
}