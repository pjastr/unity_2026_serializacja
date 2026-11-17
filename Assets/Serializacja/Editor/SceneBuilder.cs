using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Serializacja.EditorTools
{
    /// <summary>
    /// Generator scen demonstracyjnych + polecenia w górnym menu "Serializacja".
    /// Sceny są tworzone kodem, więc zawsze można je odtworzyć (Serializacja > Generuj wszystkie sceny).
    /// Lista scen: SceneList.cs, definicje scen: SceneDefinitions_*.cs.
    /// </summary>
    public static partial class SceneBuilder
    {
        public const string Root = "Assets/Serializacja";
        public const string ScenesDir = Root + "/Scenes";
        public const string MatDir = Root + "/Materials";
        public const string TexDir = Root + "/Textures";
        public const string DataDir = Root + "/Data";
        const string ActionsPath = "Assets/InputSystem_Actions.inputactions";
        public const string BuildPath = "Builds/Windows/SerializacjaDemo.exe";

        public struct SceneDef
        {
            public string file;
            public Action build;
            public SceneDef(string file, Action build) { this.file = file; this.build = build; }
        }

        public static string ScenePath(string file) => $"{ScenesDir}/{file}.unity";

        // =====================================================================
        //  MENU
        // =====================================================================

        [MenuItem("Serializacja/Generuj wszystkie sceny", priority = 0)]
        public static void GenerateAllMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            GenerateAll();
            EditorSceneManager.OpenScene(ScenePath(Scenes[0].file));
            EditorUtility.DisplayDialog("Serializacja",
                $"Wygenerowano {Scenes.Length} scen w {ScenesDir} i dodano je do Build Settings.\n\n" +
                "Naciśnij Play. Klawisze 1/2 przełączają sceny.", "OK");
        }

        [MenuItem("Serializacja/Panel scen (otwieranie i generowanie)", priority = 1)]
        public static void OpenBrowser() => SceneBrowserWindow.ShowWindow();

        [MenuItem("Serializacja/Otwórz pierwszą scenę i uruchom Play", priority = 2)]
        public static void OpenFirstAndPlay()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (!File.Exists(ScenePath(Scenes[0].file)))
                GenerateAll();
            EditorSceneManager.OpenScene(ScenePath(Scenes[0].file));
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Serializacja/Zbuduj wersję Windows (x64)", priority = 20)]
        public static void BuildWindowsMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var report = BuildWindows();
            bool ok = report != null && report.summary.result == BuildResult.Succeeded;
            if (ok)
            {
                EditorUtility.DisplayDialog("Serializacja", $"Build zakończony powodzeniem:\n{Path.GetFullPath(BuildPath)}", "OK");
                EditorUtility.RevealInFinder(BuildPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Serializacja", "Build nie powiódł się – szczegóły w oknie Console.", "OK");
            }
        }

        [MenuItem("Serializacja/Napraw ustawienia projektu (Input System, Build Settings)", priority = 21)]
        public static void FixProjectSettingsMenu()
        {
            FixProjectSettings();
            UpdateBuildSettings();
            EditorUtility.DisplayDialog("Serializacja",
                "Ustawiono:\n• Active Input Handling = Input System Package (New)\n" +
                "• Project-wide Actions = Assets/InputSystem_Actions.inputactions\n" +
                "• Asset Serialization = Force Text\n" +
                $"• sceny 01–{Scenes.Length:00} w Build Settings", "OK");
        }

        [MenuItem("Serializacja/Otwórz instrukcję (README.md)", priority = 40)]
        public static void OpenReadme()
        {
            string p = Path.GetFullPath("README.md");
            if (File.Exists(p))
                EditorUtility.OpenWithDefaultApp(p);
            else
                Debug.LogWarning("Nie znaleziono README.md w katalogu projektu.");
        }

        // =====================================================================
        //  BATCH MODE (wywołania z linii poleceń: -executeMethod ...)
        // =====================================================================

        /// <summary>Unity.exe -batchmode -projectPath ... -executeMethod Serializacja.EditorTools.SceneBuilder.BatchGenerate</summary>
        public static void BatchGenerate()
        {
            int code = 0;
            try
            {
                GenerateAll();
                Debug.Log("[Serializacja] BatchGenerate: OK");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        /// <summary>Generuje sceny i buduje wersję Windows x64.</summary>
        public static void BatchGenerateAndBuildWindows()
        {
            int code = 0;
            try
            {
                GenerateAll();
                var report = BuildWindows();
                if (report == null || report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError("[Serializacja] Build Windows: NIEPOWODZENIE " + (report != null ? report.summary.result.ToString() : "brak raportu"));
                    code = 2;
                }
                else
                {
                    Debug.Log($"[Serializacja] Build Windows: OK – {report.summary.totalSize / (1024 * 1024)} MB, błędów: {report.summary.totalErrors}");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            EditorApplication.Exit(code);
        }

        // =====================================================================
        //  GENEROWANIE
        // =====================================================================

        public static void GenerateAll()
        {
            FixProjectSettings();
            EnsureFolders();
            try
            {
                EditorUtility.DisplayProgressBar("Serializacja", "Tworzenie zasobów", 0f);
                CreateSharedAssets();
                for (int i = 0; i < Scenes.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Serializacja", "Generowanie " + Scenes[i].file, (float)i / Scenes.Length);
                    GenerateScene(Scenes[i], false);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateScene(SceneDef def) => GenerateScene(def, true);

        static void GenerateScene(SceneDef def, bool createAssets)
        {
            EnsureFolders();
            if (createAssets)
                CreateSharedAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            s_Hud = null;
            s_Camera = null;
            def.build();
            string path = ScenePath(def.file);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[Serializacja] Zapisano scenę: " + path);
        }

        public static void UpdateBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var d in Scenes)
            {
                string p = ScenePath(d.file);
                if (File.Exists(p))
                    list.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }

        public static BuildReport BuildWindows()
        {
            UpdateBuildSettings();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                GenerateAll();
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(BuildPath));
            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };
            return BuildPipeline.BuildPlayer(opts);
        }

        public static void FixProjectSettings()
        {
            // 1) Active Input Handling = Input System Package (New)
            var psAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (psAsset != null)
            {
                var so = new SerializedObject(psAsset);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue == 0)
                {
                    prop.intValue = 1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.LogWarning("[Serializacja] Ustawiono Active Input Handling = Input System Package (New). Uruchom ponownie edytor.");
                }
            }

            // 2) Project-wide actions = domyślny plik InputSystem_Actions
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions != null && InputSystem.actions != actions)
            {
                InputSystem.actions = actions;
                Debug.Log("[Serializacja] Ustawiono Project-wide Actions: " + ActionsPath);
            }
            else if (actions == null)
            {
                Debug.LogWarning("[Serializacja] Nie znaleziono " + ActionsPath + " – sterowanie nie będzie działać.");
            }

            // 3) Zasoby zapisywane tekstowo (YAML) – można je czytać i porównywać w systemie kontroli wersji
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorSettings.serializationMode = SerializationMode.ForceText;
                Debug.Log("[Serializacja] Ustawiono Asset Serialization Mode = Force Text");
            }
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { Root, ScenesDir, MatDir, TexDir, DataDir })
                EnsureFolder(f);
        }

        public static void EnsureFolder(string f)
        {
            if (AssetDatabase.IsValidFolder(f))
                return;
            string parent = Path.GetDirectoryName(f).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(f));
        }

        /// <summary>Tworzy lub nadpisuje zasób (asset) pod podaną ścieżką i zwraca obiekt zapisany w AssetDatabase.</summary>
        public static T SaveAsset<T>(T obj, string path) where T : UnityEngine.Object
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
            return obj;
        }

        // =====================================================================
        //  MATERIAŁY I TEKSTURY
        // =====================================================================

        static Shader LitShader
        {
            get
            {
                var s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Standard");
                return s;
            }
        }

        public static Material Mat(string name, Color c, float smoothness = 0.35f)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(LitShader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material UnlitMat(string name, Color c)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material TransparentMat(string name, Color c)
        {
            var m = Mat(name, c);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.SetShaderPassEnabled("DepthOnly", false);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material s_FloorMat;

        static Material FloorMat()
        {
            if (s_FloorMat != null)
                return s_FloorMat;
            string texPath = $"{TexDir}/Szachownica.png";
            if (!File.Exists(texPath))
            {
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
                Color a = new Color(0.58f, 0.6f, 0.63f);
                Color b = new Color(0.46f, 0.48f, 0.51f);
                Color line = new Color(0.36f, 0.38f, 0.41f);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool cell = ((x / (size / 2)) + (y / (size / 2))) % 2 == 0;
                    bool border = x % (size / 2) < 2 || y % (size / 2) < 2;
                    tex.SetPixel(x, y, border ? line : (cell ? a : b));
                }
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.anisoLevel = 8;
                imp.filterMode = FilterMode.Trilinear;
                imp.SaveAndReimport();
            }
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var m = Mat("Podloga", Color.white, 0.15f);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
            m.mainTexture = t;
            m.mainTextureScale = new Vector2(60, 60); // podłoga 120 m → kratka 1 m
            EditorUtility.SetDirty(m);
            s_FloorMat = m;
            return m;
        }

        public static readonly Color ColTitle = new Color(1f, 0.85f, 0.4f);

        public static Material M_Red => Mat("Czerwony", new Color(0.86f, 0.27f, 0.23f));
        public static Material M_Orange => Mat("Pomaranczowy", new Color(0.95f, 0.55f, 0.15f));
        public static Material M_Yellow => Mat("Zolty", new Color(0.95f, 0.82f, 0.25f));
        public static Material M_Green => Mat("Zielony", new Color(0.3f, 0.75f, 0.35f));
        public static Material M_Teal => Mat("Turkusowy", new Color(0.2f, 0.7f, 0.75f));
        public static Material M_Blue => Mat("Niebieski", new Color(0.25f, 0.45f, 0.9f));
        public static Material M_Purple => Mat("Fioletowy", new Color(0.6f, 0.35f, 0.85f));
        public static Material M_Gray => Mat("Szary", new Color(0.55f, 0.57f, 0.6f));
        public static Material M_Dark => Mat("Ciemny", new Color(0.22f, 0.23f, 0.26f));
        public static Material M_White => Mat("Bialy", new Color(0.92f, 0.92f, 0.92f));
        public static Material M_Wall => Mat("Sciana", new Color(0.72f, 0.66f, 0.58f));
        public static Material M_Wood => Mat("Drewno", new Color(0.6f, 0.42f, 0.25f));

        public static Material[] Palette => new[] { M_Red, M_Orange, M_Yellow, M_Green, M_Teal, M_Blue, M_Purple, M_White };

        // =====================================================================
        //  SCENA, KAMERA, HUD
        // =====================================================================

        static DemoHud s_Hud;
        static Camera s_Camera;

        /// <summary>
        /// Wspólny początek sceny: światło, kamera główna, HUD.
        /// freeCamera – swobodna kamera (PPM + mysz, WASD). statusWidth – szerokość panelu stanu (px przy 1920×1080).
        /// </summary>
        public static Camera Begin(string title, string description, string sceneHint, string controls,
            bool freeCamera = false, float statusWidth = 600f, float descriptionWidth = 560f)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.46f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.21f, 0.2f);
            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.97f, 0.92f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            s_Camera = camGo.AddComponent<Camera>();
            s_Camera.nearClipPlane = 0.1f;
            s_Camera.farClipPlane = 500f;
            s_Camera.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();
            description = SafeRichText(description);
            sceneHint = SafeRichText(sceneHint);
            if (freeCamera)
            {
                camGo.AddComponent<FreeCamera>();
                controls = "<b>PPM + mysz</b> – obrót kamery   <b>WASD</b> – ruch kamery   <b>kółko</b> – przód/tył" +
                           (string.IsNullOrEmpty(controls) ? "" : "   " + controls);
            }
            s_Hud = CreateHud(title, description, sceneHint, controls, statusWidth, descriptionWidth);
            return s_Camera;
        }

        /// <summary>
        /// Legacy Text interpretuje znaczniki &lt;...&gt; – zamieniamy nawiasy typów generycznych (List&lt;T&gt;)
        /// na ‹ › i zostawiamy znaczniki formatowania (b, i, color, size).
        /// </summary>
        public static string SafeRichText(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return System.Text.RegularExpressions.Regex.Replace(s, @"<(?!/?(b|i|color|size)\b)([^<>]*)>", "‹$2›");
        }

        public static void Cam(Vector3 pos, Vector3 lookAt)
        {
            s_Camera.transform.position = pos;
            s_Camera.transform.LookAt(lookAt);
        }

        static DemoHud CreateHud(string title, string description, string hint, string controls, float statusWidth, float descriptionWidth)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("UI_Opis", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var labels = NewRect("Etykiety", canvasGo.transform);
            labels.anchorMin = Vector2.zero;
            labels.anchorMax = Vector2.zero;
            labels.pivot = Vector2.zero;
            labels.anchoredPosition = Vector2.zero;
            labels.sizeDelta = Vector2.zero;

            var panel = NewPanel("PanelOpisu", canvasGo.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -12), descriptionWidth,
                new Color(0.04f, 0.06f, 0.09f, 0.82f));
            var titleText = NewText("Tytul", panel, font, 24, FontStyle.Bold, ColTitle, TextAnchor.UpperLeft);
            titleText.text = title;
            var descText = NewText("Opis", panel, font, 16, FontStyle.Normal, new Color(0.93f, 0.94f, 0.96f), TextAnchor.UpperLeft);
            descText.text = description;
            descText.lineSpacing = 1.05f;

            var hintPanel = NewRect("PasekPodpowiedzi", canvasGo.transform);
            hintPanel.anchorMin = new Vector2(0, 0);
            hintPanel.anchorMax = new Vector2(1, 0);
            hintPanel.pivot = new Vector2(0.5f, 0);
            hintPanel.anchoredPosition = Vector2.zero;
            hintPanel.sizeDelta = new Vector2(0, 60);
            hintPanel.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.78f);
            var hlg = hintPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            hlg.padding = new RectOffset(16, 16, 8, 8);
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = false;
            var hfit = hintPanel.gameObject.AddComponent<ContentSizeFitter>();
            hfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var hintText = NewText("Podpowiedz", hintPanel, font, 16, FontStyle.Normal, new Color(0.9f, 0.92f, 0.95f), TextAnchor.MiddleCenter);

            var statusPanel = NewPanel("PanelStanu", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), statusWidth,
                new Color(0.04f, 0.06f, 0.09f, 0.78f));
            var statusText = NewText("Stan", statusPanel, font, 15, FontStyle.Normal, new Color(0.93f, 0.94f, 0.96f), TextAnchor.UpperLeft);
            statusPanel.gameObject.SetActive(false);

            var mgr = new GameObject("DemoManager");
            var hud = mgr.AddComponent<DemoHud>();
            hud.title = title;
            hud.description = description;
            hud.sceneHint = hint;
            hud.controlsHint = controls;
            hud.descriptionPanel = panel.gameObject;
            hud.titleText = titleText;
            hud.descriptionText = descText;
            hud.hintText = hintText;
            hud.statusPanel = statusPanel.gameObject;
            hud.statusText = statusText;
            hud.labelsRoot = labels;
            hud.font = font;
            hud.Refresh();
            return hud;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform NewPanel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, float width, Color bg)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(width, 100);
            rt.gameObject.AddComponent<Image>().color = bg;
            var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(14, 14, 10, 12);
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        static Text NewText(string name, Transform parent, Font font, int size, FontStyle style, Color color, TextAnchor align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Dodaje komponent do obiektu DemoManager bieżącej sceny.</summary>
        public static T Manager<T>() where T : Component => s_Hud.gameObject.AddComponent<T>();

        // =====================================================================
        //  OBIEKTY SCENY
        // =====================================================================

        public static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            if (parent != null) t.SetParent(parent, false);
            return t;
        }

        public static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = rot ?? Quaternion.identity;
            if (parent != null)
            {
                // rozmiar w świecie niezależny od skali rodzica
                Vector3 ps = parent.lossyScale;
                scale = new Vector3(scale.x / ps.x, scale.y / ps.y, scale.z / ps.z);
            }
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        public static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, Transform parent = null, Quaternion? rot = null)
            => Prim(PrimitiveType.Cube, name, pos, size, mat, parent, rot);

        public static GameObject Ball(string name, Vector3 pos, float diameter, Material mat, Transform parent = null)
            => Prim(PrimitiveType.Sphere, name, pos, Vector3.one * diameter, mat, parent);

        public static void NoCollider(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(c);
        }

        public static WorldLabel Label(GameObject go, string header, float offsetY = 1f, int fontSize = 15)
        {
            var l = go.AddComponent<WorldLabel>();
            l.header = header;
            l.worldOffset = new Vector3(0, offsetY, 0);
            l.fontSize = fontSize;
            return l;
        }

        public static GameObject SectionLabel(string text, Vector3 pos, Transform parent = null, int fontSize = 20)
        {
            var go = new GameObject("Napis_" + text.Split('\n')[0]);
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = pos;
            var l = Label(go, text, 0f, fontSize);
            l.color = ColTitle;
            return go;
        }

        public static GameObject Floor(float size = 120f)
        {
            return Box("Podloga", new Vector3(0, -0.5f, 0), new Vector3(size, 1, size), FloorMat());
        }

        /// <summary>Linia (np. tor, wykres) – LineRenderer z materiałem Unlit.</summary>
        public static LineRenderer Line(string name, Vector3[] points, Color color, float width, bool loop, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, true);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = loop;
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.widthMultiplier = width;
            lr.sharedMaterial = UnlitMat("Linia_" + ColorUtility.ToHtmlStringRGB(color), color);
            lr.shadowCastingMode = ShadowCastingMode.Off;
            return lr;
        }
    }
}
