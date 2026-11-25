#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VirtualLife.CameraRig;
using VirtualLife.Input;
using VirtualLife.Player;
using VirtualLife.UI;
using VirtualLife.World;
using S1 = VirtualLife.EditorTools.VirtualLifeStage1Setup;

namespace VirtualLife.EditorTools
{
    /// <summary>
    /// Stage 2 setup: Tools > VirtualLife > Stage 2 > Run Setup.
    /// Creates configs, animator, player prefab, and wires CommunityOne. Never overwrites existing
    /// assets, and skips the scene step if a PlayerSpawnManager is already present.
    /// </summary>
    public static class VirtualLifeStage2Setup
    {
        private const string Root = "Assets/VirtualLife";
        private const string InputPath = Root + "/Settings/Input/VirtualLifeInput.inputactions";
        private const string PlayerCfgPath = Root + "/ScriptableObjects/PlayerConfig/PlayerConfig.asset";
        private const string CameraCfgPath = Root + "/ScriptableObjects/PlayerConfig/CameraConfig.asset";
        private const string ControllerPath = Root + "/Prefabs/Player/PlayerAnimator.controller";
        private const string PrefabPath = Root + "/Prefabs/Player/Player.prefab";
        private const string ScenePath = Root + "/Scenes/CommunityOne.unity";

        [MenuItem("Tools/VirtualLife/Stage 2/Run Setup")]
        public static void Run()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ScenePath)))
            { Debug.LogError("[Setup2] CommunityOne scene not found. Run Stage 1 setup first."); return; }

            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (input == null) { Debug.LogError("[Setup2] Stage 1 input actions asset not found."); return; }
            if (!HasActions(input)) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Debug.Log(System.Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine") != null
                ? "[Setup2] Cinemachine is installed, but the custom lightweight rig is used (no new dependency)."
                : "[Setup2] Cinemachine not installed; using the custom lightweight camera rig.");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "VirtualLife/ScriptableObjects/PlayerConfig"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "VirtualLife/Prefabs/Player"));
            AssetDatabase.Refresh();

            var playerCfg = LoadOrCreate<PlayerConfig>(PlayerCfgPath);
            var cameraCfg = LoadOrCreate<CameraConfig>(CameraCfgPath);
            AnimatorController animator = CreateAnimatorController();
            PlayerController prefab = CreatePlayerPrefab(input, playerCfg, animator);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetupCommunityScene(input, cameraCfg, prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup2] Stage 2 setup finished. Start from the Bootstrap scene and press Play.");
        }

        private static bool HasActions(InputActionAsset input)
        {
            InputActionMap map = input.FindActionMap("Player", false);
            foreach (string a in new[] { "Move", "Look", "Jump", "Sprint", "Interact", "Pause" })
                if (map == null || map.FindAction(a, false) == null)
                { Debug.LogError($"[Setup2] Input action Player/{a} is missing."); return false; }
            return true;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            if (asset is CameraConfig)
            {
                // Everything except the Ignore Raycast layer (used by invisible boundary walls).
                var so = new SerializedObject(asset);
                so.FindProperty("collisionMask.m_Bits").intValue = Physics.DefaultRaycastLayers;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return asset;
        }

        // ---------- Animator (parameters + transitions only, no clips) ----------

        private static AnimatorController CreateAnimatorController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            c.AddParameter("Speed", AnimatorControllerParameterType.Float);
            c.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            c.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            c.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);
            c.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Land", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = c.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle"), walk = sm.AddState("Walking"), run = sm.AddState("Running"),
                          jump = sm.AddState("Jumping"), fall = sm.AddState("Falling"), land = sm.AddState("Landing");
            sm.defaultState = idle;

            Link(idle, walk, AnimatorConditionMode.Greater, 0.1f, "Speed");
            Link(walk, idle, AnimatorConditionMode.Less, 0.1f, "Speed");
            Link(walk, run, AnimatorConditionMode.Greater, 4.75f, "Speed");
            Link(run, walk, AnimatorConditionMode.Less, 4.75f, "Speed");

            AnimatorStateTransition anyJump = sm.AddAnyStateTransition(jump);
            anyJump.hasExitTime = false; anyJump.duration = 0.05f; anyJump.canTransitionToSelf = false;
            anyJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            AnimatorStateTransition anyFall = sm.AddAnyStateTransition(fall);
            anyFall.hasExitTime = false; anyFall.duration = 0.1f; anyFall.canTransitionToSelf = false;
            anyFall.AddCondition(AnimatorConditionMode.If, 0, "IsFalling");

            Link(fall, land, AnimatorConditionMode.If, 0, "Land");
            Link(jump, land, AnimatorConditionMode.If, 0, "Land");
            AnimatorStateTransition fallIdle = Link(fall, idle, AnimatorConditionMode.If, 0, "IsGrounded");
            fallIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFalling");
            AnimatorStateTransition landIdle = land.AddTransition(idle);
            landIdle.hasExitTime = true; landIdle.exitTime = 0.3f; landIdle.duration = 0.1f;
            return c;
        }

        private static AnimatorStateTransition Link(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string param)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false; t.duration = 0.1f;
            t.AddCondition(mode, threshold, param);
            return t;
        }

        // ---------- Player prefab ----------

        private static PlayerController CreatePlayerPrefab(InputActionAsset input, PlayerConfig config, AnimatorController animatorController)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                Debug.Log("[Setup2] Player prefab already exists; kept.");
                return existing.GetComponent<PlayerController>();
            }

            var root = new GameObject("Player");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.05f; cc.minMoveDistance = 0f;

            var handler = root.AddComponent<PlayerInputHandler>();
            var movement = root.AddComponent<PlayerMovement>();
            var state = root.AddComponent<PlayerState>();
            var anim = root.AddComponent<PlayerAnimationController>();
            var controller = root.AddComponent<PlayerController>();

            var visuals = new GameObject("Visuals"); visuals.transform.SetParent(root.transform, false);
            var avatar = new GameObject("AvatarModel"); avatar.transform.SetParent(visuals.transform, false);
            var animator = avatar.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            Material blue = Mat("VL_PlayerBody", new Color(0.2f, 0.45f, 0.85f));
            Material white = Mat("VL_PlayerMarker", Color.white);
            MakePart(PrimitiveType.Capsule, "PlaceholderBody", avatar.transform, new Vector3(0, 0.9f, 0), new Vector3(0.6f, 0.9f, 0.6f), blue);
            MakePart(PrimitiveType.Cube, "FacingMarker", avatar.transform, new Vector3(0, 1.45f, 0.3f), new Vector3(0.18f, 0.12f, 0.3f), white);

            var camTarget = new GameObject("CameraTarget"); camTarget.transform.SetParent(root.transform, false);
            var interaction = new GameObject("InteractionPoint"); interaction.transform.SetParent(root.transform, false);
            interaction.transform.localPosition = new Vector3(0f, 1f, 0.7f);

            S1.SetRef(handler, "actions", input);
            S1.SetRef(movement, "config", config);
            S1.SetRef(anim, "movement", movement);
            S1.SetRef(anim, "animator", animator);
            S1.SetRef(controller, "input", handler);
            S1.SetRef(controller, "movement", movement);
            S1.SetRef(controller, "playerState", state);
            S1.SetRef(controller, "animationController", anim);
            S1.SetRef(controller, "config", config);
            S1.SetRef(controller, "cameraTarget", camTarget.transform);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<PlayerController>();
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

        private static void MakePart(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(type);
            g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(g.GetComponent<Collider>()); // collision comes from the CharacterController only
        }

        // ---------- CommunityOne wiring ----------

        private static void SetupCommunityScene(InputActionAsset input, CameraConfig cameraCfg, PlayerController prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<PlayerSpawnManager>() != null)
            { Debug.Log("[Setup2] CommunityOne already has a PlayerSpawnManager; scene left untouched."); return; }

            var spawn = Object.FindFirstObjectByType<SpawnPoint>();
            if (spawn == null)
            {
                var g = new GameObject("SpawnPoint_Default");
                g.transform.position = new Vector3(0f, 0.05f, -6f);
                spawn = g.AddComponent<SpawnPoint>();
            }

            // Reuse the scene's temporary camera as the player camera: never two cameras.
            Camera cam = null;
            GameObject temp = GameObject.Find("Temp Camera");
            if (temp != null) cam = temp.GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null)
            {
                var g = new GameObject("Player Camera") { tag = "MainCamera" };
                cam = g.AddComponent<Camera>();
                g.AddComponent<AudioListener>();
            }
            cam.gameObject.name = "Player Camera";
            var collision = cam.gameObject.AddComponent<CameraCollisionHandler>();
            var rig = cam.gameObject.AddComponent<ThirdPersonCameraController>();
            S1.SetRef(collision, "config", cameraCfg);
            S1.SetRef(rig, "config", cameraCfg);
            S1.SetRef(rig, "collisionHandler", collision);

            BuildBoundary();

            if (Object.FindFirstObjectByType<EventSystem>() == null) S1.AddEventSystem(input);

            MobileControlsUI mobile = BuildMobileControls();
            BuildPauseMenu();

            var mgr = new GameObject("PlayerSpawnManager").AddComponent<PlayerSpawnManager>();
            S1.SetRef(mgr, "playerPrefab", prefab);
            S1.SetRef(mgr, "spawnPoint", spawn);
            S1.SetRef(mgr, "cameraRig", rig);
            S1.SetRef(mgr, "mobileControls", mobile);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Invisible walls around the 80x80 ground so the player cannot walk off the map.</summary>
        private static void BuildBoundary()
        {
            var root = new GameObject("PlayAreaBoundary");
            void Wall(string n, Vector3 center, Vector3 size)
            {
                var g = new GameObject(n);
                g.transform.SetParent(root.transform, false);
                g.transform.position = center;
                g.layer = 2; // Ignore Raycast, so the camera does not collide with these walls
                g.AddComponent<BoxCollider>().size = size;
            }
            Wall("North", new Vector3(0, 10, 40.5f), new Vector3(82, 20, 1));
            Wall("South", new Vector3(0, 10, -40.5f), new Vector3(82, 20, 1));
            Wall("East", new Vector3(40.5f, 10, 0), new Vector3(1, 20, 82));
            Wall("West", new Vector3(-40.5f, 10, 0), new Vector3(1, 20, 82));
        }

        private static MobileControlsUI BuildMobileControls()
        {
            Canvas canvas = S1.MakeCanvas();
            canvas.name = "MobileControlsCanvas";
            canvas.sortingOrder = 5;
            var ui = canvas.gameObject.AddComponent<MobileControlsUI>();

            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safe.transform.SetParent(canvas.transform, false);
            S1.Anchor((RectTransform)safe.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var touch = new GameObject("CameraTouchArea", typeof(RectTransform), typeof(Image), typeof(MobileCameraTouch));
            touch.transform.SetParent(safe.transform, false);
            S1.Anchor((RectTransform)touch.transform, new Vector2(0.35f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            touch.GetComponent<Image>().color = new Color(0, 0, 0, 0); // invisible but receives touches

            // Joystick (bottom-left)
            Image joyBg = Round(safe.transform, "Joystick", new Vector2(0, 0), new Vector2(0, 0), new Vector2(120, 110), 300, new Color(1, 1, 1, 0.22f), null, true);
            Image joyHandle = Round(joyBg.transform, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 120, new Color(1, 1, 1, 0.6f), null, false);
            var joystick = joyBg.gameObject.AddComponent<VirtualJoystick>();
            S1.SetRef(joystick, "background", joyBg.rectTransform);
            S1.SetRef(joystick, "handle", joyHandle.rectTransform);

            var buttons = new MobileActionButton[4];
            buttons[0] = ActionButton(safe.transform, "JumpButton", MobileAction.Jump, new Vector2(1, 0), new Vector2(-110, 130), 190, "JUMP");
            buttons[1] = ActionButton(safe.transform, "SprintButton", MobileAction.Sprint, new Vector2(1, 0), new Vector2(-340, 100), 140, "RUN");
            buttons[2] = ActionButton(safe.transform, "InteractButton", MobileAction.Interact, new Vector2(1, 0), new Vector2(-130, 350), 130, "USE");
            buttons[3] = ActionButton(safe.transform, "PauseButton", MobileAction.Pause, new Vector2(1, 1), new Vector2(-30, -30), 100, "II");

            S1.SetRef(ui, "visualRoot", safe);
            S1.SetRef(ui, "joystick", joystick);
            S1.SetRef(ui, "cameraTouch", touch.GetComponent<MobileCameraTouch>());
            S1.SetArray(ui, "actionButtons", buttons);
            return ui;
        }

        private static MobileActionButton ActionButton(Transform parent, string name, MobileAction action, Vector2 corner, Vector2 pos, float size, string label)
        {
            Vector2 pivot = corner; // anchor the button's own corner to the screen corner
            Image img = Round(parent, name, corner, pivot, pos, size, new Color(1, 1, 1, 0.35f), label, true);
            var btn = img.gameObject.AddComponent<MobileActionButton>();
            var so = new SerializedObject(btn);
            so.FindProperty("action").enumValueIndex = (int)action;
            so.FindProperty("image").objectReferenceValue = img;
            so.ApplyModifiedPropertiesWithoutUndo();
            return btn;
        }

        private static Image Round(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, float size, Color color, string label, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            img.color = color; img.raycastTarget = raycast;
            if (label != null)
            {
                Text t = S1.MakeText(go.transform, "Label", label, Mathf.RoundToInt(size * 0.24f), TextAnchor.MiddleCenter, FontStyle.Bold);
                S1.Anchor(t.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            return img;
        }

        private static void BuildPauseMenu()
        {
            Canvas canvas = S1.MakeCanvas();
            canvas.name = "PauseCanvas";
            canvas.sortingOrder = 20;
            var ui = canvas.gameObject.AddComponent<PauseMenuUI>();
            var res = S1.Res();

            var panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            S1.Anchor((RectTransform)panel.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.7f); // also blocks touches behind it

            var box = new GameObject("Box", typeof(RectTransform), typeof(VerticalLayoutGroup));
            box.transform.SetParent(panel.transform, false);
            S1.Anchor((RectTransform)box.transform, new Vector2(0.34f, 0.25f), new Vector2(0.66f, 0.75f), Vector2.zero, Vector2.zero);
            var v = box.GetComponent<VerticalLayoutGroup>();
            v.spacing = 18; v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;

            S1.Row(box.transform, S1.MakeText(box.transform, "Title", "PAUSED", 64, TextAnchor.MiddleCenter, FontStyle.Bold).gameObject, 100);
            Button resume = S1.MakeButton(box.transform, res, "RESUME");
            Button menu = S1.MakeButton(box.transform, res, "MAIN MENU");

            S1.SetRef(ui, "panel", panel);
            S1.SetRef(ui, "resumeButton", resume);
            S1.SetRef(ui, "mainMenuButton", menu);
            panel.SetActive(false);
        }
    }
}
#endif
