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
            UiFactory.Anchor(_speechBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(1520f, 148f));

            _speech = UiFactory.CreateText(_speechBox.transform, "Speech", "", 33,
                TextAnchor.MiddleCenter, Color.white);
            UiFactory.Anchor(_speech, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(1420f, 94f));
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

            _holderIndex = 0; // comeca na mao do jogador
            _banana.SnapTo(_slots[0]);
            _slots[0].SetHolding(true);
            _fuse.StartFuse(999f);
            _fuse.Stop();

            yield return Say("tut.1");
            yield return Say("tut.2");
            yield return Say("tut.3");

            // ---- Rodada 1: o jogador digita e passa ----
            _fuse.StartFuse(20f);
            if (_hud.TopFuse != null) _hud.TopFuse.ResetFuse();
            yield return PlayerTurn("tut.4", 3, false);

            _fuse.Stop();
            yield return Say("tut.6");
            _fuse.Resume();

            // ---- Os bots jogam ate o pavio acabar na mao de um deles ----
            yield return BotsPlayUntilBoom();

            yield return Say("tut.7");
            yield return Say("tut.8");

            // ---- Rodada 2: pavio novo, mais curto, e o erro de proposito ----
            _holderIndex = 0;
            _banana.gameObject.SetActive(true);
            _banana.SnapTo(_slots[0]);
            _slots[0].SetHolding(true);
            _fuse.StartFuse(26f);
            if (_hud.TopFuse != null) _hud.TopFuse.ResetFuse();

            yield return PlayerTurn("tut.9", 4, true);

            _fuse.Stop();
            yield return Say("tut.10");
            yield return Say("tut.11");
            _fuse.Resume();

            yield return PlayerTurn("tut.12", 4, false);

            _fuse.Stop();
            yield return Say("tut.13", false);

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

            Speak(speechKey);
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

                // No passo do erro, acertar nao serve: pede de novo.
                if (requireMiss && !_missedThisStep) Speak("tut.wrongonpurpose");
            };
            solver.OnArrowMissed += () =>
            {
                _missedThisStep = true;
                _progress = 0;
                OnMiss();
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
                    Speak("tut.5");
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
