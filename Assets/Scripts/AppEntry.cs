using UnityEngine.SceneManagement;

namespace BananaRoulette
{
    /// <summary>
    /// Diz para qual tela a cena deve abrir quando carregar.
    ///
    /// Trocar de tela recarrega a cena de proposito: e a forma mais barata de
    /// garantir estado limpo entre partidas, sem precisar desmontar a roda,
    /// zerar corrotinas e ressuscitar macaco na mao.
    /// </summary>
    public static class AppEntry
    {
        public enum Screen { Menu, Game, Tutorial }

        public static Screen Next = Screen.Menu;

        public static void GoTo(Screen screen)
        {
            Next = screen;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
