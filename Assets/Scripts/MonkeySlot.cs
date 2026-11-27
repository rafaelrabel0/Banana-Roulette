using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Um lugar na roda. Sabe se esta vivo, se e o humano, e cuida da propria
    /// aparencia no greybox (capsula colorida, susto, malabarismo, explosao).
    /// </summary>
    public class MonkeySlot : MonoBehaviour
    {
        public int Index { get; private set; }
        public bool IsHuman { get; private set; }
        public bool IsAlive { get; private set; } = true;

        /// <summary>Vidas que sobram. Cada explosao tira uma; no zero ele sai voando da roda.</summary>
        public int Lives { get; private set; } = 2;
        /// <summary>O poder que este macaco esta guardando (um por vez).</summary>
        public PowerUp Guardado = PowerUp.Nenhum;

        private readonly Transform[] _bolinhas = new Transform[4]; // 3 de vida + 1 do poder guardado

        public void GanhaVida() { Lives = Mathf.Min(3, Lives + 1); }

        /// <summary>Tira uma vida. Devolve verdadeiro se era a ultima.</summary>
        public bool PerdeVida()
        {
            Lives = Mathf.Max(0, Lives - 1);
            return Lives <= 0;
        }

        /// <summary>Explodiu mas ainda tem vida: cai para tras, pisca de preto e se levanta.</summary>
        public void Chamuscar()
        {
            if (_visual == null) return;
            _visual.Tocar("explode");
            _visual.Piscar(new Color(0.08f, 0.07f, 0.06f));
        }

        private void MontaBolinhas()
        {
            var tinta = Shader.Find("BananaRoulette/Retro");
            for (int i = 0; i < _bolinhas.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = i < 3 ? "Vida " + (i + 1) : "Poder guardado";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                go.transform.localPosition = i < 3 ? new Vector3(-0.34f + 0.34f * i, 3.55f, 0f) : new Vector3(0f, 4.0f, 0f); // acima de qualquer chapeu
                go.transform.localScale = Vector3.one * (i < 3 ? 0.26f : 0.32f);
                var r = go.GetComponent<Renderer>();
                var m = new Material(tinta);
                m.SetColor("_BaseColor", i < 3 ? new Color(0.92f, 0.22f, 0.2f) : new Color(0.25f, 0.85f, 0.95f));
                m.SetFloat("_SemLuz", 1f);
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _bolinhas[i] = go.transform;
            }
        }

        private Renderer _renderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private Vector3 _homePosition;
        private float _jugglePhase;
        private bool _isHolding;
        private MacacoVisual _visual;

        /// <summary>Quanto do pavio ja queimou (0 a 1). A partida atualiza; os macacos reagem.</summary>
        public static float Tensao;

        /// <summary>Liga o modelo 3D a este lugar da roda. A capsula do greybox some.</summary>
        public void UsarModelo(MacacoVisual visual)
        {
            _visual = visual;
            if (_visual != null && _renderer != null) _renderer.enabled = false;
            if (_visual != null) MontaBolinhas();
        }

        public MacacoVisual Visual { get { return _visual; } }

        /// <summary>Animacao de arremesso para o lado onde o alvo esta.</summary>
        public void Arremessar(Vector3 alvo)
        {
            if (_visual == null) return;
            // no modelo, jogaE vai para a esquerda do proprio macaco
            float lado = transform.InverseTransformPoint(alvo).x;
            _visual.Tocar(lado < 0f ? "jogaE" : "jogaD");
        }

        /// <summary>Animacao de esperar e receber a bomba.</summary>
        public void Receber()
        {
            if (_visual != null) _visual.Tocar("recebe");
        }

        /// <summary>Pulinho a cada seta acertada.</summary>
        public void Acertou()
        {
            if (_visual != null) _visual.Pulo();
        }

        public void Setup(int index, bool isHuman, Color color, Renderer bodyRenderer)
        {
            Index = index;
            IsHuman = isHuman;
            _renderer = bodyRenderer;
            _baseColor = color;
            _baseScale = transform.localScale;
            _homePosition = transform.position;
            _jugglePhase = Random.value * 10f;

            if (_renderer != null)
            {
                _renderer.material.color = color;
            }
        }

        /// <summary>Verdadeiro enquanto a bomba esta na mao deste macaco.</summary>
        public bool Holding { get { return _isHolding; } }

        public void SetHolding(bool holding)
        {
            _isHolding = holding;
        }

        private void Update()
        {
            // bolinhas vermelhas = vidas; a azul em cima = tem poder guardado
            for (int i = 0; i < _bolinhas.Length; i++)
            {
                if (_bolinhas[i] == null) continue;
                bool liga = IsAlive && (i < 3 ? i < Lives : Guardado != PowerUp.Nenhum);
                if (_bolinhas[i].gameObject.activeSelf != liga) _bolinhas[i].gameObject.SetActive(liga);
            }

            if (!IsAlive) return;

            if (_visual != null)
            {
                // Quanto mais curto o pavio, mais desesperado quem segura; os outros so se assustam no fim.
                if (_isHolding) _visual.Laco(Tensao > 0.7f ? "segura3" : Tensao > 0.38f ? "segura2" : "segura1");
                else _visual.Laco(Tensao > 0.45f ? "olhando" : "parado");
                return;
            }

            // Idle nervoso: respiracao leve. Segurando a banana: balanco mais agitado.
            float speed = _isHolding ? 9f : 2.2f;
            float amount = _isHolding ? 0.09f : 0.03f;
            float wave = Mathf.Sin((Time.time + _jugglePhase) * speed);

            transform.localScale = new Vector3(
                _baseScale.x * (1f - wave * amount * 0.5f),
                _baseScale.y * (1f + wave * amount),
                _baseScale.z * (1f - wave * amount * 0.5f));

            if (_isHolding)
            {
                // Pulinho nervoso de quem esta com a dinamite na mao.
                float hop = Mathf.Abs(Mathf.Sin((Time.time + _jugglePhase) * 6f)) * 0.14f;
                transform.position = _homePosition + Vector3.up * hop;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, _homePosition, Time.deltaTime * 8f);
            }
        }

        /// <summary>Piscada de erro: quem errou a sequencia deixa a banana escapar.</summary>
        public void FlashError()
        {
            if (_visual != null) { _visual.Tocar("erro"); _visual.Piscar(Palette.DynamiteRed); return; }
            if (_renderer == null) return;
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            _renderer.material.color = Palette.DynamiteRed;
            yield return new WaitForSeconds(0.18f);
            if (IsAlive) _renderer.material.color = _baseColor;
        }

        /// <summary>Explodiu: tosta, tomba e sai da roda.</summary>
        public void Explode()
        {
            IsAlive = false;
            _isHolding = false;
            StopAllCoroutines();
            if (_visual != null) { StartCoroutine(VoaLonge()); return; }
            StartCoroutine(ExplodeRoutine());
        }

        private System.Collections.IEnumerator ExplodeRoutine()
        {
            // Estufa por um instante, como desenho animado.
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                float k = 1f + (t / 0.12f) * 0.8f;
                transform.localScale = _baseScale * k;
                yield return null;
            }

            if (_renderer != null)
            {
                _renderer.material.color = Palette.Charred;
            }

            // Tomba para fora da roda e afunda no chao.
            Vector3 outward = (transform.position - Vector3.zero).normalized;
            Quaternion from = transform.rotation;
            Quaternion to = Quaternion.LookRotation(outward) * Quaternion.Euler(90f, 0f, 0f);
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + outward * 0.4f + Vector3.down * 0.35f;

            t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.5f);
                transform.rotation = Quaternion.Slerp(from, to, k);
                transform.position = Vector3.Lerp(startPos, endPos, k);
                transform.localScale = Vector3.Lerp(_baseScale * 1.8f, _baseScale * 0.9f, k);
                yield return null;
            }
        }

        // A bomba estourou: ele nao morre nem fica tostado. Sai voando para o lado, abanando os bracos e
        // gritando, ate sumir por cima das arvores. Menos violento e mais engracado.
        private System.Collections.IEnumerator VoaLonge()
        {
            _visual.Laco("erro"); // bracos abanando
            Vector3 ini = transform.position;
            Vector3 fora = new Vector3(ini.x, 0f, ini.z).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, fora) * (Index % 2 == 0 ? 1f : -1f);
            Vector3 rumo = (lado + fora * 0.35f).normalized;
            SomRetro.Grito(1100f, 380f, 1.2f, 0.5f);

            float t = 0f, dur = 2.0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                transform.position = ini + rumo * (30f * k) + Vector3.up * (Mathf.Sin(k * Mathf.PI * 0.8f) * 14f);
                transform.Rotate(Vector3.forward, 520f * Time.deltaTime, Space.Self);
                transform.localScale = _baseScale * Mathf.Lerp(1f, 0.4f, k * k);
                yield return null;
            }
            _visual.gameObject.SetActive(false);
        }

        /// <summary>Comemoracao do sobrevivente.</summary>
        public void Celebrate()
        {
            StopAllCoroutines();
            if (_visual != null) { _visual.Laco("vence"); return; }
            StartCoroutine(CelebrateRoutine());
        }

        private System.Collections.IEnumerator CelebrateRoutine()
        {
            while (true)
            {
                float hop = Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.9f;
                transform.position = _homePosition + Vector3.up * hop;
                transform.Rotate(Vector3.up, 180f * Time.deltaTime, Space.World);
                yield return null;
            }
        }

        public Vector3 HandPosition
        {
            get { return _visual != null && _visual.PontoMaco != null ? _visual.PontoMaco.position : transform.position + Vector3.up * 1.35f; }
        }

        /// <summary>Giro da bomba enquanto esta na mao (o modelo 3D faz malabarismo com ela).</summary>
        public bool TryGetHandRotation(out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (_visual == null || _visual.PontoMaco == null) return false;
            rotation = _visual.PontoMaco.rotation;
            return true;
        }
    }
}
