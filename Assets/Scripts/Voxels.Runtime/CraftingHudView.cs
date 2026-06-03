using UnityEngine;
using UnityEngine.UI;

namespace Voxels.Runtime
{
    /// <summary>Single craft action + feedback — does not list recipes.</summary>
    public sealed class CraftingHudView : MonoBehaviour
    {
        CraftingSystem crafting;
        Text feedbackText;
        CanvasGroup panelGroup;

        public void Initialize(CraftingSystem craftingSystem)
        {
            crafting = craftingSystem;
            EnsureUi();
        }

        void EnsureUi()
        {
            if (feedbackText != null)
            {
                return;
            }

            var canvasObject = new GameObject("CraftingHud", typeof(RectTransform), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            panelGroup = canvasObject.GetComponent<CanvasGroup>();
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-16f, 40f);
            rect.sizeDelta = new Vector2(140f, 72f);

            var buttonObject = new GameObject("CraftButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvasObject.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.85f);
            buttonObject.GetComponent<Button>().onClick.AddListener(() => crafting?.TryCraft());

            var buttonLabelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            buttonLabelObject.transform.SetParent(buttonObject.transform, false);
            var buttonLabel = buttonLabelObject.GetComponent<Text>();
            buttonLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonLabel.fontSize = 14;
            buttonLabel.alignment = TextAnchor.MiddleCenter;
            buttonLabel.color = Color.white;
            buttonLabel.text = "Craft (G)";
            var labelRect = buttonLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var feedbackObject = new GameObject("Feedback", typeof(RectTransform), typeof(Text));
            feedbackObject.transform.SetParent(canvasObject.transform, false);
            feedbackText = feedbackObject.GetComponent<Text>();
            feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            feedbackText.fontSize = 12;
            feedbackText.alignment = TextAnchor.UpperCenter;
            feedbackText.color = new Color(0.9f, 0.92f, 0.95f, 0.95f);
            var feedbackRect = feedbackText.rectTransform;
            feedbackRect.anchorMin = new Vector2(0f, 0f);
            feedbackRect.anchorMax = new Vector2(1f, 0.45f);
            feedbackRect.offsetMin = Vector2.zero;
            feedbackRect.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (feedbackText == null || crafting == null)
            {
                return;
            }

            feedbackText.text = crafting.LastMessage ?? string.Empty;
        }
    }
}
