using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Configuracoes: som, video, idioma e a lista de controles.
    /// Tudo em uma tela so, sem aba e sem rolagem.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color32(0x11, 0x22, 0x1A, 0xFA);
        private static readonly Color SectionColor = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
        private static readonly Color RowColor = new Color32(0xE4, 0xEC, 0xE6, 0xFF);
        private static readonly Color ChipOff = new Color32(0x27, 0x3D, 0x31, 0xFF);
        private static readonly Color ChipOn = Palette.BananaYellow;

        private GameObject _root;
        private Action _onBack;

        private readonly List<Action> _refreshers = new List<Action>();

        public static SettingsScreen Build(Transform canvas, Action onBack)
        {
            var go = new GameObject("Settings", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            UiFactory.Anchor(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var screen = go.AddComponent<SettingsScreen>();
            screen._root = go;
            screen._onBack = onBack;

            var veil = UiFactory.CreateImage(go.transform, "Veil", UiFactory.BoxSprite,
                new Color(0.01f, 0.05f, 0.03f, 0.86f));
            UiFactory.Anchor(veil, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            var panel = UiFactory.CreatePanel(go.transform, "Panel", PanelColor);
            UiFactory.Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1480f, 900f));

            var title = UiFactory.CreateText(panel.transform, "Title", "", 54,
                TextAnchor.MiddleCenter, SectionColor);
            UiFactory.Anchor(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(1200f, 64f));
            screen._refreshers.Add(() => title.text = Loc.Get("set.title"));

            screen.BuildAudio(panel.transform);
            screen.BuildVideo(panel.transform);
            screen.BuildLanguage(panel.transform);
            screen.BuildControls(panel.transform);

            Text backLabel;
            var back = UiFactory.CreateButton(panel.transform, "Back", "", new Vector2(300f, 70f), 34,
                Palette.BananaYellow, new Color32(0x24, 0x18, 0x0A, 0xFF), out backLabel);
            UiFactory.Anchor(back, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(300f, 70f));
            back.onClick.AddListener(() => onBack());
            screen._refreshers.Add(() => backLabel.text = Loc.Get("set.back"));

            Loc.OnLanguageChanged += screen.RefreshAll;
            screen.RefreshAll();
            return screen;
        }

        private void OnDestroy()
        {
            Loc.OnLanguageChanged -= RefreshAll;
        }

        private void RefreshAll()
        {
            for (int i = 0; i < _refreshers.Count; i++) _refreshers[i]();
        }

        // ------------------------------------------------------------------
        // Blocos
        // ------------------------------------------------------------------

        // O painel vai de -740 a +740. Coluna esquerda fecha antes do centro e a
        // direita comeca depois dele: sem essa separacao a nota do som passava
        // por cima dos controles.
        private const float LeftX = -680f;
        private const float RightX = 60f;

        private void BuildAudio(Transform panel)
        {
            float y = -150f;

            AddSection(panel, "set.audio", LeftX, y);

            AddRow(panel, "set.music", LeftX, y - 62f);
            AddVolumeBar(panel, LeftX + 4f, y - 104f,
                () => GameSettings.MusicVolume, GameSettings.SetMusic);

            AddRow(panel, "set.sfx", LeftX, y - 156f);
            AddVolumeBar(panel, LeftX + 4f, y - 198f,
                () => GameSettings.SfxVolume, GameSettings.SetSfx);

            var note = UiFactory.CreateText(panel, "AudioNote", "", 21,
                TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.42f));
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiFactory.Anchor(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(LeftX, y - 242f), new Vector2(560f, 64f));
            _refreshers.Add(() => note.text = Loc.Get("set.nosound"));
        }

        private void BuildVideo(Transform panel)
        {
            float y = -500f;

            AddSection(panel, "set.video", LeftX, y);

            // Rotulo em cima e opcoes embaixo, igual ao volume. Lado a lado, os
            // botoes atravessavam o meio do painel e batiam na outra coluna.
            AddRow(panel, "set.fullscreen", LeftX, y - 62f);
            AddSegmented(panel, LeftX + 4f, y - 104f, new[] { "set.off", "set.on" },
                () => GameSettings.Fullscreen ? 1 : 0,
                index => GameSettings.SetFullscreen(index == 1));

            AddRow(panel, "set.quality", LeftX, y - 162f);
            AddSegmented(panel, LeftX + 4f, y - 204f,
                new[] { "set.quality.low", "set.quality.mid", "set.quality.high" },
                () => GameSettings.Quality,
                GameSettings.SetQuality);
        }

        private void BuildLanguage(Transform panel)
        {
            float y = -150f;

            AddSection(panel, "set.language", RightX, y);
            AddSegmented(panel, RightX + 4f, y - 66f, new[] { "set.pt", "set.en" },
                () => (int)Loc.Current,
                index => GameSettings.SetLanguage((Language)index));
        }

        /// <summary>
        /// Os controles sao mostrados com as mesmas teclas que aparecem em jogo.
        /// Reusar o widget evita explicar com palavra o que a tela ja mostra.
        /// </summary>
        private void BuildControls(Transform panel)
        {
            float y = -300f;

            AddSection(panel, "set.controls", RightX, y);

            // Legenda em cima das teclas: o texto ao lado empurrava a linha para
            // fora do painel quando o idioma era o ingles.
            var seqText = UiFactory.CreateText(panel, "CtrlSeqText", "", 24,
                TextAnchor.MiddleLeft, RowColor);
            UiFactory.Anchor(seqText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(RightX, y - 56f), new Vector2(620f, 34f));
            _refreshers.Add(() => seqText.text = Loc.Get("set.controls.seq"));

            // A tecla ancora pelo CENTRO do anchor, nao pelo topo: deixar a
            // linha com 70 de altura e nao somar deslocamento evita que o texto
            // seguinte caia por cima dela.
            var seqRow = MakeAnchor(panel, "CtrlSeqKeys", RightX + 4f, y - 90f, new Vector2(400f, 70f));
            var dirs = new[] { ArrowDirection.Left, ArrowDirection.Up, ArrowDirection.Right, ArrowDirection.Down };
            for (int i = 0; i < dirs.Length; i++)
            {
                var key = KeyCapWidget.Create(seqRow, dirs[i], 56f);
                key.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(-200f + 36f + i * 66f, 0f);
            }

            var passText = UiFactory.CreateText(panel, "CtrlPassText", "", 24,
                TextAnchor.MiddleLeft, RowColor);
            UiFactory.Anchor(passText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(RightX, y - 182f), new Vector2(620f, 34f));
            _refreshers.Add(() => passText.text = Loc.Get("set.controls.pass"));

            var passRow = MakeAnchor(panel, "CtrlPassKeys", RightX + 4f, y - 216f, new Vector2(400f, 70f));
            var sides = new[] { ArrowDirection.Left, ArrowDirection.Right };
            for (int i = 0; i < sides.Length; i++)
            {
                var key = KeyCapWidget.Create(passRow, sides[i], 56f);
                key.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(-200f + 36f + i * 66f, 0f);
            }

            var note = UiFactory.CreateText(panel, "CtrlNote", "", 21,
                TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.42f));
            UiFactory.Anchor(note, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(RightX, y - 300f), new Vector2(620f, 34f));
            _refreshers.Add(() => note.text = Loc.Get("set.controls.note"));
        }

        // ------------------------------------------------------------------
        // Pecas reutilizaveis
        // ------------------------------------------------------------------

        private void AddSection(Transform panel, string key, float x, float y)
        {
            var text = UiFactory.CreateText(panel, "Section_" + key, "", 30,
                TextAnchor.MiddleLeft, SectionColor);
            UiFactory.Anchor(text, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(x, y), new Vector2(600f, 40f));
            _refreshers.Add(() => text.text = Loc.Get(key));
        }

        private void AddRow(Transform panel, string key, float x, float y)
        {
            var text = UiFactory.CreateText(panel, "Row_" + key, "", 26,
                TextAnchor.MiddleLeft, RowColor);
            UiFactory.Anchor(text, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 1f), new Vector2(x, y), new Vector2(400f, 38f));
            _refreshers.Add(() => text.text = Loc.Get(key));
        }

        private Transform MakeAnchor(Transform panel, string name, float x, float y, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(panel, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = size;
            return go.transform;
        }

        /// <summary>Barra de dez blocos clicaveis. Mais simples que montar um Slider por codigo.</summary>
        private void AddVolumeBar(Transform panel, float x, float y, Func<float> read, Action<float> write)
        {
            const int steps = 10;
            var row = MakeAnchor(panel, "Vol", x, y, new Vector2(420f, 30f));
            var blocks = new Image[steps];

            for (int i = 0; i < steps; i++)
            {
                int index = i;
                var go = new GameObject("Step" + i, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
                go.transform.SetParent(row, false);

                var image = go.GetComponent<Image>();
                image.sprite = UiFactory.RoundedSprite;
                image.type = Image.Type.Sliced;
                blocks[i] = image;

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(32f, 26f);
                rect.anchoredPosition = new Vector2(i * 38f, 0f);

                var button = go.GetComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => write((index + 1) / (float)steps));
            }

            Action paint = () =>
            {
                int filled = Mathf.RoundToInt(read() * steps);
                for (int i = 0; i < steps; i++)
                {
                    blocks[i].color = i < filled ? ChipOn : ChipOff;
                }
            };

            // Repinta a cada clique e quando a tela recarrega.
            for (int i = 0; i < steps; i++)
            {
                blocks[i].GetComponent<Button>().onClick.AddListener(() => paint());
            }
            _refreshers.Add(paint);
        }

        /// <summary>Linha de opcoes onde so uma fica acesa.</summary>
        private void AddSegmented(Transform panel, float x, float y, string[] keys,
            Func<int> read, Action<int> write)
        {
            var row = MakeAnchor(panel, "Seg", x, y, new Vector2(520f, 46f));
            var images = new Image[keys.Length];
            var labels = new Text[keys.Length];
            float cursor = 0f;

            for (int i = 0; i < keys.Length; i++)
            {
                int index = i;
                float width = 150f;

                Text label;
                var button = UiFactory.CreateButton(row, "Opt" + i, "", new Vector2(width, 46f), 24,
                    ChipOff, Color.white, out label);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(cursor, 0f);
                cursor += width + 10f;

                images[i] = button.GetComponent<Image>();
                labels[i] = label;
                button.onClick.AddListener(() => write(index));
            }

            Action paint = () =>
            {
                int selected = read();
                for (int i = 0; i < keys.Length; i++)
                {
                    bool on = i == selected;
                    images[i].color = on ? ChipOn : ChipOff;
                    labels[i].color = on ? new Color32(0x24, 0x18, 0x0A, 0xFF) : Color.white;
                    labels[i].text = Loc.Get(keys[i]);
                }
            };

            for (int i = 0; i < keys.Length; i++)
            {
                images[i].GetComponent<Button>().onClick.AddListener(() => paint());
            }
            _refreshers.Add(paint);
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && _onBack != null)
            {
                _onBack();
            }
        }
    }
}
