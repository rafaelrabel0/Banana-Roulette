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
        const float Largura = 1030f, Altura = 750f, Escala = 0.01f; // a tampa tem 10,3 x 7,5
        static readonly Color Tinta = new Color(0.13f, 0.07f, 0.05f, 1f);
        static readonly Color TintaFraca = new Color(0.13f, 0.07f, 0.05f, 0.72f);
        static readonly Color Marcada = new Color(0.70f, 0.13f, 0.10f, 1f);

        class Linha
        {
            public Func<string> rotulo;
            public Func<string> valor;   // nulo = linha sem valor (aviso ou VOLTAR)
            public Action<int> muda;     // recebe -1 ou +1
            public bool voltar;
            public bool Escolhivel { get { return muda != null || voltar; } }
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
        Func<string> titulo;
        Risco textoTitulo;
        int sel;

        public bool Aberta { get; private set; }

        static string T(string pt, string en) { return Loc.Current == Language.Portuguese ? pt : en; }
        static string Liga(bool v) { return Loc.Get(v ? "set.on" : "set.off"); }
        static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }

        /// <summary>Cria a pagina deitada sobre a tampa. meiaEspessura = metade da grossura da tampa.</summary>
        public static PaginaOpcoes Criar(Transform tampa, float meiaEspessura)
        {
            var go = new GameObject("Pagina de opcoes");
            go.transform.SetParent(tampa, false);
            go.transform.localPosition = new Vector3(0f, meiaEspessura + 0.03f, 0f);
            // letras de pe para quem olha da camera da pagina: "cima" do texto aponta para o fundo do caixote
            go.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.back);
            go.transform.localScale = Vector3.one * Escala;
            var p = go.AddComponent<PaginaOpcoes>();
            p.textoTitulo = p.NovoTexto("Titulo");
            p.textoTitulo.t.localPosition = new Vector3(0f, 285f, 0f);
            for (int i = 0; i < 6; i++) p.textos.Add(p.NovoTexto("Linha " + i));
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
            switch (nome)
            {
                case "som":
                    titulo = () => Loc.Get("set.audio");
                    Volume(() => Loc.Get("set.sfx"), () => GameSettings.SfxVolume, GameSettings.SetSfx);
                    break;
                case "musica":
                    titulo = () => T("MÚSICA", "MUSIC");
                    Volume(() => Loc.Get("set.music"), () => GameSettings.MusicVolume, GameSettings.SetMusic);
                    Aviso(() => T("Ainda não há música no jogo.", "There is no music in the game yet."));
                    break;
                case "tela":
                    titulo = () => Loc.Get("set.video");
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
            linhas.Add(new Linha { rotulo = () => Loc.Get("set.back"), voltar = true });

            // espalha as linhas pela tampa, abaixo do titulo
            float topo = 160f, pe = -285f;
            float passo = linhas.Count > 1 ? Mathf.Min(125f, (topo - pe) / (linhas.Count - 1)) : 0f;
            for (int i = 0; i < linhas.Count; i++) { linhas[i].y = topo - passo * i; linhas[i].meiaAltura = Mathf.Max(38f, passo * 0.5f); }
            linhas[linhas.Count - 1].y = pe;

            sel = 0;
            while (sel < linhas.Count - 1 && !linhas[sel].Escolhivel) sel++;
            Aberta = true;
            gameObject.SetActive(true);
            Desenhar();
        }

        public void Fechar() { Aberta = false; gameObject.SetActive(false); }

        void Add(Func<string> rotulo, Func<string> valor, Action<int> muda) { linhas.Add(new Linha { rotulo = rotulo, valor = valor, muda = muda }); }
        void Aviso(Func<string> texto) { linhas.Add(new Linha { rotulo = texto }); }
        void Volume(Func<string> rotulo, Func<float> le, Action<float> grava)
        {
            Add(rotulo, () => Pct(le()), dir => grava(Mathf.Round((le() + 0.1f * dir) * 10f) / 10f));
        }

        void Desenhar()
        {
            textoTitulo.Poe(titulo().ToUpperInvariant(), 80f, Tinta);
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
                if (l.voltar) t.Poe(l.rotulo().ToUpperInvariant(), 62f, cor);
                else if (l.valor == null) t.Poe(l.rotulo(), 40f, cor);
                else t.Poe(l.rotulo().ToUpperInvariant() + "   <  " + l.valor().ToUpperInvariant() + "  >", 56f, cor);
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
                    if (Mathf.Abs(l.x) < Largura * 0.5f)
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
            if (l.muda == null) return false;
            sel = linha;
            l.muda(dir);
            SomRetro.Tom(dir > 0 ? 660f : 440f, 0.06f, "square", 0.06f);
            Desenhar();
            return false;
        }
    }
}
