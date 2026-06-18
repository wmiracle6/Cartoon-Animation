using System.Collections.Generic;
using UnityEngine;

public class AudioTrackData
{
    public string TrackName;
    public List<AudioClipData> Clips = new List<AudioClipData>();
}

public class AudioClipData
{
    public AudioClip RealClip; // Ссылка на сам аудиофайл (.mp3, .wav)
    public float StartTime;    // Секунда таймлайна, на которой начнется звук
    public float Duration;     // Длина звука в секундах
}