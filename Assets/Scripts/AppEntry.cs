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

        public static void GoTo(Screen screen)
        {
            Next = screen;
            string cena = screen == Screen.Menu ? CenaDoMenu : CenaDoJogo;

            // Cena fora da lista de build (File > Build Profiles): recarrega a atual, como era antes.
            if (Application.CanStreamedLevelBeLoaded(cena)) SceneManager.LoadScene(cena);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
