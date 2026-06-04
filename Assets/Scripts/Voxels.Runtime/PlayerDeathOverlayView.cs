using System;
using UnityEngine;
using UnityEngine.UI;

namespace Voxels.Runtime
{
    public sealed class PlayerDeathOverlayView : MonoBehaviour
    {
        [SerializeField] float autoRespawnSeconds = 4f;

        Canvas canvas;
        Image dimPanel;
        Text titleText;
        Text hintText;
        Button respawnButton;
        float shownAt = -1f;
        bool visible;

        public event Action RespawnRequested;

        public bool IsVisible => visible;

        public void Show()
        {
            EnsureUi();
            visible = true;
            shownAt = Time.unscaledTime;
            canvas.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Hide()
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(false);
            }

            visible = false;
            shownAt = -1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (!visible)
            {
                return;
            }

            float remaining = autoRespawnSeconds - (Time.unscaledTime - shownAt);
            if (hintText != null)
            {
                hintText.text = remaining > 0.5f
                    ? $"Respawning in {Mathf.CeilToInt(remaining)}…  or click Respawn"
                    : "Respawning…";
            }

            if (remaining <= 0f)
            {
                RequestRespawn();
            }
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

            var canvasObject = new GameObject("DeathOverlayCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            dimPanel = CreatePanel(canvasObject.transform, "Dim", new Color(0.02f, 0.02f, 0.06f, 0.82f));
            Stretch(dimPanel.rectTransform);

            var panel = CreatePanel(canvasObject.transform, "Card", new Color(0.08f, 0.08f, 0.12f, 0.95f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(420f, 220f);

            titleText = CreateText(panel.transform, "Title", 32, TextAnchor.UpperCenter);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            titleRect.sizeDelta = new Vector2(-32f, 48f);
            titleText.text = "You died";
            titleText.color = new Color(0.95f, 0.35f, 0.32f);

            hintText = CreateText(panel.transform, "Hint", 16, TextAnchor.MiddleCenter);
            var hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0.35f);
            hintRect.anchorMax = new Vector2(1f, 0.65f);
            hintRect.offsetMin = new Vector2(16f, 0f);
            hintRect.offsetMax = new Vector2(-16f, 0f);
            hintText.color = new Color(0.85f, 0.85f, 0.9f);

            var buttonObject = new GameObject("RespawnButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.22f, 0.48f, 0.28f, 1f);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 24f);
            buttonRect.sizeDelta = new Vector2(200f, 44f);
            respawnButton = buttonObject.GetComponent<Button>();
            respawnButton.onClick.AddListener(RequestRespawn);

            var buttonLabel = CreateText(buttonObject.transform, "Label", 18, TextAnchor.MiddleCenter);
            Stretch(buttonLabel.rectTransform);
            buttonLabel.text = "Respawn";
            buttonLabel.color = Color.white;

            canvas.gameObject.SetActive(false);
        }

        void RequestRespawn()
        {
            if (!visible)
            {
                return;
            }

            Hide();
            RespawnRequested?.Invoke();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static Image CreatePanel(Transform parent, string name, Color color)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            var image = panelObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return text;
        }
    }
}
