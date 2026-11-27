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
        private int _noPoder = -1; // quantas teclas da sequencia do poder ja foram; -1 = nao esta nela

        public override void CancelarOferta() { _noPoder = -1; base.CancelarOferta(); }
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
            _noPoder = -1;
        }

        private void Update()
        {
            if (!IsActive) return;
            if (PausaNoCaixote.Aberta) return; // com o menu da partida aberto, as teclas mexem no menu
            if (Time.time < _lockoutUntil) return;
            if (Time.time < TravadoAte) return; // ataque "cola"

            ArrowDirection pressed;
            if (!TryReadArrow(out pressed)) return;

            if (_phase == Phase.Typing)
            {
                if (Invertido) pressed = Oposta(pressed); // ataque "comandos invertidos"
                HandleTyping(pressed);
            }
            else
            {
                HandleSideChoice(pressed);
            }
        }

        private static ArrowDirection Oposta(ArrowDirection d)
        {
            switch (d)
            {
                case ArrowDirection.Left: return ArrowDirection.Right;
                case ArrowDirection.Right: return ArrowDirection.Left;
                case ArrowDirection.Up: return ArrowDirection.Down;
                default: return ArrowDirection.Up;
            }
        }

        protected override void AoTrocarSequencia()
        {
            if (Sequence != null && Progress >= Sequence.Count) _phase = Phase.ChoosingSide;
            else _phase = Phase.Typing;
        }

        private void HandleTyping(ArrowDirection pressed)
        {
            // No meio da sequencia do poder: ou acerta a proxima, ou perde o poder (e isso conta como erro).
            if (_noPoder >= 0)
            {
                if (Oferta != null && _noPoder < Oferta.Count && pressed == Oferta[_noPoder])
                {
                    _noPoder++;
                    RaisePowerUpHit(_noPoder);
                    if (_noPoder >= Oferta.Count) { _noPoder = -1; RaisePowerUpGot(); }
                }
                else
                {
                    _noPoder = -1;
                    RaisePowerUpLost();
                    _lockoutUntil = Time.time + (_config != null ? _config.errorLockoutSeconds : 0.5f);
                    RaiseMiss();
                }
                return;
            }

            // A primeira tecla decide: a da bomba comeca a bomba, a do poder comeca o poder. Elas nunca sao iguais.
            if (Progress == 0 && Oferta != null && Oferta.Count > 0 && pressed == Oferta[0] && pressed != Sequence[0])
            {
                _noPoder = 1;
                RaisePowerUpHit(1);
                return;
            }

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

            // Opcoes > Jogabilidade > Teclas: com letras, W A S D valem como as setas.
            if (GameSettings.UseLetters)
            {
                if (keyboard.aKey.wasPressedThisFrame) { direction = ArrowDirection.Left; return true; }
                if (keyboard.dKey.wasPressedThisFrame) { direction = ArrowDirection.Right; return true; }
                if (keyboard.wKey.wasPressedThisFrame) { direction = ArrowDirection.Up; return true; }
                if (keyboard.sKey.wasPressedThisFrame) { direction = ArrowDirection.Down; return true; }
                return false;
            }

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
