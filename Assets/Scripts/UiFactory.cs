using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Constroi os elementos de UI por codigo. Evita depender de assets
    /// importados: o prototipo roda em qualquer cena vazia.
    /// </summary>
    public static class UiFactory
    {
        private static Sprite _arrowSprite;
        private static Sprite _roundedSprite;
        private static Sprite _roundedBoxSprite;
        private static Sprite _circleSprite;
        private static Sprite _glowSprite;
        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        /// <summary>Triangulo apontando para cima, desenhado em textura na mao.</summary>
        public static Sprite ArrowSprite
        {
            get
            {
                if (_arrowSprite != null) return _arrowSprite;

                const int size = 96;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;

                float centerX = size * 0.5f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Em Texture2D, y = 0 e a BASE da imagem. Por isso o
                        // eixo e invertido aqui: sem esta inversao o triangulo
                        // sai apontando para baixo e todas as setas ficam com o
                        // sentido trocado em 180 graus.
                        float fromTop = 1f - y / (float)(size - 1);
                        float halfWidth = Mathf.Lerp(0.03f, 0.5f, fromTop) * size;
                        bool inside = Mathf.Abs(x - centerX) <= halfWidth;
                        tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
                    }
                }

                tex.Apply();
                _arrowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _arrowSprite;
            }
        }

        /// <summary>
        /// Retangulo de cantos arredondados, fatiado nas bordas. Serve de
        /// keycap, painel e botao sem distorcer o canto quando estica.
        /// </summary>
        public static Sprite RoundedSprite
        {
            get
            {
                if (_roundedBoxSprite != null) return _roundedBoxSprite;

                const int size = 48;
                const float radius = 14f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Distancia ate o retangulo interno: define o canto redondo.
                        float dx = Mathf.Max(0f, Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius)));
                        float dy = Mathf.Max(0f, Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius)));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);

                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                tex.Apply();
                _roundedBoxSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                return _roundedBoxSprite;
            }
        }

        /// <summary>Disco cheio. Usado na faisca, que redondo de verdade le melhor.</summary>
        public static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null) return _circleSprite;
                _circleSprite = BuildRadial(64, 1f);
                return _circleSprite;
            }
        }

        /// <summary>Brilho radial que desvanece para fora. Quadrado nao serve de glow.</summary>
        public static Sprite GlowSprite
        {
            get
            {
                if (_glowSprite != null) return _glowSprite;
                _glowSprite = BuildRadial(64, 2.2f);
                return _glowSprite;
            }
        }

        private static Sprite BuildRadial(int size, float falloff)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;

                    float alpha = falloff <= 1f
                        ? Mathf.Clamp01((1f - dist) * radius)          // disco de borda limpa
                        : Mathf.Pow(Mathf.Clamp01(1f - dist), falloff); // brilho que desvanece

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Quadrado branco para fundos e paineis.</summary>
        public static Sprite BoxSprite
        {
            get
            {
                if (_roundedSprite != null) return _roundedSprite;

                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                for (int y = 0; y < 4; y++)
                    for (int x = 0; x < 4; x++)
                        tex.SetPixel(x, y, Color.white);
                tex.Apply();

                _roundedSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                return _roundedSprite;
            }
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize,
            TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = DefaultFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            return text;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        /// <summary>
        /// Botao do menu: painel arredondado com rotulo centralizado. Devolve o
        /// Button para ligar o clique e o Text para trocar de idioma depois.
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label,
            Vector2 size, int fontSize, Color fill, Color textColor, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            image.color = fill;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            labelText = CreateText(go.transform, "Label", label, fontSize,
                TextAnchor.MiddleCenter, textColor);
            Anchor(labelText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            return button;
        }

        /// <summary>Painel de fundo arredondado.</summary>
        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var image = CreateImage(parent, name, RoundedSprite, color);
            image.type = Image.Type.Sliced;
            return image;
        }

        public static RectTransform Anchor(Component component, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = component.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
