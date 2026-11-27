using System;
using System.Collections;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// A cena de cada poder: o que se VE quando alguem usa. O EFEITO no jogo ja aconteceu antes (GameManager.UsarPowerUp
    /// aplica na hora em que a tecla e apertada); aqui e so imagem e som por cima, e nada espera a cena acabar.
    ///
    ///   vida extra          a banana pula para a fila de vidas
    ///   pavio molhado       o cantil vira em cima da bomba e pinga agua
    ///   meia sequencia      o alicate corta a cinta e tres bananas de dinamite saltam do maco, que encolhe
    ///   passe relampago     o estilingue estica e estala
    ///   comandos invertidos o coco acerta a cabeca do outro: estrelas girando enquanto ele estiver com a bomba
    ///   sequencia dobrada   outro maco cai na mao dele e fica la, do lado da bomba
    ///   pavio curto         a tesoura voa, corta o pavio e um pedaco cai
    ///   nevoa               o saco de terra estoura na cara dele: nuvem de poeira enquanto ele estiver com a bomba
    ///   cola                o frasco estoura nas maos dele: uma bolha branca gruda nelas por dois segundos
    /// </summary>
    public static class CenasDePoder
    {
        static Shader tintaRetro;

        static GameObject Peca(PrimitiveType forma, Color cor, Vector3 tamanho)
        {
            var go = GameObject.CreatePrimitive(forma);
            go.name = "Efeito de poder";
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.localScale = tamanho;
            if (tintaRetro == null) tintaRetro = Shader.Find("BananaRoulette/Retro");
            var m = new Material(tintaRetro);
            m.SetColor("_BaseColor", cor); m.SetFloat("_SemLuz", 1f);
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        // Leva um objeto em arco ate um ponto que pode estar se mexendo (a mao de quem faz malabarismo, por exemplo).
        static IEnumerator Voa(GameObject o, Vector3 de, Func<Vector3> ate, float dur, float altura, float giro)
        {
            for (float t = 0f; t < 1f && o != null; t += Time.deltaTime / dur)
            {
                Vector3 q = Vector3.Lerp(de, ate(), t); q.y += Mathf.Sin(t * Mathf.PI) * altura;
                o.transform.position = q;
                o.transform.Rotate(0f, giro * Time.deltaTime, giro * 0.4f * Time.deltaTime);
                yield return null;
            }
            if (o != null) o.transform.position = ate();
        }

        // Uma peca pequena que salta, cai e some.
        static IEnumerator Salta(GameObject o, Vector3 de, Vector3 vel, float vida)
        {
            Vector3 p = de, s0 = o.transform.localScale;
            for (float t = 0f; t < vida && o != null; t += Time.deltaTime)
            {
                vel.y -= 14f * Time.deltaTime; p += vel * Time.deltaTime;
                o.transform.position = p;
                o.transform.Rotate(400f * Time.deltaTime, 300f * Time.deltaTime, 0f);
                o.transform.localScale = s0 * Mathf.Clamp01((vida - t) / (vida * 0.4f));
                yield return null;
            }
            if (o != null) UnityEngine.Object.Destroy(o);
        }

        public static IEnumerator Tocar(MonoBehaviour dono, PowerUp p, MonkeySlot quem, MonkeySlot alvo, BananaCarrier bomba, CameraShake tremor)
        {
            float esc = quem.Visual != null ? quem.Visual.transform.lossyScale.x : 0.25f;
            bool ataque = PowerUps.EhAtaque(p);
            GameObject item = ItemPoder.Criar(p, null, esc * 1.3f, ItemPoder.Parte.SoObjeto);
            if (item == null) yield break;
            Vector3 de = quem.LugarDoItem + Vector3.up * 0.3f;
            item.transform.position = de;
            Func<Vector3> naBomba = () => bomba != null ? bomba.transform.position : alvo.Cabeca;
            Func<Vector3> naCabeca = () => alvo.Cabeca;

            if (ataque && !quem.Holding) quem.Arremessar(alvo.transform.position);

            switch (p)
            {
                case PowerUp.VidaExtra:
                {
                    yield return Voa(item, de, () => quem.LugarDaVidaNova + Vector3.up * 0.2f, 0.4f, 2.2f, 500f);
                    SomRetro.Tom(880f, 0.08f, "square", 0.08f); SomRetro.Tom(1320f, 0.14f, "square", 0.08f, 0f, 0.09f);
                    UnityEngine.Object.Destroy(item);
                    break;
                }
                case PowerUp.PavioMolhado:
                {
                    Func<Vector3> acima = () => naBomba() + Vector3.up * (5f * esc);
                    yield return Voa(item, de, acima, 0.3f, 1.2f, 300f);
                    SomRetro.Ruido(0.9f, 0.12f, 1800f, "bandpass");
                    float proxima = 0f;
                    for (float t = 0f; t < 0.9f && item != null; t += Time.deltaTime)
                    {
                        item.transform.position = acima();
                        item.transform.rotation = Quaternion.Euler(0f, 0f, -Mathf.Lerp(0f, 115f, t / 0.2f));
                        if (t >= proxima)
                        {
                            proxima = t + 0.06f;
                            var gota = Peca(PrimitiveType.Sphere, new Color(0.45f, 0.78f, 0.93f), new Vector3(0.16f, 0.26f, 0.16f) * (esc * 4f));
                            Vector3 pinga = acima() + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), -0.2f, UnityEngine.Random.Range(-0.2f, 0.2f));
                            dono.StartCoroutine(Salta(gota, pinga, new Vector3(0f, -1f, 0f), 0.35f));
                        }
                        yield return null;
                    }
                    UnityEngine.Object.Destroy(item);
                    break;
                }
                case PowerUp.MeiaSequencia:
                {
                    yield return Voa(item, de, naBomba, 0.22f, 0.8f, 700f);
                    SomRetro.Ruido(0.05f, 0.25f, 5000f, "highpass"); SomRetro.Ruido(0.05f, 0.25f, 5000f, "highpass", 0.1f);
                    for (int i = 0; i < 3; i++)
                    {
                        var pau = Peca(PrimitiveType.Cylinder, new Color(0.90f, 0.76f, 0.30f), new Vector3(0.22f, 0.55f, 0.22f) * (esc * 4f));
                        pau.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                        float a = (i - 1) * 1.1f + UnityEngine.Random.Range(-0.3f, 0.3f);
                        dono.StartCoroutine(Salta(pau, naBomba(), new Vector3(Mathf.Sin(a) * 3f, 5f, Mathf.Cos(a) * 1.5f - 1f), 0.8f));
                    }
                    UnityEngine.Object.Destroy(item);
                    // o maco fica mais magro enquanto estiver com ele
                    if (bomba != null)
                    {
                        Vector3 s0 = bomba.transform.localScale;
                        bomba.transform.localScale = s0 * 0.78f;
                        for (float t = 0f; t < 12f && quem.Holding; t += Time.deltaTime) yield return null;
                        if (bomba != null) bomba.transform.localScale = s0;
                    }
                    break;
                }
                case PowerUp.PasseRelampago:
                {
                    Vector3 onde = naBomba() + Vector3.up * (2f * esc);
                    item.transform.position = onde;
                    SomRetro.Tom(300f, 0.18f, "sawtooth", 0.1f, 1400f); SomRetro.Ruido(0.12f, 0.2f, 2500f, "bandpass", 0.16f);
                    Vector3 s0 = item.transform.localScale;
                    for (float t = 0f; t < 0.4f && item != null; t += Time.deltaTime)
                    {
                        float k = t / 0.4f;
                        item.transform.localScale = new Vector3(s0.x, s0.y, s0.z * (1f + 0.8f * Mathf.Sin(k * Mathf.PI)));
                        item.transform.position = onde + Vector3.up * (k * 1.2f);
                        yield return null;
                    }
                    UnityEngine.Object.Destroy(item);
                    break;
                }
                case PowerUp.Invertido:
                {
                    yield return Voa(item, de, naCabeca, 0.3f, 1.8f, 600f);
                    SomRetro.Tom(190f, 0.16f, "sine", 0.35f, 80f); SomRetro.Ruido(0.08f, 0.3f, 900f, "bandpass");
                    if (tremor != null) tremor.Shake(0.12f, 0.12f);
                    if (alvo.Visual != null) alvo.Visual.Piscar(Color.white);
                    dono.StartCoroutine(Salta(item, alvo.Cabeca, (alvo.Cabeca - de).normalized * -2.5f + Vector3.up * 5f, 0.7f));
                    // estrelas de tontura girando em volta da cabeca enquanto ele estiver com a bomba
                    var estrelas = new GameObject[3];
                    for (int i = 0; i < 3; i++) estrelas[i] = Peca(PrimitiveType.Cube, new Color(1f, 0.86f, 0.25f), Vector3.one * (0.9f * esc));
                    for (float t = 0f; t < 10f && alvo.Holding && alvo.IsAlive; t += Time.deltaTime)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float a = t * 5f + i * 2.094f;
                            estrelas[i].transform.position = alvo.Cabeca + new Vector3(Mathf.Cos(a) * 3.4f * esc, 3.4f * esc + Mathf.Sin(a * 2f) * 0.5f * esc, Mathf.Sin(a) * 3.4f * esc);
                            estrelas[i].transform.rotation = Quaternion.Euler(45f, t * 400f, 45f);
                        }
                        yield return null;
                    }
                    foreach (var e in estrelas) UnityEngine.Object.Destroy(e);
                    break;
                }
                case PowerUp.Dobrada:
                {
                    yield return Voa(item, de, naBomba, 0.35f, 2.4f, 500f);
                    SomRetro.Tom(140f, 0.12f, "sine", 0.3f, 90f);
                    // a segunda bomba fica na mao dele, ao lado da primeira
                    for (float t = 0f; t < 12f && alvo.Holding && alvo.IsAlive && item != null; t += Time.deltaTime)
                    {
                        Vector3 lado = bomba != null ? bomba.transform.right : Vector3.right;
                        item.transform.position = naBomba() + lado * (3.2f * esc) + Vector3.up * (0.6f * esc * Mathf.Sin(t * 9f));
                        if (bomba != null) item.transform.rotation = bomba.transform.rotation;
                        yield return null;
                    }
                    if (item != null) UnityEngine.Object.Destroy(item);
                    break;
                }
                case PowerUp.PavioCurto:
                {
                    yield return Voa(item, de, () => naBomba() + Vector3.up * (2.5f * esc), 0.26f, 1.6f, 1200f);
                    SomRetro.Ruido(0.04f, 0.3f, 6000f, "highpass"); SomRetro.Ruido(0.04f, 0.3f, 6000f, "highpass", 0.08f);
                    var pedaco = Peca(PrimitiveType.Cylinder, new Color(0.35f, 0.23f, 0.11f), new Vector3(0.1f, 0.4f, 0.1f) * (esc * 4f));
                    dono.StartCoroutine(Salta(pedaco, naBomba() + Vector3.up * (2f * esc), new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), 3f, -1f), 0.7f));
                    for (int i = 0; i < 4; i++)
                    {
                        var faisca = Peca(PrimitiveType.Cube, new Color(0.95f, 0.54f, 0.11f), Vector3.one * (0.5f * esc));
                        dono.StartCoroutine(Salta(faisca, naBomba() + Vector3.up * (2f * esc), UnityEngine.Random.onUnitSphere * 2.5f + Vector3.up * 2.5f, 0.45f));
                    }
                    dono.StartCoroutine(Salta(item, naBomba() + Vector3.up * (2.5f * esc), new Vector3(0f, 4f, -2f), 0.6f));
                    break;
                }
                case PowerUp.Nevoa:
                {
                    yield return Voa(item, de, naCabeca, 0.3f, 1.8f, 500f);
                    SomRetro.Ruido(0.5f, 0.3f, 400f, "lowpass");
                    UnityEngine.Object.Destroy(item);
                    // nuvem de poeira na frente do rosto enquanto ele estiver com a bomba
                    const int n = 8;
                    var nuvens = new GameObject[n]; var desvio = new Vector3[n];
                    for (int i = 0; i < n; i++)
                    {
                        float tom = UnityEngine.Random.Range(0.55f, 0.75f);
                        nuvens[i] = Peca(PrimitiveType.Sphere, new Color(tom, tom * 0.86f, tom * 0.68f), Vector3.one * 0.1f);
                        desvio[i] = new Vector3(UnityEngine.Random.Range(-3.4f, 3.4f), UnityEngine.Random.Range(-1.6f, 2.6f), UnityEngine.Random.Range(1.5f, 3.6f)) * esc;
                    }
                    float fim = 9f;
                    for (float t = 0f; t < fim; t += Time.deltaTime)
                    {
                        if ((!alvo.Holding || !alvo.IsAlive) && fim > t + 0.4f) fim = t + 0.4f; // largou a bomba: a poeira assenta
                        float porte = Mathf.Min(1f, t / 0.25f) * Mathf.Clamp01((fim - t) / 0.4f);
                        Quaternion giro = alvo.transform.rotation;
                        for (int i = 0; i < n; i++)
                        {
                            nuvens[i].transform.position = alvo.Cabeca + giro * desvio[i] + new Vector3(0f, Mathf.Sin(t * 2f + i) * 0.12f, 0f);
                            nuvens[i].transform.localScale = Vector3.one * ((4.2f + (i % 3) * 1.1f) * esc * porte);
                        }
                        yield return null;
                    }
                    foreach (var nv in nuvens) UnityEngine.Object.Destroy(nv);
                    break;
                }
                case PowerUp.Cola:
                {
                    yield return Voa(item, de, naBomba, 0.3f, 2f, 500f);
                    SomRetro.Ruido(0.25f, 0.3f, 600f, "lowpass"); SomRetro.Tom(260f, 0.2f, "sine", 0.15f, 120f);
                    UnityEngine.Object.Destroy(item);
                    var bolha = Peca(PrimitiveType.Sphere, new Color(0.96f, 0.93f, 0.86f), Vector3.one * 0.1f);
                    for (int i = 0; i < 5; i++)
                    {
                        var pingo = Peca(PrimitiveType.Sphere, new Color(0.96f, 0.93f, 0.86f), Vector3.one * (0.8f * esc));
                        dono.StartCoroutine(Salta(pingo, naBomba(), UnityEngine.Random.onUnitSphere * 2f + Vector3.up * 2f, 0.6f));
                    }
                    for (float t = 0f; t < 2f; t += Time.deltaTime)
                    {
                        float porte = Mathf.Min(1f, t / 0.12f) * Mathf.Clamp01((2f - t) / 0.3f);
                        bolha.transform.position = naBomba() - Vector3.up * (1.2f * esc);
                        bolha.transform.localScale = new Vector3(7.5f, 3.2f + 0.4f * Mathf.Sin(t * 12f), 6f) * (esc * porte);
                        yield return null;
                    }
                    UnityEngine.Object.Destroy(bolha);
                    break;
                }
                default:
                    UnityEngine.Object.Destroy(item);
                    break;
            }
        }
    }
}
