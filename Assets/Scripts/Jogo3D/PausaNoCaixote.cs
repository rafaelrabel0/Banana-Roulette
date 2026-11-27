using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// O menu da partida. Nao e uma tela por cima do jogo: a camera sai da roda e vai ate o caixote
    /// de dinamite ao lado, onde as opcoes estao escritas (voltar, som, teclas, sair).
    ///
    /// O jogo e online, entao A PARTIDA NAO PARA enquanto o menu esta aberto. Se a bomba chegar
    /// na mao do jogador nesse meio tempo, aparece o alerta "A BOMBA ESTA NA SUA MAO!"; clicar nele
    /// (ou apertar Espaco) devolve a camera a roda na hora.
    ///
    /// Abre com Esc ou com o botao MENU. Enquanto esta aberto, as teclas nao contam como jogada.
    /// </summary>
    public class PausaNoCaixote : MonoBehaviour
    {
        /// <summary>Verdadeiro enquanto o menu esta aberto. Quem le as teclas da jogada consulta isto.</summary>
        public static bool Aberta { get; private set; }

        Camera cam;
        Transform face;
        PaginaOpcoes pagina;
        HudController hud;
        Func<bool> bombaComJogador;
        Action sair;

        Vector3 casaPos; Quaternion casaGiro; float casaCampo;
        float k;            // 0 = camera na roda, 1 = camera no caixote
        float pressa = 1f;  // o alerta volta mais rapido que o Esc
        GameObject alerta;
        Text textoAlerta, textoSeta;
        Text rotuloBotao;

        static string T(string pt, string en) { return Loc.Current == Language.Portuguese ? pt : en; }

        /// <summary>
        /// face = a tabua da frente do caixote do menu (onde as opcoes sao escritas); meiaEspessura = metade da
        /// grossura dela. bombaComJogador diz se a bomba esta com o jogador agora. sair = o que fazer em "sair da partida".
        /// </summary>
        public static PausaNoCaixote Criar(Camera camera, Transform face, float meiaEspessura, Transform canvas,
            HudController hud, Func<bool> bombaComJogador, Action sair)
        {
            var go = new GameObject("Menu da partida (caixote)");
            var p = go.AddComponent<PausaNoCaixote>();
            p.cam = camera; p.face = face; p.hud = hud; p.bombaComJogador = bombaComJogador; p.sair = sair;
            p.casaPos = camera.transform.position; p.casaGiro = camera.transform.rotation; p.casaCampo = camera.fieldOfView;
            p.pagina = PaginaOpcoes.Criar(face, new Vector3(0f, 0f, meiaEspessura + 0.03f), Quaternion.LookRotation(Vector3.back, Vector3.up), 1000f, 620f);
            p.pagina.AoEscolher = acao =>
            {
                if (acao == "nova") { Aberta = false; AppEntry.GoTo(AppEntry.Screen.Game); }
                else if (acao == "sair") p.StartCoroutine(p.SaiAteALogo());
            };
            p.MontarTela(canvas);
            Aberta = false;
            return p;
        }

        void MontarTela(Transform canvas)
        {
            // botao MENU, pequeno, no canto de cima a esquerda: embaixo ficam os rostos, as vidas e os poderes
            var botao = UiFactory.CreateButton(canvas, "BotaoMenu", "MENU", new Vector2(130f, 44f), 22,
                new Color32(0x24, 0x18, 0x0A, 0xD8), Palette.BananaYellow, out rotuloBotao);
            UiFactory.Anchor(botao, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -112f), new Vector2(130f, 44f));
            botao.onClick.AddListener(Alternar);

            // alerta: um botao grande no meio da tela
            alerta = new GameObject("AlertaBomba", typeof(RectTransform), typeof(Image), typeof(Button));
            alerta.transform.SetParent(canvas, false);
            var rt = (RectTransform)alerta.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 250f); rt.sizeDelta = new Vector2(1180f, 190f);
            alerta.GetComponent<Image>().color = new Color(0.75f, 0.12f, 0.08f, 0.94f);
            alerta.GetComponent<Button>().onClick.AddListener(VoltarJa);

            textoAlerta = UiFactory.CreateText(alerta.transform, "Texto", "", 64, TextAnchor.MiddleCenter, Color.white);
            textoAlerta.fontStyle = FontStyle.Bold;
            UiFactory.Anchor(textoAlerta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(1140f, 90f));
            textoSeta = UiFactory.CreateText(alerta.transform, "Seta", "", 34, TextAnchor.MiddleCenter, Palette.BananaYellow);
            UiFactory.Anchor(textoSeta, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1140f, 50f));
            alerta.SetActive(false);
        }

        void OnDestroy()
        {
            Aberta = false;
            MusicaRetro.Abafar(false);
            if (pagina != null) Destroy(pagina.gameObject); // ela mora na frente do caixote, fora deste objeto
        }

        public void Alternar() { if (Aberta) Fechar(1f); else Abrir(); }

        public void Abrir()
        {
            if (Aberta) return;
            Aberta = true; pressa = 1f;
            pagina.Abrir("pausa");
            if (hud != null) { hud.SetKeysVisible(false); hud.SuspendEnd(true); if (hud.TopFuse != null) hud.TopFuse.Mostrar3D(false); }
            MusicaRetro.Abafar(true);
            SomRetro.Tom(300f, 0.08f, "square", 0.05f);
        }

        void Fechar(float comPressa)
        {
            if (!Aberta) return;
            Aberta = false; pressa = comPressa;
            pagina.Fechar();
            if (hud != null) { hud.SetKeysVisible(true); hud.SuspendEnd(false); if (hud.TopFuse != null) hud.TopFuse.Mostrar3D(true); }
            MusicaRetro.Abafar(false);
        }

        bool saindo;

        // Sair da partida sem corte: a camera anda ate a frente do caixote principal (a logo do jogo) e so
        // entao a cena do menu e carregada, ja na tela "aperte qualquer tecla", com a camera no mesmo lugar.
        System.Collections.IEnumerator SaiAteALogo()
        {
            saindo = true;
            pagina.Fechar();
            alerta.SetActive(false);
            if (hud != null) { if (hud.TopFuse != null) hud.TopFuse.Mostrar3D(false); hud.SetVisible(false); }
            MusicaRetro.Tocar(MusicaRetro.Clima.Menu);
            MusicaRetro.Abafar(true); // continua abafada durante o caminho; a tela do titulo abre o som aos poucos

            Transform mundo = face.root;
            Vector3 dePos = cam.transform.position; Quaternion deGiro = cam.transform.rotation; float deCampo = cam.fieldOfView;
            Vector3 atePos = mundo.TransformPoint(new Vector3(0f, 3.7f, 17.5f));
            Quaternion ateGiro = Quaternion.LookRotation(mundo.TransformPoint(new Vector3(0f, 3.2f, 0f)) - atePos);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 1.3f;
                float s = Mathf.Clamp01(t); s = s * s * (3f - 2f * s);
                cam.transform.SetPositionAndRotation(Vector3.Lerp(dePos, atePos, s), Quaternion.Slerp(deGiro, ateGiro, s));
                cam.fieldOfView = Mathf.Lerp(deCampo, 32f, s);
                yield return null;
            }
            Aberta = false;
            MenuAbertura.EntrarNoTitulo = true;
            sair();
        }

        /// <summary>Do alerta: fecha o menu e devolve a camera a roda bem depressa.</summary>
        public void VoltarJa() { SomRetro.Tom(660f, 0.06f, "square", 0.06f); Fechar(3.5f); }

        void Update()
        {
            if (saindo) return;
            var tec = Keyboard.current;
            bool esc = tec != null && tec.escapeKey.wasPressedThisFrame;

            if (!Aberta) { if (esc) Abrir(); alerta.SetActive(false); return; }

            bool perigo = bombaComJogador != null && bombaComJogador();
            if (alerta.activeSelf != perigo) { alerta.SetActive(perigo); if (perigo) SomRetro.Grito(620f, 980f, 0.16f, 0.5f); }
            if (perigo)
            {
                textoAlerta.text = T("A BOMBA ESTÁ NA SUA MÃO!", "THE BOMB IS IN YOUR HANDS!");
                textoSeta.text = T(">>>  clique aqui ou aperte Espaço para voltar  <<<", ">>>  click here or press Space to go back  <<<");
                float pisca = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 14f);
                alerta.transform.localScale = Vector3.one * (0.96f + 0.06f * pisca);
                if (tec != null && (tec.spaceKey.wasPressedThisFrame || tec.enterKey.wasPressedThisFrame)) { VoltarJa(); return; }
            }

            if (esc || pagina.Entrada(cam)) Fechar(1f);
        }

        // Depois do tremor de camera (que roda antes), para o menu mandar na camera enquanto esta aberto.
        void LateUpdate()
        {
            if (saindo) return;
            float alvo = Aberta ? 1f : 0f;
            if (Mathf.Approximately(k, alvo)) { if (rotuloBotao != null) rotuloBotao.text = Aberta ? T("VOLTAR", "BACK") : "MENU"; return; }
            k = Mathf.MoveTowards(k, alvo, Time.deltaTime * 2.2f * pressa);

            float s = k * k * (3f - 2f * k);
            Vector3 lugar = face.TransformPoint(new Vector3(-0.6f, 0.9f, 19.5f));
            Quaternion giro = Quaternion.LookRotation(face.TransformPoint(new Vector3(0f, 0.2f, 0f)) - lugar);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(casaPos, lugar, s), Quaternion.Slerp(casaGiro, giro, s));
            cam.fieldOfView = Mathf.Lerp(casaCampo, 32f, s);
        }
    }
}
