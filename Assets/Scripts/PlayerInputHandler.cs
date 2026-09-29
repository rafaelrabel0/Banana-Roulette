using UnityEngine;
using UnityEngine.InputSystem;

namespace BananaRoulette
{
    /// <summary>
    /// Entrada do jogador humano pelas setas do teclado.
    ///
    /// Duas fases num turno:
    ///   1. Digitar a sequencia de N setas.
    ///   2. Uma seta extra, esquerda ou direita, que e o arremesso.
    /// A ultima tecla e a decisao de para quem passar. Isso resolve o conflito
    /// de usar as mesmas teclas para digitar e para escolher o lado.
    /// </summary>
    public class PlayerInputHandler : TurnSolver
    {
        private enum Phase { Typing, ChoosingSide }

        private Phase _phase;
        private float _lockoutUntil;
        private TuningConfig _config;

        public void Configure(TuningConfig config)
        {
            _config = config;
        }

        public override void BeginTurn(System.Collections.Generic.List<ArrowDirection> sequence, int lastPasserIndex)
        {
            base.BeginTurn(sequence, lastPasserIndex);
            _phase = Phase.Typing;
            _lockoutUntil = 0f;
        }

        private void Update()
        {
            if (!IsActive) return;
            if (Time.time < _lockoutUntil) return;

            ArrowDirection pressed;
            if (!TryReadArrow(out pressed)) return;

            if (_phase == Phase.Typing)
            {
                HandleTyping(pressed);
            }
            else
            {
                HandleSideChoice(pressed);
            }
        }

        private void HandleTyping(ArrowDirection pressed)
        {
            if (pressed == Sequence[Progress])
            {
                Progress++;
                RaiseHit();

                if (Progress >= Sequence.Count)
                {
                    _phase = Phase.ChoosingSide;
                }
            }
            else
            {
                // Errou: a banana voa, a sequencia reinicia e o pavio nao para.
                _lockoutUntil = Time.time + (_config != null ? _config.errorLockoutSeconds : 0.5f);
                RaiseMiss();
            }
        }

        private void HandleSideChoice(ArrowDirection pressed)
        {
            if (!pressed.IsHorizontal()) return;
            RaisePass(pressed);
        }

        private static bool TryReadArrow(out ArrowDirection direction)
        {
            direction = ArrowDirection.Left;

            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            if (keyboard.leftArrowKey.wasPressedThisFrame) { direction = ArrowDirection.Left; return true; }
            if (keyboard.rightArrowKey.wasPressedThisFrame) { direction = ArrowDirection.Right; return true; }
            if (keyboard.upArrowKey.wasPressedThisFrame) { direction = ArrowDirection.Up; return true; }
            if (keyboard.downArrowKey.wasPressedThisFrame) { direction = ArrowDirection.Down; return true; }

            return false;
        }

        /// <summary>True quando a sequencia acabou e falta so escolher o lado.</summary>
        public bool IsChoosingSide
        {
            get { return IsActive && _phase == Phase.ChoosingSide; }
        }
    }
}
