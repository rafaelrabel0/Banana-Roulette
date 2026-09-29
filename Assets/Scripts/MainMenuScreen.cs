using System;
using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Menu principal. Simples de proposito: tres botoes empilhados sobre a
    /// roda de macacos, que fica girando ao fundo.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        private Text _playLabel;
        private Text _tutorialLabel;
        private Text _settingsLabel;
        private Text _subtitle;
        private Text _tagline;
        private GameObject _root;

        public static MainMenuScreen Build(Transform canvas, Action onPlay, Action onTutorial,
            Action onSettings)
        {
            var go = new GameObject("MainMenu", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            UiFactory.Anchor(go.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var screen = go.AddComponent<MainMenuScreen>();
            screen._root = go;

            // Veu escuro para o texto descolar da selva ao fundo.
            var veil = UiFactory.CreateImage(go.transform, "Veil", UiFactory.BoxSprite,
                new Color(0.02f, 0.07f, 0.05f, 0.72f));
            UiFactory.Anchor(veil, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            screen._subtitle = UiFactory.CreateText(go.transform, "Subtitle",
                Loc.Get("menu.subtitle"), 30, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.72f));
            UiFactory.Anchor(screen._subtitle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 40f));

            var title = UiFactory.CreateText(go.transform, "Title", "BANANA ROULETTE", 96,
                TextAnchor.MiddleCenter, Palette.BananaYellow);
            UiFactory.Anchor(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1600f, 120f));

            var size = new Vector2(430f, 82f);
            float top = -30f;

            var play = UiFactory.CreateButton(go.transform, "Play", Loc.Get("menu.play"),
                size, 40, Palette.BananaYellow, new Color32(0x24, 0x18, 0x0A, 0xFF),
                out screen._playLabel);
            UiFactory.Anchor(play, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, top), size);
            play.onClick.AddListener(() => onPlay());

            var tutorial = UiFactory.CreateButton(go.transform, "Tutorial", Loc.Get("menu.tutorial"),
                size, 34, new Color32(0x25, 0x3A, 0x2E, 0xFF), Color.white,
                out screen._tutorialLabel);
            UiFactory.Anchor(tutorial, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, top - 98f), size);
            tutorial.onClick.AddListener(() => onTutorial());

            var settings = UiFactory.CreateButton(go.transform, "Settings", Loc.Get("menu.settings"),
                size, 34, new Color32(0x25, 0x3A, 0x2E, 0xFF), Color.white,
                out screen._settingsLabel);
            UiFactory.Anchor(settings, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, top - 196f), size);
            settings.onClick.AddListener(() => onSettings());

            screen._tagline = UiFactory.CreateText(go.transform, "Tagline", Loc.Get("menu.tagline"),
                28, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.5f));
            UiFactory.Anchor(screen._tagline, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(1200f, 40f));

            Loc.OnLanguageChanged += screen.Refresh;
            return screen;
        }

        private void OnDestroy()
        {
            Loc.OnLanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_playLabel != null) _playLabel.text = Loc.Get("menu.play");
            if (_tutorialLabel != null) _tutorialLabel.text = Loc.Get("menu.tutorial");
            if (_settingsLabel != null) _settingsLabel.text = Loc.Get("menu.settings");
            if (_subtitle != null) _subtitle.text = Loc.Get("menu.subtitle");
            if (_tagline != null) _tagline.text = Loc.Get("menu.tagline");
        }

        public void SetVisible(bool visible)
        {
            if (_root != null) _root.SetActive(visible);
        }
    }
}
