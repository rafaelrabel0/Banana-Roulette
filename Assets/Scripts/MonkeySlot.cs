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

        private Renderer _renderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private Vector3 _homePosition;
        private float _jugglePhase;
        private bool _isHolding;

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

        public void SetHolding(bool holding)
        {
            _isHolding = holding;
        }

        private void Update()
        {
            if (!IsAlive) return;

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

        /// <summary>Comemoracao do sobrevivente.</summary>
        public void Celebrate()
        {
            StopAllCoroutines();
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
            get { return transform.position + Vector3.up * 1.35f; }
        }
    }
}
