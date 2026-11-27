using UnityEngine;
using UnityEngine.SceneManagement;

namespace BananaRoulette
{
    /// <summary>
    /// Som de fundo da floresta: passarinho e folha mexendo. Inseto e macaco ao longe sairam: no menu pareciam
    /// pavio queimando e macaco jogando, e confundiam.
    /// Tudo sorteado e fabricado por codigo (SomRetro). Entra sozinho em toda cena.
    /// </summary>
    public class AmbienteSelva : MonoBehaviour
    {
        float passaro, folha;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            SceneManager.sceneLoaded += (cena, modo) => Criar();
            Criar();
        }

        static void Criar()
        {
            if (FindFirstObjectByType<AmbienteSelva>() != null) return;
            new GameObject("Ambiente da selva").AddComponent<AmbienteSelva>();
        }

        void Start()
        {
            passaro = 1.5f; folha = 3f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // passarinho: dois ou tres pios curtos, subindo
            passaro -= dt;
            if (passaro <= 0f)
            {
                passaro = Random.Range(2.5f, 7f);
                float[] tons = { 2100f, 2500f, 2900f, 3300f };
                float f = tons[Random.Range(0, tons.Length)];
                int n = Random.Range(2, 4);
                for (int i = 0; i < n; i++) SomRetro.Tom(f, 0.07f, "sine", 0.045f, f * 1.25f, i * 0.11f);
            }

            // folhas mexendo com o vento
            folha -= dt;
            if (folha <= 0f)
            {
                folha = Random.Range(5f, 11f);
                SomRetro.Ruido(Random.value < 0.5f ? 1.2f : 1.8f, 0.03f, 3500f, "highpass");
            }
        }
    }
}
