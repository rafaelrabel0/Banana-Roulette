using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// O macaco em 3D da partida: o modelo Resources/Jogo/macaco-jogo.glb e as animacoes de jogo.
    ///
    /// O .glb traz um clipe so, de 9 s, com todas as animacoes em fila. O arquivo macaco-jogo.json
    /// diz onde cada uma comeca e termina (parado, olhando, segura1..3, erro, jogaE, jogaD, recebe,
    /// explode, vence). Este componente toca o trecho certo.
    ///
    ///   Laco(nome)  - animacao que fica repetindo (parado, olhando, segura1, segura2, segura3, vence)
    ///   Tocar(nome) - animacao de uma vez so; no fim volta para o laco (ou fica parada no ultimo quadro)
    ///
    /// O no "PontoMaco" do modelo e onde a bomba fica enquanto esta com este macaco.
    /// </summary>
    public class MacacoVisual : MonoBehaviour
    {
        [Serializable] class ClipeJ { public string nome; public float ini, fim; public int repete; }
        [Serializable] class DadosJ { public float fps = 24f, duracao = 9f, raio = 15f; public ClipeJ[] clipes; public float[] roda; public float[] pisa; }

        static GameObject modelo;
        static AnimationClip clipe;
        static DadosJ dados;

        /// <summary>Raio da roda na previa. O raio do jogo dividido por este da a escala do modelo.</summary>
        public static float RaioDaPrevia { get { Carrega(); return dados != null ? dados.raio : 15f; } }
        /// <summary>Centro da roda na previa, ja no eixo da Unity (X espelhado).</summary>
        /// <summary>Os cinco lugares da roda da abertura, no eixo da Unity (X espelhado) e na medida da previa. O lugar 1 e o do jogador.</summary>
        public static Vector3[] Lugares { get { Carrega(); return lugares; } }
        static Vector3[] lugares;

        public static Vector3 CentroDaRoda { get { Carrega(); return dados != null && dados.roda != null ? new Vector3(-dados.roda[0], dados.roda[1], dados.roda[2]) : Vector3.zero; } }

        /// <summary>Onde o pe bate na animacao "pisa", na medida da previa (x para o lado, z para a frente).</summary>
        public static Vector2 Pisa { get { Carrega(); return dados != null && dados.pisa != null && dados.pisa.Length >= 2 ? new Vector2(dados.pisa[0], dados.pisa[1]) : new Vector2(3.3f, 3.7f); } }

        Animation anim;
        AnimationState estado;
        // Troca suave: a pose em que o macaco estava fica congelada num segundo estado e se desfaz em fracoes de
        // segundo por cima da animacao nova. Sem isso cada troca de animacao era um corte seco (o "travado").
        AnimationState estadoDe;
        float tAmostra, tDe, mistura, durMistura = 0.14f;

        // Orelhas: com fone ou cartola elas dobram para tras, por baixo do acessorio.
        readonly List<Transform> orelhas = new List<Transform>();
        Quaternion[] orelhaRepouso; Vector3[] orelhaEixo;
        float dobra;

        // Suor: gotas que saltam da cabeca. 0 = nenhuma, 1 = muitas.
        public float Suor;
        Transform cabeca;
        Transform[] gotas; float[] idadeGota; Vector3[] velGota; float proxGota;
        readonly Dictionary<string, ClipeJ> porNome = new Dictionary<string, ClipeJ>();
        ClipeJ laco, umaVez;
        float tLaco, tUmaVez;
        bool seguraNoFim;
        int voltasUmaVez = 1;
        float pulo;
        Vector3 posBase;

        readonly List<Material> pelos = new List<Material>();
        readonly List<Material> todos = new List<Material>();
        readonly List<Color> coresBase = new List<Color>();
        float tosta, pisca;
        Color corPisca;

        public Transform PontoMaco { get; private set; }

        readonly List<Transform> acessorios = new List<Transform>();

        /// <summary>
        /// Liga os acessorios escolhidos e desliga os outros. Cada um e o fim do nome do grupo no modelo
        /// (ex.: "palha" liga Ac_chapeu_palha). Vazio = nenhum naquele lugar.
        /// </summary>
        public void Vestir(string chapeu, string oculos, string brinco)
        {
            foreach (Transform t in acessorios)
            {
                bool liga = (chapeu != "" && t.name.StartsWith("Ac_chapeu_" + chapeu)) ||
                            (oculos != "" && t.name.StartsWith("Ac_oculos_" + oculos)) ||
                            (brinco != "" && t.name.StartsWith("Ac_brinco_" + brinco));
                if (t.gameObject.activeSelf != liga) t.gameObject.SetActive(liga);
            }
            dobra = chapeu == "fone" ? 1.15f : chapeu == "cartola" ? 0.5f : 0f;
        }

        static void Carrega()
        {
            if (modelo != null) return;
            modelo = Resources.Load<GameObject>("Jogo/macaco-jogo");
            var clipes = Resources.LoadAll<AnimationClip>("Jogo/macaco-jogo");
            clipe = clipes.Length > 0 ? clipes[0] : null;
            var texto = Resources.Load<TextAsset>("Jogo/macaco-jogo");
            if (texto != null)
            {
                dados = JsonUtility.FromJson<DadosJ>(texto.text);
                // "lugares":[[x,z],[x,z],...] - lista de listas, que o JsonUtility nao le; e curto, entao vai na mao
                int ini = texto.text.IndexOf("\"lugares\":[[");
                if (ini >= 0)
                {
                    ini += 12;
                    int fim = texto.text.IndexOf("]]", ini);
                    string[] pares = texto.text.Substring(ini, fim - ini).Split(new[] { "],[" }, System.StringSplitOptions.None);
                    lugares = new Vector3[pares.Length];
                    var ponto = System.Globalization.CultureInfo.InvariantCulture;
                    for (int i = 0; i < pares.Length; i++)
                    {
                        string[] n = pares[i].Trim('[', ']').Split(',');
                        lugares[i] = new Vector3(-float.Parse(n[0], ponto), 0f, float.Parse(n[1], ponto));
                    }
                }
            }
        }

        /// <summary>Cria um macaco como filho de <paramref name="pai"/>, sentado no chao, olhando para a frente do pai.</summary>
        public static MacacoVisual Criar(Transform pai, Vector3 posLocal, float escala, Color pelo)
        {
            Carrega();
            if (modelo == null || clipe == null || dados == null) return null;

            GameObject go = Instantiate(modelo, pai);
            go.name = "Macaco 3D";
            go.transform.localPosition = posLocal;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * escala;
            VisualRetro.Converter(go);

            var v = go.AddComponent<MacacoVisual>();
            v.posBase = posLocal;
            v.Montar(pelo);
            return v;
        }

        void Montar(Color pelo)
        {
            foreach (var c in dados.clipes) porNome[c.nome] = c;

            anim = GetComponent<Animation>();
            if (anim == null) anim = gameObject.AddComponent<Animation>();
            anim.playAutomatically = false;
            if (clipe.legacy)
            {
                if (anim.GetClip(clipe.name) == null) anim.AddClip(clipe, clipe.name);
                estado = anim[clipe.name];
                if (anim.GetClip("de") == null) anim.AddClip(clipe, "de");
                estadoDe = anim["de"];
            }

            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("PontoMaco")) PontoMaco = t;
                else if (t.name.StartsWith("Ac_")) acessorios.Add(t);
                else if (t.name.StartsWith("Orelha")) orelhas.Add(t);
                else if (t.name.StartsWith("Cabeca")) cabeca = t;
            }
            MedeOrelhas();
            Vestir("", "", ""); // nasce sem nada na cabeca

            // Cada macaco ganha copias dos materiais, para ter a propria cor de pelo e poder tostar sozinho.
            var copias = new Dictionary<Material, Material>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null) continue;
                    Material c;
                    if (!copias.TryGetValue(ms[i], out c))
                    {
                        c = new Material(ms[i]) { name = ms[i].name };
                        copias[ms[i]] = c;
                        todos.Add(c);
                        if (c.name.StartsWith("Pelo") && !c.name.StartsWith("PeloClaro")) pelos.Add(c);
                    }
                    ms[i] = c;
                }
                r.sharedMaterials = ms;
            }
            DefinirPelo(pelo);
            Laco("parado");
            tLaco = UnityEngine.Random.value * 2f; // para a roda nao respirar toda junta
            Amostrar();
        }

        void Pose(float t) { estado.enabled = true; estado.weight = 1f; estado.time = t; anim.Sample(); estado.enabled = false; }

        // Descobre para que lado a orelha gira comparando duas poses do proprio modelo (parado = reta, olhando =
        // um pouco para tras). Assim a dobra nao depende de como o importador converte os eixos.
        void MedeOrelhas()
        {
            ClipeJ p, o;
            if (estado == null || orelhas.Count == 0 || !porNome.TryGetValue("parado", out p) || !porNome.TryGetValue("olhando", out o)) { orelhas.Clear(); return; }
            orelhaRepouso = new Quaternion[orelhas.Count]; orelhaEixo = new Vector3[orelhas.Count];
            Pose(p.ini + 0.05f);
            for (int i = 0; i < orelhas.Count; i++) orelhaRepouso[i] = orelhas[i].localRotation;
            Pose(o.ini + 0.05f);
            for (int i = 0; i < orelhas.Count; i++)
            {
                float ang; Vector3 eixo;
                (orelhas[i].localRotation * Quaternion.Inverse(orelhaRepouso[i])).ToAngleAxis(out ang, out eixo);
                if (ang > 180f) { ang = 360f - ang; eixo = -eixo; }
                orelhaEixo[i] = ang > 1f ? eixo : Vector3.zero;
            }
        }

        void Mistura(float dur) { tDe = tAmostra; mistura = 1f; durMistura = Mathf.Max(0.01f, dur); }

        void PassoDoSuor(float dt)
        {
            if (cabeca == null) return;
            if (gotas == null)
            {
                if (Suor <= 0.02f) return;
                gotas = new Transform[6]; idadeGota = new float[6]; velGota = new Vector3[6];
                var agua = new Material(Shader.Find("BananaRoulette/Retro"));
                agua.SetColor("_BaseColor", new Color(0.8f, 0.92f, 0.97f)); agua.SetFloat("_SemLuz", 1f);
                for (int i = 0; i < gotas.Length; i++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = "Gota de suor";
                    Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(transform, false);
                    var r = go.GetComponent<Renderer>(); r.sharedMaterial = agua; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    go.SetActive(false);
                    gotas[i] = go.transform; idadeGota[i] = -1f;
                }
            }
            proxGota -= dt;
            if (Suor > 0.02f && proxGota <= 0f)
            {
                proxGota = Mathf.Lerp(0.7f, 0.08f, Mathf.Clamp01(Suor));
                for (int i = 0; i < gotas.Length; i++)
                {
                    if (idadeGota[i] >= 0f) continue;
                    float lado = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                    Vector3 c = transform.InverseTransformPoint(cabeca.position);
                    gotas[i].localPosition = c + new Vector3(lado * UnityEngine.Random.Range(1.2f, 2.2f), UnityEngine.Random.Range(3.2f, 4.6f), 0.9f);
                    velGota[i] = new Vector3(lado * UnityEngine.Random.Range(2.5f, 5f), UnityEngine.Random.Range(3f, 6f), UnityEngine.Random.Range(0f, 1.5f));
                    idadeGota[i] = 0f; gotas[i].gameObject.SetActive(true);
                    break;
                }
            }
            for (int i = 0; i < gotas.Length; i++)
            {
                if (idadeGota[i] < 0f) continue;
                idadeGota[i] += dt; velGota[i].y -= 26f * dt;
                gotas[i].localPosition += velGota[i] * dt;
                float k = idadeGota[i] / 0.55f;
                gotas[i].localScale = new Vector3(0.3f, 0.46f, 0.3f) * (1f - 0.5f * Mathf.Clamp01(k));
                if (k >= 1f) { idadeGota[i] = -1f; gotas[i].gameObject.SetActive(false); }
            }
        }

        /// <summary>Troca a cor do pelo deste macaco.</summary>
        public void DefinirPelo(Color cor)
        {
            foreach (var m in pelos) if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor);
            coresBase.Clear();
            foreach (var m in todos) coresBase.Add(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white);
            PintaTudo();
        }

        public void Laco(string nome)
        {
            ClipeJ c;
            if (!porNome.TryGetValue(nome, out c) || c == laco) return;
            laco = c; tLaco = 0f;
            if (umaVez == null) Mistura(0.18f);
        }

        /// <summary>Toca uma animacao de uma vez so. ficaNoFim = para no ultimo quadro (explosao).</summary>
        public void Tocar(string nome, bool ficaNoFim = false, int voltas = 1)
        {
            ClipeJ c;
            if (!porNome.TryGetValue(nome, out c)) return;
            umaVez = c; tUmaVez = 0f; seguraNoFim = ficaNoFim; voltasUmaVez = Mathf.Max(1, voltas);
            Mistura(nome == "explode" ? 0.05f : 0.1f);
        }

        /// <summary>Pulinho curto por cima da animacao: usado a cada seta acertada.</summary>
        public void Pulo() { pulo = 1f; }

        /// <summary>Pisca de uma cor por um instante (erro).</summary>
        public void Piscar(Color cor) { corPisca = cor; pisca = 1f; PintaTudo(); }

        /// <summary>Escurece o macaco inteiro, como tostado pela explosao.</summary>
        public void Tostar() { tosta = 1f; PintaTudo(); }

        void PintaTudo()
        {
            for (int i = 0; i < todos.Count && i < coresBase.Count; i++)
            {
                if (!todos[i].HasProperty("_BaseColor")) continue;
                Color c = coresBase[i];
                if (tosta > 0f) c = Color.Lerp(c, new Color(0.07f, 0.06f, 0.06f, c.a), tosta * 0.9f);
                if (pisca > 0f) c = Color.Lerp(c, corPisca, pisca * 0.7f);
                todos[i].SetColor("_BaseColor", c);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (umaVez != null)
            {
                tUmaVez += dt;
                if (tUmaVez >= (umaVez.fim - umaVez.ini) * voltasUmaVez)
                {
                    if (seguraNoFim) tUmaVez = umaVez.fim - umaVez.ini;
                    else { umaVez = null; tLaco = 0f; Mistura(0.2f); }
                }
            }
            if (umaVez == null && laco != null)
            {
                tLaco += dt;
                float dur = laco.fim - laco.ini;
                if (tLaco >= dur) tLaco %= dur;
            }
            if (mistura > 0f) mistura = Mathf.Max(0f, mistura - dt / durMistura);
            PassoDoSuor(dt);
            if (pisca > 0f) { pisca = Mathf.Max(0f, pisca - dt * 5f); PintaTudo(); }
            pulo = Mathf.Max(0f, pulo - dt * 7f);
            transform.localPosition = posBase + Vector3.up * (Mathf.Sin(pulo * Mathf.PI) * 0.22f);
            Amostrar();
        }

        void Amostrar()
        {
            if (estado == null) return;
            ClipeJ c = umaVez != null ? umaVez : laco;
            if (c == null) return;
            float local = umaVez != null ? tUmaVez : tLaco;
            if (umaVez != null && voltasUmaVez > 1) local %= (umaVez.fim - umaVez.ini);
            // um fio antes do fim do trecho, para nao cair no primeiro quadro da animacao seguinte
            float t = Mathf.Min(c.ini + local, c.fim - 0.5f / dados.fps);
            float m = mistura > 0f && estadoDe != null ? mistura * mistura * (3f - 2f * mistura) : 0f;
            estado.enabled = true; estado.weight = 1f - m; estado.time = t;
            if (m > 0f) { estadoDe.enabled = true; estadoDe.weight = m; estadoDe.time = tDe; }
            anim.Sample();
            estado.enabled = false;
            if (estadoDe != null) estadoDe.enabled = false;
            tAmostra = t;

            if (dobra > 0f && orelhaEixo != null)
                for (int i = 0; i < orelhas.Count; i++)
                    if (orelhaEixo[i] != Vector3.zero) orelhas[i].localRotation = Quaternion.AngleAxis(dobra * Mathf.Rad2Deg, orelhaEixo[i]) * orelhaRepouso[i];
        }
    }
}
