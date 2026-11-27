using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// HUD da partida: fileira de teclas, contador de macacos, quem esta com a
    /// banana e a tela de fim. As teclas somem nas rodadas finais, de proposito.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        private const float KeySize = 84f;
        private const float KeySpacing = 98f;

        private RectTransform _keyRow;
        private Image _keyBackdrop;
        private readonly List<KeyCapWidget> _keys = new List<KeyCapWidget>();

        private Text _aliveText;
        private Text _statusText;
        private Text _hintText;
        private Text _fuseDebugText;
        private Text _escHintText;

        private GameObject _endPanel;
        private Text _endTitle;
        private Text _endSubtitle;
        private Image _memoryVeil;

        private TopFuseBar _topFuse;
        private TuningConfig _config;
        private bool _hudHidden;

        /// <summary>O pavio do topo, para o jogo sincronizar com a banana 3D.</summary>
        public TopFuseBar TopFuse { get { return _topFuse; } }
        private int _aliveCache;
        private int _totalCache;

        public void Build(Canvas canvas, TuningConfig config)
        {
            _config = config;

            // Os elementos nascem DENTRO deste objeto, nao soltos no canvas.
            // Solto, esconder o HUD no menu nao escondia nada: o objeto vazio
            // desligava e os textos continuavam na tela.
            var root = transform;

            // O pavio da rodada mora no topo. Todo o resto do texto desce para
            // baixo dele.
            _topFuse = TopFuseBar.Build(root);

            _aliveText = UiFactory.CreateText(root, "AliveCounter", "", 32,
                TextAnchor.UpperLeft, Palette.BananaYellow);
            UiFactory.Anchor(_aliveText, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(34f, -116f), new Vector2(520f, 46f));

            _fuseDebugText = UiFactory.CreateText(root, "FuseDebug", "", 26,
                TextAnchor.UpperRight, Palette.DynamiteRed);
            UiFactory.Anchor(_fuseDebugText, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-34f, -116f), new Vector2(420f, 40f));

            _statusText = UiFactory.CreateText(root, "Status", "", 38,
                TextAnchor.UpperCenter, Color.white);
            UiFactory.Anchor(_statusText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -114f), new Vector2(1100f, 54f));

            _escHintText = UiFactory.CreateText(root, "EscHint", "", 24,
                TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.45f));
            UiFactory.Anchor(_escHintText, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-28f, 22f), new Vector2(360f, 34f));

            // Fileira de teclas, no meio da roda. Embaixo ela caia por cima do
            // macaco do jogador e sumia no chao escuro.
            var rowGo = new GameObject("KeyRow", typeof(RectTransform));
            rowGo.transform.SetParent(root, false);
            _keyRow = rowGo.GetComponent<RectTransform>();
            _keyRow.anchorMin = new Vector2(0.5f, 0.5f);
            _keyRow.anchorMax = new Vector2(0.5f, 0.5f);
            _keyRow.pivot = new Vector2(0.5f, 0.5f);
            _keyRow.anchoredPosition = new Vector2(0f, -30f);
            _keyRow.sizeDelta = new Vector2(1200f, KeySize);

            _keyBackdrop = UiFactory.CreateImage(_keyRow, "Backdrop", UiFactory.RoundedSprite,
                new Color(0f, 0f, 0f, 0.45f));
            _keyBackdrop.type = Image.Type.Sliced;
            UiFactory.Anchor(_keyBackdrop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(KeySize, KeySize));
            _keyBackdrop.gameObject.SetActive(false);

            _hintText = UiFactory.CreateText(root, "Hint", "", 34,
                TextAnchor.MiddleCenter, Palette.BananaYellow);
            UiFactory.Anchor(_hintText, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(1000f, 50f));

            _memoryVeil = UiFactory.CreateImage(root, "MemoryVeil", UiFactory.BoxSprite,
                new Color(0f, 0f, 0f, 0f));
            UiFactory.Anchor(_memoryVeil, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            _memoryVeil.transform.SetAsFirstSibling();

            BuildEndPanel(root);
            Loc.OnLanguageChanged += RefreshStaticText;
        }

        /// <summary>
        /// Na partida em 3D a fila de setas sobe para o meio da roda, que fica vazio:
        /// no lugar antigo ela tapava a bomba na mao do jogador.
        /// </summary>
        /// <summary>Esconde ou mostra a fila de setas (o menu da partida esconde enquanto esta aberto).</summary>
        private bool _endShown, _endSuspended;

        private RectTransform _puRow;
        private Text _puLabel, _invText, _livesText;
        private readonly List<KeyCapWidget> _puKeys = new List<KeyCapWidget>();

        private Text NovoTexto(string nome, int tamanho, TextAnchor ancora, Color cor)
        {
            var t = UiFactory.CreateText(transform, nome, "", tamanho, ancora, cor);
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.9f);
            o.effectDistance = new Vector2(2.5f, -2.5f);
            return t;
        }

        private void GarantePoder()
        {
            if (_puRow != null) return;
            var go = new GameObject("PowerUpRow", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _puRow = go.GetComponent<RectTransform>();
            _puRow.anchorMin = _puRow.anchorMax = _puRow.pivot = new Vector2(0.5f, 0.5f);
            _puRow.sizeDelta = new Vector2(900f, 60f);

            _puLabel = NovoTexto("PowerUpLabel", 28, TextAnchor.MiddleCenter, new Color(0.45f, 0.92f, 1f));
            UiFactory.Anchor(_puLabel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 40f));

            _invText = NovoTexto("Inventario", 28, TextAnchor.LowerLeft, new Color(0.45f, 0.92f, 1f));
            UiFactory.Anchor(_invText, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 26f), new Vector2(1300f, 44f));

            _livesText = NovoTexto("Vidas", 30, TextAnchor.UpperLeft, new Color(1f, 0.45f, 0.4f));
            UiFactory.Anchor(_livesText, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -158f), new Vector2(520f, 44f));
        }

        /// <summary>Mostra, acima da fila da bomba, o poder oferecido neste turno e as teclas para pega-lo.</summary>
        public void ShowPowerUp(string nome, List<ArrowDirection> sequence)
        {
            GarantePoder();
            HidePowerUp();
            Vector2 baseRow = _keyRow != null ? _keyRow.anchoredPosition : Vector2.zero;
            _puRow.anchoredPosition = baseRow + new Vector2(0f, 138f);
            _puLabel.rectTransform.anchoredPosition = baseRow + new Vector2(0f, 196f);
            _puLabel.text = (Loc.Current == Language.Portuguese ? "PODER: " : "POWER-UP: ") + nome.ToUpperInvariant();
            const float tam = 58f, passo = 68f;
            float largura = (sequence.Count - 1) * passo;
            for (int i = 0; i < sequence.Count; i++)
            {
                var key = KeyCapWidget.Create(_puRow, sequence[i], tam);
                key.GetComponent<RectTransform>().anchoredPosition = new Vector2(-largura * 0.5f + i * passo, 0f);
                key.SetState(i == 0 ? KeyCapWidget.State.Current : KeyCapWidget.State.Pending);
                _puKeys.Add(key);
            }
        }

        public void PowerUpHit(int progress)
        {
            for (int i = 0; i < _puKeys.Count; i++)
            {
                if (_puKeys[i] == null) continue;
                _puKeys[i].SetState(i < progress ? KeyCapWidget.State.Hit : i == progress ? KeyCapWidget.State.Current : KeyCapWidget.State.Pending);
            }
        }

        public void HidePowerUp()
        {
            for (int i = 0; i < _puKeys.Count; i++) if (_puKeys[i] != null) Destroy(_puKeys[i].gameObject);
            _puKeys.Clear();
            if (_puLabel != null) _puLabel.text = "";
        }

        /// <summary>Canto de baixo: o poder guardado e a tecla para usar. Texto vazio esconde.</summary>
        public void SetInventory(string texto) { GarantePoder(); _invText.text = texto; }

        public void SetLives(int vidas)
        {
            GarantePoder();
            _livesText.text = (Loc.Current == Language.Portuguese ? "VIDAS: " : "LIVES: ") + vidas;
        }

        /// <summary>
        /// Tira a tela de fim de partida da frente (sem esquecer que ela existe) enquanto o menu da partida
        /// esta aberto, e devolve quando ele fecha.
        /// </summary>
        public void SuspendEnd(bool suspend)
        {
            _endSuspended = suspend;
            if (_endPanel != null) _endPanel.SetActive(_endShown && !suspend);
        }

        public void SetKeysVisible(bool visible)
        {
            if (_keyRow != null) _keyRow.gameObject.SetActive(visible);
            if (_hintText != null) _hintText.gameObject.SetActive(visible);
            if (_puRow != null) _puRow.gameObject.SetActive(visible);
            if (_puLabel != null) _puLabel.gameObject.SetActive(visible);
        }

        public void Layout3D()
        {
            if (_keyRow != null) _keyRow.anchoredPosition = new Vector2(0f, 48f);
            if (_hintText != null) _hintText.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            if (_statusText != null) _statusText.rectTransform.anchoredPosition = new Vector2(0f, -132f);
            // o cenario agora e claro: contorno escuro para o texto nao sumir
            foreach (var t in new[] { _statusText, _aliveText, _hintText, _escHintText, _fuseDebugText })
            {
                if (t == null || t.GetComponent<Outline>() != null) continue;
                var o = t.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.9f);
                o.effectDistance = new Vector2(2.5f, -2.5f);
            }
        }

        private void OnDestroy()
        {
            Loc.OnLanguageChanged -= RefreshStaticText;
        }

        private void RefreshStaticText()
        {
            SetAliveCount(_aliveCache, _totalCache);
            if (_escHintText != null) _escHintText.text = Loc.Get("game.esc");
        }

        private void BuildEndPanel(Transform root)
        {
            _endPanel = new GameObject("EndPanel", typeof(RectTransform));
            _endPanel.transform.SetParent(root, false);
            UiFactory.Anchor(_endPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Escurece sem apagar: os macacos tostados no chao sao a piada da
            // tela de fim e precisam continuar visiveis atras do texto.
            var bg = UiFactory.CreateImage(_endPanel.transform, "Dim", UiFactory.BoxSprite,
                new Color(0.02f, 0.08f, 0.05f, 0.62f));
            UiFactory.Anchor(bg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            _endTitle = UiFactory.CreateText(_endPanel.transform, "Title", "", 92,
                TextAnchor.MiddleCenter, Palette.BananaYellow);
            UiFactory.Anchor(_endTitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1500f, 130f));

            _endSubtitle = UiFactory.CreateText(_endPanel.transform, "Subtitle", "", 34,
                TextAnchor.MiddleCenter, Color.white);
            UiFactory.Anchor(_endSubtitle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1500f, 60f));

            _endPanel.SetActive(false);
        }

        // ---------- Sequencia ----------

        public void ShowSequence(List<ArrowDirection> sequence, bool hidden)
        {
            // Corta qualquer animacao do turno anterior. Sem isto, a corrotina
            // da rodada de memoria terminava em cima do turno seguinte e
            // deixava texto fantasma na tela.
            StopAllCoroutines();

            _hudHidden = hidden;
            ClearKeys();

            if (hidden)
            {
                StartCoroutine(FlashThenHide(sequence));
                return;
            }

            BuildKeyRow(sequence);
            HighlightFrom(0);
        }

        private IEnumerator FlashThenHide(List<ArrowDirection> sequence)
        {
            BuildKeyRow(sequence);
            HighlightFrom(0);
            _memoryVeil.color = new Color(0f, 0f, 0f, 0f);

            yield return new WaitForSeconds(1.2f);

            ClearKeys();
            _hintText.text = Loc.Get("game.memory");
            _memoryVeil.color = new Color(0f, 0f, 0f, 0.22f);
        }

        private void BuildKeyRow(List<ArrowDirection> sequence)
        {
            float totalWidth = (sequence.Count - 1) * KeySpacing;

            if (_keyBackdrop != null)
            {
                _keyBackdrop.gameObject.SetActive(true);
                _keyBackdrop.rectTransform.sizeDelta =
                    new Vector2(totalWidth + KeySize + 56f, KeySize + 52f);
                _keyBackdrop.transform.SetAsFirstSibling();
            }

            for (int i = 0; i < sequence.Count; i++)
            {
                var key = KeyCapWidget.Create(_keyRow, sequence[i], KeySize);
                var rect = key.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-totalWidth * 0.5f + i * KeySpacing, 0f);
                _keys.Add(key);
            }
        }

        private void ClearKeys()
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                if (_keys[i] != null) Destroy(_keys[i].gameObject);
            }
            _keys.Clear();

            if (_keyBackdrop != null) _keyBackdrop.gameObject.SetActive(false);
        }

        /// <summary>Marca o que ja foi acertado e destaca a proxima tecla.</summary>
        public void HighlightFrom(int progress)
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                if (_keys[i] == null) continue;

                if (i < progress) _keys[i].SetState(KeyCapWidget.State.Hit);
                else if (i == progress) _keys[i].SetState(KeyCapWidget.State.Current);
                else _keys[i].SetState(KeyCapWidget.State.Pending);
            }
        }

        /// <summary>Acertou: a tecla afunda de verdade, com animacao.</summary>
        public void RegisterHit(int progress)
        {
            int pressed = progress - 1;
            if (pressed >= 0 && pressed < _keys.Count && _keys[pressed] != null)
            {
                _keys[pressed].SetState(KeyCapWidget.State.Hit);
                _keys[pressed].PlayPress();
            }

            for (int i = 0; i < _keys.Count; i++)
            {
                if (_keys[i] == null || i <= pressed) continue;
                _keys[i].SetState(i == progress ? KeyCapWidget.State.Current : KeyCapWidget.State.Pending);
            }
        }

        public void FlashMiss()
        {
            StartCoroutine(FlashMissRoutine());
        }

        private IEnumerator FlashMissRoutine()
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                if (_keys[i] == null) continue;
                _keys[i].SetState(KeyCapWidget.State.Wrong);
                _keys[i].PlayShake();
            }

            Vector2 home = _keyRow.anchoredPosition;
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                _keyRow.anchoredPosition = home + new Vector2(Random.Range(-10f, 10f), 0f);
                yield return null;
            }
            _keyRow.anchoredPosition = home;

            if (!_hudHidden) HighlightFrom(0);
        }

        public void ShowSideChoice(bool show)
        {
            if (show)
            {
                _hintText.text = Loc.Get("game.chooseside") + "   <   >";
                _hintText.color = Palette.BananaYellow;
            }
            else if (!_hudHidden)
            {
                _hintText.text = "";
            }
        }

        // ---------- Estado geral ----------

        public void SetAliveCount(int alive, int total)
        {
            _aliveCache = alive;
            _totalCache = total;
            _aliveText.text = Loc.Format("game.monkeys", alive, total);
        }

        public void SetStatus(string message, Color color)
        {
            _statusText.text = message;
            _statusText.color = color;
        }

        public void SetHint(string message)
        {
            _hintText.text = message;
            _hintText.color = Palette.BananaYellow;
        }

        public void SetEscHint(string message)
        {
            if (_escHintText != null) _escHintText.text = message;
        }

        public void SetFuseDebug(bool enabled, float remaining)
        {
            if (_fuseDebugText == null) return;
            _fuseDebugText.text = enabled ? string.Format("pavio: {0:0.0}s", remaining) : "";
        }

        public void ClearTurnVisuals()
        {
            StopAllCoroutines();
            ClearKeys();
            _hintText.text = "";
            _memoryVeil.color = new Color(0f, 0f, 0f, 0f);
            _hudHidden = false;
        }

        public void ShowEnd(bool playerWon)
        {
            _endTitle.text = playerWon ? Loc.Get("game.yousurvived") : Loc.Get("game.youexploded");
            _endTitle.color = playerWon ? Palette.BananaYellow : Palette.DynamiteRed;
            _endSubtitle.text = Loc.Get("game.again");
            _endShown = true;
            _endPanel.SetActive(!_endSuspended);
        }

        public void HideEnd()
        {
            _endShown = false;
            _endPanel.SetActive(false);
        }

        /// <summary>Esconde o HUD inteiro enquanto o menu esta na frente.</summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
