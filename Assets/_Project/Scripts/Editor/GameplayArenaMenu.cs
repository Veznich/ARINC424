#if UNITY_EDITOR
using System;
using Arkanoid.Configs;
using Arkanoid.Gameplay;
using Arkanoid.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arkanoid.StudioEditor
{
    /// <summary>
    /// Собирает playable Bootstrap-сцену без прямых ссылок на VContainer-типы
    /// (Editor asmdef не тянет LifetimeScope → нет CS0012).
    /// </summary>
    public static class GameplayArenaMenu
    {
        private const string ScenesFolder = "Assets/_Project/Scenes";
        private const string ScenePath = ScenesFolder + "/Bootstrap.unity";
        private const string ConfigsPath = "Assets/_Project/Configs";

        private static readonly Type RuntimeAsmProbe = typeof(PaddleController);

        [MenuItem("Arkanoid/Gameplay/Create Full Bootstrap Scene (Playable)")]
        public static void CreateFullBootstrapScene()
        {
            // Без URP asset в GraphicsSettings все URP-материалы = magenta.
            UrpSetupMenu.SetupUrp();

            EnsureFolder("Assets/_Project");
            EnsureFolder(ScenesFolder);
            EnsureFolder(ConfigsPath);

            ConfigAssetsMenu.CreateAllDefaults();
            var catalog = AssetDatabase.LoadAssetAtPath<GameConfigCatalog>(
                ConfigsPath + "/GameConfigCatalog.asset");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.015f, 0.03f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.transform.position = new Vector3(0f, 1.5f, -10f);
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.92f, 0.95f, 1f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            var projectGo = new GameObject("ProjectContext");
            // Awake не должен вызваться до назначения catalog.
            projectGo.SetActive(false);
            var projectScope = AddRuntimeComponent(projectGo, "Arkanoid.Core.ProjectLifetimeScope");
            AddRuntimeComponent(projectGo, "Arkanoid.Save.SaveLifecycleBehaviour");

            if (projectScope != null)
            {
                var projectSo = new SerializedObject(projectScope);
                projectSo.Update();
                var catalogProp = projectSo.FindProperty("configCatalog");
                if (catalogProp != null)
                {
                    catalogProp.objectReferenceValue = catalog;
                }
                else
                {
                    Debug.LogError("[Arkanoid] Не найдено поле configCatalog на ProjectLifetimeScope.");
                }

                var ddl = projectSo.FindProperty("dontDestroyOnLoad");
                if (ddl != null)
                {
                    ddl.boolValue = true;
                }

                projectSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(projectScope);
            }

            var arena = BuildArena(cam, projectScope);
            arena.SetActive(false);
            CreateBackground(arena.transform);
            MobileDebugViewMenu.SetupAll();

            projectGo.SetActive(true);
            arena.SetActive(true);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            Selection.activeGameObject = arena;
            Debug.Log(
                "[Arkanoid] Bootstrap.unity создана: " + ScenePath +
                ". Play → платформа, мяч, блоки (Этап 3).");
        }

        [MenuItem("Arkanoid/Gameplay/Create Stage2 Arena In Active Scene")]
        public static void CreateArenaInActiveScene()
        {
            var project = FindRuntimeComponent("Arkanoid.Core.ProjectLifetimeScope");
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.01f, 0.015f, 0.03f, 1f);
                camGo.AddComponent<AudioListener>();
            }

            MobileDebugViewMenu.TuneMainCameraForPortrait(cam);
            var arena = BuildArena(cam, project);
            CreateBackground(arena.transform);
            MobileDebugViewMenu.SetupAll();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = arena;
            Debug.Log("[Arkanoid] Arena добавлена в текущую сцену. Сохрани сцену и Play.");
        }

        private static GameObject BuildArena(Camera cam, Component projectScope)
        {
            var arena = new GameObject("GameplayArena");
            arena.SetActive(false);
            var scope = AddRuntimeComponent(arena, "Arkanoid.Gameplay.GameplayLifetimeScope");
            arena.AddComponent<PlayfieldBounds>();

            if (scope == null)
            {
                Debug.LogError(
                    "[Arkanoid] GameplayLifetimeScope не найден. Проверь, что Runtime собрался без ошибок.");
                return arena;
            }

            var inputGo = new GameObject("Input");
            inputGo.transform.SetParent(arena.transform);
            var input = inputGo.AddComponent<GameplayInputReader>();

            var paddleGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paddleGo.name = "Paddle";
            paddleGo.transform.SetParent(arena.transform);
            paddleGo.transform.position = new Vector3(0f, PlayfieldLayout.PaddleY, 0f);
            paddleGo.transform.localScale = new Vector3(2f, 0.4f, GameplayVisualBootstrap.PaddleDepth);
            UnityEngine.Object.DestroyImmediate(paddleGo.GetComponent<BoxCollider>());
            ApplyColor(paddleGo, new Color(0.2f, 0.95f, 1f));
            var paddle = paddleGo.AddComponent<PaddleController>();

            var ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballGo.name = "Ball";
            ballGo.transform.SetParent(arena.transform);
            ballGo.transform.position = new Vector3(0f, -2.9f, 0f);
            ballGo.transform.localScale = Vector3.one * 0.5f;
            UnityEngine.Object.DestroyImmediate(ballGo.GetComponent<SphereCollider>());
            ApplyColor(ballGo, new Color(1f, 0.25f, 0.9f));

            // Не использовать ?? — у Unity Object «destroyed» не является C# null.
            var rb = ballGo.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = ballGo.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var ball = ballGo.AddComponent<BallController>();

            var so = new SerializedObject(scope);
            SetRef(so, "inputReader", input);
            SetRef(so, "paddle", paddle);
            SetRef(so, "ball", ball);
            SetRef(so, "bounds", arena.GetComponent<PlayfieldBounds>());
            SetRef(so, "gameplayCamera", cam);
            var autoStart = so.FindProperty("autoStartGameplay");
            if (autoStart != null)
            {
                autoStart.boolValue = true;
            }

            if (projectScope != null)
            {
                var parentRef = so.FindProperty("parentReference");
                var typeName = parentRef?.FindPropertyRelative("TypeName");
                if (typeName != null)
                {
                    typeName.stringValue = "Arkanoid.Core.ProjectLifetimeScope";
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return arena;
        }

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static Component AddRuntimeComponent(GameObject go, string fullTypeName)
        {
            var type = ResolveRuntimeType(fullTypeName);
            if (type == null)
            {
                Debug.LogError("[Arkanoid] Тип не найден: " + fullTypeName);
                return null;
            }

            return go.AddComponent(type);
        }

        private static Component FindRuntimeComponent(string fullTypeName)
        {
            var type = ResolveRuntimeType(fullTypeName);
            if (type == null)
            {
                return null;
            }

            return UnityEngine.Object.FindAnyObjectByType(type) as Component;
        }

        private static Type ResolveRuntimeType(string fullTypeName)
        {
            var asm = RuntimeAsmProbe.Assembly;
            var type = asm.GetType(fullTypeName);
            if (type != null)
            {
                return type;
            }

            var shortName = fullTypeName;
            var dot = fullTypeName.LastIndexOf('.');
            if (dot >= 0)
            {
                shortName = fullTypeName.Substring(dot + 1);
            }

            foreach (var t in asm.GetTypes())
            {
                if (t.Name == shortName)
                {
                    return t;
                }
            }

            return null;
        }

        private static void CreateBackground(Transform parent)
        {
            var existing = parent.Find("Starfield");
            GameObject starGo;
            if (existing != null)
            {
                starGo = existing.gameObject;
            }
            else
            {
                starGo = new GameObject("Starfield");
                starGo.transform.SetParent(parent, false);
            }

            if (starGo.GetComponent<StarfieldBackground>() == null)
            {
                starGo.AddComponent<StarfieldBackground>();
            }

            // Старый плоский фон больше не создаём
            var old = parent.Find("PlayfieldBackground");
            if (old != null)
            {
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            var mat = Arkanoid.Utils.RuntimeMaterialUtil.CreatePseudo3d(color, 0.14f);
            if (mat != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
