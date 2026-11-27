using UnityEngine;
using UnityEngine.SceneManagement;

namespace BananaRoulette
{
    /// <summary>
    /// Diz para qual tela o jogo vai.
    ///
    /// Sao duas cenas:
    ///   - "Abertura": o menu em 3D nos caixotes e a cena de abertura;
    ///   - "Prototipo": a partida e o tutorial, montados por codigo pelo GameBootstrap.
    ///
    /// Trocar de tela recarrega a cena de proposito: e a forma mais barata de
    /// garantir estado limpo entre partidas, sem precisar desmontar a roda,
    /// zerar corrotinas e ressuscitar macaco na mao.
    /// </summary>
    public static class AppEntry
    {
        public enum Screen { Menu, Game, Tutorial }

        public const string CenaDoMenu = "Abertura";
        public const string CenaDoJogo = "Prototipo";

        public static Screen Next = Screen.Menu;

        private static UnityEngine.AsyncOperation carga;

        /// <summary>Se alguem cuida da troca de tela sem carregar cena, registra-se aqui e devolve verdadeiro.</summary>
        public static System.Func<Screen, bool> Roteador;

        public static void GoTo(Screen screen)
        {
            // Na cena unica (menu + abertura + partida) nao ha troca de cena: o menu desmonta e monta a partida no lugar.
            if (Roteador != null && Roteador(screen)) return;
            if (carga != null && !carga.isDone) return; // ja ha uma troca a caminho
            Next = screen;
            string cena = screen == Screen.Menu ? CenaDoMenu : CenaDoJogo;

            // Cena fora da lista de build (File > Build Profiles): recarrega a atual, como era antes.
            // A musica baixa antes da troca e sobe de novo na cena nova (quem chega chama MusicaRetro.Volume(1, ...)).
            // A cena carrega em segundo plano, para o tranco ser o menor possivel.
            MusicaRetro.Volume(0.3f, 0.25f);
            if (Application.CanStreamedLevelBeLoaded(cena)) carga = SceneManager.LoadSceneAsync(cena);
            else carga = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
