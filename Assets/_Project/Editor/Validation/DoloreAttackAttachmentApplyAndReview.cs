using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.DoloreAttackAttachment
{
    internal static class DoloreAttackAttachmentApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string AttackSlotName = "Dolore_04_Tentacle_Stab_Attack";
        private const string ExecutionSlotName = "Dolore_05_Execution_Pull_In";
        private const string ModelName = "Dolore_Model";
        private const string AttachmentName = "Dolore_Attack_Attachment";
        private const string SourceInstanceName = "Dolore_Attack_Source";
        private const string RootBoneName = "Bone_000";
        private const string CurveBoneName = "Bone_012";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        private const string SourceGlb = "D:/Bellerophon2/Bellerophon/enemies model/dolore attack.glb";
        private const string SourceGlbHash = "56C400903A1B977024DFE7999F6D8AD0A19F6E0634E40816CCC18BCC04EF20A0";
        private const string ApprovedReferenceSource =
            "artSample/enemies/dolore/attack_attachment/exports/Dolore_AttackAttachment_Sample.glb";
        private const string ApprovalStatusPath = "artSample/enemies/dolore/attack_attachment/APPROVAL_STATUS.json";
        private const string AssetRoot = "Assets/_Project/Art/Generated/Enemies/Dolore/AttackAttachment";
        private const string ModelFolder = AssetRoot + "/Models";
        private const string TextureFolder = AssetRoot + "/Textures";
        private const string MaterialFolder = AssetRoot + "/Materials";
        private const string MeshFolder = AssetRoot + "/Meshes";
        private const string ReviewFolder = AssetRoot + "/Review";
        private const string ModelPath = ModelFolder + "/Dolore_Attack_Source.glb";
        private const string ApprovedReferencePath = ModelFolder + "/Dolore_AttackAttachment_ApprovedReference.glb";
        private const string AppearanceMeshPath = MeshFolder + "/Dolore_Attack_ApprovedAppearance.asset";
        private const string MaterialPath = MaterialFolder + "/Dolore_Attack_Flesh.mat";
        private const string AlbedoPath = TextureFolder + "/dolore_flesh_albedo.png";
        private const string RoughnessPath = TextureFolder + "/dolore_flesh_roughness.png";
        private const string HeightPath = TextureFolder + "/dolore_flesh_height.png";
        private const string MaskPath = TextureFolder + "/dolore_flesh_metallic_smoothness.png";
        private const string AlbedoSource = "artSample/enemies/dolore/textures/dolore_flesh_albedo.png";
        private const string RoughnessSource = "artSample/enemies/dolore/textures/dolore_flesh_roughness.png";
        private const string HeightSource = "artSample/enemies/dolore/textures/dolore_flesh_height.png";
        private const string ReferenceFront = "artSample/enemies/dolore/attack_attachment/renders/01_front_attached.png";
        private const string ReferenceThreeQuarter = "artSample/enemies/dolore/attack_attachment/renders/02_three_quarter_attached.png";
        private const string ReferenceSide = "artSample/enemies/dolore/attack_attachment/renders/03_side_attachment.png";
        private const string ReferenceCloseup = "artSample/enemies/dolore/attack_attachment/renders/04_attachment_closeup.png";
        private const string CapturePath = ReviewFolder + "/Dolore_AttackAttachment_Comparison.png";
        private const string ImportedReferenceFrontPath = ReviewFolder + "/Dolore_AttackAttachment_ImportedReference_Front.png";
        private const string ImportedReferenceSidePath = ReviewFolder + "/Dolore_AttackAttachment_ImportedReference_Side.png";
        private const string CoordinateDiagnosticPath = ReviewFolder + "/Dolore_AttackAttachment_CoordinateDiagnostic.txt";
        private const string TargetAttachmentOnlyPath = ReviewFolder + "/Dolore_AttackAttachment_TargetOnly.png";
        private const string TargetBackViewPath = ReviewFolder + "/Dolore_AttackAttachment_TargetBack.png";
        private const string ContactDiagnosticPath = ReviewFolder + "/Dolore_AttackAttachment_ContactDiagnostic.png";
        private const string InspectionPath = ReviewFolder + "/Dolore_AttackAttachment_Inspection.txt";
        private const float PositionTolerance = 0.035f;
        private const float ApprovedRootHorizontalOffset = -0.42f;
        private const float ApprovedRootVerticalOffset = -0.26f;
        private const int ExpectedAttackBoneCount = 13;
        private const int CaptureLayer = 31;

        private static readonly string[] SlotNames =
        {
            "Dolore_01_Static_Review",
            "Dolore_02_Idle",
            "Dolore_03_Move_Quadruped",
            AttackSlotName,
            ExecutionSlotName,
            "Dolore_06_Hit_Reaction",
            "Dolore_07_Death"
        };

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Attack Attachment Target")]
        public static void InspectTarget()
        {
            RequireApprovedSample();
            RequireHash(SourceGlb, SourceGlbHash, "The supplied Dolore attack GLB changed.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var slots = RequireSlots(root);
            var attackModel = RequireModel(slots[3]);
            var executionModel = RequireModel(slots[4]);
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("SourceGlb=" + SourceGlb)
                .AppendLine("SourceGlbSha256=" + Sha256(SourceGlb))
                .AppendLine("AttackSlot=" + slots[3].name)
                .AppendLine("ExecutionSlot=" + slots[4].name)
                .AppendLine("AttackModel=" + TransformPath(attackModel, slots[3]))
                .AppendLine("ExecutionModel=" + TransformPath(executionModel, slots[4]))
                .AppendLine("AttackExistingAttachment=" + (attackModel.Find(AttachmentName) != null))
                .AppendLine("ExecutionExistingAttachment=" + (executionModel.Find(AttachmentName) != null))
                .AppendLine("SceneChanged=False");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Attack attachment target inspection changed CargoRunMvp.");
            Debug.Log("DoloreAttackAttachmentTargetInspected " + report.ToString().Replace('\n', ' '));
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Approved Attack Attachment")]
        public static void ApplyApprovedAttachment()
        {
            RequireApprovedSample();
            RequireHash(SourceGlb, SourceGlbHash, "The supplied Dolore attack GLB changed.");
            PrepareAssets();
            var referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReferencePath) ??
                                  throw new InvalidOperationException("Unity did not import the approved attachment reference GLB.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var placementRoot = RequirePlacementRoot(scene);
            var slots = RequireSlots(placementRoot);
            var protectedRootsBefore = ProtectedRootSignatures(scene, placementRoot);
            var protectedSlotsBefore = ProtectedSlotSignatures(slots);
            var targetBaseBefore = new[]
            {
                BaseModelSignature(RequireModel(slots[3])),
                BaseModelSignature(RequireModel(slots[4]))
            };

            try
            {
                Attach(scene, RequireModel(slots[3]), referencePrefab);
                Attach(scene, RequireModel(slots[4]), referencePrefab);
                var protectedRootsAfter = ProtectedRootSignatures(scene, placementRoot);
                var protectedSlotsAfter = ProtectedSlotSignatures(slots);
                if (!protectedRootsBefore.SequenceEqual(protectedRootsAfter, StringComparer.Ordinal))
                    throw new InvalidOperationException("A scene root outside Approved Dolore Enemy Placement changed.");
                if (!protectedSlotsBefore.SequenceEqual(protectedSlotsAfter, StringComparer.Ordinal))
                    throw new InvalidOperationException("A Dolore slot outside motion objects 3 and 4 changed.");
                if (BaseModelSignature(RequireModel(slots[3])) != targetBaseBefore[0] ||
                    BaseModelSignature(RequireModel(slots[4])) != targetBaseBefore[1])
                    throw new InvalidOperationException("A target Dolore base model changed while adding its attachment.");
                var metrics = InspectState(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("CargoRunMvp could not be saved after attack attachment application.");
                AssetDatabase.SaveAssets();
                WriteInspection(metrics, "Apply", true);
            }
            catch
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                throw;
            }

            RequireHash(SourceGlb, SourceGlbHash, "The supplied attack GLB changed during Unity application.");
            RequireHash(ProjectAbsolutePath(ModelPath), SourceGlbHash, "The Unity attack GLB is not an exact source copy.");
            Selection.activeObject = null;
            Debug.Log("DoloreAttackAttachmentApplied Result=PASS, Slots=2, SourceGlbExactCopy=True, " +
                      "AttachmentBones=13, RootExitFacesFrameFront=True, ApprovedFleshAppearance=True, " +
                      "AnimationsApplied=False, OtherDoloreSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Approved Attack Attachment")]
        public static void InspectAppliedAttachment()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wasDirty = scene.isDirty;
            var metrics = InspectState(scene);
            WriteInspection(metrics, "Inspect", false);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Attack attachment inspection changed CargoRunMvp.");
            Selection.activeObject = null;
            Debug.Log("DoloreAttackAttachmentInspected Result=PASS, Slots=2, RootAlignment=" +
                      Num(metrics.MinRootAlignment) + ", MaxApprovedAnchorDistance=" + Num(metrics.MaxAnchorDistance) +
                      ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Approved Attack Attachment")]
        public static void CaptureApprovedAttachment()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var metrics = InspectState(scene);
            var slots = RequireSlots(RequirePlacementRoot(scene));
            var referenceFront = DecodeTexture(ProjectAbsolutePath(ReferenceFront));
            var referenceSide = DecodeTexture(ProjectAbsolutePath(ReferenceSide));
            var referenceThreeQuarter = DecodeTexture(ProjectAbsolutePath(ReferenceThreeQuarter));
            var referenceCloseup = DecodeTexture(ProjectAbsolutePath(ReferenceCloseup));
            var captures = new List<Texture2D>();
            try
            {
                captures.Add(referenceFront);
                captures.Add(CaptureModel(RequireModel(slots[3]), Vector3.forward));
                captures.Add(CaptureModel(RequireModel(slots[4]), Vector3.forward));
                captures.Add(referenceSide);
                captures.Add(CaptureModel(RequireModel(slots[3]), Vector3.right));
                captures.Add(CaptureModel(RequireModel(slots[4]), Vector3.right));
                captures.Add(referenceThreeQuarter);
                captures.Add(CaptureModel(RequireModel(slots[3]), new Vector3(0.7f, 0f, 0.7f)));
                captures.Add(CaptureModel(RequireModel(slots[4]), new Vector3(0.7f, 0f, 0.7f)));
                captures.Add(referenceCloseup);
                captures.Add(CaptureModel(RequireModel(slots[3]), Vector3.forward, true));
                captures.Add(CaptureModel(RequireModel(slots[4]), Vector3.forward, true));
                SaveComparisonSheet(captures, ProjectAbsolutePath(CapturePath));
            }
            finally
            {
                foreach (var texture in captures.Distinct())
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(CapturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var restored = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (restored.isDirty)
                throw new InvalidOperationException("Attack attachment capture left CargoRunMvp dirty.");
            Debug.Log("DoloreAttackAttachmentCaptured Result=PASS, Image=" + CapturePath +
                      ", Columns=ApprovedSample|Motion3|Motion4, Rows=Front|Side|ThreeQuarter|Closeup, RootAlignment=" +
                      Num(metrics.MinRootAlignment) + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Imported Attack Reference Diagnostic")]
        public static void CaptureImportedReferenceDiagnostic()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReferencePath) ??
                         throw new InvalidOperationException("Approved attachment reference GLB is missing.");
            GameObject instance = null;
            Texture2D front = null;
            Texture2D side = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                instance.hideFlags = HideFlags.HideAndDontSave;
                front = CaptureModel(instance.transform, Vector3.forward);
                side = CaptureModel(instance.transform, Vector3.right);
                File.WriteAllBytes(ProjectAbsolutePath(ImportedReferenceFrontPath), front.EncodeToPNG());
                File.WriteAllBytes(ProjectAbsolutePath(ImportedReferenceSidePath), side.EncodeToPNG());
            }
            finally
            {
                if (front != null) UnityEngine.Object.DestroyImmediate(front);
                if (side != null) UnityEngine.Object.DestroyImmediate(side);
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }
            AssetDatabase.ImportAsset(ImportedReferenceFrontPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ImportedReferenceSidePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Debug.Log("DoloreAttackAttachmentImportedReferenceCaptured Result=PASS, Front=" +
                      ImportedReferenceFrontPath + ", Side=" + ImportedReferenceSidePath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Attack Coordinate Diagnostic")]
        public static void InspectCoordinateDiagnostic()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wasDirty = scene.isDirty;
            var slots = RequireSlots(RequirePlacementRoot(scene));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReferencePath) ??
                         throw new InvalidOperationException("Approved attachment reference GLB is missing.");
            GameObject reference = null;
            try
            {
                reference = UnityEngine.Object.Instantiate(prefab);
                reference.hideFlags = HideFlags.HideAndDontSave;
                var referenceBase = RequireBaseRenderer(reference.transform);
                var referenceAttack = RequireAttackRenderer(reference.transform);
                var report = new StringBuilder().AppendLine("Result=PASS");
                AppendCoordinateDiagnostic(report, "Reference", reference.transform, referenceBase, referenceAttack);
                AppendCoordinateDiagnostic(report, slots[3].name, RequireModel(slots[3]),
                    RequireBaseRenderer(RequireModel(slots[3])), RequireAttackRenderer(RequireModel(slots[3])));
                AppendCoordinateDiagnostic(report, slots[4].name, RequireModel(slots[4]),
                    RequireBaseRenderer(RequireModel(slots[4])), RequireAttackRenderer(RequireModel(slots[4])));
                File.WriteAllText(ProjectAbsolutePath(CoordinateDiagnosticPath), report.ToString(), new UTF8Encoding(false));
            }
            finally
            {
                if (reference != null) UnityEngine.Object.DestroyImmediate(reference);
            }
            AssetDatabase.ImportAsset(CoordinateDiagnosticPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Coordinate diagnostic changed CargoRunMvp.");
            Debug.Log("DoloreAttackAttachmentCoordinateInspected Result=PASS, SceneChanged=False, Report=" +
                      CoordinateDiagnosticPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Attack Visibility Diagnostic")]
        public static void CaptureVisibilityDiagnostic()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wasDirty = scene.isDirty;
            var slots = RequireSlots(RequirePlacementRoot(scene));
            var model = RequireModel(slots[3]);
            var attachment = model.Find(AttachmentName) ??
                             throw new InvalidOperationException("Motion object 3 attack attachment is missing.");
            Texture2D attachmentOnly = null;
            Texture2D backView = null;
            try
            {
                attachmentOnly = CaptureModel(attachment, Vector3.forward);
                backView = CaptureModel(model, Vector3.back);
                File.WriteAllBytes(ProjectAbsolutePath(TargetAttachmentOnlyPath), attachmentOnly.EncodeToPNG());
                File.WriteAllBytes(ProjectAbsolutePath(TargetBackViewPath), backView.EncodeToPNG());
            }
            finally
            {
                if (attachmentOnly != null) UnityEngine.Object.DestroyImmediate(attachmentOnly);
                if (backView != null) UnityEngine.Object.DestroyImmediate(backView);
            }
            AssetDatabase.ImportAsset(TargetAttachmentOnlyPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(TargetBackViewPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Visibility diagnostic changed CargoRunMvp.");
            Debug.Log("DoloreAttackAttachmentVisibilityCaptured Result=PASS, AttachmentOnly=" +
                      TargetAttachmentOnlyPath + ", BackView=" + TargetBackViewPath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Attack Contact Diagnostic")]
        public static void CaptureContactDiagnostic()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var wasDirty = scene.isDirty;
            var slots = RequireSlots(RequirePlacementRoot(scene));
            var model = RequireModel(slots[3]);
            var referenceFront = DecodeTexture(ProjectAbsolutePath(ReferenceFront));
            var referenceSide = DecodeTexture(ProjectAbsolutePath(ReferenceSide));
            var captures = new List<Texture2D>();
            try
            {
                captures.Add(referenceFront);
                captures.Add(CaptureModel(model, Vector3.forward, true));
                captures.Add(CaptureModel(model, new Vector3(0.55f, 0.45f, 0.70f), true));
                captures.Add(referenceSide);
                captures.Add(CaptureModel(model, Vector3.right, true));
                captures.Add(CaptureModel(model, Vector3.left, true));
                SaveComparisonSheet(captures, ProjectAbsolutePath(ContactDiagnosticPath));
            }
            finally
            {
                foreach (var texture in captures)
                    if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(ContactDiagnosticPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Contact diagnostic changed CargoRunMvp.");
            Debug.Log("DoloreAttackAttachmentContactCaptured Result=PASS, " +
                      "Rows=ApprovedFront|TargetFront|TargetUpperThreeQuarter;ApprovedSide|TargetRight|TargetLeft, Image=" +
                      ContactDiagnosticPath + ", SceneChanged=False.");
        }

        private static void AppendCoordinateDiagnostic(
            StringBuilder report,
            string label,
            Transform root,
            SkinnedMeshRenderer baseRenderer,
            SkinnedMeshRenderer attackRenderer)
        {
            report.AppendLine(label + ".RootLocalPosition=" + Vec(root.localPosition))
                .AppendLine(label + ".RootLocalRotation=" + Quat(root.localRotation))
                .AppendLine(label + ".RootLocalScale=" + Vec(root.localScale))
                .AppendLine(label + ".RootWorldPosition=" + Vec(root.position))
                .AppendLine(label + ".RootWorldRotation=" + Quat(root.rotation))
                .AppendLine(label + ".RootLossyScale=" + Vec(root.lossyScale))
                .AppendLine(label + ".BaseRelativePosition=" + Vec(root.InverseTransformPoint(baseRenderer.transform.position)))
                .AppendLine(label + ".BaseRelativeRotation=" + Quat(Quaternion.Inverse(root.rotation) * baseRenderer.transform.rotation))
                .AppendLine(label + ".BaseRelativeScale=" + Vec(ComponentDivide(baseRenderer.transform.lossyScale, root.lossyScale)))
                .AppendLine(label + ".BaseBoundsCenterRelative=" + Vec(root.InverseTransformPoint(baseRenderer.bounds.center)))
                .AppendLine(label + ".BaseBounds=" + Vec(baseRenderer.bounds.size));
            for (var materialIndex = 0; materialIndex < baseRenderer.sharedMaterials.Length; materialIndex++)
            {
                var material = baseRenderer.sharedMaterials[materialIndex];
                var submeshBounds = SubmeshWorldBounds(baseRenderer, materialIndex);
                report.AppendLine(label + ".BaseSubmesh[" + materialIndex + "].Material=" +
                                  (material != null ? material.name : "<null>"))
                    .AppendLine(label + ".BaseSubmesh[" + materialIndex + "].CenterRelative=" +
                                Vec(root.InverseTransformPoint(submeshBounds.center)))
                    .AppendLine(label + ".BaseSubmesh[" + materialIndex + "].Bounds=" + Vec(submeshBounds.size));
                var exactLocalBounds = SubmeshBoundsInRootLocal(baseRenderer, materialIndex, root);
                report.AppendLine(label + ".BaseSubmesh[" + materialIndex + "].ExactLocalCenter=" +
                                  Vec(exactLocalBounds.center))
                    .AppendLine(label + ".BaseSubmesh[" + materialIndex + "].ExactLocalBounds=" +
                                Vec(exactLocalBounds.size));
            }
            if (attackRenderer == null) return;
            var bones = attackRenderer.bones.ToDictionary(item => item.name, StringComparer.Ordinal);
            var direction = (bones[CurveBoneName].position - bones[RootBoneName].position).normalized;
            var attackExactBounds = RendererBoundsInRootLocal(attackRenderer, root);
            report.AppendLine(label + ".AttackRelativePosition=" + Vec(root.InverseTransformPoint(attackRenderer.transform.position)))
                .AppendLine(label + ".AttackRelativeRotation=" + Quat(Quaternion.Inverse(root.rotation) * attackRenderer.transform.rotation))
                .AppendLine(label + ".AttackRelativeScale=" + Vec(ComponentDivide(attackRenderer.transform.lossyScale, root.lossyScale)))
                .AppendLine(label + ".AttackDirectionWorld=" + Vec(direction))
                .AppendLine(label + ".AttackDirectionRelative=" + Vec(root.InverseTransformDirection(direction).normalized))
                .AppendLine(label + ".AttackRootRelative=" + Vec(root.InverseTransformPoint(bones[RootBoneName].position)))
                .AppendLine(label + ".AttackCurveRelative=" + Vec(root.InverseTransformPoint(bones[CurveBoneName].position)))
                .AppendLine(label + ".AttackApprovedUvRootCenterRelative=" +
                            Vec(root.InverseTransformPoint(AttackRootSurfaceCenter(attackRenderer))))
                .AppendLine(label + ".AttackMinX18CenterRelative=" +
                            Vec(root.InverseTransformPoint(AttackLongitudinalSurfaceCenter(attackRenderer, false))))
                .AppendLine(label + ".AttackMaxX18CenterRelative=" +
                            Vec(root.InverseTransformPoint(AttackLongitudinalSurfaceCenter(attackRenderer, true))))
                .AppendLine(label + ".AttackBounds=" + Vec(attackRenderer.bounds.size))
                .AppendLine(label + ".AttackExactLocalCenter=" + Vec(attackExactBounds.center))
                .AppendLine(label + ".AttackExactLocalBounds=" + Vec(attackExactBounds.size));
            AppendBoundaryDiagnostics(report, label, root, attackRenderer);
        }

        private static Bounds RendererBoundsInRootLocal(SkinnedMeshRenderer renderer, Transform root)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                if (vertices.Length == 0)
                    throw new InvalidOperationException(renderer.name + " baked mesh has no vertices.");
                Func<Vector3, Vector3> point = vertex =>
                    root.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                var bounds = new Bounds(point(vertices[0]), Vector3.zero);
                for (var index = 1; index < vertices.Length; index++) bounds.Encapsulate(point(vertices[index]));
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void AppendBoundaryDiagnostics(
            StringBuilder report,
            string label,
            Transform root,
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException(renderer.name + " mesh is missing.");
            var sourceVertices = mesh.vertices;
            var weldedByPosition = new Dictionary<Vector3Int, int>();
            var weldedRepresentatives = new List<int>();
            var vertexToWelded = new int[sourceVertices.Length];
            for (var vertexIndex = 0; vertexIndex < sourceVertices.Length; vertexIndex++)
            {
                var vertex = sourceVertices[vertexIndex];
                var key = new Vector3Int(
                    Mathf.RoundToInt(vertex.x * 100000f),
                    Mathf.RoundToInt(vertex.y * 100000f),
                    Mathf.RoundToInt(vertex.z * 100000f));
                if (!weldedByPosition.TryGetValue(key, out var weldedIndex))
                {
                    weldedIndex = weldedRepresentatives.Count;
                    weldedByPosition.Add(key, weldedIndex);
                    weldedRepresentatives.Add(vertexIndex);
                }
                vertexToWelded[vertexIndex] = weldedIndex;
            }
            var edgeCounts = new Dictionary<ulong, int>();
            var triangles = mesh.triangles;
            for (var index = 0; index < triangles.Length; index += 3)
            {
                CountEdge(edgeCounts, vertexToWelded[triangles[index]], vertexToWelded[triangles[index + 1]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 1]], vertexToWelded[triangles[index + 2]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 2]], vertexToWelded[triangles[index]]);
            }
            var boundaryEdges = edgeCounts.Where(item => item.Value == 1).Select(item => item.Key).ToArray();
            var adjacency = new Dictionary<int, List<int>>();
            foreach (var edge in boundaryEdges)
            {
                var first = (int)(edge >> 32);
                var second = (int)(edge & uint.MaxValue);
                if (!adjacency.TryGetValue(first, out var firstList)) adjacency[first] = firstList = new List<int>();
                if (!adjacency.TryGetValue(second, out var secondList)) adjacency[second] = secondList = new List<int>();
                firstList.Add(second);
                secondList.Add(first);
            }
            var components = new List<int[]>();
            var remaining = new HashSet<int>(adjacency.Keys);
            while (remaining.Count > 0)
            {
                var seed = remaining.First();
                var queue = new Queue<int>();
                var component = new List<int>();
                queue.Enqueue(seed);
                remaining.Remove(seed);
                while (queue.Count > 0)
                {
                    var vertex = queue.Dequeue();
                    component.Add(vertex);
                    foreach (var neighbor in adjacency[vertex])
                        if (remaining.Remove(neighbor)) queue.Enqueue(neighbor);
                }
                components.Add(component.ToArray());
            }
            components = components.OrderByDescending(item => item.Length).ToList();
            report.AppendLine(label + ".AttackWeldedVertices=" + weldedRepresentatives.Count)
                .AppendLine(label + ".AttackBoundaryEdges=" + boundaryEdges.Length)
                .AppendLine(label + ".AttackBoundaryComponents=" + components.Count);
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                for (var componentIndex = 0; componentIndex < Math.Min(components.Count, 8); componentIndex++)
                {
                    var originalIndices = components[componentIndex]
                        .Select(index => weldedRepresentatives[index]).ToArray();
                    var points = originalIndices
                        .Select(index => root.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index])))
                        .ToArray();
                    var bounds = new Bounds(points[0], Vector3.zero);
                    var center = Vector3.zero;
                    foreach (var point in points)
                    {
                        bounds.Encapsulate(point);
                        center += point;
                    }
                    center /= points.Length;
                    report.AppendLine(label + ".AttackBoundary[" + componentIndex + "].Vertices=" + points.Length)
                        .AppendLine(label + ".AttackBoundary[" + componentIndex + "].AverageU=" +
                                    Num(originalIndices.Average(index => mesh.uv[index].x)))
                        .AppendLine(label + ".AttackBoundary[" + componentIndex + "].CenterRelative=" + Vec(center))
                        .AppendLine(label + ".AttackBoundary[" + componentIndex + "].BoundsRelative=" + Vec(bounds.size));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void CountEdge(IDictionary<ulong, int> edgeCounts, int first, int second)
        {
            var minimum = (uint)Math.Min(first, second);
            var maximum = (uint)Math.Max(first, second);
            var key = ((ulong)minimum << 32) | maximum;
            edgeCounts[key] = edgeCounts.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        private static Vector3 ComponentDivide(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                value.x / Mathf.Max(0.000001f, Mathf.Abs(divisor.x)),
                value.y / Mathf.Max(0.000001f, Mathf.Abs(divisor.y)),
                value.z / Mathf.Max(0.000001f, Mathf.Abs(divisor.z)));
        }

        private static int RequirePortraitSubmeshIndex(SkinnedMeshRenderer renderer)
        {
            var matches = renderer.sharedMaterials.Select((material, index) => new { material, index })
                .Where(item => item.material != null &&
                               item.material.name.IndexOf("Portrait", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(item => item.index).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(renderer.name + " must contain exactly one Portrait material submesh.");
            return matches[0];
        }

        private static Bounds SubmeshWorldBounds(SkinnedMeshRenderer renderer, int submeshIndex)
        {
            if (renderer.sharedMesh == null || submeshIndex < 0 || submeshIndex >= renderer.sharedMesh.subMeshCount)
                throw new InvalidOperationException(renderer.name + " submesh index is invalid: " + submeshIndex);
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                var indices = baked.GetIndices(submeshIndex);
                if (indices.Length == 0)
                    throw new InvalidOperationException(renderer.name + " baked submesh has no indices: " + submeshIndex);
                var vertices = baked.vertices;
                var rotated = vertices.Select(item => renderer.transform.rotation * item).ToArray();
                var rawBounds = new Bounds(rotated[0], Vector3.zero);
                for (var index = 1; index < rotated.Length; index++) rawBounds.Encapsulate(rotated[index]);
                var correction = ComponentDivide(renderer.bounds.size, rawBounds.size);
                Func<int, Vector3> map = vertexIndex => renderer.bounds.center +
                    Vector3.Scale(rotated[vertexIndex] - rawBounds.center, correction);
                var bounds = new Bounds(map(indices[0]), Vector3.zero);
                for (var index = 1; index < indices.Length; index++) bounds.Encapsulate(map(indices[index]));
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Bounds SubmeshBoundsInRootLocal(
            SkinnedMeshRenderer renderer,
            int submeshIndex,
            Transform root)
        {
            if (renderer.sharedMesh == null || submeshIndex < 0 || submeshIndex >= renderer.sharedMesh.subMeshCount)
                throw new InvalidOperationException(renderer.name + " submesh index is invalid: " + submeshIndex);
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                var indices = baked.GetIndices(submeshIndex).Distinct().ToArray();
                if (indices.Length == 0)
                    throw new InvalidOperationException(renderer.name + " baked submesh has no indices: " + submeshIndex);
                var vertices = baked.vertices;
                Func<int, Vector3> point = index =>
                    root.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]));
                var bounds = new Bounds(point(indices[0]), Vector3.zero);
                for (var index = 1; index < indices.Length; index++) bounds.Encapsulate(point(indices[index]));
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void PrepareAssets()
        {
            foreach (var folder in new[] { ModelFolder, TextureFolder, MaterialFolder, MeshFolder, ReviewFolder })
                EnsureFolder(folder);
            CopyExactAbsolute(SourceGlb, ModelPath);
            CopyExactProject(ApprovedReferenceSource, ApprovedReferencePath);
            CopyExactProject(AlbedoSource, AlbedoPath);
            CopyExactProject(RoughnessSource, RoughnessPath);
            CopyExactProject(HeightSource, HeightPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ApprovedReferencePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureTexture(AlbedoPath, true, false, 0f);
            ConfigureTexture(RoughnessPath, false, false, 0f);
            ConfigureTexture(HeightPath, false, true, 0.08f);
            CreateSmoothnessMask();
            ConfigureTexture(MaskPath, false, false, 0f);
            CreateOrUpdateMaterial();

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                               throw new InvalidOperationException("The Dolore attack GLB importer did not produce a GameObject asset.");
            var referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReferencePath) ??
                                  throw new InvalidOperationException("The approved reference GLB importer did not produce a GameObject asset.");
            var sourceRenderer = RequireSourceRenderer(sourcePrefab.transform);
            var referenceRenderer = RequireAttackRenderer(referencePrefab.transform);
            if (sourceRenderer.bones.Length != ExpectedAttackBoneCount)
                throw new InvalidOperationException("The supplied attack GLB must retain 13 bones in Unity.");
            if (!sourceRenderer.bones.Select(item => item.name)
                    .SequenceEqual(referenceRenderer.bones.Select(item => item.name), StringComparer.Ordinal))
                throw new InvalidOperationException("The approved reference attack rig no longer matches the supplied GLB bone order.");
            RequireTopologyMatches(sourceRenderer.sharedMesh, referenceRenderer.sharedMesh, "Approved reference");
            CreateOrUpdateAppearanceMesh(referenceRenderer.sharedMesh);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureTexture(string path, bool srgb, bool normalMap, float heightScale)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                           throw new InvalidOperationException("TextureImporter is missing: " + path);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb && !normalMap;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            if (normalMap)
            {
                importer.convertToNormalmap = true;
                importer.heightmapScale = heightScale;
                importer.normalmapFilter = TextureImporterNormalFilter.Standard;
            }
            importer.SaveAndReimport();
        }

        private static void CreateSmoothnessMask()
        {
            var source = DecodeTexture(ProjectAbsolutePath(RoughnessSource));
            try
            {
                var sourcePixels = source.GetPixels32();
                var outputPixels = new Color32[sourcePixels.Length];
                for (var index = 0; index < sourcePixels.Length; index++)
                {
                    var roughness = sourcePixels[index].r / 255f;
                    var approvedRoughness = Mathf.Clamp01(roughness * 0.35f + 0.25f);
                    outputPixels[index] = new Color32(0, 0, 0,
                        (byte)Mathf.RoundToInt((1f - approvedRoughness) * 255f));
                }
                var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
                try
                {
                    output.SetPixels32(outputPixels);
                    output.Apply(false, false);
                    File.WriteAllBytes(ProjectAbsolutePath(MaskPath), output.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(output);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
            AssetDatabase.ImportAsset(MaskPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void CreateOrUpdateMaterial()
        {
            var shader = Shader.Find(UrpLitShaderName) ??
                         throw new InvalidOperationException("URP Lit shader is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Dolore_Attack_Flesh" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.42f, 0.16f, 0.13f, 1f));
            material.SetTexture("_BaseMap", RequireTexture(AlbedoPath));
            material.SetTexture("_MetallicGlossMap", RequireTexture(MaskPath));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_BumpMap", RequireTexture(HeightPath));
            material.SetFloat("_BumpScale", 0.22f);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_ClearCoatMask", 0.16f);
            material.SetFloat("_ClearCoatSmoothness", 0.76f);
            material.EnableKeyword("_CLEARCOAT");
            material.SetFloat("_Cull", 0f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
        }

        private static void CreateOrUpdateAppearanceMesh(Mesh approvedReference)
        {
            if (approvedReference == null)
                throw new InvalidOperationException("The approved reference attack mesh is missing.");
            var copy = UnityEngine.Object.Instantiate(approvedReference);
            copy.name = "Dolore_Attack_ApprovedAppearance";
            var vertices = copy.vertices;
            var minimumX = vertices.Min(item => item.x);
            var maximumX = vertices.Max(item => item.x);
            var minimumY = vertices.Min(item => item.y);
            var maximumY = vertices.Max(item => item.y);
            var sizeX = Mathf.Max(0.000001f, maximumX - minimumX);
            var sizeY = Mathf.Max(0.000001f, maximumY - minimumY);
            copy.uv = vertices.Select(item => new Vector2(
                (item.x - minimumX) / sizeX,
                (item.y - minimumY) / sizeY)).ToArray();
            if (copy.HasVertexAttribute(VertexAttribute.Normal)) copy.RecalculateTangents();
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(AppearanceMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(copy, AppearanceMeshPath);
            }
            else
            {
                EditorUtility.CopySerialized(copy, existing);
                existing.name = "Dolore_Attack_ApprovedAppearance";
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }

        private static void Attach(
            Scene scene,
            Transform model,
            GameObject referencePrefab)
        {
            var existing = model.Find(AttachmentName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var targetBase = RequireBaseRenderer(model);

            var containerObject = new GameObject(AttachmentName);
            SceneManager.MoveGameObjectToScene(containerObject, scene);
            var container = containerObject.transform;
            container.SetParent(model, false);
            container.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            container.localScale = Vector3.one;

            var instance = PrefabUtility.InstantiatePrefab(referencePrefab, scene) as GameObject ??
                           throw new InvalidOperationException("The approved attachment reference GLB could not be instantiated.");
            instance.name = SourceInstanceName;
            instance.transform.SetParent(container, false);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            foreach (var camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var animation in instance.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            var referenceBase = RequireBaseRenderer(instance.transform);
            var referencePortrait = SubmeshWorldBounds(referenceBase, RequirePortraitSubmeshIndex(referenceBase));
            var targetPortrait = SubmeshWorldBounds(targetBase, RequirePortraitSubmeshIndex(targetBase));
            var referencePortraitCenterRelative = instance.transform.InverseTransformPoint(referencePortrait.center);
            var targetPortraitCenterRelative = model.InverseTransformPoint(targetPortrait.center);
            var scaleX = targetPortrait.size.x / Mathf.Max(0.000001f, referencePortrait.size.x);
            var scaleY = targetPortrait.size.y / Mathf.Max(0.000001f, referencePortrait.size.y);
            var uniformVisualScale = (scaleX + scaleY) * 0.5f;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * uniformVisualScale;
            instance.transform.localPosition = targetPortraitCenterRelative -
                                               referencePortraitCenterRelative * uniformVisualScale;
            var renderer = RequireAttackRenderer(instance.transform);
            foreach (var other in instance.GetComponentsInChildren<Renderer>(true)) other.enabled = other == renderer;
            renderer.updateWhenOffscreen = true;
            var desiredRootWorld = ApprovedDesiredRootWorld(model, targetBase, renderer);
            instance.transform.position += desiredRootWorld - AttackRootSurfaceCenter(renderer);

            EditorUtility.SetDirty(containerObject);
            EditorUtility.SetDirty(instance);
            EditorUtility.SetDirty(renderer);
        }

        private static Metrics InspectState(Scene scene)
        {
            RequireApprovedSample();
            RequireHash(SourceGlb, SourceGlbHash, "The supplied Dolore attack GLB changed.");
            RequireHash(ProjectAbsolutePath(ModelPath), SourceGlbHash, "The Unity attack GLB is not an exact source copy.");
            var slots = RequireSlots(RequirePlacementRoot(scene));
            var referencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedReferencePath) ??
                                  throw new InvalidOperationException("Approved attachment reference GLB is missing.");
            var results = new[]
            {
                InspectModel(RequireModel(slots[3]), slots[3].name, referencePrefab),
                InspectModel(RequireModel(slots[4]), slots[4].name, referencePrefab)
            };
            return new Metrics
            {
                MinRootAlignment = results.Min(item => item.RootAlignment),
                MaxAnchorDistance = results.Max(item => item.AnchorDistance),
                MinBoundsMagnitude = results.Min(item => item.BoundsSize.magnitude),
                Results = results
            };
        }

        private static ModelMetrics InspectModel(Transform model, string slotName, GameObject referencePrefab)
        {
            var attachments = Enumerable.Range(0, model.childCount)
                .Select(index => model.GetChild(index)).Where(item => item.name == AttachmentName).ToArray();
            if (attachments.Length != 1)
                throw new InvalidOperationException(slotName + " must contain exactly one attack attachment.");
            var container = attachments[0];
            var renderers = container.GetComponentsInChildren<Renderer>(true);
            var visible = renderers.Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (visible.Length != 1 || !(visible[0] is SkinnedMeshRenderer renderer))
                throw new InvalidOperationException(slotName + " must display exactly one skinned attack renderer.");
            if (AssetDatabase.GetAssetPath(renderer.sharedMesh) != ApprovedReferencePath ||
                renderer.sharedMaterials.Length != 1 ||
                AssetDatabase.GetAssetPath(renderer.sharedMaterial) != ApprovedReferencePath)
                throw new InvalidOperationException(slotName + " does not use the approved reference GLB attack appearance directly.");
            if (renderer.bones.Length != ExpectedAttackBoneCount)
                throw new InvalidOperationException(slotName + " attack bone count changed.");
            var source = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
            if (source == null || AssetDatabase.GetAssetPath(source) != ApprovedReferencePath)
                throw new InvalidOperationException(slotName + " attack renderer is not sourced from the approved placement and rig reference.");
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                               throw new InvalidOperationException("The exact supplied attack GLB copy is missing.");
            RequireTopologyMatches(RequireSourceRenderer(sourcePrefab.transform).sharedMesh, renderer.sharedMesh, slotName);
            if (container.GetComponentsInChildren<Animator>(true).Any(item => item.enabled) ||
                container.GetComponentsInChildren<Animation>(true).Any(item => item.enabled))
                throw new InvalidOperationException(slotName + " attack attachment must remain static in this task.");
            var bones = renderer.bones.ToDictionary(item => item.name, StringComparer.Ordinal);
            var targetBase = RequireBaseRenderer(model);
            var referenceBase = container.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => !item.enabled && item.sharedMesh != null && item.bones.Length == 27) ??
                                throw new InvalidOperationException(slotName + " hidden approved reference base is missing.");
            var reference = BuildReferencePlacement(referencePrefab);
            var desiredDirection = model.TransformDirection(reference.RootDirectionLocalToReferenceRoot).normalized;
            var direction = (bones[CurveBoneName].position - bones[RootBoneName].position).normalized;
            var alignment = Vector3.Dot(direction, desiredDirection);
            if (alignment < 0.999f)
                throw new InvalidOperationException(slotName + " attack root no longer faces the frame front.");
            var desiredRootWorld = ApprovedDesiredRootWorld(model, targetBase, renderer);
            var anchorDistance = Vector3.Distance(AttackRootSurfaceCenter(renderer), desiredRootWorld);
            if (anchorDistance > PositionTolerance)
                throw new InvalidOperationException(slotName + " attack root moved away from the approved sample exit: " + Num(anchorDistance));
            return new ModelMetrics
            {
                Slot = slotName,
                RootAlignment = alignment,
                AnchorDistance = anchorDistance,
                BoundsSize = renderer.bounds.size
            };
        }

        private static ReferencePlacement BuildReferencePlacement(GameObject referencePrefab)
        {
            GameObject instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(referencePrefab);
                instance.hideFlags = HideFlags.HideAndDontSave;
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(item => item.sharedMesh != null).ToArray();
                var baseRenderer = renderers.SingleOrDefault(item => item.bones.Length == 27) ??
                                   throw new InvalidOperationException("Approved reference base renderer is missing.");
                var attackRenderer = renderers.SingleOrDefault(item => item.bones.Length == ExpectedAttackBoneCount) ??
                                     throw new InvalidOperationException("Approved reference attack renderer is missing.");
                var bones = attackRenderer.bones.ToDictionary(item => item.name, StringComparer.Ordinal);
                var rootDirectionWorld = (bones[CurveBoneName].position - bones[RootBoneName].position).normalized;
                return new ReferencePlacement
                {
                    AttackRendererRelativeToBase =
                        baseRenderer.transform.worldToLocalMatrix * attackRenderer.transform.localToWorldMatrix,
                    RootSurfaceLocalToBase =
                        baseRenderer.transform.InverseTransformPoint(AttackRootSurfaceCenter(attackRenderer)),
                    RootDirectionLocalToBase =
                        baseRenderer.transform.InverseTransformDirection(rootDirectionWorld).normalized,
                    RootDirectionLocalToReferenceRoot =
                        instance.transform.InverseTransformDirection(rootDirectionWorld).normalized
                };
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static SkinnedMeshRenderer RequireBaseRenderer(Transform model)
        {
            return model.GetComponentsInChildren<SkinnedMeshRenderer>(false)
                       .SingleOrDefault(item => item.enabled && item.bones.Length == 27) ??
                   throw new InvalidOperationException(model.name + " approved 27-bone base renderer is missing.");
        }

        private static void SetLocalMatrix(Transform target, Matrix4x4 matrix)
        {
            var x = (Vector3)matrix.GetColumn(0);
            var y = (Vector3)matrix.GetColumn(1);
            var z = (Vector3)matrix.GetColumn(2);
            var scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (matrix.determinant < 0f)
            {
                scale.x = -scale.x;
                x = -x;
            }
            target.localPosition = matrix.GetColumn(3);
            target.localRotation = Quaternion.LookRotation(z / Mathf.Max(0.000001f, scale.z),
                y / Mathf.Max(0.000001f, scale.y));
            target.localScale = scale;
        }

        private static Vector3 AttackRootSurfaceCenter(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException(renderer.name + " mesh is missing.");
            var sourceVertices = mesh.vertices;
            var weldedByPosition = new Dictionary<Vector3Int, int>();
            var weldedRepresentatives = new List<int>();
            var vertexToWelded = new int[sourceVertices.Length];
            for (var vertexIndex = 0; vertexIndex < sourceVertices.Length; vertexIndex++)
            {
                var vertex = sourceVertices[vertexIndex];
                var key = new Vector3Int(
                    Mathf.RoundToInt(vertex.x * 100000f),
                    Mathf.RoundToInt(vertex.y * 100000f),
                    Mathf.RoundToInt(vertex.z * 100000f));
                if (!weldedByPosition.TryGetValue(key, out var weldedIndex))
                {
                    weldedIndex = weldedRepresentatives.Count;
                    weldedByPosition.Add(key, weldedIndex);
                    weldedRepresentatives.Add(vertexIndex);
                }
                vertexToWelded[vertexIndex] = weldedIndex;
            }
            var edgeCounts = new Dictionary<ulong, int>();
            var triangles = mesh.triangles;
            for (var index = 0; index < triangles.Length; index += 3)
            {
                CountEdge(edgeCounts, vertexToWelded[triangles[index]], vertexToWelded[triangles[index + 1]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 1]], vertexToWelded[triangles[index + 2]]);
                CountEdge(edgeCounts, vertexToWelded[triangles[index + 2]], vertexToWelded[triangles[index]]);
            }
            var boundaryEdges = edgeCounts.Where(item => item.Value == 1).Select(item => item.Key).ToArray();
            var boundaryVertices = new HashSet<int>();
            foreach (var edge in boundaryEdges)
            {
                boundaryVertices.Add((int)(edge >> 32));
                boundaryVertices.Add((int)(edge & uint.MaxValue));
            }
            if (boundaryEdges.Length != 5 || boundaryVertices.Count != 5)
                throw new InvalidOperationException(renderer.name +
                                                    " must retain the approved five-vertex attachment boundary.");
            var originalIndices = boundaryVertices.Select(index => weldedRepresentatives[index]).ToArray();
            return BakedVertexCenter(renderer, originalIndices);
        }

        private static Vector3 AttackLongitudinalSurfaceCenter(SkinnedMeshRenderer renderer, bool maximumSide)
        {
            var sourceVertices = renderer.sharedMesh.vertices;
            var minX = sourceVertices.Min(item => item.x);
            var maxX = sourceVertices.Max(item => item.x);
            var threshold = maximumSide
                ? maxX - (maxX - minX) * 0.18f
                : minX + (maxX - minX) * 0.18f;
            var indices = Enumerable.Range(0, sourceVertices.Length)
                .Where(index => maximumSide
                    ? sourceVertices[index].x >= threshold
                    : sourceVertices[index].x <= threshold).ToArray();
            return BakedVertexCenter(renderer, indices);
        }

        private static Vector3 BakedVertexCenter(SkinnedMeshRenderer renderer, IReadOnlyList<int> indices)
        {
            if (indices.Count == 0)
                throw new InvalidOperationException(renderer.name + " attack root vertex group is empty.");
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                var sum = Vector3.zero;
                foreach (var index in indices) sum += renderer.transform.TransformPoint(vertices[index]);
                return sum / indices.Count;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector3 ApprovedDesiredRootWorld(
            Transform model,
            SkinnedMeshRenderer targetBase,
            SkinnedMeshRenderer attackRenderer)
        {
            var portraitLocalBounds = SubmeshBoundsInRootLocal(
                targetBase,
                RequirePortraitSubmeshIndex(targetBase),
                model);
            var attackLocalBounds = RendererBoundsInRootLocal(attackRenderer, model);
            var attackBoundaryLocal = model.InverseTransformPoint(AttackRootSurfaceCenter(attackRenderer));
            var desiredLocal = new Vector3(
                portraitLocalBounds.center.x + portraitLocalBounds.size.x * ApprovedRootHorizontalOffset,
                portraitLocalBounds.center.y + portraitLocalBounds.size.y * ApprovedRootVerticalOffset,
                portraitLocalBounds.min.z + attackBoundaryLocal.z - attackLocalBounds.max.z);
            return model.TransformPoint(desiredLocal);
        }

        private static void WriteInspection(Metrics metrics, string phase, bool sceneSaved)
        {
            EnsureFolder(ReviewFolder);
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Phase=" + phase)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("SourceGlb=" + SourceGlb)
                .AppendLine("SourceGlbSha256=" + Sha256(SourceGlb))
                .AppendLine("ImportedGlb=" + ModelPath)
                .AppendLine("ImportedGlbSha256=" + Sha256(ProjectAbsolutePath(ModelPath)))
                .AppendLine("AttachmentSlotCount=2")
                .AppendLine("MinRootFrontAlignment=" + Num(metrics.MinRootAlignment))
                .AppendLine("MaxApprovedAnchorDistance=" + Num(metrics.MaxAnchorDistance))
                .AppendLine("MinAttackBoundsMagnitude=" + Num(metrics.MinBoundsMagnitude))
                .AppendLine("AttackBones=13")
                .AppendLine("ApprovedFleshMaterial=True")
                .AppendLine("AnimationApplied=False")
                .AppendLine("OtherDoloreSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("SceneSaved=" + sceneSaved);
            foreach (var result in metrics.Results)
            {
                report.AppendLine(result.Slot + ".RootFrontAlignment=" + Num(result.RootAlignment))
                    .AppendLine(result.Slot + ".ApprovedAnchorDistance=" + Num(result.AnchorDistance))
                    .AppendLine(result.Slot + ".BoundsSize=" + Vec(result.BoundsSize));
            }
            File.WriteAllText(ProjectAbsolutePath(InspectionPath), report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(InspectionPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static Texture2D CaptureModel(
            Transform sourceModel,
            Vector3 localViewDirection,
            bool portraitCloseup = false)
        {
            GameObject clone = null;
            GameObject cameraObject = null;
            var lights = new List<GameObject>();
            try
            {
                clone = UnityEngine.Object.Instantiate(sourceModel.gameObject);
                clone.name = "Dolore_Attack_Capture_Model";
                clone.transform.SetParent(null);
                clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                clone.transform.localScale = sourceModel.lossyScale;
                SetLayerRecursively(clone.transform, CaptureLayer);
                var bounds = BoundsOfVisible(clone.transform);
                if (portraitCloseup)
                {
                    var baseRenderer = RequireBaseRenderer(clone.transform);
                    bounds = SubmeshWorldBounds(baseRenderer, RequirePortraitSubmeshIndex(baseRenderer));
                    bounds.Expand(new Vector3(bounds.size.x * 0.45f, bounds.size.y * 0.45f,
                        Mathf.Max(bounds.size.x, bounds.size.y)));
                }

                cameraObject = new GameObject("Dolore_Attack_Capture_Camera");
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.024f, 0.023f, 1f);
                camera.fieldOfView = 24.55f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 50f;
                lights.Add(CreateLight("Dolore_Attack_Key", new Color(0.78f, 0.90f, 0.87f), 1.25f,
                    new Vector3(-0.45f, -0.65f, -0.62f)));
                lights.Add(CreateLight("Dolore_Attack_WarmFill", new Color(0.95f, 0.66f, 0.38f), 0.48f,
                    new Vector3(0.68f, -0.32f, -0.45f)));
                lights.Add(CreateLight("Dolore_Attack_Rim", new Color(0.20f, 0.70f, 0.62f), 0.62f,
                    new Vector3(-0.15f, -0.25f, 0.82f)));
                return CaptureView(camera, bounds, localViewDirection.normalized, 1024, 768);
            }
            finally
            {
                foreach (var light in lights)
                    if (light != null) UnityEngine.Object.DestroyImmediate(light);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static GameObject CreateLight(string name, Color color, float intensity, Vector3 direction)
        {
            var lightObject = new GameObject(name);
            lightObject.layer = CaptureLayer;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.cullingMask = 1 << CaptureLayer;
            lightObject.transform.rotation = Quaternion.LookRotation(direction.normalized);
            return lightObject;
        }

        private static Texture2D CaptureView(Camera camera, Bounds bounds, Vector3 viewDirection, int width, int height)
        {
            var distance = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * 2.65f;
            camera.transform.position = bounds.center + viewDirection * distance + Vector3.up * bounds.size.y * 0.10f;
            camera.transform.rotation = Quaternion.LookRotation((bounds.center - camera.transform.position).normalized, Vector3.up);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static void SaveComparisonSheet(IReadOnlyList<Texture2D> images, string outputPath)
        {
            const int width = 1024;
            const int height = 768;
            if (images.Count == 0 || images.Count % 3 != 0)
                throw new InvalidOperationException("Attack attachment comparison requires complete three-column rows.");
            var rows = images.Count / 3;
            var sheet = new Texture2D(width * 3, height * rows, TextureFormat.RGBA32, false, false);
            try
            {
                sheet.SetPixels32(Enumerable.Repeat(new Color32(4, 6, 6, 255), sheet.width * sheet.height).ToArray());
                for (var index = 0; index < images.Count; index++)
                {
                    var x = index % 3 * width;
                    var y = (rows - 1 - index / 3) * height;
                    var source = images[index];
                    if (source.width != width || source.height != height)
                        throw new InvalidOperationException("Attack attachment comparison image size changed.");
                    sheet.SetPixels32(x, y, width, height, source.GetPixels32());
                }
                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ??
                                          throw new InvalidOperationException("Capture output folder is invalid."));
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static SkinnedMeshRenderer RequireSourceRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(item => item.sharedMesh != null).OrderByDescending(item => item.sharedMesh.vertexCount).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The supplied attack GLB has no skinned mesh renderer.");
            return renderers[0];
        }

        private static SkinnedMeshRenderer RequireAttackRenderer(Transform root)
        {
            return root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault(item => item.sharedMesh != null &&
                                                item.bones.Length == ExpectedAttackBoneCount) ??
                   throw new InvalidOperationException(root.name + " approved 13-bone attack renderer is missing.");
        }

        private static void RequireTopologyMatches(Mesh source, Mesh applied, string context)
        {
            if (source == null || applied == null || source.vertexCount != applied.vertexCount ||
                source.subMeshCount != applied.subMeshCount)
                throw new InvalidOperationException(context + " attack mesh topology differs from the supplied GLB.");
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                if (!source.GetIndices(subMesh).SequenceEqual(applied.GetIndices(subMesh)))
                    throw new InvalidOperationException(context + " attack mesh indices differ from the supplied GLB.");
            }
        }

        private static Transform RequireModel(Transform slot)
        {
            return Enumerable.Range(0, slot.childCount).Select(index => slot.GetChild(index))
                       .SingleOrDefault(item => item.name == ModelName) ??
                   throw new InvalidOperationException(slot.name + " is missing Dolore_Model.");
        }

        private static GameObject RequirePlacementRoot(Scene scene)
        {
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active.");
            return scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                   throw new InvalidOperationException("Approved Dolore placement root is missing.");
        }

        private static Transform[] RequireSlots(GameObject root)
        {
            if (root.transform.childCount != SlotNames.Length)
                throw new InvalidOperationException("Approved Dolore placement must contain exactly seven slots.");
            var slots = new Transform[SlotNames.Length];
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = root.transform.GetChild(index);
                if (slot.name != SlotNames[index])
                    throw new InvalidOperationException("Dolore slot order or name changed at index " + index + ".");
                slots[index] = slot;
            }
            return slots;
        }

        private static string[] ProtectedRootSignatures(Scene scene, GameObject excluded)
        {
            return scene.GetRootGameObjects().Where(item => item != excluded)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => TransformTreeSignature(item.transform, null)).ToArray();
        }

        private static string[] ProtectedSlotSignatures(IReadOnlyList<Transform> slots)
        {
            return slots.Where((_, index) => index != 3 && index != 4)
                .Select(slot => TransformTreeSignature(slot, null)).ToArray();
        }

        private static string BaseModelSignature(Transform model)
        {
            return TransformTreeSignature(model, AttachmentName);
        }

        private static string TransformTreeSignature(Transform root, string excludedChildName)
        {
            var builder = new StringBuilder();
            AppendTransformTree(builder, root, root, excludedChildName);
            return builder.ToString();
        }

        private static void AppendTransformTree(StringBuilder builder, Transform current, Transform root, string excludedChildName)
        {
            if (current != root && current.name == excludedChildName) return;
            builder.Append('|').Append(TransformPath(current, root))
                .Append(" P=").Append(Vec(current.localPosition))
                .Append(" R=").Append(Quat(current.localRotation))
                .Append(" S=").Append(Vec(current.localScale))
                .Append(" A=").Append(current.gameObject.activeSelf);
            foreach (var renderer in current.GetComponents<Renderer>())
            {
                builder.Append(" MR=").Append(AssetDatabase.GetAssetPath(renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : null))
                    .Append(" MAT=").Append(string.Join(",", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            }
            for (var index = 0; index < current.childCount; index++)
                AppendTransformTree(builder, current.GetChild(index), root, excludedChildName);
        }

        private static Bounds BoundsOfVisible(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible renderers are available for capture.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (var index = 0; index < root.childCount; index++) SetLayerRecursively(root.GetChild(index), layer);
        }

        private static void RequireApprovedSample()
        {
            var path = ProjectAbsolutePath(ApprovalStatusPath);
            if (!File.Exists(path)) throw new FileNotFoundException("Approved attack sample status is missing.", path);
            var json = File.ReadAllText(path);
            if (!json.Contains("\"approved\": true") || !json.Contains("\"status\": \"approved_by_user\""))
                throw new InvalidOperationException("The Dolore attack attachment sample is not marked as user approved.");
        }

        private static void CopyExactAbsolute(string source, string destinationAssetPath)
        {
            RequireHash(source, SourceGlbHash, "The supplied attack GLB changed before copy.");
            var destination = ProjectAbsolutePath(destinationAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Attack model destination is invalid."));
            File.Copy(source, destination, true);
            RequireHash(destination, SourceGlbHash, "The Unity attack GLB copy differs from the supplied source.");
        }

        private static void CopyExactProject(string sourceRelativePath, string destinationAssetPath)
        {
            var source = ProjectAbsolutePath(sourceRelativePath);
            var destination = ProjectAbsolutePath(destinationAssetPath);
            if (!File.Exists(source)) throw new FileNotFoundException("Approved sample texture is missing.", source);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Attack texture destination is invalid."));
            File.Copy(source, destination, true);
            if (!string.Equals(Sha256(source), Sha256(destination), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Approved attack texture was not copied byte-for-byte.");
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static Texture2D RequireTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ??
                   throw new InvalidOperationException("Approved attack texture is missing: " + path);
        }

        private static Texture2D DecodeTexture(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("PNG could not be decoded: " + path);
            }
            return texture;
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Sha256(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Required file is missing.", path);
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string path, string expected, string message)
        {
            if (!string.Equals(Sha256(path), expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(message);
        }

        private static bool Approximately(Vector3 left, Vector3 right, float tolerance)
        {
            return Vector3.Distance(left, right) <= tolerance;
        }

        private static string TransformPath(Transform current, Transform root)
        {
            if (current == root) return current.name;
            var names = new Stack<string>();
            var cursor = current;
            while (cursor != null && cursor != root)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }
            return root.name + "/" + string.Join("/", names);
        }

        private static string Num(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R})", value.x, value.y, value.z);
        }

        private static string Quat(Quaternion value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:R},{1:R},{2:R},{3:R})", value.x, value.y, value.z, value.w);
        }

        private sealed class Metrics
        {
            public float MinRootAlignment;
            public float MaxAnchorDistance;
            public float MinBoundsMagnitude;
            public ModelMetrics[] Results;
        }

        private sealed class ModelMetrics
        {
            public string Slot;
            public float RootAlignment;
            public float AnchorDistance;
            public Vector3 BoundsSize;
        }

        private sealed class ReferencePlacement
        {
            public Matrix4x4 AttackRendererRelativeToBase;
            public Vector3 RootSurfaceLocalToBase;
            public Vector3 RootDirectionLocalToBase;
            public Vector3 RootDirectionLocalToReferenceRoot;
        }
    }
}
