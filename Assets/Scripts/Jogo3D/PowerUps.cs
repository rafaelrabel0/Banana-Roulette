using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Os poderes da partida.
    ///
    /// Como se ganha: so aparece para quem esta com a bomba, com uma sequencia propria de 3 teclas cuja primeira
    /// e sempre diferente da primeira tecla da bomba. A primeira tecla apertada decide qual das duas o jogador
    /// esta fazendo. Errar a sequencia do poder perde o poder e conta como erro.
    ///
    /// Como se usa: Espaco. Cada macaco guarda um por vez.
    ///   - AJUDA  (vale para quem usa): vida extra e pavio molhado a qualquer hora; meia sequencia e passe
    ///     relampago so com a bomba na mao.
    ///   - ATAQUE (vai em quem esta com a bomba): so funciona quando a bomba NAO esta com quem usa.
    ///
    /// Para criar um poder novo: acrescentar aqui (nome, tipo) e tratar o efeito em GameManager.UsarPowerUp.
    /// </summary>
    public enum PowerUp
    {
        Nenhum,
        VidaExtra,       // +1 vida (no maximo 3)
        PavioMolhado,    // o pavio para de queimar por 3 segundos
        MeiaSequencia,   // a sequencia de quem usa cai pela metade
        PasseRelampago,  // passa a bomba na hora, sem terminar a sequencia
        Invertido,       // ataque: as teclas do portador ficam trocadas ate o fim do turno
        Dobrada,         // ataque: a sequencia do portador dobra de tamanho
        PavioCurto,      // ataque: queima 15% do pavio de uma vez
        Nevoa,           // ataque: a sequencia do portador some da tela (vira memoria)
        Cola             // ataque: o portador fica 2 segundos sem conseguir apertar nada
    }

    public static class PowerUps
    {
        static string T(string pt, string en) { return Loc.Current == Language.Portuguese ? pt : en; }

        public static string Nome(PowerUp p)
        {
            switch (p)
            {
                case PowerUp.VidaExtra: return T("Vida extra", "Extra life");
                case PowerUp.PavioMolhado: return T("Pavio molhado", "Wet fuse");
                case PowerUp.MeiaSequencia: return T("Meia sequência", "Half sequence");
                case PowerUp.PasseRelampago: return T("Passe relâmpago", "Lightning pass");
                case PowerUp.Invertido: return T("Comandos invertidos", "Reversed keys");
                case PowerUp.Dobrada: return T("Sequência dobrada", "Double sequence");
                case PowerUp.PavioCurto: return T("Pavio curto", "Short fuse");
                case PowerUp.Nevoa: return T("Névoa", "Fog");
                case PowerUp.Cola: return T("Cola", "Glue");
                default: return "";
            }
        }

        /// <summary>Em uma linha, o que faz e quando usar. Aparece ao lado do nome.</summary>
        public static string Dica(PowerUp p)
        {
            switch (p)
            {
                case PowerUp.VidaExtra: return T("ganha uma vida", "gain a life");
                case PowerUp.PavioMolhado: return T("o pavio para 3 s", "fuse stops for 3 s");
                case PowerUp.MeiaSequencia: return T("use com a bomba: corta sua sequência", "use with the bomb: halves your sequence");
                case PowerUp.PasseRelampago: return T("use com a bomba: passa na hora", "use with the bomb: pass now");
                case PowerUp.Invertido: return T("ataque: troca as teclas de quem está com a bomba", "attack: swaps the holder's keys");
                case PowerUp.Dobrada: return T("ataque: dobra a sequência de quem está com a bomba", "attack: doubles the holder's sequence");
                case PowerUp.PavioCurto: return T("ataque: queima um pedaço do pavio", "attack: burns a chunk of fuse");
                case PowerUp.Nevoa: return T("ataque: esconde a sequência de quem está com a bomba", "attack: hides the holder's sequence");
                case PowerUp.Cola: return T("ataque: trava quem está com a bomba por 2 s", "attack: freezes the holder for 2 s");
                default: return "";
            }
        }

        /// <summary>Ataque = vai em quem esta com a bomba e so funciona quando ela nao esta com quem usa.</summary>
        public static bool EhAtaque(PowerUp p)
        {
            return p == PowerUp.Invertido || p == PowerUp.Dobrada || p == PowerUp.PavioCurto || p == PowerUp.Nevoa || p == PowerUp.Cola;
        }

        /// <summary>So funciona com a bomba na mao de quem usa.</summary>
        public static bool PrecisaDaBomba(PowerUp p)
        {
            return p == PowerUp.MeiaSequencia || p == PowerUp.PasseRelampago;
        }

        public static PowerUp Sortear()
        {
            return (PowerUp)Random.Range(1, 10);
        }
    }
}
