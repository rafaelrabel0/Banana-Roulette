using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// O pavio da rodada atravessado no topo da tela, com a banana de dinamite
    /// numa ponta. A faisca corre da esquerda para a direita e o cordao some
    /// atras dela, deixando o rastro queimado a vista.
    ///
    /// Anda pela MESMA fracao do <see cref="FuseTimer"/> que a banana 3D na mao
    /// dos macacos: quando uma estoura, a outra estoura junto. O jogador continua
    /// sem saber quantos segundos faltam, mas passa a ver o tempo acabando sem
    /// precisar procurar a banana no meio da roda.
    /// </summary>
    public class TopFuseBar : MonoBehaviour
    {
        private const float BarHeight = 88f;
        private const float CordThickness = 17f;
        private const float LeftMargin = 64f;
        private const float RightMargin = 268f;

        /// <summary>Onde a dinamite fica, contada a partir da borda direita.</summary>
        private const float DynamiteX = -156f;

        private RectTransform _cordArea;
        private RectTransform _cordRect;
        private Image _cordImage;
        private RectTransform _sparkRect;
        private Image _sparkImage;
        private Image _sparkGlow;

        private RectTransform _dynamite;
        private Image _dynamiteBody;
        private Image _dynamiteStripe;
        private Vector3 _dynamiteHomeScale;

        private Image _flash;
        private readonly List<RectTransform> _shards = new List<RectTransform>();

        private float _burned;
        private bool _exploded;

        public static TopFuseBar Build(Transform parent)
        {
            var go = new GameObject("TopFuseBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -BarHeight);
            rect.offsetMax = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(0f, -14f);

            var bar = go.AddComponent<TopFuseBar>();
            bar.BuildParts(rect);
            return bar;
        }

        private void BuildParts(RectTransform root)
        {
            // Faixa do cordao, entre a margem esquerda e o espaco da dinamite.
            var areaGo = new GameObject("CordArea", typeof(RectTransform));
            areaGo.transform.SetParent(root, false);
            _cordArea = areaGo.GetComponent<RectTransform>();
            _cordArea.anchorMin = new Vector2(0f, 0.5f);
            _cordArea.anchorMax = new Vector2(1f, 0.5f);
            _cordArea.pivot = new Vector2(0.5f, 0.5f);
            _cordArea.offsetMin = new Vector2(LeftMargin, -CordThickness * 0.5f);
            _cordArea.offsetMax = new Vector2(-RightMargin, CordThickness * 0.5f);

            // Rastro queimado: fica embaixo e so aparece onde o cordao ja sumiu.
            var burned = UiFactory.CreateImage(_cordArea, "Burned", UiFactory.RoundedSprite,
                new Color32(0x2A, 0x24, 0x1E, 0xFF));
            burned.type = Image.Type.Sliced;
            UiFactory.Anchor(burned, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            // Cordao que sobra, ancorado a direita: encolhe pela esquerda.
            _cordImage = UiFactory.CreateImage(_cordArea, "Cord", UiFactory.RoundedSprite,
                new Color32(0xD8, 0xC0, 0x8E, 0xFF));
            _cordImage.type = Image.Type.Sliced;
            _cordRect = _cordImage.GetComponent<RectTransform>();
            _cordRect.anchorMin = new Vector2(1f, 0.5f);
            _cordRect.anchorMax = new Vector2(1f, 0.5f);
            _cordRect.pivot = new Vector2(1f, 0.5f);
            _cordRect.anchoredPosition = Vector2.zero;
            _cordRect.sizeDelta = new Vector2(0f, CordThickness);

            // Brilho atras da faisca, para a chama nao ficar um ponto seco.
            // Radial de verdade: com o sprite de canto arredondado o brilho
            // saia quadrado e chamava atencao pelo motivo errado.
            _sparkGlow = UiFactory.CreateImage(_cordArea, "SparkGlow", UiFactory.GlowSprite,
                new Color(1f, 0.72f, 0.28f, 0.35f));
            var glowRect = _sparkGlow.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(1f, 0.5f);
            glowRect.anchorMax = new Vector2(1f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(88f, 88f);

            _sparkImage = UiFactory.CreateImage(_cordArea, "Spark", UiFactory.CircleSprite,
                Palette.BananaYellow);
            _sparkRect = _sparkImage.GetComponent<RectTransform>();
            _sparkRect.anchorMin = new Vector2(1f, 0.5f);
            _sparkRect.anchorMax = new Vector2(1f, 0.5f);
            _sparkRect.pivot = new Vector2(0.5f, 0.5f);
            _sparkRect.sizeDelta = new Vector2(30f, 30f);

            BuildDynamite(root);
            BuildShards(root);

            // Clarao da explosao: brilho redondo em cima da dinamite, nao uma
            // faixa cheia. Retangulo solido atravessando a tela parecia defeito
            // de render, nao explosao.
            _flash = UiFactory.CreateImage(root, "Flash", UiFactory.GlowSprite,
                new Color(1f, 0.85f, 0.55f, 0f));
            var flashRect = _flash.GetComponent<RectTransform>();
            flashRect.anchorMin = new Vector2(1f, 0.5f);
            flashRect.anchorMax = new Vector2(1f, 0.5f);
            flashRect.pivot = new Vector2(0.5f, 0.5f);
            flashRect.anchoredPosition = new Vector2(DynamiteX, 0f);
            flashRect.sizeDelta = new Vector2(520f, 520f);

            ResetFuse();
        }

        /// <summary>A banana de dinamite na ponta direita, desenhada com retangulos.</summary>
        private void BuildDynamite(RectTransform root)
        {
            var go = new GameObject("Dynamite", typeof(RectTransform));
            go.transform.SetParent(root, false);
            _dynamite = go.GetComponent<RectTransform>();
            _dynamite.anchorMin = new Vector2(1f, 0.5f);
            _dynamite.anchorMax = new Vector2(1f, 0.5f);
            _dynamite.pivot = new Vector2(0.5f, 0.5f);
            _dynamite.anchoredPosition = new Vector2(DynamiteX, 0f);
            _dynamite.sizeDelta = new Vector2(150f, 70f);
            _dynamiteHomeScale = Vector3.one;

            // Casca de banana por tras: a curva amarela que da nome a coisa.
            var peel = UiFactory.CreateImage(_dynamite, "Peel", UiFactory.RoundedSprite,
                Palette.BananaYellow);
            peel.type = Image.Type.Sliced;
            var peelRect = peel.GetComponent<RectTransform>();
            peelRect.anchorMin = new Vector2(0.5f, 0.5f);
            peelRect.anchorMax = new Vector2(0.5f, 0.5f);
            peelRect.pivot = new Vector2(0.5f, 0.5f);
            peelRect.sizeDelta = new Vector2(132f, 44f);
            peelRect.anchoredPosition = new Vector2(2f, -9f);
            peelRect.localRotation = Quaternion.Euler(0f, 0f, -7f);

            _dynamiteBody = UiFactory.CreateImage(_dynamite, "Body", UiFactory.RoundedSprite,
                Palette.DynamiteRed);
            _dynamiteBody.type = Image.Type.Sliced;
            var bodyRect = _dynamiteBody.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.sizeDelta = new Vector2(122f, 40f);
            bodyRect.anchoredPosition = new Vector2(0f, 4f);
            bodyRect.localRotation = Quaternion.Euler(0f, 0f, -7f);

            _dynamiteStripe = UiFactory.CreateImage(bodyRect, "Stripe", UiFactory.BoxSprite,
                new Color(0f, 0f, 0f, 0.28f));
            UiFactory.Anchor(_dynamiteStripe, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(122f, 11f));
        }

        private void BuildShards(RectTransform root)
        {
            for (int i = 0; i < 10; i++)
            {
                var shard = UiFactory.CreateImage(root, "Shard" + i, UiFactory.RoundedSprite,
                    i % 2 == 0 ? Palette.DynamiteRed : Palette.BananaYellow);
                shard.type = Image.Type.Sliced;

                var rect = shard.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(16f, 16f);
                rect.anchoredPosition = new Vector2(DynamiteX, 0f);

                shard.gameObject.SetActive(false);
                _shards.Add(rect);
            }
        }

        // ------------------------------------------------------------------

        /// <summary>Pavio novo: cordao inteiro e dinamite de volta.</summary>
        public void ResetFuse()
        {
            _exploded = false;
            _burned = 0f;

            if (_dynamite != null)
            {
                _dynamite.gameObject.SetActive(true);
                _dynamite.localScale = _dynamiteHomeScale;
                _dynamite.localRotation = Quaternion.identity;
            }

            if (_sparkImage != null) _sparkImage.gameObject.SetActive(true);
            if (_sparkGlow != null) _sparkGlow.gameObject.SetActive(true);
            if (_cordImage != null) _cordImage.gameObject.SetActive(true);
            if (_flash != null) _flash.color = new Color(1f, 0.85f, 0.55f, 0f);

            for (int i = 0; i < _shards.Count; i++) _shards[i].gameObject.SetActive(false);

            Apply(0f);
        }

        /// <summary>Recebe a mesma fracao queimada que a banana na mao do macaco.</summary>
        public void SetProgress(float burnedFraction)
        {
            if (_exploded) return;
            _burned = Mathf.Clamp01(burnedFraction);
            Apply(_burned);
        }

        private void Apply(float burned)
        {
            if (_cordArea == null) return;

            float areaWidth = _cordArea.rect.width;
            float remaining = Mathf.Max(0f, 1f - burned);
            float cordWidth = areaWidth * remaining;

            _cordRect.sizeDelta = new Vector2(cordWidth, CordThickness);

            // A faisca mora na ponta esquerda do que sobrou.
            float sparkX = -cordWidth;
            _sparkRect.anchoredPosition = new Vector2(sparkX, 0f);

            float rate = Mathf.Lerp(7f, 26f, burned);
            float pulse = 0.8f + Mathf.Abs(Mathf.Sin(Time.time * rate)) * 0.5f;
            _sparkRect.sizeDelta = new Vector2(30f * pulse, 30f * pulse);
            _sparkImage.color = Color.Lerp(Palette.BananaYellow, Color.white, burned * 0.5f);

            var glowRect = _sparkGlow.GetComponent<RectTransform>();
            glowRect.anchoredPosition = new Vector2(sparkX, 0f);
            glowRect.sizeDelta = new Vector2(88f * pulse, 88f * pulse);
            _sparkGlow.color = new Color(1f, 0.72f, 0.28f, 0.26f + 0.3f * pulse);

            // Perto do fim a dinamite comeca a tremer e a inchar.
            if (_dynamite != null)
            {
                float panic = Mathf.Clamp01((burned - 0.7f) / 0.3f);
                float shake = Mathf.Sin(Time.time * 34f) * 4f * panic;
                _dynamite.anchoredPosition = new Vector2(DynamiteX + shake, Mathf.Cos(Time.time * 29f) * 3f * panic);
                _dynamite.localScale = _dynamiteHomeScale * (1f + panic * 0.12f * pulse);
            }
        }

        /// <summary>A dinamite do topo estoura junto com a da mao do macaco.</summary>
        public IEnumerator Explode()
        {
            if (_exploded) yield break;
            _exploded = true;

            _cordImage.gameObject.SetActive(false);
            _sparkImage.gameObject.SetActive(false);
            _sparkGlow.gameObject.SetActive(false);
            _dynamite.gameObject.SetActive(false);

            Vector2 origin = new Vector2(DynamiteX, 0f);
            var velocities = new Vector2[_shards.Count];
            for (int i = 0; i < _shards.Count; i++)
            {
                float angle = (360f / _shards.Count) * i + Random.Range(-12f, 12f);
                float rad = angle * Mathf.Deg2Rad;
                velocities[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * Random.Range(260f, 460f);

                _shards[i].anchoredPosition = origin;
                _shards[i].localScale = Vector3.one;
                _shards[i].gameObject.SetActive(true);
            }

            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float k = t / 0.55f;

                _flash.color = new Color(1f, 0.85f, 0.55f, Mathf.Lerp(0.85f, 0f, k));

                for (int i = 0; i < _shards.Count; i++)
                {
                    _shards[i].anchoredPosition = origin + velocities[i] * t;
                    _shards[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, k);
                    _shards[i].localRotation = Quaternion.Euler(0f, 0f, t * 420f * (i % 2 == 0 ? 1f : -1f));
                }

                yield return null;
            }

            _flash.color = new Color(1f, 0.85f, 0.55f, 0f);
            for (int i = 0; i < _shards.Count; i++) _shards[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Estouro curto quando um erro queima pavio de uma vez. Sem isto o
        /// salto da faisca passa despercebido, que e justamente o que o jogador
        /// precisa notar.
        /// </summary>
        public void FlashBurn()
        {
            if (_exploded) return;
            StartCoroutine(FlashBurnRoutine());
        }

        private IEnumerator FlashBurnRoutine()
        {
            float t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                float k = 1f - t / 0.32f;
                _flash.color = new Color(1f, 0.45f, 0.30f, 0.34f * k);
                _cordImage.color = Color.Lerp(new Color32(0xD8, 0xC0, 0x8E, 0xFF),
                    Palette.DynamiteRed, k);
                yield return null;
            }

            _flash.color = new Color(1f, 0.85f, 0.55f, 0f);
            _cordImage.color = new Color32(0xD8, 0xC0, 0x8E, 0xFF);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
