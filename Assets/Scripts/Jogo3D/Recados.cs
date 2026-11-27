using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Emojis e emotes da partida.
    ///
    /// EMOJI (teclas 1 a 6): um balao com uma carinha de macaco animada em 2D nasce em cima de quem manda e voa
    /// ate quem esta com a bomba. Se a bomba esta com quem manda, vai para quem passou a bomba para ele.
    /// As carinhas vem de Resources/Jogo/emojis.png: 6 linhas (riso, lingua, bravo, choro, medo, sono), 4 quadros cada.
    ///
    /// EMOTE (tecla E): o macaco de quem aperta faz a gracinha escolhida em PERSONAGEM. Nao sai com a bomba na mao.
    ///
    /// Os bots tambem mandam emoji de vez em quando.
    /// </summary>
    public class Recados : MonoBehaviour
    {
        public const int Quantos = 6;
        const int Quadros = 4;

        List<MonkeySlot> slots;
        Func<int> portador, quemPassou;
        Camera cam;
        Texture2D folha;
        readonly float[] proximo = new float[16];   // cada macaco so manda outro depois de um tempinho
        float proximoBot;

        public static Recados Criar(List<MonkeySlot> slots, Func<int> portador, Func<int> quemPassou, Camera camera, Transform canvas)
        {
            var go = new GameObject("Recados (emojis e emotes)");
            var r = go.AddComponent<Recados>();
            r.slots = slots; r.portador = portador; r.quemPassou = quemPassou; r.cam = camera;
            r.StartCoroutine(r.MontaDepois(canvas)); // espera os macacos estarem vestidos: a fileira usa o rosto do jogador
            r.proximoBot = Time.time + 6f;
            return r;
        }

        IEnumerator MontaDepois(Transform canvas)
        {
            yield return null; yield return null;
            folha = slots.Count > 0 ? slots[0].Rosto : null;
            MontaBarra(canvas);
        }

        // Fileira de carinhas no canto de cima, com o numero de cada uma.
        // As carinhas sao as do proprio jogador: mesma cor de pelo, mesmo chapeu, oculos e brinco.
        void MontaBarra(Transform canvas)
        {
            if (folha == null || canvas == null) return;
            const float lado = 54f, passo = 62f;
            for (int i = 0; i < Quantos; i++)
            {
                var go = new GameObject("Emoji " + (i + 1), typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(canvas, false);
                var img = go.GetComponent<RawImage>();
                img.texture = folha; img.raycastTarget = false;
                img.uvRect = RostoDoMacaco.Recorte(i + 1, 0);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-34f - (Quantos - 1 - i) * passo, -150f);
                rt.sizeDelta = new Vector2(lado, lado);

                var n = UiFactory.CreateText(go.transform, "Numero", (i + 1).ToString(), 22, TextAnchor.MiddleCenter, Color.white);
                n.fontStyle = FontStyle.Bold;
                var o = n.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.95f); o.effectDistance = new Vector2(2f, -2f);
                UiFactory.Anchor(n, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(lado, 26f));
            }
            var dica = UiFactory.CreateText(canvas, "DicaEmote", Loc.Current == Language.Portuguese ? "E: gracinha" : "E: emote", 22, TextAnchor.UpperRight, Color.white);
            var od = dica.gameObject.AddComponent<Outline>(); od.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.95f); od.effectDistance = new Vector2(2f, -2f);
            UiFactory.Anchor(dica, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -236f), new Vector2(360f, 30f));
        }

        void Update()
        {
            if (slots == null || slots.Count == 0) return;

            var tec = Keyboard.current;
            if (tec != null && !PausaNoCaixote.Aberta && slots[0].IsAlive)
            {
                int qual = -1;
                if (tec.digit1Key.wasPressedThisFrame || tec.numpad1Key.wasPressedThisFrame) qual = 0;
                else if (tec.digit2Key.wasPressedThisFrame || tec.numpad2Key.wasPressedThisFrame) qual = 1;
                else if (tec.digit3Key.wasPressedThisFrame || tec.numpad3Key.wasPressedThisFrame) qual = 2;
                else if (tec.digit4Key.wasPressedThisFrame || tec.numpad4Key.wasPressedThisFrame) qual = 3;
                else if (tec.digit5Key.wasPressedThisFrame || tec.numpad5Key.wasPressedThisFrame) qual = 4;
                else if (tec.digit6Key.wasPressedThisFrame || tec.numpad6Key.wasPressedThisFrame) qual = 5;
                if (qual >= 0) Mandar(slots[0], qual);
                if (tec.eKey.wasPressedThisFrame) Gracinha(slots[0], GameSettings.Emote);
            }

            // bots: de vez em quando um deles manda um emoji (medo se esta com a bomba, provocacao se nao esta)
            if (Time.time >= proximoBot)
            {
                proximoBot = Time.time + UnityEngine.Random.Range(4f, 10f);
                int i = UnityEngine.Random.Range(1, slots.Count);
                if (slots[i].IsAlive)
                {
                    bool comBomba = portador() == i;
                    int[] opcoes = comBomba ? new[] { 4, 3 } : new[] { 0, 1, 0, 2, 5 };
                    Mandar(slots[i], opcoes[UnityEngine.Random.Range(0, opcoes.Length)]);
                }
            }
        }

        /// <summary>Manda um emoji (0 a 5) de um macaco para quem esta com a bomba.</summary>
        public void Mandar(MonkeySlot de, int emoji)
        {
            if (de == null || !de.IsAlive || de.Rosto == null) return;
            if (Time.time < proximo[de.Index % proximo.Length]) return;
            proximo[de.Index % proximo.Length] = Time.time + 1.2f;

            int alvo = portador();
            if (alvo == de.Index) alvo = quemPassou();
            MonkeySlot para = alvo >= 0 && alvo < slots.Count && alvo != de.Index && slots[alvo].IsAlive ? slots[alvo] : null;
            // O balao fica em cima de quem mandou: voando ate o outro, um emoji tampava o do vizinho.
            StartCoroutine(Voa(de, null, Mathf.Clamp(emoji, 0, Quantos - 1)));
            Som(emoji);
        }

        /// <summary>O macaco faz a gracinha escolhida. Com a bomba na mao nao sai (ele esta ocupado).</summary>
        public void Gracinha(MonkeySlot quem, int qual)
        {
            if (quem == null || !quem.IsAlive || quem.Visual == null || quem.Holding) return;
            quem.Visual.Tocar(Aparencia.Emote(qual).id, false, 2);
            SomRetro.Grito(520f, 780f, 0.12f, 0.35f); SomRetro.Grito(600f, 900f, 0.12f, 0.35f, 0.16f);
        }

        static void Som(int emoji)
        {
            switch (emoji)
            {
                case 0: SomRetro.Grito(520f, 640f, 0.07f, 0.3f); SomRetro.Grito(560f, 680f, 0.07f, 0.3f, 0.12f); SomRetro.Grito(600f, 720f, 0.09f, 0.3f, 0.24f); break;
                case 1: SomRetro.Tom(520f, 0.22f, "sawtooth", 0.07f, 260f); break;
                case 2: SomRetro.Grito(320f, 220f, 0.3f, 0.4f); break;
                case 3: SomRetro.Tom(700f, 0.2f, "sine", 0.08f, 420f); SomRetro.Tom(660f, 0.25f, "sine", 0.08f, 380f, 0.25f); break;
                case 4: SomRetro.Grito(700f, 1000f, 0.1f, 0.35f); SomRetro.Grito(760f, 1080f, 0.1f, 0.35f, 0.13f); break;
                default: SomRetro.Tom(220f, 0.5f, "sine", 0.06f, 150f); break;
            }
        }

        IEnumerator Voa(MonkeySlot de, MonkeySlot para, int emoji)
        {
            // um retangulo proprio: o recorte da carinha vai nas coordenadas da malha (o material de sprite ignora recorte)
            var go = new GameObject("Balao de emoji", typeof(MeshFilter), typeof(MeshRenderer));
            var malha = new UnityEngine.Mesh();
            malha.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f) };
            malha.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            go.GetComponent<MeshFilter>().sharedMesh = malha;
            var r = go.GetComponent<Renderer>();
            var m = new Material(Shader.Find("Sprites/Default"));
            m.mainTexture = de.Rosto; // o emoji tem a cara de quem mandou
            int quadroAtual = -1;
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            const float tamanho = 1.9f, altura = 4.4f;
            float t = 0f, fim = para != null ? 2.5f : 1.8f;
            Vector3 sai = de.transform.position + Vector3.up * altura;
            while (t < fim)
            {
                t += Time.deltaTime;
                Vector3 p = de != null ? de.transform.position + Vector3.up * altura : sai;
                if (para != null)
                {
                    // fica um instante em cima de quem mandou, voa em arco e pousa em cima do alvo
                    float k = Mathf.Clamp01((t - 0.45f) / 0.5f); k = k * k * (3f - 2f * k);
                    Vector3 chega = para.transform.position + Vector3.up * altura;
                    p = Vector3.Lerp(p, chega, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.6f);
                }
                p.y += Mathf.Sin(t * 9f) * 0.08f;
                go.transform.position = p;
                if (cam != null) go.transform.rotation = cam.transform.rotation;

                // nasce estufando e some encolhendo
                float porte = t < 0.15f ? Mathf.Lerp(0.2f, 1.15f, t / 0.15f) : t < 0.25f ? Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.1f) : t > fim - 0.2f ? Mathf.Lerp(1f, 0f, (t - (fim - 0.2f)) / 0.2f) : 1f;
                go.transform.localScale = Vector3.one * tamanho * porte;

                int quadro = Mathf.FloorToInt(t * 8f) % Quadros;
                if (quadro != quadroAtual)
                {
                    quadroAtual = quadro;
                    Rect rc = RostoDoMacaco.Recorte(emoji + 1, quadro);
                    float u0 = rc.xMin, u1 = rc.xMax, v0 = rc.yMin, v1 = rc.yMax;
                    malha.uv = new[] { new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0) };
                }
                yield return null;
            }
            Destroy(go);
            Destroy(m);
            Destroy(malha);
        }
    }
}
