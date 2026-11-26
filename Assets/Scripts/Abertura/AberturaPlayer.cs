using System;
using UnityEngine;
using UnityEngine.Events;

namespace BananaRoulette
{
    /// <summary>
    /// Toca a cena de abertura que veio assada dentro de <c>Assets/Abertura/abertura.glb</c>.
    ///
    /// O .glb traz todas as pecas e um unico clipe com 41 s de movimento. Este componente
    /// faz o que o .glb nao consegue carregar:
    ///   - poe a camera da Unity onde o no "Camera" do .glb esta a cada quadro;
    ///   - troca o campo de visao, que muda de plano para plano (vem em abertura.json);
    ///   - troca o fundo e a nevoa entre floresta e ceu (planos do aviador).
    ///
    /// O tempo e controlado aqui, nao pelo Animator: assim da para pausar, pular e
    /// saber exatamente em que quadro a cena esta.
    /// </summary>
    public class AberturaPlayer : MonoBehaviour
    {
        [Header("Ligacoes (o menu Banana Roulette > Montar cena de abertura preenche)")]
        public Animator animator;
        public Animation animacaoLegada;
        public string nomeDoClipe = "Abertura";
        public TextAsset dados;
        public Camera cameraDaCena;
        public Transform noCamera;

        [Header("Comportamento")]
        public bool tocarAoIniciar = true;
        public bool repetir = false;
        [Tooltip("A camera do three.js olha para tras em relacao a da Unity. Desligue se a cena aparecer de costas.")]
        public bool girarCamera180 = true;
        [Range(0.1f, 3f)] public float velocidade = 1f;
        [Tooltip("Desligado, a cena fica parada no quadro atual e outro script (o menu) cuida da camera.")]
        public bool controlaCamera = true;
        public bool comSom = true;

        [Header("Nevoa da floresta")]
        public float nevoaInicio = 125f;
        public float nevoaFim = 340f;

        public UnityEvent aoTerminar;

        [Serializable]
        class Dados
        {
            public float fps = 24f;
            public int quadros;
            public float duracao;
            public float[] fov;
            public int[] ceu;
            public int[] titulo;
            public string corSelva = "#93BC7C";
            public string corCeu = "#8EC5E8";
        }

        Dados d;
        Color corSelva = new Color(0.576f, 0.737f, 0.486f);
        Color corCeu = new Color(0.557f, 0.773f, 0.910f);
        int estado;
        float tempo;
        bool tocando;

        /// <summary>Tempo atual da cena, em segundos.</summary>
        public float Tempo => tempo;
        public float Duracao => d != null && d.duracao > 0f ? d.duracao : 41.05f;
        public bool Tocando => tocando;
        /// <summary>Verdadeiro do momento em que o letreiro "A partida comeca!" deve aparecer.</summary>
        public bool MostrarTitulo { get; private set; }

        void Awake()
        {
            if (dados != null) d = JsonUtility.FromJson<Dados>(dados.text);
            if (d == null) d = new Dados();
            Color c;
            if (ColorUtility.TryParseHtmlString(d.corSelva, out c)) corSelva = c;
            if (ColorUtility.TryParseHtmlString(d.corCeu, out c)) corCeu = c;
            estado = Animator.StringToHash(nomeDoClipe);
            if (cameraDaCena == null) cameraDaCena = Camera.main;
            if (animator != null) animator.speed = 0f;
        }

        void Start()
        {
            if (tocarAoIniciar) Tocar();
            else Amostrar();
        }

        public void Tocar() { TocarDe(0f); }
        /// <summary>Comeca a tocar a partir de um ponto, em segundos.</summary>
        public void TocarDe(float segundos) { tempo = Mathf.Clamp(segundos, 0f, Duracao); tocando = true; }

        /// <summary>
        /// Faz a camera sair de onde ela esta agora (a do menu) e chegar na da cena em 1,5 s, sem corte.
        /// Chamar logo antes de TocarDe.
        /// </summary>
        public void MisturarDe(Vector3 posicao, Quaternion giro, float campo)
        {
            saidaPos = posicao; saidaGiro = giro; saidaCampo = campo; mistura = 1f;
        }

        Vector3 saidaPos; Quaternion saidaGiro; float saidaCampo, mistura;

        // Sons da cena, no tempo em que acontecem (os mesmos da previa).
        struct Evento { public float t; public Action som; public Evento(float t, Action som) { this.t = t; this.som = som; } }
        static readonly Evento[] Eventos =
        {
            new Evento(0.1f, () => SomRetro.Ruido(1.2f, 0.15f, 600f, "bandpass")),
            new Evento(7.35f, () => SomRetro.Tom(260f, 0.18f, "triangle", 0.12f, 330f)),
            new Evento(9.95f, () => SomRetro.Tom(240f, 0.2f, "triangle", 0.12f, 300f)),
            new Evento(11.55f, () => SomRetro.Tom(700f, 0.09f, "sine", 0.15f, 1100f)),
            new Evento(15.1f, () => SomRetro.Tom(700f, 0.09f, "sine", 0.15f, 1200f)),
            new Evento(16.15f, () => SomRetro.Grito(420f, 760f, 0.16f, 0.4f)),
            new Evento(17.1f, () => { SomRetro.Grito(320f, 520f, 0.14f, 0.5f); SomRetro.Grito(360f, 600f, 0.18f, 0.5f, 0.18f); }),
            new Evento(18.4f, () => SomRetro.Tom(92f, 8.6f, "sawtooth", 0.11f, 84f)),
            new Evento(22.1f, () => SomRetro.Ruido(1.0f, 0.06f, 2500f, "highpass")),
            new Evento(23.8f, () => SomRetro.Ruido(1.3f, 0.1f, 900f, "bandpass")),
            new Evento(25.35f, () => SomRetro.Tom(1400f, 0.06f, "square", 0.08f)),
            new Evento(25.8f, () => SomRetro.Tom(1200f, 1.2f, "sine", 0.06f, 300f)),
            new Evento(28.55f, () => SomRetro.Tom(420f, 0.05f, "square", 0.06f)),
            new Evento(29.25f, () => { SomRetro.Ruido(0.25f, 0.25f, 3000f, "highpass"); SomRetro.Ruido(5.4f, 0.1f, 5000f, "highpass"); }),
            new Evento(33.1f, () => { SomRetro.Grito(620f, 980f, 0.16f, 0.6f); SomRetro.Grito(680f, 1080f, 0.2f, 0.6f, 0.2f); SomRetro.Grito(720f, 1160f, 0.26f, 0.6f, 0.42f); }),
            new Evento(34.3f, () => SomRetro.Ruido(0.5f, 0.2f, 900f, "bandpass")),
            new Evento(35.95f, () => SomRetro.Tom(1500f, 0.65f, "sine", 0.07f, 350f)),
            new Evento(36.6f, () => { SomRetro.Ruido(0.2f, 0.3f, 300f, "lowpass"); SomRetro.Tom(110f, 0.15f, "sine", 0.3f, 60f); }),
            new Evento(37.5f, () => { SomRetro.Grito(640f, 1000f, 0.18f, 0.6f); SomRetro.Grito(700f, 1100f, 0.2f, 0.5f, 0.15f); }),
            new Evento(38.27f, () => SomRetro.Ruido(0.35f, 0.2f, 900f, "bandpass")),
            new Evento(38.85f, () => { SomRetro.Tom(140f, 0.1f, "sine", 0.25f, 80f); SomRetro.Grito(720f, 1160f, 0.3f, 0.6f); }),
            new Evento(39.5f, () => { SomRetro.Tom(392f, 0.5f, "square", 0.08f); SomRetro.Tom(523f, 0.5f, "square", 0.08f); SomRetro.Tom(659f, 0.6f, "square", 0.08f); })
        };
        public void Pausar() { tocando = false; }
        public void Continuar() { tocando = true; }
        public void IrPara(float segundos) { tempo = Mathf.Clamp(segundos, 0f, Duracao); Amostrar(); }

        void Update()
        {
            if (tocando)
            {
                float antes = tempo;
                tempo += Time.deltaTime * velocidade;
                mistura = Mathf.Max(0f, mistura - Time.deltaTime / 1.5f);
                if (comSom)
                    for (int e = 0; e < Eventos.Length; e++)
                        if (antes < Eventos[e].t && tempo >= Eventos[e].t) Eventos[e].som();
                if (tempo >= Duracao)
                {
                    if (repetir) tempo = 0f;
                    else { tempo = Duracao; tocando = false; aoTerminar?.Invoke(); }
                }
            }
            Amostrar();
        }

        // Poe todas as pecas na pose do tempo atual.
        void Amostrar()
        {
            float fracao = Mathf.Clamp01(tempo / Duracao) * 0.9999f;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.speed = 0f;
                animator.Play(estado, 0, fracao);
            }
            else if (animacaoLegada != null)
            {
                AnimationState s = animacaoLegada[nomeDoClipe];
                if (s != null)
                {
                    s.enabled = true; s.weight = 1f; s.time = fracao * s.length;
                    animacaoLegada.Sample();
                    s.enabled = false;
                }
            }
        }

        // Depois que a animacao moveu os nos, copia o no "Camera" para a camera de verdade.
        void LateUpdate()
        {
            if (cameraDaCena == null || noCamera == null) return;
            Quaternion giro = girarCamera180 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            int n = d.fov != null ? d.fov.Length : 0;
            int i = n > 0 ? Mathf.Clamp(Mathf.FloorToInt(tempo * d.fps), 0, n - 1) : 0;
            if (controlaCamera)
            {
                Vector3 pos = noCamera.position;
                Quaternion rot = noCamera.rotation * giro;
                float campo = n > 0 ? d.fov[i] : cameraDaCena.fieldOfView;
                if (mistura > 0f)
                {
                    float w = mistura * mistura * (3f - 2f * mistura);
                    pos = Vector3.Lerp(pos, saidaPos, w);
                    rot = Quaternion.Slerp(rot, saidaGiro, w);
                    campo = Mathf.Lerp(campo, saidaCampo, w);
                }
                cameraDaCena.transform.SetPositionAndRotation(pos, rot);
                cameraDaCena.fieldOfView = campo;
            }

            bool noCeu = d.ceu != null && i < d.ceu.Length && d.ceu[i] == 1;
            MostrarTitulo = d.titulo != null && i < d.titulo.Length && d.titulo[i] == 1;

            Color fundo = noCeu ? corCeu : corSelva;
            cameraDaCena.clearFlags = CameraClearFlags.SolidColor;
            cameraDaCena.backgroundColor = fundo;
            RenderSettings.fog = !noCeu;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fundo;
            RenderSettings.fogStartDistance = nevoaInicio;
            RenderSettings.fogEndDistance = nevoaFim;
        }
    }
}
