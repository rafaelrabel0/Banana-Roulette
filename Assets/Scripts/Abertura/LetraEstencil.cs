using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// A letra de estencil pintada nos caixotes (Stardos Stencil), para escrever texto vivo em 3D.
    ///
    /// A letra vem como imagem: Resources/Abertura/letra-estencil.png (todas as letras, brancas)
    /// e letra-estencil.json (onde cada letra esta na imagem). Foram exportadas da previa.
    /// Aqui cada linha de texto vira uma malha de retangulos, um por letra.
    /// </summary>
    public static class LetraEstencil
    {
        [Serializable] class Glifo { public int c; public float x, y, w, a; }
        [Serializable] class Dados { public float tamanho = 64f, pad = 4f, celula = 96f, @base = 70f, largura = 1024f, altura = 512f; public Glifo[] glifos; }

        static Dados d;
        static Dictionary<int, Glifo> mapa;
        static Material material;

        static void Carrega()
        {
            if (d != null) return;
            d = JsonUtility.FromJson<Dados>(Resources.Load<TextAsset>("Abertura/letra-estencil").text);
            mapa = new Dictionary<int, Glifo>();
            foreach (var g in d.glifos) mapa[g.c] = g;
        }

        /// <summary>Material unico para todo texto. A cor de cada linha vai nos vertices.</summary>
        public static Material Material
        {
            get
            {
                if (material == null)
                {
                    material = new Material(Shader.Find("Sprites/Default"));
                    material.mainTexture = Resources.Load<Texture2D>("Abertura/letra-estencil");
                }
                return material;
            }
        }

        /// <summary>Largura que o texto ocupa nesse tamanho.</summary>
        public static float Largura(string texto, float tamanho)
        {
            Carrega();
            float s = tamanho / d.tamanho, soma = 0f;
            for (int i = 0; i < texto.Length; i++) { Glifo g; if (mapa.TryGetValue(texto[i], out g)) soma += g.a * s; }
            return soma;
        }

        /// <summary>
        /// Escreve o texto na malha, centrado em (0, 0), no plano XY. tamanho = altura da letra, na unidade da malha.
        /// Se passar de larguraMaxima, a linha encolhe para caber.
        /// </summary>
        public static void Escreve(UnityEngine.Mesh malha, string texto, float tamanho, Color cor, float larguraMaxima)
        {
            Carrega();
            // cor de vertice nao passa pela correcao de cor da Unity: sem isto a tinta escura sai cinza
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) cor = cor.linear;
            float larg = Largura(texto, tamanho);
            if (larguraMaxima > 0f && larg > larguraMaxima) { tamanho *= larguraMaxima / larg; larg = larguraMaxima; }
            float s = tamanho / d.tamanho;
            float pena = -larg * 0.5f, linha = -0.34f * tamanho; // linha de base um pouco abaixo do centro

            var vs = new List<Vector3>(); var uvs = new List<Vector2>(); var cs = new List<Color>(); var ts = new List<int>();
            for (int i = 0; i < texto.Length; i++)
            {
                Glifo g;
                if (!mapa.TryGetValue(texto[i], out g)) continue;
                if (texto[i] != ' ')
                {
                    float x0 = pena - d.pad * s, x1 = x0 + g.w * s;
                    float y1 = linha + d.@base * s, y0 = y1 - d.celula * s;
                    float u0 = g.x / d.largura, u1 = (g.x + g.w) / d.largura;
                    float v1 = 1f - g.y / d.altura, v0 = 1f - (g.y + d.celula) / d.altura;
                    // a letra e desenhada cinco vezes, um fio deslocada: engrossa a tinta, que sumia na imagem de 320 linhas
                    float o = tamanho * 0.024f;
                    for (int k = 0; k < 5; k++)
                    {
                        float dx = k == 1 ? o : k == 2 ? -o : 0f, dy = k == 3 ? o : k == 4 ? -o : 0f;
                        int n = vs.Count;
                        vs.Add(new Vector3(x0 + dx, y0 + dy, 0f)); vs.Add(new Vector3(x0 + dx, y1 + dy, 0f)); vs.Add(new Vector3(x1 + dx, y1 + dy, 0f)); vs.Add(new Vector3(x1 + dx, y0 + dy, 0f));
                        uvs.Add(new Vector2(u0, v0)); uvs.Add(new Vector2(u0, v1)); uvs.Add(new Vector2(u1, v1)); uvs.Add(new Vector2(u1, v0));
                        cs.Add(cor); cs.Add(cor); cs.Add(cor); cs.Add(cor);
                        ts.Add(n); ts.Add(n + 1); ts.Add(n + 2); ts.Add(n); ts.Add(n + 2); ts.Add(n + 3);
                    }
                }
                pena += g.a * s;
            }
            malha.Clear();
            malha.SetVertices(vs); malha.SetUVs(0, uvs); malha.SetColors(cs); malha.SetTriangles(ts, 0);
            malha.RecalculateBounds();
        }
    }
}
