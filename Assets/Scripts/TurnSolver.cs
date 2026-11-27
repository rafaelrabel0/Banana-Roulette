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

        /// <summary>Acertou uma tecla da sequencia do poder. Parametro: quantas ja foram.</summary>
        public event Action<int> OnPowerUpHit;
        /// <summary>Completou a sequencia do poder: ele e seu.</summary>
        public event Action OnPowerUpGot;
        /// <summary>Errou a sequencia do poder: ele se perdeu.</summary>
        public event Action OnPowerUpLost;

        /// <summary>Sequencia do poder oferecido neste turno. Nulo = nenhum.</summary>
        protected List<ArrowDirection> Oferta { get; private set; }

        /// <summary>Ataque "comandos invertidos": esquerda vale direita, cima vale baixo. Vale ate o fim do turno.</summary>
        public bool Invertido { get; set; }
        /// <summary>Ataque "cola": ate este instante nenhuma tecla conta.</summary>
        public float TravadoAte { get; set; }
        /// <summary>Quanto os bots ficam mais lentos neste turno (nevoa e invertido atrapalham os bots assim).</summary>
        public float Lentidao { get; set; }
        /// <summary>Quantas teclas da sequencia da bomba ja foram acertadas.</summary>
        public int ProgressoAtual { get { return Progress; } }

        /// <summary>Oferece um poder neste turno. Chamar depois de BeginTurn.</summary>
        public virtual void Oferecer(List<ArrowDirection> sequenciaDoPoder) { Oferta = sequenciaDoPoder; }

        /// <summary>O tempo do poder acabou (a bombinha dele estourou): some a oferta, sem contar como erro.</summary>
        public virtual void CancelarOferta() { if (Oferta != null) RaisePowerUpLost(); }

        /// <summary>Troca a sequencia no meio do turno (meia sequencia, sequencia dobrada).</summary>
        public void TrocarSequencia(List<ArrowDirection> nova)
        {
            if (!IsActive || nova == null) return;
            Sequence = nova;
            if (Progress > nova.Count) Progress = nova.Count;
            AoTrocarSequencia();
        }

        protected virtual void AoTrocarSequencia() { }

        protected void RaisePowerUpHit(int progress) { if (OnPowerUpHit != null) OnPowerUpHit(progress); }
        protected void RaisePowerUpGot() { Oferta = null; if (OnPowerUpGot != null) OnPowerUpGot(); }
        protected void RaisePowerUpLost() { Oferta = null; if (OnPowerUpLost != null) OnPowerUpLost(); }

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
            Oferta = null; Invertido = false; TravadoAte = 0f; Lentidao = 1f;
        }

        public virtual void EndTurn()
        {
            IsActive = false;
            Sequence = null;
            Progress = 0;
            Oferta = null;
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
            OnPowerUpHit = null;
            OnPowerUpGot = null;
            OnPowerUpLost = null;
        }
    }
}
