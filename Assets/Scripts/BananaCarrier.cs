using System.Collections;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// A banana de dinamite. Fica na mao de quem segura, faz malabarismo, voa em
    /// arco quando passada, e tem um pavio que ENCURTA de verdade conforme queima.
    ///
    /// O pavio visual e a peca central da tensao: o jogador nunca sabe quantos
    /// segundos faltam, mas ve o cordao sumindo e a faisca descendo.
    /// </summary>
    public class BananaCarrier : MonoBehaviour
    {
        private Transform _fuseCord;
        private Transform _fuseSpark;
        private Renderer _bodyRenderer;
        private Renderer _sparkRenderer;
        private MonkeySlot _holder;
        private bool _inFlight;

        private float _cordFullLength;
        private float _cordBaseY;
        private float _burnFlash;

        public void Setup(Renderer bodyRenderer, Transform fuseCord, Transform fuseSpark,
            Renderer sparkRenderer)
        {
            _bodyRenderer = bodyRenderer;
            _fuseCord = fuseCord;
            _fuseSpark = fuseSpark;
            _sparkRenderer = sparkRenderer;

            _cordFullLength = fuseCord.localScale.y;
            _cordBaseY = fuseCord.localPosition.y - _cordFullLength;
        }

        public void SnapTo(MonkeySlot slot)
        {
            _holder = slot;
            _inFlight = false;
            transform.position = slot.HandPosition;
        }

        /// <summary>Voo em arco ate o novo dono. Puramente visual: a posse ja mudou.</summary>
        public IEnumerator FlyTo(MonkeySlot target, float duration)
        {
            _inFlight = true;
            Vector3 start = transform.position;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                Vector3 flat = Vector3.Lerp(start, target.HandPosition, k);
                flat.y += Mathf.Sin(k * Mathf.PI) * 2.2f;
                transform.position = flat;
                transform.Rotate(Vector3.forward, 900f * Time.deltaTime, Space.Self);
                yield return null;
            }

            _holder = target;
            _inFlight = false;
            transform.position = target.HandPosition;
        }

        /// <summary>Erro do portador: a banana escapa da mao e volta. Custa tempo.</summary>
        public IEnumerator Fumble(float duration)
        {
            _inFlight = true;
            Vector3 home = _holder != null ? _holder.HandPosition : transform.position;
            Vector3 away = home + new Vector3(Random.Range(-1f, 1f), 1.6f, Random.Range(-0.6f, 0.6f));
            float half = Mathf.Max(0.05f, duration * 0.5f);

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(home, away, t / half);
                transform.Rotate(Vector3.forward, 1200f * Time.deltaTime, Space.Self);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                Vector3 target = _holder != null ? _holder.HandPosition : home;
                transform.position = Vector3.Lerp(away, target, t / half);
                transform.Rotate(Vector3.forward, 1200f * Time.deltaTime, Space.Self);
                yield return null;
            }

            _inFlight = false;
        }

        private void Update()
        {
            if (!_inFlight && _holder != null)
            {
                transform.position = _holder.HandPosition + Vector3.up * Mathf.Sin(Time.time * 7f) * 0.12f;
                transform.Rotate(Vector3.forward, 160f * Time.deltaTime, Space.Self);
            }

            if (_burnFlash > 0f) _burnFlash = Mathf.Max(0f, _burnFlash - Time.deltaTime * 2.2f);
        }

        /// <summary>
        /// Encurta o cordao do pavio e desce a faisca. A fracao vai de 0 (inteiro)
        /// a 1 (no fim).
        /// </summary>
        public void UpdateFuseVisual(float burnedFraction)
        {
            if (_fuseCord == null || _fuseSpark == null) return;

            float remaining = Mathf.Clamp01(1f - burnedFraction);
            float length = Mathf.Max(0.02f, _cordFullLength * remaining);

            // O cilindro cresce para os dois lados a partir do centro: manter a
            // base fixa e subir o centro conforme encurta.
            var scale = _fuseCord.localScale;
            scale.y = length;
            _fuseCord.localScale = scale;
            _fuseCord.localPosition = new Vector3(
                _fuseCord.localPosition.x, _cordBaseY + length, _fuseCord.localPosition.z);

            // A faisca mora na ponta do que sobrou.
            _fuseSpark.localPosition = new Vector3(
                _fuseCord.localPosition.x, _cordBaseY + length * 2f, _fuseCord.localPosition.z);

            float rate = Mathf.Lerp(5f, 26f, burnedFraction);
            float pulse = 0.65f + Mathf.Abs(Mathf.Sin(Time.time * rate)) * 0.85f;
            float flash = 1f + _burnFlash * 1.8f;
            _fuseSpark.localScale = Vector3.one * 0.2f * pulse * flash;

            if (_sparkRenderer != null)
            {
                _sparkRenderer.material.color = Color.Lerp(
                    Palette.BananaYellow, Color.white, Mathf.Clamp01(burnedFraction * 0.5f + _burnFlash));
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.material.color = Color.Lerp(
                    Palette.DynamiteRed, Color.white,
                    Mathf.Clamp01(burnedFraction * 0.22f * pulse + _burnFlash * 0.5f));
            }
        }

        /// <summary>Estouro visual quando um erro queima pavio de uma vez.</summary>
        public void FlashBurn()
        {
            _burnFlash = 1f;
        }

        public Vector3 SparkWorldPosition
        {
            get { return _fuseSpark != null ? _fuseSpark.position : transform.position; }
        }
    }
}
