using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Todos os numeros do prototipo em um lugar so.
    /// Fica como asset para que os ajustes feitos durante o Play mode persistam
    /// depois de sair dele. Ajustar isto ao vivo no playtest e o objetivo.
    /// </summary>
    [CreateAssetMenu(fileName = "TuningConfig", menuName = "Banana Roulette/Tuning Config")]
    public class TuningConfig : ScriptableObject
    {
        [Header("Roda")]
        [Tooltip("Quantos macacos comecam a partida, contando o jogador. 5 = 4 bots + voce.")]
        public int monkeyCount = 5;

        [Tooltip("Raio da roda em metros.")]
        public float circleRadius = 5f;

        [Header("Sequencia de setas")]
        [Tooltip("Setas na sequencia quando todos os macacos estao vivos.")]
        public int baseSequenceLength = 3;

        [Tooltip("Setas somadas a cada macaco eliminado.")]
        public int arrowsPerElimination = 1;

        [Tooltip("Teto de setas, para a sequencia nao virar absurdo.")]
        public int maxSequenceLength = 8;

        [Tooltip("Com este numero de vivos ou menos, o HUD some e vira memoria.")]
        public int hideHudAtAliveCount = 2;

        [Header("Pavio (segredo do jogador)")]
        [Tooltip("Duracao media do pavio na primeira rodada, em segundos.")]
        public float baseFuseSeconds = 22f;

        [Tooltip("Segundos descontados do pavio a cada eliminacao.")]
        public float fuseReductionPerElimination = 2.5f;

        [Tooltip("O pavio nunca fica mais curto que isto.")]
        public float minimumFuseSeconds = 8f;

        [Tooltip("Variacao aleatoria aplicada ao pavio. 0.2 = mais ou menos 20 por cento.")]
        [Range(0f, 0.5f)] public float fuseJitter = 0.2f;

        [Header("Jogador")]
        [Tooltip("Tempo travado apos errar uma seta. A banana voa e a sequencia reinicia.")]
        public float errorLockoutSeconds = 0.5f;

        [Tooltip("Pavio queimado de uma vez a cada erro. E o que faz pressa e calma brigarem.")]
        public float errorFusePenaltySeconds = 1.2f;

        [Header("Bots")]
        [Tooltip("Tempo minimo que um bot leva por seta.")]
        public float botMinSecondsPerArrow = 0.35f;

        [Tooltip("Tempo maximo que um bot leva por seta.")]
        public float botMaxSecondsPerArrow = 0.70f;

        [Tooltip("Chance de um bot errar cada seta.")]
        [Range(0f, 1f)] public float botErrorChance = 0.12f;

        [Tooltip("Chance do bot devolver a banana para quem acabou de passar. O padrao e seguir adiante.")]
        [Range(0f, 1f)] public float botRevengeChance = 0.30f;

        [Header("Ritmo das transicoes")]
        [Tooltip("Duracao do voo da banana entre dois macacos.")]
        public float passFlightSeconds = 0.35f;

        [Tooltip("Pausa depois da explosao, antes da proxima rodada.")]
        public float explosionPauseSeconds = 1.6f;

        [Header("Debug")]
        [Tooltip("Mostra o pavio na tela. So para desenvolvimento: no jogo ele e secreto.")]
        public bool showFuseDebug = false;

        [Tooltip("Escreve cada passo da partida no console. Util para auditar uma partida inteira.")]
        public bool verboseLog = true;

        /// <summary>Setas na sequencia para um dado numero de macacos vivos.</summary>
        public int SequenceLengthFor(int aliveCount)
        {
            int eliminated = Mathf.Max(0, monkeyCount - aliveCount);
            int length = baseSequenceLength + eliminated * arrowsPerElimination;
            return Mathf.Clamp(length, 1, maxSequenceLength);
        }

        /// <summary>Duracao sorteada do pavio para um dado numero de macacos vivos.</summary>
        public float RollFuseDuration(int aliveCount)
        {
            int eliminated = Mathf.Max(0, monkeyCount - aliveCount);
            float target = baseFuseSeconds - eliminated * fuseReductionPerElimination;
            target = Mathf.Max(minimumFuseSeconds, target);
            float jitter = Random.Range(-fuseJitter, fuseJitter);
            return target * (1f + jitter);
        }
    }
}
