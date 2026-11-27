using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// A musica do jogo, fabricada por codigo: uma orquestrinha de desenho animado (flauta, clarinete,
    /// fagote, trompete, trombone, cordas beliscadas, cordas tremidas, timpano, caixa e bloco de madeira).
    /// Nao ha arquivo de audio. E rascunho: quando houver musica gravada, basta trocar esta classe.
    ///
    /// Uso:  MusicaRetro.Tocar(MusicaRetro.Clima.Jogo);   MusicaRetro.Perigo = 0.8f;
    ///
    /// Climas:
    ///   Menu    - o tema, devagar e leve (menu, opcoes, personagem)
    ///   Calmo   - macaco com fome e preguica: fagote arrastado, um pouco de misterio
    ///   Duvida  - flauta fazendo pergunta ("tana, tan?") e clarinete respondendo
    ///   Alegria - o tema, rapido, com trompete
    ///   Vilao   - marcha pesada em tom menor, trombone (o aviador)
    ///   Tensao  - cordas tremidas subindo, timpano e caixa (pavio aceso)
    ///   Jogo    - o tema; conforme Perigo sobe (pavio acabando) acelera e ganha timpano, caixa e cordas tremidas
    ///
    /// A troca de clima acontece na virada do compasso, para nao cortar a musica no meio.
    /// O volume segue Opcoes > Musica.
    /// </summary>
    public class MusicaRetro : MonoBehaviour
    {
        public enum Clima { Silencio, Menu, Calmo, Duvida, Alegria, Vilao, Tensao, Jogo }

        // tipos de instrumento
        const int Flauta = 0, Clarinete = 1, Trompete = 2, Trombone = 3, Beliscada = 4, Tremida = 5, Timpano = 6, Caixa = 7, Bloco = 8, Fagote = 9;

        static MusicaRetro eu;
        static volatile int pedido = (int)Clima.Silencio;
        static volatile float perigo, volume = 0.7f;

        /// <summary>De 0 (pavio inteiro) a 1 (pavio no fim). So muda a musica no clima Jogo.</summary>
        public static float Perigo { set { perigo = Mathf.Clamp01(value); } }

        public static void Tocar(Clima clima) { Garante(); pedido = (int)clima; }

        static volatile float ganhoAlvo = 1f, ganhoPorSegundo = 1f, abafaAlvo;

        /// <summary>Musica abafada e mais baixa (menu da partida aberto). Entra e sai aos poucos.</summary>
        public static void Abafar(bool sim) { abafaAlvo = sim ? 1f : 0f; }

        /// <summary>Leva o volume ate "alvo" (0 a 1) em "segundos". Usado nas trocas de tela, para nao haver corte.</summary>
        public static void Volume(float alvo, float segundos)
        {
            ganhoPorSegundo = 1f / Mathf.Max(0.05f, segundos);
            ganhoAlvo = Mathf.Clamp01(alvo);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Garante()
        {
            if (eu != null) return;
            var go = new GameObject("Musica");
            DontDestroyOnLoad(go); // atravessa a troca de cena sem cortar
            eu = go.AddComponent<MusicaRetro>();
            var fonte = go.AddComponent<AudioSource>();
            fonte.clip = AudioClip.Create("silencio", 4410, 1, 44100, false);
            fonte.loop = true; fonte.spatialBlend = 0f; fonte.volume = 1f;
            fonte.Play(); // o som de verdade e escrito em OnAudioFilterRead
        }

        void Awake() { taxa = AudioSettings.outputSampleRate; MontaPadroes(); }
        void Update() { volume = GameSettings.MusicVolume; }

        // ------------------------------------------------------------------ partitura

        class Trilha { public int[] notas; public int tipo; public float vol; public int desde; /* so toca com perigo >= desde/100 */ }
        class Padrao { public float bpm; public List<Trilha> trilhas = new List<Trilha>(); }

        Padrao[] padroes;

        static int Midi(string n)
        {
            int semi = "C D EF G A B".IndexOf(n[0]);
            int i = 1;
            if (n[i] == '#') { semi++; i++; } else if (n[i] == 'b') { semi--; i++; }
            return 12 * (int.Parse(n.Substring(i)) + 1) + semi;
        }

        // "E5 - G5 . | ..." -> numeros: nota (liga), 0 (segura), -1 (cala). As barras sao so para o olho.
        static int[] Le(string texto)
        {
            var saida = new List<int>();
            foreach (string t in texto.Split(new[] { ' ', '|', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                saida.Add(t == "-" ? 0 : t == "." ? -1 : t == "x" ? 60 : Midi(t));
            return saida.ToArray();
        }

        static void Add(Padrao p, int tipo, float vol, string notas, int desde = 0)
        {
            p.trilhas.Add(new Trilha { notas = Le(notas), tipo = tipo, vol = vol, desde = desde });
        }

        // acordes beliscados no contratempo (o "pa" do "um-pa"), um acorde por compasso
        static void Contratempo(Padrao p, float vol, params string[] acordes)
        {
            for (int v = 0; v < 3; v++)
            {
                string s = "";
                foreach (string a in acordes) { string n = a.Split(',')[v]; s += ". . " + n + " . . . " + n + " . | "; }
                Add(p, Beliscada, vol, s);
            }
        }

        const string TemaMel =
            "E5 - G5 E5 C5 - E5 - | D5 - F5 D5 B4 - G4 - | C5 E5 G5 E5 A5 - G5 - | F5 E5 D5 C5 D5 - . . | " +
            "E5 - G5 E5 C5 - E5 - | A5 - G5 F5 E5 - D5 - | C5 D5 E5 F5 G5 A5 B5 G5 | C6 - G5 - C5 - . .";
        const string TemaBaixo =
            "C2 . G2 . C2 . G2 . | G2 . D3 . G2 . D3 . | C2 . G2 . A2 . E3 . | D2 . A2 . G2 . G2 . | " +
            "C2 . G2 . C2 . G2 . | F2 . C3 . G2 . D3 . | C2 . G2 . C2 . G2 . | C2 . G2 . C2 . . .";
        static readonly string[] TemaAcordes = { "E4,G4,C5", "D4,G4,B4", "E4,G4,C5", "D4,F4,B4", "E4,G4,C5", "F4,A4,C5", "E4,G4,C5", "E4,G4,C5" };

        void MontaPadroes()
        {
            padroes = new Padrao[8];
            padroes[(int)Clima.Silencio] = new Padrao { bpm = 100f };

            // Menu: o tema, devagar, clarinete uma oitava abaixo
            var p = new Padrao { bpm = 98f };
            Add(p, Clarinete, 0.55f, TemaMel.Replace("4", "3").Replace("5", "4").Replace("6", "5"));
            Add(p, Beliscada, 0.6f, TemaBaixo);
            Contratempo(p, 0.22f, TemaAcordes);
            padroes[(int)Clima.Menu] = p;

            // Calmo: fome e preguica. Fagote arrastado, tuba marcando, flauta distante
            p = new Padrao { bpm = 84f };
            Add(p, Fagote, 0.7f, "C3 - . E3 - . G3 - | A3 - G3 - E3 - . . | D3 - . F3 - . A3 - | G3 - F3 - D3 - . . | C3 - . E3 - . G3 - | C4 - B3 - A3 - G3 - | F3 - E3 - D3 - B2 - | C3 - - - . . . .");
            Add(p, Trombone, 0.4f, "C2 - . . G1 - . . | C2 - . . G1 - . . | D2 - . . A1 - . . | G1 - . . D2 - . . | C2 - . . G1 - . . | F2 - . . C2 - . . | G1 - . . G1 - . . | C2 - . . . . . .");
            Contratempo(p, 0.16f, "E4,G4,C5", "E4,G4,C5", "D4,F4,A4", "D4,G4,B4", "E4,G4,C5", "F4,A4,C5", "D4,G4,B4", "E4,G4,C5");
            Add(p, Flauta, 0.22f, ". . . . . . . . | . . . . . . G5 E5 | . . . . . . . . | . . . . . . A5 F5 | . . . . . . . . | . . . . . . . . | . . . . . . . . | . . . . G5 . C6 .");
            padroes[(int)Clima.Calmo] = p;

            // Duvida: a flauta pergunta, o clarinete responde
            p = new Padrao { bpm = 104f };
            Add(p, Flauta, 0.7f, "G5 A5 . . C6 - . . | . . . . . . . . | G5 A5 . . D6 - . . | . . . . . . . . | G5 A5 B5 . E6 - . . | . . . . . . . . | E6 D6 C6 B5 A5 G5 A5 B5 | C6 - . . . . . .");
            Add(p, Clarinete, 0.5f, ". . . . . . . . | . . E4 . G4 . C5 . | . . . . . . . . | . . F4 . A4 . D5 . | . . . . . . . . | . . G4 . B4 . E5 . | . . . . . . . . | . . . . G4 . C4 .");
            Add(p, Beliscada, 0.55f, "C3 . . . G2 . . . | C3 . . . G2 . . . | D3 . . . G2 . . . | D3 . . . G2 . . . | E3 . . . G2 . . . | E3 . . . G2 . . . | F3 . . . G2 . . . | C3 . . . . . . .");
            Add(p, Bloco, 0.2f, ". . . . x . . . | . . . . . . . . | . . . . x . . . | . . . . . . . . | . . . . x . . . | . . . . . . . . | . . . . . . . . | . . . . x . x .");
            padroes[(int)Clima.Duvida] = p;

            // Alegria: o tema, rapido, com trompete e caixa
            p = new Padrao { bpm = 146f };
            Add(p, Trompete, 0.55f, TemaMel);
            Add(p, Flauta, 0.3f, TemaMel.Replace("6", "7").Replace("5", "6").Replace("4", "5"));
            Add(p, Beliscada, 0.65f, TemaBaixo);
            Contratempo(p, 0.25f, TemaAcordes);
            Add(p, Caixa, 0.16f, ". . x . . . x x | . . x . . . x x");
            padroes[(int)Clima.Alegria] = p;

            // Vilao: marcha em tom menor, trombone e tuba
            p = new Padrao { bpm = 92f };
            Add(p, Trombone, 0.75f, "C3 - . C3 Eb3 - D3 - | C3 - . G2 Ab2 - G2 - | C3 - . C3 Eb3 - F3 - | G3 - F#3 - G3 - . .");
            Add(p, Trombone, 0.5f, "C2 . G1 . C2 . G1 . | C2 . G1 . Ab1 . G1 . | C2 . G1 . C2 . F1 . | G1 . G1 . G1 . . .");
            Contratempo(p, 0.2f, "Eb4,G4,C5", "Eb4,G4,C5", "Eb4,G4,C5", "D4,G4,B4");
            Add(p, Caixa, 0.2f, "x . . x x . x . | x . . x x . x x");
            Add(p, Timpano, 0.5f, "C2 . . . . . . . | . . . . G1 . . . | C2 . . . . . . . | G1 . . . G1 . G1 .");
            padroes[(int)Clima.Vilao] = p;

            // Tensao: cordas tremidas subindo e descendo, timpano, caixa em rufo
            p = new Padrao { bpm = 154f };
            Add(p, Tremida, 0.5f, "C4 - Eb4 - F#4 - A4 - | C5 - A4 - F#4 - Eb4 - | C4 - Eb4 - F#4 - A4 - | C5 - Eb5 - F#5 - A5 -");
            Add(p, Tremida, 0.35f, "F#3 - - - - - - - | A3 - - - - - - - | F#3 - - - - - - - | C4 - - - - - - -");
            Add(p, Timpano, 0.7f, "C2 . . . C2 . C2 . | C2 . . . C2 . C2 . | C2 . . . C2 . C2 . | C2 . C2 . C2 C2 C2 C2");
            Add(p, Trompete, 0.4f, ". . . . . . C5 . | . . . . . . Eb5 . | . . . . . . F#5 . | . . . . A5 - - -");
            Add(p, Caixa, 0.14f, "x x x x x x x x");
            padroes[(int)Clima.Tensao] = p;

            // Jogo: o tema; com o pavio acabando entram timpano, caixa e cordas tremidas (e o andamento sobe)
            // E o meio termo entre a tensao do fim da abertura e a brincadeira do tema: a mesma melodia, em tom
            // menor, com as cordas graves tremendo por baixo e o timpano marcando desde o comeco.
            p = new Padrao { bpm = 130f };
            string menor = TemaMel.Replace("E", "Eb").Replace("A", "Ab");
            Add(p, Flauta, 0.5f, menor);
            Add(p, Clarinete, 0.32f, menor.Replace("4", "3").Replace("5", "4").Replace("6", "5"));
            Add(p, Beliscada, 0.65f, TemaBaixo.Replace("E", "Eb").Replace("A", "Ab"));
            Contratempo(p, 0.22f, "Eb4,G4,C5", "D4,G4,B4", "Eb4,G4,C5", "D4,F4,B4", "Eb4,G4,C5", "F4,Ab4,C5", "Eb4,G4,C5", "Eb4,G4,C5");
            Add(p, Bloco, 0.14f, "x . . . x . . .");
            Add(p, Tremida, 0.16f, "C3 - - - - - - - | B2 - - - - - - - | C3 - - - - - - - | D3 - - - G2 - - -");
            Add(p, Timpano, 0.38f, "C2 . . . . . . . | G1 . . . . . . .");
            Add(p, Timpano, 0.6f, ". . . . G1 . . .", 45);
            Add(p, Caixa, 0.15f, ". . x . . . x x", 60);
            Add(p, Tremida, 0.32f, "G4 - - - - - - - | Ab4 - - - - - - - | G4 - - - - - - - | B4 - - - - - - -", 70);
            Add(p, Caixa, 0.12f, "x x x x x x x x", 88);
            padroes[(int)Clima.Jogo] = p;
        }

        // ------------------------------------------------------------------ orquestra (roda na linha do audio)

        struct Voz { public double fase; public float freq, env, vol, t, filtro, dur; public int tipo; public bool ligada; }

        int taxa = 44100;
        int climaAtual = -1, passo;
        double ateOPasso;
        Voz[] vozes = new Voz[0];
        float ganho = 1f, abafa, filtroAbafa;
        uint sorte = 22222;

        float Ruido() { sorte = sorte * 1664525u + 1013904223u; return (sorte >> 9) / 4194304f - 1f; }

        void TrocaClima(int novo)
        {
            climaAtual = novo; passo = 0;
            var v = new Voz[padroes[novo].trilhas.Count];
            for (int i = 0; i < v.Length; i++) v[i].tipo = padroes[novo].trilhas[i].tipo;
            vozes = v;
        }

        void OnAudioFilterRead(float[] dados, int canais)
        {
            if (padroes == null) return;
            if (climaAtual < 0) TrocaClima(pedido);
            float dt = 1f / taxa;

            for (int i = 0; i < dados.Length; i += canais)
            {
                Padrao p = padroes[climaAtual];
                float risco = climaAtual == (int)Clima.Jogo ? perigo : 0f;

                if (ateOPasso <= 0.0)
                {
                    // na virada do compasso, troca de clima se pediram outro
                    if (pedido != climaAtual && passo % 8 == 0) { TrocaClima(pedido); p = padroes[climaAtual]; }
                    for (int t = 0; t < p.trilhas.Count; t++)
                    {
                        Trilha tr = p.trilhas[t];
                        int n = tr.notas[passo % tr.notas.Length];
                        if (n == 0) continue;
                        if (n < 0 || risco * 100f < tr.desde) { vozes[t].ligada = false; continue; }
                        vozes[t].freq = 440f * Mathf.Pow(2f, (n - 69) / 12f);
                        vozes[t].ligada = true; vozes[t].t = 0f; vozes[t].vol = tr.vol;
                        if (tr.tipo >= Beliscada && tr.tipo != Tremida && tr.tipo != Fagote) vozes[t].env = 1f; // percussivos comecam cheios
                    }
                    passo++;
                    float bpm = p.bpm * (1f + 0.28f * Mathf.Clamp01((risco - 0.4f) / 0.6f));
                    ateOPasso += taxa * 30.0 / bpm; // um passo = uma colcheia
                }
                ateOPasso -= 1.0;

                float soma = 0f;
                for (int v = 0; v < vozes.Length; v++) soma += Amostra(ref vozes[v], dt);
                soma *= 0.2f * volume;
                soma = soma / (1f + Mathf.Abs(soma)); // segura o pico sem estalar

                // volume e abafado andam devagar ate o alvo, amostra por amostra: nada de degrau no som
                ganho = Mathf.MoveTowards(ganho, ganhoAlvo, ganhoPorSegundo * dt);
                abafa += (abafaAlvo - abafa) * 0.0003f;
                filtroAbafa += (soma - filtroAbafa) * 0.05f;
                soma = Mathf.Lerp(soma, filtroAbafa * 1.25f, abafa) * ganho * (1f - 0.4f * abafa);

                for (int c = 0; c < canais; c++) dados[i + c] += soma;
            }
        }

        float Amostra(ref Voz v, float dt)
        {
            if (v.env <= 0.0005f && !v.ligada) return 0f;
            v.t += dt;
            float f = v.freq, s;
            float sobe = 0.02f, desce = 0.09f;

            switch (v.tipo)
            {
                case Flauta:
                    if (v.t > 0.12f) f *= 1f + 0.006f * Mathf.Sin(v.t * 34f);
                    v.fase += f * dt; v.fase -= Math.Floor(v.fase);
                    { float a = (float)v.fase * 6.2832f; s = Mathf.Sin(a) + 0.24f * Mathf.Sin(2f * a) + 0.07f * Mathf.Sin(3f * a) + 0.03f * Ruido(); }
                    sobe = 0.03f; break;
                case Clarinete:
                case Fagote:
                    v.fase += f * dt; v.fase -= Math.Floor(v.fase);
                    { float a = (float)v.fase * 6.2832f; s = Mathf.Sin(a) + 0.5f * Mathf.Sin(3f * a) + 0.22f * Mathf.Sin(5f * a) + (v.tipo == Fagote ? 0.14f * Mathf.Sin(7f * a) : 0f); s *= 0.8f; }
                    sobe = v.tipo == Fagote ? 0.035f : 0.02f; break;
                case Trompete:
                case Trombone:
                    if (v.t > 0.15f) f *= 1f + 0.004f * Mathf.Sin(v.t * 32f);
                    v.fase += f * dt; v.fase -= Math.Floor(v.fase);
                    {
                        float serra = 2f * (float)v.fase - 1f;
                        // filtro que abre no ataque: e o "pa" do metal
                        float corte = (v.tipo == Trompete ? 0.16f : 0.07f) + 0.25f * Mathf.Exp(-v.t * 14f);
                        v.filtro += (serra - v.filtro) * Mathf.Min(1f, corte);
                        s = v.filtro * 1.5f;
                    }
                    sobe = 0.018f; desce = 0.07f; break;
                case Tremida:
                    v.fase += f * dt; v.fase -= Math.Floor(v.fase);
                    {
                        float serra = 2f * (float)v.fase - 1f + 0.5f * (2f * (float)((v.fase * 1.007) % 1.0) - 1f);
                        v.filtro += (serra - v.filtro) * 0.12f;
                        s = v.filtro * 1.3f * (0.55f + 0.45f * Mathf.Sign(Mathf.Sin(v.t * 88f)));
                    }
                    sobe = 0.04f; desce = 0.12f; break;
                case Beliscada:
                    v.fase += f * dt; v.fase -= Math.Floor(v.fase);
                    { float p = (float)v.fase; float tri = 1f - 4f * Mathf.Abs(p - 0.5f); v.filtro += ((2f * p - 1f) * 0.5f + tri - v.filtro) * 0.2f; s = v.filtro * 1.2f; }
                    v.env *= Mathf.Exp(-dt * (f < 140f ? 5f : 8.5f));
                    return s * v.env * v.vol;
                case Timpano:
                    v.fase += f * (1f + 0.6f * Mathf.Exp(-v.t * 40f)) * dt; v.fase -= Math.Floor(v.fase);
                    s = Mathf.Sin((float)v.fase * 6.2832f) + 0.25f * Ruido() * Mathf.Exp(-v.t * 30f);
                    v.env *= Mathf.Exp(-dt * 4.5f);
                    return s * v.env * v.vol;
                case Caixa:
                    v.filtro += (Ruido() - v.filtro) * 0.7f;
                    v.env *= Mathf.Exp(-dt * 34f);
                    return v.filtro * v.env * v.vol;
                default: // Bloco de madeira
                    v.fase += 880.0 * dt; v.fase -= Math.Floor(v.fase);
                    v.env *= Mathf.Exp(-dt * 42f);
                    return Mathf.Sin((float)v.fase * 6.2832f) * v.env * v.vol;
            }

            // instrumentos de sopro e cordas tremidas: sobem enquanto a nota esta ligada, caem quando cala
            if (v.ligada) v.env = Mathf.Min(1f, v.env + dt / sobe);
            else v.env = Mathf.Max(0f, v.env - dt / desce);
            return s * v.env * v.vol;
        }
    }
}
