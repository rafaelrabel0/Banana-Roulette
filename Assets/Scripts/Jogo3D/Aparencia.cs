using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Catalogo da personalizacao: cores de pelo, acessorios de cabeca e cores da bomba.
    ///
    /// Os acessorios ja vem dentro do modelo do macaco (Resources/Jogo/macaco-jogo.glb), cada um num
    /// grupo chamado "Ac_lugar_nome" (ex.: Ac_chapeu_palha). Aqui so se liga o escolhido.
    /// Para acrescentar um acessorio: modelar na previa (funcao acessorios), exportar de novo e
    /// por o nome na lista abaixo.
    /// </summary>
    public static class Aparencia
    {
        public struct Item
        {
            public string id, pt, en; public Color cor, cor2;
            public Item(string id, string pt, string en, uint cor = 0, uint cor2 = 0)
            { this.id = id; this.pt = pt; this.en = en; this.cor = Hex(cor); this.cor2 = Hex(cor2); }
            public string Nome { get { return Loc.Current == Language.Portuguese ? pt : en; } }
        }

        static Color Hex(uint h) { return new Color32((byte)(h >> 16), (byte)(h >> 8), (byte)h, 0xFF); }

        public static readonly Item[] Pelos =
        {
            new Item("marrom", "Marrom", "Brown", 0x4A2A14), new Item("castanho", "Castanho", "Chestnut", 0x7A4A28),
            new Item("ruivo", "Ruivo", "Ginger", 0xA8552A), new Item("preto", "Preto", "Black", 0x231C1C),
            new Item("cinza", "Cinza", "Grey", 0x8A857D), new Item("branco", "Branco", "White", 0xE6DFCF)
        };

        public static readonly Item[] Chapeus =
        {
            new Item("", "Nenhum", "None"), new Item("palha", "Palha", "Straw"), new Item("bone", "Boné", "Cap"),
            new Item("cartola", "Cartola", "Top hat"), new Item("fone", "Fone", "Headphones")
        };

        public static readonly Item[] Oculos =
        {
            new Item("", "Nenhum", "None"), new Item("redondo", "Redondo", "Round"), new Item("escuro", "Escuro", "Shades")
        };

        public static readonly Item[] Brincos =
        {
            new Item("", "Nenhum", "None"), new Item("argola", "Argola", "Hoop"), new Item("pedra", "Pedra", "Stud")
        };

        // So cores de banana: madura, verde e passada. cor = os bastoes, cor2 = as cintas.
        public static readonly Item[] Bombas =
        {
            new Item("amarela", "Amarela", "Yellow", 0xE6C24E, 0x3D4C5E), new Item("verde", "Verde", "Green", 0x9BC44A, 0x2F4A2A),
            new Item("podre", "Podre", "Rotten", 0x6B5226, 0x241B14)
        };

        static int Volta(int i, int n) { return ((i % n) + n) % n; }
        public static Item Pelo(int i) { return Pelos[Volta(i, Pelos.Length)]; }
        public static Item Chapeu(int i) { return Chapeus[Volta(i, Chapeus.Length)]; }
        public static Item Oculo(int i) { return Oculos[Volta(i, Oculos.Length)]; }
        public static Item Brinco(int i) { return Brincos[Volta(i, Brincos.Length)]; }
        public static Item Bomba(int i) { return Bombas[Volta(i, Bombas.Length)]; }

        /// <summary>Veste o macaco com o que o jogador escolheu em Personagem.</summary>
        public static void VestirJogador(MacacoVisual m)
        {
            if (m == null) return;
            m.DefinirPelo(Pelo(GameSettings.Fur).cor);
            m.Vestir(Chapeu(GameSettings.Hat).id, Oculo(GameSettings.Glasses).id, Brinco(GameSettings.Earring).id);
        }

        /// <summary>Veste um bot: pelo e acessorios sorteados, para a roda nao ficar toda igual.</summary>
        public static void VestirBot(MacacoVisual m, Color pelo)
        {
            if (m == null) return;
            // a cor e a do macaco que ocupava esse lugar na abertura
            m.DefinirPelo(pelo);
            m.Vestir(Random.value < 0.5f ? "" : Chapeus[Random.Range(1, Chapeus.Length)].id,
                     Random.value < 0.65f ? "" : Oculos[Random.Range(1, Oculos.Length)].id,
                     Random.value < 0.7f ? "" : Brincos[Random.Range(1, Brincos.Length)].id);
        }

        /// <summary>Pinta um maco de dinamite com a cor de banana escolhida. Os materiais viram copias so deste objeto.</summary>
        public static void PintaBomba(GameObject raiz, int tipo)
        {
            if (raiz == null) return;
            Item b = Bomba(tipo);
            var copias = new Dictionary<Material, Material>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                bool mudou = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null) continue;
                    bool bastao = ms[i].name.StartsWith("Dinamite"), cinta = ms[i].name.StartsWith("Cinta");
                    if (!bastao && !cinta) continue;
                    Material c;
                    if (!copias.TryGetValue(ms[i], out c)) { c = new Material(ms[i]) { name = ms[i].name }; copias[ms[i]] = c; }
                    if (c.HasProperty("_BaseColor")) c.SetColor("_BaseColor", bastao ? b.cor : b.cor2);
                    ms[i] = c; mudou = true;
                }
                if (mudou) r.sharedMaterials = ms;
            }
        }
    }
}
