using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Fonte de entrada de um turno. O GameManager nao sabe se quem esta
    /// segurando a banana e uma pessoa ou um bot: fala com esta abstracao.
    ///
    /// E por aqui que o multiplayer entra depois, como uma terceira
    /// implementacao (entrada vinda da rede), sem mexer no GameManager.
    /// </summary>
    public abstract class TurnSolver : MonoBehaviour
    {
        /// <summary>Acertou uma seta. Parametro: quantas ja foram acertadas.</summary>
        public event Action<int> OnArrowHit;

        /// <summary>Errou. A banana escapa e a sequencia reinicia do zero.</summary>
        public event Action OnArrowMissed;

        /// <summary>Terminou a sequencia e escolheu o lado. Parametro: Left ou Right.</summary>
        public event Action<ArrowDirection> OnPassChosen;

        protected List<ArrowDirection> Sequence { get; private set; }
        protected int Progress { get; set; }
        protected bool IsActive { get; private set; }

        /// <summary>Indice de quem passou a banana para ca. -1 se ninguem (inicio da partida).</summary>
        protected int LastPasserIndex { get; private set; }

        public virtual void BeginTurn(List<ArrowDirection> sequence, int lastPasserIndex)
        {
            Sequence = sequence;
            Progress = 0;
            LastPasserIndex = lastPasserIndex;
            IsActive = true;
        }

        public virtual void EndTurn()
        {
            IsActive = false;
            Sequence = null;
            Progress = 0;
        }

        protected void RaiseHit()
        {
            if (OnArrowHit != null) OnArrowHit(Progress);
        }

        protected void RaiseMiss()
        {
            Progress = 0;
            if (OnArrowMissed != null) OnArrowMissed();
        }

        protected void RaisePass(ArrowDirection side)
        {
            IsActive = false;
            if (OnPassChosen != null) OnPassChosen(side);
        }

        public void ClearListeners()
        {
            OnArrowHit = null;
            OnArrowMissed = null;
            OnPassChosen = null;
        }
    }
}
