using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Cores do pitch, reaproveitadas no greybox. Custa zero e ja liga o
    /// prototipo a identidade visual do projeto.
    /// </summary>
    public static class Palette
    {
        public static readonly Color JungleNight = new Color32(0x0B, 0x1D, 0x15, 0xFF);
        public static readonly Color BananaYellow = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
        public static readonly Color DynamiteRed = new Color32(0xFF, 0x5A, 0x3C, 0xFF);
        public static readonly Color Charred = new Color32(0x2A, 0x22, 0x1C, 0xFF);
        public static readonly Color Ground = new Color32(0x16, 0x33, 0x25, 0xFF);
        public static readonly Color HitGreen = new Color32(0x6B, 0xE0, 0x7A, 0xFF);
        /// <summary>Seta ainda nao digitada. Clara o bastante para ler sobre o chao da selva.</summary>
        public static readonly Color Dim = new Color32(0x9A, 0xAD, 0xA2, 0xFF);

        /// <summary>Cores dos bots. O humano usa o amarelo banana e nunca entra aqui.</summary>
        private static readonly Color[] BotColors =
        {
            new Color32(0x8D, 0x6E, 0x5A, 0xFF), // marrom classico
            new Color32(0xE0, 0x8B, 0x4F, 0xFF), // mandril laranja
            new Color32(0xE8, 0xE2, 0xD4, 0xFF), // neve
            new Color32(0x6F, 0x7C, 0x9B, 0xFF), // sombra azulada
            new Color32(0xA8, 0x6C, 0x8E, 0xFF), // roxo
            new Color32(0x5F, 0xA8, 0x8C, 0xFF), // verde agua
            new Color32(0xC2, 0xA3, 0x5E, 0xFF), // ocre
            new Color32(0x94, 0x8A, 0x7A, 0xFF)  // cinza quente
        };

        public static Color ForSlot(int index, bool isHuman)
        {
            if (isHuman) return BananaYellow;
            return BotColors[index % BotColors.Length];
        }
    }
}
