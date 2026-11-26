using System.Collections;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// O pavio do topo da tela, em 3D: um cordao que atravessa a tela e termina no maco de dinamite,
    /// a direita. A faisca nasce na ponta esquerda e anda ate a bomba conforme o pavio queima.
    ///
    /// Fica preso a camera, bem perto dela, para nunca atravessar o cenario. Refaz o tamanho sozinho
    /// quando a janela muda de proporcao. Recebe as mesmas ordens que a barra antiga (TopFuseBar).
    /// </summary>
    public class PavioTopo3D : MonoBehaviour
    {
        const float Distancia = 3f;
        static readonly Color CorCordao = new Color32(0xD8, 0xC0, 0x8E, 0xFF);

        Camera cam;
        Transform bomba, faisca;
        UnityEngine.Mesh malha;
        Material matCordao, matFaisca;
        MeshRenderer cordao;
        Vector3[] curva;
        Vector3 bombaCasa;
        float escalaBomba, raio, proporcao = -1f, campo = -1f;
        float queimado, clarao;
        bool estourou;

        public static PavioTopo3D Criar(Camera camera)
        {
            var go = new GameObject("Pavio do topo (3D)");
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, Distancia);
            var p = go.AddComponent<PavioTopo3D>();
            p.cam = camera;
            p.Montar();
            return p;
        }

        static Material Tinta(Color cor)
        {
            var m = new Material(Shader.Find("BananaRoulette/Retro"));
            m.SetColor("_BaseColor", cor); m.SetFloat("_SemLuz", 1f); m.SetFloat("_Cull", 0f);
            return m;
        }

        void Montar()
        {
            var c = new GameObject("Cordao", typeof(MeshFilter), typeof(MeshRenderer));
            c.transform.SetParent(transform, false);
            malha = new UnityEngine.Mesh();
            c.GetComponent<MeshFilter>().sharedMesh = malha;
            cordao = c.GetComponent<MeshRenderer>();
            matCordao = Tinta(CorCordao);
            cordao.sharedMaterial = matCordao;
            cordao.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            f.name = "Faisca";
            Destroy(f.GetComponent<Collider>());
            f.transform.SetParent(transform, false);
            faisca = f.transform;
            matFaisca = Tinta(new Color(1f, 0.62f, 0.12f));
            var rf = f.GetComponent<MeshRenderer>();
            rf.sharedMaterial = matFaisca;
            rf.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var modelo = Resources.Load<GameObject>("Jogo/maco-jogo");
            var b = new GameObject("Bomba");
            b.transform.SetParent(transform, false);
            bomba = b.transform;
            if (modelo != null)
            {
                var copia = Instantiate(modelo, bomba);
                copia.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // a boca do pavio virada para a esquerda
                VisualRetro.Converter(copia);
                foreach (Transform t in copia.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("Pavio") || t.name.StartsWith("Faisca")) t.gameObject.SetActive(false);
                foreach (Renderer r in copia.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    // copias sem sombra recebida: a sombra das arvores nao deve cair no que esta colado na tela
                    Material[] ms = r.sharedMaterials;
                    for (int i = 0; i < ms.Length; i++) if (ms[i] != null) { ms[i] = new Material(ms[i]) { name = ms[i].name }; ms[i].SetFloat("_SemSombra", 1f); }
                    r.sharedMaterials = ms;
                }
            }
            Ajustar();
            ResetFuse();
        }

        // Recalcula o desenho para a proporcao atual da tela.
        void Ajustar()
        {
            float a = (float)Screen.width / Mathf.Max(1, Screen.height);
            if (Mathf.Approximately(a, proporcao) && Mathf.Approximately(cam.fieldOfView, campo)) return;
            proporcao = a; campo = cam.fieldOfView;

            float meiaAlt = Distancia * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), meiaLarg = meiaAlt * a;
            float y = meiaAlt * 0.86f, xEsq = -meiaLarg * 0.90f, xDir = meiaLarg * 0.64f;
            raio = meiaAlt * 0.016f;

            // do lado da bomba para a ponta esquerda: o que sobra do pavio e sempre o trecho junto da bomba
            const int n = 40;
            curva = new Vector3[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float k = (float)i / n;
                curva[i] = new Vector3(Mathf.Lerp(xDir, xEsq, k), y + Mathf.Sin(k * Mathf.PI * 5f) * meiaAlt * 0.022f - Mathf.Sin(k * Mathf.PI) * meiaAlt * 0.03f, 0f);
            }
            escalaBomba = meiaLarg * 0.17f / 4.2f; // o maco tem 4,2 de comprimento
            bombaCasa = new Vector3(xDir + meiaLarg * 0.09f, y, 0f);
            bomba.localPosition = bombaCasa;
            bomba.localScale = Vector3.one * escalaBomba;
            Desenhar();
        }

        void Desenhar()
        {
            if (estourou) return;
            float resta = Mathf.Clamp01(1f - queimado);
            Vector3 ponta = BananaCarrier.DesenhaCordao(malha, curva, Mathf.Max(0.01f, resta), raio);
            faisca.localPosition = ponta;
        }

        void LateUpdate()
        {
            Ajustar();
            if (estourou) return;

            float ritmo = Mathf.Lerp(5f, 26f, queimado);
            float pulso = 0.65f + Mathf.Abs(Mathf.Sin(Time.time * ritmo)) * 0.85f;
            faisca.localScale = Vector3.one * raio * 5f * pulso * (1f + clarao * 1.5f);
            matFaisca.SetColor("_BaseColor", Color.Lerp(new Color(1f, 0.62f, 0.12f), Color.white, Mathf.Clamp01(queimado * 0.5f + clarao)));

            // a bomba treme mais conforme o pavio acaba
            float panico = Mathf.Clamp01((queimado - 0.5f) / 0.5f);
            bomba.localPosition = bombaCasa + new Vector3(Mathf.Sin(Time.time * 37f), Mathf.Cos(Time.time * 29f), 0f) * raio * 1.6f * panico;
            bomba.localScale = Vector3.one * escalaBomba * (1f + panico * 0.12f * pulso);
            bomba.localRotation = Quaternion.Euler(0f, 0f, -8f + Mathf.Sin(Time.time * 2f) * 3f);

            if (clarao > 0f)
            {
                clarao = Mathf.Max(0f, clarao - Time.deltaTime / 0.32f);
                matCordao.SetColor("_BaseColor", Color.Lerp(CorCordao, Palette.DynamiteRed, clarao));
            }
        }

        // ------------------------------------------------------------------ mesmas ordens da barra antiga

        public void ResetFuse()
        {
            estourou = false; queimado = 0f; clarao = 0f;
            cordao.enabled = true; faisca.gameObject.SetActive(true); bomba.gameObject.SetActive(true);
            matCordao.SetColor("_BaseColor", CorCordao);
            Desenhar();
        }

        public void SetProgress(float fracaoQueimada)
        {
            if (estourou) return;
            queimado = Mathf.Clamp01(fracaoQueimada);
            Desenhar();
        }

        public void FlashBurn() { if (!estourou) clarao = 1f; }

        public IEnumerator Explode()
        {
            if (estourou) yield break;
            estourou = true;
            cordao.enabled = false; faisca.gameObject.SetActive(false);
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                bomba.localScale = Vector3.one * escalaBomba * (1f + t / 0.12f * 0.9f);
                yield return null;
            }
            bomba.gameObject.SetActive(false);
        }
    }
}
