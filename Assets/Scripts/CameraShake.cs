using System.Collections;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>Tremida da camera. E o que faz a explosao virar reacao.</summary>
    public class CameraShake : MonoBehaviour
    {
        private Vector3 _home;
        private bool _homeCaptured;

        /// <summary>
        /// A posicao de descanso e capturada no primeiro tremor, nao no Awake.
        /// O Awake roda junto com o AddComponent, antes de o bootstrap colocar a
        /// camera no lugar: capturar ali guardava (0,0,0) e a primeira explosao
        /// jogava a camera para a origem, deixando a cena fora de quadro.
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            if (!_homeCaptured)
            {
                _home = transform.localPosition;
                _homeCaptured = true;
            }

            StopAllCoroutines();
            transform.localPosition = _home;
            StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float falloff = 1f - Mathf.Clamp01(t / duration);
                transform.localPosition = _home + (Vector3)Random.insideUnitCircle * magnitude * falloff;
                yield return null;
            }
            transform.localPosition = _home;
        }
    }
}
