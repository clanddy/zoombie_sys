using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AshenDao.UI
{
    public static class PhoneTheme
    {
        public static readonly Color Screen = new Color(0.035f, 0.05f, 0.047f, 1f);
        public static readonly Color Frame = new Color(0.075f, 0.085f, 0.08f, 1f);
        public static readonly Color Card = new Color(0.09f, 0.125f, 0.115f, 0.96f);
        public static readonly Color CardLight = new Color(0.13f, 0.18f, 0.16f, 0.9f);
        public static readonly Color Text = new Color(0.92f, 0.9f, 0.84f, 1f);
        public static readonly Color Muted = new Color(0.55f, 0.63f, 0.59f, 1f);
        public static readonly Color Jade = new Color(0.56f, 0.85f, 0.74f, 1f);
        public static readonly Color Gold = new Color(0.85f, 0.71f, 0.42f, 1f);
        public static readonly Color Danger = new Color(0.88f, 0.44f, 0.38f, 1f);
        public static readonly Color Qi = new Color(0.5f, 0.85f, 0.9f, 1f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);
    }

    public static class UiFactory
    {
        private static Font font;
        private static readonly Dictionary<int, Sprite> roundedSprites = new Dictionary<int, Sprite>();

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>("Fonts/AshenDaoSans");
                    if (font == null)
                    {
                        font = Font.CreateDynamicFontFromOSFont(new[]
                        {
                            "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", "Noto Sans CJK SC",
                            "Arial Unicode MS", "Arial"
                        }, 22);
                    }
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform FullScreen(Transform parent, string name, Color color)
        {
            var rt = NewRect(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return rt;
        }

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 size, Vector2 position, Color color,
            float radius = 0f)
        {
            var rt = NewRect(parent, name);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            // position 约定为“距左上角 (x 向右, y 向下)”
            rt.anchoredPosition = new Vector2(position.x, -position.y);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            if (radius > 0f)
            {
                image.sprite = RoundedSprite((int)radius);
                image.type = Image.Type.Sliced;
            }
            return rt;
        }

        public static RectTransform CenterPanel(Transform parent, string name, Vector2 size, Color color, float radius)
        {
            var rt = NewRect(parent, name);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            if (radius > 0f)
            {
                image.sprite = RoundedSprite((int)radius);
                image.type = Image.Type.Sliced;
            }
            return rt;
        }

        public static Text Label(Transform parent, string name, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = true)
        {
            var rt = NewRect(parent, name);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.lineSpacing = 1.08f;
            text.text = content;
            return text;
        }

        public static void Place(Text text, float x, float y, float width, float height)
        {
            var rt = (RectTransform)text.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        public static void Place(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        public static Button Button(Transform parent, string name, string label, int fontSize, Vector2 size,
            Vector2 position, Action onClick, Color background = default, Color textColor = default)
        {
            var rt = Panel(parent, name, size, position, background == default ? PhoneTheme.CardLight : background, 10f);
            var button = rt.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.9f, 0.85f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);
            button.colors = colors;
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(() => onClick?.Invoke());
            var text = Label(rt, "Label", label, fontSize, textColor == default ? PhoneTheme.Text : textColor,
                TextAnchor.MiddleCenter, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        public static Image Bar(Transform parent, string name, float x, float y, float width, float height,
            Color fillColor, float value)
        {
            var track = Panel(parent, name, new Vector2(width, height), new Vector2(x, y),
                new Color(1f, 1f, 1f, 0.08f), height * 0.5f);
            var fillRt = NewRect(track, "Fill");
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var image = fillRt.gameObject.AddComponent<Image>();
            image.color = fillColor;
            image.sprite = RoundedSprite(Mathf.Max(2, (int)(height * 0.5f)));
            image.type = Image.Type.Sliced;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillAmount = Mathf.Clamp01(value);
            return image;
        }

        public static RectTransform Scroll(Transform parent, float x, float y, float width, float height,
            out ScrollRect scroll, out RectTransform content)
        {
            var viewport = Panel(parent, "Viewport", new Vector2(width, height), new Vector2(x, y),
                new Color(0f, 0f, 0f, 0f), 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            content = NewRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            content.sizeDelta = new Vector2(0f, 0f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(2, 2, 2, 12);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scroll = scrollRect;
            return viewport;
        }

        public static RectTransform Card(Transform parent, float height)
        {
            var rt = NewRect(parent, "Card");
            var image = rt.gameObject.AddComponent<Image>();
            image.color = PhoneTheme.Card;
            image.sprite = RoundedSprite(12);
            image.type = Image.Type.Sliced;
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return rt;
        }

        public static Sprite RoundedSprite(int radius)
        {
            if (roundedSprites.TryGetValue(radius, out var cached)) return cached;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Rounded_" + radius;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            var r = Mathf.Clamp(radius, 0, size / 2);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(r - x, 0f) + Mathf.Max(x - (size - 1 - r), 0f);
                    var dy = Mathf.Max(r - y, 0f) + Mathf.Max(y - (size - 1 - r), 0f);
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) - r;
                    var alpha = Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            roundedSprites[radius] = sprite;
            return sprite;
        }

        public static Sprite RadialSprite(int size = 128)
        {
            if (roundedSprites.TryGetValue(-size, out var cached)) return cached;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Radial_" + size;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            roundedSprites[-size] = sprite;
            return sprite;
        }

        public static Sprite GradientSprite(Color top, Color bottom, int width = 256, int height = 512)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                var t = y / (float)(height - 1);
                var color = Color.Lerp(bottom, top, t);
                for (var x = 0; x < width; x++)
                {
                    var noise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.03f;
                    pixels[y * width + x] = new Color(color.r + noise, color.g + noise, color.b + noise, 1f);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public static string ShortNumber(float value)
        {
            return Mathf.RoundToInt(value).ToString();
        }
    }
}
