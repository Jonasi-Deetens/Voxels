using UnityEngine;
using Voxels.World.Climate;

namespace Voxels.Runtime
{
    public sealed class WeatherAudioController : MonoBehaviour
    {
        [SerializeField] float maxRainVolume = 0.45f;
        [SerializeField] float maxWindVolume = 0.28f;

        AudioSource rainSource;
        AudioSource windSource;

        public void Initialize()
        {
            rainSource = CreateLoopSource("RainLoop", 220f);
            windSource = CreateLoopSource("WindLoop", 95f);
        }

        public void ApplyWeather(in WeatherSnapshot snapshot)
        {
            if (rainSource == null)
            {
                Initialize();
            }

            float rain = snapshot.Precipitation;
            if (snapshot.Kind == WeatherKind.Storm)
            {
                rain = Mathf.Max(rain, 0.9f);
            }

            rainSource.volume = Mathf.Lerp(0f, maxRainVolume, rain);
            windSource.volume = Mathf.Lerp(0f, maxWindVolume, snapshot.WindMultiplier * 0.55f);

            if (rainSource.volume > 0.02f && !rainSource.isPlaying)
            {
                rainSource.Play();
            }
            else if (rainSource.volume <= 0.02f && rainSource.isPlaying)
            {
                rainSource.Stop();
            }

            if (windSource.volume > 0.02f && !windSource.isPlaying)
            {
                windSource.Play();
            }
            else if (windSource.volume <= 0.02f && windSource.isPlaying)
            {
                windSource.Stop();
            }
        }

        static AudioSource CreateLoopSource(string name, float frequency)
        {
            var sourceObject = new GameObject(name);
            sourceObject.transform.SetParent(null);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.clip = CreateNoiseLoop(name, frequency);
            source.volume = 0f;
            return source;
        }

        static AudioClip CreateNoiseLoop(string clipName, float baseFrequency)
        {
            const int sampleRate = 22050;
            const float duration = 2f;
            int sampleCount = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float noise = Mathf.PerlinNoise(t * baseFrequency * 0.01f, 0.12f) * 2f - 1f;
                float tone = Mathf.Sin(2f * Mathf.PI * baseFrequency * t) * 0.15f;
                samples[i] = (noise + tone) * 0.04f;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
