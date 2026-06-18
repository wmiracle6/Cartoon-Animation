using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// 1. ВИЗУАЛЬНЫЙ ЭЛЕМЕНТ ДОРОЖКИ (Содержит шапку и рабочую область)
public class AudioTrackUI : VisualElement
{
    private AudioTrackData _data;
    private VisualElement _trackHeader;
    private VisualElement _trackContent;
    private List<AudioClipUI> _clipViews = new List<AudioClipUI>();

    public AudioTrackUI(AudioTrackData data)
    {
        _data = data;

        // Базовые стили контейнера дорожки
        style.flexDirection = FlexDirection.Row;
        style.height = 60;
        style.marginBottom = 2;

        // Создаем левую часть — Шапку трека
        _trackHeader = new VisualElement();
        _trackHeader.style.width = 150;
        _trackHeader.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
        _trackHeader.style.justifyContent = Justify.Center;
        _trackHeader.style.paddingLeft = 10;

        var label = new Label(_data.TrackName);
        label.style.color = Color.white;
        _trackHeader.Add(label);
        Add(_trackHeader);

        // Создаем правую часть — Таймлайн-контент, где лежат блоки аудио
        _trackContent = new VisualElement();
        _trackContent.style.flexGrow = 1;
        _trackContent.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
        _trackContent.style.position = Position.Relative;
        Add(_trackContent);

        // Спавним клипы внутрь дорожки
        foreach (var clipData in _data.Clips)
        {
            var clipUI = new AudioClipUI(clipData);
            _trackContent.Add(clipUI);
            _clipViews.Add(clipUI);
        }
    }
}

// 2. ВИЗУАЛЬНЫЙ ЭЛЕМЕНТ КЛИПА (Рисует блок и генерирует сетку волны)
public class AudioClipUI : VisualElement
{
    private AudioClipData _clipData;
    private float[] _waveformCache;
    private Color _waveColor = new Color(0.2f, 0.6f, 1f); // Голубой цвет волны

    public AudioClipUI(AudioClipData clipData)
    {
        _clipData = clipData;

        style.position = Position.Absolute;
        style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
        style.borderLeftColor = Color.cyan;
        style.borderLeftWidth = 2;

        // Подписываем Unity-колбэк на генерацию кастомного визуала (меша волны)
        generateVisualContent += OnGenerateVisualContent;

        // Заполняем фейковую волну для теста, если нет реального аудиофайла
        GenerateDummyWaveform();
        UpdateLayout();
    }

    private void GenerateDummyWaveform()
    {
        _waveformCache = new float[120];
        for (int i = 0; i < _waveformCache.Length; i++)
        {
            _waveformCache[i] = Mathf.Abs(Mathf.Sin(i * 0.15f)) * Random.Range(0.4f, 1f);
        }
    }

    public void UpdateLayout()
    {
        float pixelsPerSecond = 25f; // Масштаб времени (1 секунда = 25 пикселей)
        style.left = _clipData.StartTime * pixelsPerSecond;
        style.width = _clipData.Duration * pixelsPerSecond;
        style.height = Length.Percent(100);
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        if (_waveformCache == null || _waveformCache.Length == 0) return;

        float width = contentRect.width;
        float height = contentRect.height;
        float halfHeight = height / 2f;

        int totalVertices = _waveformCache.Length * 4;
        int totalIndices = _waveformCache.Length * 6;

        MeshWriteData mwd = mgc.Allocate(totalVertices, totalIndices);

        Vertex[] allVertices = new Vertex[totalVertices];
        ushort[] allIndices = new ushort[totalIndices];

        for (int i = 0; i < _waveformCache.Length; i++)
        {
            float xPos = (i / (float)_waveformCache.Length) * width;
            float amplitude = _waveformCache[i] * halfHeight;

            int vIdx = i * 4;
            allVertices[vIdx + 0].position = new Vector3(xPos, halfHeight - amplitude, Vertex.nearZ);
            allVertices[vIdx + 1].position = new Vector3(xPos + 1.5f, halfHeight - amplitude, Vertex.nearZ);
            allVertices[vIdx + 2].position = new Vector3(xPos, halfHeight + amplitude, Vertex.nearZ);
            allVertices[vIdx + 3].position = new Vector3(xPos + 1.5f, halfHeight + amplitude, Vertex.nearZ);

            allVertices[vIdx + 0].tint = _waveColor;
            allVertices[vIdx + 1].tint = _waveColor;
            allVertices[vIdx + 2].tint = _waveColor;
            allVertices[vIdx + 3].tint = _waveColor;

            int iIdx = i * 6;
            allIndices[iIdx + 0] = (ushort)(vIdx + 0);
            allIndices[iIdx + 1] = (ushort)(vIdx + 2);
            allIndices[iIdx + 2] = (ushort)(vIdx + 1);
            allIndices[iIdx + 3] = (ushort)(vIdx + 1);
            allIndices[iIdx + 4] = (ushort)(vIdx + 2);
            allIndices[iIdx + 5] = (ushort)(vIdx + 3);
        }

        mwd.SetAllVertices(allVertices);
        mwd.SetAllIndices(allIndices);
    }
}