using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// O rosto em emoji de cada macaco, com a cor do pelo e os acessorios dele.
    ///
    /// Em vez de uma imagem por combinacao (seriam centenas), ha uma folha so, Resources/Jogo/caras.png, com o
    /// desenho separado em CAMADAS: balao, pelo (branco, tingido aqui), cara, cada chapeu, cada oculos, cada brinco.
    /// Este codigo empilha as camadas certas e devolve a folha pronta daquele macaco:
    ///   7 linhas (serio, riso, lingua, bravo, choro, medo, sono) x 4 quadros de 96 px.
    /// A linha "serio" (sem balao) e o retrato do painel; as outras seis sao os emojis que ele manda.
    ///
    /// As camadas sao desenhadas na previa por __cena.exportaCaras(). A imagem precisa estar marcada como legivel.
    /// </summary>
    public static class RostoDoMacaco
    {
        public const int Quadros = 4, Linhas = 7;
        const int Q = 96, BW = Q * Quadros, BH = Q * Linhas, Cols = 4;
        static readonly string[] Camadas = { "balao", "pelo", "cara", "chapeu_palha", "chapeu_bone", "chapeu_cartola", "chapeu_fone", "oculos_redondo", "oculos_escuro", "brinco_argola", "brinco_pedra" };

        static Color32[] atlas; static int aw, ah; static bool tentou;
        static readonly Dictionary<string, Texture2D> feitos = new Dictionary<string, Texture2D>();

        /// <summary>Recorte de um quadro: linha 0 = serio, 1 a 6 = emojis.</summary>
        public static Rect Recorte(int linha, int quadro)
        {
            return new Rect(quadro / (float)Quadros, 1f - (linha + 1) / (float)Linhas, 1f / Quadros, 1f / Linhas);
        }

        public static Texture2D Folha(Color pelo, string chapeu, string oculos, string brinco)
        {
            if (!tentou)
            {
                tentou = true;
                var t = Resources.Load<Texture2D>("Jogo/caras");
                if (t != null && t.isReadable) { atlas = t.GetPixels32(); aw = t.width; ah = t.height; }
            }
            if (atlas == null) return null;

            Color32 cor = pelo;
            string chave = cor.r + "," + cor.g + "," + cor.b + "|" + chapeu + "|" + oculos + "|" + brinco;
            Texture2D pronta;
            if (feitos.TryGetValue(chave, out pronta) && pronta != null) return pronta;

            var px = new Color32[BW * BH];
            Poe(px, "balao", Color.white);
            Poe(px, "pelo", pelo);
            Poe(px, "cara", Color.white);
            if (!string.IsNullOrEmpty(brinco)) Poe(px, "brinco_" + brinco, Color.white);
            if (!string.IsNullOrEmpty(oculos)) Poe(px, "oculos_" + oculos, Color.white);
            if (!string.IsNullOrEmpty(chapeu)) Poe(px, "chapeu_" + chapeu, Color.white);

            pronta = new Texture2D(BW, BH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "Rosto " + chave };
            pronta.SetPixels32(px); pronta.Apply();
            feitos[chave] = pronta;
            return pronta;
        }

        // Empilha uma camada por cima do que ja esta desenhado, multiplicada por uma cor.
        static void Poe(Color32[] px, string camada, Color tinta)
        {
            int k = System.Array.IndexOf(Camadas, camada);
            if (k < 0) return;
            int x0 = (k % Cols) * BW, y0 = ah - (k / Cols + 1) * BH; // a imagem e guardada de baixo para cima
            if (x0 + BW > aw || y0 < 0) return;
            for (int y = 0; y < BH; y++)
            {
                int linha = (y0 + y) * aw + x0, sai = y * BW;
                for (int x = 0; x < BW; x++)
                {
                    Color32 s = atlas[linha + x];
                    if (s.a == 0) continue;
                    float a = s.a / 255f;
                    Color32 d = px[sai + x];
                    float r = s.r * tinta.r, g = s.g * tinta.g, b = s.b * tinta.b;
                    px[sai + x] = new Color32(
                        (byte)(r * a + d.r * (1f - a)), (byte)(g * a + d.g * (1f - a)), (byte)(b * a + d.b * (1f - a)),
                        (byte)Mathf.Max(d.a, s.a));
                }
            }
        }
    }
}
