using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// O painel de baixo da partida, no lugar dos textos "MACACOS 5/5", "VIDAS: 2" e "Esc: menu".
    ///
    ///   canto esquerdo: o rosto de cada macaco que ainda esta na roda (retrato tirado do proprio modelo, com a cor
    ///                   do pelo e os acessorios de cada um; o do jogador tem moldura amarela) e, embaixo, uma
    ///                   banana para cada vida do jogador;
    ///   canto direito:  os poderes guardados do jogador, na ordem. O maior e o que o Espaco usa primeiro.
    ///
    /// Le tudo dos lugares da roda a cada quadro: ninguem precisa avisar o painel de nada.
    /// </summary>
    public class PainelDaPartida : MonoBehaviour
    {
        const int Lado = 128;
        List<MonkeySlot> slots;
        RectTransform[] molduras;
        RenderTexture[] telas;
        readonly List<RawImage> bananas = new List<RawImage>(), poderes = new List<RawImage>();
        Text rotulo;
        Texture2D texBanana, texPoderes;
        int vivos = -1, vidas = -1, bolsa = -1;

        public static PainelDaPartida Criar(Transform hud, List<MonkeySlot> slots)
        {
            var go = new GameObject("Painel da partida", typeof(RectTransform));
            go.transform.SetParent(hud, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var p = go.AddComponent<PainelDaPartida>();
            p.slots = slots;
            p.Monta();
            return p;
        }

        static RectTransform Canto(GameObject go, Transform pai, bool direita, Vector2 pos, float lado)
        {
            go.transform.SetParent(pai, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(direita ? 1f : 0f, 0f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(lado, lado);
            return rt;
        }

        void Monta()
        {
            // rostos
            molduras = new RectTransform[slots.Count]; telas = new RenderTexture[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                var m = new GameObject("Rosto " + (i + 1), typeof(RectTransform), typeof(Image));
                molduras[i] = Canto(m, transform, false, Vector2.zero, 92f);
                var cor = m.GetComponent<Image>();
                cor.color = slots[i].IsHuman ? new Color(0.98f, 0.80f, 0.22f) : new Color(0.14f, 0.09f, 0.05f);
                cor.raycastTarget = false;

                telas[i] = new RenderTexture(Lado, Lado, 16) { filterMode = FilterMode.Point };
                var f = new GameObject("Retrato", typeof(RectTransform), typeof(RawImage));
                f.transform.SetParent(m.transform, false);
                var frt = (RectTransform)f.transform;
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = new Vector2(6f, 6f); frt.offsetMax = new Vector2(-6f, -6f);
                var img = f.GetComponent<RawImage>(); img.texture = telas[i]; img.raycastTarget = false;
            }

            // bananas de vida
            texBanana = DesenhaBanana();
            for (int i = 0; i < 3; i++)
            {
                var b = new GameObject("Banana de vida " + (i + 1), typeof(RectTransform), typeof(RawImage));
                Canto(b, transform, false, new Vector2(34f + i * 62f, 24f), 64f);
                var img = b.GetComponent<RawImage>(); img.texture = texBanana; img.raycastTarget = false;
                bananas.Add(img);
            }

            // poderes guardados
            texPoderes = Resources.Load<Texture2D>("Jogo/poderes");
            if (texPoderes != null) texPoderes.filterMode = FilterMode.Point;
            float x = -34f;
            for (int i = 0; i < MonkeySlot.MaxNaBolsa; i++)
            {
                float lado = i == 0 ? 108f : 70f;
                var g = new GameObject("Poder " + (i + 1), typeof(RectTransform), typeof(RawImage));
                Canto(g, transform, true, new Vector2(x, 26f), lado);
                x -= lado + 10f;
                var img = g.GetComponent<RawImage>(); img.texture = texPoderes; img.raycastTarget = false;
                g.SetActive(false);
                poderes.Add(img);
            }
            rotulo = UiFactory.CreateText(transform, "Poder da vez", "", 24, TextAnchor.LowerRight, Color.white);
            var o = rotulo.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.95f); o.effectDistance = new Vector2(2f, -2f);
            UiFactory.Anchor(rotulo, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 140f), new Vector2(900f, 32f));

            StartCoroutine(TiraRetratos());
        }

        // Uma camera so para isso fotografa a cabeca de cada macaco de frente, uma vez, no comeco da partida.
        IEnumerator TiraRetratos()
        {
            yield return null; yield return null; yield return null; // os macacos ja vestidos e na pose de parado
            var go = new GameObject("Camera dos retratos");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.93f, 0.86f, 0.68f);
            cam.nearClipPlane = 0.05f; cam.allowHDR = false; cam.allowMSAA = false;
            for (int i = 0; i < slots.Count; i++)
            {
                MacacoVisual v = slots[i].Visual;
                if (v == null || v.Cabeca == null) continue;
                float esc = v.transform.lossyScale.x;
                Vector3 frente = v.transform.forward;
                Vector3 centro = v.Cabeca.position + Vector3.up * (2.9f * esc);
                float dist = 16f * esc;
                cam.orthographicSize = 4.7f * esc;
                cam.farClipPlane = dist + 3.2f * esc; // corta o que esta atras da cabeca
                cam.transform.position = centro + frente * dist;
                cam.transform.rotation = Quaternion.LookRotation(-frente, Vector3.up);
                cam.targetTexture = telas[i];
                cam.Render();
            }
            cam.targetTexture = null;
            Destroy(go);
        }

        // Banana em lua, desenhada ponto a ponto: dentro de um circulo e fora de outro, com contorno escuro.
        static Texture2D DesenhaBanana()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            Color32 amarelo = new Color32(0xF6, 0xCC, 0x38, 0xFF), claro = new Color32(0xFF, 0xE8, 0x7A, 0xFF), tinta = new Color32(0x1E, 0x0F, 0x0C, 0xFF), ponta = new Color32(0x5A, 0x3A, 0x1C, 0xFF);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 46f)), d2 = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 62f));
                    bool dentro = d1 < 30f && d2 > 33f, borda = d1 < 33.5f && d2 > 29.5f && y < 50;
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (dentro) c = (x < 9 || x > 54) ? ponta : (d1 > 25f ? amarelo : claro);
                    else if (borda) c = tinta;
                    px[y * n + x] = c;
                }
            t.SetPixels32(px); t.Apply();
            return t;
        }

        void Update()
        {
            if (slots == null || slots.Count == 0) return;

            // rostos: so de quem ainda esta na roda, encostados um no outro
            int marca = 0;
            for (int i = 0; i < slots.Count; i++) if (slots[i].IsAlive) marca |= 1 << i;
            if (marca != vivos)
            {
                vivos = marca;
                int n = 0;
                for (int i = 0; i < slots.Count; i++)
                {
                    bool vivo = slots[i].IsAlive;
                    if (molduras[i].gameObject.activeSelf != vivo) molduras[i].gameObject.SetActive(vivo);
                    if (vivo) molduras[i].anchoredPosition = new Vector2(34f + (n++) * 100f, 98f);
                }
            }

            // bananas: as vidas do jogador
            MonkeySlot eu = slots[0];
            int v = eu.IsAlive ? eu.Lives : 0;
            if (v != vidas)
            {
                vidas = v;
                for (int i = 0; i < bananas.Count; i++) bananas[i].gameObject.SetActive(i < v);
            }

            // poderes do jogador
            int b = eu.Bolsa.Count;
            for (int i = 0; i < eu.Bolsa.Count; i++) b = b * 16 + (int)eu.Bolsa[i];
            if (b != bolsa)
            {
                bolsa = b;
                for (int i = 0; i < poderes.Count; i++)
                {
                    bool tem = i < eu.Bolsa.Count && texPoderes != null;
                    poderes[i].gameObject.SetActive(tem);
                    if (!tem) continue;
                    int k = (int)eu.Bolsa[i] - 1;
                    poderes[i].uvRect = new Rect((k % 4) / 4f, 1f - (k / 4 + 1) / 4f, 0.25f, 0.25f);
                }
                bool pt = Loc.Current == Language.Portuguese;
                rotulo.text = eu.Bolsa.Count > 0 ? (pt ? "ESPAÇO: " : "SPACE: ") + PowerUps.Nome(eu.Bolsa[0]).ToUpperInvariant() : "";
            }
        }

        void OnDestroy()
        {
            if (telas != null) foreach (var t in telas) if (t != null) t.Release();
            if (texBanana != null) Destroy(texBanana);
        }
    }
}
