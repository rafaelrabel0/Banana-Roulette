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

        // --- versao 3D (maco de dinamite da previa) ---
        private bool _tresD;
        private Vector3[] _curva;          // caminho do pavio, no espaco do modelo
        private UnityEngine.Mesh _malhaPavio;
        private Material[] _corpo;
        private Color[] _corpoBase;
        private float _raioPavio;

        private Transform _ponto;

        /// <summary>Faz a bomba acompanhar um ponto qualquer (a mao do macaco de mostra, no menu). Nulo solta.</summary>
        public void SeguirPonto(Transform ponto) { _ponto = ponto; }

        /// <summary>
        /// Monta o maco de dinamite em 3D dentro de "root": modelo, pavio que encurta, faisca e materiais proprios.
        /// Devolve nulo se o modelo nao existir. escala = tamanho do modelo; corBomba = posicao em Aparencia.Bombas.
        /// </summary>
        public static BananaCarrier Montar3D(GameObject root, float escala, int corBomba)
        {
            var modelo = Resources.Load<GameObject>("Jogo/maco-jogo");
            if (modelo == null) return null;

            var copia = Instantiate(modelo, root.transform);
            copia.transform.localScale = Vector3.one * escala;
            VisualRetro.Converter(copia);
            Aparencia.PintaBomba(copia, corBomba);

            Transform faisca = null, pavioVelho = null;
            foreach (Transform t in copia.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Faisca")) faisca = t;
                else if (t.name.StartsWith("Pavio")) pavioVelho = t;
            }
            if (faisca == null) { Destroy(copia); return null; }
            if (pavioVelho != null) pavioVelho.gameObject.SetActive(false); // o cordao pronto sai; entra o que encurta

            var cordao = new GameObject("Pavio vivo", typeof(MeshFilter), typeof(MeshRenderer));
            cordao.transform.SetParent(faisca.parent, false);
            var tinta = new Material(Shader.Find("BananaRoulette/Retro"));
            tinta.SetColor("_BaseColor", new Color32(0x5A, 0x3A, 0x1C, 0xFF)); tinta.SetFloat("_SemLuz", 1f); tinta.SetFloat("_Cull", 0f);
            cordao.GetComponent<MeshRenderer>().sharedMaterial = tinta;

            // corpo do maco com materiais proprios, para poder clarear quando o pavio esta no fim
            var corpo = new System.Collections.Generic.List<Material>();
            foreach (Renderer r in copia.GetComponentsInChildren<Renderer>(true))
            {
                Material[] ms = r.sharedMaterials;
                bool mudou = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null || !ms[i].name.StartsWith("Dinamite")) continue;
                    ms[i] = new Material(ms[i]) { name = ms[i].name };
                    corpo.Add(ms[i]); mudou = true;
                }
                if (mudou) r.sharedMaterials = ms;
            }

            var rFaisca = faisca.GetComponentInChildren<Renderer>();
            if (rFaisca != null) rFaisca.sharedMaterial = new Material(rFaisca.sharedMaterial);

            // caminho do pavio na previa (eixo do three.js); aqui o X entra espelhado
            var curva = new[]
            {
                new Vector3(-2f, 0f, 0f), new Vector3(-2.65f, 0.3f, 0.1f), new Vector3(-3.25f, -0.4f, 0.3f),
                new Vector3(-3.8f, -1.08f, 0.2f), new Vector3(-4.4f, -1.1f, -0.1f)
            };
            var carrier = root.AddComponent<BananaCarrier>();
            carrier.Setup3D(faisca, rFaisca, cordao.GetComponent<MeshFilter>(), curva, 0.09f, corpo.ToArray());
            return carrier;
        }

        /// <summary>
        /// Liga o modelo 3D. curva = pontos por onde o pavio passa (do maco ate a ponta), no espaco do modelo;
        /// pavio = objeto vazio com MeshFilter onde o cordao e redesenhado conforme queima.
        /// </summary>
        public void Setup3D(Transform fuseSpark, Renderer sparkRenderer, MeshFilter pavio, Vector3[] curva,
            float raioPavio, Material[] corpo)
        {
            _tresD = true;
            _fuseSpark = fuseSpark;
            _sparkRenderer = sparkRenderer;
            _curva = Suaviza(curva, 5);
            _raioPavio = raioPavio;
            _malhaPavio = new UnityEngine.Mesh();
            pavio.sharedMesh = _malhaPavio;
            _corpo = corpo;
            _corpoBase = new Color[corpo.Length];
            for (int i = 0; i < corpo.Length; i++) _corpoBase[i] = corpo[i].HasProperty("_BaseColor") ? corpo[i].GetColor("_BaseColor") : Color.white;
            UpdateFuseVisual(0f);
        }

        // Curva suave passando pelos pontos (Catmull-Rom), com "passos" pedacos entre cada par.
        private static Vector3[] Suaviza(Vector3[] p, int passos)
        {
            var saida = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < p.Length - 1; i++)
            {
                Vector3 a = p[Mathf.Max(0, i - 1)], b = p[i], c = p[i + 1], d = p[Mathf.Min(p.Length - 1, i + 2)];
                for (int k = 0; k < passos; k++)
                {
                    float t = (float)k / passos, t2 = t * t, t3 = t2 * t;
                    saida.Add(0.5f * ((2f * b) + (-a + c) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (-a + 3f * b - 3f * c + d) * t3));
                }
            }
            saida.Add(p[p.Length - 1]);
            return saida.ToArray();
        }

        /// <summary>Cordao de 4 lados ao longo dos primeiros "ate" pontos da curva (com fracao no ultimo).</summary>
        public static Vector3 DesenhaCordao(UnityEngine.Mesh malha, Vector3[] curva, float fracao, float raio)
        {
            float alvo = Mathf.Clamp01(fracao) * (curva.Length - 1);
            int cheios = Mathf.FloorToInt(alvo);
            var pts = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i <= cheios && i < curva.Length; i++) pts.Add(curva[i]);
            if (cheios < curva.Length - 1 && alvo - cheios > 0.001f) pts.Add(Vector3.Lerp(curva[cheios], curva[cheios + 1], alvo - cheios));
            if (pts.Count < 2) pts.Add(pts[0] + (curva[1] - curva[0]).normalized * 0.01f);

            var vs = new Vector3[pts.Count * 4]; var ts = new int[(pts.Count - 1) * 48];
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 dir = (pts[Mathf.Min(pts.Count - 1, i + 1)] - pts[Mathf.Max(0, i - 1)]).normalized;
                Vector3 lado = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
                Vector3 cima = Vector3.Cross(lado, dir);
                vs[i * 4] = pts[i] + lado * raio; vs[i * 4 + 1] = pts[i] + cima * raio; vs[i * 4 + 2] = pts[i] - lado * raio; vs[i * 4 + 3] = pts[i] - cima * raio;
            }
            int n = 0;
            for (int i = 0; i < pts.Count - 1; i++)
                for (int k = 0; k < 4; k++)
                {
                    int a = i * 4 + k, b = i * 4 + (k + 1) % 4, c = a + 4, d = b + 4;
                    ts[n++] = a; ts[n++] = c; ts[n++] = b; ts[n++] = b; ts[n++] = c; ts[n++] = d;
                    // as duas faces, para o cordao aparecer de qualquer lado
                    ts[n++] = a; ts[n++] = b; ts[n++] = c; ts[n++] = b; ts[n++] = d; ts[n++] = c;
                }
            malha.Clear(); malha.vertices = vs; malha.triangles = ts; malha.RecalculateBounds();
            return pts[pts.Count - 1];
        }

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
        public IEnumerator FlyTo(MonkeySlot target, float duration, float height = 2.2f)
        {
            _inFlight = true;
            Vector3 start = transform.position;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                Vector3 flat = Vector3.Lerp(start, target.HandPosition, k);
                flat.y += Mathf.Sin(k * Mathf.PI) * height;
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
            if (_ponto != null) { transform.SetPositionAndRotation(_ponto.position, _ponto.rotation); }
            Quaternion giro;
            if (_ponto != null) { }
            else if (!_inFlight && _holder != null && _holder.TryGetHandRotation(out giro))
            {
                // 3D: a bomba segue a mao do macaco, que ja faz o malabarismo na animacao
                transform.SetPositionAndRotation(_holder.HandPosition, giro);
            }
            else if (!_inFlight && _holder != null)
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
            if (_tresD) { Atualiza3D(burnedFraction); return; }
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

        private void Atualiza3D(float burnedFraction)
        {
            float resta = Mathf.Clamp01(1f - burnedFraction);
            Vector3 ponta = DesenhaCordao(_malhaPavio, _curva, Mathf.Max(0.04f, resta), _raioPavio);

            float rate = Mathf.Lerp(5f, 26f, burnedFraction);
            float pulse = 0.65f + Mathf.Abs(Mathf.Sin(Time.time * rate)) * 0.85f;
            float flash = 1f + _burnFlash * 1.8f;
            if (_fuseSpark != null)
            {
                _fuseSpark.localPosition = ponta;
                _fuseSpark.localScale = Vector3.one * pulse * flash;
            }
            if (_sparkRenderer != null && _sparkRenderer.sharedMaterial.HasProperty("_BaseColor"))
                _sparkRenderer.sharedMaterial.SetColor("_BaseColor", Color.Lerp(new Color(0.95f, 0.54f, 0.1f), Color.white, Mathf.Clamp01(burnedFraction * 0.5f + _burnFlash)));

            float branco = Mathf.Clamp01(burnedFraction * 0.22f * pulse + _burnFlash * 0.5f);
            for (int i = 0; i < _corpo.Length; i++)
                if (_corpo[i].HasProperty("_BaseColor")) _corpo[i].SetColor("_BaseColor", Color.Lerp(_corpoBase[i], Color.white, branco));
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
