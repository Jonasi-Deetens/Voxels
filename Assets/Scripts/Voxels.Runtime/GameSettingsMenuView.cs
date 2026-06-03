using UnityEngine;
using UnityEngine.UI;
using Voxels.World;

namespace Voxels.Runtime
{
    public sealed class GameSettingsMenuView : MonoBehaviour
    {
        const string PrefPreset = "voxels_perf_preset";
        const string PrefFog = "voxels_fog";
        const string PrefWeather = "voxels_weather";
        const string PrefClouds = "voxels_cloud_density";

        Canvas canvas;
        bool visible;
        WorldSettings settings;
        WeatherSystem weatherSystem;
        HexSkyCloudController cloudController;
        ProceduralSkyController skyController;
        Text presetLabel;

        public void Initialize(
            WorldSettings worldSettings,
            WeatherSystem weather,
            HexSkyCloudController clouds,
            ProceduralSkyController sky)
        {
            settings = worldSettings;
            weatherSystem = weather;
            cloudController = clouds;
            skyController = sky;
            LoadPrefs();
            EnsureUi();
            SetVisible(false);
        }

        void Update()
        {
            if (GameInput.WasSettingsMenuPressedThisFrame())
            {
                SetVisible(!visible);
            }
        }

        void LoadPrefs()
        {
            if (settings == null)
            {
                return;
            }

            int preset = PlayerPrefs.GetInt(PrefPreset, (int)settings.PerformancePreset);
            settings.SetPerformancePreset((WorldPerformancePreset)Mathf.Clamp(preset, 0, 2));
        }

        void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            var root = new GameObject("SettingsMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var panel = CreatePanel(root.transform, "Panel", new Color(0.06f, 0.08f, 0.12f, 0.92f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(320f, 280f);

            CreateLabel(panelRect, "Settings (Esc)", 18, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -12f), new Vector2(-12f, -40f));
            presetLabel = CreateLabel(panelRect, "Preset", 14, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -48f), new Vector2(-12f, -72f));

            CreateButton(panelRect, "Low", new Vector2(12f, -80f), () => ApplyPreset(WorldPerformancePreset.Low));
            CreateButton(panelRect, "Balanced", new Vector2(112f, -80f), () => ApplyPreset(WorldPerformancePreset.Balanced));
            CreateButton(panelRect, "High", new Vector2(212f, -80f), () => ApplyPreset(WorldPerformancePreset.High));

            CreateToggle(panelRect, "Distance fog", PrefFog, 1, -120f, v => skyController?.SetFogEnabled(v));

            CreateToggle(panelRect, "Weather", PrefWeather, 1, -155f, v => weatherSystem?.SetEnabled(v));
            CreateToggle(panelRect, "Dense clouds", PrefClouds, 0, -190f, v =>
            {
                if (cloudController != null)
                {
                    cloudController.SetCloudDensityScale(v ? 1.35f : 1f);
                }
            });

            CreateLabel(panelRect, "See PERFORMANCE.md in repo root", 11,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(12f, 12f), new Vector2(-12f, 36f));
        }

        void ApplyPreset(WorldPerformancePreset preset)
        {
            settings?.SetPerformancePreset(preset);
            PlayerPrefs.SetInt(PrefPreset, (int)preset);
            if (presetLabel != null)
            {
                presetLabel.text = $"Preset: {preset}";
            }
        }

        void SetVisible(bool show)
        {
            visible = show;
            if (canvas != null)
            {
                canvas.enabled = show;
            }

            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = show;
        }

        static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static Text CreateLabel(Transform parent, string text, int size, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.color = Color.white;
            label.text = text;
            var rect = label.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return label;
        }

        static void CreateButton(Transform parent, string caption, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(90f, 28f);
            go.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.32f, 0.95f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            CreateLabel(go.transform, caption, 13, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        static void CreateToggle(Transform parent, string caption, string prefKey, int defaultOn, float y, System.Action<bool> onChanged)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(12f, y - 24f);
            rect.offsetMax = new Vector2(-12f, y);
            var toggle = go.GetComponent<Toggle>();
            toggle.isOn = PlayerPrefs.GetInt(prefKey, defaultOn) == 1;
            onChanged?.Invoke(toggle.isOn);
            toggle.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetInt(prefKey, v ? 1 : 0);
                onChanged?.Invoke(v);
            });
            CreateLabel(go.transform, caption, 13, new Vector2(0.05f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
        }
    }
}
