using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bellerophon.PlayerConsumables;
using Bellerophon.PlayerHands;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    [InitializeOnLoad]
    internal static class ConsumableRiggedSharedMotionTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ControllerFolder =
            "Assets/_Project/Art/Player/Animations/Consumable";
        private const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/Consumable/ConsumableRiggedShared.controller";
        private const string OutputFolder =
            "docs/validation/consumable_item_grips_2026-09-09";
        private const string BatteryPath = "Assets/_Project/Art/Items/Consumable/AuxiliaryBattery/AuxiliaryBattery.fbx";
        private const string BatteryFolder = "Assets/_Project/Art/Items/Consumable/AuxiliaryBattery";
        private const string RigRootName = "ConsumableRiggedSharedRig";
        private const string LeftTargetName = "LeftHandTarget";
        private const string LeftControlName = "LeftArmRotationControl";
        private const string SocketName = "LeftWristDorsalSocket";
        private const string RightTargetName = "RightHandTarget";
        private const string GripName = "AuxiliaryBatteryGrip";
        private const string CandidateReviewRoot = "ConsumableHandWeightReview";
        private const string CandidateReviewPending = "Bellerophon.Consumable.HandWeightReview";
        private const string CandidateReviewFolder = "Assets/_Project/Art/Player/HandsRig";
        private static GameObject candidateReviewRoot;
        private static readonly Dictionary<string, GameObject> candidateReviewTargets = new Dictionary<string, GameObject>();

        static ConsumableRiggedSharedMotionTools()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if(state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(CandidateReviewPending, false))
                {
                    SessionState.EraseBool(CandidateReviewPending);
                    candidateReviewRoot = new GameObject(CandidateReviewRoot) { hideFlags = HideFlags.DontSave };
                    foreach(string name in TargetNames)
                    {
                        var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CandidateReviewFolder + "/WeightReview_" + name + ".prefab"), candidateReviewRoot.transform);
                        instance.name = name;
                        candidateReviewTargets[name] = instance;
                    }
                }
                if(state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                {
                    if(candidateReviewRoot != null) UnityEngine.Object.DestroyImmediate(candidateReviewRoot);
                    candidateReviewRoot = null; candidateReviewTargets.Clear();
                    SessionState.EraseBool(CandidateReviewPending);
                }
            };
        }

        internal static void PrepareConsumableHandWeightReview()
            => PrepareConsumableHandReview(false);

        internal static void PrepareConsumableHandTopologyReview()
            => PrepareConsumableHandReview(true);

        private static void PrepareConsumableHandReview(bool topologyRepair)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author candidate prefabs only in Edit Mode.");
            PlayerHandRigTools.RefreshPlayerHandWeightReview(topologyRepair);
            var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerHandRigTools.CandidatePath).GetComponentInChildren<SkinnedMeshRenderer>();
            var report = new StringBuilder("Candidate-only prefabs; current four scene models and shared source are unchanged.\n");
            foreach(string name in TargetNames)
            {
                GameObject source = FindUnique(RequireScene(), name);
                string before = TargetSnapshot(source);
                GameObject copy = UnityEngine.Object.Instantiate(source);
                try
                {
                    copy.name = name;
                    // Placement belongs to the authored disposable preview, not observation.
                    copy.transform.position += new Vector3(0, 100, 0);
                    var skin = copy.GetComponentInChildren<SkinnedMeshRenderer>();
                    var names = skin.bones.ToDictionary(b => b.name);
                    skin.sharedMesh = candidate.sharedMesh;
                    skin.bones = candidate.bones.Select(b => names[b.name]).ToArray();
                    var reviewedPose=AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(CandidateReviewFolder+"/WeightReview_"+name+"GripPose.asset");
                    if(reviewedPose!=null)
                    {
                        var motion=copy.GetComponent<ConsumableRiggedSharedMotion>();
                        foreach(var joint in reviewedPose.Joints.Where(j=>j.BoneName.StartsWith("Right",StringComparison.Ordinal)))
                            RequireDescendant(motion.RightHandTarget,joint.BoneName+"GripControl").localRotation=joint.LocalRotation;
                    }
                    PrefabUtility.SaveAsPrefabAsset(copy, CandidateReviewFolder + "/WeightReview_" + name + ".prefab");
                    report.AppendLine(name + " unchangedMotionAndGrip=True candidateMesh=" + AssetDatabase.GetAssetPath(skin.sharedMesh));
                }
                finally { UnityEngine.Object.DestroyImmediate(copy); }
                if(TargetSnapshot(source) != before) throw new InvalidOperationException("Candidate authoring modified a scene target.");
            }
            File.WriteAllText(Absolute(OutputFolder + "/candidate_preview_prepared.txt"), report.ToString());
        }

        internal static void EnterConsumableHandWeightReview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop the current review before entering the candidate preview.");
            SessionState.SetBool(CandidateReviewPending, true);
            EditorApplication.EnterPlaymode();
        }

        private static GameObject ObservationTarget(string name) => candidateReviewTargets.TryGetValue(name, out var target)
            ? target : FindUnique(RequireScene(), name);

        internal static void RefitConsumableHandWeightReview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Refit candidate assets only in Edit Mode.");
            var report=new StringBuilder("Candidate-only grip authoring: real skin surface, anatomical thumb axis, contact and deformation terms.\n");
            foreach(string name in TargetNames)
            {
                string path=CandidateReviewFolder+"/WeightReview_"+name+".prefab";
                GameObject instance=PrefabUtility.LoadPrefabContents(path);
                PlayerHandGripPose profile=null;
                try
                {
                    instance.name=name;
                    var motion=instance.GetComponent<ConsumableRiggedSharedMotion>();
                    string sourcePath=Array.IndexOf(TargetNames,name)==0?CandidateReviewFolder+"/SharedBatteryGripPose.asset":ProfilePath(RequestedItemNames[Array.IndexOf(TargetNames,name)-1]);
                    profile=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(sourcePath));
                    FitRequestedGrip(motion,motion.BatteryGrip.GetComponentInChildren<MeshFilter>(),profile,name=="Nanomachine_Inject",report,true);
                    profile.name="WeightReview_"+name+"GripPose";
                    string posePath=CandidateReviewFolder+"/"+profile.name+".asset";
                    var stored=AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(posePath);
                    if(stored==null) {AssetDatabase.CreateAsset(profile,posePath);stored=profile;profile=null;}
                    else EditorUtility.CopySerialized(profile,stored);
                    EditorUtility.SetDirty(stored);AssetDatabase.SaveAssetIfDirty(stored);
                    foreach(var joint in stored.Joints.Where(j=>j.BoneName.StartsWith("Right",StringComparison.Ordinal)))
                        RequireDescendant(motion.RightHandTarget,joint.BoneName+"GripControl").localRotation=joint.LocalRotation;
                    PrefabUtility.SaveAsPrefabAsset(instance,path);
                    File.WriteAllText(Absolute(OutputFolder+"/candidate_grip_authoring.txt"),report.ToString()+"completed="+name+"\n");
                }
                finally{if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);PrefabUtility.UnloadPrefabContents(instance);}
            }
            File.WriteAllText(Absolute(OutputFolder+"/candidate_grip_authoring.txt"),report+"authoring=COMPLETE; directReviewRequired=True\n");
        }

        internal static string[] RightFingerGripAssetPaths()=>new[]{CandidateReviewFolder+"/SharedBatteryGripPose.asset"}
            .Concat(RequestedItemNames.Select(ProfilePath)).ToArray();

        internal static void ApplyReviewedRightFingerGrips()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply reviewed grips only in Edit Mode.");
            string[] paths=RightFingerGripAssetPaths();
            for(int i=0;i<TargetNames.Length;i++)
            {
                var motion=FindUnique(RequireScene(),TargetNames[i]).GetComponent<ConsumableRiggedSharedMotion>();
                var reviewed=AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(CandidateReviewFolder+"/WeightReview_"+TargetNames[i]+"GripPose.asset");
                var actual=AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(paths[i]);
                if(reviewed==null||actual==null)throw new InvalidOperationException("A reviewed right-hand grip is missing.");
                var right=reviewed.Joints.Where(j=>j.BoneName.StartsWith("Right",StringComparison.Ordinal)).ToDictionary(j=>j.BoneName);
                if(right.Count!=15)throw new InvalidOperationException("Expected the existing fifteen right finger controls.");
                Undo.RegisterCompleteObjectUndo(actual,"Reviewed right finger grip");
                actual.Joints=actual.Joints.Select(j=>right.TryGetValue(j.BoneName,out var replacement)?replacement:j).ToArray();
                foreach(var joint in right.Values)
                {
                    Transform control=RequireDescendant(motion.RightHandTarget,joint.BoneName+"GripControl");
                    Undo.RecordObject(control,"Reviewed right finger control");control.localRotation=joint.LocalRotation;
                    EditorUtility.SetDirty(control);
                }
                EditorUtility.SetDirty(actual);AssetDatabase.SaveAssetIfDirty(actual);
            }
        }

        private static readonly string[] RequestedItemNames =
            { "ShieldSwitch", "RapidProtectiveBarrierBuffer", "NanomachineTherapeutics" };
        private static string RequestedItemPath(string name) =>
            "Assets/_Project/Art/Items/Consumable/" + name + "/" + name + ".fbx";

        private static readonly float[] ReviewTimes =
        {
            0f, 0.25f, 0.5f, 0.75f, 1f, 1.1f, 1.2f, 1.45f,
            1.7f, 2.3f, 3f, 3.25f, 3.5f, 4.2f, 4.95f, 5f
        };

        private static readonly string[] TargetNames =
        {
            "Battery_Aux_Plug",
            "Converter_Shield_Plug",
            "Buffer_Shield_Plug",
            "Nanomachine_Inject"
        };

        internal static void InspectConsumableRiggedMotionSources()
        {
            Scene scene = RequireScene();
            Type rigBuilderType = RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            Type rigType = RequireRiggingType("UnityEngine.Animations.Rigging.Rig");
            Type twoBoneType = RequireRiggingType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint");
            Type chainIkType = RequireRiggingType("UnityEngine.Animations.Rigging.ChainIKConstraint");

            var report = new StringBuilder();
            report.AppendLine("Consumable rigged shared-motion source inspection");
            report.AppendLine("scene=" + scene.path);
            report.AppendLine("rigBuilderType=" + rigBuilderType.AssemblyQualifiedName);
            report.AppendLine("rigType=" + rigType.AssemblyQualifiedName);
            report.AppendLine("twoBoneType=" + twoBoneType.AssemblyQualifiedName);
            report.AppendLine("chainIkType=" + chainIkType.AssemblyQualifiedName);

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Animator[] animators = target.GetComponentsInChildren<Animator>(true);
                Transform model = animators.Length == 1 ? animators[0].transform : FindModelRoot(target.transform);
                report.AppendLine();
                report.AppendLine("target=" + targetName);
                report.AppendLine("rootLocalPosition=" + Format(target.transform.localPosition));
                report.AppendLine("rootLocalRotation=" + Format(target.transform.localRotation.eulerAngles));
                report.AppendLine("rootLocalScale=" + Format(target.transform.localScale));
                report.AppendLine("animatorCount=" + animators.Length);
                report.AppendLine("rigBuilderCount=" + target.GetComponentsInChildren(rigBuilderType, true).Length);
                report.AppendLine("rigRootCount=" + target.GetComponentsInChildren<Transform>(true)
                    .Count(item => item.name == RigRootName));
                report.AppendLine("rendererSignature=" + RendererSignature(target));
                if (animators.Length == 1)
                    report.AppendLine("controller=" + AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController));
                AppendRigMeasurements(report, model);
            }

            Directory.CreateDirectory(Absolute(OutputFolder));
            foreach (string name in RequestedItemNames) InspectRequestedItem(name, report);
            File.WriteAllText(
                Absolute(OutputFolder + "/source_inspection.txt"),
                report.ToString(),
                new UTF8Encoding(false));
            Debug.Log("Consumable rigged shared-motion sources inspected.");
        }

        private static void InspectRequestedItem(string name, StringBuilder report) => InspectBatteryAsset(report, name);

        private static void InspectBatteryAsset(StringBuilder report, string itemName = "AuxiliaryBattery")
        {
            string path = RequestedItemPath(itemName);
            PrepareBatteryImport(path, itemName);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new InvalidOperationException("Battery asset is not imported.");
            report.AppendLine("itemAsset=" + path);
            foreach (Transform t in asset.GetComponentsInChildren<Transform>(true))
                report.AppendLine("batteryNode=" + HierarchyPath(t, asset.transform) + " position=" + Format(t.localPosition) +
                    " rotation=" + Format(t.localEulerAngles) + " scale=" + Format(t.localScale));
            foreach (MeshFilter filter in asset.GetComponentsInChildren<MeshFilter>(true))
            {
                report.AppendLine("batteryMesh=" + filter.name + " vertices=" + filter.sharedMesh.vertexCount +
                    " boundsCenter=" + Format(filter.sharedMesh.bounds.center) + " boundsSize=" + Format(filter.sharedMesh.bounds.size));
                Vector3[] points = filter.sharedMesh.vertices;
                for (int axis = 0; axis < 3; axis++)
                {
                    float lo = points.Min(v => v[axis]), hi = points.Max(v => v[axis]);
                    for (int bin = 0; bin < 10; bin++)
                    {
                        Vector3[] slice = points.Where(v => v[axis] >= Mathf.Lerp(lo, hi, bin / 10f) &&
                            v[axis] <= Mathf.Lerp(lo, hi, (bin + 1) / 10f)).ToArray();
                        if (slice.Length == 0) continue;
                        Bounds b = new Bounds(slice[0], Vector3.zero); foreach (Vector3 v in slice) b.Encapsulate(v);
                        report.AppendLine("slice axis=" + axis + " bin=" + bin + " n=" + slice.Length + " center=" + Format(b.center) + " size=" + Format(b.size));
                    }
                }
            }
            foreach (Material material in asset.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
            {
                report.AppendLine("batteryMaterial=" + material.name + " shader=" + material.shader.name);
                foreach (string property in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(property);
                    report.AppendLine("texture=" + property + " name=" + (texture == null ? "null" : texture.name) +
                        " path=" + AssetDatabase.GetAssetPath(texture));
                }
            }
            Vector3[] views = { Vector3.forward, Vector3.right, Vector3.up, new Vector3(1f, 0.5f, 1f).normalized };
            for (int i = 0; i < views.Length; i++)
            {
                var preview = new PreviewRenderUtility();
                Texture2D result = null;
                try
                {
                    GameObject instance = UnityEngine.Object.Instantiate(asset);
                    preview.AddSingleGO(instance);
                    Bounds bounds = CombinedBounds(instance);
                    float radius = bounds.extents.magnitude;
                    preview.camera.orthographic = true;
                    preview.camera.orthographicSize = radius * 1.05f;
                    preview.camera.nearClipPlane = Mathf.Max(0.001f, radius * 0.01f);
                    preview.camera.farClipPlane = radius * 8f;
                    preview.camera.transform.position = bounds.center + views[i] * radius * 4f;
                    preview.camera.transform.LookAt(bounds.center, i == 2 ? Vector3.forward : Vector3.up);
                    preview.camera.clearFlags = CameraClearFlags.SolidColor;
                    preview.camera.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
                    preview.lights[0].intensity = 1.3f;
                    preview.lights[0].transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                    preview.lights[1].intensity = 0.7f;
                    preview.ambientColor = new Color(0.5f, 0.5f, 0.5f);
                    preview.BeginStaticPreview(new Rect(0, 0, 1024, 768));
                    preview.Render(true);
                    result = preview.EndStaticPreview();
                    File.WriteAllBytes(Absolute(OutputFolder + "/" + itemName + "_source_" + i + ".png"), result.EncodeToPNG());
                }
                finally
                {
                    if (result != null) UnityEngine.Object.DestroyImmediate(result);
                    preview.Cleanup();
                }
            }
        }

        private static void PrepareBatteryImport(string modelPath = BatteryPath, string itemName = "AuxiliaryBattery")
        {
            // FBX texture basenames are shared with other assets. Never resolve them globally.
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Battery ModelImporter is missing.");
            string itemFolder = Path.GetDirectoryName(modelPath).Replace('\\', '/');
            string textureFolder = itemFolder + "/Textures";
            EnsureAssetFolder(textureFolder);
            if (!File.Exists(Absolute(textureFolder + "/base_color.jpg")))
            {
                if (!importer.ExtractTextures(textureFolder))
                    throw new InvalidOperationException("The source FBX has no extractable embedded textures.");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/base_color.jpg");
            if (albedo == null) throw new InvalidOperationException("Battery's own embedded albedo is missing.");
            foreach (string name in new[] { "normal.jpg", "texture_0_roughness.png", "texture_0_metallic.png" })
            {
                var textureImporter = AssetImporter.GetAtPath(textureFolder + "/" + name) as TextureImporter;
                if (textureImporter == null) throw new InvalidOperationException("Missing embedded texture " + name);
                bool normal = name == "normal.jpg";
                textureImporter.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                textureImporter.sRGBTexture = false;
                textureImporter.isReadable = !normal;
                textureImporter.SaveAndReimport();
            }
            string packedPath = textureFolder + "/" + (itemName == "AuxiliaryBattery" ? "Battery" : itemName) + "MetallicSmoothness.png";
            if (!File.Exists(Absolute(packedPath)))
            {
                Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/texture_0_roughness.png");
                Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/texture_0_metallic.png");
                var packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
                try
                {
                    var pixels = new Color[metallic.width * metallic.height];
                    for (int y = 0; y < metallic.height; y++)
                        for (int x = 0; x < metallic.width; x++)
                        {
                            float u = (x + 0.5f) / metallic.width, v = (y + 0.5f) / metallic.height;
                            pixels[y * metallic.width + x] = new Color(metallic.GetPixelBilinear(u, v).r, 0, 0,
                                1f - roughness.GetPixelBilinear(u, v).r);
                        }
                    packed.SetPixels(pixels); packed.Apply();
                    File.WriteAllBytes(Absolute(packedPath), packed.EncodeToPNG());
                }
                finally { UnityEngine.Object.DestroyImmediate(packed); }
                AssetDatabase.ImportAsset(packedPath, ImportAssetOptions.ForceSynchronousImport);
                var packedImporter = (TextureImporter)AssetImporter.GetAtPath(packedPath);
                packedImporter.sRGBTexture = false;
                packedImporter.SaveAndReimport();
            }
            string materialPath = itemFolder + "/" + itemName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureFolder + "/normal.jpg"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            material.SetFloat("_Metallic", 1f); material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.importAnimation = false;
            importer.isReadable = true;
            foreach (string sourceMaterial in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().Select(m => m.name).Distinct())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceMaterial), material);
            // Subsequent imports can expose only the remapped material, so retain the existing mapping.
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
        }

        internal static void ApplyConsumableRiggedSharedMotion() => ApplyRequestedItemGrips();

        // Retained historical authoring path; the item replacement command never rebuilds the common rig.
        private static void ApplyOriginalSharedMotion()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Consumable rig application requires Edit Mode.");

            Scene scene = RequireScene();
            EnsureAssetFolder(ControllerFolder);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            var report = new StringBuilder();
            report.AppendLine("Consumable rigged shared motion applied");
            report.AppendLine("controller=" + ControllerPath);
            report.AppendLine("controllerContainsTransformCurves=False");
            report.AppendLine("durationSeconds=5.000000");
            report.AppendLine("leftConstraint=MultiRotationConstraint(LeftArm); bind elbow and wrist preserved");
            report.AppendLine("rightConstraint=TwoBoneIKConstraint(RightShoulder,RightArm,RightForeArm) with shoulder hint; neutral wrist; 15 finger rotations");
            report.AppendLine("torsoConstraint=three distributed existing spine rotations");
            report.AppendLine("runtimeWrites=rig controls only; actual battery plug-tip trajectory");
            report.AppendLine("minimumReachReserve=" + Format(ConsumableRiggedSharedMotion.MinimumReachReserve));

            string backupFolder = Absolute("Backups/PlayerHandsRig_2026-09-09/consumable_apply_" + DateTime.Now.ToString("HHmmss"));
            Directory.CreateDirectory(backupFolder);
            File.Copy(Absolute(ScenePath), Path.Combine(backupFolder, "CargoRunMvp.unity"));
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Vector3 rootPosition = target.transform.localPosition;
                Quaternion rootRotation = target.transform.localRotation;
                Vector3 rootScale = target.transform.localScale;
                string rendererSignature = RendererSignature(target, true);
                Dictionary<Transform, BoneInvariant> boneInvariants = CaptureBoneInvariants(target.transform);

                ConfigureTarget(target, controller);

                RequireUnchanged(rootPosition, target.transform.localPosition, targetName + " root position");
                RequireUnchanged(rootRotation, target.transform.localRotation, targetName + " root rotation");
                RequireUnchanged(rootScale, target.transform.localScale, targetName + " root scale");
                RequireBoneInvariants(boneInvariants, targetName + " apply");
                if (!string.Equals(rendererSignature, RendererSignature(target, true), StringComparison.Ordinal))
                    throw new InvalidOperationException(targetName + " renderer, mesh or material changed during rig application.");
                AppendAppliedStructure(report, target);
                AppendAuthoredReach(report, target.GetComponent<ConsumableRiggedSharedMotion>());
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(
                Absolute(OutputFolder + "/applied.txt"),
                report.ToString(),
                new UTF8Encoding(false));
            Debug.Log("Consumable rigged shared motion applied to four targets.");
        }

        internal static void EnterConsumableRiggedSharedMotionReview()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RequireAppliedStructure(RequireScene());
                EditorApplication.EnterPlaymode();
                Debug.Log("Consumable rigged shared-motion review entered Play Mode.");
                return;
            }

            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Consumable rigged review is waiting for Play Mode.");

            InspectConsumableRiggedSharedMotion();
        }

        internal static void InspectConsumableRiggedSharedMotion() => BeginObservation(null);

        internal static void CaptureConsumableRiggedSharedMotionDiagnostic()
        {
            // The separately approved item-replacement task includes repeated diagnostic comparison.
            for (int index = 1; ; index++)
            {
                string folder = OutputFolder + "/diagnostic_" + index.ToString("00", CultureInfo.InvariantCulture);
                if (Directory.Exists(Absolute(folder))) continue;
                BeginObservation(folder);
                return;
            }
        }

        internal static void CaptureConsumableRiggedSharedMotionFinal()
        {
            string folder = OutputFolder + "/final";
            if (Directory.Exists(Absolute(folder)))
                throw new InvalidOperationException("The one-time final capture already exists.");
            string inspection = Absolute(OutputFolder + "/inspection.txt");
            if (!File.Exists(inspection) || !File.ReadAllText(inspection).Contains("result=PASS"))
                throw new InvalidOperationException("Live secondary inspection must pass after direct visual review.");
            BeginObservation(folder);
        }

        internal static void StopConsumableRiggedSharedMotionReview()
        {
            if (observation != null) throw new InvalidOperationException("Wait for the read-only observation to finish.");
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= RecordRestoredReviewState;
                EditorApplication.playModeStateChanged += RecordRestoredReviewState;
                EditorApplication.ExitPlaymode();
            }
            else RecordRestoredReviewState(PlayModeStateChange.EnteredEditMode);
        }

        private static void RecordRestoredReviewState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= RecordRestoredReviewState;
            var report = new StringBuilder();
            report.AppendLine("playing=" + EditorApplication.isPlaying + " paused=" + EditorApplication.isPaused);
            report.AppendLine("animationPreview=" + AnimationMode.InAnimationMode() + " frameDebugger=" + UnityEngine.FrameDebugger.enabled);
            report.AppendLine("temporaryObservationCameras=" + Resources.FindObjectsOfTypeAll<Camera>().Count(c => c.name == "ConsumableReadOnlyObservationCamera"));
            report.AppendLine("activeObservation=" + (observation != null) + " inputConfigurationChanged=False; actualKeyboardMouseNotDirectlyVerified=True");
            foreach (string name in TargetNames)
            {
                var motion = FindUnique(RequireScene(), name).GetComponent<ConsumableRiggedSharedMotion>();
                AppendAppliedStructure(report, motion.gameObject);
                report.AppendLine("target=" + name + " item=" + AssetDatabase.GetAssetPath(motion.BatteryGrip.GetComponentInChildren<MeshFilter>().sharedMesh));
            }
            File.WriteAllText(Absolute(OutputFolder + "/restored_state.txt"), report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());
        }

        private static LiveObservation observation;
        private static void BeginObservation(string captureFolder)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
                throw new InvalidOperationException("Unpaused live automatic playback is required.");
            if (observation != null) throw new InvalidOperationException("An observation is already running.");
            RequireAppliedStructure(RequireScene());
            observation = new LiveObservation(captureFolder);
            ConsumableRiggedSharedMotion.PoseEvaluated += ObservePose;
            Debug.Log("Consumable read-only natural playback observation STARTED; wait for its report.");
        }

        private static void ObservePose(ConsumableRiggedSharedMotion motion)
        {
            if (observation == null) return;
            try
            {
                if (!observation.Observe(motion)) return;
                observation.Finish();
            }
            catch (Exception exception)
            {
                observation.Abort(exception);
                Debug.LogException(exception);
            }
            ConsumableRiggedSharedMotion.PoseEvaluated -= ObservePose;
            observation.Dispose();
            observation = null;
        }

        // Observer never pauses, rebinds, samples, evaluates, or writes the animation target.
        private sealed class LiveObservation : IDisposable
        {
            private const int Width = 640, Height = 480;
            private readonly string folder;
            private readonly StringBuilder report = new StringBuilder();
            private readonly List<string> failures = new List<string>();
            private readonly Dictionary<Transform, BoneInvariant>[] invariants = new Dictionary<Transform, BoneInvariant>[4];
            private readonly Vector3[] roots = new Vector3[4], scales = new Vector3[4];
            private readonly Quaternion[] rotations = new Quaternion[4];
            private readonly string[] rendererSignatures = new string[4];
            private readonly SkinnedMeshRenderer[] skins = new SkinnedMeshRenderer[4];
            private readonly Mesh[] poseReadbacks = new Mesh[4];
            private readonly Texture2D[,] sheets;
            private readonly float[] previous = { -1f, -1f, -1f, -1f };
            private readonly int[] phases = { -1, -1, -1, -1 };
            private int captureTarget, frameSamples, failureCount;
            private float minimumBody = float.MaxValue, minimumArm = float.MaxValue;
            private float minimumRightReserve = float.MaxValue, maximumLeftAngleDrift, maximumRightError;
            private readonly double started = EditorApplication.timeSinceStartup;

            public LiveObservation(string captureFolder)
            {
                folder = captureFolder;
                if (folder != null)
                {
                    Directory.CreateDirectory(Absolute(folder));
                    sheets = new Texture2D[4,8];
                    for (int i = 0; i < 4; i++)
                        for (int v = 0; v < 8; v++)
                            sheets[i,v] = NewFilledTexture(Width * 4, Height * 4, new Color32(9,12,18,255));
                }
                report.AppendLine("mode=unmodified automatic playback; callback=WaitForEndOfFrame after live skin rendering");
                report.AppendLine("directVisualReviewRequired=True; metricRole=secondary-support");
                report.AppendLine("targetTransformsWritten=False; Rebind=False; forcedEvaluation=False");
                report.AppendLine("candidateOnlyPreview=" + (candidateReviewTargets.Count > 0));
                report.AppendLine("grid=row-major,4x4; phases=" + string.Join(",", ReviewTimes.Select(ReviewTimeLabel)));
                for (int i = 0; i < 4; i++)
                {
                    GameObject target = ObservationTarget(TargetNames[i]);
                    skins[i] = target.GetComponentInChildren<SkinnedMeshRenderer>();
                    poseReadbacks[i] = new Mesh();
                    invariants[i] = CaptureBoneInvariants(target.transform);
                    roots[i] = target.transform.localPosition; scales[i] = target.transform.localScale;
                    rotations[i] = target.transform.localRotation;
                    rendererSignatures[i] = RendererSignature(target);
                }
            }
            public bool Observe(ConsumableRiggedSharedMotion motion)
            {
                int i = Array.IndexOf(TargetNames, motion.name);
                if (i < 0) return false;
                if (motion.gameObject != skins[i].GetComponentInParent<ConsumableRiggedSharedMotion>().gameObject) return false;
                if (motion.ManualReview) throw new InvalidOperationException("Manual playback is not valid evidence.");
                if (EditorApplication.timeSinceStartup - started > 180)
                    throw new InvalidOperationException("Natural observation timed out; phase coverage incomplete.");
                float time = motion.CurrentSampleTime;
                // Read the evaluated skin before inspecting lazily synchronized bone Transforms.
                // BakeMesh is readback only; no Animator sampling or forced time evaluation.
                skins[i].BakeMesh(poseReadbacks[i]);
                PoseMetrics metrics = MeasurePose(motion);
                frameSamples++;
                minimumBody = Mathf.Min(minimumBody, metrics.BodyClearance);
                minimumArm = Mathf.Min(minimumArm, metrics.ArmCenterlineClearance);
                minimumRightReserve = Mathf.Min(minimumRightReserve, metrics.RightReachReserve);
                maximumLeftAngleDrift = Mathf.Max(maximumLeftAngleDrift, metrics.LeftElbowDrift);
                maximumRightError = Mathf.Max(maximumRightError, metrics.RightTargetError);
                var currentFailures = new List<string>();
                CollectMetricFailures(currentFailures, motion.name, time, metrics);
                RequireBoneInvariants(invariants[i], motion.name);
                RequireUnchanged(roots[i], motion.transform.localPosition, motion.name + " root position");
                RequireUnchanged(rotations[i], motion.transform.localRotation, motion.name + " root rotation");
                RequireUnchanged(scales[i], motion.transform.localScale, motion.name + " root scale");
                RequireUnchanged(motion.LeftForeArmRestRotation, motion.LeftForeArm.localRotation, motion.name + " left elbow rotation");
                RequireUnchanged(motion.LeftHandRestRotation, motion.LeftHand.localRotation, motion.name + " left wrist rotation");
                if (RendererSignature(motion.gameObject) != rendererSignatures[i])
                    currentFailures.Add(motion.name + " mesh/material changed");
                bool wrapped = previous[i] > 4f && time < 0.5f;
                bool active = folder == null || i == captureTarget;
                if (active && phases[i] < 0 && wrapped) phases[i] = 0;
                if (active && phases[i] >= 0 && phases[i] < ReviewTimes.Length)
                {
                    int phase = phases[i];
                    bool due = phase == ReviewTimes.Length - 1 ? wrapped : time >= ReviewTimes[phase];
                    if (due)
                    {
                        report.AppendLine("target=" + motion.name + " phase=" + ReviewTimeLabel(ReviewTimes[phase]) +
                            " actual=" + Format(time) + " frame=" + Time.frameCount);
                        AppendPoseMetrics(report, time, metrics);
                        if (phase == 9) AppendHandSurfaceReadout(report, motion, currentFailures);
                        AppendFingerReadout(report, motion);
                        if (folder != null)
                        {
                            Vector3[] directions = { motion.transform.forward,
                                (motion.transform.forward + motion.transform.right).normalized,
                                (motion.transform.forward - motion.transform.right).normalized, motion.transform.right,
                                (motion.transform.forward + motion.transform.right).normalized, motion.transform.right,
                                motion.RightHand.TransformDirection(Vector3.left), motion.RightHand.TransformDirection(new Vector3(-1f, 0f, -1f).normalized) };
                            for (int v = 0; v < 8; v++)
                                sheets[i,v].SetPixels32((phase % 4) * Width, (3 - phase / 4) * Height,
                                    Width, Height, RenderUpperBody(motion.gameObject, directions[v], Width, Height, v >= 4, v >= 6));
                            if (phase == 9)
                                foreach (int view in new[] { 4, 6, 7, 16, 17, 18, 19 })
                                {
                                    var detail = new Texture2D(1280, 960, TextureFormat.RGBA32, false);
                                    try
                                    {
                                        int directionIndex = view >= 10 ? view - 10 : view;
                                        Vector3 detailDirection = view == 18 ? motion.RightHand.right : view == 19
                                            ? motion.RightHand.TransformDirection(new Vector3(1f,.35f,-1f).normalized) : directions[directionIndex];
                                        detail.SetPixels32(RenderUpperBody(motion.gameObject, detailDirection, 1280, 960, true, view >= 6, view >= 10)); detail.Apply();
                                        File.WriteAllBytes(Absolute(folder + "/" + motion.name + "_contact_" + view + ".png"), detail.EncodeToPNG());
                                    }
                                    finally { UnityEngine.Object.DestroyImmediate(detail); }
                                }
                            File.WriteAllText(Absolute(folder + "/capture_progress.txt"), motion.name + " phase=" + phase + " frame=" + Time.frameCount);
                        }
                        phases[i]++;
                        if (phases[i] == ReviewTimes.Length && folder != null) captureTarget++;
                    }
                }
                previous[i] = time;
                failureCount += currentFailures.Count;
                if (failures.Count < 30) failures.AddRange(currentFailures.Take(30 - failures.Count));
                return phases.All(phase => phase == ReviewTimes.Length);
            }
            public void Finish()
            {
                report.AppendLine("frameSamples=" + frameSamples + "; coverage=4 targets x 16 phases plus every observed frame");
                report.AppendLine("minBodyClearance=" + Format(minimumBody) + "; minArmClearance=" + Format(minimumArm));
                report.AppendLine("minRightReserve=" + Format(minimumRightReserve) + "; maxLeftElbowDrift=" + Format(maximumLeftAngleDrift) +
                    "; maxRightTargetError=" + Format(maximumRightError));
                report.AppendLine("failureCount=" + failureCount);
                foreach (string failure in failures) report.AppendLine("failure=" + failure);
                // Capture success is deliberately not a visual approval.
                report.AppendLine("result=" + (failureCount == 0 ? "PASS" : "FAIL"));
                if (folder != null)
                {
                    string[] views = { "front", "right_oblique", "left_oblique", "right_side", "wrist_oblique_detail", "wrist_side_detail", "grip_palm_detail", "grip_thumb_detail" };
                    for (int i = 0; i < 4; i++)
                        for (int v = 0; v < 8; v++)
                        {
                            sheets[i,v].Apply(false, false);
                            File.WriteAllBytes(Absolute(folder + "/" + TargetNames[i] + "_" + views[v] + ".png"),
                                sheets[i,v].EncodeToPNG());
                        }
                }
                WriteReport();
                Debug.Log("Consumable natural playback observation FINISHED; " +
                    (folder ?? OutputFolder) + "; secondaryResult=" + (failureCount == 0 ? "PASS" : "FAIL"));
            }
            public void Abort(Exception exception) { report.AppendLine("result=ABORT; exception=" + exception); WriteReport(); }
            private void WriteReport()
            {
                Directory.CreateDirectory(Absolute(OutputFolder));
                File.WriteAllText(Absolute(folder == null ? OutputFolder + "/inspection.txt" : folder + "/observation.txt"),
                    report.ToString(), new UTF8Encoding(false));
            }
            public void Dispose()
            {
                foreach (Mesh mesh in poseReadbacks) if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
                if (sheets == null) return;
                foreach (Texture2D sheet in sheets) if (sheet != null) UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static string ProfilePath(string itemName) =>
            "Assets/_Project/Art/Player/HandsRig/" + itemName + "GripPose.asset";

        private static PlayerHandGripPose ProfileFor(ConsumableRiggedSharedMotion motion)
        {
            if(candidateReviewTargets.TryGetValue(motion.name,out var preview)&&preview==motion.gameObject)
            {
                var authored=AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(CandidateReviewFolder+"/WeightReview_"+motion.name+"GripPose.asset");
                if(authored!=null)return authored;
            }
            int index = Array.IndexOf(TargetNames, motion.name);
            string path = index > 0 && motion.BatteryGrip.name == "ConsumableItemGrip"
                ? ProfilePath(RequestedItemNames[index - 1])
                : "Assets/_Project/Art/Player/HandsRig/SharedBatteryGripPose.asset";
            return AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(path) ??
                throw new InvalidOperationException("Missing reviewed/authored hand profile: " + path);
        }

        private static string TargetSnapshot(GameObject target) => string.Join("\n", target
            .GetComponentsInChildren<Component>(true).Where(c => c != null)
            .Select(c => c.GetInstanceID() + ":" + EditorJsonUtility.ToJson(c)));

        private static void ApplyRequestedItemGrips()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Item placement requires Edit Mode.");
            Scene scene = RequireScene();
            if (scene.isDirty) throw new InvalidOperationException("The scene has unsaved edits; preserve them before item placement.");
            GameObject battery = FindUnique(scene, TargetNames[0]);
            string batteryBefore = TargetSnapshot(battery);
            string backup = Absolute("Backups/ConsumableItemGrips_2026-09-09/apply_" + DateTime.Now.ToString("HHmmss"));
            Directory.CreateDirectory(backup);
            File.Copy(Absolute(ScenePath), Path.Combine(backup, "CargoRunMvp.unity"));
            var report = new StringBuilder("Targeted item placement; original common rig and timing retained\n");
            for (int index = 1; index < TargetNames.Length; index++)
            {
                GameObject target = FindUnique(scene, TargetNames[index]);
                var motion = target.GetComponent<ConsumableRiggedSharedMotion>();
                AppendAppliedStructure(null, target);
                var invariants = CaptureBoneInvariants(target.transform);
                string skinBefore = RendererSignature(target, true);
                string sourceMotion = EditorJsonUtility.ToJson(motion);
                var beforePoses = Enumerable.Range(0, 101).Select(i => motion.GetAuthoredRigPose(i * .05f)).ToArray();
                // Full serialized motion is backed up; only prop references/offset and right contact orientation change below.
                report.AppendLine("before " + target.name + "=" + sourceMotion);
                string itemName = RequestedItemNames[index - 1];
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(RequestedItemPath(itemName)) ??
                    throw new InvalidOperationException("Import the requested item first: " + itemName);
                var profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(
                    "Assets/_Project/Art/Player/HandsRig/SharedBatteryGripPose.asset"));
                profile.name = itemName + "GripPose";
                // The original carrier alone is replaced; skeleton, controls and constraints stay in place.
                Transform oldGrip = motion.BatteryGrip;
                Transform grip = NewRigTransform("ConsumableItemGrip", motion.RightHand, motion.RightHand.position, motion.RightHand.rotation);
                grip.localPosition = profile.RightItemPosition;
                grip.localRotation = profile.RightItemRotation;
                grip.localScale = oldGrip.localScale;
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                item.name = itemName; item.transform.SetParent(grip, false);
                item.transform.localPosition = asset.transform.localPosition;
                bool syringe = index == 3;
                // These models have different authored axes. Preserve vertices/UVs and apply a rigid import correction.
                item.transform.localRotation = syringe ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
                float length = syringe ? .30f : .13f;
                item.transform.localScale = asset.transform.localScale * length;
                MeshFilter filter = item.GetComponentInChildren<MeshFilter>();
                Vector3 axis = syringe ? Vector3.down : Vector3.right;
                Vector3[] vertices = filter.sharedMesh.vertices;
                float extreme = vertices.Max(v => Vector3.Dot(v, axis));
                Vector3[] ends = vertices.Where(v => Vector3.Dot(v, axis) > extreme - .000005f).ToArray();
                if (ends.Length == 0) throw new InvalidOperationException("No physical item tip vertices.");
                Vector3 end = ends.Aggregate(Vector3.zero, (sum, v) => sum + v) / ends.Length;
                Transform tip = NewRigTransform(syringe ? "NeedleTip" : "PlugTipCenter", grip, filter.transform.TransformPoint(end), grip.rotation);
                Transform axisBase = NewRigTransform("ItemAxisBase", grip,
                    filter.transform.TransformPoint(end - axis * .001f), grip.rotation);
                if (!syringe)
                    foreach (int sign in new[] { -1, 1 })
                    {
                        Vector3[] prong = ends.Where(v => Mathf.Sign(v.z) == sign).ToArray();
                        if (prong.Length == 0) throw new InvalidOperationException("Both physical plug prongs are required.");
                        Vector3 center = prong.Aggregate(Vector3.zero, (sum, v) => sum + v) / prong.Length;
                        NewRigTransform(sign < 0 ? "PlugTipLeft" : "PlugTipRight", grip, filter.transform.TransformPoint(center), grip.rotation);
                    }
                FitRequestedGrip(motion, filter, profile, syringe, report);
                profile.RightItemPosition = grip.localPosition; profile.RightItemRotation = grip.localRotation;
                string path = ProfilePath(itemName);
                var stored = AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(path);
                if (stored == null) { AssetDatabase.CreateAsset(profile, path); stored = profile; }
                else { EditorUtility.CopySerialized(profile, stored); UnityEngine.Object.DestroyImmediate(profile); }
                AssetDatabase.SaveAssetIfDirty(stored);
                foreach (var joint in stored.Joints.Where(j => j.BoneName.StartsWith("Right", StringComparison.Ordinal)))
                    RequireDescendant(motion.RightHandTarget, joint.BoneName + "GripControl").localRotation = joint.LocalRotation;
                Quaternion handRotation = Quaternion.Inverse(target.transform.rotation) * motion.RightHand.rotation;
                var data = new SerializedObject(motion);
                SetObject(data, "batteryGrip", grip); SetObject(data, "batteryPlugTip", tip); SetObject(data, "batteryPlugBase", axisBase);
                data.FindProperty("plugOffsetInHand").vector3Value = Quaternion.Inverse(handRotation) *
                    target.transform.InverseTransformVector(tip.position - motion.RightHand.position);
                data.ApplyModifiedPropertiesWithoutUndo();
                Vector3 tipAxis = motion.RightHand.InverseTransformDirection(tip.position - axisBase.position).normalized;
                Quaternion baseForearm = Quaternion.FromToRotation(tipAxis, Vector3.down) * Quaternion.Inverse(motion.RightHandRestRotation);
                Quaternion contact = SelectContactRotation(motion, baseForearm, motion.RightHandRestRotation);
                data.Update(); data.FindProperty("rightContactRotation").quaternionValue = contact;
                data.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(oldGrip.gameObject);
                RequireBoneInvariants(invariants, target.name);
                for (int sample = 0; sample < beforePoses.Length; sample++)
                {
                    var before = beforePoses[sample]; var after = motion.GetAuthoredRigPose(sample * .05f);
                    RequireUnchanged(before.Spine0, after.Spine0, "spine 0 trajectory");
                    RequireUnchanged(before.Spine1, after.Spine1, "spine 1 trajectory");
                    RequireUnchanged(before.Spine2, after.Spine2, "spine 2 trajectory");
                    RequireUnchanged(before.LeftArmRotation, after.LeftArmRotation, "left arm trajectory");
                    RequireUnchanged(before.LeftArmOrigin, after.LeftArmOrigin, "left arm origin");
                }
                if (RendererSignature(target, true) != skinBefore) throw new InvalidOperationException("Player skin changed.");
                report.AppendLine("item=" + itemName + " length=" + length + " physicalTip=" + Format(end));
                AppendAuthoredReach(report, motion);
                EditorUtility.SetDirty(motion);
            }
            if (batteryBefore != TargetSnapshot(battery)) throw new InvalidOperationException("Battery_Aux_Plug changed.");
            report.AppendLine("Battery_Aux_Plug all serialized components unchanged=True");
            Directory.CreateDirectory(Absolute(OutputFolder));
            File.WriteAllText(Absolute(OutputFolder + "/item_applied_" + DateTime.Now.ToString("HHmmss") + ".txt"), report.ToString(), new UTF8Encoding(false));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Three requested item grips applied; Battery_Aux_Plug and common motion retained.");
        }

        // Pure weighted-surface authoring. No live skeleton, Animator time, or skin weights are changed.
        private static void FitRequestedGrip(ConsumableRiggedSharedMotion motion, MeshFilter item,
            PlayerHandGripPose profile, bool syringe, StringBuilder report, bool preserveSurface = false)
        {
            Transform hand = motion.RightHand;
            if (!preserveSurface && syringe) item.transform.parent.localPosition = new Vector3(-.082f, .125f, -.005f);
            else if (!preserveSurface && item.name == "ShieldSwitch") item.transform.parent.localPosition = new Vector3(-.088f, .125f, .020f);
            var skin = motion.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh mesh = skin.sharedMesh; Vector3[] source = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights; Matrix4x4[] bind = mesh.bindposes;
            var proposed = profile.Joints.ToDictionary(j => j.BoneName, j => j.LocalRotation);
            Matrix4x4 itemToHand = hand.worldToLocalMatrix * item.transform.localToWorldMatrix;
            var surface = new ItemAuthoringSurface(item.sharedMesh.vertices.Select(itemToHand.MultiplyPoint3x4).ToArray(), item.sharedMesh.triangles);
            var anglesByDigit = new Dictionary<string, float[]>
            {
                ["Thumb"] = new[] { 30f, 48f, 15f, -24f, 48f }, ["Index"] = new[] { 45f, 72f, 15f, 3f, 0f },
                ["Middle"] = new[] { 48f, 21f, 24f, -9f, 0f }, ["Ring"] = new[] { 54f, 0f, 9f, -15f, 0f },
                ["Little"] = new[] { 39f, 0f, 0f, -15f, 0f }
            };
            Matrix4x4 MatrixFor(Transform bone)
            {
                if (bone == hand) return Matrix4x4.identity;
                if (!bone.IsChildOf(hand)) return hand.worldToLocalMatrix * bone.localToWorldMatrix;
                return MatrixFor(bone.parent) * Matrix4x4.TRS(bone.localPosition,
                    proposed.TryGetValue(bone.name, out Quaternion q) ? q : bone.localRotation, bone.localScale);
            }
            for (int pass = 0; pass < (preserveSurface?3:2); pass++)
            foreach (string digit in new[] { "Index", "Middle", "Ring", "Little", "Thumb" })
            {
                Transform[] bones = new[] { "Proximal", "Intermediate", "Distal" }.Select(s => RequireDescendant(hand, "Right" + digit + s)).ToArray();
                int[] ids = bones.Select(b => Array.IndexOf(skin.bones, b)).ToArray();
                Quaternion[] start = bones.Select(b => b.localRotation).ToArray();
                Vector3[] axes = bones.Select((b, j) => b.InverseTransformDirection(Vector3.Cross(
                    j < 2 ? (bones[j + 1].position - b.position).normalized : b.up,
                    hand.TransformDirection(preserveSurface&&digit=="Thumb"?Vector3.forward:Vector3.left)).normalized)).ToArray();
                Vector3 spreadAxis = bones[0].InverseTransformDirection(hand.right);
                int[] selected = Enumerable.Range(0, source.Length).Where(v =>
                {
                    BoneWeight w = weights[v];
                    return ids.Contains(w.boneIndex0) || (w.weight1 > .01f && ids.Contains(w.boneIndex1)) ||
                        (w.weight2 > .01f && ids.Contains(w.boneIndex2)) || (w.weight3 > .01f && ids.Contains(w.boneIndex3));
                }).ToArray();
                var selectedSet=new HashSet<int>(selected);
                int[] triangles=mesh.triangles;
                var relatedTriangles=new List<(int A,int B,int C)>();
                if(preserveSurface)
                {
                    for(int t=0;t<triangles.Length;t+=3)
                        if(selectedSet.Contains(triangles[t])||selectedSet.Contains(triangles[t+1])||selectedSet.Contains(triangles[t+2]))
                            relatedTriangles.Add((triangles[t],triangles[t+1],triangles[t+2]));
                    selected=relatedTriangles.SelectMany(t=>new[]{t.A,t.B,t.C}).Distinct().OrderBy(v=>v).ToArray();
                }
                var localIndices=selected.Select((v,i)=>(v,i)).ToDictionary(p=>p.v,p=>p.i);
                var edgeSet=new HashSet<(int,int)>();
                foreach(var t in relatedTriangles)foreach(var edge in new[]{(t.A,t.B),(t.B,t.C),(t.C,t.A)})
                    edgeSet.Add(edge.Item1<edge.Item2?edge:(edge.Item2,edge.Item1));
                var edges=edgeSet.Select(e=>(A:localIndices[e.Item1],B:localIndices[e.Item2],Length:Vector3.Distance(source[e.Item1],source[e.Item2])))
                    .Where(e=>e.Length>.0005f).ToArray();
                Matrix4x4[] otherMatrices = skin.bones.Select((b, i) => MatrixFor(b) * bind[i]).ToArray();
                var fixedPoints = new Vector3[selected.Length]; var bonePoints = new Vector3[3, selected.Length];
                var influence = new float[3, selected.Length];
                Matrix4x4 sourceToHand = bind[Array.IndexOf(skin.bones, hand)];
                Vector3 distalRest = hand.InverseTransformPoint(bones[2].position);
                bool[] contact = selected.Select(v =>
                {
                    Vector3 p = sourceToHand.MultiplyPoint3x4(source[v]);
                    return skin.bones[weights[v].boneIndex0].name == bones[2].name && p.y > distalRest.y - .006f &&
                        (digit == "Thumb" ? p.z - distalRest.z : -(p.x - distalRest.x)) > .002f;
                }).ToArray();
                if (contact.Count(c => c) < 4) throw new InvalidOperationException("Missing fingertip pad samples: " + digit);
                for (int s = 0; s < selected.Length; s++)
                {
                    int v = selected[s]; BoneWeight w = weights[v];
                    int[] indices = { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
                    float[] values = { w.weight0, w.weight1, w.weight2, w.weight3 };
                    for (int k = 0; k < 4; k++)
                    {
                        int j = Array.IndexOf(ids, indices[k]);
                        if (j < 0) fixedPoints[s] += otherMatrices[indices[k]].MultiplyPoint3x4(source[v]) * values[k];
                        else { bonePoints[j, s] = bind[indices[k]].MultiplyPoint3x4(source[v]); influence[j, s] += values[k]; }
                    }
                }
                var faceChecks=relatedTriangles.Select(t=>
                {
                    Vector3 normal=Vector3.Cross(source[t.B]-source[t.A],source[t.C]-source[t.A]);
                    float area=normal.magnitude;
                    Vector3 fixedNormal=Vector3.zero;var variableNormals=new Vector3[3];
                    foreach(int vertex in new[]{t.A,t.B,t.C})
                    {
                        BoneWeight weight=weights[vertex];
                        int[] ii={weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};
                        float[] ww={weight.weight0,weight.weight1,weight.weight2,weight.weight3};
                        for(int k=0;k<4;k++)
                        {
                            int j=Array.IndexOf(ids,ii[k]);
                            if(j<0)fixedNormal+=otherMatrices[ii[k]].MultiplyVector(normal)*(ww[k]/3);
                            else variableNormals[j]+=bind[ii[k]].MultiplyVector(normal)*(ww[k]/3);
                        }
                    }
                    return (A:localIndices[t.A],B:localIndices[t.B],C:localIndices[t.C],Area:area,Fixed:fixedNormal,Variable:variableNormals);
                }).Where(t=>t.Area>1e-10f).ToArray();
                Quaternion[] Rotations(float[] angles) => Enumerable.Range(0, 3).Select(j => start[j] *
                    (j == 0 ? Quaternion.AngleAxis(angles[3], spreadAxis) : Quaternion.identity) *
                    Quaternion.AngleAxis(angles[j], axes[j]) *
                    (j == 0 ? Quaternion.AngleAxis(angles[4], Vector3.up) : Quaternion.identity)).ToArray();
                float Score(float[] angles)
                {
                    Quaternion[] q = Rotations(angles); var matrices = new Matrix4x4[3];
                    for (int j = 0; j < 3; j++) matrices[j] = (j == 0 ? Matrix4x4.identity : matrices[j - 1]) *
                        Matrix4x4.TRS(bones[j].localPosition, q[j], bones[j].localScale);
                    var nearest = Enumerable.Repeat(float.PositiveInfinity, 8).ToArray();
                    float penetration = 0, maximum = 0;
                    var positions=preserveSurface?new Vector3[selected.Length]:null;
                    for (int s = 0; s < selected.Length; s++)
                    {
                        Vector3 p = fixedPoints[s];
                        for (int j = 0; j < 3; j++) p += matrices[j].MultiplyPoint3x4(bonePoints[j, s]) * influence[j, s];
                        if(preserveSurface)positions[s]=p;
                        float d = surface.SignedDistance(p);
                        // Authoring clearance protects the complete faces between vertex samples.
                        // The independent runtime contact/penetration tolerances remain unchanged.
                        float intrusion=d-(preserveSurface?.0008f:0f);
                        if (intrusion < 0) { maximum = Mathf.Max(maximum, -intrusion); penetration += intrusion * intrusion; }
                        float contactDistance = Mathf.Abs(d - (preserveSurface?.001f:.0002f));
                        if (contact[s] && contactDistance < nearest[7])
                        {
                            nearest[7] = contactDistance; Array.Sort(nearest);
                        }
                    }
                    float deformation=0,maxStretch=0,maxFold=0,folds=0;
                    if(preserveSurface)foreach(var edge in edges)
                    {
                        float length=Vector3.Distance(positions[edge.A],positions[edge.B]);
                        float stretch=Mathf.Max(0,length-edge.Length*1.7f,edge.Length*.35f-length);
                        deformation+=stretch*stretch;maxStretch=Mathf.Max(maxStretch,stretch);
                        // Edge samples detect face-level intrusion missed by vertex-only fitting.
                        if(length>.004f)
                        {
                            float d=surface.SignedDistance((positions[edge.A]+positions[edge.B])*.5f)-.0008f;
                            if(d<0){maximum=Mathf.Max(maximum,-d);penetration+=d*d;}
                        }
                    }
                    if(preserveSurface)foreach(var face in faceChecks)
                    {
                        Vector3 expected=face.Fixed;
                        for(int j=0;j<3;j++)expected+=matrices[j].MultiplyVector(face.Variable[j]);
                        Vector3 cross=Vector3.Cross(positions[face.B]-positions[face.A],positions[face.C]-positions[face.A]);
                        float deficit=Mathf.Max(0,.15f-Vector3.Dot(cross,expected/Mathf.Max(1e-20f,expected.magnitude))/face.Area);
                        maxFold=Mathf.Max(maxFold,deficit);folds+=deficit*deficit;
                        float d=surface.SignedDistance((positions[face.A]+positions[face.B]+positions[face.C])/3)-.0008f;
                        if(d<0){maximum=Mathf.Max(maximum,-d);penetration+=d*d;}
                    }
                    return (preserveSurface?25f:1f)*nearest.Where(float.IsFinite).Average(d => d * d) + 1000 * maximum * maximum +
                        100 * penetration / selected.Length + angles.Take(3).Sum(a => a * a) * .00000000003f+
                        30*maxStretch*maxStretch+3*deformation/Mathf.Max(1,edges.Length)+
                        .01f*maxFold*maxFold+.001f*folds/Mathf.Max(1,faceChecks.Length);
                }
                var valuesNow = anglesByDigit[digit]; float best = Score(valuesNow);
                foreach (float step in new[] { 24f, 12f, 6f, 3f, 1.5f, .5f })
                    for (int iteration = 0; iteration < 4; iteration++)
                    {
                        bool improved = false;
                        for (int parameter = 0; parameter < 5; parameter++)
                        foreach (float sign in new[] { -1f, 1f })
                        {
                            float[] trial = (float[])valuesNow.Clone(); trial[parameter] += sign * step;
                            float[] limits = digit == "Thumb" ? new[] { 40f, 55f, 40f, 45f, 60f } : new[] { 75f, 90f, 65f, 20f, 0f };
                            if (Mathf.Abs(trial[parameter]) > limits[parameter] || parameter < 3 && trial[parameter] < 0) continue;
                            if (digit != "Thumb" && trial[2] > trial[1] * .8f + 15f) continue;
                            float score = Score(trial);
                            if (score >= best) continue;
                            best = score; valuesNow = trial; improved = true;
                        }
                        if (!improved) break;
                    }
                Quaternion[] result = Rotations(valuesNow);
                anglesByDigit[digit] = valuesNow;
                for (int j = 0; j < 3; j++) proposed[bones[j].name] = result[j];
                report.AppendLine("gripAuthoring " + motion.name + " pass=" + pass + " " + digit + " score=" + best + " deltas=" + string.Join(",", valuesNow));
            }
            profile.Joints = profile.Joints.Select(j => new PlayerHandGripPose.JointPose
                { BoneName = j.BoneName, LocalRotation = proposed[j.BoneName] }).ToArray();
        }

        // Acceleration for authoring against the real item triangles, including the syringe's flange.
        // Live verification retains its independent mesh surface reader and unchanged tolerances.
        private sealed class ItemAuthoringSurface
        {
            private readonly Vector3[] vertices;
            private readonly int[] triangles, order;
            private readonly Bounds[] boxes;
            private readonly List<Node> nodes = new List<Node>();
            private readonly Vector3 rayDirection = new Vector3(1, .313f, .177f).normalized;
            private readonly List<float> hits = new List<float>();
            private struct Node { public Bounds Box; public int Start, Count, Left, Right; }
            public ItemAuthoringSurface(Vector3[] points, int[] indices)
            {
                vertices = points; triangles = indices; order = Enumerable.Range(0, indices.Length / 3).ToArray();
                boxes = order.Select(t => { var b = new Bounds(points[indices[t * 3]], Vector3.zero);
                    b.Encapsulate(points[indices[t * 3 + 1]]); b.Encapsulate(points[indices[t * 3 + 2]]); return b; }).ToArray();
                Build(0, order.Length);
            }
            private int Build(int start, int count)
            {
                Bounds box = boxes[order[start]]; for (int i = start + 1; i < start + count; i++) box.Encapsulate(boxes[order[i]]);
                int index = nodes.Count; nodes.Add(default);
                var node = new Node { Box = box, Start = start, Count = count, Left = -1, Right = -1 };
                if (count > 8)
                {
                    int axis = box.size.x >= box.size.y && box.size.x >= box.size.z ? 0 : box.size.y >= box.size.z ? 1 : 2;
                    Array.Sort(order, start, count, Comparer<int>.Create((a, b) => boxes[a].center[axis].CompareTo(boxes[b].center[axis])));
                    int half = count / 2; node.Left = Build(start, half); node.Right = Build(start + half, count - half);
                }
                nodes[index] = node; return index;
            }
            public float SignedDistance(Vector3 point)
            {
                float best = float.PositiveInfinity; Nearest(0, point, ref best);
                float distance = Mathf.Sqrt(best);
                if (distance < .00001f || !nodes[0].Box.Contains(point)) return distance;
                hits.Clear(); RayHits(0, point);
                return hits.Count % 2 == 1 ? -distance : distance;
            }
            private void Nearest(int index, Vector3 point, ref float best)
            {
                Node n = nodes[index]; if (n.Box.SqrDistance(point) > best) return;
                if (n.Left >= 0)
                {
                    bool leftFirst = nodes[n.Left].Box.SqrDistance(point) < nodes[n.Right].Box.SqrDistance(point);
                    Nearest(leftFirst ? n.Left : n.Right, point, ref best); Nearest(leftFirst ? n.Right : n.Left, point, ref best); return;
                }
                for (int i = n.Start; i < n.Start + n.Count; i++)
                {
                    int t = order[i] * 3;
                    Vector3 closest = PlayerHandRigTools.HandItemSurface.ClosestTrianglePoint(point,
                        vertices[triangles[t]], vertices[triangles[t + 1]], vertices[triangles[t + 2]]);
                    best = Mathf.Min(best, (closest - point).sqrMagnitude);
                }
            }
            private bool RayBox(Bounds box, Vector3 point)
            {
                float near = 0, far = float.PositiveInfinity;
                for (int axis = 0; axis < 3; axis++)
                {
                    float a = (box.min[axis] - point[axis]) / rayDirection[axis], b = (box.max[axis] - point[axis]) / rayDirection[axis];
                    near = Mathf.Max(near, Mathf.Min(a, b)); far = Mathf.Min(far, Mathf.Max(a, b));
                }
                return far >= near;
            }
            private void RayHits(int index, Vector3 point)
            {
                Node n = nodes[index]; if (!RayBox(n.Box, point)) return;
                if (n.Left >= 0) { RayHits(n.Left, point); RayHits(n.Right, point); return; }
                for (int i = n.Start; i < n.Start + n.Count; i++)
                {
                    int t = order[i] * 3; Vector3 a = vertices[triangles[t]];
                    Vector3 e1 = vertices[triangles[t + 1]] - a, e2 = vertices[triangles[t + 2]] - a;
                    Vector3 p = Vector3.Cross(rayDirection, e2); float det = Vector3.Dot(e1, p);
                    if (Mathf.Abs(det) < 1e-12f) continue;
                    float inv = 1 / det; Vector3 offset = point - a; float u = Vector3.Dot(offset, p) * inv;
                    if (u < 0 || u > 1) continue;
                    Vector3 q = Vector3.Cross(offset, e1); float v = Vector3.Dot(rayDirection, q) * inv;
                    if (v < 0 || u + v > 1) continue;
                    float d = Vector3.Dot(e2, q) * inv;
                    if (d > .000001f && !hits.Any(previous => Mathf.Abs(previous - d) < .00001f)) hits.Add(d);
                }
            }
        }

        private static void ConfigureTarget(GameObject target, AnimatorController controller)
        {
            Transform model = target.transform;
            foreach (Transform existingSocket in model.GetComponentsInChildren<Transform>(true).Where(t => t.name == SocketName || t.name == GripName).ToArray())
                UnityEngine.Object.DestroyImmediate(existingSocket.gameObject);
            foreach (ConsumableRiggedSharedMotion existing in target.GetComponents<ConsumableRiggedSharedMotion>())
                UnityEngine.Object.DestroyImmediate(existing);
            foreach (Transform existing in Enumerable.Range(0, model.childCount)
                .Select(model.GetChild)
                .Where(item => item.name == RigRootName)
                .ToArray())
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            Type builderType = RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            foreach (Component existing in target.GetComponents(builderType))
                UnityEngine.Object.DestroyImmediate(existing);

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
                animator = target.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;

            Transform leftArm = RequireDescendant(model, "LeftArm");
            Transform leftForeArm = RequireDescendant(model, "LeftForeArm");
            Transform leftHand = RequireDescendant(model, "LeftHand");
            Transform rightShoulder = RequireDescendant(model, "RightShoulder");
            Transform rightArm = RequireDescendant(model, "RightArm");
            Transform rightForeArm = RequireDescendant(model, "RightForeArm");
            Transform rightHand = RequireDescendant(model, "RightHand");

            var rigObject = new GameObject(RigRootName);
            rigObject.transform.SetParent(model, false);
            Component rig = rigObject.AddComponent(RequireRiggingType("UnityEngine.Animations.Rigging.Rig"));
            SetFloat(rig, "m_Weight", 1f);

            Transform leftControl = NewRigTransform(LeftControlName, rigObject.transform, leftArm.position, leftArm.rotation);
            Transform leftTarget = NewRigTransform(LeftTargetName, leftControl, leftHand.position, leftHand.rotation);
            Vector3 forearmAxis = (leftHand.position - leftForeArm.position).normalized;
            Transform socket = NewRigTransform(SocketName, leftForeArm,
                leftHand.position - forearmAxis * 0.065f + model.forward * 0.043f, leftForeArm.rotation);
            Transform rightTarget = NewRigTransform(RightTargetName, rigObject.transform, rightHand.position, rightHand.rotation);
            // This parent is not itself a constraint source. Sync it explicitly so child digit
            // sources inherit the current hand orientation in the Animation Rigging stream.
            rightTarget.gameObject.AddComponent(RequireRiggingType("UnityEngine.Animations.Rigging.RigTransform"));

            Transform[] spineBones = new[] { "Spine02", "Spine01", "Spine" }.Select(name => RequireDescendant(model, name)).ToArray();
            Transform[] spineControls = spineBones.Select(bone =>
                NewRigTransform(bone.name + "RotationControl", rigObject.transform, bone.position, bone.rotation)).ToArray();
            for (int i = 0; i < spineBones.Length; i++)
                CreateRotationConstraint(rigObject.transform, spineBones[i], spineControls[i], spineBones[i].name + "ReachRotation");
            CreateRotationConstraint(rigObject.transform, leftArm, leftControl, "LeftArmExtendedRotation");
            // Explicit inputs prevent accumulated IK with an empty base controller.
            var seeds = new List<Transform>();
            foreach (Transform bone in new[] { rightShoulder, rightArm })
            {
                Transform seed = NewRigTransform(bone.name + "RestControl", rigObject.transform, bone.position, bone.rotation);
                seeds.Add(seed);
                CreateRotationConstraint(rigObject.transform, bone, seed, bone.name + "RestRotation");
            }
            Transform elbowTarget = NewRigTransform("RightElbowTarget", rigObject.transform, rightForeArm.position, rightForeArm.rotation);
            Transform forearmControl = NewRigTransform("RightForearmRotationControl", rigObject.transform, rightForeArm.position, rightForeArm.rotation);
            Transform shoulderHint = NewRigTransform("RightShoulderHint", rigObject.transform, rightArm.position, rightArm.rotation);
            CreateRightChainConstraint(rigObject.transform, rightShoulder, rightArm, rightForeArm, elbowTarget, shoulderHint);
            CreateRotationConstraint(rigObject.transform, rightForeArm, forearmControl, "RightForearmNeutralWristRotation");

            var gripPose = AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>("Assets/_Project/Art/Player/HandsRig/SharedBatteryGripPose.asset") ??
                throw new InvalidOperationException("The reviewed shared hand grip profile is missing.");
            Transform grip = CreateBatteryGrip(target, rightHand, gripPose, out Transform plugTip, out Transform plugBase);
            CreateFingerGripConstraints(rigObject.transform, rightHand, rightTarget, gripPose);
            Vector3 plugAxisInHand = rightHand.InverseTransformDirection(plugTip.position - plugBase.position).normalized;
            Quaternion contactForearmRotation = Quaternion.FromToRotation(plugAxisInHand, Vector3.down) * Quaternion.Inverse(rightHand.localRotation);
            Component builder = ConfigureRigBuilder(target, rig);
            var motion = target.AddComponent<ConsumableRiggedSharedMotion>();
            motion.Configure(model, leftArm, leftForeArm, leftHand, rightShoulder, rightArm,
                rightForeArm, rightHand, leftControl, leftTarget, socket, rightTarget, elbowTarget,
                forearmControl, grip, plugTip, plugBase, contactForearmRotation, spineBones, spineControls, shoulderHint, seeds.ToArray());
            // Search only immutable rig geometry; this authors a target orientation, never poses an Animator.
            Quaternion selected = SelectContactRotation(motion, contactForearmRotation, rightHand.localRotation);
            var motionData = new SerializedObject(motion);
            motionData.FindProperty("rightContactRotation").quaternionValue = selected;
            motionData.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(rigObject);
            EditorUtility.SetDirty(rig);
            EditorUtility.SetDirty(builder);
            EditorUtility.SetDirty(motion);
        }


        private static Transform CreateBatteryGrip(GameObject target, Transform hand, PlayerHandGripPose profile,
            out Transform plugTip, out Transform plugBase)
        {
            Transform model = target.transform;
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(BatteryPath) ??
                throw new InvalidOperationException("The approved battery asset has not been imported.");
            Transform grip = NewRigTransform(GripName, hand, hand.position, hand.rotation);
            grip.localPosition = profile.RightItemPosition;
            Vector3 handScale = hand.lossyScale, modelScale = model.lossyScale;
            grip.localScale = new Vector3(modelScale.x / handScale.x, modelScale.y / handScale.y, modelScale.z / handScale.z);
            grip.localRotation = profile.RightItemRotation;
            GameObject battery = (GameObject)PrefabUtility.InstantiatePrefab(asset, target.scene);
            battery.name = "AuxiliaryBattery";
            battery.transform.SetParent(grip, false);
            battery.transform.localPosition = asset.transform.localPosition;
            battery.transform.localRotation = asset.transform.localRotation;
            // Uniform 15 cm full length; the imported model, UVs and vertices remain intact.
            battery.transform.localScale = asset.transform.localScale * 0.15f;
            MeshFilter filter = battery.GetComponentInChildren<MeshFilter>(true);
            Vector3[] vertices = filter.sharedMesh.vertices;
            float endX = vertices.Max(v => v.x);
            Vector3[] ends = vertices.Where(v => v.x > endX - 0.000015f).ToArray();
            if (ends.Length < 4) throw new InvalidOperationException("Actual two-prong tip vertices are unavailable.");
            var tipMarkers = new List<Transform>();
            foreach (bool positive in new[] { false, true })
            {
                Vector3[] end = ends.Where(v => (v.y >= 0f) == positive).ToArray();
                if (end.Length == 0) throw new InvalidOperationException("Both physical plug prongs must be represented.");
                Vector3 center = end.Aggregate(Vector3.zero, (sum, v) => sum + v) / end.Length;
                tipMarkers.Add(NewRigTransform(positive ? "BatteryPlugTipRight" : "BatteryPlugTipLeft",
                    grip, filter.transform.TransformPoint(center), grip.rotation));
            }
            plugTip = NewRigTransform("BatteryPlugTipCenter", grip,
                (tipMarkers[0].position + tipMarkers[1].position) * 0.5f, grip.rotation);
            plugBase = NewRigTransform("BatteryPlugAxisBase", grip,
                plugTip.position - grip.right * 0.015f, grip.rotation);
            EditorUtility.SetDirty(battery);
            return grip;
        }

        private static void CreateFingerGripConstraints(Transform rigRoot, Transform hand, Transform handTarget, PlayerHandGripPose profile)
        {
            var controls = new Dictionary<Transform, Transform> { [hand] = handTarget };
            Transform[] fingers = hand.GetComponentsInChildren<Transform>(true)
                .Where(bone => profile.Joints.Any(joint => joint.BoneName == bone.name)).ToArray();
            if (fingers.Length != 15) throw new InvalidOperationException("The global right-hand rig must contain all fifteen digit joints.");
            foreach (Transform bone in fingers)
            {
                if (!controls.TryGetValue(bone.parent, out Transform parent))
                    throw new InvalidOperationException("Finger controls require parent-first hierarchy order: " + bone.name);
                Transform control = new GameObject(bone.name + "GripControl").transform;
                control.SetParent(parent, false);
                control.localPosition = bone.localPosition;
                control.localRotation = profile.RotationFor(bone.name);
                control.localScale = bone.localScale;
                controls.Add(bone, control);
                CreateRotationConstraint(rigRoot, bone, control, bone.name + "GripRotation");
            }
        }

        private static Quaternion SelectContactRotation(ConsumableRiggedSharedMotion motion, Quaternion baseForearm, Quaternion wristRest)
        {
            Quaternion selected = baseForearm;
            float best = float.PositiveInfinity;
            for (int yaw = 0; yaw < 360; yaw++)
            {
                Quaternion candidate = Quaternion.AngleAxis(yaw, Vector3.up) * baseForearm;
                float score = 0f;
                for (int sample = 0; sample <= 100; sample++)
                {
                    var pose = motion.GetAuthoredRigPose(sample * .05f, candidate);
                    PredictRightArm(motion, pose, out float reachError, out float elbow, out float shoulderTurn);
                    float clearance = Mathf.Min(BodyClearance(pose.Elbow), BodyClearance(pose.Hand), BodyClearance((pose.Elbow + pose.Hand) * .5f));
                    float reserve = Vector3.Distance(motion.RightShoulder.position, motion.RightArm.position) +
                        Vector3.Distance(motion.RightArm.position, motion.RightForeArm.position) +
                        Vector3.Distance(motion.RightForeArm.position, motion.RightHand.position) - Vector3.Distance(pose.ShoulderOrigin, pose.Hand);
                    score += 100000f * (reachError * reachError + Mathf.Pow(Mathf.Max(0, .015f - clearance), 2) +
                        Mathf.Pow(Mathf.Max(0, ConsumableRiggedSharedMotion.MinimumReachReserve - reserve), 2));
                    score += Mathf.Pow(Mathf.Max(0, elbow - 168f), 2) + Mathf.Pow(Mathf.Max(0, 22f - elbow), 2);
                    score += shoulderTurn * shoulderTurn * .0001f;
                }
                if (score < best) { best = score; selected = candidate; }
            }
            return selected;
        }

        private static Vector3 PredictRightArm(ConsumableRiggedSharedMotion motion, ConsumableRiggedSharedMotion.AuthoredRigPose pose,
            out float reachError, out float elbowAngle, out float shoulderTurn)
        {
            float a = Vector3.Distance(motion.RightShoulder.position, motion.RightArm.position);
            float b = Vector3.Distance(motion.RightArm.position, motion.RightForeArm.position);
            Vector3 delta = pose.Elbow - pose.ShoulderOrigin;
            float distance = delta.magnitude;
            reachError = Mathf.Max(0, distance - a - b, Mathf.Abs(a - b) - distance);
            Vector3 axis = delta / Mathf.Max(distance, .000001f);
            float along = (a * a - b * b + distance * distance) / (2f * Mathf.Max(distance, .000001f));
            Vector3 pole = Vector3.ProjectOnPlane(pose.ShoulderHint - pose.ShoulderOrigin, axis).normalized;
            Vector3 arm = pose.ShoulderOrigin + axis * along + pole * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            elbowAngle = Vector3.Angle(arm - pose.Elbow, pose.Hand - pose.Elbow);
            shoulderTurn = Vector3.Angle(pose.ShoulderHint - pose.ShoulderOrigin, arm - pose.ShoulderOrigin);
            return arm;
        }

        private static void AppendAuthoredReach(StringBuilder report, ConsumableRiggedSharedMotion motion)
        {
            report.AppendLine("authoringPredictionOnly=True; not a substitute for natural rendered observation");
            for (int sample = 0; sample <= 20; sample++)
            {
                float time = sample * .25f;
                var pose = motion.GetAuthoredRigPose(time);
                Vector3 arm = PredictRightArm(motion, pose, out float reachError, out float elbow, out float shoulderTurn);
                float clearance = Mathf.Min(BodyClearance(pose.Elbow), BodyClearance(pose.Hand), BodyClearance((pose.Elbow + pose.Hand) * .5f));
                report.AppendLine("authoring t=" + Format(time) + " reachError=" + Format(reachError) + " elbow=" + Format(elbow) +
                    " shoulderTurn=" + Format(shoulderTurn) + " rightBodyClearance=" + Format(clearance) + " arm=" + Format(arm) +
                    " forearm=" + Format(pose.Elbow) + " hand=" + Format(pose.Hand));
            }
        }

        private static void AppendFingerReadout(StringBuilder report, ConsumableRiggedSharedMotion motion)
        {
            var profile = ProfileFor(motion);
            float localError = 0f, controlError = 0f;
            foreach (var joint in profile.Joints.Where(joint => joint.BoneName.StartsWith("Right", StringComparison.Ordinal)))
            {
                Transform bone = RequireDescendant(motion.RightHand, joint.BoneName);
                Transform control = RequireDescendant(motion.RightHandTarget, joint.BoneName + "GripControl");
                localError = Mathf.Max(localError, Quaternion.Angle(bone.localRotation, joint.LocalRotation));
                controlError = Mathf.Max(controlError, Quaternion.Angle(bone.rotation, control.rotation));
            }
            foreach (var skin in motion.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                int fingers = skin.bones.Count(bone => bone != null && profile.Joints.Any(joint => joint.BoneName == bone.name));
                report.AppendLine("gripReadOnly mesh=" + AssetDatabase.GetAssetPath(skin.sharedMesh) + " bones=" + skin.bones.Length +
                    " digitBindings=" + fingers + " localPoseError=" + Format(localError) + " worldControlError=" + Format(controlError));
            }
        }

        [Serializable] private class HandWeightPoseReadout
        {
            public Vector3[] vertices;
            public int[] triangles;
            public BoneWeight[] weights;
            public string[] names;
            public Matrix4x4[] matrices;
            public Matrix4x4 handBind;
            public Vector3[] bakedHandPoints;
            public Vector3[] renderedHandPoints;
            public Vector3[] itemVertices;
            public int[] itemTriangles;
        }

        private static void AppendHandSurfaceReadout(StringBuilder report, ConsumableRiggedSharedMotion motion, List<string> surfaceFailures)
        {
            var skin = motion.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh source = skin.sharedMesh;
            var baked = new Mesh();
            try
            {
                skin.BakeMesh(baked);
                Vector3[] rest = source.vertices, actual = baked.vertices;
                BoneWeight[] weights = source.boneWeights;
                Matrix4x4[] matrices = skin.bones.Select((bone, index) =>
                    skin.transform.worldToLocalMatrix * bone.localToWorldMatrix * source.bindposes[index]).ToArray();
                float skinError = 0f; int fingerVertices = 0;
                for (int v = 0; v < rest.Length; v++)
                {
                    BoneWeight w = weights[v];
                    string name = skin.bones[w.boneIndex0].name;
                    if (!name.StartsWith("Right", StringComparison.Ordinal) || name == "RightArm" || name == "RightForeArm" || name == "RightShoulder") continue;
                    Vector3 expected = matrices[w.boneIndex0].MultiplyPoint3x4(rest[v]) * w.weight0 +
                        matrices[w.boneIndex1].MultiplyPoint3x4(rest[v]) * w.weight1 + matrices[w.boneIndex2].MultiplyPoint3x4(rest[v]) * w.weight2 +
                        matrices[w.boneIndex3].MultiplyPoint3x4(rest[v]) * w.weight3;
                    skinError = Mathf.Max(skinError, Vector3.Distance(expected, actual[v])); fingerVertices++;
                }
                report.AppendLine("handSurface quality=" + skin.quality + " qualityWeights="+QualitySettings.skinWeights+" deformation="+PlayerSettings.meshDeformation+" vertices=" + source.vertexCount + " fingerSamples=" + fingerVertices + " cpuVsBaked=" + Format(skinError));
                Vector3[] renderedHandPoints = null;
                using(var buffer=skin.GetVertexBuffer())
                if(buffer!=null)
                {
                    var raw=new float[buffer.count*buffer.stride/4];buffer.GetData(raw);
                    int stride=buffer.stride/4;
                    Matrix4x4 rootToHand=motion.RightHand.worldToLocalMatrix*skin.rootBone.localToWorldMatrix;
                    Matrix4x4 meshToHand=motion.RightHand.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                    renderedHandPoints=Enumerable.Range(0,buffer.count).Select(v=>rootToHand.MultiplyPoint3x4(new Vector3(raw[v*stride],raw[v*stride+1],raw[v*stride+2]))).ToArray();
                    float error=Enumerable.Range(0,actual.Length).Max(v=>Vector3.Distance(renderedHandPoints[v],meshToHand.MultiplyPoint3x4(actual[v])));
                    report.AppendLine("renderedSkin count="+buffer.count+" stride="+buffer.stride+" gpuVsBaked="+error+" layout="+string.Join(";",source.GetVertexAttributes().Select(a=>a.ToString())));
                    if(error>.001f)surfaceFailures.Add(motion.name+" rendered skin differs from baked surface="+error);
                }
                else report.AppendLine("renderedSkin unavailable: target was not rendered by an observation camera.");
                var reference = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Player/HandsRig/player_hands_candidate.fbx").GetComponentInChildren<SkinnedMeshRenderer>();
                Vector3[] referenceVertices = reference.sharedMesh.vertices; BoneWeight[] referenceWeights = reference.sharedMesh.boneWeights;
                float vertexDifference = 0f, weightDifference = 0f, bindDifference = 0f; int boneIndexDifferences = 0;
                if (referenceVertices.Length != rest.Length) throw new InvalidOperationException("Reviewed/current skin topology differs.");
                for (int v = 0; v < rest.Length; v++)
                {
                    vertexDifference = Mathf.Max(vertexDifference, Vector3.Distance(rest[v], referenceVertices[v]));
                    BoneWeight a = weights[v], b = referenceWeights[v];
                    weightDifference = Mathf.Max(weightDifference, Mathf.Abs(a.weight0-b.weight0), Mathf.Abs(a.weight1-b.weight1), Mathf.Abs(a.weight2-b.weight2), Mathf.Abs(a.weight3-b.weight3));
                    if (a.boneIndex0!=b.boneIndex0 || a.boneIndex1!=b.boneIndex1 || a.boneIndex2!=b.boneIndex2 || a.boneIndex3!=b.boneIndex3) boneIndexDifferences++;
                }
                for(int i=0;i<source.bindposes.Length;i++)for(int r=0;r<4;r++)for(int c=0;c<4;c++)
                    bindDifference=Mathf.Max(bindDifference,Mathf.Abs(source.bindposes[i][r,c]-reference.sharedMesh.bindposes[i][r,c]));
                report.AppendLine("reviewedCandidateDifference vertices="+Format(vertexDifference)+" weights="+Format(weightDifference)+" boneIndices="+boneIndexDifferences+" bindposes="+Format(bindDifference));
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Player/HandsRig/Review_BatteryGrip.prefab");
                var profile=ProfileFor(motion);
                var prefabBones=prefab.GetComponentsInChildren<Transform>(true).Where(t=>profile.Joints.Any(j=>j.BoneName==t.name)).ToDictionary(t=>t.name);
                foreach(var joint in profile.Joints.Where(j=>j.BoneName.StartsWith("Right",StringComparison.Ordinal)))
                {
                    Transform sceneBone=RequireDescendant(motion.RightHand,joint.BoneName),refBone=prefabBones[joint.BoneName];
                    report.AppendLine("gripBinding "+joint.BoneName+" position="+Format(sceneBone.localPosition)+" referencePosition="+Format(refBone.localPosition)+
                        " scale="+Format(sceneBone.localScale)+" referenceScale="+Format(refBone.localScale));
                    Transform bound=skin.bones.Single(b=>b.name==joint.BoneName);
                    report.AppendLine("boundIdentity="+(bound==sceneBone)+" boundRoot="+bound.root.name+" boundScene="+bound.gameObject.scene.path+
                        " boundLocalRotation="+bound.localRotation.ToString("F6"));
                }
                Transform referenceGrip=prefab.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="RightBatteryGripReview");
                report.AppendLine("gripBinding handScale="+Format(motion.RightHand.lossyScale)+" gripPosition="+Format(motion.BatteryGrip.localPosition)+
                    " referenceGripPosition="+Format(referenceGrip.localPosition)+" gripScale="+Format(motion.BatteryGrip.localScale)+" referenceGripScale="+Format(referenceGrip.localScale));
                MeshFilter item=motion.BatteryGrip.GetComponentInChildren<MeshFilter>();
                Matrix4x4 toHand=motion.RightHand.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                Matrix4x4 itemToHand=motion.RightHand.worldToLocalMatrix*item.transform.localToWorldMatrix;
                MeshFilter referenceItem=referenceGrip.GetComponentInChildren<MeshFilter>();
                Matrix4x4 referenceItemToHand=referenceGrip.parent.worldToLocalMatrix*referenceItem.transform.localToWorldMatrix;
                report.AppendLine("itemToHand="+itemToHand.ToString("F6")+" referenceItemToHand="+referenceItemToHand.ToString("F6"));
                var surface=new PlayerHandRigTools.HandItemSurface(item.sharedMesh.vertices.Select(itemToHand.MultiplyPoint3x4).ToArray(),item.sharedMesh.triangles,itemToHand.inverse,item.sharedMesh.bounds);
                foreach(string digit in new[]{"Thumb","Index","Middle","Ring","Little"})
                {
                    int near=0,inside=0;float distance=float.PositiveInfinity,depth=0;
                    for(int v=0;v<actual.Length;v++)
                    {
                        if(!skin.bones[weights[v].boneIndex0].name.StartsWith("Right"+digit,StringComparison.Ordinal))continue;
                        float d=surface.SignedDistance(toHand.MultiplyPoint3x4(actual[v]));
                        distance=Mathf.Min(distance,Mathf.Abs(d));if(Mathf.Abs(d)<=.003f)near++;if(d<-.0005f){inside++;depth=Mathf.Max(depth,-d);}
                    }
                    report.AppendLine("actualGrip "+digit+" near3mm="+near+" nearest="+Format(distance)+" insideOver0.5mm="+inside+" depth="+Format(depth));
                    if(near==0||inside>0)surfaceFailures.Add(motion.name+" actual grip "+digit+" missing contact or penetrating battery");
                }
                Vector3[] handPoints = actual.Select(toHand.MultiplyPoint3x4).ToArray();
                bool[] handVertices = weights.Select(w =>
                {
                    string name = skin.bones[w.boneIndex0].name;
                    return name == "RightHand" || new[] { "Thumb", "Index", "Middle", "Ring", "Little" }
                        .Any(digit => name.StartsWith("Right" + digit, StringComparison.Ordinal));
                }).ToArray();
                int crossingTriangles = 0, foldedFaces = 0, collapsedFaces = 0; int[] skinTriangles = source.triangles;
                for (int t = 0; t < skinTriangles.Length; t += 3)
                {
                    int a = skinTriangles[t], b = skinTriangles[t + 1], c = skinTriangles[t + 2];
                    if (!(handVertices[a] || handVertices[b] || handVertices[c])) continue;
                    if (surface.CrossesTriangle(handPoints[a], handPoints[b], handPoints[c])) crossingTriangles++;
                    if(handVertices[a]&&handVertices[b]&&handVertices[c])
                    {
                        Vector3 normal=Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]);
                        if(normal.magnitude>1e-10f)
                        {
                            Vector3 expected=Vector3.zero;
                            foreach(int v in new[]{a,b,c})
                            {
                                BoneWeight w=weights[v];
                                expected+=matrices[w.boneIndex0].MultiplyVector(normal)*w.weight0+
                                    matrices[w.boneIndex1].MultiplyVector(normal)*w.weight1+
                                    matrices[w.boneIndex2].MultiplyVector(normal)*w.weight2+
                                    matrices[w.boneIndex3].MultiplyVector(normal)*w.weight3;
                            }
                            Vector3 posed=Vector3.Cross(actual[b]-actual[a],actual[c]-actual[a]);
                            if(Vector3.Dot(posed,expected)/Mathf.Max(1e-20f,posed.magnitude*expected.magnitude)<-.1f)foldedFaces++;
                            if(posed.magnitude<normal.magnitude*.1f)collapsedFaces++;
                        }
                    }
                }
                report.AppendLine("actualHandItemCrossingTriangles=" + crossingTriangles);
                if (crossingTriangles > 0) surfaceFailures.Add(motion.name + " hand/item triangle intersections=" + crossingTriangles);
                report.AppendLine("actualHandFoldedFaces="+foldedFaces+" collapsedFaces="+collapsedFaces);
                if(foldedFaces>0||collapsedFaces>0)surfaceFailures.Add(motion.name+" folded/collapsed hand faces="+foldedFaces+"/"+collapsedFaces);
                float maxEdgeRatio = 0; int stretched = 0, clipOutside = 0;
                Vector3 cameraCenter = motion.RightHand.TransformPoint(new Vector3(0, .125f, 0));
                foreach (int v in Enumerable.Range(0, actual.Length).Where(v => handVertices[v]))
                {
                    Vector3 world = skin.transform.TransformPoint(actual[v]);
                    if (Mathf.Abs(Vector3.Dot(world - cameraCenter, motion.RightHand.TransformDirection(Vector3.left))) > .24f) clipOutside++;
                }
                for (int t = 0; t < skinTriangles.Length; t += 3)
                {
                    int a = skinTriangles[t], b = skinTriangles[t + 1], c = skinTriangles[t + 2];
                    if (!(handVertices[a] && handVertices[b] && handVertices[c])) continue;
                    foreach (var edge in new[] { (a, b), (b, c), (c, a) })
                    {
                        float original = Vector3.Distance(rest[edge.Item1], rest[edge.Item2]); if (original < .0005f) continue;
                        float ratio = Vector3.Distance(actual[edge.Item1], actual[edge.Item2]) / original;
                        maxEdgeRatio = Mathf.Max(maxEdgeRatio, ratio);
                        if (ratio > 4f && Vector3.Distance(actual[edge.Item1], actual[edge.Item2]) > .015f)
                        {
                            stretched++;
                            if (stretched <= 12) report.AppendLine("handStretch edge=" + edge + " ratio=" + ratio + " restLength=" + original +
                                " bones=" + skin.bones[weights[edge.Item1].boneIndex0].name + "/" + skin.bones[weights[edge.Item2].boneIndex0].name +
                                " weightsA=" + DescribeWeights(edge.Item1) + " weightsB=" + DescribeWeights(edge.Item2) +
                                " restA=" + Format(reference.sharedMesh.bindposes[Array.IndexOf(skin.bones, motion.RightHand)].MultiplyPoint3x4(rest[edge.Item1])) +
                                " restB=" + Format(reference.sharedMesh.bindposes[Array.IndexOf(skin.bones, motion.RightHand)].MultiplyPoint3x4(rest[edge.Item2])));
                        }
                    }
                }
                report.AppendLine("handSurfaceDiagnostic maxEdgeRatio=" + maxEdgeRatio + " stretchedEdgeOccurrences=" + stretched + " palmCameraClippedHandVertices=" + clipOutside);
                if(stretched > 0) surfaceFailures.Add(motion.name + " excessive hand surface stretching=" + stretched + " maxRatio=" + maxEdgeRatio);
                string poseFolder = Absolute(OutputFolder + "/weight_pose_readout");
                Directory.CreateDirectory(poseFolder);
                File.WriteAllText(poseFolder + "/" + motion.name + ".json", JsonUtility.ToJson(new HandWeightPoseReadout
                {
                    vertices=rest, triangles=skinTriangles, weights=weights, names=skin.bones.Select(b=>b.name).ToArray(),
                    matrices=skin.bones.Select((bone,index)=>motion.RightHand.worldToLocalMatrix*bone.localToWorldMatrix*source.bindposes[index]).ToArray(),
                    handBind=source.bindposes[Array.IndexOf(skin.bones,motion.RightHand)],
                    bakedHandPoints=handPoints,renderedHandPoints=renderedHandPoints,
                    itemVertices=item.sharedMesh.vertices.Select(itemToHand.MultiplyPoint3x4).ToArray(),itemTriangles=item.sharedMesh.triangles
                }));
                int splitSeams = 0; float maximumSeamGap = 0;
                var seamGroups = Enumerable.Range(0, rest.Length).Where(v => handVertices[v]).GroupBy(v =>
                    Math.Round(rest[v].x, 6) + "/" + Math.Round(rest[v].y, 6) + "/" + Math.Round(rest[v].z, 6));
                string DescribeWeights(int v)
                {
                    BoneWeight w = weights[v];
                    return skin.bones[w.boneIndex0].name + ":" + w.weight0 + "," + skin.bones[w.boneIndex1].name + ":" + w.weight1 +
                        "," + skin.bones[w.boneIndex2].name + ":" + w.weight2 + "," + skin.bones[w.boneIndex3].name + ":" + w.weight3;
                }
                foreach (var group in seamGroups.Where(g => g.Count() > 1))
                {
                    int[] ids = group.ToArray();
                    for (int a = 0; a < ids.Length; a++) for (int b = a + 1; b < ids.Length; b++)
                    {
                        float gap = Vector3.Distance(handPoints[ids[a]], handPoints[ids[b]]);
                        maximumSeamGap = Mathf.Max(maximumSeamGap, gap);
                        if (gap <= .001f) continue;
                        splitSeams++;
                        if (splitSeams <= 15) report.AppendLine("splitSeam vertices=" + ids[a] + "/" + ids[b] + " restGap=" + Vector3.Distance(rest[ids[a]], rest[ids[b]]) +
                            " posedGap=" + gap + " weightsA=" + DescribeWeights(ids[a]) + " weightsB=" + DescribeWeights(ids[b]));
                    }
                }
                report.AppendLine("coincidentRestVertexSeams splitPairs=" + splitSeams + " maximumPosedGap=" + maximumSeamGap);
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
        }

        private static Transform NewRigTransform(
            string name,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            var value = new GameObject(name).transform;
            value.SetParent(parent, false);
            value.position = worldPosition;
            value.rotation = worldRotation;
            return value;
        }

        private static void CreateRotationConstraint(Transform rigRoot, Transform bone, Transform control, string name)
        {
            var value = new GameObject(name);
            value.transform.SetParent(rigRoot, false);
            Component constraint = value.AddComponent(RequireRiggingType("UnityEngine.Animations.Rigging.MultiRotationConstraint"));
            var data = new SerializedObject(constraint);
            SetObject(data, "m_Data.m_ConstrainedObject", bone);
            SetInteger(data, "m_Data.m_SourceObjects.m_Length", 1);
            SetObject(data, "m_Data.m_SourceObjects.m_Item0.transform", control);
            SetFloat(data, "m_Data.m_SourceObjects.m_Item0.weight", 1f);
            SetBool(data, "m_Data.m_MaintainOffset", false);
            foreach (string axis in new[] { "x", "y", "z" }) SetBool(data, "m_Data.m_ConstrainedAxes." + axis, true);
            SetFloat(data, "m_Weight", 1f);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(constraint);
        }

        private static void CreateRightChainConstraint(
            Transform rigRoot,
            Transform root,
            Transform mid,
            Transform tip,
            Transform target,
            Transform hint)
        {
            var constraintObject = new GameObject("RightArmShoulderTwoBoneIK");
            constraintObject.transform.SetParent(rigRoot, false);
            Component constraint = constraintObject.AddComponent(
                RequireRiggingType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint"));
            var serialized = new SerializedObject(constraint);
            SetObject(serialized, "m_Data.m_Root", root);
            SetObject(serialized, "m_Data.m_Mid", mid);
            SetObject(serialized, "m_Data.m_Tip", tip);
            SetObject(serialized, "m_Data.m_Target", target);
            SetObject(serialized, "m_Data.m_Hint", hint);
            SetFloat(serialized, "m_Data.m_TargetPositionWeight", 1f);
            SetFloat(serialized, "m_Data.m_TargetRotationWeight", 0f);
            SetFloat(serialized, "m_Data.m_HintWeight", 1f);
            SetBool(serialized, "m_Data.m_MaintainTargetPositionOffset", false);
            SetBool(serialized, "m_Data.m_MaintainTargetRotationOffset", false);
            SetFloat(serialized, "m_Weight", 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(constraint);
        }

        private static Component ConfigureRigBuilder(GameObject target, Component rig)
        {
            Component builder = target.AddComponent(
                RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder"));
            ((Behaviour)builder).enabled = true;
            var serialized = new SerializedObject(builder);
            SerializedProperty layers = serialized.FindProperty("m_RigLayers") ??
                throw new InvalidOperationException("Animation Rigging layer property is unavailable.");
            layers.arraySize = 1;
            SerializedProperty layer = layers.GetArrayElementAtIndex(0);
            layer.FindPropertyRelative("m_Rig").objectReferenceValue = rig;
            layer.FindPropertyRelative("m_Active").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return builder;
        }

        private static void RequireAppliedStructure(Scene scene)
        {
            foreach (string targetName in TargetNames)
                AppendAppliedStructure(null, FindUnique(scene, targetName));
        }

        private static void AppendAppliedStructure(StringBuilder report, GameObject target)
        {
            Animator[] animators = target.GetComponents<Animator>();
            if (animators.Length != 1)
                throw new InvalidOperationException(target.name + " must have one direct Animator. Found=" + animators.Length);
            if (!string.Equals(
                AssetDatabase.GetAssetPath(animators[0].runtimeAnimatorController),
                ControllerPath,
                StringComparison.Ordinal))
                throw new InvalidOperationException(target.name + " does not use the rigged shared controller.");

            ConsumableRiggedSharedMotion[] motions = target.GetComponents<ConsumableRiggedSharedMotion>();
            if (motions.Length != 1)
                throw new InvalidOperationException(target.name + " must have one rig target driver. Found=" + motions.Length);
            Component builder = RequireSingleComponent(target, "UnityEngine.Animations.Rigging.RigBuilder");
            Transform[] rigRoots = Enumerable.Range(0, target.transform.childCount)
                .Select(target.transform.GetChild)
                .Where(item => item.name == RigRootName)
                .ToArray();
            if (rigRoots.Length != 1)
                throw new InvalidOperationException(target.name + " must have one rig root. Found=" + rigRoots.Length);
            Transform rigRoot = rigRoots[0];
            RequireSingleComponent(motions[0].RightHandTarget.gameObject, "UnityEngine.Animations.Rigging.RigTransform");
            RequireSingleComponent(rigRoot.gameObject, "UnityEngine.Animations.Rigging.Rig");
            if (rigRoot.GetComponentsInChildren(RequireRiggingType("UnityEngine.Animations.Rigging.MultiRotationConstraint"), true).Length != 22)
                throw new InvalidOperationException(target.name + " requires three spine, one left arm, two right seeds, one forearm and fifteen finger rotations.");
            RequireSingleComponentInChildren(rigRoot, "UnityEngine.Animations.Rigging.TwoBoneIKConstraint");
            if (motions[0].ContactSocket == null || motions[0].ContactSocket.parent != motions[0].LeftForeArm)
                throw new InvalidOperationException(target.name + " socket is not attached to the left forearm.");
            if (rigRoot.GetComponentsInChildren(RequireRiggingType("UnityEngine.Animations.Rigging.ChainIKConstraint"), true).Length != 0)
                throw new InvalidOperationException(target.name + " still has the superseded ChainIK.");
            if (motions[0].BatteryGrip == null || motions[0].BatteryGrip.parent != motions[0].RightHand ||
                motions[0].BatteryPlugTip == null || motions[0].BatteryPlugBase == null ||
                motions[0].RightElbowTarget == null)
                throw new InvalidOperationException(target.name + " requires the right-hand battery and physical plug references.");
            if (motions[0].BatteryGrip.name == "ConsumableItemGrip")
            {
                int index = Array.IndexOf(TargetNames, target.name);
                MeshFilter[] items = motions[0].BatteryGrip.GetComponentsInChildren<MeshFilter>(true);
                if (index < 1 || items.Length != 1 || AssetDatabase.GetAssetPath(items[0].sharedMesh) != RequestedItemPath(RequestedItemNames[index - 1]))
                    throw new InvalidOperationException(target.name + " does not contain its assigned original item model.");
            }

            var serialized = new SerializedObject(builder);
            SerializedProperty layers = serialized.FindProperty("m_RigLayers");
            if (layers == null || layers.arraySize != 1 ||
                layers.GetArrayElementAtIndex(0).FindPropertyRelative("m_Rig").objectReferenceValue == null ||
                !layers.GetArrayElementAtIndex(0).FindPropertyRelative("m_Active").boolValue)
                throw new InvalidOperationException(target.name + " RigBuilder layer is not active and singular.");

            if (motions[0].ModelRoot != target.transform ||
                motions[0].LeftArm != RequireDescendant(target.transform, "LeftArm") ||
                motions[0].LeftForeArm != RequireDescendant(target.transform, "LeftForeArm") ||
                motions[0].LeftHand != RequireDescendant(target.transform, "LeftHand") ||
                motions[0].RightShoulder != RequireDescendant(target.transform, "RightShoulder") ||
                motions[0].RightArm != RequireDescendant(target.transform, "RightArm") ||
                motions[0].RightForeArm != RequireDescendant(target.transform, "RightForeArm") ||
                motions[0].RightHand != RequireDescendant(target.transform, "RightHand"))
                throw new InvalidOperationException(target.name + " runtime rig references do not match the imported skeleton.");

            if (report != null)
            {
                report.AppendLine(target.name +
                    ": Animator=1 RigBuilder=1 Rig=1 SpineRotation=3 LeftRotation=1 RightRestRotation=2 RightForearmOrientation=1 RightElbowTwoBoneIK=1 RightFingerRotation=15 Battery=1" +
                    " rootRendererMeshMaterialUnchanged=True");
            }
        }

        private static Component RequireSingleComponent(GameObject target, string typeName)
        {
            Type type = RequireRiggingType(typeName);
            Component[] components = target.GetComponents(type);
            if (components.Length != 1)
                throw new InvalidOperationException(target.name + " must have one " + type.Name + ". Found=" + components.Length);
            return components[0];
        }

        private static Component RequireSingleComponentInChildren(Transform root, string typeName)
        {
            Type type = RequireRiggingType(typeName);
            Component[] components = root.GetComponentsInChildren(type, true);
            if (components.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + type.Name + ". Found=" + components.Length);
            return components[0];
        }

        private static PoseMetrics MeasurePose(ConsumableRiggedSharedMotion motion)
        {
            Transform model = motion.ModelRoot;
            float leftLength = Vector3.Distance(motion.LeftArm.position, motion.LeftForeArm.position) +
                               Vector3.Distance(motion.LeftForeArm.position, motion.LeftHand.position);
            float rightLength = Vector3.Distance(motion.RightShoulder.position, motion.RightArm.position) +
                                Vector3.Distance(motion.RightArm.position, motion.RightForeArm.position) +
                                Vector3.Distance(motion.RightForeArm.position, motion.RightHand.position);

            Vector3 leftArm = model.InverseTransformPoint(motion.LeftArm.position);
            Vector3 leftForeArm = model.InverseTransformPoint(motion.LeftForeArm.position);
            Vector3 leftHand = model.InverseTransformPoint(motion.LeftHand.position);
            Vector3 rightArm = model.InverseTransformPoint(motion.RightArm.position);
            Vector3 rightForeArm = model.InverseTransformPoint(motion.RightForeArm.position);
            Vector3 rightHand = model.InverseTransformPoint(motion.RightHand.position);

            float bodyClearance = new[]
            {
                BodyClearance(leftForeArm),
                BodyClearance(Vector3.Lerp(leftForeArm, leftHand, 0.5f)),
                BodyClearance(leftHand),
                BodyClearance(rightForeArm),
                BodyClearance(Vector3.Lerp(rightForeArm, rightHand, 0.5f)),
                BodyClearance(rightHand)
            }.Min();

            float armClearance = new[]
            {
                SegmentDistance(leftArm, leftForeArm, rightArm, rightForeArm),
                SegmentDistance(leftArm, leftForeArm, rightForeArm, rightHand),
                SegmentDistance(leftForeArm, leftHand, rightArm, rightForeArm),
                SegmentDistance(leftForeArm, leftHand, rightForeArm, rightHand)
            }.Min();

            return new PoseMetrics
            {
                LeftElbowDrift = Mathf.Abs(JointAngle(motion.LeftArm.position, motion.LeftForeArm.position, motion.LeftHand.position) - motion.LeftRestElbowAngle),
                RightReachReserve = rightLength - Vector3.Distance(motion.RightShoulder.position, motion.RightHandTarget.position),
                LeftTargetError = Vector3.Distance(motion.LeftHand.position, motion.LeftHandTarget.position),
                RightTargetError = Vector3.Distance(motion.RightHand.position, motion.RightHandTarget.position),
                BodyClearance = bodyClearance,
                ArmCenterlineClearance = armClearance,
                LeftElbowAngle = JointAngle(motion.LeftArm.position, motion.LeftForeArm.position, motion.LeftHand.position),
                RightElbowAngle = JointAngle(motion.RightArm.position, motion.RightForeArm.position, motion.RightHand.position),
                RightWristDeviation = Quaternion.Angle(motion.RightHandRestRotation, motion.RightHand.localRotation),
                PlugContactError = Vector3.Distance(motion.BatteryPlugTip.position, motion.ContactSocket.position),
                PlugDownAngle = Vector3.Angle(motion.BatteryPlugTip.position - motion.BatteryPlugBase.position, -model.up)
            };
        }

        private static void AppendPoseMetrics(StringBuilder report, float time, PoseMetrics metrics)
        {
            report.AppendLine(
                "time=" + ReviewTimeLabel(time) +
                " leftElbowDrift=" + Format(metrics.LeftElbowDrift) +
                " rightReachReserve=" + Format(metrics.RightReachReserve) +
                " leftTargetError=" + Format(metrics.LeftTargetError) +
                " rightTargetError=" + Format(metrics.RightTargetError) +
                " bodyClearance=" + Format(metrics.BodyClearance) +
                " armCenterlineClearance=" + Format(metrics.ArmCenterlineClearance) +
                " leftElbowAngle=" + Format(metrics.LeftElbowAngle) +
                " rightElbowAngle=" + Format(metrics.RightElbowAngle) +
                " rightWristDeviation=" + Format(metrics.RightWristDeviation) +
                " plugContactError=" + Format(metrics.PlugContactError) +
                " plugDownAngle=" + Format(metrics.PlugDownAngle));
        }

        private static void CollectMetricFailures(
            ICollection<string> failures,
            string targetName,
            float time,
            PoseMetrics metrics)
        {
            string label = targetName + "@" + ReviewTimeLabel(time);
            if (metrics.RightWristDeviation > 0.1f)
                failures.Add(label + " right wrist changed from neutral=" + Format(metrics.RightWristDeviation));
            if (time >= 1f && metrics.PlugDownAngle > 3f)
                failures.Add(label + " plug is not downward=" + Format(metrics.PlugDownAngle));
            if (time >= 1.7f && time < 3f && metrics.PlugContactError > 0.005f)
                failures.Add(label + " physical plug contact error=" + Format(metrics.PlugContactError));
            if (metrics.LeftElbowDrift > 0.1f)
                failures.Add(label + " left elbow drift=" + Format(metrics.LeftElbowDrift));
            if (metrics.RightReachReserve + 0.0005f < ConsumableRiggedSharedMotion.MinimumReachReserve)
                failures.Add(label + " right reach reserve=" + Format(metrics.RightReachReserve));
            if (metrics.LeftTargetError > 0.012f)
                failures.Add(label + " left target error=" + Format(metrics.LeftTargetError));
            if (metrics.RightTargetError > 0.012f)
                failures.Add(label + " right target error=" + Format(metrics.RightTargetError));
            if (metrics.BodyClearance < 0.015f)
                failures.Add(label + " body clearance=" + Format(metrics.BodyClearance));
            if (metrics.ArmCenterlineClearance < 0.055f)
                failures.Add(label + " arm centerline clearance=" + Format(metrics.ArmCenterlineClearance));
            if (metrics.LeftElbowAngle < 175f || metrics.LeftElbowAngle > 180f)
                failures.Add(label + " left elbow angle=" + Format(metrics.LeftElbowAngle));
            if (metrics.RightElbowAngle < 20f || metrics.RightElbowAngle > 170f)
                failures.Add(label + " right elbow angle=" + Format(metrics.RightElbowAngle));
        }

        private static float BodyClearance(Vector3 point)
        {
            if (point.y < 0.95f || point.y > 1.37f)
                return 1f;
            const float centerX = -0.004f;
            const float centerZ = 0.095f;
            const float radiusX = 0.205f;
            const float radiusZ = 0.185f;
            float normalized = Mathf.Sqrt(
                Mathf.Pow((point.x - centerX) / radiusX, 2f) +
                Mathf.Pow((point.z - centerZ) / radiusZ, 2f));
            return (normalized - 1f) * Mathf.Min(radiusX, radiusZ);
        }

        private static float JointAngle(Vector3 root, Vector3 mid, Vector3 tip) =>
            Vector3.Angle(root - mid, tip - mid);

        private static float SegmentDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            Vector3 d1 = q1 - p1;
            Vector3 d2 = q2 - p2;
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);
            float s;
            float t;

            if (a <= 0.0000001f && e <= 0.0000001f)
                return Vector3.Distance(p1, p2);
            if (a <= 0.0000001f)
            {
                s = 0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= 0.0000001f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denominator = a * e - b * b;
                    s = Mathf.Abs(denominator) > 0.0000001f
                        ? Mathf.Clamp01((b * f - c * e) / denominator)
                        : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            return Vector3.Distance(p1 + d1 * s, p2 + d2 * t);
        }

        private static string ReviewTimeLabel(float time) =>
            Mathf.Abs(time - ConsumableRiggedSharedMotion.Duration) < 0.0001f
                ? "5->0"
                : time.ToString("0.##", CultureInfo.InvariantCulture);

        private static Dictionary<Transform, BoneInvariant> CaptureBoneInvariants(Transform model)
        {
            string[] names =
            {
                "Hips", "Spine02", "Spine01", "Spine", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
                "neck", "Head", "RightShoulder", "RightArm", "RightForeArm", "RightHand"
            };
            return names
                .Select(name => RequireDescendant(model, name))
                .ToDictionary(
                bone => bone,
                bone => new BoneInvariant(bone.localPosition, bone.localScale));
        }

        private static void RequireBoneInvariants(
            IReadOnlyDictionary<Transform, BoneInvariant> invariants,
            string label)
        {
            foreach (KeyValuePair<Transform, BoneInvariant> pair in invariants)
            {
                RequireUnchanged(pair.Value.LocalPosition, pair.Key.localPosition, label + " " + pair.Key.name + " local position");
                RequireUnchanged(pair.Value.LocalScale, pair.Key.localScale, label + " " + pair.Key.name + " local scale");
            }
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void SetObject(SerializedObject value, string path, UnityEngine.Object reference)
        {
            SerializedProperty property = value.FindProperty(path) ??
                throw new InvalidOperationException("Serialized property is unavailable: " + path);
            property.objectReferenceValue = reference;
        }

        private static void SetFloat(Component value, string path, float number)
        {
            var serialized = new SerializedObject(value);
            SetFloat(serialized, path, number);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(SerializedObject value, string path, float number)
        {
            SerializedProperty property = value.FindProperty(path) ??
                throw new InvalidOperationException("Serialized property is unavailable: " + path);
            property.floatValue = number;
        }

        private static void SetInteger(SerializedObject value, string path, int number)
        {
            SerializedProperty property = value.FindProperty(path) ??
                throw new InvalidOperationException("Serialized property is unavailable: " + path);
            property.intValue = number;
        }

        private static void SetBool(SerializedObject value, string path, bool state)
        {
            SerializedProperty property = value.FindProperty(path) ??
                throw new InvalidOperationException("Serialized property is unavailable: " + path);
            property.boolValue = state;
        }

        private readonly struct BoneInvariant
        {
            public BoneInvariant(Vector3 localPosition, Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalScale = localScale;
            }

            public Vector3 LocalPosition { get; }
            public Vector3 LocalScale { get; }
        }

        private struct PoseMetrics
        {
            public float LeftElbowDrift;
            public float RightReachReserve;
            public float LeftTargetError;
            public float RightTargetError;
            public float BodyClearance;
            public float ArmCenterlineClearance;
            public float LeftElbowAngle;
            public float RightElbowAngle;
            public float RightWristDeviation, PlugContactError, PlugDownAngle;
        }
        private static void AppendRigMeasurements(StringBuilder report, Transform model)
        {
            Transform leftArm = RequireDescendant(model, "LeftArm");
            Transform leftForearm = RequireDescendant(model, "LeftForeArm");
            Transform leftHand = RequireDescendant(model, "LeftHand");
            Transform rightArm = RequireDescendant(model, "RightArm");
            Transform rightForearm = RequireDescendant(model, "RightForeArm");
            Transform rightHand = RequireDescendant(model, "RightHand");
            report.AppendLine("model=" + HierarchyPath(model, model.root));
            report.AppendLine("leftUpperLength=" + Format(Vector3.Distance(leftArm.position, leftForearm.position)));
            report.AppendLine("leftLowerLength=" + Format(Vector3.Distance(leftForearm.position, leftHand.position)));
            report.AppendLine("rightUpperLength=" + Format(Vector3.Distance(rightArm.position, rightForearm.position)));
            report.AppendLine("rightLowerLength=" + Format(Vector3.Distance(rightForearm.position, rightHand.position)));
            foreach (string boneName in new[] { "Hips", "Spine", "Neck", "Head", "LeftShoulder", "RightShoulder" })
            {
                Transform bone = FindUniqueDescendantOrNull(model, boneName);
                report.AppendLine(boneName + "=" +
                    (bone == null ? "MISSING" : Format(model.InverseTransformPoint(bone.position))));
            }
            report.AppendLine("rigHierarchyCandidates=" + string.Join(",", model
                .GetComponentsInChildren<Transform>(true)
                .Where(item => new[] { "hips", "spine", "chest", "neck", "head", "shoulder", "arm", "forearm", "hand" }
                    .Any(token => item.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(item => item.name + "@" + Format(model.InverseTransformPoint(item.position)))));
        }

        private static Type RequireRiggingType(string fullName) =>
            Type.GetType(fullName + ", Unity.Animation.Rigging", false) ??
            throw new InvalidOperationException("Animation Rigging type is unavailable: " + fullName);

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException("CargoRunMvp must be active. Active=" + scene.path);
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item.scene == scene && item.name == name && item.transform.root.name != CandidateReviewRoot)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one scene object named " + name + ". Found=" + matches.Length);
            return matches[0];
        }

        private static Transform FindModelRoot(Transform target)
        {
            Transform hips = RequireDescendant(target, "Hips");
            Transform current = hips;
            while (current.parent != null && current.parent != target)
                current = current.parent;
            return current.parent == target ? current : target;
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + name + ". Found=" + matches.Length);
            return matches[0];
        }

        private static Transform FindUniqueDescendantOrNull(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        private static string RendererSignature(GameObject target, bool playerOnly = false) =>
            string.Join(";", target.GetComponentsInChildren<Renderer>(true)
                .Where(item => !playerOnly || item is SkinnedMeshRenderer)
                .OrderBy(item => HierarchyPath(item.transform, target.transform), StringComparer.Ordinal)
                .Select(item =>
                {
                    string mesh = item is SkinnedMeshRenderer skinned
                        ? AssetDatabase.GetAssetPath(skinned.sharedMesh)
                        : item.TryGetComponent(out MeshFilter filter)
                            ? AssetDatabase.GetAssetPath(filter.sharedMesh)
                            : string.Empty;
                    string materials = string.Join(",", item.sharedMaterials
                        .Select(AssetDatabase.GetAssetPath));
                    return HierarchyPath(item.transform, target.transform) + "|" + item.GetType().Name +
                        "|" + mesh + "|" + materials;
                }));

        private static string HierarchyPath(Transform value, Transform root)
        {
            var path = value.name;
            Transform current = value.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private static Bounds CombinedBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(target.name + " has no enabled renderer.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static Color32[] RenderUpperBody(GameObject target, Vector3 viewDirection, int width, int height, bool wristDetail = false, bool gripDetail = false, bool unclipped = false)
        {
            Transform spine02 = RequireDescendant(target.transform, "Spine02");
            Transform head = RequireDescendant(target.transform, "Head");
            Bounds bounds = CombinedBounds(target);
            Transform hand = RequireDescendant(target.transform, "RightHand");
            Vector3 center = gripDetail ? hand.TransformPoint(new Vector3(0f,.125f,0f)) : wristDetail ? hand.position :
                target.transform.TransformPoint(new Vector3(0f, 1.23f, 0.25f));
            float distance = Mathf.Max(4f, bounds.size.magnitude * 1.5f);
            if (unclipped) distance = .8f;
            var cameraObject = new GameObject("ConsumableReadOnlyObservationCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.cameraType = CameraType.Game;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = wristDetail ? 0.19f : 0.52f;
            camera.aspect = width / (float)height;
            // Isolate the target in depth without touching scene layers, renderers or visibility.
            camera.nearClipPlane = distance - 0.85f;
            camera.farClipPlane = distance + 0.85f;
            camera.transform.position = center + viewDirection.normalized * distance;
            camera.transform.LookAt(center, gripDetail ? hand.up : target.transform.up);
            if (gripDetail) { camera.nearClipPlane = distance - .24f; camera.farClipPlane = distance + .24f; }
            if (unclipped) { camera.nearClipPlane = .01f; camera.farClipPlane = 2f; }

            RenderTexture previous = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply(false, false);
                return image.GetPixels32();
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D NewFilledTexture(int width, int height, Color32 color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = Enumerable.Repeat(color, width * height).ToArray();
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void RequireUnchanged(Vector3 before, Vector3 after, string label)
        {
            if ((before - after).sqrMagnitude > 0.00000001f)
                throw new InvalidOperationException(label + " changed.");
        }

        private static void RequireUnchanged(Quaternion before, Quaternion after, string label)
        {
            if (Quaternion.Angle(before, after) > 0.001f)
                throw new InvalidOperationException(label + " changed.");
        }

        private static string Format(Vector3 value) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "({0:F6},{1:F6},{2:F6})", value.x, value.y, value.z);

        private static string Format(float value) =>
            value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
    }
}
