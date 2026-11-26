#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VirtualLife.Core;
using VirtualLife.Input;
using VirtualLife.UI;
using VirtualLife.World;

namespace VirtualLife.EditorTools
{
    /// <summary>
    /// One-click Stage 1 setup: Tools > VirtualLife > Stage 1 > Run Setup.
    /// Never overwrites: existing folders, assets and scenes are skipped and reported.
    /// </summary>
    public static class VirtualLifeStage1Setup
    {
        private const string Root = "Assets/VirtualLife";
        private const string InputPath = Root + "/Settings/Input/VirtualLifeInput.inputactions";
        private const string CatalogPath = Root + "/ScriptableObjects/WorldConfig/SceneCatalog.asset";

        private static readonly string[] Folders =
        {
            "Art/Characters", "Art/Environment", "Art/Buildings", "Art/Props", "Art/Materials", "Art/UI",
            "Audio/Music", "Audio/Environment", "Audio/SFX",
            "Prefabs/Player", "Prefabs/Buildings", "Prefabs/Interactables", "Prefabs/UI", "Prefabs/Environment",
            "Scenes",
            "ScriptableObjects/PlayerConfig", "ScriptableObjects/WorldConfig", "ScriptableObjects/GraphicsConfig",
            "Scripts/Core", "Scripts/Player", "Scripts/Camera", "Scripts/World", "Scripts/Interaction",
            "Scripts/SaveSystem", "Scripts/UI", "Scripts/Environment", "Scripts/Networking",
            "Settings/Input"
        };

        private static readonly string[] SceneNames =
            { "Bootstrap", "MainMenu", "AvatarCreation", "CommunityOne", "TestEnvironment" };

        private struct Mats { public Material Ground, Road, Building, BuildingAlt; }

        [MenuItem("Tools/VirtualLife/Stage 1/Run Setup")]
        public static void RunSetup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("[Setup] Set Project Settings > Player > Active Input Handling to 'Input System Package (New)' (or Both), then restart the Editor.");
#endif
            Debug.Log($"[Setup] Unity {Application.unityVersion}. Render pipeline: " +
                      (GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "NONE (Built-in)"));

            CreateFolders();
            InputActionAsset input = CreateInputActions();
            SceneCatalog catalog = CreateCatalog();
            Mats mats = CreateMaterials();

            CreateBootstrapScene(catalog);
            CreateMainMenuScene(input, mats);
            CreateAvatarScene(input);
            CreateCommunityScene(mats);
            CreateTestScene(mats);
            RegisterBuildScenes();

            if (GraphicsSettings.currentRenderPipeline == null)
                Debug.LogWarning("[Setup] No URP asset is active. Create one (Assets > Create > Rendering > URP Asset with Universal Renderer) and assign it in Project Settings > Graphics and each Quality level.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Setup] Stage 1 setup finished. Open Scenes/Bootstrap and press Play.");
        }

        // ---------- Folders / assets ----------

        private static void CreateFolders()
        {
            foreach (string f in Folders)
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "VirtualLife", f));
            AssetDatabase.Refresh();
        }

        private static InputActionAsset CreateInputActions()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (existing != null) { Debug.Log("[Setup] Input actions already exist; kept."); return existing; }

            var a = ScriptableObject.CreateInstance<InputActionAsset>();
            a.name = "VirtualLifeInput";

            InputActionMap ui = a.AddActionMap("UI");
            InputAction nav = ui.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            nav.AddCompositeBinding("2DVector")
               .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
               .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            nav.AddBinding("<Gamepad>/leftStick");
            nav.AddBinding("<Gamepad>/dpad");
            InputAction submit = ui.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
            submit.AddBinding("<Gamepad>/buttonSouth");
            InputAction cancel = ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");
            ui.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position", expectedControlLayout: "Vector2");
            ui.AddAction("Click", InputActionType.PassThrough, "<Pointer>/press");
            ui.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");

            InputActionMap player = a.AddActionMap("Player");
            InputAction move = player.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
            InputAction look = player.AddAction("Look", InputActionType.Value, "<Pointer>/delta", expectedControlLayout: "Vector2");
            look.AddBinding("<Gamepad>/rightStick");
            player.AddAction("Jump", InputActionType.Button, "<Keyboard>/space").AddBinding("<Gamepad>/buttonSouth");
            player.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift").AddBinding("<Gamepad>/leftStickPress");
            player.AddAction("Interact", InputActionType.Button, "<Keyboard>/e").AddBinding("<Gamepad>/buttonWest");
            player.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape").AddBinding("<Gamepad>/start");

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), InputPath), a.ToJson());
            Object.DestroyImmediate(a);
            AssetDatabase.ImportAsset(InputPath);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        }

        private static SceneCatalog CreateCatalog()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SceneCatalog>(CatalogPath);
            if (existing != null) return existing;
            var catalog = ScriptableObject.CreateInstance<SceneCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static Mats CreateMaterials()
        {
            return new Mats
            {
                Ground = Mat("VL_Ground", new Color(0.36f, 0.5f, 0.3f)),
                Road = Mat("VL_Road", new Color(0.18f, 0.18f, 0.2f)),
                Building = Mat("VL_Building", new Color(0.82f, 0.8f, 0.76f)),
                BuildingAlt = Mat("VL_BuildingAlt", new Color(0.7f, 0.55f, 0.45f)),
            };
        }

        private static Material Mat(string name, Color color)
        {
            string path = $"{Root}/Art/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---------- Scenes ----------

        private static string ScenePath(string name) => $"{Root}/Scenes/{name}.unity";

        private static bool BeginScene(string name)
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ScenePath(name))))
            {
                Debug.Log($"[Setup] Scene {name} already exists; left untouched.");
                return false;
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return true;
        }

        private static void EndScene(string name)
        {
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath(name));
        }

        private static void CreateBootstrapScene(SceneCatalog catalog)
        {
            if (!BeginScene("Bootstrap")) return;
            MakeCamera("Main Camera", new Vector3(0, 1, -10), new Color(0.05f, 0.06f, 0.08f));
            var go = new GameObject("GameBootstrapper");
            var b = go.AddComponent<GameBootstrapper>();
            SetRef(b, "catalog", catalog);
            EndScene("Bootstrap");
        }

        private static void CreateMainMenuScene(InputActionAsset input, Mats mats)
        {
            if (!BeginScene("MainMenu")) return;
            SetSkyLook();
            MakeLight();
            var world = new GameObject("MenuBackdrop").transform;
            BuildSubdivision(world, mats);

            var pivot = new GameObject("CameraPivot").transform;
            pivot.position = new Vector3(0, 0, 0);
            Camera cam = MakeCamera("Main Camera", new Vector3(26, 9, 26), new Color(0.55f, 0.72f, 0.9f));
            var drift = cam.gameObject.AddComponent<MenuCameraDrift>();
            SetRef(drift, "pivot", pivot);

            Canvas canvas = MakeCanvas();
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            AddEventSystem(input);
            new GameObject("InputDeviceMonitor").AddComponent<InputDeviceMonitor>();

            var res = Res();
            // Dim gradient-ish overlay on the left for legibility.
            Image shade = MakeImage(canvas.transform, "Shade", new Color(0.03f, 0.05f, 0.09f, 0.55f));
            Anchor(shade.rectTransform, new Vector2(0, 0), new Vector2(0.42f, 1), Vector2.zero, Vector2.zero);

            Text title = MakeText(canvas.transform, "Title", "VIRTUAL LIFE", 96, TextAnchor.MiddleLeft, FontStyle.Bold);
            Anchor(title.rectTransform, new Vector2(0.05f, 0.68f), new Vector2(0.6f, 0.9f), Vector2.zero, Vector2.zero);
            Text sub = MakeText(canvas.transform, "Subtitle", "A WORLD TO LIVE IN", 28, TextAnchor.MiddleLeft, FontStyle.Normal);
            sub.color = new Color(0.7f, 0.85f, 1f);
            Anchor(sub.rectTransform, new Vector2(0.05f, 0.6f), new Vector2(0.6f, 0.69f), Vector2.zero, Vector2.zero);

            var column = new GameObject("MainPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(canvas.transform, false);
            Anchor((RectTransform)column.transform, new Vector2(0.05f, 0.2f), new Vector2(0.3f, 0.58f), Vector2.zero, Vector2.zero);
            var v = column.GetComponent<VerticalLayoutGroup>();
            v.spacing = 18; v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;

            Button enter = MakeButton(column.transform, res, "ENTER WORLD");
            Button avatar = MakeButton(column.transform, res, "AVATAR");
            Button settings = MakeButton(column.transform, res, "SETTINGS");
            Button exit = MakeButton(column.transform, res, "EXIT GAME");

            SettingsPanelController sp = BuildSettingsPanel(canvas.transform, res);

            var controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            SetRef(controller, "enterWorldButton", enter);
            SetRef(controller, "avatarButton", avatar);
            SetRef(controller, "settingsButton", settings);
            SetRef(controller, "exitButton", exit);
            SetRef(controller, "mainPanel", column);
            SetRef(controller, "settingsPanel", sp);
            SetRef(controller, "fadeGroup", group);
            EndScene("MainMenu");
        }

        private static SettingsPanelController BuildSettingsPanel(Transform parent, DefaultControls.Resources res)
        {
            var panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.11f, 0.92f);
            Anchor((RectTransform)panel.transform, new Vector2(0.28f, 0.1f), new Vector2(0.72f, 0.9f), Vector2.zero, Vector2.zero);
            var v = panel.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(48, 48, 36, 36);
            v.spacing = 10; v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;

            Row(panel.transform, MakeText(panel.transform, "Header", "SETTINGS", 48, TextAnchor.MiddleLeft, FontStyle.Bold).gameObject, 70);

            Row(panel.transform, MakeText(panel.transform, "VolumeLabel", "MASTER VOLUME", 24, TextAnchor.MiddleLeft, FontStyle.Normal).gameObject, 36);
            GameObject sliderGo = DefaultControls.CreateSlider(res);
            sliderGo.name = "MasterVolume"; sliderGo.transform.SetParent(panel.transform, false);
            Row(panel.transform, sliderGo, 40);

            Row(panel.transform, MakeText(panel.transform, "QualityLabel", "GRAPHICS QUALITY", 24, TextAnchor.MiddleLeft, FontStyle.Normal).gameObject, 36);
            GameObject quality = DefaultControls.CreateDropdown(res);
            quality.name = "QualityDropdown"; quality.transform.SetParent(panel.transform, false);
            Row(panel.transform, quality, 48);

            // Desktop-only controls
            GameObject resLabel = MakeText(panel.transform, "ResolutionLabel", "RESOLUTION", 24, TextAnchor.MiddleLeft, FontStyle.Normal).gameObject;
            Row(panel.transform, resLabel, 36);
            GameObject resDrop = DefaultControls.CreateDropdown(res);
            resDrop.name = "ResolutionDropdown"; resDrop.transform.SetParent(panel.transform, false);
            Row(panel.transform, resDrop, 48);
            GameObject toggle = DefaultControls.CreateToggle(res);
            toggle.name = "FullscreenToggle"; toggle.transform.SetParent(panel.transform, false);
            Text tl = toggle.GetComponentInChildren<Text>(); tl.text = " Fullscreen"; tl.color = Color.white; tl.fontSize = 24;
            Row(panel.transform, toggle, 44);

            // Mobile-only controls
            GameObject mobLabel = MakeText(panel.transform, "MobilePresetLabel", "MOBILE GRAPHICS PRESET", 24, TextAnchor.MiddleLeft, FontStyle.Normal).gameObject;
            Row(panel.transform, mobLabel, 36);
            GameObject mobDrop = DefaultControls.CreateDropdown(res);
            mobDrop.name = "MobilePresetDropdown"; mobDrop.transform.SetParent(panel.transform, false);
            Row(panel.transform, mobDrop, 48);

            Button back = MakeButton(panel.transform, res, "BACK");

            var sp = panel.AddComponent<SettingsPanelController>();
            SetRef(sp, "masterVolume", sliderGo.GetComponent<Slider>());
            SetRef(sp, "qualityDropdown", quality.GetComponent<Dropdown>());
            SetRef(sp, "resolutionDropdown", resDrop.GetComponent<Dropdown>());
            SetRef(sp, "fullscreenToggle", toggle.GetComponent<Toggle>());
            SetRef(sp, "mobilePresetDropdown", mobDrop.GetComponent<Dropdown>());
            SetRef(sp, "backButton", back);
            SetArray(sp, "desktopOnly", new Object[] { resLabel, resDrop, toggle });
            SetArray(sp, "mobileOnly", new Object[] { mobLabel, mobDrop });
            return sp;
        }

        private static void CreateAvatarScene(InputActionAsset input)
        {
            if (!BeginScene("AvatarCreation")) return;
            MakeCamera("Main Camera", new Vector3(0, 1, -10), new Color(0.07f, 0.09f, 0.14f));
            Canvas canvas = MakeCanvas();
            AddEventSystem(input);
            var res = Res();

            Text title = MakeText(canvas.transform, "Title", "AVATAR CUSTOMIZATION", 72, TextAnchor.MiddleCenter, FontStyle.Bold);
            Anchor(title.rectTransform, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.78f), Vector2.zero, Vector2.zero);
            Text msg = MakeText(canvas.transform, "Message", "Avatar customization will be implemented in a future stage.", 32, TextAnchor.MiddleCenter, FontStyle.Normal);
            Anchor(msg.rectTransform, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.58f), Vector2.zero, Vector2.zero);

            Button back = MakeButton(canvas.transform, res, "BACK");
            Anchor((RectTransform)back.transform, new Vector2(0.4f, 0.22f), new Vector2(0.6f, 0.32f), Vector2.zero, Vector2.zero);

            var c = new GameObject("AvatarCreationController").AddComponent<AvatarCreationController>();
            SetRef(c, "backButton", back);
            EndScene("AvatarCreation");
        }

        private static void CreateCommunityScene(Mats mats)
        {
            if (!BeginScene("CommunityOne")) return;
            SetSkyLook();
            MakeLight();
            var root = new GameObject("Graybox").transform;
            BuildSubdivision(root, mats);

            var spawn = new GameObject("SpawnPoint_Default");
            spawn.transform.position = new Vector3(0, 0.05f, -6f);
            spawn.AddComponent<SpawnPoint>();

            // Temporary camera so the scene is viewable until Stage 2 adds the player camera.
            MakeCamera("Temp Camera", new Vector3(0, 6, -16), new Color(0.55f, 0.72f, 0.9f)).transform.LookAt(spawn.transform.position);
            EndScene("CommunityOne");
        }

        private static void CreateTestScene(Mats mats)
        {
            if (!BeginScene("TestEnvironment")) return;
            SetSkyLook();
            MakeLight();
            Prim(PrimitiveType.Plane, "Ground", null, Vector3.zero, new Vector3(5, 1, 5), mats.Ground);
            MakeCamera("Main Camera", new Vector3(0, 5, -12), new Color(0.55f, 0.72f, 0.9f));
            EndScene("TestEnvironment");
        }

        /// <summary>Ground, a cross-shaped road network and placeholder buildings.</summary>
        private static void BuildSubdivision(Transform parent, Mats mats)
        {
            Prim(PrimitiveType.Plane, "Ground", parent, Vector3.zero, new Vector3(8, 1, 8), mats.Ground);
            Prim(PrimitiveType.Cube, "Road_NS", parent, new Vector3(0, 0.02f, 0), new Vector3(6, 0.04f, 60), mats.Road);
            Prim(PrimitiveType.Cube, "Road_EW", parent, new Vector3(0, 0.03f, 0), new Vector3(60, 0.04f, 6), mats.Road);
            Prim(PrimitiveType.Cube, "Road_Ring", parent, new Vector3(0, 0.025f, 24), new Vector3(60, 0.04f, 5), mats.Road);

            var rng = new System.Random(7);
            int n = 0;
            foreach (float x in new[] { -18f, -9f, 9f, 18f })
                foreach (float z in new[] { -18f, -9f, 9f, 16f })
                {
                    float h = 3f + (float)rng.NextDouble() * 3f;
                    Prim(PrimitiveType.Cube, $"House_{n++:00}", parent, new Vector3(x, h / 2f, z),
                         new Vector3(6, h, 6), n % 2 == 0 ? mats.Building : mats.BuildingAlt);
                }
        }

        // ---------- Build settings ----------

        private static void RegisterBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool Has(string p) => list.Any(s => s.path == p);

            string boot = ScenePath("Bootstrap");
            if (!Has(boot))
            {
                list.Insert(0, new EditorBuildSettingsScene(boot, true));
                Debug.Log("[Setup] Bootstrap inserted at build index 0 (required as the first scene). Existing scenes were kept after it.");
            }
            foreach (string n in SceneNames)
            {
                string p = ScenePath(n);
                if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), p)) && !Has(p))
                    list.Add(new EditorBuildSettingsScene(p, true));
            }
            if (list[0].path != boot)
                Debug.LogWarning("[Setup] Bootstrap is already in Build Settings but not first. Move it to index 0 to start from the Bootstrap scene.");
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------- Helpers ----------

        private static void SetSkyLook()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);
        }

        private static void MakeLight()
        {
            var go = new GameObject("Directional Light");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.shadows = LightShadows.None; // keep mobile-friendly; enable/bake per environment later
            l.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private static Camera MakeCamera(string name, Vector3 pos, Color bg)
        {
            var go = new GameObject(name) { tag = "MainCamera" };
            go.transform.position = pos;
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            go.AddComponent<AudioListener>();
            return cam;
        }

        private static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(t);
            g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return g;
        }

        internal static Canvas MakeCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        internal static void AddEventSystem(InputActionAsset asset)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var m = go.AddComponent<InputSystemUIInputModule>();

            // Write serialized fields directly. Using the actionsAsset setter throws in newer Input
            // System versions because the module's default actions are not part of any asset.
            var so = new SerializedObject(m);
            bool ok = SetProp(so, "m_ActionsAsset", asset);
            ok &= SetProp(so, "m_PointAction", Ref("UI/Point"));
            ok &= SetProp(so, "m_LeftClickAction", Ref("UI/Click"));
            ok &= SetProp(so, "m_MoveAction", Ref("UI/Navigate"));
            ok &= SetProp(so, "m_SubmitAction", Ref("UI/Submit"));
            ok &= SetProp(so, "m_CancelAction", Ref("UI/Cancel"));
            ok &= SetProp(so, "m_ScrollWheelAction", Ref("UI/Scroll"));
            // Not used by Stage 1 UI: clear the defaults so nothing points outside our asset.
            foreach (string unused in new[] { "m_MiddleClickAction", "m_RightClickAction",
                         "m_TrackedDevicePositionAction", "m_TrackedDeviceOrientationAction" })
                SetProp(so, unused, null);
            so.ApplyModifiedPropertiesWithoutUndo();
            if (!ok) Debug.LogWarning("[Setup] Some UI action references could not be linked; check the EventSystem's Input System UI Input Module.");
        }

        private static bool SetProp(SerializedObject so, string name, Object value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) return false;
            p.objectReferenceValue = value;
            return value != null || name.Contains("Middle") || name.Contains("Right") || name.Contains("Tracked");
        }

        internal static InputActionReference Ref(string key)
        {
            return AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.actionMap.name + "/" + r.action.name == key);
        }

        internal static DefaultControls.Resources Res()
        {
            Sprite S(string n) => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/" + n + ".psd");
            return new DefaultControls.Resources
            {
                standard = S("UISprite"), background = S("Background"), inputField = S("InputFieldBackground"),
                knob = S("Knob"), checkmark = S("Checkmark"), dropdown = S("DropdownArrow"), mask = S("UIMask")
            };
        }

        private static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        internal static Text MakeText(Transform parent, string name, string text, int size, TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = UiFont; t.text = text; t.fontSize = size; t.alignment = anchor; t.fontStyle = style;
            t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        internal static Image MakeImage(Transform parent, string name, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var i = go.GetComponent<Image>(); i.color = c; i.raycastTarget = false;
            return i;
        }

        internal static Button MakeButton(Transform parent, DefaultControls.Resources res, string label)
        {
            GameObject go = DefaultControls.CreateButton(res);
            go.name = label + "_Button";
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.2f, 0.9f);
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.55f, 0.9f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.75f);
            btn.colors = colors;
            Text t = go.GetComponentInChildren<Text>();
            t.text = label; t.color = Color.white; t.fontSize = 32; t.fontStyle = FontStyle.Bold;
            var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 76; le.minHeight = 60;
            return btn;
        }

        internal static void Row(Transform parent, GameObject go, float height)
        {
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
        }

        internal static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        internal static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Field '{field}' not found on {target.GetType().Name}."); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Setup] Field '{field}' not found on {target.GetType().Name}."); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
