using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Menu inicial em 3D, na propria cena de abertura. Os caixotes nao se mexem: so a camera anda.
    ///
    ///   espera   -> clique: o carimbo bate na tampa do caixote principal (logo do estudio)
    ///   titulo   -> camera desce para a frente do caixote (logo do jogo)
    ///   menu     -> frente do caixote da esquerda: JOGAR, OPCOES, TUTORIAL, CREDITOS
    ///   opcoes   -> costas do mesmo caixote; cada item abre a pagina na tampa do caixote de tras
    ///   creditos -> escritos no chao
    ///   cena     -> JOGAR: a abertura toca e, no fim, comeca a partida (cena Prototipo)
    ///
    /// Posicoes de camera e areas de clique vem de Resources/Abertura/menu.json, exportado da previa.
    /// Os numeros de la estao no eixo do three.js; aqui o X e espelhado (funcao V).
    /// </summary>
    public class MenuAbertura : MonoBehaviour
    {
        public AberturaPlayer tocador;
        public Camera cam;

        enum Modo { Espera, Carimbo, Titulo, Menu, Opcoes, Pagina, Creditos, Cena, Fim }

        [Serializable] class CamJ { public string nome; public float[] p; public float[] o; public float f = 32f; }
        [Serializable] class OpcJ { public string nome; public float[] r; }
        [Serializable]
        class DadosJ
        {
            public CamJ[] cams; public OpcJ[] menu; public OpcJ[] opcoes;
            public float[] caixote; public float[] esquerda; public float tampaY = 6.54f; public float[] creditos;
        }

        // Na primeira vez o carimbo bate; voltando da partida, o menu abre direto.
        static bool jaCarimbou;

        DadosJ d;
        Modo modo = Modo.Espera, modoAnt = Modo.Espera, origem = Modo.Menu;
        float relogio, camFov = 32f, fimEm = -1f;
        Vector3 camPos, camOlhar;
        int sel, selO;
        bool arcoAtivo, arcoIda; float arcoK;

        Transform caixote, carimbo, seta;
        Material matTampa, matPagina, matCreditos;
        Texture texLisa, texEstudio;
        Renderer creditos;
        Text dica, letreiro;
        Canvas tela;
        SettingsScreen ajustes;
        bool pronto;

        static Vector3 V(float x, float y, float z) { return new Vector3(-x, y, z); }
        static Vector3 V(float[] a) { return new Vector3(-a[0], a[1], a[2]); }
        static float Suave(float k) { k = Mathf.Clamp01(k); return k * k * (3f - 2f * k); }
        static float Seg(float r, float a, float b) { return Mathf.Clamp01((r - a) / (b - a)); }

        void Awake()
        {
            GameSettings.Load();
            d = JsonUtility.FromJson<DadosJ>(Resources.Load<TextAsset>("Abertura/menu").text);
            if (tocador == null) tocador = GetComponent<AberturaPlayer>();
            if (cam == null) cam = Camera.main;
            tocador.tocarAoIniciar = false;
            tocador.controlaCamera = false;
            tocador.aoTerminar.AddListener(() => { if (modo == Modo.Cena) { modo = Modo.Fim; fimEm = Time.time + 1.6f; } });

            texLisa = Resources.Load<Texture2D>("Abertura/tampa-lisa");
            texEstudio = Resources.Load<Texture2D>("Abertura/tampa-estudio");

            // O carimbo nasce aqui, antes do Start do visual retro, para ganhar o mesmo material das outras pecas.
            var modelo = Resources.Load<GameObject>("Abertura/carimbo");
            carimbo = new GameObject("Carimbo").transform;
            if (modelo != null)
            {
                var copia = Instantiate(modelo, carimbo);
                var rs = copia.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    copia.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z); // base no chao, centrado
                }
            }
            carimbo.gameObject.SetActive(false);

            MontarCreditos();
            MontarTela();
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem)).AddComponent<InputSystemUIInputModule>();
            if (FindFirstObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            CamJ c = Cam(jaCarimbou ? "menu" : "espera");
            camPos = V(c.p); camOlhar = V(c.o); camFov = c.f;
            if (jaCarimbou) { modo = modoAnt = Modo.Menu; relogio = 8f; }
        }

        IEnumerator Start()
        {
            yield return null; // um quadro: o visual retro ja trocou os materiais
            foreach (var r in tocador.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.name.StartsWith("TampaEstudio")) matTampa = m;
                    else if (m.name.StartsWith("TampaPagina")) matPagina = m;
                }
            }
            foreach (Transform t in tocador.transform)
                if (t.name.StartsWith("CaixoteEsquerda")) caixote = t;
            MontarSeta();
            pronto = true;
        }

        CamJ Cam(string nome)
        {
            foreach (var c in d.cams) if (c.nome == nome) return c;
            return d.cams[0];
        }

        static void PoeTextura(Material m, Texture t)
        {
            if (m == null || t == null) return;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            else if (m.HasProperty("baseColorTexture")) m.SetTexture("baseColorTexture", t);
        }

        static Material Tinta(Color cor)
        {
            var m = new Material(Shader.Find("BananaRoulette/Retro"));
            m.SetColor("_BaseColor", cor); m.SetFloat("_SemLuz", 1f); m.SetFloat("_Cull", 0f);
            return m;
        }

        // Seta que marca a opcao escolhida, colada na face do caixote.
        void MontarSeta()
        {
            if (caixote == null) return;
            seta = new GameObject("Seta do menu").transform;
            seta.SetParent(caixote, false);
            var malha = new UnityEngine.Mesh();
            // ponta virada para -X (na Unity o X do three.js e espelhado: -X e a direita de quem olha a frente)
            malha.vertices = new[] { new Vector3(0.28f, 0.3f, 0f), new Vector3(-0.3f, 0f, 0f), new Vector3(0.28f, -0.3f, 0f) };
            malha.triangles = new[] { 0, 1, 2 };
            malha.RecalculateBounds();
            Camada("contorno", malha, Tinta(new Color(0.118f, 0.059f, 0.047f)), 1.35f, 0f);
            Camada("miolo", malha, Tinta(new Color(0.957f, 0.761f, 0.122f)), 1f, 0.02f);
            seta.gameObject.SetActive(false);
        }

        void Camada(string nome, UnityEngine.Mesh malha, Material m, float escala, float z)
        {
            var go = new GameObject(nome, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(seta, false);
            go.transform.localScale = Vector3.one * escala;
            go.transform.localPosition = new Vector3(0f, 0f, z);
            go.GetComponent<MeshFilter>().sharedMesh = malha;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Creditos riscados no chao: um plano deitado, que aparece e some.
        void MontarCreditos()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Creditos no chao";
            Destroy(go.GetComponent<Collider>());
            float[] c = d.creditos;
            go.transform.position = V(c[0], c[1], c[2]);
            go.transform.rotation = Quaternion.Euler(90f, 180f, 0f);
            go.transform.localScale = new Vector3(c[3], c[4], 1f);
            matCreditos = new Material(Shader.Find("Sprites/Default"));
            matCreditos.mainTexture = Resources.Load<Texture2D>("Abertura/creditos-chao");
            matCreditos.color = new Color(1f, 1f, 1f, 0f);
            creditos = go.GetComponent<Renderer>();
            creditos.sharedMaterial = matCreditos;
            creditos.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            creditos.enabled = false;
        }

        // Dica no pe da tela e o letreiro do fim da abertura.
        void MontarTela()
        {
            var go = new GameObject("Tela do menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            tela = go.GetComponent<Canvas>();
            tela.renderMode = RenderMode.ScreenSpaceOverlay;
            tela.sortingOrder = 10;
            var escala = go.GetComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;

            dica = UiFactory.CreateText(go.transform, "Dica", "", 34, TextAnchor.MiddleCenter, Color.white);
            Sombra(dica);
            var rt = dica.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 46f); rt.sizeDelta = new Vector2(0f, 60f);

            letreiro = UiFactory.CreateText(go.transform, "Letreiro", "A partida começa!", 120, TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.2f));
            letreiro.fontStyle = FontStyle.Bold;
            Sombra(letreiro);
            rt = letreiro.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 120f); rt.sizeDelta = new Vector2(0f, 200f);
            letreiro.enabled = false;
        }

        static void Sombra(Text t)
        {
            var s = t.gameObject.AddComponent<Outline>();
            s.effectColor = new Color(0.1f, 0.05f, 0.03f, 0.95f);
            s.effectDistance = new Vector2(3f, -3f);
        }

        // ------------------------------------------------------------------ atalhos de teste

        /// <summary>Em que tela o menu esta agora (Espera, Carimbo, Titulo, Menu, Opcoes, Pagina, Creditos, Cena, Fim).</summary>
        public string ModoAtual { get { return modo.ToString(); } }

        /// <summary>Pula direto para uma tela do menu, sem clicar. Serve para testar.</summary>
        public void IrParaModo(string nome, float noRelogio = -1f)
        {
            modo = (Modo)Enum.Parse(typeof(Modo), nome);
            if (noRelogio >= 0f) relogio = noRelogio;
        }

        /// <summary>Marca a opcao de numero i (comecando do zero) e, se pedido, a aciona como um clique.</summary>
        public void Escolhe(int i, bool aciona)
        {
            if (modo == Modo.Opcoes) { selO = Mathf.Clamp(i, 0, d.opcoes.Length - 1); if (aciona) { if (d.opcoes[selO].nome == "voltar") modo = Modo.Menu; else AbrePagina(d.opcoes[selO].nome); } }
            else { sel = Mathf.Clamp(i, 0, d.menu.Length - 1); if (aciona) Ativa(d.menu[sel].nome); }
        }

        // ------------------------------------------------------------------ entrada

        void Update()
        {
            if (!pronto) return;
            float dt = Time.deltaTime;
            if (ajustes == null) Entrada();
            Passo(dt);
            Desenha(dt);
        }

        void Entrada()
        {
            var tec = Keyboard.current; var rato = Mouse.current;
            bool clique = rato != null && rato.leftButton.wasPressedThisFrame;
            bool enter = tec != null && (tec.enterKey.wasPressedThisFrame || tec.numpadEnterKey.wasPressedThisFrame || tec.spaceKey.wasPressedThisFrame);
            bool esc = tec != null && tec.escapeKey.wasPressedThisFrame;
            bool qualquer = clique || (tec != null && tec.anyKey.wasPressedThisFrame);
            int passo = 0;
            if (tec != null)
            {
                if (tec.downArrowKey.wasPressedThisFrame || tec.sKey.wasPressedThisFrame || tec.rightArrowKey.wasPressedThisFrame) passo = 1;
                if (tec.upArrowKey.wasPressedThisFrame || tec.wKey.wasPressedThisFrame || tec.leftArrowKey.wasPressedThisFrame) passo = -1;
            }

            switch (modo)
            {
                case Modo.Espera:
                    if (qualquer) { relogio = 0f; modo = Modo.Carimbo; }
                    break;
                case Modo.Carimbo:
                    if (qualquer && relogio > 1.2f) { relogio = Mathf.Max(relogio, 5f); modo = Modo.Titulo; }
                    break;
                case Modo.Titulo:
                    if (qualquer) modo = Modo.Menu;
                    break;
                case Modo.Menu:
                {
                    int em = OpcaoSobOPonteiro(d.menu, true);
                    if (em >= 0 && rato != null && (rato.delta.ReadValue().sqrMagnitude > 0f || clique)) sel = em;
                    if (passo != 0) { sel = (sel + passo + d.menu.Length) % d.menu.Length; SomRetro.Tom(520f, 0.04f, "square", 0.04f); }
                    if ((clique && em >= 0) || enter) Ativa(d.menu[sel].nome);
                    break;
                }
                case Modo.Opcoes:
                {
                    int em = OpcaoSobOPonteiro(d.opcoes, false);
                    if (em >= 0 && rato != null && (rato.delta.ReadValue().sqrMagnitude > 0f || clique)) selO = em;
                    if (passo != 0) { selO = (selO + passo + d.opcoes.Length) % d.opcoes.Length; SomRetro.Tom(520f, 0.04f, "square", 0.04f); }
                    if (esc) { modo = Modo.Menu; break; }
                    if ((clique && em >= 0) || enter)
                    {
                        string nome = d.opcoes[selO].nome;
                        if (nome == "voltar") modo = Modo.Menu;
                        else AbrePagina(nome);
                    }
                    break;
                }
                case Modo.Pagina:
                    if (enter) AbreAjustes();
                    else if (qualquer) modo = origem;
                    break;
                case Modo.Creditos:
                    if (qualquer) modo = Modo.Menu;
                    break;
                case Modo.Cena:
                    if (enter || esc) ComecaPartida();
                    break;
            }
        }

        void Ativa(string nome)
        {
            if (nome == "jogar")
            {
                // a camera sai do menu direto para dentro da cena: a abertura comeca do ponto em que ela se afasta
                SomRetro.Ruido(0.5f, 0.2f, 500f, "bandpass");
                jaCarimbou = true;
                PoeTextura(matTampa, texEstudio);
                tocador.MisturarDe(cam.transform.position, cam.transform.rotation, cam.fieldOfView);
                tocador.controlaCamera = true;
                tocador.TocarDe(0.3f);
                modo = Modo.Cena;
            }
            else if (nome == "tutorial") { SomRetro.Tom(300f, 0.08f, "square", 0.05f); IrPara(AppEntry.Screen.Tutorial); }
            else if (nome == "opcoes") { SomRetro.Tom(300f, 0.08f, "square", 0.05f); modo = Modo.Opcoes; }
            else if (nome == "creditos") { SomRetro.Tom(300f, 0.08f, "square", 0.05f); modo = Modo.Creditos; }
        }

        void AbrePagina(string nome)
        {
            PoeTextura(matPagina, Resources.Load<Texture2D>("Abertura/pagina-" + nome));
            origem = Modo.Opcoes; modo = Modo.Pagina;
            SomRetro.Tom(300f, 0.08f, "square", 0.05f);
        }

        // Os ajustes de verdade (som, tela, idioma) continuam na tela que o jogo ja tinha.
        void AbreAjustes()
        {
            ajustes = SettingsScreen.Build(tela.transform, () =>
            {
                if (ajustes != null) Destroy(ajustes.gameObject);
                ajustes = null;
            });
        }

        void ComecaPartida() { jaCarimbou = true; IrPara(AppEntry.Screen.Game); }

        void IrPara(AppEntry.Screen qual)
        {
            jaCarimbou = true;
            modo = Modo.Fim; fimEm = -1f;
            AppEntry.GoTo(qual);
        }

        // Em que opcao o ponteiro esta: raio da camera ate a face pintada do caixote, e dali a posicao no desenho.
        int OpcaoSobOPonteiro(OpcJ[] lista, bool frente)
        {
            var rato = Mouse.current;
            if (rato == null || caixote == null || arcoAtivo) return -1;
            Vector2 p = rato.position.ReadValue();
            // a camera desenha numa imagem pequena: o raio sai da posicao relativa na tela, nao do pixel
            Ray raio = cam.ViewportPointToRay(new Vector3(p.x / Screen.width, p.y / Screen.height, 0f));
            Vector3 o = caixote.InverseTransformPoint(raio.origin), dir = caixote.InverseTransformDirection(raio.direction);
            float W = d.caixote[0], H = d.caixote[1], D = d.caixote[2];
            float zFace = frente ? D * 0.5f : -D * 0.5f;
            if (Mathf.Abs(dir.z) < 1e-5f) return -1;
            float t = (zFace - o.z) / dir.z;
            if (t <= 0f) return -1;
            Vector3 l = o + dir * t;
            float px = (frente ? 0.5f - l.x / W : 0.5f + l.x / W) * 1000f;
            float py = (1f - l.y / H) * 620f;
            for (int i = 0; i < lista.Length; i++)
            {
                float[] r = lista[i].r;
                if (px >= r[0] && px <= r[2] && py >= r[1] && py <= r[3]) return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------ camera e pecas

        void Passo(float dt)
        {
            float antes = relogio; relogio += dt;
            if (modo == Modo.Cena || modo == Modo.Fim)
            {
                if (modo == Modo.Fim && fimEm > 0f && Time.time >= fimEm) { fimEm = -1f; ComecaPartida(); }
                return;
            }
            if (modo == Modo.Carimbo)
            {
                if (antes < 0.45f && relogio >= 0.45f) SomRetro.Ruido(0.35f, 0.12f, 700f, "bandpass");
                if (antes < 1.0f && relogio >= 1.0f) { SomRetro.Ruido(0.2f, 0.55f, 220f, "lowpass"); SomRetro.Tom(95f, 0.25f, "sine", 0.5f, 45f); }
                if (antes < 1.6f && relogio >= 1.6f) SomRetro.Grito(420f, 640f, 0.12f, 0.5f);
                if (antes < 1.8f && relogio >= 1.8f) SomRetro.Grito(460f, 700f, 0.12f, 0.5f);
                if (antes < 2.0f && relogio >= 2.0f) SomRetro.Grito(520f, 840f, 0.18f, 0.55f);
                if (antes < 2.3f && relogio >= 2.3f) SomRetro.Grito(400f, 560f, 0.24f, 0.45f);
                if (relogio > 4.9f) modo = Modo.Titulo; // a tampa carimbada fica na tela antes de a camera descer
            }

            // do menu para as opcoes (e de volta) a camera da meia-volta no caixote num giro so
            if (modo != modoAnt)
            {
                arcoAtivo = (modo == Modo.Opcoes && modoAnt == Modo.Menu) || (modo == Modo.Menu && modoAnt == Modo.Opcoes);
                arcoIda = modo == Modo.Opcoes; arcoK = 0f;
                modoAnt = modo;
            }
            if (arcoAtivo)
            {
                arcoK = Mathf.Min(1f, arcoK + dt / 0.85f);
                float e = Suave(arcoK), kk = arcoIda ? e : 1f - e;
                float cx = d.esquerda[0], cz = d.esquerda[2], giro = d.esquerda[3];
                float ang = giro - Mathf.PI * kk, R = Mathf.Lerp(19f, 13.3f, kk) + Mathf.Sin(Mathf.PI * kk) * 1.5f;
                camPos = V(cx + Mathf.Sin(ang) * R, 3.9f + Mathf.Sin(Mathf.PI * kk) * 2.2f, cz + Mathf.Cos(ang) * R);
                camOlhar = V(cx - Mathf.Sin(giro) * 3.6f * kk, Mathf.Lerp(3.4f, 3.15f, kk), cz - Mathf.Cos(giro) * 3.6f * kk);
                camFov = Mathf.Lerp(32f, 46f, kk);
                if (arcoK >= 1f) arcoAtivo = false;
                return;
            }

            CamJ c = Cam(NomeDaCam());
            float a = 1f - Mathf.Exp(-dt * (modo == Modo.Titulo && relogio < 7.5f ? 2.4f : 4.2f));
            camPos = Vector3.Lerp(camPos, V(c.p), a);
            camOlhar = Vector3.Lerp(camOlhar, V(c.o), a);
            camFov = Mathf.Lerp(camFov, c.f, a);
        }

        string NomeDaCam()
        {
            switch (modo)
            {
                case Modo.Espera: return "espera";
                case Modo.Carimbo: return "carimbo";
                case Modo.Titulo: return "titulo";
                case Modo.Opcoes: return "opcoes";
                case Modo.Pagina: return "pagina";
                case Modo.Creditos: return "creditos";
                default: return "menu";
            }
        }

        void Desenha(float dt)
        {
            float r = relogio;
            bool noMenu = modo != Modo.Cena && modo != Modo.Fim;

            if (noMenu)
            {
                float bate = modo == Modo.Carimbo && r >= 1f ? Mathf.Max(0f, 1f - (r - 1f) / 0.3f) : 0f;
                Vector3 p = camPos;
                p.x += Mathf.Sin(r * 0.5f) * 0.35f + Mathf.Sin(r * 70f) * 0.12f * bate;
                p.y += Mathf.Sin(r * 0.37f) * 0.12f + Mathf.Cos(r * 83f) * 0.12f * bate;
                cam.transform.position = p;
                cam.transform.LookAt(camOlhar);
                cam.fieldOfView = camFov;
            }

            // carimbo: vem de tras, paira, bate na tampa, segura, sobe e sai. A tinta aparece na batida
            bool mostra = modo == Modo.Carimbo && r < 2.4f;
            if (carimbo.gameObject.activeSelf != mostra) carimbo.gameObject.SetActive(mostra);
            if (mostra)
            {
                float ch = Suave(Seg(r, 0.3f, 0.8f)), de = Mathf.Pow(Seg(r, 0.82f, 1f), 2f), so = Suave(Seg(r, 1.38f, 1.8f)), sa = Suave(Seg(r, 1.8f, 2.35f));
                float alto = (1f - de) * (1f - so) + so, k = ch * (1f - sa);
                carimbo.position = V(0f, d.tampaY + Mathf.Lerp(15f, 3.6f * (r < 1f ? 1f - de : so), k), Mathf.Lerp(-14f, 0f, k));
                carimbo.rotation = Quaternion.Euler((0.5f * (1f - k) + 0.1f * alto * k) * Mathf.Rad2Deg, 0f, -0.06f * Mathf.Sin(r * 5f) * alto * Mathf.Rad2Deg);
                carimbo.localScale = new Vector3(1f, r >= 1f && r < 1.38f ? 1f - 0.07f * Mathf.Max(0f, 1f - (r - 1f) / 0.12f) : 1f, 1f);
            }
            if (noMenu) PoeTextura(matTampa, modo != Modo.Espera && (modo != Modo.Carimbo || r >= 1f) ? texEstudio : texLisa);

            // seta ao lado da opcao escolhida
            bool comSeta = seta != null && (modo == Modo.Menu || modo == Modo.Opcoes);
            if (seta != null && seta.gameObject.activeSelf != comSeta) seta.gameObject.SetActive(comSeta);
            if (comSeta)
            {
                float W = d.caixote[0], H = d.caixote[1], D = d.caixote[2];
                bool frente = modo == Modo.Menu;
                float[] q = frente ? d.menu[sel].r : d.opcoes[selO].r;
                float x3 = ((q[0] - 14f) / 1000f - 0.5f) * W + Mathf.Sin(r * 7f) * 0.08f; // posicao no eixo do three.js
                float y = (1f - (q[1] + q[3]) * 0.5f / 620f) * H;
                seta.localPosition = frente ? new Vector3(-x3, y, D * 0.5f + 0.2f) : new Vector3(x3, y, -D * 0.5f - 0.2f);
                seta.localScale = frente ? Vector3.one : new Vector3(-1f, 1f, -1f);
            }

            // creditos no chao
            Color cc = matCreditos.color;
            cc.a = Mathf.Lerp(cc.a, modo == Modo.Creditos ? 1f : 0f, 1f - Mathf.Exp(-dt * 7f));
            matCreditos.color = cc;
            creditos.enabled = cc.a > 0.02f;

            dica.text = Dica();
            Color cd = dica.color; cd.a = ajustes != null ? 0f : 0.55f + 0.45f * Mathf.Sin(r * 4f); dica.color = cd;
            letreiro.enabled = (modo == Modo.Cena || modo == Modo.Fim) && (tocador.MostrarTitulo || modo == Modo.Fim);
        }

        string Dica()
        {
            switch (modo)
            {
                case Modo.Espera: return "Clique para começar";
                case Modo.Titulo: return "Clique ou aperte qualquer tecla";
                case Modo.Menu: return "Clique numa opção · ou setas e Enter";
                case Modo.Opcoes: return "Clique numa opção · Esc volta";
                case Modo.Pagina: return "Enter abre os ajustes · clique para voltar";
                case Modo.Creditos: return "Clique para voltar";
                case Modo.Cena: return tocador.Tempo < 6f ? "Enter pula a abertura" : "";
                default: return "";
            }
        }
    }
}
