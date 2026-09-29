using UnityEngine;

namespace BananaRoulette
{
    /// <summary>As quatro setas do teclado. Tambem sao a escolha de lado no arremesso.</summary>
    public enum ArrowDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public static class ArrowDirectionExtensions
    {
        /// <summary>Rotacao no plano da tela para desenhar o triangulo da seta.</summary>
        public static float ScreenRotation(this ArrowDirection dir)
        {
            switch (dir)
            {
                case ArrowDirection.Up: return 0f;
                case ArrowDirection.Left: return 90f;
                case ArrowDirection.Down: return 180f;
                case ArrowDirection.Right: return -90f;
                default: return 0f;
            }
        }

        public static bool IsHorizontal(this ArrowDirection dir)
        {
            return dir == ArrowDirection.Left || dir == ArrowDirection.Right;
        }
    }
}
