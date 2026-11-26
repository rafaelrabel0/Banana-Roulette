using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Tutorial dirigido. Nao usa o GameManager: roda o proprio roteiro com os
    /// mesmos macacos, banana e HUD, porque cada passo precisa acontecer na hora
    /// certa — inclusive a explosao e o erro.
    ///
    /// O roteiro mostra, nesta ordem:
    ///   1. uma rodada completa do jogador, com arremesso
    ///   2. o pavio acabando na mao de um bot, que explode
    ///   3. um pavio novo, mais curto, queimando a vista
    ///   4. um erro de proposito, e o pedaco de pavio que o erro custa
    /// </summary>
    public class TutorialDirector : MonoBehaviour
    {
        private TuningConfig _config;
        private List<MonkeySlot> _slots;
        private List<TurnSolver> _solvers;
        private BananaCarrier _banana;
        private HudController _hud;
        private FuseTimer _fuse;
        private CameraShake _shake;
        private Light _flashLight;

        private Image _speechBox;
        private Text _speech;
        private Text _prompt;
        private Text _burnPopup;
        private GameObject _endButtons;

        private int _holderIndex;
        private bool _missedThisStep;
        private bool _passed;
        private ArrowDirection _chosenSide;
        private int _progress;
        private bool _lendo;   // lendo as falas antes de jogar: o pavio para em um terco
        private bool _naVez;   // digitando a sequencia: o pavio nao chega ao fim

        static string T(string pt, string en) { return Loc.Current == Language.Portuguese ? pt : en; }
        static string Teclas { get { return GameSettings.UseLetters ? "W A S D" : T("as setas", "the arrow keys"); } }
        static string Lados { get { return GameSettings.UseLetters ? "A / D" : T("seta para a esquerda ou para a direita", "left or right arrow"); } }

        public void Setup(TuningConfig config, List<MonkeySlot> slots, List<TurnSolver> solvers,
            BananaCarrier banana, HudController hud, FuseTimer fuse, CameraShake shake,
            Light flashLight, Transform canvas)
        {
            _config = config;
            _slots = slots;
            _solvers = solvers;
            _banana = banana;
            _hud = hud;
            _fuse = fuse;
            _shake = shake;
            _flashLight = flashLight;

            BuildUi(canvas);
            StartCoroutine(RunTutorial());
        }

        // ------------------------------------------------------------------
        // Interface do tutorial
        // ------------------------------------------------------------------

        private void BuildUi(Transform canvas)
        {
            _speechBox = UiFactory.CreatePanel(canvas, "TutorialBox", new Color(0.02f, 0.09f, 0.06f, 0.94f));
            // no topo, logo abaixo do pavio: e para la que a fala aponta
            UiFactory.Anchor(_speechBox, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -196f), new Vector2(1520f, 186f));

            _speech = UiFactory.CreateText(_speechBox.transform, "Speech", "", 31,
                TextAnchor.MiddleCenter, Color.white);
            UiFactory.Anchor(_speech, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(1440f, 126f));
            _speech.horizontalOverflow = HorizontalWrapMode.Wrap;

            _prompt = UiFactory.CreateText(_speechBox.transform, "Prompt", "", 23,
                TextAnchor.MiddleCenter, Palette.BananaYellow);
            UiFactory.Anchor(_prompt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1400f, 32f));

            // Abaixo do contador de macacos, que mora no mesmo canto.
            var title = UiFactory.CreateText(canvas, "TutTitle", "", 28,
                TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Anchor(title, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(34f, -76f), new Vector2(500f, 40f));
            title.text = Loc.Get("tut.title");

            var skip = UiFactory.CreateText(canvas, "TutSkip", "", 24,
                TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.45f));
            UiFactory.Anchor(skip, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-28f, 22f), new Vector2(400f, 34f));
            skip.text = Loc.Get("tut.skip");

            // Aviso flutuante de pavio queimado, no centro alto.
            _burnPopup = UiFactory.CreateText(canvas, "BurnPopup", "", 44,
                TextAnchor.MiddleCenter, Palette.DynamiteRed);
            UiFactory.Anchor(_burnPopup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(900f, 60f));
            _burnPopup.text = "";

            BuildEndButtons(canvas);
        }

        private void BuildEndButtons(Transform canvas)
        {
            _endButtons = new GameObject("TutEnd", typeof(RectTransform));
            _endButtons.transform.SetParent(canvas, false);
            UiFactory.Anchor(_endButtons.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 258f), new Vector2(900f, 80f));

            Text playLabel, menuLabel;

            var play = UiFactory.CreateButton(_endButtons.transform, "PlayNow", Loc.Get("tut.done"),
                new Vector2(340f, 66f), 30, Palette.BananaYellow,
                new Color32(0x24, 0x18, 0x0A, 0xFF), out playLabel);
            UiFactory.Anchor(play, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-180f, 0f), new Vector2(340f, 66f));
            play.onClick.AddListener(() => AppEntry.GoTo(AppEntry.Screen.Game));

            var menu = UiFactory.CreateButton(_endButtons.transform, "BackMenu", Loc.Get("tut.menu"),
                new Vector2(340f, 66f), 30, new Color32(0x25, 0x3A, 0x2E, 0xFF), Color.white,
                out menuLabel);
            UiFactory.Anchor(menu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(180f, 0f), new Vector2(340f, 66f));
            menu.onClick.AddListener(() => AppEntry.GoTo(AppEntry.Screen.Menu));

            _endButtons.SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                AppEntry.GoTo(AppEntry.Screen.Menu);
                return;
            }

            if (_fuse != null && _banana != null)
            {
                float burned = _fuse.IsRunning || _fuse.Remaining < _fuse.Total
                    ? _fuse.BurnedFraction
                    : 0f;

                _banana.UpdateFuseVisual(burned);
                if (_hud != null && _hud.TopFuse != null) _hud.TopFuse.SetProgress(burned);
                MonkeySlot.Tensao = burned;

                // Enquanto ele le, o pavio queima de verdade, mas para em um terco: tem de sobrar tempo para jogar.
                if (_lendo && _fuse.IsRunning && _fuse.BurnedFraction >= 1f / 3f) _fuse.Stop();
                // E na vez dele o pavio nunca chega ao fim: aqui e para aprender, nao para perder.
                if (_naVez && _fuse.IsRunning && _fuse.Remaining < 4f) _fuse.Stop();
            }

            if (_prompt != null && !string.IsNullOrEmpty(_prompt.text))
            {
                float a = 0.55f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.45f;
                _prompt.color = new Color(Palette.BananaYellow.r, Palette.BananaYellow.g,
                    Palette.BananaYellow.b, a);
            }
        }

        // ------------------------------------------------------------------
        // Roteiro
        // ------------------------------------------------------------------

        private IEnumerator RunTutorial()
        {
            _hud.SetAliveCount(_slots.Count, _config.monkeyCount);
            _hud.SetEscHint("");

            // Continuacao da abertura: a bomba cai do alto na mao do jogador, ele se assusta e comeca o malabarismo.
            _holderIndex = 0;
            _speechBox.gameObject.SetActive(false);
            _banana.transform.position = _slots[0].HandPosition + Vector3.up * 14f;
            _slots[0].Receber();
            yield return _banana.FlyTo(_slots[0], 0.45f);
            _banana.SnapTo(_slots[0]);
            _slots[0].SetHolding(true);
            _speechBox.gameObject.SetActive(true);

            // O pavio ja comeca a queimar enquanto ele le (e para em um terco, ver Update).
            _fuse.StartFuse(48f);
            if (_hud.TopFuse != null) _hud.TopFuse.ResetFuse();
            _lendo = true;

            yield return SayText(T("Rápido! Você é um macaco e uma bomba caiu no seu colo. Seja ágil e aperte os comandos a tempo para jogar a bomba para o macaco ao lado.",
                                   "Quick! You are a monkey and a bomb just landed in your lap. Be fast and press the commands in time to throw the bomb to the monkey next to you."));
            yield return SayText(T("Este cordão aqui em cima é o pavio da bomba. Ele está queimando agora mesmo. Quanto mais você demora, menos pavio sobra.",
                                   "This cord up here is the bomb's fuse. It is burning right now. The longer you take, the less fuse is left."));
            yield return SayText(T("Cada erro queima um pedaço do pavio de uma vez. Erre muitas vezes e a bomba explode na sua mão.",
                                   "Each mistake burns a chunk of the fuse at once. Miss too many times and the bomb blows up in your hands."));
            yield return SayText(T("No meio da roda vai aparecer uma fila de teclas. Aperte uma por uma, da esquerda para a direita, usando " + Teclas + ".",
                                   "A row of keys will show up in the middle of the circle. Press them one by one, left to right, using " + Teclas + "."));

            // ---- A vez do jogador: digitar a sequencia e arremessar ----
            _lendo = false;
            _fuse.Resume();
            _naVez = true;
            yield return PlayerTurn(null, 3, false);
            _naVez = false;

            _fuse.Stop();
            yield return SayText(T("Isso! A bomba agora é problema do vizinho. Ela continua passando de mão em mão, e o pavio não para.",
                                   "That's it! The bomb is now your neighbour's problem. It keeps going from hand to hand, and the fuse never stops."));
            _fuse.Resume();

            // ---- Os bots jogam ate o pavio acabar na mao de um deles ----
            yield return BotsPlayUntilBoom();

            yield return SayText(T("Quando o pavio acaba, quem está com a bomba explode e sai da roda. Depois de cada explosão o pavio fica mais curto e a fila de teclas, mais comprida.",
                                   "When the fuse runs out, whoever holds the bomb blows up and leaves the circle. After each explosion the fuse gets shorter and the row of keys gets longer."));
            SpeakText(T("O último macaco na roda vence. Boa sorte!", "The last monkey in the circle wins. Good luck!"));

            _hud.ClearTurnVisuals();
            _hud.SetStatus("", Color.white);
            _prompt.text = "";
            _endButtons.SetActive(true);
        }

        /// <summary>
        /// Um turno do jogador de verdade, com o PlayerInputHandler real.
        /// Se <paramref name="requireMiss"/>, o passo so avanca quando ele errar
        /// de proposito — e o erro cobra pavio, na frente dele.
        /// </summary>
        private IEnumerator PlayerTurn(string speechKey, int length, bool requireMiss)
        {
            var solver = _solvers[0] as PlayerInputHandler;
            if (solver == null) yield break;

            var sequence = SequenceGenerator.Generate(length);

            if (speechKey != null) Speak(speechKey);
            else SpeakText(T("Sua vez! Aperte a primeira tecla da fila, a que está amarela.", "Your turn! Press the first key in the row, the yellow one."));
            _prompt.text = "";
            _hud.SetStatus(Loc.Get("game.yourturn"), Palette.BananaYellow);
            _hud.ShowSequence(sequence, false);

            _missedThisStep = false;
            _passed = false;
            _progress = 0;

            solver.ClearListeners();
            solver.OnArrowHit += progress =>
            {
                _progress = progress;
                _hud.RegisterHit(progress);
                _slots[0].Acertou();
                if (speechKey == null && progress < length)
                    SpeakText(T("Boa! Agora a próxima. Faltam " + (length - progress) + ".", "Nice! Now the next one. " + (length - progress) + " to go."));

                // No passo do erro, acertar nao serve: pede de novo.
                if (requireMiss && !_missedThisStep) Speak("tut.wrongonpurpose");
            };
            solver.OnArrowMissed += () =>
            {
                _missedThisStep = true;
                _progress = 0;
                OnMiss();
                if (speechKey == null)
                    SpeakText(T("Errou a tecla! Olhe lá em cima: o pavio queimou um pedaço. A fila recomeça do início.", "Wrong key! Look up there: the fuse burned a chunk. The row starts over."));
            };
            solver.OnPassChosen += side => { _chosenSide = side; _passed = true; };

            solver.BeginTurn(sequence, -1);

            if (requireMiss)
            {
                // Se ele acertar a sequencia inteira sem errar, o turno reinicia
                // com uma sequencia nova. Sem isto o tutorial travava esperando
                // um erro que nao viria mais depois da escolha de lado.
                while (!_missedThisStep)
                {
                    if (solver.IsChoosingSide)
                    {
                        solver.EndTurn();
                        sequence = SequenceGenerator.Generate(length);
                        _hud.ShowSequence(sequence, false);
                        Speak("tut.wrongonpurpose");
                        solver.BeginTurn(sequence, -1);
                    }
                    yield return null;
                }

                solver.EndTurn();
                solver.ClearListeners();
                yield break;
            }

            bool hintShown = false;
            while (!_passed)
            {
                if (!hintShown && solver.IsChoosingSide)
                {
                    if (speechKey != null) Speak("tut.5");
                    else SpeakText(T("Fila completa! Agora escolha para quem jogar: " + Lados + ".", "Row complete! Now pick who gets it: " + Lados + "."));
                    _hud.ShowSideChoice(true);
                    hintShown = true;
                }
                yield return null;
            }

            solver.EndTurn();
            solver.ClearListeners();
            _hud.ShowSideChoice(false);

            yield return PassTo(NextIndex(_holderIndex, _chosenSide));
        }

        private void OnMiss()
        {
            _slots[_holderIndex].FlashError();
            StartCoroutine(_banana.Fumble(_config.errorLockoutSeconds));

            _fuse.Burn(_config.errorFusePenaltySeconds);
            _banana.FlashBurn();
            if (_hud.TopFuse != null) _hud.TopFuse.FlashBurn();
            if (_shake != null) _shake.Shake(0.2f, 0.16f);

            _hud.FlashMiss();
            StartCoroutine(BurnPopup());
        }

        private IEnumerator BurnPopup()
        {
            _burnPopup.text = Loc.Get("tut.fuseburn");

            float t = 0f;
            var rect = _burnPopup.GetComponent<RectTransform>();
            Vector2 home = new Vector2(0f, 160f);

            while (t < 1.1f)
            {
                t += Time.deltaTime;
                float k = t / 1.1f;
                rect.anchoredPosition = home + new Vector2(0f, k * 46f);
                _burnPopup.color = new Color(Palette.DynamiteRed.r, Palette.DynamiteRed.g,
                    Palette.DynamiteRed.b, 1f - k);
                yield return null;
            }

            _burnPopup.text = "";
            rect.anchoredPosition = home;
        }

        private IEnumerator PassTo(int target)
        {
            _slots[_holderIndex].SetHolding(false);
            _slots[_holderIndex].Arremessar(_slots[target].transform.position);
            _slots[target].Receber();
            _holderIndex = target;
            _slots[target].SetHolding(true);
            _hud.ClearTurnVisuals();
            yield return _banana.FlyTo(_slots[target], _config.passFlightSeconds);
        }

        /// <summary>
        /// Os bots passam entre si ate o roteiro decidir que o pavio acabou.
        /// A explosao e forcada para cair num bot, nunca no jogador: aqui o
        /// objetivo e mostrar a regra, nao derrotar quem esta aprendendo.
        /// </summary>
        private IEnumerator BotsPlayUntilBoom()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                _hud.SetStatus(Loc.Format("game.holder", _holderIndex + 1), Color.white);
                yield return new WaitForSeconds(1.15f);

                int next = NextIndex(_holderIndex, pass % 2 == 0 ? ArrowDirection.Right : ArrowDirection.Left);
                if (next == 0) next = NextIndex(next, ArrowDirection.Right); // pula o jogador
                yield return PassTo(next);
            }

            _hud.SetStatus(Loc.Format("game.holder", _holderIndex + 1), Color.white);
            yield return new WaitForSeconds(0.9f);

            // Fim do pavio, na hora marcada pelo roteiro.
            _fuse.Burn(_fuse.Remaining + 1f);
            yield return Explode(_holderIndex);
        }

        private IEnumerator Explode(int index)
        {
            var victim = _slots[index];

            _hud.ClearTurnVisuals();
            _banana.gameObject.SetActive(false);

            if (_shake != null) _shake.Shake(0.45f, 0.55f);
            if (_flashLight != null) StartCoroutine(Flash(victim.HandPosition));
            if (_hud.TopFuse != null) StartCoroutine(_hud.TopFuse.Explode());

            victim.Explode();
            _hud.SetStatus(Loc.Format("game.exploded", index + 1), Palette.DynamiteRed);
            _hud.SetAliveCount(CountAlive(), _config.monkeyCount);

            yield return new WaitForSeconds(1.3f);
        }

        private IEnumerator Flash(Vector3 position)
        {
            _flashLight.transform.position = position;
            _flashLight.enabled = true;

            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                _flashLight.intensity = Mathf.Lerp(28f, 0f, t / 0.35f);
                yield return null;
            }

            _flashLight.enabled = false;
        }

        // ------------------------------------------------------------------
        // Falas
        // ------------------------------------------------------------------

        private void Speak(string key)
        {
            _speech.text = Loc.Get(key);
        }

        private void SpeakText(string text)
        {
            _speech.text = text;
        }

        private IEnumerator SayText(string text)
        {
            SpeakText(text);
            _prompt.text = Loc.Get("tut.next");
            yield return null; // evita consumir o mesmo toque duas vezes seguidas
            var keyboard = Keyboard.current;
            while (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            {
                keyboard = Keyboard.current;
                yield return null;
            }
            _prompt.text = "";
        }

        private IEnumerator Say(string key, bool waitForSpace = true)
        {
            Speak(key);

            if (!waitForSpace)
            {
                _prompt.text = "";
                yield break;
            }

            _prompt.text = Loc.Get("tut.next");

            // Evita consumir o mesmo toque duas vezes seguidas.
            yield return null;

            var keyboard = Keyboard.current;
            while (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            {
                keyboard = Keyboard.current;
                yield return null;
            }

            _prompt.text = "";
        }

        // ------------------------------------------------------------------

        private int NextIndex(int from, ArrowDirection side)
        {
            int step = side == ArrowDirection.Right ? 1 : -1;
            int count = _slots.Count;

            for (int i = 1; i < count; i++)
            {
                int index = ((from + step * i) % count + count) % count;
                if (_slots[index].IsAlive) return index;
            }

            return from;
        }

        private int CountAlive()
        {
            int count = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsAlive) count++;
            }
            return count;
        }
    }
}
