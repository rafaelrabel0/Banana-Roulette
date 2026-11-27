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

        enum Modo { Espera, Carimbo, Titulo, Menu, Opcoes, Pagina, Personagem, Creditos, Cena, Fim }

        [Serializable] class FrenteJ { public OpcJ[] normal; public OpcJ[] primeira; }

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

        /// <summary>Ligado por quem sai de uma partida: o menu nasce na tela do titulo ("aperte qualquer tecla"), com a camera ja na logo.</summary>
        public static bool EntrarNoTitulo;

        DadosJ d;
        Modo modo = Modo.Espera, modoAnt = Modo.Espera, origem = Modo.Menu;
        float relogio, camFov = 32f, fimEm = -1f;
        Vector3 camPos, camOlhar;
        int sel, selO;
        bool arcoAtivo, arcoIda; float arcoK;

        Transform caixote, carimbo, seta;
        Material matTampa, matPagina, matCreditos;
        Texture texLisa, texEstudio;
        // faces pintadas que mudam com o idioma: [0] portugues (a que veio no .glb), [1] ingles (Resources)
        Material matFrente, matOpcoes, matLateral;
        readonly Texture[] texFrente = new Texture[2], texOpcoes = new Texture[2], texLateral = new Texture[2], texCreditos = new Texture[2], texTampa = new Texture[2];
        Renderer creditos;
        Text dica, letreiro;
        Canvas tela;
        PaginaOpcoes pagina;
        bool pronto;

        // O mundo (a raiz do modelo da abertura) esta encolhido e girado para a roda cair no centro da partida.
        // V = ponto dado no eixo da previa (three.js, X espelhado); W = ponto dado no eixo da Unity antes de encolher.
        Transform mundo;
        float e = 1f; // quanto o mundo encolheu
        Vector3 V(float x, float y, float z) { return mundo.TransformPoint(new Vector3(-x, y, z)); }
        Vector3 V(float[] a) { return mundo.TransformPoint(new Vector3(-a[0], a[1], a[2])); }
        Vector3 W(Vector3 p) { return mundo.TransformPoint(p); }
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
            // a abertura acabou: a partida comeca ali mesmo, sem esperar
            tocador.aoTerminar.AddListener(() => { if (modo == Modo.Cena) { modo = Modo.Fim; fimEm = Time.time + 0.05f; } });

            // ---- cena unica: o mundo vai para a escala e a posicao da partida, uma vez so ----
            config = Resources.Load<TuningConfig>("TuningConfig");
            raio = config != null ? config.circleRadius : 5f;
            mundo = tocador.transform;
            Mundo.Aplicar(mundo, raio);
            e = mundo.lossyScale.x;
            tocador.nevoaInicio *= e; tocador.nevoaFim *= e;
            foreach (Light luz in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (luz.type == LightType.Directional) luz.transform.rotation = mundo.rotation * luz.transform.rotation;
            Mundo.Clareira(mundo, Mundo.CameraDoJogo(raio), raio);
            AppEntry.Roteador = Roteia;

            texLisa = Resources.Load<Texture2D>("Abertura/tampa-lisa");
            texTampa[0] = Resources.Load<Texture2D>("Abertura/tampa-estudio");
            texTampa[1] = Resources.Load<Texture2D>("Abertura/tampa-estudio-en");
            // Frente do caixote: quem nunca jogou ve o TUTORIAL logo abaixo do JOGAR; depois ele desce para o lado do PERSONAGEM.
            CarregaFrente();
            texOpcoes[1] = Resources.Load<Texture2D>("Abertura/menu-opcoes-en");
            texLateral[1] = Resources.Load<Texture2D>("Abertura/menu-lateral-en");
            texCreditos[0] = Resources.Load<Texture2D>("Abertura/creditos-chao");
            texCreditos[1] = Resources.Load<Texture2D>("Abertura/creditos-chao-en");
            texEstudio = texTampa[0];

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

            CamJ c = Cam(EntrarNoTitulo ? "titulo" : jaCarimbou ? "menu" : "espera");
            camPos = V(c.p); camOlhar = V(c.o); camFov = c.f;
            if (EntrarNoTitulo) { modo = modoAnt = Modo.Titulo; relogio = 8f; jaCarimbou = true; EntrarNoTitulo = false; }
            else if (jaCarimbou) { modo = modoAnt = Modo.Menu; relogio = 8f; }
            MusicaRetro.Tocar(MusicaRetro.Clima.Menu);
            MusicaRetro.Abafar(false);
            MusicaRetro.Volume(1f, 2.5f); // quem chega de uma partida ouve a musica abrir aos poucos
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
                    else if (m.name.StartsWith("MenuFrente"))
                    {
                        matFrente = m; if (texFrente[0] == null) texFrente[0] = LeTextura(m);
                        var mf = r.GetComponent<MeshFilter>();
                        float meia = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.extents.z : 0.17f;
                        // texto de pe na frente do caixote: quem olha de frente ve a direita do texto no -X da peca
                        if (paginaFrente == null) paginaFrente = PaginaOpcoes.Criar(r.transform, new Vector3(0f, 0f, meia + 0.03f), Quaternion.LookRotation(Vector3.back, Vector3.up), 1000f, 620f);
                    }
                    else if (m.name.StartsWith("MenuOpcoes")) { matOpcoes = m; texOpcoes[0] = LeTextura(m); }
                    else if (m.name.StartsWith("MenuLateral")) { matLateral = m; texLateral[0] = LeTextura(m); }
                    else if (m.name.StartsWith("TampaPagina"))
                    {
                        matPagina = m;
                        var mf = r.GetComponent<MeshFilter>();
                        float meia = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.extents.y : 0.17f;
                        if (pagina == null) pagina = PaginaOpcoes.Criar(r.transform, meia);
                    }
                }
            }
            foreach (Transform t in tocador.transform)
                if (t.name.StartsWith("CaixoteEsquerda")) caixote = t;
            MontarSeta();
            // a tampa de tras comeca lisa: o tutorial pintado nela nao vale mais (o botao abre o tutorial de verdade)
            PoeTextura(matPagina, texLisa);
            Loc.OnLanguageChanged += AplicaIdioma;
            AplicaIdioma();
            pronto = true;
        }

        void OnDestroy()
        {
            Loc.OnLanguageChanged -= AplicaIdioma;
            if (AppEntry.Roteador == Roteia) AppEntry.Roteador = null;
        }

        static string T(string pt, string en) { return Loc.Current == Language.Portuguese ? pt : en; }

        static Texture LeTextura(Material m)
        {
            if (m.HasProperty("_BaseMap")) return m.GetTexture("_BaseMap");
            return m.HasProperty("baseColorTexture") ? m.GetTexture("baseColorTexture") : null;
        }

        // Frente do caixote nas duas arrumacoes: "primeira" (nunca jogou) e "normal". Chamado de novo quando o teste troca.
        void CarregaFrente()
        {
            string arrumacao = GameSettings.Played ? "normal" : "primeira";
            var frente = Resources.Load<TextAsset>("Abertura/menu-frente");
            if (frente != null)
            {
                var f = JsonUtility.FromJson<FrenteJ>(frente.text);
                d.menu = GameSettings.Played ? f.normal : f.primeira;
            }
            texFrente[0] = Resources.Load<Texture2D>("Abertura/menu-frente-" + arrumacao + "-pt");
            texFrente[1] = Resources.Load<Texture2D>("Abertura/menu-frente-" + arrumacao + "-en");
            if (d.menu != null && sel >= d.menu.Length) sel = 0;
        }

        // Troca a tinta dos caixotes, da tampa e dos creditos pela do idioma escolhido em Opcoes > Idioma.
        void AplicaIdioma()
        {
            int i = Loc.Current == Language.Portuguese ? 0 : 1;
            PoeTextura(matFrente, texFrente[i] != null ? texFrente[i] : texFrente[0]);
            PoeTextura(matOpcoes, texOpcoes[i] != null ? texOpcoes[i] : texOpcoes[0]);
            PoeTextura(matLateral, texLateral[i] != null ? texLateral[i] : texLateral[0]);
            texEstudio = texTampa[i] != null ? texTampa[i] : texTampa[0];
            if (matCreditos != null) matCreditos.mainTexture = texCreditos[i] != null ? texCreditos[i] : texCreditos[0];
            if (letreiro != null) letreiro.text = T("A partida começa!", "The match begins!");
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
            go.transform.rotation = mundo.rotation * Quaternion.Euler(90f, 180f, 0f);
            go.transform.localScale = new Vector3(c[3], c[4], 1f) * e;
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
            rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0f, 58f); rt.sizeDelta = new Vector2(0f, 120f);
            dica.alignment = TextAnchor.LowerCenter; dica.supportRichText = true; // cabe a linha do teste por cima da dica

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

        /// <summary>A pagina de opcoes aberta na tampa (nula antes de o menu ficar pronto). Serve para testar.</summary>
        public PaginaOpcoes Pagina { get { return modo == Modo.Personagem ? paginaFrente : pagina; } }

        // ------------------------------------------------------------------ entrada

        void Update()
        {
            if (!pronto || emPartida) return;
            float dt = Time.deltaTime;
            Entrada();
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
                    // F3 = botao de teste: troca entre "primeira vez" (historia inteira) e "ja jogou" (entrada curta)
                    if (tec != null && tec.f3Key.wasPressedThisFrame)
                    {
                        GameSettings.SetPlayed(!GameSettings.Played);
                        CarregaFrente(); AplicaIdioma();
                        SomRetro.Tom(GameSettings.Played ? 520f : 780f, 0.1f, "square", 0.06f);
                        break;
                    }
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
                    if (pagina == null) { if (qualquer) modo = origem; }
                    else if (pagina.Entrada(cam)) { pagina.Fechar(); modo = origem; if (!perguntando) { CarregaFrente(); AplicaIdioma(); } }
                    break;
                case Modo.Personagem:
                    if (paginaFrente == null || paginaFrente.Entrada(cam)) FechaPersonagem();
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
                // Primeira vez: antes de jogar, pergunta se quer o tutorial (na tampa do caixote de tras).
                if (!GameSettings.Played && pagina != null)
                {
                    SomRetro.Tom(300f, 0.08f, "square", 0.05f);
                    PoeTextura(matPagina, texLisa);
                    pagina.AoEscolher = resposta =>
                    {
                        GameSettings.SetPlayed(true);
                        pagina.Fechar();
                        if (resposta == "tutorial") IrPara(AppEntry.Screen.Tutorial);
                        else ComecaAbertura();
                    };
                    pagina.Abrir("primeira");
                    perguntando = true;
                    origem = Modo.Menu; modo = Modo.Pagina;
                }
                else if (GameSettings.Played)
                {
                    // Ja jogou: nada de historia. A camera so se afasta e o macaco do fundo joga a bomba para a roda.
                    SomRetro.Ruido(0.5f, 0.2f, 500f, "bandpass");
                    GameManager.EntradaCurta = true;
                    IrPara(AppEntry.Screen.Game);
                }
                else ComecaAbertura();
            }
            else if (nome == "personagem") AbrePersonagem();
            else if (nome == "tutorial") { SomRetro.Tom(300f, 0.08f, "square", 0.05f); GameSettings.SetPlayed(true); IrPara(AppEntry.Screen.Tutorial); }
            else if (nome == "opcoes") { SomRetro.Tom(300f, 0.08f, "square", 0.05f); modo = Modo.Opcoes; }
            else if (nome == "creditos") { SomRetro.Tom(300f, 0.08f, "square", 0.05f); modo = Modo.Creditos; }
        }

        // A camera sai de onde esta direto para dentro da cena: a abertura comeca do ponto em que ela se afasta.
        void ComecaAbertura()
        {
            SomRetro.Ruido(0.5f, 0.2f, 500f, "bandpass");
            jaCarimbou = true;
            PoeTextura(matTampa, texEstudio);
            // o macaco que recebe a bomba no fim da abertura e o jogador: ja aparece com a cor de pelo escolhida
            foreach (Transform t in mundo)
            {
                if (!t.name.StartsWith("MacacoRoda1")) continue;
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                        if (m != null && m.name.StartsWith("Pelo") && !m.name.StartsWith("PeloClaro") && m.HasProperty("_BaseColor"))
                            m.SetColor("_BaseColor", Aparencia.Pelo(GameSettings.Fur).cor);
            }
            tocador.MisturarDe(cam.transform.position, cam.transform.rotation, cam.fieldOfView);
            tocador.controlaCamera = true;
            tocador.TocarDe(0.3f);
            modo = Modo.Cena;
        }

        // ------------------------------------------------------------------ personagem

        // Onde o macaco de mostra fica (ao lado do caixote de tras) e de onde a camera olha os dois.
        static readonly Vector3 LugarDoMacaco = new Vector3(-1.2f, 0f, 6.6f);
        const float EscalaDaMostra = 0.55f;
        PaginaOpcoes paginaFrente; // as escolhas ficam escritas na frente do caixote do menu, de pe, ao lado do macaco
        static readonly Vector3 CamPersonagemP = new Vector3(5.5f, 5.4f, 25f), CamPersonagemO = new Vector3(6.2f, 4.4f, 1f);
        const float CamPersonagemF = 34f;

        MacacoVisual mostra;
        bool perguntando; // a pagina aberta e o aviso de primeira vez, nao uma pagina de opcoes
        GameObject bombaMostra;

        void AbrePersonagem()
        {
            if (paginaFrente == null) return;
            SomRetro.Tom(300f, 0.08f, "square", 0.05f);
            PoeTextura(matFrente, texLisa); // a frente do caixote fica lisa enquanto as escolhas estao escritas nela
            if (mostra == null)
            {
                var lugar = new GameObject("Macaco de mostra");
                lugar.transform.position = W(LugarDoMacaco);
                Vector3 paraACamera = W(CamPersonagemP) - W(LugarDoMacaco); paraACamera.y = 0f;
                lugar.transform.rotation = Quaternion.LookRotation(paraACamera);
                mostra = MacacoVisual.Criar(lugar.transform, Vector3.zero, EscalaDaMostra * e, Aparencia.Pelo(GameSettings.Fur).cor);
                bombaMostra = new GameObject("Bomba de mostra");
                pavioDaMostra = BananaCarrier.Montar3D(bombaMostra, EscalaDaMostra * e * 1.4f, GameSettings.Bomb); // 1,4: o tamanho para o qual as maos foram posadas
            }
            if (mostra != null) { mostra.transform.parent.gameObject.SetActive(true); mostra.Laco("segura1"); }
            if (bombaMostra != null) bombaMostra.SetActive(true);
            faseMostra = 0; queimaMostra = 0f; tMostra = 0f;
            if (pavioDaMostra != null && mostra != null) { pavioDaMostra.SeguirPonto(mostra.PontoMaco); pavioDaMostra.UpdateFuseVisual(0f); }
            VesteMostra();
            paginaFrente.AoMudar = VesteMostra;
            paginaFrente.Abrir("personagem");
            modo = Modo.Personagem;
        }

        // O macaco de mostra fica calmo, para dar para ver o rosto e os acessorios: so faz o malabarismo leve com a
        // bomba. O nervoso aparece no suor, que aumenta com o pavio. Quando falta pouco ele joga a bomba para o alto,
        // ela estoura la em cima, ele relaxa e outra cai na mao. Nenhum grito aqui: so pavio, vento e o estouro.
        BananaCarrier pavioDaMostra;
        int faseMostra;            // 0 segurando, 1 bomba subindo, 2 esperando a nova, 3 nova caindo
        float queimaMostra, tMostra, proxEstalo;
        Vector3 jogouDe;

        void CicloDaMostra(float dt)
        {
            if (mostra == null || pavioDaMostra == null || mostra.PontoMaco == null) return;
            tMostra += dt;

            if (faseMostra == 0)
            {
                queimaMostra += dt / 11f;
                pavioDaMostra.UpdateFuseVisual(queimaMostra);
                mostra.Laco("segura1");
                mostra.Suor = queimaMostra;

                if (Time.time >= proxEstalo)
                {
                    proxEstalo = Time.time + Mathf.Lerp(0.5f, 0.12f, queimaMostra);
                    SomRetro.Ruido(0.05f, 0.03f, 6000f, "highpass");
                }

                if (queimaMostra >= 0.9f)
                {
                    faseMostra = 1; tMostra = 0f;
                    mostra.Tocar("jogaCima");
                    SomRetro.Ruido(0.4f, 0.12f, 800f, "bandpass", 0.25f);
                }
            }
            else if (faseMostra == 1)
            {
                // a bomba fica na mao ate o braco subir (0,3 s) e dali segue reto para o alto, perdendo forca
                const float solta = 0.3f, sobe = 0.75f;
                if (tMostra < solta) { jogouDe = mostra.PontoMaco.position; return; }
                pavioDaMostra.SeguirPonto(null);
                mostra.Suor = 0f;
                float k = Mathf.Clamp01((tMostra - solta) / sobe);
                Vector3 p = jogouDe + Vector3.up * (6.5f * e * (1f - (1f - k) * (1f - k)));
                bombaMostra.transform.position = p;
                bombaMostra.transform.Rotate(Vector3.forward, 500f * dt, Space.Self);
                if (k >= 1f)
                {
                    Explosao.Criar(p, 0.45f);
                    SomRetro.Ruido(0.6f, 0.4f, 180f, "lowpass");
                    SomRetro.Tom(90f, 0.45f, "sine", 0.35f, 35f);
                    bombaMostra.SetActive(false);
                    mostra.Laco("parado");
                    faseMostra = 2; tMostra = 0f;
                }
            }
            else if (faseMostra == 2)
            {
                if (tMostra > 1.6f)
                {
                    // outra bomba vem do alto
                    faseMostra = 3; tMostra = 0f; queimaMostra = 0f;
                    bombaMostra.SetActive(true);
                    pavioDaMostra.UpdateFuseVisual(0f);
                    mostra.Tocar("recebe");
                    SomRetro.Tom(1500f, 0.45f, "sine", 0.05f, 350f);
                }
            }
            else
            {
                float k = Mathf.Clamp01(tMostra / 0.45f);
                bombaMostra.transform.position = mostra.PontoMaco.position + Vector3.up * (16f * e * (1f - k * k));
                bombaMostra.transform.Rotate(Vector3.forward, 500f * dt, Space.Self);
                if (k >= 1f)
                {
                    SomRetro.Tom(220f, 0.1f, "sine", 0.12f, 140f); // o peso caindo na mao
                    pavioDaMostra.SeguirPonto(mostra.PontoMaco);
                    mostra.Laco("segura1");
                    faseMostra = 0; tMostra = 0f;
                }
            }
        }

        void VesteMostra()
        {
            Aparencia.VestirJogador(mostra);
            Aparencia.PintaBomba(bombaMostra, GameSettings.Bomb);
        }

        void FechaPersonagem()
        {
            paginaFrente.AoMudar = null;
            paginaFrente.Fechar();
            AplicaIdioma(); // devolve a pintura do menu a frente do caixote
            if (mostra != null) { mostra.Suor = 0f; mostra.transform.parent.gameObject.SetActive(false); }
            if (bombaMostra != null) bombaMostra.SetActive(false);
            modo = Modo.Menu;
        }

        void AbrePagina(string nome)
        {
            // a tampa fica lisa e a pagina e escrita por cima, ligada aos ajustes de verdade
            perguntando = false;
            if (pagina != null) { PoeTextura(matPagina, texLisa); pagina.Abrir(nome); }
            else PoeTextura(matPagina, Resources.Load<Texture2D>("Abertura/pagina-" + nome));
            origem = Modo.Opcoes; modo = Modo.Pagina;
            SomRetro.Tom(300f, 0.08f, "square", 0.05f);
        }

        void ComecaPartida() { jaCarimbou = true; IrPara(AppEntry.Screen.Game); }

        // ------------------------------------------------------------------ cena unica: a partida nasce e some aqui dentro

        TuningConfig config;
        float raio = 5f;
        bool emPartida;
        readonly System.Collections.Generic.HashSet<GameObject> antesDaPartida = new System.Collections.Generic.HashSet<GameObject>();

        // AppEntry.GoTo passa por aqui: nada de carregar cena. Espera um quadro, porque quem pediu a troca
        // costuma ser um objeto da propria partida, que vai ser destruido.
        bool Roteia(AppEntry.Screen qual)
        {
            StartCoroutine(Troca(qual));
            return true;
        }

        IEnumerator Troca(AppEntry.Screen qual)
        {
            yield return null;
            if (emPartida) EncerraPartida();
            if (qual == AppEntry.Screen.Menu) VaiParaOTitulo();
            else IniciaPartida(qual);
        }

        void MostraAtores(bool sim)
        {
            foreach (Transform t in mundo)
                if (Mundo.EhAtor(t.name) && t.gameObject.activeSelf != sim) t.gameObject.SetActive(sim);
        }

        // Monta a partida (ou o tutorial) por cima da cena, do jeito que ela esta. A camera segue de onde estiver.
        void IniciaPartida(AppEntry.Screen qual)
        {
            jaCarimbou = true;
            modo = modoAnt = Modo.Fim; fimEm = -1f; arcoAtivo = false;
            if (pagina != null) pagina.Fechar();
            if (paginaFrente != null) paginaFrente.Fechar();
            if (mostra != null) mostra.transform.parent.gameObject.SetActive(false);
            if (bombaMostra != null) bombaMostra.SetActive(false);
            if (carimbo != null) carimbo.gameObject.SetActive(false);
            if (seta != null) seta.gameObject.SetActive(false);
            creditos.enabled = false;
            dica.text = "";
            StartCoroutine(ApagaLetreiro());

            // a abertura para no quadro em que esta e os personagens dela saem de cena
            tocador.Pausar();
            tocador.controlaCamera = false;
            tocador.enabled = false;
            MostraAtores(false);

            antesDaPartida.Clear();
            foreach (GameObject go in gameObject.scene.GetRootGameObjects()) antesDaPartida.Add(go);

            AppEntry.Next = qual;
            var partida = new GameObject("GameBootstrap");
            var montador = partida.AddComponent<GameBootstrap>();
            montador.config = config;
            montador.mundoPronto = mundo;
            montador.cameraPronta = cam;
            emPartida = true;
        }

        IEnumerator ApagaLetreiro()
        {
            yield return new WaitForSeconds(1.1f);
            if (letreiro != null) letreiro.enabled = false;
        }

        // Desmonta a partida: some tudo o que nasceu depois dela e a cena volta a ser a do menu.
        void EncerraPartida()
        {
            foreach (GameObject go in gameObject.scene.GetRootGameObjects())
                if (!antesDaPartida.Contains(go)) Destroy(go);
            foreach (Transform filho in cam.transform)
                if (filho.name.StartsWith("Pavio do topo")) Destroy(filho.gameObject);
            Time.timeScale = 1f;
            MonkeySlot.Tensao = 0f;
            MusicaRetro.Perigo = 0f;
            emPartida = false;

            tocador.enabled = true;
            tocador.IrPara(0f);
            tocador.Pausar();
            MostraAtores(true);
            AplicaIdioma(); // devolve a pintura do menu a frente do caixote
            PoeTextura(matPagina, texLisa);
            if (letreiro != null) letreiro.enabled = false;
        }

        // Volta para a tela "aperte qualquer tecla". A camera segue de onde estiver ate a logo.
        void VaiParaOTitulo()
        {
            modo = modoAnt = Modo.Titulo; relogio = 8f; fimEm = -1f;
            camPos = cam.transform.position;
            camOlhar = cam.transform.position + cam.transform.forward * (17.5f * e);
            camFov = cam.fieldOfView;
            MusicaRetro.Tocar(MusicaRetro.Clima.Menu);
            MusicaRetro.Abafar(false);
            MusicaRetro.Volume(1f, 2.5f);
        }

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
            AmbienteSelva.Mudo = modo != Modo.Cena && modo != Modo.Fim; // no menu so toca a musica
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

            float a = 1f - Mathf.Exp(-dt * (modo == Modo.Titulo && relogio < 7.5f ? 2.4f : 4.2f));
            if (modo == Modo.Personagem)
            {
                camPos = Vector3.Lerp(camPos, W(CamPersonagemP), a);
                camOlhar = Vector3.Lerp(camOlhar, W(CamPersonagemO), a);
                camFov = Mathf.Lerp(camFov, CamPersonagemF, a);
                return;
            }
            CamJ c = Cam(NomeDaCam());
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

            // A musica acompanha a emocao de cada trecho da abertura.
            if (modo == Modo.Cena)
            {
                float tc = tocador.Tempo;
                MusicaRetro.Tocar(tc < 7.25f ? MusicaRetro.Clima.Calmo     // chega com fome e preguica
                                : tc < 15.1f ? MusicaRetro.Clima.Duvida    // olha o caixote sem entender
                                : tc < 18.4f ? MusicaRetro.Clima.Alegria   // "sao bananas!"
                                : tc < 27.0f ? MusicaRetro.Clima.Vilao     // o aviador
                                : MusicaRetro.Clima.Tensao);               // a bituca, o pavio aceso, a roda
            }

            if (noMenu)
            {
                float bate = modo == Modo.Carimbo && r >= 1f ? Mathf.Max(0f, 1f - (r - 1f) / 0.3f) : 0f;
                Vector3 p = camPos;
                p += cam.transform.right * ((Mathf.Sin(r * 0.5f) * 0.35f + Mathf.Sin(r * 70f) * 0.12f * bate) * e);
                p.y += (Mathf.Sin(r * 0.37f) * 0.12f + Mathf.Cos(r * 83f) * 0.12f * bate) * e;
                cam.transform.position = p;
                cam.transform.LookAt(camOlhar);
                cam.fieldOfView = camFov;
            }

            // carimbo: vem de tras, paira, bate na tampa, segura, sobe e sai. A tinta aparece na batida
            bool comCarimbo = modo == Modo.Carimbo && r < 2.4f;
            if (carimbo.gameObject.activeSelf != comCarimbo) carimbo.gameObject.SetActive(comCarimbo);
            if (comCarimbo)
            {
                float ch = Suave(Seg(r, 0.3f, 0.8f)), de = Mathf.Pow(Seg(r, 0.82f, 1f), 2f), so = Suave(Seg(r, 1.38f, 1.8f)), sa = Suave(Seg(r, 1.8f, 2.35f));
                float alto = (1f - de) * (1f - so) + so, k = ch * (1f - sa);
                carimbo.position = V(0f, d.tampaY + Mathf.Lerp(15f, 3.6f * (r < 1f ? 1f - de : so), k), Mathf.Lerp(-14f, 0f, k));
                carimbo.rotation = mundo.rotation * Quaternion.Euler((0.5f * (1f - k) + 0.1f * alto * k) * Mathf.Rad2Deg, 0f, -0.06f * Mathf.Sin(r * 5f) * alto * Mathf.Rad2Deg);
                carimbo.localScale = new Vector3(1f, r >= 1f && r < 1.38f ? 1f - 0.07f * Mathf.Max(0f, 1f - (r - 1f) / 0.12f) : 1f, 1f) * e;
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

            // a bomba de mostra acompanha a mao do macaco de mostra
            if (modo == Modo.Personagem && bombaMostra != null) CicloDaMostra(dt);

            // creditos no chao
            Color cc = matCreditos.color;
            cc.a = Mathf.Lerp(cc.a, modo == Modo.Creditos ? 1f : 0f, 1f - Mathf.Exp(-dt * 7f));
            matCreditos.color = cc;
            creditos.enabled = cc.a > 0.02f;

            dica.text = Dica();
            Color cd = dica.color; cd.a = 0.55f + 0.45f * Mathf.Sin(r * 4f); dica.color = cd;
            letreiro.enabled = (modo == Modo.Cena || modo == Modo.Fim) && (tocador.MostrarTitulo || modo == Modo.Fim);
        }

        string Dica()
        {
            switch (modo)
            {
                case Modo.Espera: return T("Clique para começar", "Click to start");
                case Modo.Titulo: return T("Clique ou aperte qualquer tecla", "Click or press any key");
                case Modo.Menu:
                    return "<size=24>" + (GameSettings.Played ? T("F3 (teste): JÁ JOGOU — sem história", "F3 (test): PLAYED — no story") : T("F3 (teste): PRIMEIRA VEZ — história inteira", "F3 (test): FIRST TIME — full story")) + "</size>\n" +
                        T("Clique numa opção · ou setas e Enter", "Click an option · or arrows and Enter");
                case Modo.Opcoes: return T("Clique numa opção · Esc volta", "Click an option · Esc goes back");
                case Modo.Pagina:
                    if (perguntando) return T("Clique numa resposta · Esc volta", "Click an answer · Esc goes back");
                    goto case Modo.Personagem;
                case Modo.Personagem: return T("Clique à esquerda ou à direita da linha para mudar · Esc volta", "Click left or right of the line to change · Esc goes back");
                case Modo.Creditos: return T("Clique para voltar", "Click to go back");
                case Modo.Cena: return tocador.Tempo < 6f ? T("Enter pula a abertura", "Enter skips the intro") : "";
                default: return "";
            }
        }
    }
}
