using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Visual retro, valendo para o jogo inteiro (menu, abertura e partida):
    ///   - PIXEL: a camera principal desenha numa imagem pequena (320 linhas) e essa imagem
    ///     e esticada na tela sem suavizar;
    ///   - TREMOR: os vertices pulam para uma grade (feito no shader BananaRoulette/Retro);
    ///   - os materiais que vieram do .glb sao trocados pelo shader retro, com a luz da Unity.
    ///
    /// Entra sozinho em toda cena: nao precisa colocar nada na Hierarchy.
    /// Teclas de teste: F1 liga/desliga o pixel, F2 liga/desliga o tremor.
    /// </summary>
    public class VisualRetro : MonoBehaviour
    {
        public static bool Pixel = true;
        public static bool Tremor = true;
        public static int Linhas = 320;

        static readonly int IdGrade = Shader.PropertyToID("_RetroGrade");
        static readonly int IdBaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int IdSemLuz = Shader.PropertyToID("_SemLuz");
        static readonly int IdCull = Shader.PropertyToID("_Cull");

        RenderTexture _imagem;
        Camera _alvo;
        Camera _tela;
        RawImage _quadro;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Iniciar()
        {
            SceneManager.sceneLoaded += (cena, modo) => Criar();
            Criar();
        }

        static void Criar()
        {
            if (FindFirstObjectByType<VisualRetro>() != null) return;
            new GameObject("Visual retro").AddComponent<VisualRetro>();
        }

        void Start()
        {
            TrocarMateriais();
            MontarTela();
        }

        // Troca os materiais do .glb (glTFast) pelo shader retro. Cada material e trocado uma vez so.
        void TrocarMateriais()
        {
            Shader retro = Shader.Find("BananaRoulette/Retro");
            if (retro == null) { Debug.LogWarning("[Visual retro] Shader BananaRoulette/Retro nao encontrado."); return; }

            var feitos = new Dictionary<Material, Material>();
            foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Material[] ms = r.sharedMaterials;
                bool mudou = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    Material m = ms[i];
                    if (m == null || m.shader == null || !m.shader.name.Contains("glTF")) continue;
                    // transparente (fumaca, vidro, balao) fica como veio
                    if (m.renderQueue >= 2450) continue;

                    Material novo;
                    if (!feitos.TryGetValue(m, out novo))
                    {
                        novo = new Material(retro) { name = m.name };
                        Texture tex = m.HasProperty("baseColorTexture") ? m.GetTexture("baseColorTexture") : null;
                        if (tex != null)
                        {
                            // textura pequena fica de pixel duro; a que tem escrita continua suavizada, para dar para ler
                            tex.filterMode = tex.width <= 96 ? FilterMode.Point : FilterMode.Trilinear;
                            novo.SetTexture(IdBaseMap, tex);
                            if (m.HasProperty("baseColorTexture_ST")) novo.SetVector("_BaseMap_ST", m.GetVector("baseColorTexture_ST"));
                        }
                        novo.SetColor(IdBaseColor, m.HasProperty("baseColorFactor") ? m.GetColor("baseColorFactor") : Color.white);
                        novo.SetFloat(IdSemLuz, m.shader.name.ToLowerInvariant().Contains("unlit") ? 1f : 0f);
                        novo.SetFloat(IdCull, m.HasProperty("_Cull") ? m.GetFloat("_Cull") : 2f);
                        feitos[m] = novo;
                    }
                    ms[i] = novo;
                    mudou = true;
                }
                if (mudou) r.sharedMaterials = ms;
            }
        }

        // Camera que nao desenha nada do mundo + um quadro que mostra a imagem pequena esticada.
        void MontarTela()
        {
            var camGo = new GameObject("Tela (pixel)");
            camGo.transform.SetParent(transform, false);
            _tela = camGo.AddComponent<Camera>();
            _tela.cullingMask = 0;
            _tela.clearFlags = CameraClearFlags.SolidColor;
            _tela.backgroundColor = Color.black;
            _tela.depth = 100f;

            var canvasGo = new GameObject("Quadro (pixel)", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // por baixo da interface do jogo

            var imgGo = new GameObject("Imagem", typeof(RawImage));
            imgGo.transform.SetParent(canvasGo.transform, false);
            _quadro = imgGo.GetComponent<RawImage>();
            _quadro.raycastTarget = false;
            var rt = _quadro.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            var teclado = Keyboard.current;
            if (teclado != null)
            {
                if (teclado.f1Key.wasPressedThisFrame) Pixel = !Pixel;
                if (teclado.f2Key.wasPressedThisFrame) Tremor = !Tremor;
            }

            if (_alvo == null || !_alvo.isActiveAndEnabled)
            {
                Camera c = Camera.main;
                if (c != null && c != _tela) _alvo = c;
            }

            int alt = Mathf.Max(64, Linhas);
            int larg = Mathf.Max(64, Mathf.RoundToInt(alt * (float)Screen.width / Mathf.Max(1, Screen.height)));

            if (Pixel && _alvo != null)
            {
                if (_imagem == null || _imagem.width != larg || _imagem.height != alt)
                {
                    if (_imagem != null) { if (_alvo.targetTexture == _imagem) _alvo.targetTexture = null; _imagem.Release(); Destroy(_imagem); }
                    _imagem = new RenderTexture(larg, alt, 24, RenderTextureFormat.Default) { filterMode = FilterMode.Point, name = "Imagem retro" };
                }
                if (_alvo.targetTexture != _imagem) _alvo.targetTexture = _imagem;
                _quadro.texture = _imagem;
                _quadro.enabled = true;
                _tela.enabled = true;
            }
            else
            {
                if (_alvo != null && _alvo.targetTexture == _imagem) _alvo.targetTexture = null;
                if (_quadro != null) _quadro.enabled = false;
                if (_tela != null) _tela.enabled = false;
            }

            float gx = Pixel ? larg * 0.5f : Screen.width * 0.25f;
            float gy = Pixel ? alt * 0.5f : Screen.height * 0.25f;
            Shader.SetGlobalVector(IdGrade, Tremor ? new Vector4(gx, gy, 0f, 0f) : Vector4.zero);
        }

        void OnDestroy()
        {
            if (_alvo != null && _alvo.targetTexture == _imagem) _alvo.targetTexture = null;
            if (_imagem != null) { _imagem.Release(); Destroy(_imagem); }
        }
    }
}
