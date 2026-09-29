using System;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// O pavio. A duracao e sorteada e nunca mostrada ao jogador: o medo vem
    /// de nao saber quanto falta. Corre durante a digitacao e durante o voo.
    /// </summary>
    public class FuseTimer : MonoBehaviour
    {
        public event Action OnFuseEnded;

        public bool IsRunning { get; private set; }
        public float Remaining { get; private set; }
        public float Total { get; private set; }

        /// <summary>Fracao ja queimada, de 0 a 1. Usado so pelo debug e pelos efeitos.</summary>
        public float BurnedFraction
        {
            get { return Total <= 0f ? 0f : Mathf.Clamp01(1f - Remaining / Total); }
        }

        public void StartFuse(float seconds)
        {
            Total = seconds;
            Remaining = seconds;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        /// <summary>Volta a queimar de onde parou. Usado pelo tutorial entre as falas.</summary>
        public void Resume()
        {
            if (Remaining > 0f) IsRunning = true;
        }

        /// <summary>
        /// Queima um pedaco extra de uma vez. E o preco de errar uma seta:
        /// o erro nao custa so tempo perdido, custa pavio.
        /// </summary>
        public void Burn(float seconds)
        {
            if (!IsRunning || seconds <= 0f) return;

            Remaining -= seconds;
            if (OnBurned != null) OnBurned(seconds);

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                IsRunning = false;
                if (OnFuseEnded != null) OnFuseEnded();
            }
        }

        /// <summary>Disparado quando um erro queima pavio. Parametro: segundos queimados.</summary>
        public event Action<float> OnBurned;

        private void Update()
        {
            if (!IsRunning) return;

            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                IsRunning = false;
                if (OnFuseEnded != null) OnFuseEnded();
            }
        }
    }
}
