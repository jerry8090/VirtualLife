#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using VirtualLife.Player;

namespace VirtualLife.EditorTools
{
    /// <summary>
    /// Replaces ONLY the placeholder visual in the existing Player prefab with a Mixamo humanoid.
    /// Tools > VirtualLife > Character > Integrate Mixamo Model.
    /// Backs up the prefab first, never touches the Player root components, the CharacterController,
    /// input, camera/interaction objects, or the original animator controller asset.
    /// </summary>
    public static class VirtualLifeMixamoIntegration
    {
        private const string Root = "Assets/VirtualLife";
        private const string PrefabPath = Root + "/Prefabs/Player/Player.prefab";
        private const string BackupFolder = Root + "/Prefabs/Player/Backups";
        private const float CapsuleHeight = 1.8f; // matches the CharacterController height

        [MenuItem("Tools/VirtualLife/Character/Integrate Mixamo Model")]
        public static void Integrate()
        {
            var log = new StringBuilder("[Mixamo] ===== Integration report =====\n");
            try { Run(log); }
            catch (Exception e) { log.AppendLine("ERROR  " + e.Message); Debug.LogException(e); }
            Debug.Log(log.ToString());
        }

        private static void Run(StringBuilder log)
        {
            // 1) Locate the model (selected FBX wins, otherwise search by name).
            string modelPath = FindModelPath();
            if (modelPath == null)
            {
                log.AppendLine("ERROR  No Mixamo FBX found. Import WalkingInPlace.fbx (e.g. into Assets/VirtualLife/Art/Characters), or select it and run again.");
                return;
            }
            log.AppendLine("INFO  Model: " + modelPath);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            { log.AppendLine("ERROR  Player prefab not found at " + PrefabPath); return; }

            // 2) Verify Rig = Humanoid and clip looping. Changes importer settings only after confirmation.
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) { log.AppendLine("ERROR  Asset is not a model."); return; }

            bool needsHumanoid = importer.animationType != ModelImporterAnimationType.Human;
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            bool needsLoop = clips.Any(c => !c.loopTime);
            if (needsHumanoid || needsLoop)
            {
                string what = (needsHumanoid ? $"Rig is '{importer.animationType}' (needs Humanoid). " : "") +
                              (needsLoop ? "Animation clip is not set to Loop Time. " : "");
                if (!EditorUtility.DisplayDialog("Mixamo import settings", what + "\n\nApply the fix to the FBX import settings?", "Apply", "Cancel"))
                { log.AppendLine("ABORT  Import settings left unchanged by user choice."); return; }
                if (needsHumanoid)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                }
                if (needsLoop)
                {
                    for (int i = 0; i < clips.Length; i++) clips[i].loopTime = true;
                    importer.clipAnimations = clips;
                }
                importer.SaveAndReimport();
                log.AppendLine("FIXED  FBX import settings updated (Humanoid / Loop Time).");
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            { log.AppendLine("ERROR  The FBX has no valid Humanoid Avatar. Open its Rig tab > Configure and fix the bone mapping."); return; }
            log.AppendLine("PASS  Humanoid Avatar is valid: " + avatar.name);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            AnimationClip walk = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                                              .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

            // 3) Backup before any prefab change.
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), BackupFolder));
            AssetDatabase.Refresh();
            string backup = AssetDatabase.GenerateUniqueAssetPath($"{BackupFolder}/Player_BACKUP_{DateTime.Now:yyyyMMdd_HHmmss}.prefab");
            if (!AssetDatabase.CopyAsset(PrefabPath, backup)) { log.AppendLine("ERROR  Could not create backup; nothing was changed."); return; }
            log.AppendLine("PASS  Backup created: " + backup);

            // 4) Edit the prefab contents.
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { EditPrefab(root, modelAsset, avatar, walk, log); }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            log.AppendLine("NOT TESTED  Runtime checks (visibility in Play Mode, movement, camera, PC and Android controls, console while playing).");
            log.AppendLine("Restore with: copy the backup over Player.prefab, or revert in version control.");
        }

        private static void EditPrefab(GameObject root, GameObject modelAsset, Avatar avatar, AnimationClip walk, StringBuilder log)
        {
            Transform avatarModel = FindDeep(root.transform, "AvatarModel");
            if (avatarModel == null) { log.AppendLine("ERROR  'AvatarModel' not found in the Player prefab; nothing changed."); return; }
            if (avatarModel.Find("MixamoModel") != null) { log.AppendLine("SKIP  MixamoModel already present; prefab unchanged."); return; }

            var pac = root.GetComponent<PlayerAnimationController>();
            if (pac == null) { log.AppendLine("ERROR  PlayerAnimationController missing on the Player root; nothing changed."); return; }

            Animator old = avatarModel.GetComponent<Animator>();
            RuntimeAnimatorController controller = old != null ? old.runtimeAnimatorController : null;
            if (controller == null) log.AppendLine("WARN  The existing Animator has no controller; the new Animator will have none either.");

            // Optional: a COPY of the controller with the imported walking clip, original stays untouched.
            string basePath = controller != null ? AssetDatabase.GetAssetPath(controller) : null;
            if (walk != null && controller is AnimatorController && !string.IsNullOrEmpty(basePath) &&
                EditorUtility.DisplayDialog("Animator controller",
                    "Your PlayerAnimator has states but no animation clips, so the model would stay in its T-pose.\n\n" +
                    "Create a COPY of the controller with the imported walking clip on the Walking and Running states (Running reuses it at 1.5x as a stand-in)? The original controller is not modified. Idle stays empty until you add an Idle clip.",
                    "Create copy", "Keep original"))
            {
                controller = CreateControllerCopy(basePath, walk, log);
            }

            Transform placeholder = avatarModel.Find("PlaceholderBody");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, avatarModel);
            instance.name = "MixamoModel";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Exactly one active Animator: the new one on the model. The old one is disabled, not deleted.
            Animator anim = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false; // movement is owned by CharacterController
            if (old != null) old.enabled = false;

            if (placeholder != null) UnityEngine.Object.DestroyImmediate(placeholder.gameObject);
            else log.AppendLine("INFO  PlaceholderBody was not found (already removed).");

            // Scale / ground alignment check against the 1.8 m capsule.
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) log.AppendLine("FAIL  The model has no renderers.");
            else
            {
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
                float height = b.size.y;
                log.AppendLine($"INFO  Model height {height:0.00} m (capsule {CapsuleHeight:0.00} m), feet offset {b.min.y - root.transform.position.y:0.000} m, skinned meshes: {instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length}");

                if (height > 0.01f && Mathf.Abs(height - CapsuleHeight) / CapsuleHeight > 0.15f)
                {
                    float s = CapsuleHeight / height;
                    instance.transform.localScale = Vector3.one * s;
                    log.AppendLine($"FIXED  Model scale was off by more than 15%; applied uniform scale {s:0.###} on MixamoModel only.");
                    b = renderers[0].bounds; foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
                }
                float feet = b.min.y - root.transform.position.y;
                if (Mathf.Abs(feet) > 0.05f)
                {
                    instance.transform.localPosition += Vector3.down * feet;
                    log.AppendLine($"FIXED  Feet were {feet:0.00} m off the player root; MixamoModel shifted to ground level.");
                }
            }

            FixMaterialsForRenderPipeline(renderers, log);

            var so = new SerializedObject(pac);
            SerializedProperty animProp = so.FindProperty("animator");
            if (animProp == null) { log.AppendLine("ERROR  PlayerAnimationController has no 'animator' field."); return; }
            animProp.objectReferenceValue = anim;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(anim);

            int activeAnimators = root.GetComponentsInChildren<Animator>(true).Count(a => a.enabled);
            log.AppendLine($"{(activeAnimators == 1 ? "PASS" : "FAIL")}  Enabled Animators in prefab: {activeAnimators} (expected 1)");
            log.AppendLine("PASS  PlayerAnimationController.animator -> MixamoModel Animator; Apply Root Motion = off.");
            log.AppendLine("INFO  Untouched: Player root components, CharacterController, CameraTarget, InteractionPoint, FacingMarker" +
                           (FindDeep(root.transform, "FacingMarker") != null ? " (note: it may poke through the new model's head; hide its renderer if so)." : "."));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            log.AppendLine("SAVED  " + PrefabPath);
        }

        /// <summary>FBX materials can import with a non-URP shader (pink). Creates URP copies, never overwrites.</summary>
        private static void FixMaterialsForRenderPipeline(Renderer[] renderers, StringBuilder log)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;

            foreach (Renderer r in renderers)
            {
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    if (m == null || (m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline"))) continue;
                    string path = AssetDatabase.GenerateUniqueAssetPath($"{Root}/Art/Materials/VL_Mixamo_{m.name}.mat");
                    var copy = new Material(lit);
                    if (m.HasProperty("_Color")) copy.color = m.color;
                    if (m.HasProperty("_MainTex") && m.mainTexture != null) copy.mainTexture = m.mainTexture;
                    AssetDatabase.CreateAsset(copy, path);
                    mats[i] = copy; changed = true;
                    log.AppendLine($"FIXED  '{m.name}' used shader '{(m.shader != null ? m.shader.name : "none")}'; created URP material {path}");
                }
                if (changed) { r.sharedMaterials = mats; PrefabUtility.RecordPrefabInstancePropertyModifications(r); }
            }
        }

        private static AnimatorController CreateControllerCopy(string basePath, AnimationClip walk, StringBuilder log)
        {
            string dst = AssetDatabase.GenerateUniqueAssetPath(Root + "/Prefabs/Player/PlayerAnimator_Mixamo.controller");
            if (!AssetDatabase.CopyAsset(basePath, dst)) { log.AppendLine("WARN  Could not copy the controller; keeping the original."); return AssetDatabase.LoadAssetAtPath<AnimatorController>(basePath); }
            var copy = AssetDatabase.LoadAssetAtPath<AnimatorController>(dst);
            foreach (ChildAnimatorState cs in copy.layers[0].stateMachine.states)
            {
                if (cs.state.name == "Walking") cs.state.motion = walk;
                else if (cs.state.name == "Running") { cs.state.motion = walk; cs.state.speed = 1.5f; }
            }
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();
            log.AppendLine($"INFO  Controller copy created: {dst} (Walking/Running use '{walk.name}'; parameters and transitions unchanged).");
            return copy;
        }

        private static string FindModelPath()
        {
            string selected = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (!string.IsNullOrEmpty(selected) && AssetImporter.GetAtPath(selected) is ModelImporter) return selected;
            string guid = AssetDatabase.FindAssets("WalkingInPlace t:Model").FirstOrDefault();
            return guid != null ? AssetDatabase.GUIDToAssetPath(guid) : null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
#endif
