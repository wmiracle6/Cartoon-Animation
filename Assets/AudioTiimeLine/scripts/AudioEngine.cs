using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class AudioUtils
{
    public static IEnumerator ImportAudioRoutine(string path, System.Action<AudioClip> onLoaded)
    {
        string uri = "file://" + path;
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.UNKNOWN))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                onLoaded?.Invoke(clip);
            }
            else
            {
                Debug.LogError($"Error loading audio: {www.error}");
                onLoaded?.Invoke(null);
            }
        }
    }

    private static Dictionary<AudioClip, float[]> _waveformCache = new Dictionary<AudioClip, float[]>();

    public static float[] GetWaveform(AudioClip clip, int resolution = 1024)
    {
        if (_waveformCache.TryGetValue(clip, out float[] cached)) return cached;

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        float[] waveform = new float[resolution];
        int packSize = (samples.Length / resolution);

        for (int i = 0; i < resolution; i++)
        {
            float max = 0;
            for (int j = 0; j < packSize; j++)
            {
                int index = i * packSize + j;
                if (index < samples.Length)
                {
                    float absVal = Mathf.Abs(samples[index]);
                    if (absVal > max) max = absVal;
                }
            }
            waveform[i] = max;
        }

        _waveformCache[clip] = waveform;
        return waveform;
    }
}

public interface ITimelineController
{
    float CurrentTime { get; }
    bool IsPlaying { get; }
    event System.Action<float> OnTimeChanged;
}