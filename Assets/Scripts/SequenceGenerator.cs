using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Gera a sequencia de setas que o portador precisa digitar para se livrar
    /// da banana. O tamanho cresce conforme os macacos morrem.
    /// </summary>
    public static class SequenceGenerator
    {
        public static List<ArrowDirection> Generate(int length)
        {
            var sequence = new List<ArrowDirection>(length);
            ArrowDirection previous = (ArrowDirection)(-1);

            for (int i = 0; i < length; i++)
            {
                ArrowDirection next;
                int guard = 0;

                // Evita tres setas iguais seguidas: vira spam de tecla, nao ritmo.
                do
                {
                    next = (ArrowDirection)Random.Range(0, 4);
                    guard++;
                }
                while (next == previous && i > 0 && guard < 8);

                sequence.Add(next);
                previous = next;
            }

            return sequence;
        }
    }
}
