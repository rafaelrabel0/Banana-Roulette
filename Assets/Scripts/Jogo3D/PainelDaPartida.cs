using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// O painel de baixo da partida, no lugar dos textos "MACACOS 5/5", "VIDAS: 2" e "Esc: menu".
    ///
    ///   canto esquerdo, em cima:  uma banana (o modelo 3D, fotografado na diagonal) para cada vida do jogador;
    ///   canto esquerdo, embaixo:  o rosto em emoji de cada macaco que ainda esta na roda, com a cor do pelo e os
    ///                             acessorios de cada um (RostoDoMacaco). O do jogador tem moldura amarela;
    ///   canto direito:            os poderes guardados do jogador, na ordem. O maior e o que o Espaco usa primeiro.
    ///
    /// Le tudo dos lugares da roda a cada quadro: ninguem precisa avisar o painel de nada.
    /// </summary>
    public class PainelDaPartida : MonoBehaviour
    {
        List<MonkeySlot> slots;
        RectTransform[] molduras;
        RawImage[] rostos;
        RenderTexture fotoBanana;
        readonly List<RawImage> bananas = new List<RawImage>(), poderes = new List<RawImage>();
        Text rotulo;
        Texture2D texPoderes;
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
            // rostos (embaixo)
            molduras = new RectTransform[slots.Count]; rostos = new RawImage[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                var m = new GameObject("Rosto " + (i + 1), typeof(RectTransform), typeof(Image));
                molduras[i] = Canto(m, transform, false, Vector2.zero, 88f);
                var cor = m.GetComponent<Image>();
                cor.color = slots[i].IsHuman ? new Color(0.98f, 0.80f, 0.22f) : new Color(0.14f, 0.09f, 0.05f);
                cor.raycastTarget = false;

                var fundo = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
                fundo.transform.SetParent(m.transform, false);
                var brt = (RectTransform)fundo.transform;
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = new Vector2(5f, 5f); brt.offsetMax = new Vector2(-5f, -5f);
                var bi = fundo.GetComponent<Image>(); bi.color = new Color(1f, 0.97f, 0.91f); bi.raycastTarget = false;

                var f = new GameObject("Emoji", typeof(RectTransform), typeof(RawImage));
                f.transform.SetParent(m.transform, false);
                var frt = (RectTransform)f.transform;
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = new Vector2(-2f, -6f); frt.offsetMax = new Vector2(2f, -2f);
                rostos[i] = f.GetComponent<RawImage>(); rostos[i].raycastTarget = false; rostos[i].uvRect = RostoDoMacaco.Recorte(0, 0);
                rostos[i].enabled = false;
            }

            // bananas de vida (em cima), afastadas uma da outra
            fotoBanana = new RenderTexture(160, 160, 16, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            for (int i = 0; i < 3; i++)
            {
                var b = new GameObject("Banana de vida " + (i + 1), typeof(RectTransform), typeof(RawImage));
                Canto(b, transform, false, new Vector2(26f + i * 104f, 118f), 100f);
                var img = b.GetComponent<RawImage>(); img.texture = fotoBanana; img.raycastTarget = false;
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

            StartCoroutine(Prepara());
        }

        IEnumerator Prepara()
        {
            yield return null; yield return null; // os macacos ja vestidos
            for (int i = 0; i < slots.Count; i++)
            {
                Texture2D t = slots[i].Rosto;
                if (t == null) continue;
                rostos[i].texture = t; rostos[i].enabled = true;
            }
            FotografaBanana();
        }

        // A banana do painel e a mesma banana 3D do chao, fotografada uma vez, na diagonal, com fundo transparente.
        void FotografaBanana()
        {
            Vector3 longe = new Vector3(0f, -300f, 0f);
            GameObject banana = MonkeySlot.NovaBanana();
            banana.transform.position = longe;
            banana.transform.rotation = Quaternion.Euler(-28f, 40f, 24f);

            var go = new GameObject("Camera da banana");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true; cam.orthographicSize = 0.46f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 8f; cam.allowHDR = false; cam.allowMSAA = false;
            Vector3 centro = banana.transform.TransformPoint(new Vector3(0f, 0.17f, 0.12f));
            Vector3 de = new Vector3(0f, 0.75f, -1f).normalized;
            cam.transform.position = centro + de * 4f;
            cam.transform.rotation = Quaternion.LookRotation(-de, Vector3.up);
            cam.targetTexture = fotoBanana;
            cam.Render();
            cam.targetTexture = null;
            Destroy(go);
            Destroy(banana);
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
                    if (vivo) molduras[i].anchoredPosition = new Vector2(34f + (n++) * 96f, 24f);
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
            if (fotoBanana != null) fotoBanana.Release();
        }
    }
}
