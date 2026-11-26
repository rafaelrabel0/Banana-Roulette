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

        public void Tocar() { tempo = 0f; tocando = true; }
        public void Pausar() { tocando = false; }
        public void Continuar() { tocando = true; }
        public void IrPara(float segundos) { tempo = Mathf.Clamp(segundos, 0f, Duracao); Amostrar(); }

        void Update()
        {
            if (tocando)
            {
                tempo += Time.deltaTime * velocidade;
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
            cameraDaCena.transform.SetPositionAndRotation(noCamera.position, noCamera.rotation * giro);

            int n = d.fov != null ? d.fov.Length : 0;
            int i = n > 0 ? Mathf.Clamp(Mathf.FloorToInt(tempo * d.fps), 0, n - 1) : 0;
            if (n > 0) cameraDaCena.fieldOfView = d.fov[i];

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
