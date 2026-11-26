using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BananaRoulette.EditorTools
{
    /// <summary>
    /// Menu "Banana Roulette > Montar cena de abertura".
    ///
    /// Cria a cena Assets/Scenes/Abertura.unity a partir de Assets/Resources/Abertura/abertura.glb:
    /// poe o modelo na cena, cria o Animator com o clipe assado, a camera, a luz e
    /// liga tudo no AberturaPlayer. Pode rodar de novo quando o .glb mudar.
    /// </summary>
    public static class MontaAbertura
    {
        const string Pasta = "Assets/Abertura";
        const string CaminhoGlb = Pasta + "/abertura.glb";
        const string CaminhoDados = Pasta + "/abertura.json";
        const string CaminhoControle = Pasta + "/Abertura.controller";
        const string CaminhoCena = "Assets/Scenes/Abertura.unity";

        [MenuItem("Banana Roulette/Montar cena de abertura")]
        public static void Montar()
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CaminhoGlb);
            if (modelo == null)
            {
                EditorUtility.DisplayDialog("Abertura",
                    "Nao consegui abrir " + CaminhoGlb + " como modelo.\n\n" +
                    "O pacote glTFast (com.unity.cloud.gltfast) precisa estar instalado. " +
                    "Confira em Window > Package Manager e veja se ha erro vermelho no Console.", "OK");
                return;
            }

            AnimationClip clipe = AssetDatabase.LoadAllAssetsAtPath(CaminhoGlb)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clipe == null)
            {
                EditorUtility.DisplayDialog("Abertura",
                    "O modelo abriu, mas veio sem animacao.\n\n" +
                    "Clique em " + CaminhoGlb + " na janela Project e, no Inspector, confira se " +
                    "Animation esta em Mecanim (ou Legacy). Aperte Apply e rode este menu de novo.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject raiz = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            raiz.name = "Abertura";

            AberturaPlayer tocador = raiz.GetComponent<AberturaPlayer>();
            if (tocador == null) tocador = raiz.AddComponent<AberturaPlayer>();
            tocador.nomeDoClipe = clipe.name;

            if (clipe.legacy)
            {
                Animation legado = raiz.GetComponent<Animation>();
                if (legado == null) legado = raiz.AddComponent<Animation>();
                legado.playAutomatically = false;
                legado.AddClip(clipe, clipe.name);
                legado.clip = clipe;
                tocador.animacaoLegada = legado;
                tocador.animator = null;
            }
            else
            {
                AssetDatabase.DeleteAsset(CaminhoControle);
                AnimatorController controle = AnimatorController.CreateAnimatorControllerAtPathWithClip(CaminhoControle, clipe);
                Animator animator = raiz.GetComponent<Animator>();
                if (animator == null) animator = raiz.AddComponent<Animator>();
                animator.runtimeAnimatorController = controle;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                tocador.animator = animator;
                tocador.animacaoLegada = null;
            }

            tocador.dados = AssetDatabase.LoadAssetAtPath<TextAsset>(CaminhoDados);
            tocador.noCamera = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Camera");

            // Camera de verdade. O tocador a move a cada quadro.
            GameObject cam = new GameObject("Camera da abertura");
            cam.tag = "MainCamera";
            Camera c = cam.AddComponent<Camera>();
            c.nearClipPlane = 0.5f;
            c.farClipPlane = 900f;
            c.fieldOfView = 36f;
            c.clearFlags = CameraClearFlags.SolidColor;
            cam.AddComponent<AudioListener>();
            tocador.cameraDaCena = c;

            // Menu em 3D nos caixotes: ele segura a cena parada e so a solta quando o jogador escolhe JOGAR.
            tocador.tocarAoIniciar = false;
            MenuAbertura menu = raiz.GetComponent<MenuAbertura>();
            if (menu == null) menu = raiz.AddComponent<MenuAbertura>();
            menu.tocador = tocador;
            menu.cam = c;

            // Sol. Na previa ele fica em (40, 70, 50) olhando para o centro; o importador espelha o eixo X.
            GameObject sol = new GameObject("Sol");
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            sol.transform.position = new Vector3(-40f, 70f, 50f);
            sol.transform.rotation = Quaternion.LookRotation(-sol.transform.position.normalized);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.68f);

            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AssetDatabase.SaveAssets();
            PoeNaListaDeBuild();
            Selection.activeGameObject = raiz;

            string aviso = tocador.noCamera == null
                ? "\n\nATENCAO: nao achei o no \"Camera\" dentro do modelo. A camera nao vai se mexer."
                : "";
            EditorUtility.DisplayDialog("Abertura",
                "Cena criada em " + CaminhoCena + ".\n\nAperte Play para assistir." + aviso, "OK");
        }

        /// <summary>Poe a Abertura como primeira cena do jogo e a Prototipo logo depois (File > Build Profiles).</summary>
        [MenuItem("Banana Roulette/Por as cenas na lista de build")]
        public static void PoeNaListaDeBuild()
        {
            var lista = EditorBuildSettings.scenes.Where(c => c.path != CaminhoCena).ToList();
            lista.Insert(0, new EditorBuildSettingsScene(CaminhoCena, true));
            const string jogo = "Assets/Scenes/Prototipo.unity";
            if (!lista.Any(c => c.path == jogo)) lista.Add(new EditorBuildSettingsScene(jogo, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }
    }
}
