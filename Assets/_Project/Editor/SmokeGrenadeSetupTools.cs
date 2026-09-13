using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class SmokeGrenadeSetupTools
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string ItemFolder = "Assets/_Project/Art/Items/SmokeGrenade";
        internal const string ReviewFolder = ItemFolder + "/Review";
        internal const string FourStateDiagnosticPath = ReviewFolder + "/four_state_diagnostic.png";
        internal const string FlightDiagnosticPath = ReviewFolder + "/throw_release_flight_diagnostic.png";
        internal const string FourStateFinalPath = ReviewFolder + "/four_state_final.png";
        internal const string FlightFinalPath = ReviewFolder + "/throw_release_flight_final.png";
        internal const string CaptureRequestPath = ReviewFolder + "/capture_request.txt";
        internal const string CaptureFailurePath = ReviewFolder + "/capture_failure.txt";
        internal const string RightHandPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand";
        internal const string PropName = "SmokeGrenade_Prop";

        private const string ExternalModelPath = "item model/smoke shell.fbx";
        private const string ModelPath = ItemFolder + "/SmokeShell.fbx";
        private const string TextureFolder = ItemFolder + "/Textures";
        private const string MaterialFolder = ItemFolder + "/Materials";
        private const string MetallicSmoothnessPath =
            TextureFolder + "/SmokeShell_MetallicSmoothness.png";
        private const string AnimationFolder = "Assets/_Project/Animation/SmokeGrenade";
        private const string AppliedReportPath = ReviewFolder + "/applied.txt";
        private const string MotionTypeName =
            "Bellerophon.PlayerAnimation.FlashbangThrowReleaseFlightBehaviour, Assembly-CSharp";
        private const string SmokeMotionTypeName =
            "Bellerophon.PlayerAnimation.SmokeGrenadeThrowReleaseFlightBehaviour, Bellerophon.Runtime";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.05f;

        internal static readonly string[] TargetNames =
        {
            "SmokeGrenade_Idle",
            "SmokeGrenade_Throw_Aim",
            "SmokeGrenade_Throw_Release",
            "SmokeGrenade_Throw_Cancel"
        };

        internal static readonly string[] SourceNames =
        {
            "Flashbang_Idle",
            "Flashbang_Throw_Aim",
            "Flashbang_Throw_Release",
            "Flashbang_Throw_Cancel"
        };

        internal static string AbsolutePath(string assetOrProjectPath)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root, assetOrProjectPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        internal static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        internal static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        internal static Transform RequirePath(Transform root, string path)
        {
            return root.Find(path) ?? throw new InvalidOperationException(
                root.name + " is missing path " + path + ".");
        }

        internal static Transform RequireProp(GameObject target)
        {
            Transform hand = RequirePath(target.transform, RightHandPath);
            return hand.Cast<Transform>().SingleOrDefault(item => item.name == PropName) ??
                throw new InvalidOperationException(target.name + " is missing " + PropName + ".");
        }

        internal static Animator RequireAnimator(GameObject target)
        {
            return target.GetComponent<Animator>() ??
                throw new InvalidOperationException(target.name + " Animator is missing.");
        }

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            string external = AbsolutePath(ExternalModelPath);
            if (!File.Exists(external))
                throw new FileNotFoundException("Smoke shell source FBX is missing.", external);

            var report = new StringBuilder()
                .AppendLine("SmokeGrenade four-state source inspection")
                .AppendLine("sourceModelSha256=" + Sha256File(external));
            for (int index = 0; index < TargetNames.Length; index++)
            {
                GameObject source = FindUnique(scene, SourceNames[index]);
                FindUnique(scene, TargetNames[index]);
                Animator animator = RequireAnimator(source);
                if (animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException(SourceNames[index] + " controller is missing.");
                Transform hand = RequirePath(source.transform, RightHandPath);
                Transform prop = hand.Cast<Transform>()
                    .SingleOrDefault(item => item.name == "Flashbang_Prop") ??
                    throw new InvalidOperationException(SourceNames[index] + " Flashbang_Prop is missing.");
                if (prop.GetComponentsInChildren<Renderer>(true).Length == 0)
                    throw new InvalidOperationException(SourceNames[index] + " prop renderer is missing.");
                report.AppendLine(TargetNames[index] + "<-" + SourceNames[index] +
                    "|controller=" + AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) +
                    "|propPosition=" + Vec(prop.localPosition) +
                    "|propRotation=" + Quat(prop.localRotation) +
                    "|propScale=" + Vec(prop.localScale));
            }

            RequireMotionProxy(FindUnique(scene, "Flashbang_Idle"), "IdlePalmForward");
            RequireMotionProxy(FindUnique(scene, "Flashbang_Throw_Release"), "ThrowRelease");
            Debug.Log(report.ToString());
        }

        internal static void ImportAssets()
        {
            RequireEditMode();
            EnsureFolder(ItemFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(ReviewFolder);

            string external = AbsolutePath(ExternalModelPath);
            string destination = AbsolutePath(ModelPath);
            if (!File.Exists(external))
                throw new FileNotFoundException("Smoke shell source FBX is missing.", external);
            File.Copy(external, destination, true);
            AssetDatabase.ImportAsset(ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Smoke shell ModelImporter is unavailable.");
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();

            Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>()
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            bool firstExtraction = embeddedMaterials.Length > 0;
            bool extracted = firstExtraction &&
                importer.ExtractTextures(AbsolutePath(TextureFolder));
            if (firstExtraction)
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            List<Texture2D> textures = LoadTextures();
            if (textures.Count == 0 || (firstExtraction && !extracted && textures.Count < 4))
                throw new InvalidOperationException("Smoke shell embedded texture extraction failed.");

            Texture2D baseColor = FindTexture(textures, "base", "color");
            Texture2D normal = FindTexture(textures, "normal");
            Texture2D metallic = FindTexture(textures, "metallic");
            Texture2D roughness = FindTexture(textures, "roughness");
            Texture2D metallicSmoothness = CreateMetallicSmoothness(metallic, roughness);
            ConfigureTextureImporters(baseColor, normal, metallic, roughness, metallicSmoothness);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            var materialPaths = new List<string>();
            if (firstExtraction)
            {
                foreach (Material embedded in embeddedMaterials)
                {
                    string materialPath = MaterialFolder + "/" +
                        SanitizeFileName(embedded.name) + ".mat";
                    materialPaths.Add(materialPath);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (material == null)
                    {
                        material = new Material(shader) { name = embedded.name };
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    importer.AddRemap(
                        new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name),
                        material);
                }
            }
            else materialPaths.AddRange(AssetDatabase.FindAssets(
                    "t:Material", new[] { MaterialFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal));
            if (materialPaths.Count == 0)
                throw new InvalidOperationException(
                    "Smoke shell has neither embedded nor previously externalized materials.");

            // Keep the imported material slots in the FBX and remap them explicitly.
            // External mode can regenerate and overwrite a same-named material on reimport.
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.SaveAndReimport();
            foreach (string materialPath in materialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath) ??
                    throw new InvalidOperationException(
                        "External smoke shell material disappeared: " + materialPath);
                ConfigureUrpMaterial(
                    material, shader, baseColor, normal, metallicSmoothness);
            }
            AssetDatabase.SaveAssets();
            RequireEqual(Sha256File(external), Sha256File(destination),
                "source/imported smoke shell FBX hash");
            InspectImportedAppearance();
            Debug.Log("[SmokeGrenade] Exact FBX copied and all four embedded textures plus " +
                materialPaths.Count + " embedded material slot(s) externalized for URP. " +
                "firstExtraction=" + firstExtraction + ".");
        }

        private static void ConfigureUrpMaterial(
            Material material,
            Shader shader,
            Texture2D baseColor,
            Texture2D normal,
            Texture2D metallicSmoothness)
        {
            material.shader = shader;
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", 1f);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
        }

        internal static void ApplyFourStateSetup()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported smoke shell model is missing.");
            InspectImportedAppearance();
            EnsureFolder(AnimationFolder);

            string sourceModelHash = Sha256File(AbsolutePath(ExternalModelPath));
            var report = new StringBuilder()
                .AppendLine("SmokeGrenade exact Flashbang four-state setup")
                .AppendLine("sourceModelSha256=" + sourceModelHash)
                .AppendLine("animationCurvesGenerated=False")
                .AppendLine("flashbangSourcesChanged=False");

            for (int index = 0; index < TargetNames.Length; index++)
            {
                GameObject source = FindUnique(scene, SourceNames[index]);
                GameObject target = FindUnique(scene, TargetNames[index]);
                CopyExistingSkeletonPose(source.transform, target.transform);

                Animator sourceAnimator = RequireAnimator(source);
                Animator targetAnimator = target.GetComponent<Animator>() ??
                    Undo.AddComponent<Animator>(target);
                string controllerPath = AnimationFolder + "/" + TargetNames[index] + ".controller";
                AnimatorController controller = CloneControllerExact(
                    sourceAnimator.runtimeAnimatorController as AnimatorController ??
                        throw new InvalidOperationException(SourceNames[index] +
                            " does not use an AnimatorController."),
                    controllerPath,
                    TargetNames[index]);
                Undo.RecordObject(targetAnimator, "Connect exact SmokeGrenade animation copy");
                targetAnimator.runtimeAnimatorController = controller;
                targetAnimator.applyRootMotion = sourceAnimator.applyRootMotion;
                targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                targetAnimator.enabled = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetAnimator);
                EditorUtility.SetDirty(targetAnimator);

                Transform sourceHand = RequirePath(source.transform, RightHandPath);
                Transform sourceProp = sourceHand.Cast<Transform>()
                    .Single(item => item.name == "Flashbang_Prop");
                Transform targetHand = RequirePath(target.transform, RightHandPath);
                Transform targetProp = EnsureProp(targetHand, model);
                Undo.RecordObject(targetProp, "Copy Flashbang grip transform exactly");
                targetProp.localPosition = sourceProp.localPosition;
                targetProp.localRotation = sourceProp.localRotation;
                targetProp.localScale = sourceProp.localScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetProp);
                EditorUtility.SetDirty(targetProp);

                if (index == 0)
                    CopyMotionComponent(source, target, targetAnimator, targetHand, targetProp);

                report.AppendLine(TargetNames[index] + "<-" + SourceNames[index] +
                    "|controller=" + controllerPath +
                    "|motion=" + MotionSignature(DefaultMotion(controller)) +
                    "|propPosition=" + Vec(targetProp.localPosition) +
                    "|propRotation=" + Quat(targetProp.localRotation) +
                    "|propScale=" + Vec(targetProp.localScale) +
                    "|rightHandFollow=True");
            }

            RequireEqual(sourceModelHash, Sha256File(AbsolutePath(ExternalModelPath)),
                "smoke shell source FBX");
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            WriteText(AppliedReportPath, report.ToString());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[SmokeGrenade] Four exact Flashbang animation copies, grip transforms, " +
                "and right-hand model parents applied.");
        }

        internal static void InspectThrowReleaseSource()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject source = FindUnique(scene, "Flashbang_Throw_Release");
            MotionProxy motion = RequireMotionProxy(source, "ThrowRelease");
            if (motion.ReleaseTime <= 0f || motion.ReleaseTime >= motion.ClipLength)
                throw new InvalidOperationException("Flashbang release time is outside its clip.");
            if (motion.ReleaseVelocity.z <= 0f && motion.SpinRadiansPerSecond <= 0f)
                Debug.Log("[SmokeGrenade] Release runtime values initialize during Play Mode; " +
                    "serialized source configuration is present.");
            Debug.Log("[SmokeGrenade] Flashbang release source inspected. releaseTime=" +
                Num(motion.ReleaseTime) + ", clipLength=" + Num(motion.ClipLength) +
                ", spin=" + Num(motion.SpinRadiansPerSecond) + ".");
        }

        internal static void ApplyThrowReleaseFlight()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject source = FindUnique(scene, "Flashbang_Throw_Release");
            GameObject target = FindUnique(scene, "SmokeGrenade_Throw_Release");
            Animator animator = RequireAnimator(target);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Transform prop = RequireProp(target);
            CopyMotionComponent(source, target, animator, hand, prop);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            Debug.Log("[SmokeGrenade] Exact Flashbang handoff, ballistic gravity, velocity tilt, " +
                "natural spin, restore, and repeated throw behaviour copied.");
        }

        internal static void InspectFourStateSetup()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            InspectImportedAppearance();
            for (int index = 0; index < TargetNames.Length; index++)
            {
                GameObject source = FindUnique(scene, SourceNames[index]);
                GameObject target = FindUnique(scene, TargetNames[index]);
                AnimatorController sourceController = RequireAnimator(source)
                    .runtimeAnimatorController as AnimatorController ??
                    throw new InvalidOperationException(SourceNames[index] + " controller is invalid.");
                AnimatorController targetController = RequireAnimator(target)
                    .runtimeAnimatorController as AnimatorController ??
                    throw new InvalidOperationException(TargetNames[index] + " controller is invalid.");
                RequireEqual(MotionSignature(DefaultMotion(sourceController)),
                    MotionSignature(DefaultMotion(targetController)),
                    TargetNames[index] + " animation motion");
                Transform sourceProp = RequirePath(source.transform, RightHandPath)
                    .Cast<Transform>().Single(item => item.name == "Flashbang_Prop");
                Transform targetProp = RequireProp(target);
                RequireNear(sourceProp.localPosition, targetProp.localPosition,
                    PositionTolerance, TargetNames[index] + " grip position");
                if (Quaternion.Angle(sourceProp.localRotation, targetProp.localRotation) >
                    RotationTolerance)
                    throw new InvalidOperationException(TargetNames[index] +
                        " grip rotation differs from Flashbang.");
                RequireNear(sourceProp.localScale, targetProp.localScale,
                    PositionTolerance, TargetNames[index] + " grip scale");
                if (targetProp.parent != RequirePath(target.transform, RightHandPath))
                    throw new InvalidOperationException(TargetNames[index] +
                        " prop does not directly follow the right hand.");
            }

            RequireMotionProxy(FindUnique(scene, "SmokeGrenade_Idle"), "IdlePalmForward");
            RequireMotionProxy(FindUnique(scene, "SmokeGrenade_Throw_Release"), "ThrowRelease");
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SmokeGrenade] Four-state exact motion, grip, appearance, and follow " +
                "inspection passed without manipulating validation targets.");
        }

        internal static void InspectThrowReleaseFlight()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            MotionProxy source = RequireMotionProxy(
                FindUnique(scene, "Flashbang_Throw_Release"), "ThrowRelease");
            MotionProxy target = RequireMotionProxy(
                FindUnique(scene, "SmokeGrenade_Throw_Release"), "ThrowRelease");
            SerializedObject sourceSerialized = new SerializedObject(source.Component);
            SerializedObject targetSerialized = new SerializedObject(target.Component);
            GameObject targetObject = FindUnique(scene, "SmokeGrenade_Throw_Release");
            RequireReference(targetSerialized, "animator", RequireAnimator(targetObject));
            RequireReference(targetSerialized, "rightHand",
                RequirePath(targetObject.transform, RightHandPath));
            RequireReference(targetSerialized, "smokeGrenade", RequireProp(targetObject));
            string[] fields =
            {
                "heldLocalPosition", "heldLocalRotation", "heldLocalScale",
                "clipLength", "releaseTime", "launchVelocityInTransporterSpace",
                "spinRadiansPerSecond", "modelFlightAxis", "colliderCenter", "colliderSize"
            };
            foreach (string field in fields)
            {
                string sourceValue = SerializedValue(sourceSerialized.FindProperty(field));
                string targetValue = SerializedValue(targetSerialized.FindProperty(field));
                RequireEqual(sourceValue, targetValue, "release field " + field);
            }
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SmokeGrenade] Throw Release serialized flight behaviour matches " +
                "Flashbang exactly; runtime flight is checked by direct Play Mode capture.");
        }

        private static void RequireReference(
            SerializedObject serialized,
            string field,
            UnityEngine.Object expected)
        {
            SerializedProperty property = serialized.FindProperty(field) ??
                throw new InvalidOperationException("Release reference field is missing: " + field);
            if (property.objectReferenceValue != expected)
                throw new InvalidOperationException(
                    "SmokeGrenade release " + field + " points outside its target.");
        }

        private static void CopyMotionComponent(
            GameObject source,
            GameObject target,
            Animator animator,
            Transform hand,
            Transform prop)
        {
            Type flashbangType = RequireMotionType();
            MonoBehaviour sourceMotion = source.GetComponent(flashbangType) as MonoBehaviour ??
                throw new InvalidOperationException(source.name + " motion component is missing.");
            MotionProxy sourceProxy = new MotionProxy(sourceMotion);
            if (sourceProxy.ModeName == "ThrowRelease")
            {
                MonoBehaviour obsolete = target.GetComponent(flashbangType) as MonoBehaviour;
                if (obsolete != null) Undo.DestroyObjectImmediate(obsolete);
                Type smokeType = RequireSmokeMotionType();
                MonoBehaviour smokeMotion = target.GetComponent(smokeType) as MonoBehaviour;
                if (smokeMotion == null)
                    smokeMotion = Undo.AddComponent(target, smokeType) as MonoBehaviour;
                if (smokeMotion == null)
                    throw new InvalidOperationException(
                        "Could not add SmokeGrenade release motion component.");
                SerializedObject sourceSerialized = new SerializedObject(sourceMotion);
                var targetProxy = new MotionProxy(smokeMotion);
                targetProxy.Invoke(
                    "ConfigureThrow",
                    animator,
                    hand,
                    prop,
                    sourceSerialized.FindProperty("clipLength").floatValue,
                    sourceSerialized.FindProperty("releaseTime").floatValue,
                    sourceSerialized.FindProperty("launchVelocityInTransporterSpace").vector3Value,
                    sourceSerialized.FindProperty("spinRadiansPerSecond").floatValue,
                    sourceSerialized.FindProperty("modelFlightAxis").vector3Value,
                    sourceSerialized.FindProperty("colliderCenter").vector3Value,
                    sourceSerialized.FindProperty("colliderSize").vector3Value);
                smokeMotion.enabled = sourceMotion.enabled;
                PrefabUtility.RecordPrefabInstancePropertyModifications(smokeMotion);
                EditorUtility.SetDirty(smokeMotion);
                return;
            }

            MonoBehaviour targetMotion = target.GetComponent(flashbangType) as MonoBehaviour;
            if (targetMotion == null)
                targetMotion = Undo.AddComponent(target, flashbangType) as MonoBehaviour;
            if (targetMotion == null)
                throw new InvalidOperationException("Could not add copied motion component.");
            Undo.RecordObject(targetMotion, "Copy exact Flashbang motion behaviour");
            EditorUtility.CopySerialized(sourceMotion, targetMotion);
            SerializedObject serialized = new SerializedObject(targetMotion);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("rightHand").objectReferenceValue = hand;
            serialized.FindProperty("flashbang").objectReferenceValue = prop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            targetMotion.enabled = sourceMotion.enabled;
            new MotionProxy(targetMotion).Invoke("RefreshPreview");
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetMotion);
            EditorUtility.SetDirty(targetMotion);
        }

        internal static MotionProxy RequireMotionProxy(GameObject target, string mode)
        {
            Type type = target.name == "SmokeGrenade_Throw_Release"
                ? RequireSmokeMotionType()
                : RequireMotionType();
            MonoBehaviour component = target.GetComponent(type) as MonoBehaviour ??
                throw new InvalidOperationException(target.name + " motion component is missing.");
            var motion = new MotionProxy(component);
            if (motion.ModeName != mode)
                throw new InvalidOperationException(target.name + " motion mode is " +
                    motion.ModeName + ".");
            return motion;
        }

        private static Type RequireMotionType()
        {
            return Type.GetType(MotionTypeName, false) ??
                throw new InvalidOperationException(
                    "Flashbang runtime motion type is unavailable: " + MotionTypeName);
        }

        private static Type RequireSmokeMotionType()
        {
            return Type.GetType(SmokeMotionTypeName, false) ??
                throw new InvalidOperationException(
                    "SmokeGrenade runtime motion type is unavailable: " + SmokeMotionTypeName);
        }

        private static Transform EnsureProp(Transform hand, GameObject model)
        {
            Transform[] matches = hand.Cast<Transform>()
                .Where(item => item.name == PropName).ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException(hand.name + " has duplicate " + PropName + ".");
            if (matches.Length == 1) return matches[0];
            GameObject instance = PrefabUtility.InstantiatePrefab(model, hand) as GameObject ??
                throw new InvalidOperationException("Could not instantiate smoke shell model.");
            instance.name = PropName;
            return instance.transform;
        }

        private static void CopyExistingSkeletonPose(Transform source, Transform target)
        {
            Transform sourceArmature = source.Find("Armature") ??
                throw new InvalidOperationException(source.name + " Armature is missing.");
            foreach (Transform sourceBone in sourceArmature.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(sourceBone, source);
                Transform destination = target.Find(path);
                if (destination == null) continue;
                Undo.RecordObject(destination, "Copy current Flashbang skeleton baseline");
                destination.localPosition = sourceBone.localPosition;
                destination.localRotation = sourceBone.localRotation;
                destination.localScale = sourceBone.localScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
                EditorUtility.SetDirty(destination);
            }
        }

        private static AnimatorController CloneControllerExact(
            AnimatorController source,
            string destinationPath,
            string targetName)
        {
            AnimatorController destination =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(destinationPath);
            if (destination == null)
                destination = AnimatorController.CreateAnimatorControllerAtPath(destinationPath);

            destination.parameters = source.parameters.Select(parameter =>
                new AnimatorControllerParameter
                {
                    name = parameter.name,
                    type = parameter.type,
                    defaultBool = parameter.defaultBool,
                    defaultFloat = parameter.defaultFloat,
                    defaultInt = parameter.defaultInt
                }).ToArray();

            if (source.layers.Length != 1)
                throw new InvalidOperationException(source.name + " must have one exact source layer.");
            AnimatorControllerLayer sourceLayer = source.layers[0];
            ChildAnimatorState[] sourceStates = sourceLayer.stateMachine.states;
            if (sourceStates.Length != 1 || sourceLayer.stateMachine.anyStateTransitions.Length != 0 ||
                sourceLayer.stateMachine.entryTransitions.Length != 0)
                throw new InvalidOperationException(source.name +
                    " source state machine is not a single direct looping state.");

            var stateMachine = new AnimatorStateMachine
            {
                name = sourceLayer.stateMachine.name + "_SmokeGrenadeExact"
            };
            AssetDatabase.AddObjectToAsset(stateMachine, destination);
            AnimatorState sourceState = sourceStates[0].state;
            AnimatorState targetState = stateMachine.AddState(sourceState.name, sourceStates[0].position);
            var cloned = new Dictionary<Motion, Motion>();
            int clipIndex = 0;
            targetState.motion = CloneMotionExact(
                sourceState.motion, destination, destinationPath, targetName, cloned, ref clipIndex);
            targetState.speed = sourceState.speed;
            targetState.cycleOffset = sourceState.cycleOffset;
            targetState.mirror = sourceState.mirror;
            targetState.iKOnFeet = sourceState.iKOnFeet;
            targetState.writeDefaultValues = sourceState.writeDefaultValues;
            targetState.speedParameter = sourceState.speedParameter;
            targetState.speedParameterActive = sourceState.speedParameterActive;
            targetState.mirrorParameter = sourceState.mirrorParameter;
            targetState.mirrorParameterActive = sourceState.mirrorParameterActive;
            targetState.cycleOffsetParameter = sourceState.cycleOffsetParameter;
            targetState.cycleOffsetParameterActive = sourceState.cycleOffsetParameterActive;
            targetState.timeParameter = sourceState.timeParameter;
            targetState.timeParameterActive = sourceState.timeParameterActive;
            stateMachine.defaultState = targetState;

            destination.layers = new[]
            {
                new AnimatorControllerLayer
                {
                    name = sourceLayer.name,
                    avatarMask = sourceLayer.avatarMask,
                    blendingMode = sourceLayer.blendingMode,
                    defaultWeight = sourceLayer.defaultWeight,
                    iKPass = sourceLayer.iKPass,
                    syncedLayerAffectsTiming = sourceLayer.syncedLayerAffectsTiming,
                    syncedLayerIndex = sourceLayer.syncedLayerIndex,
                    stateMachine = stateMachine
                }
            };
            destination.name = targetName;
            EditorUtility.SetDirty(targetState);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(destination);
            AssetDatabase.SaveAssets();
            return destination;
        }

        private static Motion CloneMotionExact(
            Motion source,
            AnimatorController controller,
            string controllerPath,
            string targetName,
            IDictionary<Motion, Motion> cloned,
            ref int clipIndex)
        {
            if (source == null) throw new InvalidOperationException("Source motion is null.");
            if (cloned.TryGetValue(source, out Motion prior)) return prior;
            if (source is AnimationClip sourceClip)
            {
                string folder = Path.GetDirectoryName(controllerPath)?.Replace('\\', '/') ??
                    AnimationFolder;
                string path = folder + "/" + targetName + "_Motion_" +
                    clipIndex.ToString("00", CultureInfo.InvariantCulture) + ".anim";
                clipIndex++;
                AnimationClip destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (destination == null)
                {
                    destination = new AnimationClip();
                    EditorUtility.CopySerialized(sourceClip, destination);
                    AssetDatabase.CreateAsset(destination, path);
                }
                else EditorUtility.CopySerialized(sourceClip, destination);
                destination.name = sourceClip.name;
                EditorUtility.SetDirty(destination);
                cloned[source] = destination;
                RequireEqual(CurveSignature(sourceClip), CurveSignature(destination),
                    targetName + " clip " + sourceClip.name);
                return destination;
            }
            if (source is BlendTree sourceTree)
            {
                var destination = new BlendTree
                {
                    name = sourceTree.name,
                    blendType = sourceTree.blendType,
                    blendParameter = sourceTree.blendParameter,
                    blendParameterY = sourceTree.blendParameterY,
                    minThreshold = sourceTree.minThreshold,
                    maxThreshold = sourceTree.maxThreshold,
                    useAutomaticThresholds = sourceTree.useAutomaticThresholds
                };
                AssetDatabase.AddObjectToAsset(destination, controller);
                cloned[source] = destination;
                ChildMotion[] sourceChildren = sourceTree.children;
                var targetChildren = new ChildMotion[sourceChildren.Length];
                for (int index = 0; index < sourceChildren.Length; index++)
                {
                    ChildMotion child = sourceChildren[index];
                    targetChildren[index] = new ChildMotion
                    {
                        motion = CloneMotionExact(child.motion, controller, controllerPath,
                            targetName, cloned, ref clipIndex),
                        threshold = child.threshold,
                        position = child.position,
                        timeScale = child.timeScale,
                        cycleOffset = child.cycleOffset,
                        mirror = child.mirror,
                        directBlendParameter = child.directBlendParameter
                    };
                }
                destination.children = targetChildren;
                EditorUtility.SetDirty(destination);
                return destination;
            }
            throw new InvalidOperationException("Unsupported exact source motion: " + source.GetType().Name);
        }

        private static Motion DefaultMotion(AnimatorController controller)
        {
            if (controller.layers.Length != 1 ||
                controller.layers[0].stateMachine.defaultState == null)
                throw new InvalidOperationException(controller.name + " has no single default state.");
            return controller.layers[0].stateMachine.defaultState.motion ??
                throw new InvalidOperationException(controller.name + " default motion is missing.");
        }

        private static string MotionSignature(Motion motion)
        {
            if (motion is AnimationClip clip) return "Clip:" + CurveSignature(clip);
            if (motion is BlendTree tree)
            {
                var text = new StringBuilder()
                    .Append("Tree:").Append((int)tree.blendType).Append('|')
                    .Append(tree.blendParameter).Append('|').Append(tree.blendParameterY);
                foreach (ChildMotion child in tree.children)
                    text.Append("|[").Append(Vec2(child.position)).Append('|')
                        .Append(Num(child.threshold)).Append('|').Append(Num(child.timeScale))
                        .Append('|').Append(Num(child.cycleOffset)).Append('|')
                        .Append(child.mirror).Append('|').Append(child.directBlendParameter)
                        .Append('|').Append(MotionSignature(child.motion)).Append(']');
                return Sha256Text(text.ToString());
            }
            throw new InvalidOperationException("Unsupported motion signature target.");
        }

        private static string CurveSignature(AnimationClip clip)
        {
            var text = new StringBuilder()
                .Append(Num(clip.length)).Append('|').Append(Num(clip.frameRate)).Append('|')
                .Append(clip.wrapMode).Append('|').Append(clip.isLooping);
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                text.Append('|').Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|').Append(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                    text.Append('|').Append(Num(key.time)).Append(',').Append(Num(key.value))
                        .Append(',').Append(Num(key.inTangent)).Append(',')
                        .Append(Num(key.outTangent)).Append(',').Append((int)key.weightedMode)
                        .Append(',').Append(Num(key.inWeight)).Append(',').Append(Num(key.outWeight));
            }
            return Sha256Text(text.ToString());
        }

        private static void InspectImportedAppearance()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported smoke shell model is missing.");
            List<Texture2D> textures = LoadTextures();
            if (textures.Count < 5)
                throw new InvalidOperationException(
                    "Smoke shell texture set is incomplete; expected four embedded textures " +
                    "plus deterministic metallic/smoothness packing.");
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Smoke shell model has no renderer.");
            foreach (Material material in renderers.SelectMany(item => item.sharedMaterials))
            {
                if (material == null)
                    throw new InvalidOperationException("Smoke shell has an empty material slot.");
                string path = AssetDatabase.GetAssetPath(material);
                if (!path.StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Smoke shell renderer material was not externalized: " + path);
                if (material.shader == null ||
                    material.shader.name != "Universal Render Pipeline/Lit" ||
                    material.GetTexture("_BaseMap") == null ||
                    material.GetTexture("_BumpMap") == null ||
                    material.GetTexture("_MetallicGlossMap") == null)
                    throw new InvalidOperationException(
                        "Smoke shell URP material texture assignment is incomplete: " + path);
            }
        }

        private static Texture2D CreateMetallicSmoothness(Texture2D metallic, Texture2D roughness)
        {
            SetReadable(metallic, true);
            SetReadable(roughness, true);
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException(
                    "Smoke shell metallic and roughness texture sizes differ.");
            Color32[] metal = metallic.GetPixels32();
            Color32[] rough = roughness.GetPixels32();
            var outputPixels = new Color32[metal.Length];
            for (int index = 0; index < outputPixels.Length; index++)
                outputPixels[index] = new Color32(
                    metal[index].r, metal[index].r, metal[index].r,
                    (byte)(255 - rough[index].r));
            var output = new Texture2D(
                metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            output.SetPixels32(outputPixels);
            output.Apply(false, false);
            File.WriteAllBytes(AbsolutePath(MetallicSmoothnessPath), output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(MetallicSmoothnessPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicSmoothnessPath) ??
                throw new InvalidOperationException("Metallic/smoothness packing import failed.");
        }

        private static void ConfigureTextureImporters(
            Texture2D baseColor,
            Texture2D normal,
            Texture2D metallic,
            Texture2D roughness,
            Texture2D metallicSmoothness)
        {
            SetTextureType(baseColor, TextureImporterType.Default, true, false);
            SetTextureType(normal, TextureImporterType.NormalMap, false, false);
            SetTextureType(metallic, TextureImporterType.Default, false, false);
            SetTextureType(roughness, TextureImporterType.Default, false, false);
            SetTextureType(metallicSmoothness, TextureImporterType.Default, false, false);
        }

        private static void SetReadable(Texture2D texture, bool value)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(texture)) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is missing: " + texture.name);
            if (importer.isReadable == value) return;
            importer.isReadable = value;
            importer.SaveAndReimport();
        }

        private static void SetTextureType(
            Texture2D texture,
            TextureImporterType type,
            bool srgb,
            bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(
                AssetDatabase.GetAssetPath(texture)) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is missing: " + texture.name);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        private static List<Texture2D> LoadTextures()
        {
            return AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Where(item => item != null)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToList();
        }

        private static Texture2D FindTexture(IEnumerable<Texture2D> textures, params string[] words)
        {
            Texture2D match = textures.SingleOrDefault(texture =>
                AssetDatabase.GetAssetPath(texture) != MetallicSmoothnessPath &&
                words.All(word => texture.name.IndexOf(
                    word, StringComparison.OrdinalIgnoreCase) >= 0));
            return match ?? throw new InvalidOperationException(
                "Smoke shell embedded texture is missing: " + string.Join("+", words));
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("SmokeGrenade setup requires Edit Mode.");
        }

        private static void WriteText(string assetPath, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AbsolutePath(assetPath)) ??
                throw new InvalidOperationException("Output directory is unavailable."));
            File.WriteAllText(AbsolutePath(assetPath), contents, Encoding.UTF8);
        }

        private static string SerializedValue(SerializedProperty property)
        {
            if (property == null) return "<missing>";
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return property.longValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                case SerializedPropertyType.Float: return property.doubleValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.Enum: return property.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector3: return Vec(property.vector3Value);
                case SerializedPropertyType.Quaternion: return Quat(property.quaternionValue);
                default: return property.propertyPath + ":" + property.propertyType;
            }
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256Text(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "SmokeShell" : value;
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " differs. expected=" + expected +
                    ", actual=" + actual);
        }

        private static void RequireNear(Vector3 expected, Vector3 actual, float tolerance, string label)
        {
            if (Vector3.Distance(expected, actual) > tolerance)
                throw new InvalidOperationException(label + " differs. expected=" + Vec(expected) +
                    ", actual=" + Vec(actual));
        }

        internal static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        internal static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Vec2(Vector2 value) => Num(value.x) + "," + Num(value.y);
        internal static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        internal sealed class MotionProxy
        {
            internal MotionProxy(MonoBehaviour component)
            {
                Component = component;
            }

            internal MonoBehaviour Component { get; }
            internal string ModeName => Get<object>("Mode").ToString();
            internal Transform Prop => Get<Transform>("Flashbang");
            internal bool IsReleased => Get<bool>("IsReleased");
            internal float FlightElapsed => Get<float>("FlightElapsed");
            internal int ReleaseCount => Get<int>("ReleaseCount");
            internal int ResetCount => Get<int>("ResetCount");
            internal float ReleaseTime => Get<float>("ReleaseTime");
            internal float ClipLength => Get<float>("ClipLength");
            internal float CurrentClipTime => Get<float>("CurrentClipTime");
            internal Vector3 ReleaseVelocity => Get<Vector3>("ReleaseVelocity");
            internal float SpinRadiansPerSecond => Get<float>("SpinRadiansPerSecond");
            internal float MaximumHeldPositionError => Get<float>("MaximumHeldPositionError");
            internal float MaximumHeldRotationError => Get<float>("MaximumHeldRotationError");
            internal float MaximumVelocityAlignmentError =>
                Get<float>("MaximumVelocityAlignmentError");
            internal float MaximumReleaseHandoffPositionError =>
                Get<float>("MaximumReleaseHandoffPositionError");
            internal float MaximumReleaseHandoffRotationError =>
                Get<float>("MaximumReleaseHandoffRotationError");
            internal float MaximumReleaseHandoffVerticalDrop =>
                Get<float>("MaximumReleaseHandoffVerticalDrop");

            internal void Invoke(string methodName, params object[] arguments)
            {
                System.Reflection.MethodInfo method = Component.GetType().GetMethod(
                    methodName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic) ??
                    throw new InvalidOperationException(
                        Component.GetType().FullName + "." + methodName + " is missing.");
                method.Invoke(Component, arguments);
            }

            private T Get<T>(string propertyName)
            {
                System.Reflection.PropertyInfo property = Component.GetType().GetProperty(
                    propertyName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic) ??
                    throw new InvalidOperationException(
                        Component.GetType().FullName + "." + propertyName + " is missing.");
                return (T)property.GetValue(Component);
            }
        }
    }
}
