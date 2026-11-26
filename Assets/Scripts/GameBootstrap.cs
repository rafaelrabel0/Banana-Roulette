using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BananaRoulette
{
    /// <summary>
    /// Monta o greybox inteiro por codigo: roda de macacos, banana, camera, luz,
    /// HUD e menus. A cena e praticamente vazia de proposito, entao nada aqui
    /// depende de asset importado, prefab ou serializacao.
    ///
    /// Ao carregar, abre a tela que o <see cref="AppEntry"/> pedir.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Arraste o asset TuningConfig. Sem ele, o jogo cria um com os valores padrao.")]
        public TuningConfig config;

        private MainMenuScreen _menu;
        private SettingsScreen _settings;
        private HudController _hud;
        private GameManager _manager;
        private BananaCarrier _banana;
        private Canvas _canvas;

        [Tooltip("Ligado: macacos, bomba, pavio e cenario em 3D (os mesmos da abertura). Desligado: o greybox de capsulas.")]
        public bool usar3D = true;

        // Cores de pelo da roda, as mesmas da abertura. O lugar 0 e o jogador.
        private static readonly Color[] Pelos =
        {
            new Color32(0x4A, 0x2A, 0x14, 0xFF), new Color32(0x23, 0x1C, 0x1C, 0xFF), new Color32(0x7A, 0x4A, 0x28, 0xFF),
            new Color32(0x8A, 0x85, 0x7D, 0xFF), new Color32(0xE6, 0xDF, 0xCF, 0xFF), new Color32(0x5E, 0x3A, 0x22, 0xFF),
            new Color32(0x3A, 0x30, 0x2A, 0xFF), new Color32(0xB0, 0x8A, 0x5A, 0xFF)
        };
        private static readonly Color CorSelva = new Color(0.576f, 0.737f, 0.486f);

        /// <summary>Quanto o mundo da previa encolhe para caber na roda do jogo.</summary>
        // Na previa a roda tem raio 15 e os macacos quase se encostam. Aqui a roda fica um terco mais folgada,
        // para o centro ficar livre e a bomba aparecer por cima da cabeca do jogador.
        private float Escala { get { return config.circleRadius / (MacacoVisual.RaioDaPrevia * 1.33f); } }

        private void Start()
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<TuningConfig>();
                Debug.LogWarning("[Banana Roulette] Sem TuningConfig atribuido. Usando valores padrao em memoria.");
            }

            GameSettings.Load();
            MonkeySlot.Tensao = 0f; // sobra da partida anterior

            if (usar3D && Resources.Load<GameObject>("Jogo/macaco-jogo") == null) usar3D = false;

            var camera = BuildCamera();
            if (usar3D) { BuildWorld(camera); }
            else { BuildLighting(); BuildGround(); }
            BuildEventSystem();

            List<MonkeySlot> slots;
            List<TurnSolver> solvers;
            BuildMonkeys(out slots, out solvers);

            _banana = BuildBanana();
            _canvas = BuildCanvas();
            _hud = BuildHud(_canvas);
            if (usar3D && _hud.TopFuse != null) _hud.TopFuse.Usar3D(PavioTopo3D.Criar(camera));
            if (usar3D) _hud.Layout3D();
            var flash = BuildFlashLight();

            var fuse = gameObject.AddComponent<FuseTimer>();
            var shake = camera.GetComponent<CameraShake>();

            switch (AppEntry.Next)
            {
                case AppEntry.Screen.Game:
                    _manager = gameObject.AddComponent<GameManager>();
                    _manager.Setup(config, slots, solvers, _banana, _hud, fuse, shake, flash);
                    _manager.StartMatch();
                    break;

                case AppEntry.Screen.Tutorial:
                    var tutorial = gameObject.AddComponent<TutorialDirector>();
                    tutorial.Setup(config, slots, solvers, _banana, _hud, fuse, shake, flash,
                        _canvas.transform);
                    break;

                default:
                    ShowMenu();
                    break;
            }
        }

        /// <summary>No menu a roda fica ali atras, parada, sem banana acesa.</summary>
        private void ShowMenu()
        {
            _banana.gameObject.SetActive(false);
            _hud.SetVisible(false);

            _menu = MainMenuScreen.Build(_canvas.transform,
                () => AppEntry.GoTo(AppEntry.Screen.Game),
                () => AppEntry.GoTo(AppEntry.Screen.Tutorial),
                OpenSettings);
        }

        private void OpenSettings()
        {
            _menu.SetVisible(false);
            _settings = SettingsScreen.Build(_canvas.transform, CloseSettings);
        }

        private void CloseSettings()
        {
            if (_settings != null) Destroy(_settings.gameObject);
            _settings = null;
            _menu.SetVisible(true);
        }

        // ------------------------------------------------------------------

        private Camera BuildCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(CameraShake));
            go.tag = "MainCamera";

            var cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.JungleNight;
            cam.fieldOfView = 50f;

            // Atras do macaco do jogador, olhando para o centro da roda.
            // Mais alta e mais inclinada do que o obvio: assim a roda inteira
            // sobe no quadro, o macaco do jogador para de tapar o centro e
            // sobra a faixa de baixo da tela para a fila de setas.
            float radius = config.circleRadius;
            go.transform.position = new Vector3(0f, usar3D ? 9.4f : 8.6f, -(radius + 7.6f));
            go.transform.LookAt(new Vector3(0f, usar3D ? 0.9f : 0.6f, 0.4f));

            return cam;
        }

        /// <summary>
        /// O cenario da abertura (floresta, caixotes, pedras) em volta da roda, com a luz de la.
        /// O modelo vem na medida da previa: encolhe e gira para a roda cair no centro do jogo,
        /// com os caixotes ao fundo.
        /// </summary>
        private void BuildWorld(Camera camera)
        {
            var mundo = new GameObject("Mundo");
            var modelo = Resources.Load<GameObject>("Abertura/abertura");
            if (modelo != null)
            {
                var copia = Instantiate(modelo, mundo.transform);
                copia.name = "Cenario da abertura";

                // poe as pecas na pose do instante zero da abertura (a do menu) e desliga a animacao
                var clipes = Resources.LoadAll<AnimationClip>("Abertura/abertura");
                if (clipes.Length > 0 && clipes[0].legacy)
                {
                    var anim = copia.GetComponent<Animation>();
                    if (anim == null) anim = copia.AddComponent<Animation>();
                    anim.playAutomatically = false;
                    if (anim.GetClip(clipes[0].name) == null) anim.AddClip(clipes[0], clipes[0].name);
                    var estado = anim[clipes[0].name];
                    estado.enabled = true; estado.weight = 1f; estado.time = 0f;
                    anim.Sample();
                    estado.enabled = false;
                    anim.enabled = false;
                }

                // os atores da abertura nao entram na partida
                foreach (Transform t in copia.transform)
                {
                    string n = t.name;
                    if (n.StartsWith("MacacoHeroi") || n.StartsWith("MacacoRoda") || n.StartsWith("Aviador") ||
                        n.StartsWith("Balao") || n.StartsWith("Bituca") || n.StartsWith("Camera"))
                        t.gameObject.SetActive(false);
                }
                VisualRetro.Converter(copia);
            }

            float e = Escala;
            Quaternion giro = Quaternion.Euler(0f, 180f, 0f);
            mundo.transform.localScale = Vector3.one * e;
            mundo.transform.rotation = giro;
            mundo.transform.position = -(giro * (MacacoVisual.CentroDaRoda * e));

            // Sol e ar da abertura. Na previa o sol fica em (40, 70, 50); aqui entra espelhado e girado junto com o mundo.
            var sol = new GameObject("Sol", typeof(Light));
            var luz = sol.GetComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.LookRotation(-(giro * new Vector3(-40f, 70f, 50f)).normalized);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.68f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = CorSelva;
            RenderSettings.fogStartDistance = 125f * e;
            RenderSettings.fogEndDistance = 340f * e;
            camera.backgroundColor = CorSelva;

            // Clareira: some com tronco, copa e pedra alta que ficariam entre a camera e a roda, ou dentro dela.
            Vector2 de = new Vector2(camera.transform.position.x, camera.transform.position.z), ate = new Vector2(0f, config.circleRadius);
            foreach (Renderer r in mundo.GetComponentsInChildren<Renderer>())
            {
                Bounds b = r.bounds;
                if (b.size.y < 0.8f) continue; // capim e chao ficam
                Vector2 p = new Vector2(b.center.x, b.center.z), eixo = ate - de;
                float k = Mathf.Clamp01(Vector2.Dot(p - de, eixo) / eixo.sqrMagnitude);
                float dist = (p - (de + eixo * k)).magnitude;
                if (dist < config.circleRadius + 1.5f + Mathf.Max(b.extents.x, b.extents.z) * 0.5f) r.enabled = false;
            }
        }

        private void BuildLighting()
        {
            var go = new GameObject("Directional Light", typeof(Light));
            var light = go.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.15f;
            go.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.30f, 0.26f);
        }

        private Light BuildFlashLight()
        {
            var go = new GameObject("ExplosionFlash", typeof(Light));
            var light = go.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.45f);
            light.range = 22f;
            light.intensity = 0f;
            light.enabled = false;
            return light;
        }

        /// <summary>Sem EventSystem os botoes do menu nao recebem clique.</summary>
        private void BuildEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            // O projeto usa o Input System novo: o modulo antigo nao funcionaria.
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = Vector3.one * 4f;
            Paint(ground, Palette.Ground);

            var clearing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clearing.name = "Clearing";
            clearing.transform.position = new Vector3(0f, 0.01f, 0f);
            clearing.transform.localScale = new Vector3(config.circleRadius * 2.3f, 0.01f, config.circleRadius * 2.3f);
            Destroy(clearing.GetComponent<Collider>());
            Paint(clearing, new Color(0.13f, 0.24f, 0.18f));
        }

        private void BuildMonkeys(out List<MonkeySlot> slots, out List<TurnSolver> solvers)
        {
            slots = new List<MonkeySlot>();
            solvers = new List<TurnSolver>();

            int count = config.monkeyCount;
            float radius = config.circleRadius;

            for (int i = 0; i < count; i++)
            {
                // Indice 0 fica na frente da camera. Indice crescente vai para
                // a direita da tela, que e o mesmo sentido da seta direita.
                float angle = -90f + (360f / count) * i;
                float rad = angle * Mathf.Deg2Rad;
                var position = new Vector3(Mathf.Cos(rad) * radius, 1f, Mathf.Sin(rad) * radius);

                bool isHuman = i == 0;

                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = isHuman ? "Monkey_00_VOCE" : string.Format("Monkey_{0:00}_Bot", i);
                body.transform.position = position;
                body.transform.LookAt(new Vector3(0f, 1f, 0f));
                Destroy(body.GetComponent<Collider>());

                var color = Palette.ForSlot(i, isHuman);
                Paint(body, color);

                var slot = body.AddComponent<MonkeySlot>();
                slot.Setup(i, isHuman, color, body.GetComponent<Renderer>());
                if (usar3D)
                {
                    // a capsula fica a 1 de altura; o modelo desce para sentar no chao
                    slot.UsarModelo(MacacoVisual.Criar(body.transform, new Vector3(0f, -1f, 0f), Escala, Pelos[i % Pelos.Length]));
                }

                TurnSolver solver;
                if (isHuman)
                {
                    var player = body.AddComponent<PlayerInputHandler>();
                    player.Configure(config);
                    solver = player;

                    BuildPlayerMarker(position);
                }
                else
                {
                    var bot = body.AddComponent<BotBrain>();
                    bot.Configure(config);
                    solver = bot;
                }

                slots.Add(slot);
                solvers.Add(solver);
            }
        }

        /// <summary>Anel no chao sob o macaco do jogador, para ele nunca se perder na roda.</summary>
        private void BuildPlayerMarker(Vector3 position)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "PlayerMarker";
            marker.transform.position = new Vector3(position.x, 0.02f, position.z);
            marker.transform.localScale = new Vector3(1.8f, 0.01f, 1.8f);
            Destroy(marker.GetComponent<Collider>());
            PaintUnlit(marker, Palette.BananaYellow);
        }

        private BananaCarrier BuildBanana()
        {
            var root = new GameObject("BananaDeDinamite");
            if (usar3D && BuildBanana3D(root)) return root.GetComponent<BananaCarrier>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.28f, 0.42f, 0.28f);
            Destroy(body.GetComponent<Collider>());
            Paint(body, Palette.DynamiteRed);

            // Cordao do pavio: e ele que encurta conforme o tempo passa. Cor de
            // corda, clara, porque escuro ele sumia contra a selva a noite.
            var cord = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cord.name = "FuseCord";
            cord.transform.SetParent(root.transform, false);
            cord.transform.localScale = new Vector3(0.09f, 0.30f, 0.09f);
            cord.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            Destroy(cord.GetComponent<Collider>());
            Paint(cord, new Color32(0xD8, 0xC0, 0x8E, 0xFF));

            // Faisca: mora na ponta do que sobrou do cordao.
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "FuseSpark";
            spark.transform.SetParent(root.transform, false);
            spark.transform.localPosition = new Vector3(0f, 0.98f, 0f);
            spark.transform.localScale = Vector3.one * 0.2f;
            Destroy(spark.GetComponent<Collider>());
            PaintUnlit(spark, Palette.BananaYellow);

            var carrier = root.AddComponent<BananaCarrier>();
            carrier.Setup(body.GetComponent<Renderer>(), cord.transform, spark.transform,
                spark.GetComponent<Renderer>());

            return carrier;
        }

        /// <summary>O maco de dinamite da previa, com o pavio que encurta de verdade.</summary>
        private bool BuildBanana3D(GameObject root)
        {
            var modelo = Resources.Load<GameObject>("Jogo/maco-jogo");
            if (modelo == null) return false;

            var copia = Instantiate(modelo, root.transform);
            copia.transform.localScale = Vector3.one * Escala * 1.4f; // um pouco maior que o real, para se ver de longe
            VisualRetro.Converter(copia);

            Transform faisca = null, pavioVelho = null;
            foreach (Transform t in copia.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Faisca")) faisca = t;
                else if (t.name.StartsWith("Pavio")) pavioVelho = t;
            }
            if (faisca == null) { Destroy(copia); return false; }
            if (pavioVelho != null) pavioVelho.gameObject.SetActive(false); // o cordao pronto sai; entra o que encurta

            var cordao = new GameObject("Pavio vivo", typeof(MeshFilter), typeof(MeshRenderer));
            cordao.transform.SetParent(faisca.parent, false);
            var tinta = new Material(Shader.Find("BananaRoulette/Retro"));
            tinta.SetColor("_BaseColor", new Color32(0x5A, 0x3A, 0x1C, 0xFF)); tinta.SetFloat("_SemLuz", 1f); tinta.SetFloat("_Cull", 0f);
            cordao.GetComponent<MeshRenderer>().sharedMaterial = tinta;

            // corpo do maco com materiais proprios, para poder clarear quando o pavio esta no fim
            var corpo = new List<Material>();
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
            return true;
        }

        private Canvas BuildCanvas()
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private HudController BuildHud(Canvas canvas)
        {
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            UiFactory.Anchor(hudGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var hud = hudGo.AddComponent<HudController>();
            hud.Build(canvas, config);
            return hud;
        }

        // ------------------------------------------------------------------

        private static Shader _litShader;
        private static Shader _unlitShader;

        private static void Paint(GameObject go, Color color)
        {
            if (_litShader == null)
            {
                _litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (_litShader == null) _litShader = Shader.Find("Standard");
            }

            var renderer = go.GetComponent<Renderer>();
            var material = new Material(_litShader);
            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            renderer.material = material;
        }

        private static void PaintUnlit(GameObject go, Color color)
        {
            if (_unlitShader == null)
            {
                _unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (_unlitShader == null) _unlitShader = Shader.Find("Unlit/Color");
            }

            var renderer = go.GetComponent<Renderer>();
            var material = new Material(_unlitShader);
            material.color = color;
            renderer.material = material;
        }
    }
}
