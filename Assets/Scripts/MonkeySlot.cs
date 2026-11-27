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
        public PowerUp Guardado { get { return Bolsa.Count > 0 ? Bolsa[0] : PowerUp.Nenhum; } }

        /// <summary>Os poderes guardados, na ordem em que foram pegos. Espaco usa o primeiro que servir.</summary>
        public readonly System.Collections.Generic.List<PowerUp> Bolsa = new System.Collections.Generic.List<PowerUp>();
        public const int MaxNaBolsa = 3;

        // ---------------------------------------------------------------- vidas: bananas no chao, ao lado do macaco
        // A banana da vez fica onde o pe dele bate (animacao "pisa"); as outras fazem fila para fora.
        // Perdeu uma vida: ele pisa nela e sobra a casca. As que restam andam um lugar.
        private readonly Transform[] _bananas = new Transform[3];
        private Transform _casca;
        private PowerUp _seloDe = PowerUp.Nenhum;
        private Vector3 _lugarDoPe, _passoDaFila;
        private int _vidasVisiveis = 2;
        private bool _pisando;

        public void GanhaVida() { Lives = Mathf.Min(3, Lives + 1); }

        /// <summary>Tira uma vida. Devolve verdadeiro se era a ultima.</summary>
        public bool PerdeVida()
        {
            Lives = Mathf.Max(0, Lives - 1);
            return Lives <= 0;
        }

        /// <summary>Explodiu mas ainda tem vida: cai para tras, pisca de preto, levanta e pisa na banana que perdeu.</summary>
        public void Chamuscar()
        {
            if (_visual == null) return;
            _visual.Tocar("explode");
            _visual.Piscar(new Color(0.08f, 0.07f, 0.06f));
            StartCoroutine(PisaNaBanana());
        }

        private System.Collections.IEnumerator PisaNaBanana()
        {
            _pisando = true;
            yield return new WaitForSeconds(1.0f);
            _visual.Tocar("pisa");
            yield return new WaitForSeconds(0.8f * 0.47f); // o instante em que o pe chega ao chao
            Esmaga();
            _pisando = false;
        }

        // A banana da vez vira casca. Ploft.
        private void Esmaga()
        {
            if (_bananas[0] == null || _vidasVisiveis <= 0) return;
            Transform b = _bananas[0];
            b.gameObject.SetActive(false);
            for (int i = 0; i < _bananas.Length - 1; i++) _bananas[i] = _bananas[i + 1];
            _bananas[_bananas.Length - 1] = b;
            _vidasVisiveis--;
            if (_casca != null)
            {
                _casca.localPosition = _lugarDoPe;
                _casca.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                _casca.localScale = Vector3.one * 1.5f; // nasce esparramada e assenta
                _casca.gameObject.SetActive(true);
            }
            SomRetro.Ruido(0.12f, 0.35f, 500f, "lowpass");
            SomRetro.Tom(190f, 0.14f, "sine", 0.22f, 70f);
        }

        private static UnityEngine.Mesh _malhaBanana;

        // Banana de poucos lados, em arco, deitada no chao. Feita por codigo ate o modelo 3D definitivo chegar.
        private static UnityEngine.Mesh MalhaBanana()
        {
            if (_malhaBanana != null) return _malhaBanana;
            const int aneis = 8, lados = 5;
            var vs = new System.Collections.Generic.List<Vector3>();
            var ts = new System.Collections.Generic.List<int>();
            for (int a = 0; a < aneis; a++)
            {
                float u = a / (float)(aneis - 1), ang = Mathf.Lerp(-1.0f, 1.0f, u);
                Vector3 centro = new Vector3(Mathf.Sin(ang) * 0.55f, 0.17f, (1f - Mathf.Cos(ang)) * 0.55f);
                Vector3 tang = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)), lado = new Vector3(-tang.z, 0f, tang.x);
                float r = 0.035f + 0.125f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.08f, 0.94f, u)), 0.6f);
                for (int l = 0; l < lados; l++)
                {
                    float t = l / (float)lados * Mathf.PI * 2f;
                    vs.Add(centro + (lado * Mathf.Cos(t) + Vector3.up * Mathf.Sin(t)) * r);
                }
            }
            for (int a = 0; a < aneis - 1; a++)
                for (int l = 0; l < lados; l++)
                {
                    int i0 = a * lados + l, i1 = a * lados + (l + 1) % lados, i2 = i0 + lados, i3 = i1 + lados;
                    ts.Add(i0); ts.Add(i2); ts.Add(i1); ts.Add(i1); ts.Add(i2); ts.Add(i3);
                }
            _malhaBanana = new UnityEngine.Mesh { name = "Banana de vida" };
            _malhaBanana.SetVertices(vs); _malhaBanana.SetTriangles(ts, 0);
            _malhaBanana.RecalculateNormals(); _malhaBanana.RecalculateBounds();
            return _malhaBanana;
        }

        private static Material Tinta(Color cor, bool doisLados = false)
        {
            var m = new Material(Shader.Find("BananaRoulette/Retro"));
            m.SetColor("_BaseColor", cor);
            if (doisLados) m.SetFloat("_Cull", 0f);
            return m;
        }

        private static Transform Bloco(Transform pai, Vector3 pos, Vector3 tamanho, float giroY, Material tinta)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(pai, false);
            go.transform.localPosition = pos; go.transform.localScale = tamanho; go.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
            go.GetComponent<Renderer>().sharedMaterial = tinta;
            return go.transform;
        }

        private void MontaVidas()
        {
            float esc = _visual.transform.localScale.x;
            Vector2 pe = MacacoVisual.Pisa;
            // a previa tem o X ao contrario da Unity
            _lugarDoPe = _visual.transform.localPosition + _visual.transform.localRotation * (new Vector3(-pe.x, 0f, pe.y) * esc);
            _passoDaFila = new Vector3(-0.62f, 0f, 0.05f) * (esc / 0.25f); // fila para o lado, para nenhuma ficar atras do braco
            float porte = 0.85f * (esc / 0.25f);

            Material amarelo = Tinta(new Color(0.96f, 0.80f, 0.22f), true), ponta = Tinta(new Color(0.30f, 0.20f, 0.10f));
            Material cascaFora = Tinta(new Color(0.90f, 0.72f, 0.20f)), cascaDentro = Tinta(new Color(0.97f, 0.92f, 0.70f));
            for (int i = 0; i < _bananas.Length; i++)
            {
                var go = new GameObject("Vida " + (i + 1), typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = MalhaBanana();
                go.GetComponent<Renderer>().sharedMaterial = amarelo;
                Bloco(go.transform, new Vector3(-0.47f, 0.17f, 0.25f), Vector3.one * 0.07f, 30f, ponta);
                Bloco(go.transform, new Vector3(0.49f, 0.2f, 0.28f), new Vector3(0.06f, 0.06f, 0.14f), -35f, ponta);
                go.transform.localPosition = _lugarDoPe + _passoDaFila * i - new Vector3(0f, 0f, 0.25f) * porte;
                go.transform.localRotation = Quaternion.Euler(0f, 20f + 25f * i, 0f);
                go.transform.localScale = Vector3.zero;
                go.AddComponent<Porte>().alvo = porte;
                _bananas[i] = go.transform;
            }

            var casca = new GameObject("Casca de banana");
            casca.transform.SetParent(transform, false);
            for (int k = 0; k < 4; k++)
            {
                float g = 90f * k + 20f, rad = g * Mathf.Deg2Rad;
                Bloco(casca.transform, new Vector3(Mathf.Cos(rad) * 0.2f, 0.03f, -Mathf.Sin(rad) * 0.2f) * porte, new Vector3(0.36f, 0.03f, 0.13f) * porte, g, k % 2 == 0 ? cascaFora : cascaDentro);
            }
            Bloco(casca.transform, new Vector3(0f, 0.05f, 0f), new Vector3(0.12f, 0.08f, 0.12f) * porte, 45f, ponta);
            casca.SetActive(false);
            _casca = casca.transform;

            // os poderes guardados ficam no chao atras dele, do lado direito, em fila para tras: fora do caminho dos bracos
            _lugarDoItem = new Vector3(1.45f, 0f, -0.7f) * (esc / 0.25f);
            _passoDoItem = new Vector3(0.45f, 0f, -0.85f) * (esc / 0.25f);
            _escalaDoItem = esc * 1.25f;
        }

        private Vector3 _lugarDoItem, _passoDoItem;
        private float _escalaDoItem = 0.2f;
        private readonly System.Collections.Generic.List<GameObject> _itens = new System.Collections.Generic.List<GameObject>();
        private int _bolsaMostrada;

        /// <summary>A folha de emojis deste macaco (rosto com a cor e os acessorios dele).</summary>
        public Texture2D Rosto { get { return _visual != null ? _visual.Rosto : null; } }

        /// <summary>Onde fica a banana de vida mais nova, no mundo.</summary>
        public Vector3 LugarDaVidaNova { get { return transform.TransformPoint(_lugarDoPe + _passoDaFila * Mathf.Max(0, Lives - 1)); } }

        /// <summary>O meio da cabeca, no mundo.</summary>
        public Vector3 Cabeca
        {
            get
            {
                if (_visual == null || _visual.Cabeca == null) return transform.position + Vector3.up * 2.6f;
                return _visual.Cabeca.position + Vector3.up * (2.4f * _visual.transform.lossyScale.x);
            }
        }

        /// <summary>Uma banana de vida solta (a mesma do chao), para o painel fotografar.</summary>
        public static GameObject NovaBanana()
        {
            var go = new GameObject("Banana", typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = MalhaBanana();
            go.GetComponent<Renderer>().sharedMaterial = Tinta(new Color(0.96f, 0.80f, 0.22f), true);
            Material ponta = Tinta(new Color(0.30f, 0.20f, 0.10f));
            Bloco(go.transform, new Vector3(-0.47f, 0.17f, 0.25f), Vector3.one * 0.07f, 30f, ponta);
            Bloco(go.transform, new Vector3(0.49f, 0.2f, 0.28f), new Vector3(0.06f, 0.06f, 0.14f), -35f, ponta);
            return go;
        }

        /// <summary>Onde o poder guardado deste macaco fica, no mundo.</summary>
        public Vector3 LugarDoItem { get { return transform.TransformPoint(_lugarDoItem); } }

        /// <summary>Faz o objeto crescer ate o tamanho certo quando aparece (a banana nova "brota").</summary>
        private class Porte : MonoBehaviour
        {
            public float alvo = 1f;
            private void OnEnable() { transform.localScale = Vector3.zero; }
            private void Update()
            {
                float s = Mathf.MoveTowards(transform.localScale.x, alvo, Time.deltaTime * alvo * 6f);
                transform.localScale = Vector3.one * s;
            }
        }

        private void PassoDasVidas()
        {
            if (_bananas[0] == null) return;
            if (!_pisando) _vidasVisiveis = Lives;
            for (int i = 0; i < _bananas.Length; i++)
            {
                bool liga = IsAlive && i < _vidasVisiveis;
                if (_bananas[i].gameObject.activeSelf != liga) _bananas[i].gameObject.SetActive(liga);
                if (liga) _bananas[i].localPosition = Vector3.Lerp(_bananas[i].localPosition, _lugarDoPe + _passoDaFila * i, Time.deltaTime * 6f);
            }
            if (_casca != null && _casca.gameObject.activeSelf)
                _casca.localScale = Vector3.Lerp(_casca.localScale, Vector3.one, Time.deltaTime * 10f);

            int marca = 0;
            if (IsAlive) { marca = Bolsa.Count; for (int i = 0; i < Bolsa.Count; i++) marca = marca * 16 + (int)Bolsa[i]; }
            if (marca == _bolsaMostrada) return;
            _bolsaMostrada = marca;
            foreach (GameObject velho in _itens) if (velho != null) Destroy(velho);
            _itens.Clear();
            if (!IsAlive) return;
            for (int i = 0; i < Bolsa.Count; i++)
            {
                GameObject it = ItemPoder.Criar(Bolsa[i], transform, _escalaDoItem, ItemPoder.Parte.SoObjeto);
                if (it == null) continue;
                it.transform.localPosition = _lugarDoItem + _passoDoItem * i;
                it.transform.localRotation = Quaternion.Euler(0f, -35f + 25f * i, 0f);
                it.AddComponent<Porte>().alvo = _escalaDoItem;
                _itens.Add(it);
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
            if (_visual != null) MontaVidas();
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
            PassoDasVidas();

            if (!IsAlive) return;

            if (_visual != null)
            {
                // Quanto mais curto o pavio, mais desesperado quem segura; os outros so se assustam no fim.
                _visual.Suor = _isHolding ? Tensao : 0f; // quem segura sua cada vez mais
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
            _pisando = false;
            if (_visual != null)
            {
                // a ultima banana e esmagada pela explosao; a casca fica no chao, ele vai embora voando
                _vidasVisiveis = Mathf.Max(1, _vidasVisiveis);
                Esmaga();
                _vidasVisiveis = 0;
                if (_casca != null) _casca.SetParent(null, true);
                _visual.Suor = 0f;
                StartCoroutine(VoaLonge());
                return;
            }
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
            _pisando = false;
            if (_visual != null) { _visual.Suor = 0f; _visual.Laco("vence"); return; }
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
