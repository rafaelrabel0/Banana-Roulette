using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Som de rascunho, fabricado na hora (nenhum arquivo de audio): tom, ruido e grito de macaco.
    /// E o mesmo som da previa em three.js. Quando o som de verdade entrar, e so trocar as chamadas.
    ///
    /// Uso: SomRetro.Tom(440, 0.2f, "square", 0.1f);  SomRetro.Ruido(0.3f, 0.2f, 900, "bandpass");
    /// O volume segue o ajuste de som do jogo (GameSettings mexe no AudioListener).
    /// </summary>
    public class SomRetro : MonoBehaviour
    {
        const int Taxa = 44100;
        static SomRetro eu;
        static readonly Dictionary<string, AudioClip> feitos = new Dictionary<string, AudioClip>();

        AudioSource fonte;
        readonly List<KeyValuePair<float, AudioClip>> fila = new List<KeyValuePair<float, AudioClip>>();

        static SomRetro Eu
        {
            get
            {
                if (eu == null)
                {
                    var go = new GameObject("Som retro");
                    eu = go.AddComponent<SomRetro>();
                    eu.fonte = go.AddComponent<AudioSource>();
                    eu.fonte.playOnAwake = false;
                    eu.fonte.spatialBlend = 0f;
                    eu.fonte.volume = 0.7f;
                }
                return eu;
            }
        }

        void Update()
        {
            for (int i = fila.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < fila[i].Key) continue;
                if (fila[i].Value != null) fonte.PlayOneShot(fila[i].Value);
                fila.RemoveAt(i);
            }
        }

        static void Toca(string chave, Func<float[]> faz, float atraso)
        {
            AudioClip clip;
            if (!feitos.TryGetValue(chave, out clip) || clip == null)
            {
                float[] a = faz();
                clip = AudioClip.Create(chave, a.Length, 1, Taxa, false);
                clip.SetData(a, 0);
                feitos[chave] = clip;
            }
            if (atraso <= 0f) Eu.fonte.PlayOneShot(clip);
            else Eu.fila.Add(new KeyValuePair<float, AudioClip>(Time.unscaledTime + atraso, clip));
        }

        /// <summary>Tom simples. tipo: sine, square, sawtooth ou triangle. f2 maior que zero faz a nota escorregar ate ele.</summary>
        public static void Tom(float f, float d, string tipo, float v, float f2 = 0f, float atraso = 0f)
        {
            Toca("tom" + f + "_" + d + tipo + v + "_" + f2, () =>
            {
                int n = Mathf.CeilToInt(Taxa * d);
                var a = new float[n];
                double fase = 0;
                for (int i = 0; i < n; i++)
                {
                    float k = (float)i / n;
                    float fr = f2 > 0f ? f * Mathf.Pow(f2 / f, k) : f;
                    fase += fr / Taxa;
                    a[i] = Onda(tipo, (float)(fase % 1.0)) * v * Mathf.Pow(0.001f / v, k);
                }
                return a;
            }, atraso);
        }

        /// <summary>Chiado filtrado. tipo: lowpass, highpass ou bandpass. fc = frequencia do filtro.</summary>
        public static void Ruido(float d, float v, float fc, string tipo, float atraso = 0f)
        {
            Toca("ruido" + d + "_" + v + "_" + fc + tipo, () =>
            {
                int n = Mathf.CeilToInt(Taxa * d);
                var a = new float[n];
                var sorte = new System.Random(12345);
                var filtro = new Filtro(tipo, fc, tipo == "bandpass" ? 1f : 0.707f);
                for (int i = 0; i < n; i++)
                {
                    float k = (float)i / n;
                    a[i] = filtro.Passa((float)(sorte.NextDouble() * 2.0 - 1.0)) * v * Mathf.Pow(0.001f / v, k);
                }
                return a;
            }, atraso);
        }

        /// <summary>Grito de macaco: serra que sobe de f1 a f2, passada por um filtro de voz.</summary>
        public static void Grito(float f1, float f2, float d, float v, float atraso = 0f)
        {
            Toca("grito" + f1 + "_" + f2 + "_" + d + "_" + v, () =>
            {
                int n = Mathf.CeilToInt(Taxa * d);
                var a = new float[n];
                var filtro = new Filtro("bandpass", 1100f, 3f);
                double fase = 0;
                int ataque = Mathf.Max(1, Mathf.CeilToInt(Taxa * 0.02f));
                for (int i = 0; i < n; i++)
                {
                    float k = (float)i / n;
                    fase += f1 * Mathf.Pow(f2 / f1, k) / Taxa;
                    float g = i < ataque
                        ? 0.001f * Mathf.Pow(v / 0.001f, (float)i / ataque)
                        : v * Mathf.Pow(0.001f / v, (float)(i - ataque) / Mathf.Max(1, n - ataque));
                    a[i] = filtro.Passa(Onda("sawtooth", (float)(fase % 1.0))) * g * 2.2f;
                }
                return a;
            }, atraso);
        }

        static float Onda(string tipo, float p)
        {
            switch (tipo)
            {
                case "square": return p < 0.5f ? 1f : -1f;
                case "sawtooth": return 2f * p - 1f;
                case "triangle": return 1f - 4f * Mathf.Abs(p - 0.5f);
                default: return Mathf.Sin(p * 2f * Mathf.PI);
            }
        }

        // Filtro de segunda ordem (formulas classicas do "Audio EQ Cookbook").
        struct Filtro
        {
            float b0, b1, b2, a1, a2, x1, x2, y1, y2;

            public Filtro(string tipo, float fc, float q)
            {
                float w = 2f * Mathf.PI * Mathf.Min(fc, Taxa * 0.45f) / Taxa;
                float c = Mathf.Cos(w), s = Mathf.Sin(w), alfa = s / (2f * q);
                float a0 = 1f + alfa;
                if (tipo == "lowpass") { b0 = (1f - c) / 2f; b1 = 1f - c; b2 = b0; }
                else if (tipo == "highpass") { b0 = (1f + c) / 2f; b1 = -(1f + c); b2 = b0; }
                else { b0 = alfa; b1 = 0f; b2 = -alfa; }
                b0 /= a0; b1 /= a0; b2 /= a0;
                a1 = -2f * c / a0; a2 = (1f - alfa) / a0;
                x1 = x2 = y1 = y2 = 0f;
            }

            public float Passa(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }
    }
}
