using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Puts a downloaded Mixamo character on the player. Expects the files in Assets/ThirdParty/PlayerBrian:
    /// character.fbx (the model with its skin) and Ch01_nonPBR@&lt;Name&gt;.fbx for each animation, exported
    /// "In Place" and "Without Skin". It sets the models up as Humanoid, extracts the textures and converts the
    /// materials to URP, builds the Animator Controller from the clips it finds, adds the character to the
    /// player prefab and turns the mannequin off. Only Walking is required.
    ///
    /// Clips used (the file name after the @): Idle, Walking, Running, Male Crouch Pose or Crouch Idle,
    /// Crouched Walking, Jump. Running it again replaces the character, so it can be run after adding animations.
    /// </summary>
    public static class MixamoPlayerTool
    {
        public const string Folder = "Assets/ThirdParty/PlayerBrian";
        public const string ModelPath = Folder + "/character.fbx";
        public const string TexturesFolder = Folder + "/Textures";
        public const string MaterialsFolder = Folder + "/Materials";
        public const string ControllerPath = "Assets/_Project/Art/Animation/PlayerBody.controller";

        private const float EyeHeight = 1.66f;

        /// <summary>Distance from the head joint up to the eyes.</summary>
        private const float EyesAboveHead = 0.09f;

        /// <summary>Distance from the eyes up to the top of the head, as in the player's capsule.</summary>
        private const float EyesBelowTop = 0.14f;

        /// <summary>The character stands this far behind the camera, so the chest does not fill the view when looking down.</summary>
        private const float BackOffset = 0.12f;

        /// <summary>The jump animation is played this fast, so its length is about the time in the air plus the landing.</summary>
        private const float JumpAnimationSeconds = 0.8f;

        private static readonly string[] CrouchPoseNames = { "Crouch Idle", "Male Crouch Pose" };

        [MenuItem("Tools/Cold Shutdown/Set Up Mixamo Player Body")]
        public static void SetUp()
        {
            if (!File.Exists(ModelPath))
            {
                Debug.LogError($"{ModelPath} not found. Put the Mixamo character there.");
                return;
            }

            var modelImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (modelImporter == null)
            {
                Debug.LogError($"{ModelPath} has not been imported yet. Focus the Unity Editor, wait for the import, then run this again.");
                return;
            }

            try
            {
                var avatar = ConfigureCharacter(modelImporter);
                if (avatar == null) return;

                ConvertMaterials(modelImporter);
                var clips = ConfigureAnimations(avatar);
                if (!clips.ContainsKey("Walking"))
                {
                    Debug.LogError($"No Walking animation found in {Folder} (expected a file named *@Walking.fbx).");
                    return;
                }

                // The standing eye height decides the size of the character: its eyes end up at the camera's eye height.
                clips.TryGetValue("Idle", out var idleClip);
                float standingEyes = MeasureEyeHeight(avatar, idleClip, 1f) ?? MeasureEyeHeight(avatar, null, 1f) ?? 1.65f;
                float scale = Mathf.Clamp(EyeHeight / standingEyes, 0.8f, 1.2f);

                var speeds = MeasureSpeeds(avatar, clips, scale);
                var controller = BuildController(clips, speeds);
                AddCharacterToPlayerPrefab(avatar, controller, clips, speeds, scale, standingEyes);
                AssetDatabase.SaveAssets();
                Debug.Log("Mixamo player body set up. Press Play in a sandbox scene and look down.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Setting up the Mixamo player body failed: {exception}");
            }
        }

        private static Avatar ConfigureCharacter(ModelImporter importer)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.addCollider = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isHuman)
            {
                Debug.LogError("The character could not be set up as a Humanoid. Check the rig of character.fbx.");
                return null;
            }
            return avatar;
        }

        /// <summary>Extracts the embedded textures, sizes them for a body that is seen from close but rarely, and makes URP materials.</summary>
        private static void ConvertMaterials(ModelImporter importer)
        {
            Directory.CreateDirectory(TexturesFolder);
            Directory.CreateDirectory(MaterialsFolder);

            if (!Directory.EnumerateFiles(TexturesFolder, "*.png").Any() && !Directory.EnumerateFiles(TexturesFolder, "*.jpg").Any())
            {
                if (importer.ExtractTextures(TexturesFolder))
                {
                    AssetDatabase.Refresh();
                    Debug.Log($"Textures extracted to {TexturesFolder}.");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath(path);
                string name = Path.GetFileNameWithoutExtension(path);
                textureImporter.maxTextureSize = 1024;
                if (name.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    textureImporter.textureType = TextureImporterType.NormalMap;
                }
                else if (name.IndexOf("Specular", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Gloss", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    textureImporter.sRGBTexture = false;
                }
                textureImporter.SaveAndReimport();
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var sources = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().ToList();
            foreach (var source in sources)
            {
                string path = $"{MaterialsFolder}/M_{source.name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }

                Texture baseMap = source.mainTexture;
                if (baseMap == null) baseMap = FindExtracted("Diffuse");
                Texture normalMap = source.HasProperty("_BumpMap") ? source.GetTexture("_BumpMap") : null;
                if (normalMap == null) normalMap = FindExtracted("Normal");
                material.SetTexture("_BaseMap", baseMap);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 0.25f);
                if (normalMap != null)
                {
                    material.SetTexture("_BumpMap", normalMap);
                    material.EnableKeyword("_NORMALMAP");
                }

                string lower = source.name.ToLowerInvariant();
                if (lower.Contains("hair") || lower.Contains("eyelash"))
                {
                    // Cut-out, two-sided: strands and lashes are flat cards with an alpha texture.
                    material.SetFloat("_AlphaClip", 1f);
                    material.SetFloat("_Cutoff", 0.5f);
                    material.SetFloat("_Cull", 0f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                }
                EditorUtility.SetDirty(material);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), material);
                Debug.Log($"Material {source.name}: base map {(baseMap != null ? baseMap.name : "none")}, normal map {(normalMap != null ? normalMap.name : "none")} -> {path}");
            }

            importer.SaveAndReimport();
        }

        private static Texture FindExtracted(string kind)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path).IndexOf(kind, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }
            return null;
        }

        /// <summary>Sets every Ch01_nonPBR@Name.fbx up as Humanoid on the character's avatar, and returns the clips by name.</summary>
        private static Dictionary<string, AnimationClip> ConfigureAnimations(Avatar avatar)
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string path in Directory.GetFiles(Folder, "*@*.fbx"))
            {
                string assetPath = path.Replace('\\', '/');
                string clipName = Path.GetFileNameWithoutExtension(assetPath).Split('@').Last();
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"{assetPath} has not been imported yet: focus the Unity Editor, wait, and run this again. Skipped for now.");
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importCameras = false;
                importer.importLights = false;

                var settings = importer.defaultClipAnimations;
                if (settings.Length > 0)
                {
                    var clip = settings[0];
                    clip.name = clipName;
                    // Everything repeats except the jump, which plays once.
                    clip.loopTime = !clipName.StartsWith("Jump", StringComparison.OrdinalIgnoreCase);
                    // The character stays where it is: the speed comes from the controller, not from the animation.
                    // Height and turning are baked into the pose (the hips bob, the body does not turn away), but the
                    // walking forward is NOT: it stays root motion, which the Animator ignores (Apply Root Motion off).
                    // Baking it would keep the travel inside the mesh and the body would walk away from the camera.
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = false;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                    importer.clipAnimations = new[] { clip };
                }
                importer.SaveAndReimport();

                var loaded = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (loaded != null)
                {
                    clips[clipName] = loaded;
                    Debug.Log($"Animation {clipName}: {loaded.length:0.00} s, average speed {loaded.averageSpeed.magnitude:0.00} m/s.");
                }
            }
            return clips;
        }

        /// <summary>How fast the animations move the character by themselves, in m/s; the foot sync needs them.</summary>
        private struct ClipSpeeds
        {
            public float Walk;
            public float Run;
            public float CrouchWalk;
        }

        /// <summary>
        /// Standing: Idle and a walk/run blend by Speed. Crouched (Crouch above one half): the crouch pose and
        /// crouched walking, blended by Speed. Jump: plays once from any state when the trigger is set and goes
        /// back to Idle or the walk/run blend after landing. A state without its animation is left out.
        /// </summary>
        private static AnimatorController BuildController(Dictionary<string, AnimationClip> clips, ClipSpeeds speeds)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter { name = "MotionSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter(new AnimatorControllerParameter { name = "CrouchMotionSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            controller.AddParameter(new AnimatorControllerParameter { name = "Grounded", type = AnimatorControllerParameterType.Bool, defaultBool = true });
            controller.AddParameter("Crouch", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;

            clips.TryGetValue("Running", out var running);
            clips.TryGetValue("Crouched Walking", out var crouchWalking);
            clips.TryGetValue("Jump", out var jumping);
            var crouchPose = CrouchPoseNames.Where(clips.ContainsKey).Select(name => clips[name]).FirstOrDefault();

            // Standing still.
            var idle = machine.AddState("Idle");
            if (clips.TryGetValue("Idle", out var idleClip))
            {
                idle.motion = idleClip;
            }
            else
            {
                idle.motion = clips["Walking"];
                idle.speed = 0f;
                Debug.LogWarning("No Idle animation found: standing still shows a frozen walking pose.");
            }

            // Walking and running.
            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var locomotionTree);
            locomotionTree.blendParameter = "Speed";
            locomotionTree.useAutomaticThresholds = false;
            locomotionTree.AddChild(clips["Walking"], speeds.Walk);
            if (running != null) locomotionTree.AddChild(running, speeds.Run);
            locomotion.speedParameterActive = true;
            locomotion.speedParameter = "MotionSpeed";

            machine.defaultState = idle;
            Connect(idle, locomotion, 0.12f, ("Speed", AnimatorConditionMode.Greater, 0.2f));
            Connect(locomotion, idle, 0.12f, ("Speed", AnimatorConditionMode.Less, 0.1f));

            // Crouching.
            AnimatorState crouch = null;
            if (crouchPose != null || crouchWalking != null)
            {
                crouch = controller.CreateBlendTreeInController("Crouch", out var crouchTree);
                crouchTree.blendParameter = "Speed";
                crouchTree.useAutomaticThresholds = false;
                if (crouchPose != null) crouchTree.AddChild(crouchPose, 0f);
                if (crouchWalking != null) crouchTree.AddChild(crouchWalking, speeds.CrouchWalk);
                crouch.speedParameterActive = true;
                crouch.speedParameter = "CrouchMotionSpeed";

                Connect(idle, crouch, 0.2f, ("Crouch", AnimatorConditionMode.Greater, 0.5f));
                Connect(locomotion, crouch, 0.2f, ("Crouch", AnimatorConditionMode.Greater, 0.5f));
                Connect(crouch, idle, 0.25f, ("Crouch", AnimatorConditionMode.Less, 0.5f));
            }
            else
            {
                Debug.Log("No crouch animations: crouching shows the standing animation.");
            }

            // Jumping: from any state when the trigger is set; back to the ground animations after landing.
            if (jumping != null)
            {
                var jump = machine.AddState("Jump");
                jump.motion = jumping;
                jump.speed = Mathf.Max(1f, jumping.length / JumpAnimationSeconds);

                var enter = machine.AddAnyStateTransition(jump);
                enter.hasExitTime = false;
                enter.canTransitionToSelf = false;
                enter.duration = 0.05f;
                enter.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

                var landIdle = jump.AddTransition(idle);
                landIdle.hasExitTime = true;
                landIdle.exitTime = 0.7f;
                landIdle.duration = 0.15f;
                landIdle.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
                landIdle.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");

                var landMoving = jump.AddTransition(locomotion);
                landMoving.hasExitTime = true;
                landMoving.exitTime = 0.7f;
                landMoving.duration = 0.15f;
                landMoving.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
                landMoving.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            }
            else
            {
                Debug.Log("No Jump animation: jumping shows the standing animation.");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void Connect(AnimatorState from, AnimatorState to, float duration, (string parameter, AnimatorConditionMode mode, float value) condition)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(condition.mode, condition.value, condition.parameter);
        }

        /// <summary>
        /// How fast each walking animation moves the character, in m/s. An in-place animation has no travel
        /// distance to read, so it is measured: the foot that is on the floor slides back under the body at exactly
        /// the speed the body would have to move for the foot to stay put. Falls back to typical values.
        /// </summary>
        private static ClipSpeeds MeasureSpeeds(Avatar avatar, Dictionary<string, AnimationClip> clips, float scale)
        {
            return new ClipSpeeds
            {
                Walk = SpeedOf(avatar, clips, "Walking", scale, 1.45f),
                Run = SpeedOf(avatar, clips, "Running", scale, 3.6f),
                CrouchWalk = SpeedOf(avatar, clips, "Crouched Walking", scale, 1.1f),
            };
        }

        private static float SpeedOf(Avatar avatar, Dictionary<string, AnimationClip> clips, string name, float scale, float fallback)
        {
            if (!clips.TryGetValue(name, out var clip)) return fallback;

            float recorded = clip.averageSpeed.magnitude;
            if (recorded > 0.2f)
            {
                Debug.Log($"{name}: speed {recorded:0.00} m/s (from the animation).");
                return recorded;
            }

            float? measured = MeasureStrideSpeed(avatar, clip, scale);
            if (measured.HasValue)
            {
                Debug.Log($"{name}: speed {measured.Value:0.00} m/s (measured from the planted foot).");
                return measured.Value;
            }

            Debug.LogWarning($"{name}: could not measure the speed, using {fallback:0.00} m/s. Tune walkClipSpeed / runClipSpeed / crouchClipSpeed in Play mode if the feet slide.");
            return fallback;
        }

        /// <summary>
        /// Samples a looping walk or run frame by frame on a copy of the character in a preview scene. A foot is
        /// planted while it is within 3 cm of its lowest point; its movement backwards along the character's
        /// forward axis, divided by the time, is the speed. Null if the result is not a believable walking speed.
        /// </summary>
        private static float? MeasureStrideSpeed(Avatar avatar, AnimationClip clip, float scale)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), scene);
                copy.transform.localScale = Vector3.one * scale;
                var animator = copy.GetComponent<Animator>();
                if (animator == null) animator = copy.AddComponent<Animator>();
                animator.avatar = avatar;

                var feet = new[]
                {
                    FindBone(copy, animator, HumanBodyBones.LeftFoot, "LeftFoot"),
                    FindBone(copy, animator, HumanBodyBones.RightFoot, "RightFoot"),
                };
                // Measured against the hips, not the object: the foot on the floor slides back under the hips at the
                // body's speed, however the clip carries its root.
                var hips = FindBone(copy, animator, HumanBodyBones.Hips, "Hips");
                if (hips == null || feet.Any(foot => foot == null)) return null;

                int frames = Mathf.Max(12, Mathf.RoundToInt(clip.length * 60f));
                float step = clip.length / frames;
                var heights = new float[2][];
                var distances = new float[2][];
                for (int f = 0; f < 2; f++)
                {
                    heights[f] = new float[frames + 1];
                    distances[f] = new float[frames + 1];
                }
                for (int i = 0; i <= frames; i++)
                {
                    clip.SampleAnimation(copy, i * step);
                    // The root may move while sampling; the direction the character faces is what counts.
                    var forward = copy.transform.forward;
                    for (int f = 0; f < 2; f++)
                    {
                        heights[f][i] = feet[f].position.y;
                        distances[f][i] = Vector3.Dot(feet[f].position - hips.position, forward);
                    }
                }

                float total = 0f;
                int samples = 0;
                for (int f = 0; f < 2; f++)
                {
                    float lowest = heights[f].Min();
                    for (int i = 0; i < frames; i++)
                    {
                        if (heights[f][i] < lowest + 0.03f && heights[f][i + 1] < lowest + 0.03f)
                        {
                            total += (distances[f][i] - distances[f][i + 1]) / step;
                            samples++;
                        }
                    }
                }

                if (samples == 0) return null;
                float speed = total / samples;
                return speed > 0.2f && speed < 10f ? speed : (float?)null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not measure the stride of {clip.name}: {exception.Message}");
                return null;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static Transform FindBone(GameObject model, Animator animator, HumanBodyBones bone, string nameEnding)
        {
            var found = animator.GetBoneTransform(bone);
            if (found != null) return found;

            return model.GetComponentsInChildren<Transform>().FirstOrDefault(candidate => candidate.name.EndsWith(nameEnding, StringComparison.Ordinal));
        }

        /// <summary>
        /// Height of the eyes (the head joint plus about 9 cm) for a pose, on a fresh copy of the character in a
        /// preview scene, so the real model is never left in a sampled pose. Null if the pose cannot be sampled.
        /// </summary>
        private static float? MeasureEyeHeight(Avatar avatar, AnimationClip clip, float scale)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), scene);
                copy.transform.localScale = Vector3.one * scale;
                var animator = copy.GetComponent<Animator>();
                if (animator == null) animator = copy.AddComponent<Animator>();
                animator.avatar = avatar;

                if (clip != null) clip.SampleAnimation(copy, 0f);
                var head = FindHead(copy, animator);
                return head != null ? head.position.y + EyesAboveHead : (float?)null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not measure the eye height: {exception.Message}");
                return null;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static Transform FindHead(GameObject model, Animator animator)
        {
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) return head;

            // Outside Play mode the animator may not map its bones yet; Mixamo names the bone mixamorig:Head.
            return model.GetComponentsInChildren<Transform>()
                .FirstOrDefault(bone => bone.name.EndsWith("Head", StringComparison.Ordinal) && !bone.name.EndsWith("HeadTop_End", StringComparison.Ordinal));
        }

        private static void AddCharacterToPlayerPrefab(Avatar avatar, AnimatorController controller, Dictionary<string, AnimationClip> clips, ClipSpeeds speeds, float scale, float standingEyes)
        {
            // The crouch animation decides how low the camera goes when crouching.
            float? crouchCapsule = null;
            var crouchPose = CrouchPoseNames.Where(clips.ContainsKey).Select(name => clips[name]).FirstOrDefault();
            if (crouchPose != null)
            {
                float? crouchEyes = MeasureEyeHeight(avatar, crouchPose, scale);
                if (crouchEyes.HasValue && crouchEyes.Value > 0.8f && crouchEyes.Value < 1.4f)
                {
                    crouchCapsule = crouchEyes.Value + EyesBelowTop;
                    Debug.Log($"Crouch pose: eyes at {crouchEyes.Value:0.00} m, so the crouched capsule is {crouchCapsule.Value:0.00} m tall.");
                }
                else
                {
                    Debug.LogWarning($"The crouch pose gave an eye height of {crouchEyes?.ToString("0.00") ?? "nothing"}; the crouch height stays as it is.");
                }
            }

            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabTool.PrefabPath);
            try
            {
                var old = root.transform.Find("Character");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

                var holder = new GameObject("Character").transform;
                holder.SetParent(root.transform, false);

                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), holder);
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                // Often out of the view, but the pose has to be right the moment the player looks down.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var head = FindHead(model, animator);
                if (head == null) throw new InvalidOperationException("The character has no head bone.");

                // Scaled so the eyes are at the camera's height, and the head just behind the camera.
                model.transform.localScale = Vector3.one * scale;
                var headPosition = head.position - model.transform.position;
                model.transform.localPosition = new Vector3(-headPosition.x, 0f, -BackOffset - headPosition.z);

                // The hair and the lashes would hang in front of the camera: shadow only. The head itself is scaled away at runtime.
                foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    string lower = renderer.name.ToLowerInvariant();
                    if (lower.Contains("hair") || lower.Contains("eyelash") || lower.Contains("lash"))
                    {
                        renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    }
                    renderer.updateWhenOffscreen = true;
                }

                var character = holder.gameObject.AddComponent<PlayerCharacterAnimator>();
                var serialized = new SerializedObject(character);
                serialized.FindProperty("controller").objectReferenceValue = root.GetComponent<FirstPersonController>();
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("headBone").objectReferenceValue = head;
                serialized.FindProperty("walkClipSpeed").floatValue = speeds.Walk;
                serialized.FindProperty("runClipSpeed").floatValue = speeds.Run;
                serialized.FindProperty("crouchClipSpeed").floatValue = speeds.CrouchWalk;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (crouchCapsule.HasValue)
                {
                    var player = new SerializedObject(root.GetComponent<FirstPersonController>());
                    player.FindProperty("crouchingHeight").floatValue = Mathf.Clamp(crouchCapsule.Value, 1.0f, 1.5f);
                    player.ApplyModifiedPropertiesWithoutUndo();
                }

                // The mannequin stays in the prefab, switched off: the old body can be brought back by ticking it on.
                var mannequin = root.transform.Find("Body");
                if (mannequin != null) mannequin.gameObject.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabTool.PrefabPath);
                Debug.Log($"Character added to the player prefab (scale {scale:0.00}, standing eyes {standingEyes:0.00} m before scaling). The mannequin is switched off.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
