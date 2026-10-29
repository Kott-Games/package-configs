using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kott.Configs.Basics
{
    /// <summary>
    ///     Builds the sample's uGUI in code, so the scene stays minimal. Not part of the Configs API: real games would
    ///     author this UI as prefabs.
    /// </summary>
    public static class BasicsUI
    {
        /// <summary>Background of panels.</summary>
        public static readonly Color PanelColor = new(0.08f, 0.09f, 0.12f, 0.92f);

        /// <summary>Default text color.</summary>
        public static readonly Color TextColor = new(0.92f, 0.93f, 0.95f);

        private static Font _font;

        private static Font Font =>
            _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Creates a screen-space overlay canvas (1920×1080 reference) under <paramref name="parent" />.</summary>
        public static RectTransform CreateCanvas(string name, Transform parent)
        {
            GameObject canvasObject = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return (RectTransform)canvasObject.transform;
        }

        /// <summary>Creates the EventSystem uGUI needs for input, under <paramref name="parent" />.</summary>
        public static GameObject CreateEventSystem(Transform parent)
        {
            GameObject eventSystem = new("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(parent, false);
            return eventSystem;
        }

        /// <summary>
        ///     Creates a vertical layout of <paramref name="width" />, anchored to the top center of its parent; children
        ///     stack top to bottom and the column grows with them.
        /// </summary>
        public static RectTransform Column(Transform parent, float width, Color? background = null,
            string name = "Column")
        {
            RectTransform column = CreateRect(name, parent);
            column.anchorMin = column.anchorMax = column.pivot = new Vector2(0.5f, 1f);
            column.anchoredPosition = new Vector2(0f, -40f);
            column.sizeDelta = new Vector2(width, 0f);

            if (background.HasValue)
            {
                column.gameObject.AddComponent<Image>().color = background.Value;
            }

            VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return column;
        }

        /// <summary>
        ///     Creates a horizontal row inside a <see cref="Column" />. With <paramref name="expand" />, children share the
        ///     width equally (buttons); without, they take their preferred or flexible width (icon + text).
        /// </summary>
        public static RectTransform Row(Transform parent, bool expand = true, Color? background = null,
            string name = "Row")
        {
            RectTransform row = CreateRect(name, parent);
            if (background.HasValue)
            {
                row.gameObject.AddComponent<Image>().color = background.Value;
            }

            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expand;
            layout.childForceExpandHeight = false;
            if (!expand) layout.padding = new RectOffset(12, 12, 12, 12);
            return row;
        }

        /// <summary>Creates a text label. In a layout group it takes its preferred height and the remaining width.</summary>
        public static Text Label(Transform parent, string text, int fontSize = 28,
            TextAnchor alignment = TextAnchor.MiddleLeft, string name = "Label")
        {
            RectTransform rect = CreateRect(name, parent);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = TextColor;
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            rect.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return label;
        }

        /// <summary>Creates a square image of <paramref name="size" />; set its <c>sprite</c> later.</summary>
        public static Image Icon(Transform parent, float size, Color color)
        {
            RectTransform rect = CreateRect("Icon", parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.preserveAspect = true;

            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = size;
            layout.minHeight = layout.preferredHeight = size;
            layout.flexibleWidth = 0f;
            return image;
        }

        /// <summary>Creates a button with a label that invokes <paramref name="onClick" />.</summary>
        public static Button Button(Transform parent, string text, Action onClick, int fontSize = 26)
        {
            RectTransform rect = CreateRect(text, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.24f, 0.32f);

            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            rect.gameObject.AddComponent<LayoutElement>().minHeight = fontSize * 2.2f;

            Text label = Label(rect, text, fontSize, TextAnchor.MiddleCenter);
            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return button;
        }

        /// <summary>Destroys all children of <paramref name="parent" />.</summary>
        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }
    }
}
