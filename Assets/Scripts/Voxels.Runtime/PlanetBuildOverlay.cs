using UnityEngine;
using UnityEngine.UI;

namespace Voxels.Runtime
{
    public sealed class PlanetBuildOverlay : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [SerializeField] Slider progressSlider;
        [SerializeField] Text statusText;

        public bool IsVisible { get; private set; }

        public static PlanetBuildOverlay Ensure()
        {
            PlanetBuildOverlay existing = FindAnyObjectByType<PlanetBuildOverlay>();
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject("PlanetBuildOverlay");
            DontDestroyOnLoad(root);

            var canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(root.transform, false);
            var canvasComponent = canvasObject.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComponent.sortingOrder = 500;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasObject.transform, false);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var sliderObject = new GameObject("Progress");
            sliderObject.transform.SetParent(panel.transform, false);
            var slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.2f, 0.48f);
            sliderRect.anchorMax = new Vector2(0.8f, 0.52f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            var background = new GameObject("Background");
            background.transform.SetParent(sliderObject.transform, false);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(8f, 8f);
            fillAreaRect.offsetMax = new Vector2(-8f, -8f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.75f, 0.45f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            var statusObject = new GameObject("Status");
            statusObject.transform.SetParent(panel.transform, false);
            var status = statusObject.AddComponent<Text>();
            status.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status.fontSize = 22;
            status.alignment = TextAnchor.MiddleCenter;
            status.color = Color.white;
            status.text = "Building planet…";
            var statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.54f);
            statusRect.anchorMax = new Vector2(0.9f, 0.62f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            var overlay = root.AddComponent<PlanetBuildOverlay>();
            overlay.canvas = canvasComponent;
            overlay.progressSlider = slider;
            overlay.statusText = status;
            overlay.SetVisible(true);
            return overlay;
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (canvas != null)
            {
                canvas.enabled = visible;
            }
        }

        public void Report(float progress, string status)
        {
            if (progressSlider != null)
            {
                progressSlider.value = Mathf.Clamp01(progress);
            }

            if (statusText != null && !string.IsNullOrEmpty(status))
            {
                statusText.text = status;
            }
        }
    }
}
