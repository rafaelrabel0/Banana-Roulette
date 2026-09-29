using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Uma tecla de teclado desenhada com profundidade: uma base escura fixa e
    /// um keycap por cima que AFUNDA quando a seta e acertada.
    ///
    /// Vale mais que um triangulo chapado porque o jogo inteiro e teclado: ver a
    /// tecla descer e o mesmo gesto que a mao acabou de fazer.
    /// </summary>
    public class KeyCapWidget : MonoBehaviour
    {
        public enum State { Pending, Current, Hit, Wrong }

        private static readonly Color BaseColor = new Color32(0x16, 0x20, 0x1A, 0xFF);
        private static readonly Color CapPending = new Color32(0xCB, 0xD6, 0xCD, 0xFF);
        private static readonly Color CapCurrent = Palette.BananaYellow;
        private static readonly Color CapHit = Palette.HitGreen;
        private static readonly Color CapWrong = Palette.DynamiteRed;
        private static readonly Color GlyphDark = new Color32(0x1E, 0x2A, 0x23, 0xFF);

        private RectTransform _capRect;
        private Image _capImage;
        private Image _glyph;
        private float _depth;
        private State _state = State.Pending;
        private Coroutine _motion;

        /// <summary>Monta a tecla inteira por codigo e devolve o widget pronto.</summary>
        public static KeyCapWidget Create(Transform parent, ArrowDirection direction, float size)
        {
            var root = new GameObject("Key_" + direction, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(size, size);

            var widget = root.AddComponent<KeyCapWidget>();
            widget._depth = Mathf.Round(size * 0.15f);

            // Base: a lateral da tecla, que fica aparecendo enquanto o cap esta em cima.
            var baseImage = UiFactory.CreateImage(root.transform, "Base", UiFactory.RoundedSprite, BaseColor);
            baseImage.type = Image.Type.Sliced;
            var baseRect = baseImage.GetComponent<RectTransform>();
            baseRect.anchorMin = new Vector2(0.5f, 0.5f);
            baseRect.anchorMax = new Vector2(0.5f, 0.5f);
            baseRect.pivot = new Vector2(0.5f, 0.5f);
            baseRect.sizeDelta = new Vector2(size, size);
            baseRect.anchoredPosition = new Vector2(0f, -widget._depth);

            // Cap: o topo da tecla. E ele que desce.
            widget._capImage = UiFactory.CreateImage(root.transform, "Cap", UiFactory.RoundedSprite, CapPending);
            widget._capImage.type = Image.Type.Sliced;
            widget._capRect = widget._capImage.GetComponent<RectTransform>();
            widget._capRect.anchorMin = new Vector2(0.5f, 0.5f);
            widget._capRect.anchorMax = new Vector2(0.5f, 0.5f);
            widget._capRect.pivot = new Vector2(0.5f, 0.5f);
            widget._capRect.sizeDelta = new Vector2(size, size);
            widget._capRect.anchoredPosition = Vector2.zero;

            // Seta gravada no topo da tecla.
            widget._glyph = UiFactory.CreateImage(widget._capRect, "Glyph", UiFactory.ArrowSprite, GlyphDark);
            var glyphRect = widget._glyph.GetComponent<RectTransform>();
            glyphRect.anchorMin = new Vector2(0.5f, 0.5f);
            glyphRect.anchorMax = new Vector2(0.5f, 0.5f);
            glyphRect.pivot = new Vector2(0.5f, 0.5f);
            glyphRect.sizeDelta = new Vector2(size * 0.52f, size * 0.52f);
            glyphRect.localRotation = Quaternion.Euler(0f, 0f, direction.ScreenRotation());

            widget.SetState(State.Pending);
            return widget;
        }

        public void SetState(State state)
        {
            _state = state;

            switch (state)
            {
                case State.Pending:
                    _capImage.color = CapPending;
                    _glyph.color = GlyphDark;
                    MoveCapTo(0f);
                    transform.localScale = Vector3.one;
                    break;

                case State.Current:
                    _capImage.color = CapCurrent;
                    _glyph.color = GlyphDark;
                    MoveCapTo(0f);
                    transform.localScale = Vector3.one * 1.12f;
                    break;

                case State.Hit:
                    _capImage.color = CapHit;
                    _glyph.color = GlyphDark;
                    // Fica afundada: o progresso vira relevo, nao so cor.
                    MoveCapTo(-_depth);
                    transform.localScale = Vector3.one * 0.94f;
                    break;

                case State.Wrong:
                    _capImage.color = CapWrong;
                    _glyph.color = Color.white;
                    transform.localScale = Vector3.one;
                    break;
            }
        }

        /// <summary>Afunda e volta. Usado quando a tecla e acertada.</summary>
        public void PlayPress()
        {
            if (_motion != null) StopCoroutine(_motion);
            _motion = StartCoroutine(PressRoutine());
        }

        private IEnumerator PressRoutine()
        {
            // Desce rapido...
            float t = 0f;
            while (t < 0.05f)
            {
                t += Time.deltaTime;
                MoveCapTo(Mathf.Lerp(0f, -_depth, t / 0.05f));
                yield return null;
            }

            MoveCapTo(-_depth);

            // ...e sobe um pouco menos do que desceu, porque fica marcada como acertada.
            float target = _state == State.Hit ? -_depth : 0f;
            t = 0f;
            while (t < 0.10f)
            {
                t += Time.deltaTime;
                MoveCapTo(Mathf.Lerp(-_depth, target, t / 0.10f));
                yield return null;
            }

            MoveCapTo(target);
            _motion = null;
        }

        /// <summary>Tremida curta de tecla errada.</summary>
        public void PlayShake()
        {
            if (_motion != null) StopCoroutine(_motion);
            _motion = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            Vector2 home = _capRect.anchoredPosition;
            float t = 0f;
            while (t < 0.24f)
            {
                t += Time.deltaTime;
                float falloff = 1f - Mathf.Clamp01(t / 0.24f);
                _capRect.anchoredPosition = home + new Vector2(
                    Mathf.Sin(t * 70f) * 7f * falloff, 0f);
                yield return null;
            }

            _capRect.anchoredPosition = home;
            _motion = null;
        }

        private void MoveCapTo(float y)
        {
            if (_capRect == null) return;
            _capRect.anchoredPosition = new Vector2(0f, y);
        }

        /// <summary>Pulsacao leve na tecla que o jogador precisa apertar agora.</summary>
        private void Update()
        {
            if (_state != State.Current) return;
            float pulse = 1.12f + Mathf.Sin(Time.time * 6f) * 0.04f;
            transform.localScale = Vector3.one * pulse;
        }
    }
}
