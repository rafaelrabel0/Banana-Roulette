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

        /// <summary>Ligado pelo bootstrap quando a partida tem o menu no caixote.</summary>
        public bool TemMenuDaPartida;

        /// <summary>Lugar de quem esta com a bomba agora.</summary>
        public int HolderIndex { get { return _holderIndex; } }
        /// <summary>Lugar de quem passou a bomba por ultimo (-1 se ninguem).</summary>
        public int LastPasserIndex { get { return _lastPasserIndex; } }

        /// <summary>O macaco la atras, que joga a bomba nova para a roda depois de cada explosao. Nulo = a bomba so aparece.</summary>
        public MacacoVisual Lancador;

        // --- poderes ---
        private PowerUp _oferta = PowerUp.Nenhum;      // o poder oferecido no turno atual
        private TurnSolver _solverAtual;
        private List<ArrowDirection> _seqAtual;
        private bool _memoriaAtual;

        private static string T(string pt, string en) { return Loc.Current == Language.Portuguese ? pt : en; }
        private string QuemE(MonkeySlot s) { return s.IsHuman ? T("VOCÊ", "YOU") : T("MACACO ", "MONKEY ") + (s.Index + 1); }

        private void MostraInventario()
        {
            PowerUp p = _slots[0].Guardado;
            _hud.SetInventory(p == PowerUp.Nenhum ? "" : T("ESPAÇO: ", "SPACE: ") + PowerUps.Nome(p).ToUpperInvariant() + "  ·  " + PowerUps.Dica(p));
        }

        /// <summary>
        /// Usa o poder guardado de um macaco. Devolve falso se nao da para usar agora
        /// (ataque com a bomba na propria mao, ou poder de portador sem a bomba).
        /// </summary>
        public bool UsarPowerUp(MonkeySlot quem)
        {
            PowerUp p = quem.Guardado;
            if (p == PowerUp.Nenhum || !quem.IsAlive || !_running) return false;
            bool rodadaCorrendo = Current == State.Holding || Current == State.Passing;
            bool comABomba = rodadaCorrendo && _slots[_holderIndex] == quem;
            MonkeySlot alvo = _slots[_holderIndex];

            if (PowerUps.EhAtaque(p) && (comABomba || Current != State.Holding || _solverAtual == null)) return false;
            if (PowerUps.PrecisaDaBomba(p) && (!comABomba || Current != State.Holding || _solverAtual == null)) return false;
            if (p == PowerUp.PavioMolhado && !rodadaCorrendo) return false;

            switch (p)
            {
                case PowerUp.VidaExtra:
                    quem.GanhaVida();
                    if (quem.IsHuman) _hud.SetLives(quem.Lives);
                    break;
                case PowerUp.PavioMolhado:
                    _fuse.Molhar(3f);
                    break;
                case PowerUp.MeiaSequencia:
                {
                    var nova = _seqAtual.GetRange(0, Mathf.Max(1, Mathf.CeilToInt(_seqAtual.Count * 0.5f)));
                    TrocaSequencia(quem, nova);
                    break;
                }
                case PowerUp.PasseRelampago:
                    _chosenSide = ArrowDirection.Right;
                    _turnResolved = true;
                    break;
                case PowerUp.Invertido:
                    _solverAtual.Invertido = true;
                    _solverAtual.Lentidao = 1.6f;
                    break;
                case PowerUp.Dobrada:
                {
                    var nova = new List<ArrowDirection>(_seqAtual);
                    nova.AddRange(SequenceGenerator.Generate(Mathf.Min(_seqAtual.Count, 5)));
                    TrocaSequencia(alvo, nova);
                    break;
                }
                case PowerUp.PavioCurto:
                    _fuse.Burn(_fuse.Total * 0.15f);
                    _banana.FlashBurn();
                    if (_hud.TopFuse != null) _hud.TopFuse.FlashBurn();
                    break;
                case PowerUp.Nevoa:
                    _memoriaAtual = true;
                    _solverAtual.Lentidao = 1.6f;
                    if (alvo.IsHuman) _hud.ShowSequence(_seqAtual, true);
                    break;
                case PowerUp.Cola:
                    _solverAtual.TravadoAte = Time.time + 2f;
                    break;
            }

            quem.Guardado = PowerUp.Nenhum;
            MostraInventario();
            bool ataque = PowerUps.EhAtaque(p);
            _hud.SetStatus(QuemE(quem) + ": " + PowerUps.Nome(p).ToUpperInvariant() + (ataque ? T(" EM ", " ON ") + QuemE(alvo) : "") + "!",
                ataque ? Palette.DynamiteRed : new Color(0.45f, 0.92f, 1f));
            if (ataque) { SomRetro.Tom(520f, 0.09f, "sawtooth", 0.1f, 260f); SomRetro.Tom(390f, 0.14f, "sawtooth", 0.1f, 190f, 0.1f); }
            else { SomRetro.Tom(660f, 0.08f, "square", 0.08f); SomRetro.Tom(880f, 0.08f, "square", 0.08f, 0f, 0.09f); SomRetro.Tom(1320f, 0.14f, "square", 0.08f, 0f, 0.18f); }
            Trace(QuemE(quem) + " usou " + p + (ataque ? " em " + QuemE(alvo) : ""));
            return true;
        }

        private void TrocaSequencia(MonkeySlot dono, List<ArrowDirection> nova)
        {
            _seqAtual = nova;
            _solverAtual.TrocarSequencia(nova);
            if (!dono.IsHuman) return;
            _hud.ShowSequence(nova, _memoriaAtual);
            if (!_memoriaAtual) _hud.HighlightFrom(_solverAtual.ProgressoAtual);
        }

        /// <summary>Verdadeiro enquanto a bomba esta na mao do jogador humano e a rodada corre.</summary>
        public bool BombaComJogador
        {
            get
            {
                if (!_running || _slots == null || _holderIndex < 0 || _holderIndex >= _slots.Count) return false;
                return _slots[_holderIndex].IsHuman && _slots[_holderIndex].IsAlive && (Current == State.Holding || Current == State.Passing);
            }
        }

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
                MonkeySlot.Tensao = burned;
                MusicaRetro.Perigo = Current == State.GameOver ? 0f : burned;
                SonsDaRodada(burned);

                // A barra do topo anda pela MESMA fracao: e o mesmo pavio, visto
                // de outro jeito.
                if (_hud.TopFuse != null) _hud.TopFuse.SetProgress(burned);
            }

            // bots com poder guardado tentam usar de vez em quando (so sai quando a situacao permite)
            if (_slots != null)
            {
                for (int i = 1; i < _slots.Count; i++)
                    if (_slots[i].Guardado != PowerUp.Nenhum && Random.value < Time.deltaTime * 0.35f) UsarPowerUp(_slots[i]);
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Espaco: usa o poder guardado
            if (keyboard.spaceKey.wasPressedThisFrame && !PausaNoCaixote.Aberta && _slots != null && _slots[0].Guardado != PowerUp.Nenhum)
            {
                if (!UsarPowerUp(_slots[0])) SomRetro.Tom(150f, 0.12f, "sawtooth", 0.08f); // agora nao da
            }

            // Esc abre o menu da partida (PausaNoCaixote). Sem ele (greybox), Esc sai direto como antes.
            if (keyboard.escapeKey.wasPressedThisFrame && !TemMenuDaPartida)
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
            MusicaRetro.Tocar(MusicaRetro.Clima.Jogo);

            // A banana comeca na mao do jogador, nao sorteada: apertar JOGAR
            // tem que virar acao na hora, sem esperar a roda dar meia volta.
            _holderIndex = 0;
            _banana.SnapTo(_slots[_holderIndex]);
            _hud.SetAliveCount(AliveCount, _config.monkeyCount);
            _hud.SetLives(_slots[0].Lives);
            MostraInventario();
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
                holder.Acertou();
                SomRetro.Tom(660f + 110f * Mathf.Min(progress, 6), 0.05f, "square", 0.05f); // cada acerto sobe um degrau
            };
            solver.OnArrowMissed += () => HandleMiss(holder);
            solver.OnPassChosen += side => { _chosenSide = side; _turnResolved = true; };
            solver.OnPowerUpHit += p =>
            {
                if (holder.IsHuman) _hud.PowerUpHit(p);
                holder.Acertou();
                SomRetro.Tom(990f + 110f * Mathf.Min(p, 4), 0.05f, "sine", 0.07f);
            };
            solver.OnPowerUpGot += () =>
            {
                holder.Guardado = _oferta;
                Trace(QuemE(holder) + " pegou o poder " + _oferta);
                _oferta = PowerUp.Nenhum;
                if (holder.IsHuman) { _hud.HidePowerUp(); MostraInventario(); }
                SomRetro.Tom(784f, 0.08f, "square", 0.08f); SomRetro.Tom(1175f, 0.16f, "square", 0.08f, 0f, 0.09f);
            };
            solver.OnPowerUpLost += () =>
            {
                _oferta = PowerUp.Nenhum;
                if (holder.IsHuman) _hud.HidePowerUp();
            };

            solver.BeginTurn(sequence, _lastPasserIndex);
            _solverAtual = solver; _seqAtual = sequence; _memoriaAtual = memoryRound;

            // Poder: so aparece para quem esta com a bomba e nao esta guardando outro. A primeira tecla dele
            // nunca e igual a primeira da bomba, para a primeira tecla apertada dizer qual dos dois ele escolheu.
            _oferta = PowerUp.Nenhum;
            _hud.HidePowerUp();
            if (holder.Guardado == PowerUp.Nenhum && Random.value < 0.45f)
            {
                _oferta = PowerUps.Sortear();
                var seqPoder = SequenceGenerator.Generate(3);
                if (seqPoder[0] == sequence[0]) seqPoder[0] = (ArrowDirection)(((int)seqPoder[0] + 1) % 4);
                solver.Oferecer(seqPoder);
                if (holder.IsHuman && !memoryRound) _hud.ShowPowerUp(PowerUps.Nome(_oferta), seqPoder);
            }

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
            _hud.HidePowerUp();
            _solverAtual = null; _oferta = PowerUp.Nenhum;

            if (_fuseEnded) yield break;

            yield return PassBanana();
        }

        private float _proximoGrito, _proximoEstalo;

        // Quem segura a bomba faz barulho, e mais barulho quanto menos pavio sobra. O pavio estala junto.
        private void SonsDaRodada(float burned)
        {
            if (Current != State.Holding && Current != State.Passing) return;
            if (Time.time >= _proximoGrito)
            {
                _proximoGrito = Time.time + Mathf.Lerp(1.3f, 0.32f, burned) * Random.Range(0.8f, 1.2f);
                float f = burned > 0.7f ? 760f : burned > 0.38f ? 620f : 480f;
                SomRetro.Grito(f, f * 1.45f, burned > 0.7f ? 0.2f : 0.13f, Mathf.Round(Mathf.Lerp(0.22f, 0.45f, burned) * 20f) / 20f); // volume em degraus, para nao fabricar um som novo a cada quadro
            }
            if (Time.time >= _proximoEstalo)
            {
                _proximoEstalo = Time.time + Mathf.Lerp(0.45f, 0.07f, burned);
                SomRetro.Ruido(0.05f, 0.05f, 6000f, "highpass");
            }
        }

        /// <summary>
        /// Errar nao custa so os milissegundos perdidos: queima um pedaco do
        /// pavio de uma vez. E o que faz pressa e calma brigarem de verdade.
        /// </summary>
        private void HandleMiss(MonkeySlot holder)
        {
            holder.FlashError();
            StartCoroutine(_banana.Fumble(_config.errorLockoutSeconds));
            SomRetro.Tom(170f, 0.2f, "sawtooth", 0.12f, 90f);   // buzina de erro
            SomRetro.Grito(760f, 420f, 0.22f, 0.4f);             // o macaco se assusta

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
            _slots[_holderIndex].Arremessar(_slots[nextIndex].transform.position);
            _slots[nextIndex].Receber();
            SomRetro.Ruido(0.25f, 0.18f, 900f, "bandpass");            // a bomba cortando o ar
            SomRetro.Grito(600f, 950f, 0.14f, 0.45f, 0.3f);            // quem recebe da um berro
            SomRetro.Grito(520f, 640f, 0.07f, 0.3f, 0.5f);             // quem jogou ri: ha, ha, ha
            SomRetro.Grito(560f, 680f, 0.07f, 0.3f, 0.62f);
            SomRetro.Grito(600f, 720f, 0.09f, 0.3f, 0.74f);
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
            Explosao.Criar(victim.HandPosition, 1f);
            SomRetro.Ruido(0.7f, 0.7f, 180f, "lowpass");   // bum
            SomRetro.Tom(90f, 0.5f, "sine", 0.6f, 35f);

            // As duas bananas estouram no mesmo instante: a da mao e a do topo.
            if (_hud.TopFuse != null) StartCoroutine(_hud.TopFuse.Explode());

            // Duas vidas: a explosao tira uma. Com vida sobrando ele se levanta e continua; sem vida, sai voando.
            bool fora = victim.PerdeVida();
            if (fora) victim.Explode(); else victim.Chamuscar();
            if (victim.IsHuman) _hud.SetLives(victim.Lives);
            Trace(string.Format("EXPLODIU o macaco {0}{1}. Restam {2}.",
                _holderIndex, victim.IsHuman ? " (VOCE)" : "", AliveCount));

            if (fora)
                _hud.SetStatus(victim.IsHuman
                    ? Loc.Get("game.youexploded")
                    : Loc.Format("game.exploded", _holderIndex + 1), Palette.DynamiteRed);
            else
                _hud.SetStatus(QuemE(victim) + T(" PERDEU UMA VIDA", " LOST A LIFE"), Palette.DynamiteRed);
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

                    // A bomba nova nao aparece do nada: o macaco la atras, cercado de bombas acesas, joga mais uma para a roda.
                    if (Lancador != null && Lancador.PontoMaco != null)
                    {
                        _banana.UpdateFuseVisual(0f);
                        _banana.transform.position = Lancador.PontoMaco.position;
                        Lancador.Tocar(Lancador.transform.InverseTransformPoint(_slots[_holderIndex].transform.position).x < 0f ? "jogaE" : "jogaD");
                        _slots[_holderIndex].Receber();
                        SomRetro.Grito(820f, 520f, 0.25f, 0.4f);              // o grito de quem se livra dela
                        SomRetro.Ruido(0.5f, 0.16f, 800f, "bandpass", 0.1f);  // a bomba no ar
                        yield return _banana.FlyTo(_slots[_holderIndex], 0.95f, 6.5f);
                        SomRetro.Grito(600f, 950f, 0.14f, 0.45f);
                    }
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
