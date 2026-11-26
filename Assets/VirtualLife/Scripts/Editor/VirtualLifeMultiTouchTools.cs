#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VirtualLife.Input;
using VirtualLife.UI;

namespace VirtualLife.EditorTools
{
    /// <summary>
    /// Multi-touch diagnostics and the one config fix: independent per-finger UI pointers need
    /// "&lt;Touchscreen&gt;/touch*" bindings on UI/Point and UI/Click (generic &lt;Pointer&gt; bindings only
    /// expose the primary touch). Never touches anything else.
    /// </summary>
    public static class VirtualLifeMultiTouchTools
    {
        private const string InputPath = "Assets/VirtualLife/Settings/Input/VirtualLifeInput.inputactions";
        private const string TouchPos = "<Touchscreen>/touch*/position";
        private const string TouchPress = "<Touchscreen>/touch*/press";

        [MenuItem("Tools/VirtualLife/Input/Fix UI Touch Bindings")]
        public static void FixBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (asset == null) { Debug.LogError("[MultiTouch] Input actions asset not found at " + InputPath); return; }

            bool changed = FixAction(asset, "UI/Point", "<Pointer>/position", "<Mouse>/position", "<Pen>/position", TouchPos);
            changed |= FixAction(asset, "UI/Click", "<Pointer>/press", "<Mouse>/leftButton", "<Pen>/tip", TouchPress);

            if (!changed) { Debug.Log("[MultiTouch] UI Point/Click already have per-finger touch bindings. Nothing changed."); return; }
            // Action ids are preserved by ToJson, so existing InputActionReferences stay valid.
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), InputPath), asset.ToJson());
            AssetDatabase.ImportAsset(InputPath);
            Debug.Log("[MultiTouch] UI Point/Click updated with per-finger touch bindings. Mouse, pen and gamepad bindings were kept.");
        }

        private static bool FixAction(InputActionAsset asset, string path, string genericPointer, params string[] wanted)
        {
            InputAction a = asset.FindAction(path, false);
            if (a == null) { Debug.LogError($"[MultiTouch] Action {path} not found."); return false; }

            bool changed = false;
            for (int i = a.bindings.Count - 1; i >= 0; i--)
                if (a.bindings[i].path == genericPointer) { a.ChangeBinding(i).Erase(); changed = true; }
            foreach (string p in wanted)
                if (!a.bindings.Any(b => b.path == p)) { a.AddBinding(p); changed = true; }
            return changed;
        }

        [MenuItem("Tools/VirtualLife/Input/Audit Multi-Touch Setup")]
        public static void Audit()
        {
            var sb = new StringBuilder("[MultiTouch] ===== Audit =====\n");

#if ENABLE_INPUT_SYSTEM
            sb.AppendLine("PASS  Active Input Handling includes the Input System package.");
#else
            sb.AppendLine("FAIL  Active Input Handling does NOT include the Input System package.");
#endif
            sb.AppendLine($"INFO  Default orientation: {PlayerSettings.defaultInterfaceOrientation}");

            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (asset == null) sb.AppendLine("FAIL  Input actions asset not found.");
            else
            {
                foreach (string name in new[] { "UI/Point", "UI/Click" })
                {
                    InputAction a = asset.FindAction(name, false);
                    string list = a == null ? "(missing)" : string.Join(", ", a.bindings.Select(b => b.path));
                    bool touch = a != null && a.bindings.Any(b => b.path.Contains("<Touchscreen>/touch*"));
                    bool generic = a != null && a.bindings.Any(b => b.path.StartsWith("<Pointer>"));
                    sb.AppendLine($"{(touch && !generic ? "PASS" : "FAIL")}  {name}: {list}");
                }
            }

            EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine($"{(systems.Length == 1 ? "PASS" : "WARN")}  EventSystems in open scenes: {systems.Length} (expected 1)");
            foreach (EventSystem es in systems)
            {
                var m = es.GetComponent<InputSystemUIInputModule>();
                if (m == null) { sb.AppendLine("FAIL  EventSystem has no InputSystemUIInputModule."); continue; }
                sb.AppendLine($"INFO  Pointer Behavior: {m.pointerBehavior} (multi-touch capable: default or 'All Pointers As Is')");
                sb.AppendLine($"{(m.actionsAsset == asset ? "PASS" : "WARN")}  Module actions asset: {(m.actionsAsset != null ? m.actionsAsset.name : "none")}");
                sb.AppendLine($"{(m.point != null && m.leftClick != null ? "PASS" : "FAIL")}  Module Point/Click references assigned.");
            }

            foreach (MobileControlsUI ui in Object.FindObjectsByType<MobileControlsUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(ui);
                sb.AppendLine($"{(so.FindProperty("joystick").objectReferenceValue != null ? "PASS" : "FAIL")}  MobileControlsUI.joystick assigned");
                sb.AppendLine($"{(so.FindProperty("cameraTouch").objectReferenceValue != null ? "PASS" : "FAIL")}  MobileControlsUI.cameraTouch assigned");
                SerializedProperty btns = so.FindProperty("actionButtons");
                sb.AppendLine($"{(btns.arraySize == 4 ? "PASS" : "WARN")}  Action buttons: {btns.arraySize} (expected 4)");
                Canvas c = ui.GetComponent<Canvas>();
                if (c != null)
                    sb.AppendLine($"{(c.renderMode == RenderMode.ScreenSpaceOverlay || c.worldCamera != null ? "PASS" : "FAIL")}  Canvas render mode {c.renderMode}");
            }

            int handlers = Object.FindObjectsByType<PlayerInputHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            sb.AppendLine($"INFO  PlayerInputHandler instances in open scenes: {handlers} (the player is spawned at runtime, so 0 is normal)");
            sb.AppendLine("NOTE  Real simultaneous multi-touch can only be confirmed on a physical Android device.");
            Debug.Log(sb.ToString());
        }
    }
}
#endif
