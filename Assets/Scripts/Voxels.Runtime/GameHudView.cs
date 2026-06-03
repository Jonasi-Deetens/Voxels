using UnityEngine;
using UnityEngine.UI;
using Voxels.Core.Hex;
using Voxels.World;

namespace Voxels.Runtime
{
  public sealed class GameHudView : MonoBehaviour
  {
    [SerializeField] bool showDebug = true;
    [SerializeField] bool showMinimap = true;

    Canvas canvas;
    Text debugText;
    Text[] hotbarLabels;
    RawImage minimapImage;
    Texture2D minimapTexture;
    WorldScroller scroller;
    HexChunkManager chunkManager;
    CelestialSystem celestial;
    BlockHotbar hotbar;
    PlayerToolState toolState;
    WorldRuntimeProfiler profiler;
    WorldSettings settings;
    HexBlockInteractor blockInteractor;
    PlayerInventory inventory;
    Slider breakSlider;

    const int MinimapSize = 25;

    public void Initialize(
      WorldScroller worldScroller,
      HexChunkManager chunks,
      CelestialSystem celestialSystem,
      BlockHotbar blockHotbar,
      PlayerToolState tools,
      WorldRuntimeProfiler runtimeProfiler,
      WorldSettings worldSettings,
      HexBlockInteractor interactor = null,
      PlayerInventory playerInventory = null)
    {
      scroller = worldScroller;
      chunkManager = chunks;
      celestial = celestialSystem;
      hotbar = blockHotbar;
      toolState = tools;
      profiler = runtimeProfiler;
      settings = worldSettings;
      blockInteractor = interactor;
      inventory = playerInventory;
      EnsureUi();
    }

    void EnsureUi()
    {
      if (canvas != null)
      {
        return;
      }

      if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
      {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
      }

      var canvasObject = new GameObject("GameHudCanvas");
      canvasObject.transform.SetParent(transform, false);
      canvas = canvasObject.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      canvasObject.AddComponent<GraphicRaycaster>();

      debugText = CreateText(canvasObject.transform, "DebugText", 14, TextAnchor.UpperLeft);
      var debugRect = debugText.rectTransform;
      debugRect.anchorMin = new Vector2(0f, 1f);
      debugRect.anchorMax = new Vector2(0f, 1f);
      debugRect.pivot = new Vector2(0f, 1f);
      debugRect.anchoredPosition = new Vector2(12f, -12f);
      debugRect.sizeDelta = new Vector2(460f, 220f);

      hotbarLabels = new Text[9];
      var hotbarRoot = new GameObject("Hotbar", typeof(RectTransform));
      hotbarRoot.transform.SetParent(canvasObject.transform, false);
      var hotbarRect = hotbarRoot.GetComponent<RectTransform>();
      hotbarRect.anchorMin = new Vector2(0.5f, 0f);
      hotbarRect.anchorMax = new Vector2(0.5f, 0f);
      hotbarRect.pivot = new Vector2(0.5f, 0f);
      hotbarRect.anchoredPosition = new Vector2(0f, 24f);
      hotbarRect.sizeDelta = new Vector2(520f, 36f);

      for (int i = 0; i < hotbarLabels.Length; i++)
      {
        hotbarLabels[i] = CreateText(hotbarRoot.transform, $"Slot{i + 1}", 13, TextAnchor.MiddleCenter);
        var rect = hotbarLabels[i].rectTransform;
        rect.anchorMin = new Vector2(i / 9f, 0f);
        rect.anchorMax = new Vector2((i + 1) / 9f, 1f);
        rect.offsetMin = new Vector2(2f, 0f);
        rect.offsetMax = new Vector2(-2f, 0f);
      }

      var minimapObject = new GameObject("Minimap", typeof(RectTransform), typeof(RawImage));
      minimapObject.transform.SetParent(canvasObject.transform, false);
      var minimapRect = minimapObject.GetComponent<RectTransform>();
      minimapRect.anchorMin = new Vector2(1f, 1f);
      minimapRect.anchorMax = new Vector2(1f, 1f);
      minimapRect.pivot = new Vector2(1f, 1f);
      minimapRect.anchoredPosition = new Vector2(-16f, -16f);
      minimapRect.sizeDelta = new Vector2(130f, 130f);
      minimapImage = minimapObject.GetComponent<RawImage>();
      minimapTexture = new Texture2D(MinimapSize, MinimapSize, TextureFormat.RGBA32, false)
      {
        filterMode = FilterMode.Point,
        wrapMode = TextureWrapMode.Clamp,
      };
      minimapImage.texture = minimapTexture;

      var breakObject = new GameObject("BreakProgress", typeof(RectTransform), typeof(Slider));
      breakObject.transform.SetParent(canvasObject.transform, false);
      var breakRect = breakObject.GetComponent<RectTransform>();
      breakRect.anchorMin = new Vector2(0.5f, 0.5f);
      breakRect.anchorMax = new Vector2(0.5f, 0.5f);
      breakRect.sizeDelta = new Vector2(160f, 12f);
      breakSlider = breakObject.GetComponent<Slider>();
      breakSlider.minValue = 0f;
      breakSlider.maxValue = 1f;
      breakSlider.gameObject.SetActive(false);
    }

    static Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor)
    {
      var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
      textObject.transform.SetParent(parent, false);
      var text = textObject.GetComponent<Text>();
      text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
      text.fontSize = fontSize;
      text.alignment = anchor;
      text.color = Color.white;
      text.horizontalOverflow = HorizontalWrapMode.Overflow;
      text.verticalOverflow = VerticalWrapMode.Overflow;
      var shadow = textObject.AddComponent<Shadow>();
      shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
      shadow.effectDistance = new Vector2(1f, -1f);
      return text;
    }

    void Update()
    {
      if (GameInput.WasDebugTogglePressedThisFrame())
      {
        showDebug = !showDebug;
      }

      if (canvas == null)
      {
        return;
      }

      UpdateHotbar();
      UpdateBreakBar();
      if (showDebug)
      {
        UpdateDebug();
      }
      else if (debugText != null)
      {
        debugText.text = string.Empty;
      }

      if (showMinimap)
      {
        UpdateMinimap();
      }
    }

    void UpdateHotbar()
    {
      if (hotbar == null || hotbarLabels == null || scroller?.HexWorld == null)
      {
        return;
      }

      BlockRegistry registry = scroller.HexWorld.BlockRegistry;
      for (int i = 0; i < hotbarLabels.Length; i++)
      {
        bool selected = i == hotbar.SelectedIndex;
        string count = inventory != null ? inventory.FormatCount(hotbar.GetSlot(i)) : string.Empty;
        string suffix = string.IsNullOrEmpty(count) ? string.Empty : $" x{count}";
        hotbarLabels[i].text = $"{i + 1}. {hotbar.GetSlotLabel(i, registry)}{suffix}";
        hotbarLabels[i].color = selected ? new Color(1f, 0.92f, 0.45f) : Color.white;
      }
    }

    void UpdateDebug()
    {
      if (debugText == null || scroller == null)
      {
        return;
      }

      HexCoord hex = scroller.PlayerWorldHex;
      bool creative = PlayerGameplayState.Instance != null && PlayerGameplayState.Instance.CreativeMode;
      string tool = toolState != null ? toolState.ActiveTool.ToString() : "Hand";
      string perf = profiler != null ? profiler.BuildSummary() : string.Empty;

      debugText.text =
        $"Debug (F3)\n" +
        $"Hex ({hex.Q}, {hex.R})  creative {creative}  tool {tool}\n" +
        $"Edge {scroller.HexWorld.DistanceToEdge(hex)}  cache {scroller.HexWorld.DataCache.CachedCellCount}\n" +
        $"Chunks {chunkManager?.LoadedChunkCount}  queue {chunkManager?.PendingMeshJobs}\n" +
        $"Sun {celestial?.SunHeight:0.00}  TOD {celestial?.TimeOfDay:0.00}\n" +
        $"{perf}\n" +
        $"LMB break | RMB place | MMB pick | T tool | F5/F6 save";
    }

    void UpdateBreakBar()
    {
      if (breakSlider == null || blockInteractor == null)
      {
        return;
      }

      bool show = blockInteractor.IsBreaking;
      breakSlider.gameObject.SetActive(show);
      if (show)
      {
        breakSlider.value = blockInteractor.BreakProgress;
      }
    }

    void UpdateMinimap()
    {
      if (minimapTexture == null || scroller?.HexWorld == null || settings == null)
      {
        return;
      }

      HexCoord center = scroller.PlayerWorldHex;
      int radius = MinimapSize / 2;
      var pixels = new Color32[MinimapSize * MinimapSize];

      for (int y = 0; y < MinimapSize; y++)
      {
        for (int x = 0; x < MinimapSize; x++)
        {
          int dq = x - radius;
          int dr = y - radius;
          HexCoord hex = center.Add(new HexCoord(dq, dr));
          Color32 color = new Color32(20, 20, 30, 255);
          if (scroller.HexWorld.IsInsideWorld(hex))
          {
            float elevation = settings.SeaLevelLayer;
            if (scroller.HexWorld.TryGetColumn(hex, out BlockColumn column))
            {
              elevation = column.SurfaceHeight;
            }

            float t = Mathf.InverseLerp(settings.SeaLevelLayer - 20f, settings.SeaLevelLayer + 24f, elevation);
            color = Color32.Lerp(
              new Color32(38, 90, 190, 255),
              new Color32(90, 165, 60, 255),
              t);
          }

          if (dq == 0 && dr == 0)
          {
            color = new Color32(255, 240, 120, 255);
          }

          pixels[y * MinimapSize + x] = color;
        }
      }

      minimapTexture.SetPixels32(pixels);
      minimapTexture.Apply(false);
    }
  }
}
