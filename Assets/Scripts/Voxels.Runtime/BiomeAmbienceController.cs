using UnityEngine;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
  public sealed class BiomeAmbienceController : MonoBehaviour
  {
    [SerializeField] float blendSpeed = 2f;
    [SerializeField] float crossfadeSeconds = 1.5f;

    WorldScroller scroller;
    CelestialSystem celestial;
    AudioSource ambientA;
    AudioSource ambientB;
    bool useSourceA = true;
    BiomeDefinition currentBiome;
    Color currentDayAmbient;
    Color currentNightAmbient;

    public void Initialize(WorldScroller worldScroller, CelestialSystem celestialSystem)
    {
      scroller = worldScroller;
      celestial = celestialSystem;
      ambientA = CreateAmbientSource("AmbientA");
      ambientB = CreateAmbientSource("AmbientB");
      currentDayAmbient = RenderSettings.ambientLight;
      currentNightAmbient = currentDayAmbient;
    }

    static AudioSource CreateAmbientSource(string name)
    {
      var sourceObject = new GameObject(name);
      var source = sourceObject.AddComponent<AudioSource>();
      source.loop = true;
      source.spatialBlend = 0f;
      source.playOnAwake = false;
      source.volume = 0f;
      return source;
    }

    void Update()
    {
      if (scroller?.HexWorld == null)
      {
        return;
      }

      BiomeDefinition biome = scroller.HexWorld.GetBiome(scroller.PlayerWorldHex);
      if (biome != currentBiome)
      {
        currentBiome = biome;
        CrossfadeAmbient(biome);
      }

      float sun = celestial != null ? celestial.SunHeight : 1f;
      Color day = biome != null ? biome.DayAmbientColor : currentDayAmbient;
      Color night = biome != null ? biome.NightAmbientColor : currentNightAmbient;
      Color target = Color.Lerp(night, day, sun);
      RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, target, Time.deltaTime * blendSpeed);
    }

    void CrossfadeAmbient(BiomeDefinition biome)
    {
      if (biome?.AmbientLoop == null)
      {
        return;
      }

      AudioSource next = useSourceA ? ambientB : ambientA;
      AudioSource prev = useSourceA ? ambientA : ambientB;
      useSourceA = !useSourceA;

      next.clip = biome.AmbientLoop;
      next.volume = biome.AmbientVolume;
      next.Play();
      StopAllCoroutines();
      StartCoroutine(FadeSources(prev, next, crossfadeSeconds));
    }

    System.Collections.IEnumerator FadeSources(AudioSource from, AudioSource to, float duration)
    {
      float startFrom = from != null ? from.volume : 0f;
      float startTo = to != null ? to.volume : 0f;
      float elapsed = 0f;
      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        if (from != null)
        {
          from.volume = Mathf.Lerp(startFrom, 0f, t);
        }

        if (to != null)
        {
          to.volume = Mathf.Lerp(0f, startTo, t);
        }

        yield return null;
      }

      if (from != null)
      {
        from.Stop();
      }
    }
  }
}
