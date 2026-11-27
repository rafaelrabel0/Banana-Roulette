using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BananaRoulette
{
    /// <summary>
    /// Pagina de opcoes escrita na tampa do caixote de tras, ligada aos ajustes de verdade (GameSettings).
    ///
    /// E um plano deitado sobre a tampa, escrito com a mesma letra de estencil dos caixotes
    /// (LetraEstencil). Cada linha mostra "ROTULO   < valor >":
    ///   - clique na metade esquerda da linha diminui, na direita aumenta;
    ///   - setas para cima/baixo escolhem a linha, esquerda/direita mudam o valor;
    ///   - VOLTAR (ou Esc) fecha.
    ///
    /// Paginas: som, musica, tela, idioma, jogabilidade, controles.
    /// </summary>
    public class PaginaOpcoes : MonoBehaviour
    {
        const float Escala = 0.01f;
        // tamanho da face, em centesimos de unidade. A tampa tem 10,3 x 7,5; a frente do caixote, 10 x 6,2
        static float Largura = 1030f;
        float larg = 1030f, alt = 750f;
        float K { get { return alt / 750f; } } // tudo foi medido para a tampa; noutras faces encolhe junto
        static readonly Color Tinta = new Color(0.13f, 0.07f, 0.05f, 1f);
        static readonly Color TintaFraca = new Color(0.13f, 0.07f, 0.05f, 0.72f);
        static readonly Color Marcada = new Color(0.70f, 0.13f, 0.10f, 1f);

        class Linha
        {
            public Func<string> rotulo;
            public Func<string> valor;   // nulo = linha sem valor (aviso ou VOLTAR)
            public Action<int> muda;     // recebe -1 ou +1
            public string acao;          // linha que so dispara AoEscolher(acao)
            public bool voltar;
            public bool Escolhivel { get { return muda != null || voltar || acao != null; } }
            public float y, meiaAltura;
        }

        readonly List<Linha> linhas = new List<Linha>();
        // Uma linha de texto em 3D: so refaz a malha quando o texto, o tamanho ou a cor mudam.
        class Risco
        {
            public Transform t; public UnityEngine.Mesh malha; public MeshRenderer r;
            string texto; float tamanho; Color cor;
            public void Poe(string novo, float tam, Color c)
            {
                if (novo == texto && tam == tamanho && c == cor) return;
                texto = novo; tamanho = tam; cor = c;
                LetraEstencil.Escreve(malha, novo, tam, c, Largura - 90f);
            }
        }

        readonly List<Risco> textos = new List<Risco>();
        Func<string> titulo, rotuloVoltar;
        Risco textoTitulo;
        int sel;

        public bool Aberta { get; private set; }

        /// <summary>Chamado depois de qualquer valor mudar (a tela de personagem usa para vestir o macaco na hora).</summary>
        public Action AoMudar;
        /// <summary>Chamado quando o jogador escolhe uma linha de acao (aviso de primeira vez: "tutorial" ou "jogar").</summary>
        public Action<string> AoEscolher;

        static string T(string pt, string en) { return Loc.Current == Language.Portuguese ? pt : en; }
        static string Liga(bool v) { return Loc.Get(v ? "set.on" : "set.off"); }
        static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }

        /// <summary>Cria a pagina deitada sobre a tampa. meiaEspessura = metade da grossura da tampa.</summary>
        public static PaginaOpcoes Criar(Transform tampa, float meiaEspessura)
        {
            // letras de pe para quem olha da camera da pagina: "cima" do texto aponta para o fundo do caixote
            return Criar(tampa, new Vector3(0f, meiaEspessura + 0.03f, 0f), Quaternion.LookRotation(Vector3.down, Vector3.back), 1030f, 750f);
        }

        /// <summary>Cria a pagina em qualquer face. largura e altura em centesimos de unidade (10 de largura = 1000).</summary>
        public static PaginaOpcoes Criar(Transform pai, Vector3 posLocal, Quaternion giroLocal, float largura, float altura)
        {
            var go = new GameObject("Pagina de opcoes");
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posLocal;
            go.transform.localRotation = giroLocal;
            go.transform.localScale = Vector3.one * Escala;
            var p = go.AddComponent<PaginaOpcoes>();
            p.larg = largura; p.alt = altura;
            p.textoTitulo = p.NovoTexto("Titulo");
            p.textoTitulo.t.localPosition = new Vector3(0f, 285f * p.K, 0f);
            for (int i = 0; i < 7; i++) p.textos.Add(p.NovoTexto("Linha " + i));
            go.SetActive(false);
            return p;
        }

        Risco NovoTexto(string nome)
        {
            var go = new GameObject(nome, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            var r = new Risco { t = go.transform, malha = new UnityEngine.Mesh(), r = go.GetComponent<MeshRenderer>() };
            go.GetComponent<MeshFilter>().sharedMesh = r.malha;
            r.r.sharedMaterial = LetraEstencil.Material;
            r.r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.r.receiveShadows = false;
            return r;
        }

        // ------------------------------------------------------------------ conteudo

        public void Abrir(string nome)
        {
            linhas.Clear();
            rotuloVoltar = () => Loc.Get("set.back");
            switch (nome)
            {
                case "som":
                    titulo = () => T("SOM", "SOUND");
                    Volume(() => Loc.Get("set.sfx"), () => GameSettings.SfxVolume, GameSettings.SetSfx);
                    break;
                case "musica":
                    titulo = () => T("MÚSICA", "MUSIC");
                    Volume(() => Loc.Get("set.music"), () => GameSettings.MusicVolume, GameSettings.SetMusic);
                    Aviso(() => T("Ainda não há música no jogo.", "There is no music in the game yet."));
                    break;
                case "tela":
                    titulo = () => T("TELA", "SCREEN");
                    Add(() => Loc.Get("set.fullscreen"), () => Liga(GameSettings.Fullscreen), dir => GameSettings.SetFullscreen(!GameSettings.Fullscreen));
                    Add(() => Loc.Get("set.quality"),
                        () => Loc.Get(GameSettings.Quality == 0 ? "set.quality.low" : GameSettings.Quality == 1 ? "set.quality.mid" : "set.quality.high"),
                        dir => GameSettings.SetQuality((GameSettings.Quality + dir + 3) % 3));
                    Add(() => "Pixel", () => Liga(GameSettings.Pixel), dir => GameSettings.SetPixel(!GameSettings.Pixel));
                    Add(() => T("Tremor", "Wobble"), () => Liga(GameSettings.Wobble), dir => GameSettings.SetWobble(!GameSettings.Wobble));
                    break;
                case "idioma":
                    titulo = () => Loc.Get("set.language");
                    Add(() => T("Idioma", "Language"), () => Loc.Get(Loc.Current == Language.Portuguese ? "set.pt" : "set.en"),
                        dir => GameSettings.SetLanguage(Loc.Current == Language.Portuguese ? Language.English : Language.Portuguese));
                    break;
                case "personagem":
                    titulo = () => T("PERSONAGEM", "CHARACTER");
                    Add(() => T("Pelo", "Fur"), () => Aparencia.Pelo(GameSettings.Fur).Nome, dir => GameSettings.SetFur(Roda(GameSettings.Fur + dir, Aparencia.Pelos.Length)));
                    Add(() => T("Cabeça", "Head"), () => Aparencia.Chapeu(GameSettings.Hat).Nome, dir => GameSettings.SetHat(Roda(GameSettings.Hat + dir, Aparencia.Chapeus.Length)));
                    Add(() => T("Óculos", "Glasses"), () => Aparencia.Oculo(GameSettings.Glasses).Nome, dir => GameSettings.SetGlasses(Roda(GameSettings.Glasses + dir, Aparencia.Oculos.Length)));
                    Add(() => T("Brinco", "Earring"), () => Aparencia.Brinco(GameSettings.Earring).Nome, dir => GameSettings.SetEarring(Roda(GameSettings.Earring + dir, Aparencia.Brincos.Length)));
                    Add(() => T("Bomba", "Bomb"), () => Aparencia.Bomba(GameSettings.Bomb).Nome, dir => GameSettings.SetBomb(Roda(GameSettings.Bomb + dir, Aparencia.Bombas.Length)));
                    break;
                case "pausa":
                    // menu da partida, escrito no caixote ao lado da roda. A partida continua correndo.
                    titulo = () => "MENU";
                    Volume(() => Loc.Get("set.sfx"), () => GameSettings.SfxVolume, GameSettings.SetSfx);
                    Add(() => T("Teclas", "Keys"), () => GameSettings.UseLetters ? T("LETRAS (W A S D)", "LETTERS (W A S D)") : T("SETAS", "ARROWS"),
                        dir => GameSettings.SetUseLetters(!GameSettings.UseLetters));
                    linhas.Add(new Linha { rotulo = () => T("Nova partida", "New match"), acao = "nova" });
                    linhas.Add(new Linha { rotulo = () => T("Sair da partida", "Leave the match"), acao = "sair" });
                    rotuloVoltar = () => T("Voltar ao jogo", "Back to the game");
                    break;
                case "primeira":
                    titulo = () => T("PRIMEIRA VEZ?", "FIRST TIME?");
                    Aviso(() => T("Esta é a sua primeira vez jogando Banana Roulette.", "This is your first time playing Banana Roulette."));
                    Aviso(() => T("Quer fazer o tutorial?", "Would you like to do the tutorial?"));
                    linhas.Add(new Linha { rotulo = () => T("Sim, quero o tutorial", "Yes, show me the tutorial"), acao = "tutorial" });
                    linhas.Add(new Linha { rotulo = () => T("Não, quero jogar", "No, let me play"), acao = "jogar" });
                    break;
                case "jogabilidade":
                    titulo = () => T("JOGABILIDADE", "GAMEPLAY");
                    Add(() => T("Teclas", "Keys"), () => GameSettings.UseLetters ? T("LETRAS (W A S D)", "LETTERS (W A S D)") : T("SETAS", "ARROWS"),
                        dir => GameSettings.SetUseLetters(!GameSettings.UseLetters));
                    Aviso(() => T("Vale para digitar a sequência e escolher o lado.", "Used to type the sequence and pick a side."));
                    break;
                default: // controles
                    titulo = () => Loc.Get("set.controls");
                    Aviso(() => Loc.Get("set.controls.seq") + ": " + (GameSettings.UseLetters ? "W A S D" : T("as quatro setas", "the four arrows")));
                    Aviso(() => Loc.Get("set.controls.pass") + ": " + (GameSettings.UseLetters ? "A / D" : T("esquerda / direita", "left / right")));
                    Aviso(() => T("Esc: voltar ao menu", "Esc: back to menu"));
                    break;
            }
            linhas.Add(new Linha { rotulo = () => rotuloVoltar(), voltar = true });

            // espalha as linhas pela tampa, abaixo do titulo
            float topo = 160f * K, pe = -285f * K;
            float passo = linhas.Count > 1 ? Mathf.Min(125f * K, (topo - pe) / (linhas.Count - 1)) : 0f;
            for (int i = 0; i < linhas.Count; i++) { linhas[i].y = topo - passo * i; linhas[i].meiaAltura = Mathf.Max(38f * K, passo * 0.5f); }
            linhas[linhas.Count - 1].y = pe;

            sel = 0;
            while (sel < linhas.Count - 1 && !linhas[sel].Escolhivel) sel++;
            Aberta = true;
            gameObject.SetActive(true);
            Desenhar();
        }

        public void Fechar() { Aberta = false; gameObject.SetActive(false); }

        static int Roda(int i, int n) { return ((i % n) + n) % n; }

        void Add(Func<string> rotulo, Func<string> valor, Action<int> muda) { linhas.Add(new Linha { rotulo = rotulo, valor = valor, muda = muda }); }
        void Aviso(Func<string> texto) { linhas.Add(new Linha { rotulo = texto }); }
        void Volume(Func<string> rotulo, Func<float> le, Action<float> grava)
        {
            Add(rotulo, () => Pct(le()), dir => grava(Mathf.Round((le() + 0.1f * dir) * 10f) / 10f));
        }

        void Desenhar()
        {
            Largura = larg;
            textoTitulo.Poe(titulo().ToUpperInvariant(), 80f * K, Tinta);
            for (int i = 0; i < textos.Count; i++)
            {
                Risco t = textos[i];
                bool tem = i < linhas.Count;
                t.r.enabled = tem;
                if (!tem) continue;
                Linha l = linhas[i];
                t.t.localPosition = new Vector3(0f, l.y, 0f);
                bool marcada = i == sel && l.Escolhivel;
                Color cor = marcada ? Marcada : l.Escolhivel ? Tinta : TintaFraca;
                if (l.voltar) t.Poe(l.rotulo().ToUpperInvariant(), 62f * K, cor);
                else if (l.acao != null) t.Poe(l.rotulo().ToUpperInvariant(), 52f * K, cor);
                else if (l.valor == null) t.Poe(l.rotulo(), 40f * K, cor);
                else t.Poe(l.rotulo().ToUpperInvariant() + "   <  " + l.valor().ToUpperInvariant() + "  >", 56f * K, cor);
            }
        }

        // ------------------------------------------------------------------ entrada

        /// <summary>Le teclado e rato. Devolve verdadeiro quando o jogador pediu para voltar.</summary>
        public bool Entrada(Camera cam)
        {
            if (!Aberta) return false;
            var tec = Keyboard.current; var rato = Mouse.current;
            bool voltar = false;

            if (tec != null)
            {
                if (tec.escapeKey.wasPressedThisFrame) voltar = true;
                if (tec.downArrowKey.wasPressedThisFrame || tec.sKey.wasPressedThisFrame) Anda(1);
                if (tec.upArrowKey.wasPressedThisFrame || tec.wKey.wasPressedThisFrame) Anda(-1);
                if (tec.leftArrowKey.wasPressedThisFrame || tec.aKey.wasPressedThisFrame) voltar |= Aciona(sel, -1);
                if (tec.rightArrowKey.wasPressedThisFrame || tec.dKey.wasPressedThisFrame) voltar |= Aciona(sel, 1);
                if (tec.enterKey.wasPressedThisFrame || tec.numpadEnterKey.wasPressedThisFrame || tec.spaceKey.wasPressedThisFrame) voltar |= Aciona(sel, 1);
            }

            if (rato != null && cam != null)
            {
                // a camera desenha numa imagem pequena: o raio sai da posicao relativa na tela, nao do pixel
                Vector2 p = rato.position.ReadValue();
                Ray raio = cam.ViewportPointToRay(new Vector3(p.x / Screen.width, p.y / Screen.height, 0f));
                var plano = new Plane(transform.forward, transform.position);
                float dist;
                if (plano.Raycast(raio, out dist))
                {
                    Vector3 l = transform.InverseTransformPoint(raio.GetPoint(dist));
                    int em = -1;
                    if (Mathf.Abs(l.x) < larg * 0.5f)
                        for (int i = 0; i < linhas.Count; i++)
                            if (linhas[i].Escolhivel && Mathf.Abs(l.y - linhas[i].y) <= linhas[i].meiaAltura) em = i;
                    bool clique = rato.leftButton.wasPressedThisFrame;
                    if (em >= 0 && (clique || rato.delta.ReadValue().sqrMagnitude > 0f)) sel = em;
                    if (em >= 0 && clique) voltar |= Aciona(em, l.x < 0f ? -1 : 1);
                }
            }

            Desenhar();
            return voltar;
        }

        void Anda(int passo)
        {
            for (int n = 0; n < linhas.Count; n++)
            {
                sel = (sel + passo + linhas.Count) % linhas.Count;
                if (linhas[sel].Escolhivel) break;
            }
            SomRetro.Tom(520f, 0.04f, "square", 0.04f);
        }

        /// <summary>Muda o valor da linha (dir = -1 ou +1). Devolve verdadeiro se a linha era o VOLTAR. Tambem serve para testar.</summary>
        public bool Aciona(int linha, int dir)
        {
            if (linha < 0 || linha >= linhas.Count) return false;
            Linha l = linhas[linha];
            if (l.voltar) return true;
            if (l.acao != null) { SomRetro.Tom(300f, 0.08f, "square", 0.05f); if (AoEscolher != null) AoEscolher(l.acao); return false; }
            if (l.muda == null) return false;
            sel = linha;
            l.muda(dir);
            if (AoMudar != null) AoMudar();
            SomRetro.Tom(dir > 0 ? 660f : 440f, 0.06f, "square", 0.06f);
            Desenhar();
            return false;
        }
    }
}
