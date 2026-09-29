using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BananaRoulette
{
    /// <summary>
    /// Maquina de estados da partida:
    ///   Setup -> RoundStart -> Holding -> Passing -> Explosion -> CheckWin -> GameOver
    ///
    /// O fluxo roda como corrotina porque ele e linear de verdade: uma rodada
    /// e uma sequencia de esperas. Nenhum script aqui sabe o que e rede.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum State { Setup, RoundStart, Holding, Passing, Explosion, CheckWin, GameOver }
        public State Current { get; private set; } = State.Setup;

        private TuningConfig _config;
        private List<MonkeySlot> _slots;
        private List<TurnSolver> _solvers;
        private BananaCarrier _banana;
        private HudController _hud;
        private FuseTimer _fuse;
        private CameraShake _shake;
        private Light _flashLight;

        private int _holderIndex;
        private int _lastPasserIndex = -1;

        private bool _running;
        private bool _fuseEnded;
        private bool _turnResolved;
        private ArrowDirection _chosenSide;

        public int AliveCount
        {
            get
            {
                if (_slots == null) return 0;
                int count = 0;
                for (int i = 0; i < _slots.Count; i++)
                {
                    if (_slots[i].IsAlive) count++;
                }
                return count;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        public void Setup(TuningConfig config, List<MonkeySlot> slots, List<TurnSolver> solvers,
            BananaCarrier banana, HudController hud, FuseTimer fuse, CameraShake shake, Light flashLight)
        {
            _config = config;
            _slots = slots;
            _solvers = solvers;
            _banana = banana;
            _hud = hud;
            _fuse = fuse;
            _shake = shake;
            _flashLight = flashLight;

            _fuse.OnFuseEnded += HandleFuseEnded;
        }

        /// <summary>A partida so comeca quando alguem pede. O menu decide a hora.</summary>
        public void StartMatch()
        {
            if (_running) return;
            _running = true;
            StartCoroutine(RunMatch());
        }

        private void HandleFuseEnded()
        {
            _fuseEnded = true;
        }

        private void Trace(string message)
        {
            if (_config != null && _config.verboseLog)
            {
                Debug.Log("[BR] " + message);
            }
        }

        private void Update()
        {
            if (!_running) return;

            if (_fuse != null && _hud != null)
            {
                _hud.SetFuseDebug(_config.showFuseDebug, _fuse.Remaining);

                // Entre rodadas o pavio esta parado: a faisca fica calma em vez
                // de piscar no maximo, que e como o pavio zerado se comportava.
                float burned = _fuse.IsRunning ? _fuse.BurnedFraction : 0f;

                if (_banana != null) _banana.UpdateFuseVisual(burned);

                // A barra do topo anda pela MESMA fracao: e o mesmo pavio, visto
                // de outro jeito.
                if (_hud.TopFuse != null) _hud.TopFuse.SetProgress(burned);
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                AppEntry.GoTo(AppEntry.Screen.Menu);
                return;
            }

            if (Current == State.GameOver && keyboard.enterKey.wasPressedThisFrame)
            {
                AppEntry.GoTo(AppEntry.Screen.Game);
            }
        }

        // ------------------------------------------------------------------
        // Fluxo da partida
        // ------------------------------------------------------------------

        private IEnumerator RunMatch()
        {
            Current = State.Setup;

            // A banana comeca na mao do jogador, nao sorteada: apertar JOGAR
            // tem que virar acao na hora, sem esperar a roda dar meia volta.
            _holderIndex = 0;
            _banana.SnapTo(_slots[_holderIndex]);
            _hud.SetAliveCount(AliveCount, _config.monkeyCount);
            _hud.SetStatus(Loc.Get("game.lit"), Palette.DynamiteRed);
            _hud.SetEscHint(Loc.Get("game.esc"));
            Trace("Partida iniciada com " + AliveCount + " macacos. Primeiro portador: " + _holderIndex);

            yield return new WaitForSeconds(1.2f);

            while (AliveCount > 1)
            {
                yield return RunRound();
            }

            Current = State.GameOver;
            yield return EndMatch();
        }

        /// <summary>
        /// Uma rodada vai de uma explosao ate a proxima. O pavio e sorteado no
        /// inicio e corre por cima de todos os turnos, sem parar nunca.
        /// </summary>
        private IEnumerator RunRound()
        {
            Current = State.RoundStart;

            _fuseEnded = false;
            float duration = _config.RollFuseDuration(AliveCount);
            _fuse.StartFuse(duration);
            if (_hud.TopFuse != null) _hud.TopFuse.ResetFuse();
            Trace(string.Format("Rodada com {0} vivos. Pavio sorteado: {1:0.0}s. Sequencia: {2} setas.",
                AliveCount, duration, _config.SequenceLengthFor(AliveCount)));
            _slots[_holderIndex].SetHolding(true);
            _banana.SnapTo(_slots[_holderIndex]);

            while (!_fuseEnded)
            {
                yield return RunTurn();
            }

            yield return RunExplosion();
        }

        private IEnumerator RunTurn()
        {
            Current = State.Holding;

            var holder = _slots[_holderIndex];
            var solver = _solvers[_holderIndex];

            int length = _config.SequenceLengthFor(AliveCount);
            var sequence = SequenceGenerator.Generate(length);
            bool memoryRound = AliveCount <= _config.hideHudAtAliveCount;

            _turnResolved = false;

            // O HUD so mostra a sequencia do jogador humano. Quando um bot
            // esta com a banana, o que importa e ver a banana girando na roda.
            if (holder.IsHuman)
            {
                _hud.ShowSequence(sequence, memoryRound);
                _hud.SetStatus(Loc.Get("game.yourturn"), Palette.BananaYellow);
            }
            else
            {
                _hud.ClearTurnVisuals();
                _hud.SetStatus(Loc.Format("game.holder", _holderIndex + 1), Color.white);
            }

            solver.ClearListeners();
            solver.OnArrowHit += progress =>
            {
                if (holder.IsHuman && !memoryRound) _hud.RegisterHit(progress);
            };
            solver.OnArrowMissed += () => HandleMiss(holder);
            solver.OnPassChosen += side => { _chosenSide = side; _turnResolved = true; };

            solver.BeginTurn(sequence, _lastPasserIndex);

            bool hintShown = false;
            while (!_turnResolved && !_fuseEnded)
            {
                if (holder.IsHuman && !hintShown)
                {
                    var player = solver as PlayerInputHandler;
                    if (player != null && player.IsChoosingSide)
                    {
                        _hud.ShowSideChoice(true);
                        hintShown = true;
                    }
                }
                yield return null;
            }

            solver.EndTurn();
            solver.ClearListeners();
            _hud.ShowSideChoice(false);

            if (_fuseEnded) yield break;

            yield return PassBanana();
        }

        /// <summary>
        /// Errar nao custa so os milissegundos perdidos: queima um pedaco do
        /// pavio de uma vez. E o que faz pressa e calma brigarem de verdade.
        /// </summary>
        private void HandleMiss(MonkeySlot holder)
        {
            holder.FlashError();
            StartCoroutine(_banana.Fumble(_config.errorLockoutSeconds));

            _fuse.Burn(_config.errorFusePenaltySeconds);
            _banana.FlashBurn();
            if (_hud.TopFuse != null) _hud.TopFuse.FlashBurn();
            if (_shake != null) _shake.Shake(0.16f, 0.12f);

            if (holder.IsHuman)
            {
                _hud.FlashMiss();
            }
        }

        private IEnumerator PassBanana()
        {
            Current = State.Passing;

            int nextIndex = NextAliveIndex(_holderIndex, _chosenSide);
            if (nextIndex < 0) yield break;

            _slots[_holderIndex].SetHolding(false);
            _lastPasserIndex = _holderIndex;

            // A posse muda no instante do arremesso: quem passou a tempo se
            // livrou, mesmo que o pavio acabe com a banana ainda no ar.
            _holderIndex = nextIndex;
            _slots[_holderIndex].SetHolding(true);

            Trace(string.Format("Macaco {0} passou para {1} ({2}).",
                _lastPasserIndex, nextIndex, _chosenSide));

            yield return _banana.FlyTo(_slots[nextIndex], _config.passFlightSeconds);
        }

        private IEnumerator RunExplosion()
        {
            Current = State.Explosion;

            var victim = _slots[_holderIndex];

            for (int i = 0; i < _solvers.Count; i++)
            {
                _solvers[i].EndTurn();
                _solvers[i].ClearListeners();
            }

            _hud.ClearTurnVisuals();
            _banana.gameObject.SetActive(false);

            if (_shake != null) _shake.Shake(0.45f, 0.55f);
            StartCoroutine(FlashRoutine());

            // As duas bananas estouram no mesmo instante: a da mao e a do topo.
            if (_hud.TopFuse != null) StartCoroutine(_hud.TopFuse.Explode());

            victim.Explode();
            Trace(string.Format("EXPLODIU o macaco {0}{1}. Restam {2}.",
                _holderIndex, victim.IsHuman ? " (VOCE)" : "", AliveCount));

            _hud.SetStatus(victim.IsHuman
                ? Loc.Get("game.youexploded")
                : Loc.Format("game.exploded", _holderIndex + 1), Palette.DynamiteRed);
            _hud.SetAliveCount(AliveCount, _config.monkeyCount);

            yield return new WaitForSeconds(_config.explosionPauseSeconds);

            Current = State.CheckWin;

            if (AliveCount > 1)
            {
                // A proxima rodada comeca no vizinho a direita de quem explodiu.
                int next = NextAliveIndex(_holderIndex, ArrowDirection.Right);
                if (next >= 0)
                {
                    _holderIndex = next;
                    _lastPasserIndex = -1;
                    _banana.gameObject.SetActive(true);
                    _banana.SnapTo(_slots[_holderIndex]);
                }
            }
        }

        private IEnumerator FlashRoutine()
        {
            if (_flashLight == null) yield break;

            _flashLight.transform.position = _slots[_holderIndex].HandPosition;
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

        private IEnumerator EndMatch()
        {
            MonkeySlot survivor = null;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsAlive) survivor = _slots[i];
            }

            if (survivor != null)
            {
                survivor.Celebrate();
                _hud.SetStatus(survivor.IsHuman
                    ? Loc.Get("game.lastmonkey")
                    : Loc.Format("game.monkeywon", survivor.Index + 1), Palette.BananaYellow);
            }

            Trace(survivor != null
                ? "FIM DA PARTIDA. Sobrevivente: macaco " + survivor.Index + (survivor.IsHuman ? " (VOCE)" : "")
                : "FIM DA PARTIDA sem sobrevivente.");

            yield return new WaitForSeconds(0.8f);
            _hud.ShowEnd(survivor != null && survivor.IsHuman);
        }

        // ------------------------------------------------------------------
        // Navegacao da roda
        // ------------------------------------------------------------------

        /// <summary>Proximo macaco vivo naquele lado. Pula os que ja explodiram.</summary>
        public int NextAliveIndex(int from, ArrowDirection side)
        {
            int step = side == ArrowDirection.Right ? 1 : -1;
            int count = _slots.Count;

            for (int i = 1; i < count; i++)
            {
                int index = ((from + step * i) % count + count) % count;
                if (_slots[index].IsAlive) return index;
            }

            return -1;
        }

        /// <summary>
        /// Descobre se da para devolver a banana direto para um indice.
        /// Usado pela vinganca dos bots.
        /// </summary>
        public bool TryGetSideTowards(MonkeySlot from, int targetIndex, out ArrowDirection side)
        {
            side = ArrowDirection.Left;
            if (from == null) return false;

            if (NextAliveIndex(from.Index, ArrowDirection.Left) == targetIndex)
            {
                side = ArrowDirection.Left;
                return true;
            }

            if (NextAliveIndex(from.Index, ArrowDirection.Right) == targetIndex)
            {
                side = ArrowDirection.Right;
                return true;
            }

            return false;
        }
    }
}
