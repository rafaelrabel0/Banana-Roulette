using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// O mundo da abertura (floresta, caixotes, roda) e a partida usam a MESMA cena. O modelo vem na medida
    /// da previa; aqui ele e encolhido, girado e deslocado uma vez so, de forma que:
    ///   - o centro da roda caia na origem;
    ///   - o lugar do jogador (o macaco que recebe a bomba no fim da abertura) fique em (0, 0, -raio);
    ///   - os caixotes aparecam ao fundo, por cima da roda.
    /// Com isso o codigo da partida continua trabalhando nas medidas dele, e a abertura vira a partida sem corte.
    /// </summary>
    public static class Mundo
    {
        /// <summary>Quanto o modelo da previa encolhe para a roda ter o raio do jogo.</summary>
        public static float Escala(float raioDoJogo) { return raioDoJogo / MacacoVisual.RaioDaPrevia; }

        /// <summary>Poe a raiz do modelo da abertura no lugar (ver o resumo da classe).</summary>
        public static void Aplicar(Transform raiz, float raioDoJogo)
        {
            float e = Escala(raioDoJogo);
            Vector3 centro = MacacoVisual.CentroDaRoda;
            Vector3[] lugares = MacacoVisual.Lugares;
            Vector3 rumo = lugares != null && lugares.Length > 1 ? lugares[1] - centro : Vector3.forward;
            rumo.y = 0f;
            Quaternion giro = Quaternion.Euler(0f, Vector3.SignedAngle(rumo, Vector3.back, Vector3.up), 0f);
            raiz.localScale = Vector3.one * e;
            raiz.rotation = giro;
            raiz.position = -(giro * (centro * e));
        }

        /// <summary>Qual lugar da roda da abertura (0 a 4) fica mais perto deste ponto do jogo. Serve para repetir a cor do pelo.</summary>
        public static int LugarMaisPerto(Transform raiz, Vector3 ponto)
        {
            Vector3[] lugares = MacacoVisual.Lugares;
            if (raiz == null || lugares == null) return 0;
            int melhor = 0; float menor = float.MaxValue;
            for (int i = 0; i < lugares.Length; i++)
            {
                Vector3 p = raiz.TransformPoint(lugares[i]);
                float dist = (new Vector2(p.x, p.z) - new Vector2(ponto.x, ponto.z)).sqrMagnitude;
                if (dist < menor) { menor = dist; melhor = i; }
            }
            return melhor;
        }

        /// <summary>Os personagens da abertura: nao sao cenario, entao a clareira nao mexe neles e a partida os esconde.</summary>
        public static bool EhAtor(string nome)
        {
            return nome.StartsWith("MacacoHeroi") || nome.StartsWith("MacacoRoda") || nome.StartsWith("Aviador") ||
                   nome.StartsWith("Balao") || nome.StartsWith("Bituca") || nome.StartsWith("MacoAlvo") || nome.StartsWith("Camera");
        }

        /// <summary>
        /// Clareira: esconde tronco, copa e pedra alta que ficariam dentro da roda ou entre ela e a camera da partida.
        /// </summary>
        public static void Clareira(Transform raiz, Vector3 cameraDoJogo, float raioDoJogo)
        {
            Vector2 de = new Vector2(cameraDoJogo.x, cameraDoJogo.z), ate = new Vector2(0f, raioDoJogo), eixo = ate - de;
            foreach (Transform filho in raiz)
            {
                if (EhAtor(filho.name)) continue;
                foreach (Renderer r in filho.GetComponentsInChildren<Renderer>(true))
                {
                    Bounds b = r.bounds;
                    if (b.size.y < 0.8f) continue; // capim e chao ficam
                    Vector2 p = new Vector2(b.center.x, b.center.z);
                    float k = Mathf.Clamp01(Vector2.Dot(p - de, eixo) / eixo.sqrMagnitude);
                    float dist = (p - (de + eixo * k)).magnitude;
                    if (dist < raioDoJogo + 1.5f + Mathf.Max(b.extents.x, b.extents.z) * 0.5f) r.enabled = false;
                }
            }
        }

        /// <summary>Onde a camera da partida fica, olhando a roda por cima das costas do jogador.</summary>
        public static Vector3 CameraDoJogo(float raioDoJogo) { return new Vector3(0f, 9.4f, -(raioDoJogo + 7.6f)); }
        public static readonly Vector3 OlharDoJogo = new Vector3(0f, 0.9f, 0.4f);
    }
}
