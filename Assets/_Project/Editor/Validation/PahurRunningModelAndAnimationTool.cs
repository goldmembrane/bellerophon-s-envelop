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

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static partial class PahurRunningModelAndAnimationTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Pahur Enemy Placement";
        private const string StaticSlotName =
            "Pahur_01_Static_Review";
        private const string MoveSlotName =
            "Pahur_03_Move";
        private const string MiniFlameSlotName =
            "Pahur_04_MiniFlamethrower";
        private const string ModelName = "Pahur_Model";
        private const string StaticModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx";
        private const string RunningModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurRunning.fbx";
        private const string MiniFlameModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurFlameAttack.fbx";
        private const string RunningAppearanceMeshPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/PahurRunningApprovedAppearanceMesh.asset";
        private const string SourceRunningModelPath =
            @"D:\Bellerophon2\Bellerophon\enemies model\pāḫḫur running.fbx";
        private const string SourceRunningSha256 =
            "03E78188715D61056ED2DE77D9F8289019A1F80A4604C073F94DE64A9A3F93B8";
        private const string SourceMiniFlameSha256 =
            "35AC10B350B7B643B4BB6C8A0E2CE3541A65B7DA8C7A121DD7DF7C74FDC16E5E";
        private const string ApprovedMaterialFolder =
            "Assets/_Project/Art/Enemies/Pahur/ApprovedAppearance/Materials/";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Pahur/Controllers/Pahur_03_Running.controller";
        private const string InPlaceClipPath =
            "Assets/_Project/Art/Enemies/Pahur/Animations/Pahur_03_Running_InPlace.anim";
        private const string StateName =
            "PahurRunningMixamo";
        private const string ReportPath =
            "docs/validation/pahur_running_model_animation_2026-07-31/Pahur_03_Running_Validation.txt";
        private const string CapturePath =
            "docs/validation/pahur_running_model_animation_2026-07-31/Pahur_03_Running_Review.png";
        private const int CapturePanels = 5;

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Inspect Mini Flame Attack Source")]
        public static void InspectPahurMiniFlameAttackSource()
        {
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    MiniFlameModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur mini flame attack FBX is missing.");
            var renderers =
                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(
                        MiniFlameModelPath)
                    .OfType<AnimationClip>()
                    .Where(
                        item =>
                            !item.name.StartsWith(
                                "__preview__",
                                StringComparison.Ordinal))
                    .ToArray();
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var staticMesh =
                RequireRenderer(
                    staticPrefab.transform,
                    "static FBX").sharedMesh;
            var exactMaterials =
                ExactUvTriangleMaterials(
                    staticMesh);
            var exactSamples =
                ExactUv3ValuesByUv0(
                    staticMesh);
            var output =
                new StringBuilder();
            output.Append(
                "PahurMiniFlameAttackSourceInspection Result=PASS" +
                ", Sha256=" +
                Sha256(
                    Absolute(
                        MiniFlameModelPath)) +
                ", Renderers=" +
                renderers.Length +
                ", Clips=" +
                string.Join(
                    "|",
                    clips.Select(item =>
                        item.name)));
            foreach (var renderer in renderers)
            {
                var mesh =
                    renderer.sharedMesh ??
                    throw new InvalidOperationException(
                        "A mini flame attack renderer has no mesh.");
                var uv3 =
                    new List<Vector4>();
                mesh.GetUVs(
                    3,
                    uv3);
                var exactTriangles = 0;
                var totalTriangles = 0;
                var uv = mesh.uv;
                for (var subMesh = 0;
                     subMesh < mesh.subMeshCount;
                     subMesh++)
                {
                    var triangles =
                        mesh.GetTriangles(
                            subMesh);
                    for (var index = 0;
                         index < triangles.Length;
                         index += 3)
                    {
                        totalTriangles++;
                        var key =
                            ExactUvTriangleKey(
                                uv[triangles[index]],
                                uv[triangles[index + 1]],
                                uv[triangles[index + 2]]);
                        if (exactMaterials.TryGetValue(
                                key,
                                out var mask) &&
                            mask != 0 &&
                            (mask & (mask - 1)) == 0)
                        {
                            exactTriangles++;
                        }
                    }
                }

                var exactUv3 =
                    uv.Count(
                        value =>
                            exactSamples.TryGetValue(
                                ExactUvKey(value),
                                out var sample) &&
                            sample.HasValue);
                output.Append(
                    Environment.NewLine +
                    "Renderer=" +
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        prefab.transform) +
                    ", Mesh=" +
                    mesh.name +
                    ", Vertices=" +
                    mesh.vertexCount +
                    ", Triangles=" +
                    totalTriangles +
                    ", SubMeshes=" +
                    mesh.subMeshCount +
                    ", UV0=" +
                    uv.Length +
                    ", UV3=" +
                    uv3.Count +
                    ", Bones=" +
                    renderer.bones.Length +
                    ", ExactTriangles=" +
                    exactTriangles +
                    "/" +
                    totalTriangles +
                    ", ExactUv3=" +
                    exactUv3 +
                    "/" +
                    mesh.vertexCount +
                    ", Bounds=" +
                    ScaleText(mesh.bounds.size) +
                    ", Materials=" +
                    string.Join(
                        "|",
                        renderer.sharedMaterials.Select(
                            item =>
                                item == null
                                    ? "<null>"
                                    : item.name)));
                output.Append(
                    Environment.NewLine +
                    "Bones=" +
                    string.Join(
                        "|",
                        renderer.bones.Select(
                            item => item.name)));
                var runningPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        RunningModelPath) ??
                    throw new InvalidOperationException(
                        "The approved running Pahur FBX is missing.");
                var runningRenderer =
                    RequireRenderer(
                        runningPrefab.transform,
                        "running FBX");
                var runningMesh =
                    runningRenderer.sharedMesh;
                var verticesMatch =
                    mesh.vertices.SequenceEqual(
                        runningMesh.vertices);
                var normalsMatch =
                    mesh.normals.SequenceEqual(
                        runningMesh.normals);
                var tangentsMatch =
                    mesh.tangents.SequenceEqual(
                        runningMesh.tangents);
                var uvMatch =
                    mesh.uv.SequenceEqual(
                        runningMesh.uv);
                var weightsMatch =
                    mesh.boneWeights.SequenceEqual(
                        runningMesh.boneWeights);
                var bindPosesMatch =
                    mesh.bindposes.SequenceEqual(
                        runningMesh.bindposes);
                var trianglesMatch =
                    mesh.subMeshCount ==
                        runningMesh.subMeshCount &&
                    Enumerable.Range(
                            0,
                            mesh.subMeshCount)
                        .All(
                            index =>
                                mesh.GetTriangles(index)
                                    .SequenceEqual(
                                        runningMesh
                                            .GetTriangles(
                                                index)));
                var bonesMatch =
                    renderer.bones
                        .Select(item => item.name)
                        .SequenceEqual(
                            runningRenderer.bones
                                .Select(item =>
                                    item.name));
                var geometryMatchesRunning =
                    mesh.vertexCount ==
                        runningMesh.vertexCount &&
                    verticesMatch &&
                    normalsMatch &&
                    tangentsMatch &&
                    uvMatch &&
                    weightsMatch &&
                    bindPosesMatch &&
                    trianglesMatch &&
                    bonesMatch;
                output.Append(
                    Environment.NewLine +
                    "ExactGeometrySkinUvMatchesApprovedRunning=" +
                    geometryMatchesRunning +
                    ", Vertices=" +
                    verticesMatch +
                    ", Normals=" +
                    normalsMatch +
                    ", Tangents=" +
                    tangentsMatch +
                    ", UV0=" +
                    uvMatch +
                    ", BoneWeights=" +
                    weightsMatch +
                    ", BindPoses=" +
                    bindPosesMatch +
                    ", Triangles=" +
                    trianglesMatch +
                    ", Bones=" +
                    bonesMatch);
                foreach (var bone in
                         renderer.bones.Where(
                             item =>
                                 item.name.IndexOf(
                                     "Right",
                                     StringComparison.OrdinalIgnoreCase) >=
                                 0 ||
                                 item.name.IndexOf(
                                     "Hand",
                                     StringComparison.OrdinalIgnoreCase) >=
                                 0))
                {
                    output.Append(
                        Environment.NewLine +
                        "RightArmBone=" +
                        AnimationUtility.CalculateTransformPath(
                            bone,
                            prefab.transform) +
                        ", LocalPosition=" +
                        ScaleText(bone.localPosition) +
                        ", LocalEuler=" +
                        ScaleText(
                            bone.localEulerAngles));
                }
            }

            output.Append(
                DescribeMiniWeaponComponents(
                    prefab));
            Debug.Log(
                output.ToString());
        }

        private static readonly string[] SlotNames =
        {
            "Pahur_01_Static_Review",
            "Pahur_02_Idle",
            "Pahur_03_Move",
            "Pahur_04_MiniFlamethrower",
            "Pahur_05_BreakthroughFlamethrower",
            "Pahur_06_GuardianFlamethrower",
            "Pahur_07_Stop",
            "Pahur_08_ToGuardianStance",
            "Pahur_09_FromGuardianStance",
            "Pahur_10_Hit",
            "Pahur_11_Death"
        };

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Inspect Running Source")]
        public static void InspectPahurRunningSource()
        {
            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RunningModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur running FBX is missing.");
            var renderers =
                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(
                        RunningModelPath)
                    .OfType<AnimationClip>()
                    .Where(
                        item =>
                            !item.name.StartsWith(
                                "__preview__",
                                StringComparison.Ordinal))
                    .ToArray();
            var output = new StringBuilder();
            output.Append(
                "PahurRunningSourceInspection Result=PASS");
            output.Append(
                ", Sha256=" +
                Sha256(Absolute(RunningModelPath)));
            output.Append(
                ", Renderers=" +
                renderers.Length);
            output.Append(
                ", Clips=" +
                string.Join(
                    "|",
                    clips.Select(item =>
                        item.name)));
            foreach (var renderer in renderers)
            {
                var mesh =
                    renderer.sharedMesh ??
                    throw new InvalidOperationException(
                        "A running renderer has no mesh.");
                var uv3 =
                    new List<Vector4>();
                mesh.GetUVs(
                    3,
                    uv3);
                output.Append(
                    Environment.NewLine +
                    "Renderer=" +
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        prefab.transform) +
                    ", Mesh=" +
                    mesh.name +
                    ", Vertices=" +
                    mesh.vertexCount +
                    ", Triangles=" +
                    Enumerable.Range(
                            0,
                            mesh.subMeshCount)
                        .Sum(index =>
                            mesh.GetTriangles(index).Length /
                            3) +
                    ", SubMeshes=" +
                    mesh.subMeshCount +
                    ", UV0=" +
                    mesh.uv.Length +
                    ", UV3=" +
                    uv3.Count +
                    ", Bones=" +
                    renderer.bones.Length +
                    ", Materials=" +
                    string.Join(
                        "|",
                        renderer.sharedMaterials.Select(
                            item =>
                                item == null
                                    ? "<null>"
                                    : item.name)));
            }

            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var staticRenderer =
                RequireRenderer(
                    staticPrefab.transform,
                    "static FBX");
            var staticMesh =
                staticRenderer.sharedMesh ??
                throw new InvalidOperationException(
                    "The approved static renderer has no mesh.");
            var staticTriangleMaterials =
                ExactUvTriangleMaterials(
                    staticMesh);
            var staticVertexMaterials =
                ExactUvVertexMaterials(
                    staticMesh);
            var staticUv3 =
                ExactUv3ValuesByUv0(
                    staticMesh);
            var exactTriangleCount = 0;
            var exactMaterialCount = 0;
            var exactVertexMaterialTriangleCount =
                0;
            var runningTriangleCount = 0;
            var runningVertexCount = 0;
            var exactVertexMaterialCount = 0;
            var exactUv3Count = 0;
            foreach (var renderer in renderers)
            {
                var mesh =
                    renderer.sharedMesh;
                var uv = mesh.uv;
                runningVertexCount +=
                    mesh.vertexCount;
                foreach (var value in uv)
                {
                    var key =
                        ExactUvKey(value);
                    if (staticVertexMaterials
                            .TryGetValue(
                                key,
                                out var vertexMask) &&
                        vertexMask != 0 &&
                        (vertexMask &
                         (vertexMask - 1)) == 0)
                    {
                        exactVertexMaterialCount++;
                    }

                    if (staticUv3.TryGetValue(
                            key,
                            out var sample) &&
                        sample.HasValue)
                    {
                        exactUv3Count++;
                    }
                }

                for (var subMesh = 0;
                     subMesh < mesh.subMeshCount;
                     subMesh++)
                {
                    var triangles =
                        mesh.GetTriangles(subMesh);
                    for (var index = 0;
                         index < triangles.Length;
                         index += 3)
                    {
                        runningTriangleCount++;
                        var key =
                            ExactUvTriangleKey(
                                uv[triangles[index]],
                                uv[triangles[index + 1]],
                                uv[triangles[index + 2]]);
                        if (staticTriangleMaterials
                            .TryGetValue(
                                key,
                                out var materialMask))
                        {
                            exactTriangleCount++;
                            if (materialMask != 0 &&
                                (materialMask &
                                 (materialMask - 1)) == 0)
                            {
                                exactMaterialCount++;
                            }

                            continue;
                        }

                        var firstMask =
                            staticVertexMaterials
                                .GetValueOrDefault(
                                    ExactUvKey(
                                        uv[triangles[index]]));
                        var secondMask =
                            staticVertexMaterials
                                .GetValueOrDefault(
                                    ExactUvKey(
                                        uv[triangles[index + 1]]));
                        var thirdMask =
                            staticVertexMaterials
                                .GetValueOrDefault(
                                    ExactUvKey(
                                        uv[triangles[index + 2]]));
                        var commonMask =
                            firstMask &
                            secondMask &
                            thirdMask;
                        if (commonMask != 0 &&
                            (commonMask &
                             (commonMask - 1)) == 0)
                        {
                            exactVertexMaterialTriangleCount++;
                        }
                    }
                }
            }

            output.Append(
                Environment.NewLine +
                "ExactUvTriangleMatches=" +
                exactTriangleCount +
                "/" +
                runningTriangleCount +
                ", ExactUnambiguousMaterialMatches=" +
                exactMaterialCount +
                "/" +
                runningTriangleCount +
                ", ExactVertexResolvedAdditionalTriangles=" +
                exactVertexMaterialTriangleCount +
                ", ExactVertexMaterials=" +
                exactVertexMaterialCount +
                "/" +
                runningVertexCount +
                ", ExactUv3Transfers=" +
                exactUv3Count +
                "/" +
                runningVertexCount);
            foreach (var clip in clips)
            {
                foreach (var binding in
                         AnimationUtility
                             .GetCurveBindings(clip))
                {
                    var horizontalPosition =
                        binding.propertyName ==
                            "RootT.x" ||
                        binding.propertyName ==
                            "RootT.z" ||
                        binding.propertyName ==
                            "MotionT.x" ||
                        binding.propertyName ==
                            "MotionT.z" ||
                        binding.propertyName ==
                            "m_LocalPosition.x" ||
                        binding.propertyName ==
                            "m_LocalPosition.z";
                    if (!horizontalPosition)
                    {
                        continue;
                    }

                    var curve =
                        AnimationUtility.GetEditorCurve(
                            clip,
                            binding);
                    if (curve == null ||
                        curve.length == 0)
                    {
                        continue;
                    }

                    var values =
                        curve.keys.Select(
                                item => item.value)
                            .ToArray();
                    var range =
                        values.Max() -
                        values.Min();
                    if (Mathf.Abs(range) <=
                        0.0001f)
                    {
                        continue;
                    }

                    output.Append(
                        Environment.NewLine +
                        "HorizontalCurve=" +
                        binding.path +
                        "|" +
                        binding.propertyName +
                        ", Start=" +
                        curve.Evaluate(0f)
                            .ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                        ", End=" +
                        curve.Evaluate(clip.length)
                            .ToString(
                                "R",
                                CultureInfo.InvariantCulture) +
                        ", Range=" +
                        range.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                }
            }

            Debug.Log(output.ToString());
        }

        private static Dictionary<string, int>
            ExactUvVertexMaterials(
                Mesh mesh)
        {
            var result =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            var uv = mesh.uv;
            for (var subMesh = 0;
                 subMesh < mesh.subMeshCount;
                 subMesh++)
            {
                var materialMask =
                    1 << subMesh;
                var triangles =
                    mesh.GetTriangles(subMesh);
                foreach (var vertex in triangles)
                {
                    var key =
                        ExactUvKey(uv[vertex]);
                    result.TryGetValue(
                        key,
                        out var existing);
                    result[key] =
                        existing | materialMask;
                }
            }

            return result;
        }

        private static Dictionary<string, Vector4?>
            ExactUv3ValuesByUv0(
                Mesh mesh)
        {
            var uv3 = new List<Vector4>();
            mesh.GetUVs(
                3,
                uv3);
            if (uv3.Count != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "The approved static UV3 channel is incomplete.");
            }

            var result =
                new Dictionary<string, Vector4?>(
                    StringComparer.Ordinal);
            var uv0 = mesh.uv;
            for (var index = 0;
                 index < mesh.vertexCount;
                 index++)
            {
                var key =
                    ExactUvKey(uv0[index]);
                var sample =
                    uv3[index];
                if (result.TryGetValue(
                        key,
                        out var existing) &&
                    (!existing.HasValue ||
                     existing.Value != sample))
                {
                    result[key] = null;
                    continue;
                }

                if (!result.ContainsKey(key))
                {
                    result.Add(
                        key,
                        sample);
                }
            }

            return result;
        }

        private static Dictionary<string, int>
            ExactUvTriangleMaterials(
                Mesh mesh)
        {
            var result =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            var uv = mesh.uv;
            for (var subMesh = 0;
                 subMesh < mesh.subMeshCount;
                 subMesh++)
            {
                var materialMask =
                    1 << subMesh;
                var triangles =
                    mesh.GetTriangles(subMesh);
                for (var index = 0;
                     index < triangles.Length;
                     index += 3)
                {
                    var key =
                        ExactUvTriangleKey(
                            uv[triangles[index]],
                            uv[triangles[index + 1]],
                            uv[triangles[index + 2]]);
                    result.TryGetValue(
                        key,
                        out var existing);
                    result[key] =
                        existing | materialMask;
                }
            }

            return result;
        }

        private static string ExactUvTriangleKey(
            Vector2 a,
            Vector2 b,
            Vector2 c)
        {
            var values =
                new[]
                {
                    ExactUvKey(a),
                    ExactUvKey(b),
                    ExactUvKey(c)
                };
            Array.Sort(
                values,
                StringComparer.Ordinal);
            return string.Join(
                "|",
                values);
        }

        private static string ExactUvKey(
            Vector2 value)
        {
            return
                BitConverter.SingleToInt32Bits(value.x)
                    .ToString("X8", CultureInfo.InvariantCulture) +
                BitConverter.SingleToInt32Bits(value.y)
                    .ToString("X8", CultureInfo.InvariantCulture);
        }

        private static string ExactVector4Key(
            Vector4 value)
        {
            return
                BitConverter.SingleToInt32Bits(value.x)
                    .ToString("X8", CultureInfo.InvariantCulture) +
                BitConverter.SingleToInt32Bits(value.y)
                    .ToString("X8", CultureInfo.InvariantCulture) +
                BitConverter.SingleToInt32Bits(value.z)
                    .ToString("X8", CultureInfo.InvariantCulture) +
                BitConverter.SingleToInt32Bits(value.w)
                    .ToString("X8", CultureInfo.InvariantCulture);
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Apply Running Model And Animation")]
        public static void ApplyPahurRunningModelAndAnimation()
        {
            var scene = RequireScene(true);
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticModel =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        StaticSlotName));
            var staticRenderer =
                RequireRenderer(
                    staticModel,
                    StaticSlotName);
            RequireApprovedMaterials(
                staticRenderer);
            var moveSlot =
                RequireChild(
                    placement.transform,
                    MoveSlotName);
            if (moveSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_03_Move must contain exactly one current model.");
            }

            var otherSlots =
                OtherSlotSignatures(
                    placement.transform);
            var protectedRoots =
                ProtectedRootSignatures(
                    scene,
                    placement.transform);
            var slotPosition =
                moveSlot.localPosition;
            var slotRotation =
                moveSlot.localRotation;
            var slotScale =
                moveSlot.localScale;

            RequireSourceHash();
            ImportRunningModel();
            var takeName =
                ConfigureImporter();
            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RunningModelPath) ??
                throw new InvalidOperationException(
                    "The imported Pahur running FBX is missing.");
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var runningAssetRenderer =
                RequireRenderer(
                    runningPrefab.transform,
                    "running FBX");
            var staticAssetRenderer =
                RequireRenderer(
                    staticPrefab.transform,
                    "static FBX");
            RequireSameBoneNames(
                staticAssetRenderer,
                runningAssetRenderer);
            var sourceClip =
                RequireClip(takeName);
            var clip =
                CreateInPlaceClip(
                    sourceClip,
                    runningPrefab.transform,
                    runningAssetRenderer);
            RequireNoHorizontalRootTranslation(
                runningPrefab.transform,
                runningAssetRenderer,
                clip);
            var appearanceMesh =
                CreateAppearanceMesh(
                    staticPrefab,
                    runningPrefab,
                    clip,
                    staticRenderer.sharedMaterials.Length);
            var controller =
                CreateController(clip);
            var matchedScale =
                MatchedRunningScale(
                    staticPrefab,
                    runningPrefab,
                    staticModel);

            var previous =
                moveSlot.GetChild(0);
            var previousPosition =
                previous.localPosition;
            var previousRotation =
                previous.localRotation;
            var replacement =
                PrefabUtility.InstantiatePrefab(
                    runningPrefab,
                    scene) as GameObject ??
                throw new InvalidOperationException(
                    "The running Pahur prefab could not be instantiated.");
            replacement.name = ModelName;
            replacement.transform.SetParent(
                moveSlot,
                false);
            replacement.transform
                .SetLocalPositionAndRotation(
                    previousPosition,
                    previousRotation);
            replacement.transform.localScale =
                Vector3.one * matchedScale;
            try
            {
                var renderer =
                    RequireRenderer(
                        replacement.transform,
                        MoveSlotName);
                renderer.sharedMesh =
                    appearanceMesh;
                renderer.sharedMaterials =
                    staticRenderer.sharedMaterials
                        .ToArray();
                renderer.updateWhenOffscreen =
                    true;
                EditorUtility.SetDirty(renderer);
                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        renderer);
                var animator =
                    replacement.GetComponent<Animator>() ??
                    replacement.AddComponent<Animator>();
                var sourceAnimator =
                    runningPrefab.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "The running Pahur FBX has no Animator.");
                animator.avatar =
                    sourceAnimator.avatar;
                animator.runtimeAnimatorController =
                    controller;
                animator.applyRootMotion = false;
                animator.cullingMode =
                    AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode =
                    AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                GroundCycle(
                    replacement.transform,
                    animator,
                    renderer,
                    clip,
                    staticRenderer.bounds.min.y);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(
                    replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(
                previous.gameObject);
            RequireUnchanged(
                otherSlots,
                OtherSlotSignatures(
                    placement.transform),
                "A Pahur slot outside Pahur_03_Move changed.");
            RequireUnchanged(
                protectedRoots,
                ProtectedRootSignatures(
                    scene,
                    placement.transform),
                "A scene root outside the Pahur placement changed.");
            if (moveSlot.localPosition != slotPosition ||
                moveSlot.localRotation != slotRotation ||
                moveSlot.localScale != slotScale)
            {
                throw new InvalidOperationException(
                    "The Pahur_03_Move slot transform changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "PahurRunningModelAndAnimationApplied Result=PASS" +
                ", Clip=" + clip.name +
                ", Loop=True" +
                ", RunningShapeAndPosePreserved=True" +
                ", ApprovedMaterialLayoutApplied=True" +
                ", ApprovedTexturesApplied=True" +
                ", OtherSlotsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Align Move Model Y")]
        public static void AlignPahurMoveModelY()
        {
            var scene =
                RequireScene(true);
            var placement =
                RequirePlacement(scene);
            RequireSlots(
                placement.transform);
            var staticModel =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        StaticSlotName));
            var moveModel =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        MoveSlotName));
            var otherSlots =
                OtherSlotSignatures(
                    placement.transform);
            var protectedRoots =
                ProtectedRootSignatures(
                    scene,
                    placement.transform);
            var previousPosition =
                moveModel.localPosition;
            var previousRotation =
                moveModel.localRotation;
            var previousScale =
                moveModel.localScale;
            var renderer =
                RequireRenderer(
                    moveModel,
                    MoveSlotName);
            var previousMesh =
                renderer.sharedMesh;
            var previousMaterials =
                renderer.sharedMaterials
                    .ToArray();
            var animator =
                moveModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Pahur_03_Move has no Animator.");
            var previousController =
                animator.runtimeAnimatorController;
            var previousAvatar =
                animator.avatar;
            var previousApplyRootMotion =
                animator.applyRootMotion;
            var targetPosition =
                previousPosition;
            targetPosition.y =
                staticModel.localPosition.y;
            moveModel.localPosition =
                targetPosition;
            EditorUtility.SetDirty(
                moveModel);
            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    moveModel);

            if (moveModel.localPosition.x !=
                    previousPosition.x ||
                moveModel.localPosition.z !=
                    previousPosition.z ||
                moveModel.localPosition.y !=
                    staticModel.localPosition.y ||
                moveModel.localRotation !=
                    previousRotation ||
                moveModel.localScale !=
                    previousScale ||
                renderer.sharedMesh !=
                    previousMesh ||
                !renderer.sharedMaterials
                    .SequenceEqual(
                        previousMaterials) ||
                animator.runtimeAnimatorController !=
                    previousController ||
                animator.avatar !=
                    previousAvatar ||
                animator.applyRootMotion !=
                    previousApplyRootMotion)
            {
                throw new InvalidOperationException(
                    "A Pahur property outside the move model Y position changed.");
            }

            RequireUnchanged(
                otherSlots,
                OtherSlotSignatures(
                    placement.transform),
                "A Pahur slot outside Pahur_03_Move changed.");
            RequireUnchanged(
                protectedRoots,
                ProtectedRootSignatures(
                    scene,
                    placement.transform),
                "A scene root outside the Pahur placement changed.");
            EditorSceneManager.MarkSceneDirty(
                scene);
            if (!EditorSceneManager.SaveScene(
                    scene,
                    ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved.");
            }

            Debug.Log(
                "PahurMoveModelYAligned Result=PASS" +
                ", PreviousY=" +
                previousPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", StaticY=" +
                staticModel.localPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", MoveY=" +
                moveModel.localPosition.y.ToString(
                    "R",
                    CultureInfo.InvariantCulture) +
                ", OtherPropertiesUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Validate Running Model And Animation")]
        public static void ValidatePahurRunningModelAndAnimation()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlots(placement.transform);
            var staticRenderer =
                RequireRenderer(
                    RequireModel(
                        RequireChild(
                            placement.transform,
                            StaticSlotName)),
                    StaticSlotName);
            var moveModel =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        MoveSlotName));
            var moveRenderer =
                RequireRenderer(
                    moveModel,
                    MoveSlotName);
            var appearance =
                AssetDatabase.LoadAssetAtPath<Mesh>(
                    RunningAppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "The Pahur running appearance mesh is missing.");
            if (moveRenderer.sharedMesh !=
                    appearance ||
                !moveRenderer.sharedMaterials
                    .SequenceEqual(
                        staticRenderer.sharedMaterials))
            {
                throw new InvalidOperationException(
                    "The Pahur running appearance differs from the approved static layout.");
            }

            var runningPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RunningModelPath) ??
                throw new InvalidOperationException(
                    "The running FBX is missing.");
            var sourceMesh =
                RequireRenderer(
                    runningPrefab.transform,
                    "running FBX").sharedMesh;
            var staticPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    StaticModelPath) ??
                throw new InvalidOperationException(
                    "The approved static Pahur FBX is missing.");
            var expectedScale =
                MatchedRunningScale(
                    staticPrefab,
                    runningPrefab,
                    RequireModel(
                        RequireChild(
                            placement.transform,
                            StaticSlotName)));
            if (moveModel.localScale !=
                Vector3.one * expectedScale)
            {
                throw new InvalidOperationException(
                    "The placed Pahur running model height differs from the static Pahur.");
            }

            RequireShapeAndSkinPreserved(
                sourceMesh,
                appearance);
            var animator =
                moveModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Pahur_03_Move has no Animator.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath) ??
                throw new InvalidOperationException(
                    "The Pahur running controller is missing.");
            var state =
                controller.layers[0]
                    .stateMachine.defaultState;
            var clip =
                state.motion as AnimationClip ??
                throw new InvalidOperationException(
                    "The running controller has no clip.");
            if (AssetDatabase.GetAssetPath(clip) !=
                InPlaceClipPath)
            {
                throw new InvalidOperationException(
                    "The running controller does not use the in-place Mixamo clip.");
            }

            RequireNoHorizontalRootTranslation(
                runningPrefab.transform,
                RequireRenderer(
                    runningPrefab.transform,
                    "running FBX"),
                clip);
            if (!animator.enabled ||
                animator.applyRootMotion ||
                animator.runtimeAnimatorController !=
                    controller ||
                !clip.isLooping)
            {
                throw new InvalidOperationException(
                    "The Pahur running Animator contract differs.");
            }

            WriteReport(
                clip,
                sourceMesh,
                appearance,
                staticRenderer.sharedMaterials,
                moveModel.localScale);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Pahur running validation changed the scene.");
            }

            Debug.Log(
                "PahurRunningModelAndAnimationValidated Result=PASS" +
                ", Clip=" + clip.name +
                ", RunningVertices=" +
                sourceMesh.vertexCount +
                ", ApprovedMaterialSlots=" +
                appearance.subMeshCount +
                ", ModelLocalScale=" +
                ScaleText(moveModel.localScale) +
                ", SceneChanged=False.");
        }

        [MenuItem(
            "Bellerophon/Enemies/Pahur/Capture Running Model And Animation Review")]
        public static void CapturePahurRunningModelAndAnimationReview()
        {
            var scene = RequireScene(false);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var model =
                RequireModel(
                    RequireChild(
                        placement.transform,
                        MoveSlotName));
            var animator =
                model.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Pahur_03_Move has no Animator.");
            var controller =
                animator.runtimeAnimatorController as
                    AnimatorController ??
                throw new InvalidOperationException(
                    "Pahur_03_Move has no AnimatorController.");
            var clip =
                controller.layers[0]
                    .stateMachine.defaultState.motion as
                    AnimationClip ??
                throw new InvalidOperationException(
                    "Pahur_03_Move has no running clip.");
            Capture(
                model,
                animator,
                clip,
                Absolute(CapturePath));
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "The running review capture changed the scene.");
            }

            Debug.Log(
                "PahurRunningModelAndAnimationReviewCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static Mesh CreateAppearanceMesh(
            GameObject staticPrefab,
            GameObject runningPrefab,
            AnimationClip clip,
            int materialCount)
        {
            var staticClone =
                UnityEngine.Object.Instantiate(
                    staticPrefab);
            var runningClone =
                UnityEngine.Object.Instantiate(
                    runningPrefab);
            staticClone.hideFlags =
                HideFlags.HideAndDontSave;
            runningClone.hideFlags =
                HideFlags.HideAndDontSave;
            var staticBaked = new Mesh();
            var runningBaked = new Mesh();
            try
            {
                clip.SampleAnimation(
                    runningClone,
                    0f);
                var staticRenderer =
                    RequireRenderer(
                        staticClone.transform,
                        "temporary static model");
                var runningRenderer =
                    RequireRenderer(
                        runningClone.transform,
                        "temporary running model");
                CopyTemporaryPose(
                    staticRenderer,
                    runningRenderer);
                staticRenderer.BakeMesh(
                    staticBaked);
                runningRenderer.BakeMesh(
                    runningBaked);
                var staticMesh =
                    staticRenderer.sharedMesh;
                var runningMesh =
                    runningRenderer.sharedMesh;
                var staticUv = staticMesh.uv;
                var staticSample =
                    new List<Vector4>();
                staticMesh.GetUVs(
                    3,
                    staticSample);
                if (staticMesh.subMeshCount !=
                        materialCount ||
                    staticUv.Length !=
                        staticMesh.vertexCount ||
                    staticSample.Count !=
                        staticMesh.vertexCount)
                {
                    throw new InvalidOperationException(
                        "The approved static appearance channels differ.");
                }

                var staticPoints =
                    WorldPoints(
                        staticRenderer.transform,
                        staticBaked.vertices);
                var runningPoints =
                    WorldPoints(
                        runningRenderer.transform,
                        runningBaked.vertices);
                staticPoints =
                    AlignBounds(
                        staticPoints,
                        BoundsOf(staticPoints),
                        BoundsOf(runningPoints));
                var sourceTriangles =
                    BuildTriangles(
                        staticMesh,
                        staticPoints,
                        staticUv,
                        staticSample);
                var runningUv =
                    runningMesh.uv;
                if (runningUv.Length !=
                    runningMesh.vertexCount)
                {
                    throw new InvalidOperationException(
                        "The running Pahur UV0 channel is incomplete.");
                }

                var exactSamples =
                    ExactUv3ValuesByUv0(
                        staticMesh);
                var exactTriangleMaterials =
                    ExactUvTriangleMaterials(
                        staticMesh);
                var transferredSample =
                    new List<Vector4>(
                        runningMesh.vertexCount);
                var exactSampleCount = 0;
                var authoredSampleCount = 0;
                for (var vertex = 0;
                     vertex < runningPoints.Length;
                     vertex++)
                {
                    var uvKey =
                        ExactUvKey(
                            runningUv[vertex]);
                    if (exactSamples.TryGetValue(
                            uvKey,
                            out var exactSample) &&
                        exactSample.HasValue)
                    {
                        transferredSample.Add(
                            exactSample.Value);
                        exactSampleCount++;
                        continue;
                    }

                    var hit =
                        Nearest(
                            runningPoints[vertex],
                            sourceTriangles);
                    transferredSample.Add(
                        new Vector4(
                            hit.Sample.x,
                            hit.Sample.y,
                            hit.Sample.z,
                            0f));
                    authoredSampleCount++;
                }

                var materialTriangles =
                    Enumerable.Range(
                            0,
                            materialCount)
                        .Select(_ => new List<int>())
                        .ToArray();
                var exactMaterialTriangles = 0;
                var authoredMaterialTriangles = 0;
                for (var sourceSubMesh = 0;
                     sourceSubMesh <
                     runningMesh.subMeshCount;
                     sourceSubMesh++)
                {
                    var indices =
                        runningMesh.GetTriangles(
                            sourceSubMesh);
                    for (var index = 0;
                         index < indices.Length;
                         index += 3)
                    {
                        var uvKey =
                            ExactUvTriangleKey(
                                runningUv[
                                    indices[index]],
                                runningUv[
                                    indices[index + 1]],
                                runningUv[
                                    indices[index + 2]]);
                        var target = -1;
                        if (exactTriangleMaterials
                                .TryGetValue(
                                    uvKey,
                                    out var materialMask) &&
                            materialMask != 0 &&
                            (materialMask &
                             (materialMask - 1)) == 0)
                        {
                            target =
                                SingleMaterialIndex(
                                    materialMask);
                            exactMaterialTriangles++;
                        }
                        else
                        {
                            var centroid =
                                (runningPoints[
                                     indices[index]] +
                                 runningPoints[
                                     indices[index + 1]] +
                                 runningPoints[
                                     indices[index + 2]]) /
                                3f;
                            target =
                                Nearest(
                                    centroid,
                                    sourceTriangles)
                                    .Material;
                            authoredMaterialTriangles++;
                        }

                        materialTriangles[target]
                            .Add(indices[index]);
                        materialTriangles[target]
                            .Add(indices[index + 1]);
                        materialTriangles[target]
                            .Add(indices[index + 2]);
                    }
                }

                var staticTriangleCount =
                    Enumerable.Range(
                            0,
                            staticMesh.subMeshCount)
                        .Sum(
                            subMesh =>
                                staticMesh
                                    .GetTriangles(subMesh)
                                    .Length /
                                3);
                var runningTriangleCount =
                    Enumerable.Range(
                            0,
                            runningMesh.subMeshCount)
                        .Sum(
                            subMesh =>
                                runningMesh
                                    .GetTriangles(subMesh)
                                    .Length /
                                3);
                if (exactMaterialTriangles !=
                        staticTriangleCount ||
                    exactMaterialTriangles +
                        authoredMaterialTriangles !=
                        runningTriangleCount ||
                    exactSampleCount +
                        authoredSampleCount !=
                        runningMesh.vertexCount)
                {
                    throw new InvalidOperationException(
                        "The exact and authored Pahur appearance ranges differ from the inspected source.");
                }

                var generated =
                    UnityEngine.Object.Instantiate(
                        runningMesh);
                generated.name =
                    "PahurRunningApprovedAppearanceMesh";
                generated.SetUVs(
                    3,
                    transferredSample);
                generated.subMeshCount =
                    materialCount;
                for (var material = 0;
                     material < materialCount;
                     material++)
                {
                    generated.SetTriangles(
                        materialTriangles[
                            material],
                        material,
                        false);
                }

                generated.bounds =
                    runningMesh.bounds;
                var stored =
                    AssetDatabase.LoadAssetAtPath<Mesh>(
                        RunningAppearanceMeshPath);
                if (stored != null &&
                    !AssetDatabase.DeleteAsset(
                        RunningAppearanceMeshPath))
                {
                    throw new InvalidOperationException(
                        "The previous Pahur running appearance mesh could not be removed.");
                }

                AssetDatabase.CreateAsset(
                    generated,
                    RunningAppearanceMeshPath);
                stored = generated;
                AssetDatabase.SaveAssets();
                RequireShapeAndSkinPreserved(
                    runningMesh,
                    stored);
                return stored;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    staticBaked);
                UnityEngine.Object.DestroyImmediate(
                    runningBaked);
                UnityEngine.Object.DestroyImmediate(
                    staticClone);
                UnityEngine.Object.DestroyImmediate(
                    runningClone);
            }
        }

        private static SourceTriangle[] BuildTriangles(
            Mesh mesh,
            Vector3[] points,
            Vector2[] uv,
            IReadOnlyList<Vector4> sample)
        {
            var result =
                new List<SourceTriangle>();
            for (var subMesh = 0;
                 subMesh < mesh.subMeshCount;
                 subMesh++)
            {
                var indices =
                    mesh.GetTriangles(subMesh);
                for (var index = 0;
                     index < indices.Length;
                     index += 3)
                {
                    var a = indices[index];
                    var b = indices[index + 1];
                    var c = indices[index + 2];
                    result.Add(
                        new SourceTriangle(
                            points[a],
                            points[b],
                            points[c],
                            uv[a],
                            uv[b],
                            uv[c],
                            sample[a],
                            sample[b],
                            sample[c],
                            subMesh));
                }
            }

            return result.ToArray();
        }

        private static SurfaceHit Nearest(
            Vector3 point,
            IReadOnlyList<SourceTriangle> triangles)
        {
            var bestDistance =
                float.PositiveInfinity;
            var best =
                default(SurfaceHit);
            for (var index = 0;
                 index < triangles.Count;
                 index++)
            {
                var triangle =
                    triangles[index];
                var barycentric =
                    ClosestBarycentric(
                        point,
                        triangle.A,
                        triangle.B,
                        triangle.C);
                var closest =
                    triangle.A *
                    barycentric.x +
                    triangle.B *
                    barycentric.y +
                    triangle.C *
                    barycentric.z;
                var distance =
                    (closest - point)
                    .sqrMagnitude;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best =
                    new SurfaceHit(
                        triangle.Material,
                        triangle.UvA *
                        barycentric.x +
                        triangle.UvB *
                        barycentric.y +
                        triangle.UvC *
                        barycentric.z,
                        triangle.SampleA *
                        barycentric.x +
                        triangle.SampleB *
                        barycentric.y +
                        triangle.SampleC *
                        barycentric.z);
            }

            return best;
        }

        private static void CopyTemporaryPose(
            SkinnedMeshRenderer target,
            SkinnedMeshRenderer source)
        {
            var sourceBones =
                source.bones.ToDictionary(
                    item => item.name,
                    item => item,
                    StringComparer.Ordinal);
            foreach (var bone in target.bones)
            {
                if (!sourceBones.TryGetValue(
                        bone.name,
                        out var sourceBone))
                {
                    throw new InvalidOperationException(
                        "The temporary Pahur pose bones differ.");
                }

                bone.localPosition =
                    sourceBone.localPosition;
                bone.localRotation =
                    sourceBone.localRotation;
                bone.localScale =
                    sourceBone.localScale;
            }
        }

        private static void RequireShapeAndSkinPreserved(
            Mesh source,
            Mesh appearance)
        {
            if (source == null ||
                appearance == null ||
                source.vertexCount !=
                    appearance.vertexCount ||
                source.bounds !=
                    appearance.bounds ||
                !source.vertices.SequenceEqual(
                    appearance.vertices) ||
                !source.normals.SequenceEqual(
                    appearance.normals) ||
                !source.tangents.SequenceEqual(
                    appearance.tangents) ||
                !source.boneWeights.SequenceEqual(
                    appearance.boneWeights) ||
                !source.bindposes.SequenceEqual(
                    appearance.bindposes))
            {
                throw new InvalidOperationException(
                    "The Pahur running shape, normals, skin weights, or bind poses changed.");
            }

            var sourceTriangleCount =
                Enumerable.Range(
                        0,
                        source.subMeshCount)
                    .Sum(
                        index =>
                            source.GetTriangles(index)
                                .Length);
            var appearanceTriangleCount =
                Enumerable.Range(
                        0,
                        appearance.subMeshCount)
                    .Sum(
                        index =>
                            appearance.GetTriangles(index)
                                .Length);
            if (sourceTriangleCount !=
                appearanceTriangleCount)
            {
                throw new InvalidOperationException(
                    "The Pahur running triangle count changed.");
            }
        }

        private static int Majority(
            int a,
            int b,
            int c)
        {
            if (a == b || a == c)
            {
                return a;
            }

            return b == c ? b : -1;
        }

        private static int SingleMaterialIndex(
            int materialMask)
        {
            for (var index = 0;
                 index < 31;
                 index++)
            {
                if (materialMask ==
                    1 << index)
                {
                    return index;
                }
            }

            throw new InvalidOperationException(
                "The Pahur material mask is not a single material.");
        }

        private static Vector3[] AlignBounds(
            IReadOnlyList<Vector3> points,
            Bounds source,
            Bounds target)
        {
            var scale =
                new Vector3(
                    target.size.x / source.size.x,
                    target.size.y / source.size.y,
                    target.size.z / source.size.z);
            var result =
                new Vector3[points.Count];
            for (var index = 0;
                 index < points.Count;
                 index++)
            {
                result[index] =
                    target.center +
                    Vector3.Scale(
                        points[index] -
                        source.center,
                        scale);
            }

            return result;
        }

        private static Vector3[] WorldPoints(
            Transform transform,
            IReadOnlyList<Vector3> points)
        {
            var result =
                new Vector3[points.Count];
            for (var index = 0;
                 index < points.Count;
                 index++)
            {
                result[index] =
                    transform.TransformPoint(
                        points[index]);
            }

            return result;
        }

        private static Bounds BoundsOf(
            IReadOnlyList<Vector3> points)
        {
            var bounds =
                new Bounds(
                    points[0],
                    Vector3.zero);
            for (var index = 1;
                 index < points.Count;
                 index++)
            {
                bounds.Encapsulate(
                    points[index]);
            }

            return bounds;
        }

        private static void GroundCycle(
            Transform model,
            Animator animator,
            SkinnedMeshRenderer renderer,
            AnimationClip clip,
            float ground)
        {
            var transforms =
                model.GetComponentsInChildren<Transform>(
                        true)
                    .Select(item =>
                        new TransformState(item))
                    .ToArray();
            var enabled = animator.enabled;
            var baked = new Mesh();
            try
            {
                animator.enabled = false;
                var minimum =
                    float.PositiveInfinity;
                for (var index = 0;
                     index <= 8;
                     index++)
                {
                    clip.SampleAnimation(
                        animator.gameObject,
                        clip.length * index / 8f);
                    renderer.BakeMesh(baked);
                    foreach (var vertex in
                             baked.vertices)
                    {
                        minimum =
                            Mathf.Min(
                                minimum,
                                renderer.transform
                                    .TransformPoint(
                                        vertex).y);
                    }
                }

                foreach (var state in transforms)
                {
                    state.Restore();
                }

                model.localPosition +=
                    model.parent
                        .InverseTransformVector(
                            Vector3.up *
                            (ground - minimum));
            }
            finally
            {
                var rootPosition =
                    model.localPosition;
                foreach (var state in transforms)
                {
                    state.Restore();
                }

                model.localPosition =
                    rootPosition;
                animator.enabled = enabled;
                UnityEngine.Object.DestroyImmediate(
                    baked);
            }
        }

        private static void ImportRunningModel()
        {
            var destination =
                Absolute(RunningModelPath);
            if (!File.Exists(destination) ||
                Sha256(destination) !=
                    SourceRunningSha256)
            {
                File.Copy(
                    SourceRunningModelPath,
                    destination,
                    true);
            }

            AssetDatabase.ImportAsset(
                RunningModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static string ConfigureImporter()
        {
            var importer =
                AssetImporter.GetAtPath(
                    RunningModelPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "The running Pahur importer is missing.");
            importer.importAnimation = true;
            importer.animationType =
                ModelImporterAnimationType.Generic;
            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation =
                ModelImporterMaterialLocation.InPrefab;
            var matches =
                importer.defaultClipAnimations
                    .Where(
                        item =>
                            item.name.IndexOf(
                                "mixamo",
                                StringComparison.OrdinalIgnoreCase) >= 0 ||
                            item.takeName.IndexOf(
                                "mixamo",
                                StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The running Pahur FBX must contain exactly one Mixamo take. Found=" +
                    matches.Length + ".");
            }

            var selected = matches[0];
            selected.loopTime = true;
            selected.loopPose = true;
            selected.wrapMode = WrapMode.Loop;
            selected.lockRootPositionXZ =
                true;
            selected.keepOriginalPositionXZ =
                true;
            importer.animationWrapMode =
                WrapMode.Loop;
            importer.clipAnimations =
                new[] { selected };
            importer.SaveAndReimport();
            return selected.name;
        }

        private static float MatchedRunningScale(
            GameObject staticPrefab,
            GameObject runningPrefab,
            Transform staticModel)
        {
            if (!Mathf.Approximately(
                    staticModel.localScale.x,
                    staticModel.localScale.y) ||
                !Mathf.Approximately(
                    staticModel.localScale.x,
                    staticModel.localScale.z))
            {
                throw new InvalidOperationException(
                    "The static Pahur model scale is not uniform.");
            }

            var staticHeight =
                PrefabMeshHeight(
                    staticPrefab);
            var runningHeight =
                PrefabMeshHeight(
                    runningPrefab);
            if (staticHeight <= 0f ||
                runningHeight <= 0f)
            {
                throw new InvalidOperationException(
                    "A Pahur model has no measurable mesh height.");
            }

            return
                staticModel.localScale.x *
                staticHeight /
                runningHeight;
        }

        private static float PrefabMeshHeight(
            GameObject prefab)
        {
            var renderer =
                RequireRenderer(
                    prefab.transform,
                    prefab.name);
            var mesh =
                renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "A Pahur prefab has no mesh.");
            var toRoot =
                prefab.transform.worldToLocalMatrix *
                renderer.transform.localToWorldMatrix;
            var minimum =
                float.PositiveInfinity;
            var maximum =
                float.NegativeInfinity;
            foreach (var vertex in mesh.vertices)
            {
                var y =
                    toRoot.MultiplyPoint3x4(
                        vertex).y;
                minimum =
                    Mathf.Min(
                        minimum,
                        y);
                maximum =
                    Mathf.Max(
                        maximum,
                        y);
            }

            return maximum - minimum;
        }

        private static AnimationClip RequireClip(
            string name)
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(
                        RunningModelPath)
                    .OfType<AnimationClip>()
                    .Where(
                        item =>
                            !item.name.StartsWith(
                                "__preview__",
                                StringComparison.Ordinal))
                    .ToArray();
            if (clips.Length != 1 ||
                clips[0].name != name ||
                !clips[0].isLooping)
            {
                throw new InvalidOperationException(
                    "The selected Pahur Mixamo clip is not the only looping clip.");
            }

            return clips[0];
        }

        private static AnimatorController CreateController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController
                        .CreateAnimatorControllerAtPath(
                            ControllerPath);
            }

            var machine =
                controller.layers[0]
                    .stateMachine;
            foreach (var child in
                     machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            var state =
                machine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip CreateInPlaceClip(
            AnimationClip source,
            Transform runningRoot,
            SkinnedMeshRenderer renderer)
        {
            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    InPlaceClipPath);
            if (clip == null)
            {
                clip =
                    new AnimationClip();
                AssetDatabase.CreateAsset(
                    clip,
                    InPlaceClipPath);
            }

            EditorUtility.CopySerialized(
                source,
                clip);
            clip.name =
                "Pahur_03_Running_InPlace";
            clip.wrapMode =
                WrapMode.Loop;
            var rootPath =
                AnimationUtility.CalculateTransformPath(
                    renderer.rootBone,
                    runningRoot);
            var horizontalLocalProperties =
                new HashSet<string>(
                    StringComparer.Ordinal);
            var localAxes =
                new[]
                {
                    Vector3.right,
                    Vector3.up,
                    Vector3.forward
                };
            var propertySuffixes =
                new[] { "x", "y", "z" };
            for (var axis = 0;
                 axis < localAxes.Length;
                 axis++)
            {
                var worldDirection =
                    renderer.rootBone.parent
                        .TransformDirection(
                            localAxes[axis]);
                var modelDirection =
                    runningRoot
                        .InverseTransformDirection(
                            worldDirection)
                        .normalized;
                if (Mathf.Abs(modelDirection.x) >
                        0.5f ||
                    Mathf.Abs(modelDirection.z) >
                        0.5f)
                {
                    horizontalLocalProperties.Add(
                        "m_LocalPosition." +
                        propertySuffixes[axis]);
                }
            }

            var horizontalBindings =
                AnimationUtility.GetCurveBindings(
                        clip)
                    .Where(
                        binding =>
                            (binding.path.Length == 0 &&
                             (binding.propertyName ==
                                 "RootT.x" ||
                             binding.propertyName ==
                                 "RootT.z" ||
                             binding.propertyName ==
                                 "MotionT.x" ||
                             binding.propertyName ==
                                 "MotionT.z")) ||
                            (binding.path == rootPath &&
                             horizontalLocalProperties
                                 .Contains(
                                     binding.propertyName)))
                    .ToArray();
            if (horizontalBindings.Length == 0)
            {
                throw new InvalidOperationException(
                    "The Mixamo clip has no horizontal root curves to make in-place.");
            }

            foreach (var binding in
                     horizontalBindings)
            {
                var curve =
                    AnimationUtility.GetEditorCurve(
                        clip,
                        binding) ??
                    throw new InvalidOperationException(
                        "A Mixamo horizontal root curve is missing.");
                var value =
                    curve.Evaluate(0f);
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    AnimationCurve.Constant(
                        0f,
                        clip.length,
                        value));
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(
                    clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalPositionXZ =
                true;
            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void
            RequireNoHorizontalRootTranslation(
                Transform root,
                SkinnedMeshRenderer renderer,
                AnimationClip clip)
        {
            var clone =
                UnityEngine.Object.Instantiate(
                    root.gameObject);
            clone.hideFlags =
                HideFlags.HideAndDontSave;
            try
            {
                var cloneRenderer =
                    RequireRenderer(
                        clone.transform,
                        "temporary in-place check");
                var start =
                    Vector3.zero;
                for (var index = 0;
                     index <= 8;
                     index++)
                {
                    clip.SampleAnimation(
                        clone,
                        clip.length *
                        index /
                        8f);
                    var position =
                        clone.transform
                            .InverseTransformPoint(
                                cloneRenderer.rootBone
                                    .position);
                    if (index == 0)
                    {
                        start = position;
                        continue;
                    }

                    if (Mathf.Abs(
                            position.x -
                            start.x) >
                            0.001f ||
                        Mathf.Abs(
                            position.z -
                            start.z) >
                            0.001f)
                    {
                        throw new InvalidOperationException(
                            "The running clip moves the Pahur horizontally.");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    clone);
            }
        }

        private static void Capture(
            Transform model,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid capture path."));
            var states =
                model.GetComponentsInChildren<Transform>(
                        true)
                    .Select(item =>
                        new TransformState(item))
                    .ToArray();
            var otherRenderers =
                model.gameObject.scene
                    .GetRootGameObjects()
                    .SelectMany(
                        item =>
                            item.GetComponentsInChildren<Renderer>(
                                true))
                    .Where(
                        item =>
                            !item.transform.IsChildOf(
                                model))
                    .Select(item =>
                        new RendererState(item))
                    .ToArray();
            var sourceCamera =
                GameObject.Find("Player")
                    ?.GetComponentInChildren<Camera>(
                        true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var cameraObject =
                new GameObject(
                    "PahurRunningReviewCamera",
                    typeof(Camera))
                {
                    hideFlags =
                        HideFlags.HideAndDontSave
                };
            var keyLightObject =
                new GameObject(
                    "PahurRunningReviewKeyLight",
                    typeof(Light))
                {
                    hideFlags =
                        HideFlags.HideAndDontSave
                };
            var fillLightObject =
                new GameObject(
                    "PahurRunningReviewFillLight",
                    typeof(Light))
                {
                    hideFlags =
                        HideFlags.HideAndDontSave
                };
            const int width = 384;
            const int height = 640;
            var strip =
                new Texture2D(
                    width * CapturePanels,
                    height,
                    TextureFormat.RGB24,
                    false);
            var panel =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGB24,
                    false);
            var target =
                new RenderTexture(
                    width,
                    height,
                    24);
            var previousActive =
                RenderTexture.active;
            var animatorEnabled =
                animator.enabled;
            var reviewRenderer =
                RequireRenderer(
                    model,
                    "running review");
            var frameDiagnostics =
                new StringBuilder();
            try
            {
                foreach (var state in
                         otherRenderers)
                {
                    state.Renderer.enabled =
                        false;
                }

                animator.enabled = false;
                var camera =
                    cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags =
                    CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(
                        0.14f,
                        0.15f,
                        0.17f,
                        1f);
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                var keyLight =
                    keyLightObject.GetComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color =
                    new Color(
                        1f,
                        0.91f,
                        0.84f,
                        1f);
                keyLight.intensity = 2f;
                keyLight.shadows = LightShadows.None;
                keyLightObject.transform.rotation =
                    Quaternion.Euler(
                        35f,
                        -35f,
                        0f);
                var fillLight =
                    fillLightObject.GetComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color =
                    new Color(
                        0.72f,
                        0.82f,
                        1f,
                        1f);
                fillLight.intensity = 1.25f;
                fillLight.shadows = LightShadows.None;
                fillLightObject.transform.rotation =
                    Quaternion.Euler(
                        15f,
                        145f,
                        0f);
                clip.SampleAnimation(
                    animator.gameObject,
                    0f);
                FrameCamera(
                    camera,
                    model);
                for (var index = 0;
                     index < CapturePanels;
                     index++)
                {
                    var sampleTime =
                        clip.length * index /
                        (CapturePanels - 1f);
                    clip.SampleAnimation(
                        animator.gameObject,
                        sampleTime);
                    foreach (var particles in
                             model.GetComponentsInChildren<
                                 ParticleSystem>(
                                 true))
                    {
                        particles.Simulate(
                            0.32f +
                            sampleTime,
                            false,
                            true,
                            true);
                    }

                    var hipsInModel =
                        model.InverseTransformPoint(
                            reviewRenderer.rootBone.position);
                    frameDiagnostics.Append(
                        " Frame" +
                        index +
                        " ModelPosition=" +
                        ScaleText(model.localPosition) +
                        " ModelScale=" +
                        ScaleText(model.localScale) +
                        " HipsInModel=" +
                        ScaleText(hipsInModel) +
                        " HipsScale=" +
                        ScaleText(
                            reviewRenderer.rootBone.localScale));
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(
                            0,
                            0,
                            width,
                            height),
                        0,
                        0);
                    panel.Apply();
                    var pixels =
                        panel.GetPixels32();
                    if (pixels.Any(
                            item =>
                                item.r >= 240 &&
                                item.b >= 240 &&
                                item.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "The review contains magenta shader fallback.");
                    }

                    strip.SetPixels32(
                        index * width,
                        0,
                        width,
                        height,
                        pixels);
                }

                strip.Apply();
                File.WriteAllBytes(
                    destination,
                    strip.EncodeToPNG());
                Debug.Log(
                    "PahurRunningReviewFrames" +
                    frameDiagnostics);
            }
            finally
            {
                RenderTexture.active =
                    previousActive;
                foreach (var state in
                         otherRenderers)
                {
                    state.Restore();
                }

                foreach (var state in states)
                {
                    state.Restore();
                }

                animator.enabled =
                    animatorEnabled;
                UnityEngine.Object.DestroyImmediate(
                    panel);
                UnityEngine.Object.DestroyImmediate(
                    strip);
                var camera =
                    cameraObject.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                target.Release();
                UnityEngine.Object.DestroyImmediate(
                    target);
                UnityEngine.Object.DestroyImmediate(
                    keyLightObject);
                UnityEngine.Object.DestroyImmediate(
                    fillLightObject);
                UnityEngine.Object.DestroyImmediate(
                    cameraObject);
            }
        }

        private static void FrameCamera(
            Camera camera,
            Transform model)
        {
            var renderers =
                model.GetComponentsInChildren<Renderer>(
                    false);
            var bounds = renderers[0].bounds;
            for (var index = 1;
                 index < renderers.Length;
                 index++)
            {
                bounds.Encapsulate(
                    renderers[index].bounds);
            }

            var direction =
                new Vector3(
                    0f,
                    0f,
                    -1f);
            var distance =
                bounds.extents.magnitude /
                Mathf.Tan(
                    camera.fieldOfView *
                    Mathf.Deg2Rad *
                    0.5f) *
                1.3f;
            camera.transform.position =
                bounds.center +
                direction * distance;
            camera.transform.rotation =
                Quaternion.LookRotation(
                    bounds.center -
                    camera.transform.position,
                    Vector3.up);
        }

        private static void WriteReport(
            AnimationClip clip,
            Mesh source,
            Mesh appearance,
            Material[] materials,
            Vector3 modelScale)
        {
            var destination =
                Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid report path."));
            var report =
                new StringBuilder();
            report.AppendLine(
                "Pahur Running Model And Animation Validation");
            report.AppendLine("Result=PASS");
            report.AppendLine(
                "SourceSha256=" +
                SourceRunningSha256);
            report.AppendLine(
                "Clip=" + clip.name);
            report.AppendLine("Loop=True");
            report.AppendLine(
                "RunningVertices=" +
                source.vertexCount);
            report.AppendLine(
                "RunningShapePreserved=True");
            report.AppendLine(
                "RunningSkinAndBindPosesPreserved=True");
            report.AppendLine(
                "ApprovedMaterialSlots=" +
                appearance.subMeshCount);
            report.AppendLine(
                "ApprovedMaterials=" +
                string.Join(
                    "|",
                    materials.Select(
                        AssetDatabase.GetAssetPath)));
            report.AppendLine(
                "ApprovedUvAndTextureLayoutTransferred=True");
            report.AppendLine(
                "ModelLocalScale=" +
                ScaleText(modelScale));
            report.AppendLine(
                "StaticReferenceHeightMatched=True");
            report.AppendLine(
                "HorizontalRootMotion=False");
            report.AppendLine(
                "ApplyRootMotion=False");
            File.WriteAllText(
                destination,
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static string ScaleText(
            Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:R},{1:R},{2:R})",
                value.x,
                value.y,
                value.z);
        }

        private static void RequireSourceHash()
        {
            if (!File.Exists(
                    SourceRunningModelPath) ||
                Sha256(
                    SourceRunningModelPath) !=
                    SourceRunningSha256)
            {
                throw new InvalidOperationException(
                    "The supplied Pahur running FBX is missing or changed.");
            }
        }

        private static void RequireApprovedMaterials(
            SkinnedMeshRenderer renderer)
        {
            if (renderer.sharedMaterials.Length == 0 ||
                renderer.sharedMaterials.Any(
                    item =>
                        item == null ||
                        !AssetDatabase.GetAssetPath(item)
                            .StartsWith(
                                ApprovedMaterialFolder,
                                StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "The static Pahur approved material layout is missing.");
            }
        }

        private static void RequireSameBoneNames(
            SkinnedMeshRenderer expected,
            SkinnedMeshRenderer actual)
        {
            if (!expected.bones
                    .Select(item => item.name)
                    .SequenceEqual(
                        actual.bones.Select(
                            item => item.name),
                        StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Static and running Pahur bone order differs.");
            }
        }

        private static Scene RequireScene(
            bool clean)
        {
            var scene =
                SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                !string.Equals(
                    scene.path,
                    ScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            }

            if (clean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            return scene;
        }

        private static GameObject RequirePlacement(
            Scene scene)
        {
            var matches =
                scene.GetRootGameObjects()
                    .Where(
                        item =>
                            item.name ==
                            PlacementRootName)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The Pahur placement root is missing.");
            }

            return matches[0];
        }

        private static void RequireSlots(
            Transform root)
        {
            if (root.childCount !=
                SlotNames.Length)
            {
                throw new InvalidOperationException(
                    "The Pahur slot count differs.");
            }

            for (var index = 0;
                 index < SlotNames.Length;
                 index++)
            {
                if (root.GetChild(index).name !=
                    SlotNames[index])
                {
                    throw new InvalidOperationException(
                        "The Pahur slot order differs.");
                }
            }
        }

        private static Transform RequireChild(
            Transform parent,
            string name)
        {
            var matches =
                parent.Cast<Transform>()
                    .Where(item =>
                        item.name == name)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    parent.name +
                    " must contain exactly one " +
                    name + ".");
            }

            return matches[0];
        }

        private static Transform RequireModel(
            Transform slot)
        {
            return RequireChild(
                slot,
                ModelName);
        }

        private static SkinnedMeshRenderer
            RequireRenderer(
                Transform root,
                string label)
        {
            var renderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    label +
                    " must contain exactly one skinned renderer.");
            }

            return renderers[0];
        }

        private static Dictionary<string, string>
            OtherSlotSignatures(
                Transform placement)
        {
            return OtherSlotSignatures(
                placement,
                MoveSlotName);
        }

        private static Dictionary<string, string>
            OtherSlotSignatures(
                Transform placement,
                string excludedSlotName)
        {
            return placement.Cast<Transform>()
                .Where(item =>
                    item.name !=
                    excludedSlotName)
                .ToDictionary(
                    item => item.name,
                    Signature,
                    StringComparer.Ordinal);
        }

        private static Dictionary<string, string>
            ProtectedRootSignatures(
                Scene scene,
                Transform placement)
        {
            return scene.GetRootGameObjects()
                .Where(item =>
                    item.transform != placement)
                .ToDictionary(
                    item => item.name,
                    item =>
                        Signature(
                            item.transform),
                    StringComparer.Ordinal);
        }

        private static string Signature(
            Transform root)
        {
            return string.Join(
                "\n",
                root.GetComponentsInChildren<Transform>(
                        true)
                    .Select(
                        item =>
                            AnimationUtility
                                .CalculateTransformPath(
                                    item,
                                    root) +
                            ":" +
                            item.localPosition
                                .ToString("R") +
                            ":" +
                            item.localRotation
                                .ToString("R") +
                            ":" +
                            item.localScale
                                .ToString("R") +
                            ":" +
                            item.gameObject.activeSelf));
        }

        private static void RequireUnchanged(
            IReadOnlyDictionary<string, string> before,
            IReadOnlyDictionary<string, string> after,
            string message)
        {
            if (before.Count != after.Count ||
                before.Any(
                    item =>
                        !after.TryGetValue(
                            item.Key,
                            out var value) ||
                        value != item.Value))
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Absolute(
            string relative)
        {
            var project =
                Directory.GetParent(
                    Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "The project root is missing.");
            return Path.GetFullPath(
                Path.Combine(
                    project,
                    relative.Replace(
                        '/',
                        Path.DirectorySeparatorChar)));
        }

        private static string Sha256(
            string path)
        {
            using (var stream =
                   File.OpenRead(path))
            using (var sha =
                   SHA256.Create())
            {
                return BitConverter
                    .ToString(
                        sha.ComputeHash(stream))
                    .Replace(
                        "-",
                        string.Empty);
            }
        }

        private readonly struct SourceTriangle
        {
            public SourceTriangle(
                Vector3 a,
                Vector3 b,
                Vector3 c,
                Vector2 uvA,
                Vector2 uvB,
                Vector2 uvC,
                Vector4 sampleA,
                Vector4 sampleB,
                Vector4 sampleC,
                int material)
            {
                A = a;
                B = b;
                C = c;
                UvA = uvA;
                UvB = uvB;
                UvC = uvC;
                SampleA =
                    new Vector3(
                        sampleA.x,
                        sampleA.y,
                        sampleA.z);
                SampleB =
                    new Vector3(
                        sampleB.x,
                        sampleB.y,
                        sampleB.z);
                SampleC =
                    new Vector3(
                        sampleC.x,
                        sampleC.y,
                        sampleC.z);
                Material = material;
            }

            public Vector3 A { get; }
            public Vector3 B { get; }
            public Vector3 C { get; }
            public Vector2 UvA { get; }
            public Vector2 UvB { get; }
            public Vector2 UvC { get; }
            public Vector3 SampleA { get; }
            public Vector3 SampleB { get; }
            public Vector3 SampleC { get; }
            public int Material { get; }
        }

        private readonly struct SurfaceHit
        {
            public SurfaceHit(
                int material,
                Vector2 uv,
                Vector3 sample)
            {
                Material = material;
                Uv = uv;
                Sample = sample;
            }

            public int Material { get; }
            public Vector2 Uv { get; }
            public Vector3 Sample { get; }
        }

        private sealed class TransformState
        {
            private readonly Transform _transform;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;
            private readonly Vector3 _scale;

            public TransformState(
                Transform transform)
            {
                _transform = transform;
                _position =
                    transform.localPosition;
                _rotation =
                    transform.localRotation;
                _scale =
                    transform.localScale;
            }

            public void Restore()
            {
                if (_transform == null)
                {
                    return;
                }

                _transform
                    .SetLocalPositionAndRotation(
                        _position,
                        _rotation);
                _transform.localScale =
                    _scale;
            }
        }

        private sealed class RendererState
        {
            public RendererState(
                Renderer renderer)
            {
                Renderer = renderer;
                Enabled = renderer.enabled;
            }

            public Renderer Renderer { get; }
            private bool Enabled { get; }

            public void Restore()
            {
                if (Renderer != null)
                {
                    Renderer.enabled = Enabled;
                }
            }
        }

        private static Vector3 ClosestBarycentric(
            Vector3 point,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = point - a;
            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
            {
                return new Vector3(1f, 0f, 0f);
            }

            var bp = point - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
            {
                return new Vector3(0f, 1f, 0f);
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0f &&
                d1 >= 0f &&
                d3 <= 0f)
            {
                var v =
                    d1 / (d1 - d3);
                return new Vector3(
                    1f - v,
                    v,
                    0f);
            }

            var cp = point - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
            {
                return new Vector3(0f, 0f, 1f);
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0f &&
                d2 >= 0f &&
                d6 <= 0f)
            {
                var w =
                    d2 / (d2 - d6);
                return new Vector3(
                    1f - w,
                    0f,
                    w);
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0f &&
                d4 - d3 >= 0f &&
                d5 - d6 >= 0f)
            {
                var w =
                    (d4 - d3) /
                    ((d4 - d3) +
                     (d5 - d6));
                return new Vector3(
                    0f,
                    1f - w,
                    w);
            }

            var denominator =
                1f / (va + vb + vc);
            var vInside =
                vb * denominator;
            var wInside =
                vc * denominator;
            return new Vector3(
                1f - vInside -
                wInside,
                vInside,
                wInside);
        }
    }
}
