using System.Collections;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// A explosao da bomba, no visual do jogo: um clarao, bolas de fogo em poucos triangulos que passam de
    /// amarelo para laranja, vermelho e fumaca, estilhacos voando e uma marca de queimado que fica no chao.
    ///
    /// Uso: Explosao.Criar(posicao, 1f);
    /// </summary>
    public class Explosao : MonoBehaviour
    {
        static readonly Color Amarelo = new Color32(0xFF, 0xE1, 0x4A, 0xFF), Laranja = new Color32(0xF2, 0x8A, 0x1B, 0xFF),
                              Vermelho = new Color32(0xC2, 0x3B, 0x2A, 0xFF), Fumaca = new Color32(0x4A, 0x40, 0x3A, 0xFF),
                              Queimado = new Color32(0x24, 0x1C, 0x16, 0xFF);

        static UnityEngine.Mesh bola, mancha, lasca;

        /// <summary>Cria a explosao. tamanho 1 = a da partida. A marca no chao fica; o resto some sozinho.</summary>
        public static void Criar(Vector3 onde, float tamanho = 1f, bool comMarca = true)
        {
            var go = new GameObject("Explosao");
            go.transform.position = onde;
            go.AddComponent<Explosao>().StartCoroutine_Auto(tamanho, comMarca);
        }

        void StartCoroutine_Auto(float tamanho, bool comMarca) { StartCoroutine(Roda(tamanho, comMarca)); }

        static Material Tinta(Color cor)
        {
            var m = new Material(Shader.Find("BananaRoulette/Retro"));
            m.SetColor("_BaseColor", cor); m.SetFloat("_SemLuz", 1f); m.SetFloat("_Cull", 0f);
            return m;
        }

        Transform Peca(UnityEngine.Mesh malha, Material m)
        {
            var go = new GameObject("peca", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = malha;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        IEnumerator Roda(float tam, bool comMarca)
        {
            Malhas();

            // marca de queimado: fica no chao, fora deste objeto, para nao sumir com ele
            if (comMarca)
            {
                var marca = new GameObject("Marca de explosao", typeof(MeshFilter), typeof(MeshRenderer));
                marca.transform.position = new Vector3(transform.position.x, 0.04f, transform.position.z);
                marca.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                marca.transform.localScale = Vector3.one * tam * Random.Range(1.5f, 1.9f);
                marca.GetComponent<MeshFilter>().sharedMesh = mancha;
                var rm = marca.GetComponent<MeshRenderer>();
                rm.sharedMaterial = Tinta(Queimado);
                rm.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // clarao
            var matClarao = Tinta(Color.white);
            Transform clarao = Peca(bola, matClarao);

            // bolas de fogo
            const int n = 9;
            var bolas = new Transform[n]; var mats = new Material[n]; var rumos = new Vector3[n]; var vidas = new float[n]; var portes = new float[n];
            for (int i = 0; i < n; i++)
            {
                mats[i] = Tinta(Amarelo);
                bolas[i] = Peca(bola, mats[i]);
                bolas[i].localRotation = Random.rotation;
                Vector3 d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.9f + 0.25f;
                rumos[i] = d.normalized * Random.Range(1.2f, 2.6f) * tam;
                vidas[i] = Random.Range(0.55f, 1.05f);
                portes[i] = Random.Range(0.7f, 1.25f) * tam;
            }

            // estilhacos
            const int ne = 10;
            var lascas = new Transform[ne]; var vel = new Vector3[ne];
            var matLasca = Tinta(Laranja);
            for (int i = 0; i < ne; i++)
            {
                lascas[i] = Peca(lasca, i % 2 == 0 ? matLasca : mats[0]);
                Vector3 d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) + 0.4f;
                vel[i] = d.normalized * Random.Range(5f, 10f) * tam;
            }

            float t = 0f, fim = 1.25f;
            while (t < fim)
            {
                t += Time.deltaTime;

                // clarao: cresce num estalo e some
                float kc = Mathf.Clamp01(t / 0.16f);
                clarao.localScale = Vector3.one * tam * (kc < 1f ? Mathf.Lerp(0.4f, 2.6f, kc) : 0f);

                for (int i = 0; i < n; i++)
                {
                    float k = Mathf.Clamp01(t / vidas[i]);
                    bolas[i].localPosition = rumos[i] * (1f - (1f - k) * (1f - k)) + Vector3.up * (k * k * 1.6f * tam);
                    // cresce depressa, encolhe devagar
                    float porte = k < 0.25f ? Mathf.Lerp(0.3f, 1f, k / 0.25f) : Mathf.Lerp(1f, 0f, (k - 0.25f) / 0.75f);
                    bolas[i].localScale = Vector3.one * portes[i] * porte;
                    bolas[i].Rotate(40f * Time.deltaTime, 70f * Time.deltaTime, 0f);
                    // a cor anda em degraus, como desenho: amarelo, laranja, vermelho, fumaca
                    mats[i].SetColor("_BaseColor", k < 0.2f ? Amarelo : k < 0.45f ? Laranja : k < 0.7f ? Vermelho : Fumaca);
                }

                for (int i = 0; i < ne; i++)
                {
                    vel[i] += Vector3.down * 18f * tam * Time.deltaTime;
                    lascas[i].localPosition += vel[i] * Time.deltaTime;
                    lascas[i].Rotate(600f * Time.deltaTime, 400f * Time.deltaTime, 0f);
                    lascas[i].localScale = Vector3.one * tam * Mathf.Lerp(0.5f, 0f, t / fim);
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ malhas em poucos triangulos

        static void Malhas()
        {
            if (bola != null) return;

            // solido de 20 faces (a "esfera" do visual retro)
            float f = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var v = new[]
            {
                new Vector3(-1, f, 0), new Vector3(1, f, 0), new Vector3(-1, -f, 0), new Vector3(1, -f, 0),
                new Vector3(0, -1, f), new Vector3(0, 1, f), new Vector3(0, -1, -f), new Vector3(0, 1, -f),
                new Vector3(f, 0, -1), new Vector3(f, 0, 1), new Vector3(-f, 0, -1), new Vector3(-f, 0, 1)
            };
            for (int i = 0; i < v.Length; i++) v[i] = v[i].normalized;
            bola = new UnityEngine.Mesh { name = "Bola de fogo" };
            bola.vertices = v;
            bola.triangles = new[] { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            bola.RecalculateBounds();

            // mancha no chao: uma estrela torta de 12 pontas, deitada
            const int pontas = 12;
            var vm = new Vector3[pontas + 1]; var tm = new int[pontas * 3];
            vm[0] = Vector3.zero;
            var sorte = new System.Random(7);
            for (int i = 0; i < pontas; i++)
            {
                float a = i * Mathf.PI * 2f / pontas, r = (i % 2 == 0 ? 1f : 0.62f) * (0.85f + (float)sorte.NextDouble() * 0.3f);
                vm[i + 1] = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                tm[i * 3] = 0; tm[i * 3 + 1] = 1 + (i + 1) % pontas; tm[i * 3 + 2] = 1 + i;
            }
            mancha = new UnityEngine.Mesh { name = "Marca de queimado" };
            mancha.vertices = vm; mancha.triangles = tm; mancha.RecalculateBounds();

            lasca = new UnityEngine.Mesh { name = "Estilhaco" };
            lasca.vertices = new[] { new Vector3(-0.3f, 0f, 0f), new Vector3(0.3f, 0.1f, 0f), new Vector3(0f, 0.5f, 0.1f) };
            lasca.triangles = new[] { 0, 1, 2 };
            lasca.RecalculateBounds();
        }
    }
}
