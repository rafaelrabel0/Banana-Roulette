using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Bot que simula um jogador digitando: leva um tempo por seta e as vezes
    /// erra. Cada bot sorteia a propria velocidade no inicio da partida, o que
    /// da personalidade a roda sem nenhuma IA de verdade.
    /// </summary>
    public class BotBrain : TurnSolver
    {
        private TuningConfig _config;
        private MonkeySlot _slot;
        private float _secondsPerArrow;
        private float _nextArrowTime;
        private bool _buscandoPoder;
        private int _teclasDoPoder;

        public void Configure(TuningConfig config)
        {
            _config = config;
            _slot = GetComponent<MonkeySlot>();
            // Velocidade fixa por bot: o lento continua lento a partida inteira.
            _secondsPerArrow = Random.Range(config.botMinSecondsPerArrow, config.botMaxSecondsPerArrow);
        }

        public override void BeginTurn(List<ArrowDirection> sequence, int lastPasserIndex)
        {
            base.BeginTurn(sequence, lastPasserIndex);
            _buscandoPoder = false;
            ScheduleNextArrow();
        }

        // Metade das vezes o bot resolve pegar o poder antes de mexer na bomba. Custa o tempo de digitar as teclas dele.
        public override void Oferecer(List<ArrowDirection> sequenciaDoPoder)
        {
            base.Oferecer(sequenciaDoPoder);
            if (sequenciaDoPoder == null || Random.value > 0.5f) return;
            _buscandoPoder = true;
            _teclasDoPoder = sequenciaDoPoder.Count;
            _nextArrowTime = Time.time + _secondsPerArrow * _teclasDoPoder * Random.Range(0.9f, 1.2f);
        }

        public override void CancelarOferta()
        {
            if (_buscandoPoder) { _buscandoPoder = false; ScheduleNextArrow(); }
            base.CancelarOferta();
        }

        private void ScheduleNextArrow()
        {
            // Pequena variacao a cada seta para o ritmo nao soar mecanico.
            _nextArrowTime = Time.time + _secondsPerArrow * Mathf.Max(1f, Lentidao) * Random.Range(0.8f, 1.2f);
        }

        private void Update()
        {
            if (!IsActive) return;
            if (Time.time < TravadoAte) return; // ataque "cola"
            if (Time.time < _nextArrowTime) return;

            if (_buscandoPoder)
            {
                _buscandoPoder = false;
                if (Random.value < _config.botErrorChance * 2f)
                {
                    RaisePowerUpLost();
                    RaiseMiss();
                    _nextArrowTime = Time.time + _config.errorLockoutSeconds + _secondsPerArrow;
                }
                else
                {
                    RaisePowerUpGot();
                    ScheduleNextArrow();
                }
                return;
            }

            // a sequencia pode ter encolhido no meio do turno (meia sequencia)
            if (Progress >= Sequence.Count) { RaisePass(ChooseSide()); return; }

            bool missed = Random.value < _config.botErrorChance;

            if (missed)
            {
                RaiseMiss();
                _nextArrowTime = Time.time + _config.errorLockoutSeconds + _secondsPerArrow;
                return;
            }

            Progress++;
            RaiseHit();

            if (Progress >= Sequence.Count)
            {
                RaisePass(ChooseSide());
            }
            else
            {
                ScheduleNextArrow();
            }
        }

        /// <summary>
        /// Vinganca: devolver a banana para quem acabou de passar e o que torna
        /// a roda pessoal.
        ///
        /// A vinganca e um DESVIO do padrao, nunca um sorteio a mais. Somar as
        /// duas coisas dava 72 por cento de chance de devolver (45 da vinganca
        /// mais metade dos 55 restantes, ja que so existem dois vizinhos), e a
        /// banana ficava presa em ping-pong entre dois macacos enquanto o resto
        /// da roda nunca jogava. O padrao agora e seguir adiante.
        /// </summary>
        private ArrowDirection ChooseSide()
        {
            var manager = GameManager.Instance;

            // Primeira banana da rodada: nao veio de ninguem, entao vale o acaso.
            if (manager == null || LastPasserIndex < 0)
            {
                return RandomSide();
            }

            ArrowDirection backwards;
            if (!manager.TryGetSideTowards(_slot, LastPasserIndex, out backwards))
            {
                return RandomSide();
            }

            return Random.value < _config.botRevengeChance ? backwards : Opposite(backwards);
        }

        private static ArrowDirection RandomSide()
        {
            return Random.value < 0.5f ? ArrowDirection.Left : ArrowDirection.Right;
        }

        private static ArrowDirection Opposite(ArrowDirection side)
        {
            return side == ArrowDirection.Left ? ArrowDirection.Right : ArrowDirection.Left;
        }
    }
}
