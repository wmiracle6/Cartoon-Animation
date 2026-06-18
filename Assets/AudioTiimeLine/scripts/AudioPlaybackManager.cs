using System.Collections.Generic;
using UnityEngine;

public class AudioPlaybackManager : MonoBehaviour
{
    public ITimelineController TimelineController;

    private List<AudioTrackData> _tracks = new List<AudioTrackData>();
    private Dictionary<AudioClipData, AudioSource> _activeSources = new Dictionary<AudioClipData, AudioSource>();

    private void Start()
    {
        // јвтоматически ищет TimelineControllerStub на этом же объекте
        TimelineController = GetComponent<ITimelineController>();

        if (TimelineController == null)
        {
            Debug.LogError(" ритическа€ ошибка: Ќа объекте не найден контроллер времени (TimelineControllerStub)!");
        }
    }

    private void Update()
    {
        if (TimelineController == null || !TimelineController.IsPlaying) return;

        float currentTime = TimelineController.CurrentTime;

        foreach (var track in _tracks)
        {
            foreach (var clip in track.Clips)
            {
                float clipEndTime = clip.StartTime + clip.Duration;

                // ”словие 1: ¬рем€ зашло на дорожку, а звук еще не играет Ч ¬ Ћё„ј≈ћ
                if (currentTime >= clip.StartTime && currentTime < clipEndTime)
                {
                    if (!_activeSources.ContainsKey(clip))
                    {
                        PlayClip(clip, currentTime - clip.StartTime);
                    }
                }
                // ”словие 2: ¬рем€ вышло за пределы полоски Ч ¬џ Ћё„ј≈ћ
                else if (currentTime >= clipEndTime || currentTime < clip.StartTime)
                {
                    if (_activeSources.ContainsKey(clip))
                    {
                        StopClip(clip);
                    }
                }
            }
        }
    }

    private void PlayClip(AudioClipData clipData, float timeOffset)
    {
        // —оздаем временный объект дл€ воспроизведени€ звука в »ерархии
        GameObject audioObj = new GameObject("Audio_" + clipData.RealClip.name);
        audioObj.transform.SetParent(transform);

        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.clip = clipData.RealClip;

        // —инхронизируем старт звука с текущим временем таймлайна
        source.time = timeOffset;
        source.Play();

        _activeSources.Add(clipData, source);
    }

    private void StopClip(AudioClipData clipData)
    {
        if (_activeSources.TryGetValue(clipData, out AudioSource source))
        {
            if (source != null) Destroy(source.gameObject);
            _activeSources.Remove(clipData);
        }
    }

    public void RegisterTrack(AudioTrackData track)
    {
        _tracks.Add(track);
    }
}