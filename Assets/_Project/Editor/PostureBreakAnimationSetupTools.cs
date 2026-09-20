using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PostureBreakAnimationSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "PostureBreak";
        private const string LyingCarrierName = "Death_Tergo";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string TergoAttackName = "Tergo_07_Downed_Pounce";
        private const string SourceInspectionImage = "Logs/PostureBreakSources.png";
        private const string SourceInspectionReport = "Logs/PostureBreakSources.txt";
        private const string AssetFolder = "Assets/_Project/Animations/PlayerStatusEffects/PostureBreak";
        private const string CopiedClipPath = AssetFolder + "/PostureBreak_Lying.anim";
        private const string ControllerPath = AssetFolder + "/PostureBreak_Lying.controller";
        private const string CopiedStateName = "PostureBreak_LyingCopied";
        private const string TergoCopyName = "Tergo_07_Downed_Pounce_PostureBreakCopy";
        private const string ApplicationReport = "Logs/PostureBreakApplication.txt";
        private const string InspectionReport = "Logs/PostureBreakInspection.txt";
        private const string RuntimeInspectionReport = "Logs/PostureBreakRuntimeInspection.txt";
        private const string FinalImage = "docs/validation/PostureBreakLyingTergoPlacement/Final.png";
        private const string FinalReport = "docs/validation/PostureBreakLyingTergoPlacement/Final.txt";
        private const string ChestMountSourceImage = "Logs/PostureBreakTergoChestMountSources.png";
        private const string ChestMountSourceReport = "Logs/PostureBreakTergoChestMountSources.txt";
        private const string WaistMountSourceImage = "Logs/PostureBreakTergoWaistMountSources.png";
        private const string WaistMountSourceReport = "Logs/PostureBreakTergoWaistMountSources.txt";
        private const string WaistMountTimelineImage = "Logs/PostureBreakTergoWaistMountTimeline.png";
        private const string WaistMountTimelineReport = "Logs/PostureBreakTergoWaistMountTimeline.txt";
        private const string ChestMountApplicationReport = "Logs/PostureBreakTergoChestMountApplication.txt";
        private const string ChestMountInspectionImage = "Logs/PostureBreakTergoChestMountInspection.png";
        private const string ChestMountInspectionReport = "Logs/PostureBreakTergoChestMountInspection.txt";
        private const string ChestMountRuntimeReport = "Logs/PostureBreakTergoChestMountRuntime.txt";
        private const string ChestMountPlayModeImage = "Logs/PostureBreakTergoChestMountPlayMode.png";
        private const string ChestMountPlayModeReport = "Logs/PostureBreakTergoChestMountPlayMode.txt";
        private const string ChestMountFinalImage = "docs/validation/PostureBreakTergoChestMount/Final.png";
        private const string ChestMountFinalReport = "docs/validation/PostureBreakTergoChestMount/Final.txt";
        private const string LegCloseSourceImage = "Logs/PostureBreakLegClose/Sources.png";
        private const string LegCloseSourceReport = "Logs/PostureBreakLegClose/Sources.txt";
        private const string LegCloseApplicationReport = "Logs/PostureBreakLegClose/Application.txt";
        private const string LegCloseInspectionReport = "Logs/PostureBreakLegClose/Inspection.txt";
        private const string LegCloseRuntimeReport = "Logs/PostureBreakLegClose/Runtime.txt";
        private const string LegClosePlayModeImage = "Logs/PostureBreakLegClose/PlayMode.png";
        private const string LegClosePlayModeReport = "Logs/PostureBreakLegClose/PlayMode.txt";
        private const string LegCloseFinalImage = "docs/validation/PostureBreakLegClose/Final.png";
        private const string LegCloseFinalReport = "docs/validation/PostureBreakLegClose/Final.txt";
        private const string UpperBodyTwitchClipPath = AssetFolder + "/PostureBreak_UpperBodyTwitch.anim";
        private const string UpperBodyTwitchMaskPath = AssetFolder + "/PostureBreak_UpperBodyTwitch.mask";
        private const string UpperBodyTwitchLayerName = "PostureBreak_UpperBodyTwitch";
        private const string UpperBodyTwitchStateName = "PostureBreak_UpperBodyTwitchLoop";
        private const string UpperBodyTwitchSourceImage = "Logs/PostureBreakUpperBodyTwitch/Sources.png";
        private const string UpperBodyTwitchSourceReport = "Logs/PostureBreakUpperBodyTwitch/Sources.txt";
        private const string UpperBodyTwitchApplicationReport = "Logs/PostureBreakUpperBodyTwitch/Application.txt";
        private const string UpperBodyTwitchInspectionReport = "Logs/PostureBreakUpperBodyTwitch/Inspection.txt";
        private const string UpperBodyTwitchRuntimeReport = "Logs/PostureBreakUpperBodyTwitch/Runtime.txt";
        private const string UpperBodyTwitchPlayModeImage = "Logs/PostureBreakUpperBodyTwitch/PlayMode.png";
        private const string UpperBodyTwitchPlayModeReport = "Logs/PostureBreakUpperBodyTwitch/PlayMode.txt";
        private const string UpperBodyTwitchFinalImage = "docs/validation/PostureBreakUpperBodyTwitch/Final.png";
        private const string UpperBodyTwitchFinalReport = "docs/validation/PostureBreakUpperBodyTwitch/Final.txt";
        private const string TwitchAttackSyncSourceImage = "Logs/PostureBreakUpperBodyTwitch/AttackSyncSources.png";
        private const string TwitchAttackSyncSourceReport = "Logs/PostureBreakUpperBodyTwitch/AttackSyncSources.txt";
        private const string CarrierHipsPath = "Armature/Hips";
        private const string CarrierUpperChestPath = "Armature/Hips/Spine02/Spine01/Spine";
        private const string CarrierMiddleChestPath = "Armature/Hips/Spine02/Spine01";
        private const string CarrierLowerChestPath = "Armature/Hips/Spine02";
        private const string CarrierHeadPath = "Armature/Hips/Spine02/Spine01/Spine/neck/Head";
        private const string TergoHipsPath = "Armature/Hips";
        private const string TergoLeftHandName = "LeftHand";
        private const string TergoRightHandName = "RightHand";
        private const string LeftUpperLegPath = "Armature/Hips/LeftUpLeg";
        private const string LeftLowerLegPath = "Armature/Hips/LeftUpLeg/LeftLeg";
        private const string LeftFootPath = "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot";
        private const string LeftToePath = "Armature/Hips/LeftUpLeg/LeftLeg/LeftFoot/LeftToeBase";
        private const string RightUpperLegPath = "Armature/Hips/RightUpLeg";
        private const string RightLowerLegPath = "Armature/Hips/RightUpLeg/RightLeg";
        private const string RightFootPath = "Armature/Hips/RightUpLeg/RightLeg/RightFoot";
        private const string RightToePath = "Armature/Hips/RightUpLeg/RightLeg/RightFoot/RightToeBase";
        private const float PlacementDistance = 2f;
        private const float LegLateralRemainingRatio = 0.60f;
        private const float UpperBodyTwitchAngle = 20f;
        private const float KneeTwitchAngle = 7f;
        // The first chest-stab interval begins at normalized phase 35% of the
        // 7.216667-second Tergo attack clip: 2.525833 seconds in the current source.
        private const float UpperBodyTwitchAttackStartTime = 2.525833f;
        private const float UpperBodyTwitchPeriod = 1f;
        private const float UpperBodyTwitchPeakTime = 0.10f;
        private const float UpperBodyTwitchReturnTime = 0.20f;
        // These complete leg chains stay on the base clip's first frame. Only the
        // upper legs receive the synchronized additive knee twitch.
        private static readonly string[] StaticLegPaths =
        {
            LeftUpperLegPath,
            LeftLowerLegPath,
            LeftFootPath,
            LeftToePath,
            RightUpperLegPath,
            RightLowerLegPath,
            RightFootPath,
            RightToePath
        };
        private const float WaistMountAnchorRatio = 0f;
        private const float ChestMountRootHeightOffset = 0.20f;
        // Keep the seated hips near the carrier waist while pulling the whole attacker
        // toward the carrier feet so the drill tips, rather than the hand bones, land on the chest.
        private const float ChestAttackRetreatDistance = 0.28f;
        private const int PreviewLayer = 31;
        private const int PanelSize = 512;
        private const int Gap = 8;

        [MenuItem("Bellerophon/Player/Inspect Posture Break Sources")]
        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject lying = FindUnique(scene, LyingCarrierName);
            GameObject placement = FindUnique(scene, TergoPlacementRootName);
            Transform tergoTransform = FindDirectChild(placement.transform, TergoAttackName);
            GameObject tergo = tergoTransform.gameObject;

            AnimationClip lyingClip = RequireSingleControllerClip(lying, LyingCarrierName);
            AnimationClip tergoClip = RequireSingleControllerClip(tergo, TergoAttackName);
            string[] tergoCandidates = placement.transform.Cast<Transform>()
                .Select(child => DescribeAnimatedRoot(child.gameObject))
                .ToArray();

            GameObject lyingPreview = CloneForPreview(lying, "PostureBreak_LyingSourcePreview");
            GameObject tergoPreview = CloneForPreview(tergo, "PostureBreak_TergoSourcePreview");
            try
            {
                tergoPreview.SetActive(false);
                var panelList = new List<Texture2D>
                {
                    RenderSample(lyingPreview, lyingClip, 0f),
                    RenderSample(lyingPreview, lyingClip, lyingClip.length * 0.5f),
                    RenderSample(lyingPreview, lyingClip, Mathf.Max(0f, lyingClip.length - 0.001f))
                };
                lyingPreview.SetActive(false);
                tergoPreview.SetActive(true);
                panelList.Add(RenderSample(tergoPreview, tergoClip, 0f));
                panelList.Add(RenderSample(tergoPreview, tergoClip, tergoClip.length * 0.25f));
                panelList.Add(RenderSample(tergoPreview, tergoClip, tergoClip.length * 0.5f));
                panelList.Add(RenderSample(tergoPreview, tergoClip, tergoClip.length * 0.75f));
                panelList.Add(RenderSample(tergoPreview, tergoClip, Mathf.Max(0f, tergoClip.length - 0.001f)));
                Texture2D[] panels = panelList.ToArray();
                try
                {
                    Texture2D sheet = Combine(panels, 4, 2);
                    try
                    {
                        WritePng(SourceInspectionImage, sheet);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(sheet);
                    }
                }
                finally
                {
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(lyingPreview);
            }

            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            var report = new StringBuilder()
                .AppendLine("PostureBreak source direct inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("target=" + HierarchyPath(target.transform))
                .AppendLine("lyingCarrier=" + HierarchyPath(lying.transform))
                .AppendLine("lyingClip=" + AssetDatabase.GetAssetPath(lyingClip))
                .AppendLine("lyingClipLength=" + F(lyingClip.length))
                .AppendLine("tergoCandidate=" + HierarchyPath(tergo.transform))
                .AppendLine("tergoClip=" + AssetDatabase.GetAssetPath(tergoClip))
                .AppendLine("tergoClipLength=" + F(tergoClip.length))
                .AppendLine("panels=row1 lying 0%,50%,100%; row1 col4 and row2 col1-4 tergo 0%,25%,50%,75%,100%")
                .AppendLine("allTergoRoots:")
                .AppendLine(string.Join("\n", tergoCandidates.Select(item => "- " + item)))
                .AppendLine("directVisualReviewPrimary=True")
                .AppendLine("numericAndAssetInspectionSecondary=True")
                .AppendLine("unityConsoleErrors=0");
            WriteText(SourceInspectionReport, report.ToString());
            Debug.Log("[PostureBreak] Source contact sheet captured read-only.\n" + report);
        }

        internal static string SourceInspectionAbsolutePath => Absolute(SourceInspectionImage);

        [MenuItem("Bellerophon/Player/Inspect Posture Break Tergo Chest Mount Sources")]
        internal static void InspectChestMountSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_ChestMountTargetPreview");
            GameObject tergoPreview = CloneForPreview(tergoCopy, "PostureBreak_ChestMountTergoPreview");
            targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            targetPreview.transform.localScale = target.transform.lossyScale;
            tergoPreview.transform.position = tergoCopy.transform.position - target.transform.position;
            tergoPreview.transform.rotation = tergoCopy.transform.rotation;
            tergoPreview.transform.localScale = tergoCopy.transform.lossyScale;
            float targetTime = Mathf.Max(0f, targetClip.length - 0.001f);
            float[] attackTimes =
            {
                tergoClip.length * 0.25f,
                tergoClip.length * 0.5f,
                tergoClip.length * 0.75f
            };
            try
            {
                ChestMountSolution upperChest = SolveChestMount(
                    targetPreview, targetClip, tergoPreview, tergoClip,
                    true, 0.2f, CarrierUpperChestPath);
                upperChest.Apply(tergoPreview.transform);
                var panels = new List<Texture2D>();
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Front));
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Side));
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Top));

                ChestMountSolution middleChest = SolveChestMount(
                    targetPreview, targetClip, tergoPreview, tergoClip,
                    true, 0.2f, CarrierMiddleChestPath);
                middleChest.Apply(tergoPreview.transform);
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Front));
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Side));
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Top));

                ChestMountSolution lowerChest = SolveChestMount(
                    targetPreview, targetClip, tergoPreview, tergoClip,
                    true, 0.2f, CarrierLowerChestPath);
                lowerChest.Apply(tergoPreview.transform);
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Front));
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Side));
                panels.Add(RenderPair(targetPreview, targetClip, targetTime, tergoPreview, tergoClip,
                    attackTimes[1], PreviewView.Top));

                try
                {
                    Texture2D sheet = Combine(panels, 3, 3);
                    try { WritePng(ChestMountSourceImage, sheet); }
                    finally { UnityEngine.Object.DestroyImmediate(sheet); }
                }
                finally
                {
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }

                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                var report = new StringBuilder()
                    .AppendLine("PostureBreak Tergo chest-mount source inspection")
                    .AppendLine("verificationSceneTargetsManipulated=False")
                    .AppendLine("row1=upper chest Spine mount facing carrier head at root height +0.2m; front,side,top")
                    .AppendLine("row2=middle chest Spine01 mount facing carrier head at root height +0.2m; front,side,top")
                    .AppendLine("row3=lower chest Spine02 mount facing carrier head at root height +0.2m; front,side,top")
                    .AppendLine("carrierBodyAxis=hips-to-head projected on ground")
                    .AppendLine("upperChestCandidate=" + upperChest.Describe())
                    .AppendLine("middleChestCandidate=" + middleChest.Describe())
                    .AppendLine("lowerChestCandidate=" + lowerChest.Describe())
                    .AppendLine("targetAnimationModified=False")
                    .AppendLine("tergoAnimationModified=False")
                    .AppendLine("sceneSaved=False")
                    .AppendLine("unityConsoleErrors=0")
                    .AppendLine("directVisualReviewPrimary=True");
                WriteText(ChestMountSourceReport, report.ToString());
                Debug.Log("[PostureBreak] Tergo chest-mount candidates captured read-only.\n" + report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        internal static string ChestMountSourceAbsolutePath => Absolute(ChestMountSourceImage);

        [MenuItem("Bellerophon/Player/Inspect Posture Break Tergo Waist Mount Sources")]
        internal static void InspectWaistMountSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_WaistMountTargetPreview");
            GameObject tergoPreview = CloneForPreview(tergoCopy, "PostureBreak_WaistMountTergoPreview");
            targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            targetPreview.transform.localScale = target.transform.lossyScale;
            tergoPreview.transform.position = tergoCopy.transform.position - target.transform.position;
            tergoPreview.transform.rotation = tergoCopy.transform.rotation;
            tergoPreview.transform.localScale = tergoCopy.transform.lossyScale;
            float targetTime = Mathf.Max(0f, targetClip.length - 0.001f);
            var configurations = new[]
            {
                new WaistMountConfiguration("row1=Hips exact yellow waist center, root +0.32m", 0f, 0.32f),
                new WaistMountConfiguration("row2=Hips-Spine02 25% lower abdomen, root +0.32m", 0.25f, 0.32f),
                new WaistMountConfiguration("row3=Hips-Spine02 50% abdomen, root +0.32m", 0.5f, 0.32f)
            };
            var panels = new List<Texture2D>();
            var descriptions = new List<string>();
            try
            {
                foreach (WaistMountConfiguration configuration in configurations)
                {
                    ChestMountSolution solution = SolveWaistMount(
                        targetPreview,
                        targetClip,
                        tergoPreview,
                        tergoClip,
                        configuration.AnchorRatio,
                        configuration.RootHeightOffset);
                    solution.Apply(tergoPreview.transform);
                    HandAttackMetrics attacks = AnalyzeHandAttacks(
                        targetPreview,
                        targetClip,
                        tergoPreview,
                        tergoClip);
                    panels.Add(RenderPair(
                        targetPreview, targetClip, targetTime,
                        tergoPreview, tergoClip, attacks.LeftClosestTime,
                        PreviewView.Side));
                    panels.Add(RenderPair(
                        targetPreview, targetClip, targetTime,
                        tergoPreview, tergoClip, attacks.RightClosestTime,
                        PreviewView.Side));
                    panels.Add(RenderPair(
                        targetPreview, targetClip, targetTime,
                        tergoPreview, tergoClip,
                        (attacks.LeftClosestTime + attacks.RightClosestTime) * 0.5f,
                        PreviewView.Top));
                    descriptions.Add(configuration.Label + " | " + solution.Describe() +
                        " | " + attacks.Describe());
                }

                Texture2D sheet = Combine(panels, 3, 3);
                try { WritePng(WaistMountSourceImage, sheet); }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                string report = new StringBuilder()
                    .AppendLine("PostureBreak Tergo waist-mount source inspection")
                    .AppendLine("verificationSceneTargetsManipulated=False")
                    .AppendLine("columns=left-hand closest side,right-hand closest side,mean closest phase top")
                    .AppendLine("yellowMountReference=carrier Hips-to-Spine02 abdomen segment")
                    .AppendLine("redAttackReference=carrier upper chest Spine")
                    .AppendLine(string.Join("\n", descriptions))
                    .AppendLine("targetAnimationModified=False")
                    .AppendLine("tergoAnimationModified=False")
                    .AppendLine("sceneSaved=False")
                    .AppendLine("unityConsoleErrors=0")
                    .AppendLine("directVisualReviewPrimary=True")
                    .ToString();
                WriteText(WaistMountSourceReport, report);
                Debug.Log("[PostureBreak] Tergo waist-mount candidates captured read-only.\n" + report);
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        internal static string WaistMountSourceAbsolutePath => Absolute(WaistMountSourceImage);

        [MenuItem("Bellerophon/Player/Inspect Posture Break Tergo Waist Mount Timeline")]
        internal static void InspectWaistMountTimeline()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_WaistTimelineTargetPreview");
            GameObject tergoPreview = CloneForPreview(tergoCopy, "PostureBreak_WaistTimelineTergoPreview");
            targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            targetPreview.transform.localScale = target.transform.lossyScale;
            tergoPreview.transform.position = tergoCopy.transform.position - target.transform.position;
            tergoPreview.transform.rotation = tergoCopy.transform.rotation;
            tergoPreview.transform.localScale = tergoCopy.transform.lossyScale;
            float targetTime = Mathf.Max(0f, targetClip.length - 0.001f);
            float[] phases = { 0f, 0.125f, 0.25f, 0.35f, 0.416667f, 0.5f, 0.625f, 0.75f, 0.875f, 0.999f };
            var panels = new List<Texture2D>();
            try
            {
                ChestMountSolution solution = SolveWaistMount(
                    targetPreview, targetClip, tergoPreview, tergoClip, 1f, 0.32f);
                solution.Apply(tergoPreview.transform);
                HandAttackMetrics attacks = AnalyzeHandAttacks(
                    targetPreview, targetClip, tergoPreview, tergoClip);
                foreach (float phase in phases)
                {
                    panels.Add(RenderPairFocused(
                        targetPreview, targetClip, targetTime,
                        tergoPreview, tergoClip, tergoClip.length * phase,
                        PreviewView.Side));
                }
                Texture2D sheet = Combine(panels, 5, 2);
                try { WritePng(WaistMountTimelineImage, sheet); }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                string report = new StringBuilder()
                    .AppendLine("PostureBreak Tergo waist-mount focused timeline")
                    .AppendLine("verificationSceneTargetsManipulated=False")
                    .AppendLine("candidate=Spine02 lower-abdomen, root +0.32m")
                    .AppendLine("panels=0%,12.5%,25%,35%,41.6667%,50%,62.5%,75%,87.5%,99.9% side close-up")
                    .AppendLine("solution=" + solution.Describe())
                    .AppendLine("attacks=" + attacks.Describe())
                    .AppendLine("targetAnimationModified=False")
                    .AppendLine("tergoAnimationModified=False")
                    .AppendLine("sceneSaved=False")
                    .AppendLine("unityConsoleErrors=0")
                    .AppendLine("directVisualReviewPrimary=True")
                    .ToString();
                WriteText(WaistMountTimelineReport, report);
                Debug.Log("[PostureBreak] Tergo waist-mount focused timeline captured read-only.\n" + report);
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        internal static string WaistMountTimelineAbsolutePath => Absolute(WaistMountTimelineImage);

        [MenuItem("Bellerophon/Player/Apply Posture Break Tergo Chest Mount")]
        internal static void ApplyChestMount()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject placement = FindUnique(scene, TergoPlacementRootName);
            GameObject sourceTergo = FindDirectChild(placement.transform, TergoAttackName).gameObject;
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            TransformSnapshot targetBefore = TransformSnapshot.Capture(target.transform);
            TransformSnapshot sourceBefore = TransformSnapshot.Capture(sourceTergo.transform);
            Vector3 copyScaleBefore = tergoCopy.transform.localScale;
            string copyHierarchyBefore = HierarchyComponentSignature(tergoCopy);
            string copyMaterialsBefore = RendererMaterialSignature(tergoCopy);
            string copyAnimatorsBefore = AnimatorSignature(tergoCopy);
            ChestMountSolution solution = SolveWaistMountFromSceneObjects(
                target, targetClip, tergoCopy, tergoClip);
            Vector3 appliedPosition = target.transform.position + solution.Position;

            Undo.RecordObject(tergoCopy.transform, "Mount Tergo attack duplicate on PostureBreak chest");
            tergoCopy.transform.SetPositionAndRotation(appliedPosition, solution.Rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tergoCopy.transform);
            EditorUtility.SetDirty(tergoCopy.transform);

            targetBefore.RequireUnchanged(target.transform, TargetName);
            sourceBefore.RequireUnchanged(sourceTergo.transform, TergoAttackName);
            if (Vector3.Distance(copyScaleBefore, tergoCopy.transform.localScale) > 0.00001f ||
                !string.Equals(copyHierarchyBefore, HierarchyComponentSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(copyMaterialsBefore, RendererMaterialSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(copyAnimatorsBefore, AnimatorSignature(tergoCopy), StringComparison.Ordinal))
                throw new InvalidOperationException("Tergo chest-mount application changed scale, hierarchy, materials, or animation.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save CargoRunMvp after Tergo chest mounting.");
            AssetDatabase.SaveAssets();
            InspectChestMount();

            string report = new StringBuilder()
                .AppendLine("PostureBreak Tergo chest-mount application")
                .AppendLine("target=" + HierarchyPath(target.transform))
                .AppendLine("tergoCopy=" + HierarchyPath(tergoCopy.transform))
                .AppendLine("mountReference=Hips-to-Spine02 abdomen ratio " + F(WaistMountAnchorRatio))
                .AppendLine("rootHeightOffsetMeters=" + F(ChestMountRootHeightOffset))
                .AppendLine("attackRetreatTowardFeetMeters=" + F(ChestAttackRetreatDistance))
                .AppendLine("appliedPosition=" + Vec(tergoCopy.transform.position))
                .AppendLine("appliedEuler=" + Vec(tergoCopy.transform.rotation.eulerAngles))
                .AppendLine("targetRootModified=False")
                .AppendLine("sourceTergoModified=False")
                .AppendLine("tergoAnimationModified=False")
                .AppendLine("tergoScaleModified=False")
                .AppendLine("sceneSaved=True")
                .ToString();
            WriteText(ChestMountApplicationReport, report);
            Debug.Log("[PostureBreak] Tergo duplicate mounted on the carrier chest.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Tergo Chest Mount")]
        internal static void InspectChestMount()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject placement = FindUnique(scene, TergoPlacementRootName);
            GameObject sourceTergo = FindDirectChild(placement.transform, TergoAttackName).gameObject;
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            ChestMountSolution expected = SolveWaistMountFromSceneObjects(
                target, targetClip, tergoCopy, tergoClip);
            Vector3 expectedPosition = target.transform.position + expected.Position;
            float positionError = Vector3.Distance(expectedPosition, tergoCopy.transform.position);
            float rotationError = Quaternion.Angle(expected.Rotation, tergoCopy.transform.rotation);
            if (positionError > 0.0001f || rotationError > 0.001f)
                throw new InvalidOperationException(
                    "Tergo waist mount differs from the selected lower-abdomen solution. positionError=" +
                    F(positionError) + " rotationError=" + F(rotationError));
            if (!string.Equals(HierarchyComponentSignature(sourceTergo), HierarchyComponentSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(RendererMaterialSignature(sourceTergo), RendererMaterialSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(AnimatorSignature(sourceTergo), AnimatorSignature(tergoCopy), StringComparison.Ordinal))
                throw new InvalidOperationException("Tergo chest-mount duplicate differs from its source hierarchy, materials, or animation.");
            if (Vector3.Distance(sourceTergo.transform.localScale, tergoCopy.transform.localScale) > 0.00001f)
                throw new InvalidOperationException("Tergo chest-mount duplicate scale differs from its source.");

            ChestMountGeometryMetrics geometry = EvaluateWaistMountGeometry(
                target, targetClip, tergoCopy, tergoClip);
            HandAttackMetrics attacks = AnalyzeHandAttacksFromSceneObjects(
                target, targetClip, tergoCopy, tergoClip);
            if (geometry.AverageHorizontalHipError > 0.001f || geometry.FacingDot < 0.999f)
                throw new InvalidOperationException("Tergo chest-mount geometry differs. " + geometry.Describe());
            // These are wrist-bone distances, not the forward drill-tip contact points.
            // Keep them as a broad structural sanity check; actual strike placement is judged
            // from the unmodified Play Mode models in the side-view contact sheet below.
            if (attacks.LeftClosestDistance > 0.45f || attacks.RightClosestDistance > 0.45f)
                throw new InvalidOperationException("Tergo arm roots no longer reach the carrier torso. " + attacks.Describe());
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            string report = new StringBuilder()
                .AppendLine("PostureBreak Tergo chest-mount structural inspection")
                .AppendLine("verificationSceneTargetsManipulated=False")
                .AppendLine("mountReference=Hips-to-Spine02 abdomen ratio " + F(WaistMountAnchorRatio))
                .AppendLine("rootHeightOffsetMeters=" + F(ChestMountRootHeightOffset))
                .AppendLine("attackRetreatTowardFeetMeters=" + F(ChestAttackRetreatDistance))
                .AppendLine("positionErrorMeters=" + F(positionError))
                .AppendLine("rotationErrorDegrees=" + F(rotationError))
                .AppendLine("geometry=" + geometry.Describe())
                .AppendLine("upperChestHandAttacks=" + attacks.Describe())
                .AppendLine("sourceTergoModified=False")
                .AppendLine("targetAnimationModified=False")
                .AppendLine("tergoAnimationModified=False")
                .AppendLine("hierarchyComponentsMatch=True")
                .AppendLine("materialsMatch=True")
                .AppendLine("animatorControllerMatch=True")
                .AppendLine("scaleMatch=True")
                .AppendLine("unityConsoleErrors=0")
                .AppendLine("directVisualReviewPending=True")
                .ToString();
            WriteText(ChestMountInspectionReport, report);
            Debug.Log("[PostureBreak] Tergo chest-mount structural inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Tergo Chest Mount Visual")]
        internal static void InspectChestMountVisual()
        {
            RequireEditMode();
            InspectChestMount();
            CaptureChestMountContactSheet(ChestMountInspectionImage);
            Debug.Log("[PostureBreak] Tergo chest-mount diagnostic contact sheet captured.");
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Tergo Chest Mount Runtime")]
        internal static void InspectChestMountRuntime()
        {
            RequireEditMode();
            InspectChestMount();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            AnimatorPlaybackMetrics targetMetrics = EvaluateAnimatorPlayback(target, CopiedStateName, targetClip, false);
            AnimatorPlaybackMetrics tergoMetrics = EvaluateAnimatorPlayback(tergoCopy, tergoClip.name, tergoClip, true);
            if (!targetMetrics.MotionObserved || !targetMetrics.RepeatObserved ||
                !tergoMetrics.MotionObserved || !tergoMetrics.RepeatObserved)
                throw new InvalidOperationException(
                    "Tergo chest-mount runtime evaluation failed. target=" + targetMetrics.Describe() +
                    " tergo=" + tergoMetrics.Describe());
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            string report = new StringBuilder()
                .AppendLine("PostureBreak Tergo chest-mount runtime inspection")
                .AppendLine("verificationSceneTargetsManipulated=False")
                .AppendLine("target=" + targetMetrics.Describe())
                .AppendLine("tergo=" + tergoMetrics.Describe())
                .AppendLine("targetEntireClipRepeats=True")
                .AppendLine("tergoOriginalAnimationRepeats=True")
                .AppendLine("unityConsoleErrors=0")
                .ToString();
            WriteText(ChestMountRuntimeReport, report);
            Debug.Log("[PostureBreak] Tergo chest-mount runtime inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Capture Posture Break Tergo Chest Mount Final")]
        internal static void CaptureChestMountFinal()
        {
            RequireEditMode();
            InspectChestMount();
            Scene scene = RequireScene();
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            string playModeImagePath = Absolute(ChestMountPlayModeImage);
            string playModeReportPath = Absolute(ChestMountPlayModeReport);
            if (!File.Exists(playModeImagePath) || !File.Exists(playModeReportPath))
                throw new InvalidOperationException(
                    "Fresh direct Play Mode evidence is required before the final PostureBreak capture.");
            string playModeReport = File.ReadAllText(playModeReportPath, Encoding.UTF8);
            string expectedRoot = "tergoRootPosition=" + Vec(tergoCopy.transform.position);
            if (!playModeReport.Contains(
                    "captureSource=actual Play Mode scene objects rendered in place without cloning or target manipulation") ||
                !playModeReport.Contains(expectedRoot))
                throw new InvalidOperationException(
                    "PostureBreak direct Play Mode evidence is stale or does not match the current Tergo root.");
            string finalImagePath = Absolute(ChestMountFinalImage);
            Directory.CreateDirectory(Path.GetDirectoryName(finalImagePath));
            File.WriteAllBytes(finalImagePath, File.ReadAllBytes(playModeImagePath));
            string report = new StringBuilder()
                .AppendLine("PostureBreak Tergo waist-mount final actual Play Mode contact sheet")
                .AppendLine("row1=actual side view 0%,12.5%,25%,35%")
                .AppendLine("row2=actual side view 41.6667%,50%,75%,87.5%")
                .AppendLine("row3=actual top view 35%,41.6667%,50%")
                .AppendLine("mountReference=Hips-to-Spine02 abdomen ratio " + F(WaistMountAnchorRatio))
                .AppendLine("rootHeightOffsetMeters=" + F(ChestMountRootHeightOffset))
                .AppendLine("attackRetreatTowardFeetMeters=" + F(ChestAttackRetreatDistance))
                .AppendLine(expectedRoot)
                .AppendLine("captureSource=actual Play Mode scene objects rendered in place without cloning or target manipulation")
                .AppendLine("sourceAnimationCurvesModified=False")
                .AppendLine("sourceTergoModified=False")
                .AppendLine("directVisualReviewPrimary=True")
                .AppendLine("numericAndScriptInspectionSecondary=True")
                .ToString();
            WriteText(ChestMountFinalReport, report);
            Debug.Log("[PostureBreak] Tergo chest-mount final contact sheet captured once.");
        }

        internal static string ChestMountInspectionAbsolutePath => Absolute(ChestMountInspectionImage);
        internal static string ChestMountPlayModeAbsolutePath => Absolute(ChestMountPlayModeImage);
        internal static string ChestMountFinalAbsolutePath => Absolute(ChestMountFinalImage);

        private static Texture2D CaptureActualLivePair(
            GameObject target,
            GameObject tergo,
            PreviewView view,
            bool frameWholePair = false)
        {
            Bounds targetBounds = RendererBounds(target, Array.Empty<GameObject>());
            Bounds framingBounds = targetBounds;
            if (frameWholePair)
            {
                foreach (Renderer renderer in tergo.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled)
                        framingBounds.Encapsulate(renderer.bounds);
            }
            Transform carrierHips = FindPath(target.transform, CarrierHipsPath);
            Transform carrierHead = FindPath(target.transform, CarrierHeadPath);
            Vector3 bodyAxis = Vector3.ProjectOnPlane(
                carrierHead.position - carrierHips.position,
                Vector3.up).normalized;
            if (bodyAxis.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    "PostureBreak live capture could not resolve the lying carrier body axis.");

            Vector3 focus = framingBounds.center;
            Vector3 viewDirection;
            Vector3 cameraUp;
            Vector3 screenRight;
            if (view == PreviewView.Top)
            {
                viewDirection = Vector3.down;
                cameraUp = bodyAxis;
                screenRight = Vector3.Cross(viewDirection, cameraUp).normalized;
            }
            else
            {
                screenRight = -bodyAxis;
                viewDirection = Vector3.Cross(screenRight, Vector3.up).normalized;
                cameraUp = Vector3.up;
            }

            float horizontalExtent = 0f;
            float verticalExtent = 0f;
            Vector3 min = framingBounds.min;
            Vector3 max = framingBounds.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
                Vector3 offset = corner - focus;
                horizontalExtent = Mathf.Max(horizontalExtent, Mathf.Abs(Vector3.Dot(offset, screenRight)));
                verticalExtent = Mathf.Max(verticalExtent, Mathf.Abs(Vector3.Dot(offset, cameraUp)));
            }

            var cameraObject = new GameObject("PostureBreak_ActualPlayModeCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var includedRenderers = new HashSet<Renderer>(
                target.GetComponentsInChildren<Renderer>(true)
                    .Concat(tergo.GetComponentsInChildren<Renderer>(true)));
            Renderer[] hiddenRenderers = target.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled && !includedRenderers.Contains(renderer))
                .ToArray();
            RenderTexture texture = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (Renderer renderer in hiddenRenderers)
                    renderer.enabled = false;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
                camera.cullingMask = ~0;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(verticalExtent, horizontalExtent) * 1.12f;
                camera.transform.SetPositionAndRotation(
                    focus - viewDirection * 6f,
                    Quaternion.LookRotation(viewDirection, cameraUp));
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                foreach (Renderer renderer in hiddenRenderers)
                    if (renderer != null) renderer.enabled = true;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D CaptureActualLiveActor(
            GameObject target,
            PreviewView view)
        {
            Bounds targetBounds = RendererBounds(target, Array.Empty<GameObject>());
            Transform carrierHips = FindPath(target.transform, CarrierHipsPath);
            Transform carrierHead = FindPath(target.transform, CarrierHeadPath);
            Vector3 bodyAxis = Vector3.ProjectOnPlane(
                carrierHead.position - carrierHips.position,
                Vector3.up).normalized;
            if (bodyAxis.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    "PostureBreak live leg capture could not resolve the lying carrier body axis.");

            Vector3 focus = targetBounds.center;
            Vector3 viewDirection;
            Vector3 cameraUp;
            Vector3 screenRight;
            if (view == PreviewView.Top)
            {
                viewDirection = Vector3.down;
                cameraUp = bodyAxis;
                screenRight = Vector3.Cross(viewDirection, cameraUp).normalized;
            }
            else
            {
                screenRight = -bodyAxis;
                viewDirection = Vector3.Cross(screenRight, Vector3.up).normalized;
                cameraUp = Vector3.up;
            }

            float horizontalExtent = 0f;
            float verticalExtent = 0f;
            Vector3 min = targetBounds.min;
            Vector3 max = targetBounds.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
                Vector3 offset = corner - focus;
                horizontalExtent = Mathf.Max(horizontalExtent, Mathf.Abs(Vector3.Dot(offset, screenRight)));
                verticalExtent = Mathf.Max(verticalExtent, Mathf.Abs(Vector3.Dot(offset, cameraUp)));
            }

            var cameraObject = new GameObject("PostureBreak_LegCloseActualPlayModeCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var includedRenderers = new HashSet<Renderer>(
                target.GetComponentsInChildren<Renderer>(true));
            Renderer[] hiddenRenderers = target.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled && !includedRenderers.Contains(renderer))
                .ToArray();
            RenderTexture texture = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (Renderer renderer in hiddenRenderers)
                    renderer.enabled = false;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
                camera.cullingMask = ~0;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(verticalExtent, horizontalExtent) * 1.12f;
                camera.transform.SetPositionAndRotation(
                    focus - viewDirection * 6f,
                    Quaternion.LookRotation(viewDirection, cameraUp));
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                foreach (Renderer renderer in hiddenRenderers)
                    if (renderer != null) renderer.enabled = true;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D CaptureActualLiveChestPair(
            GameObject target,
            GameObject tergo,
            PreviewView view)
        {
            Transform carrierHips = FindPath(target.transform, CarrierHipsPath);
            Transform carrierHead = FindPath(target.transform, CarrierHeadPath);
            Transform carrierLowerChest = FindPath(target.transform, CarrierLowerChestPath);
            Transform carrierUpperChest = FindPath(target.transform, CarrierUpperChestPath);
            Vector3 bodyAxis = Vector3.ProjectOnPlane(
                carrierHead.position - carrierHips.position,
                Vector3.up).normalized;
            if (bodyAxis.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(
                    "PostureBreak live chest capture could not resolve the lying carrier body axis.");

            Vector3 focus = Vector3.Lerp(carrierLowerChest.position, carrierUpperChest.position, 0.65f);
            Vector3 viewDirection;
            Vector3 cameraUp;
            if (view == PreviewView.Top)
            {
                viewDirection = Vector3.down;
                cameraUp = bodyAxis;
            }
            else
            {
                Vector3 screenRight = -bodyAxis;
                viewDirection = Vector3.Cross(screenRight, Vector3.up).normalized;
                cameraUp = Vector3.up;
            }

            var cameraObject = new GameObject("PostureBreak_ActualChestPlayModeCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            var keyLightObject = new GameObject("PostureBreak_ActualChestKeyLight");
            keyLightObject.hideFlags = HideFlags.HideAndDontSave;
            keyLightObject.transform.SetParent(cameraObject.transform, false);
            Light keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.8f;
            keyLight.color = new Color(0.90f, 0.95f, 1f);
            keyLightObject.transform.rotation = Quaternion.Euler(35f, 145f, 0f);
            var fillLightObject = new GameObject("PostureBreak_ActualChestFillLight");
            fillLightObject.hideFlags = HideFlags.HideAndDontSave;
            fillLightObject.transform.SetParent(cameraObject.transform, false);
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 1.1f;
            fillLight.color = new Color(0.65f, 0.78f, 1f);
            fillLightObject.transform.rotation = Quaternion.Euler(320f, 325f, 0f);
            var includedRenderers = new HashSet<Renderer>(
                target.GetComponentsInChildren<Renderer>(true)
                    .Concat(tergo.GetComponentsInChildren<Renderer>(true)));
            Renderer[] hiddenRenderers = target.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled && !includedRenderers.Contains(renderer))
                .ToArray();
            RenderTexture texture = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (Renderer renderer in hiddenRenderers)
                    renderer.enabled = false;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
                camera.cullingMask = ~0;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.orthographic = true;
                camera.orthographicSize = 0.54f;
                camera.transform.SetPositionAndRotation(
                    focus - viewDirection * 6f,
                    Quaternion.LookRotation(viewDirection, cameraUp));
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                foreach (Renderer renderer in hiddenRenderers)
                    if (renderer != null) renderer.enabled = true;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(texture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void WriteLegClosePlayModeEvidence(
            IReadOnlyList<Texture2D> overheadPanels,
            IReadOnlyList<Texture2D> sidePanels,
            IReadOnlyList<float> capturedPhases,
            IReadOnlyList<LegSpreadSample> spreads,
            Vector3 targetRootPosition,
            Quaternion targetRootRotation,
            int consoleErrorsBefore)
        {
            var panels = new List<Texture2D>(overheadPanels.Count + sidePanels.Count);
            panels.AddRange(overheadPanels);
            panels.AddRange(sidePanels);
            Texture2D sheet = Combine(panels, 4, 4);
            try { WritePng(LegClosePlayModeImage, sheet); }
            finally { UnityEngine.Object.DestroyImmediate(sheet); }

            LegSpreadMetrics liveMetrics = AggregateLegSpread(spreads);
            string report = new StringBuilder()
                .AppendLine("PostureBreak forty-percent full-leg closure direct Play Mode observation")
                .AppendLine("verificationSceneTargetManipulated=False")
                .AppendLine("sourceAnimationModified=False")
                .AppendLine("captureSource=actual Play Mode scene object rendered in place without cloning or pose manipulation")
                .AppendLine("overheadPhases=" + string.Join(",", capturedPhases.Select(F)))
                .AppendLine("sidePhases=" + string.Join(",", capturedPhases.Select(F)))
                .AppendLine("liveSpread=" + liveMetrics.Describe())
                .AppendLine("targetRootPosition=" + Vec(targetRootPosition))
                .AppendLine("targetRootEuler=" + Vec(targetRootRotation.eulerAngles))
                .AppendLine("consoleErrorsBefore=" + consoleErrorsBefore.ToString(CultureInfo.InvariantCulture))
                .AppendLine("directVisualReviewPrimary=True")
                .ToString();
            WriteText(LegClosePlayModeReport, report);
        }

        private static LegSpreadMetrics AggregateLegSpread(
            IReadOnlyList<LegSpreadSample> samples)
        {
            if (samples.Count == 0)
                throw new InvalidOperationException("PostureBreak live leg spread samples are missing.");
            return new LegSpreadMetrics(
                samples.Average(item => item.Knee),
                samples.Average(item => item.Ankle),
                samples.Average(item => item.Toe),
                samples.Min(item => item.Knee),
                samples.Min(item => item.Ankle),
                samples.Min(item => item.Toe),
                samples.Count);
        }

        private static void WriteUpperBodyTwitchPlayModeEvidence(
            IReadOnlyList<Texture2D> sidePanels,
            IReadOnlyList<Texture2D> topPanels,
            IReadOnlyList<float> capturedTimes,
            IReadOnlyList<float> capturedTergoTimes,
            IReadOnlyList<float> headElevations,
            IReadOnlyList<LegSpreadSample> legSpreads,
            Vector3 targetRootPosition,
            Quaternion targetRootRotation,
            int consoleErrorsBefore)
        {
            const int samplesPerCycle = 9;
            if (sidePanels.Count != samplesPerCycle * 2 || topPanels.Count != samplesPerCycle * 2 ||
                capturedTimes.Count != samplesPerCycle * 2 ||
                capturedTergoTimes.Count != samplesPerCycle * 2 ||
                headElevations.Count != samplesPerCycle * 2 ||
                legSpreads.Count != samplesPerCycle * 2)
                throw new InvalidOperationException("PostureBreak twitch Play Mode evidence is incomplete.");
            var panels = new List<Texture2D>(samplesPerCycle * 4);
            panels.AddRange(sidePanels.Take(samplesPerCycle));
            panels.AddRange(topPanels.Take(samplesPerCycle));
            panels.AddRange(sidePanels.Skip(samplesPerCycle).Take(samplesPerCycle));
            panels.AddRange(topPanels.Skip(samplesPerCycle).Take(samplesPerCycle));
            Texture2D sheet = Combine(panels, samplesPerCycle, 4);
            try { WritePng(UpperBodyTwitchPlayModeImage, sheet); }
            finally { UnityEngine.Object.DestroyImmediate(sheet); }
            float firstLift = headElevations[3] - headElevations[2];
            float secondLift = headElevations[samplesPerCycle + 3] - headElevations[samplesPerCycle + 2];
            float firstKneeSpread = legSpreads[3].Knee - legSpreads[2].Knee;
            float secondKneeSpread =
                legSpreads[samplesPerCycle + 3].Knee - legSpreads[samplesPerCycle + 2].Knee;
            float maximumCycleSyncError = capturedTimes
                .Zip(capturedTergoTimes, (targetTime, tergoTime) => Mathf.Abs(targetTime - tergoTime))
                .Max();
            string report = new StringBuilder()
                .AppendLine("PostureBreak twitch gated by the first Tergo chest stab direct Play Mode observation")
                .AppendLine("verificationSceneTargetManipulated=False")
                .AppendLine("captureSource=actual Play Mode scene objects rendered in place without cloning or pose manipulation")
                .AppendLine("rows=cycle1 side,cycle1 body-front,cycle2 side,cycle2 body-front")
                .AppendLine("columns=cycle start,0.10 seconds before attack,attack start,first peak,first return,second onset,second peak,second return,third peak")
                .AppendLine("cycle1TwitchTimesSeconds=" + string.Join(",", capturedTimes.Take(samplesPerCycle).Select(F)))
                .AppendLine("cycle2TwitchTimesSeconds=" + string.Join(",", capturedTimes.Skip(samplesPerCycle).Take(samplesPerCycle).Select(F)))
                .AppendLine("cycle1TergoTimesSeconds=" + string.Join(",", capturedTergoTimes.Take(samplesPerCycle).Select(F)))
                .AppendLine("cycle2TergoTimesSeconds=" + string.Join(",", capturedTergoTimes.Skip(samplesPerCycle).Take(samplesPerCycle).Select(F)))
                .AppendLine("maximumAnimatorCycleSyncErrorSeconds=" + F(maximumCycleSyncError))
                .AppendLine("attackStartSeconds=" + F(UpperBodyTwitchAttackStartTime))
                .AppendLine("pulsePeriodSeconds=" + F(UpperBodyTwitchPeriod))
                .AppendLine("cycle1HeadLiftMeters=" + F(firstLift))
                .AppendLine("cycle2HeadLiftMeters=" + F(secondLift))
                .AppendLine("cycle1KneeSpreadIncreaseMeters=" + F(firstKneeSpread))
                .AppendLine("cycle2KneeSpreadIncreaseMeters=" + F(secondKneeSpread))
                .AppendLine("targetRootPosition=" + Vec(targetRootPosition))
                .AppendLine("targetRootEuler=" + Vec(targetRootRotation.eulerAngles))
                .AppendLine("consoleErrorsBefore=" + consoleErrorsBefore.ToString(CultureInfo.InvariantCulture))
                .AppendLine("baseLegCurvesFrozenAtFirstPose=True")
                .AppendLine("baseNonLegCurvesModified=False")
                .AppendLine("synchronizedKneeTwitch=True")
                .AppendLine("preAttackTwitch=False")
                .AppendLine("twitchBeginsWithFirstChestStab=True")
                .AppendLine("tergoModified=False")
                .AppendLine("directVisualReviewPrimary=True")
                .ToString();
            WriteText(UpperBodyTwitchPlayModeReport, report);
        }

        private static void WriteChestMountPlayModeEvidence(
            IReadOnlyList<Texture2D> sidePanels,
            IReadOnlyList<Texture2D> topPanels,
            IReadOnlyList<float> capturedPhases,
            Vector3 tergoRootPosition,
            Quaternion tergoRootRotation,
            int consoleErrorsBefore)
        {
            var panels = new List<Texture2D>(sidePanels.Count + topPanels.Count);
            panels.AddRange(sidePanels);
            panels.AddRange(topPanels);
            Texture2D sheet = Combine(panels, 4, 3);
            try
            {
                WritePng(ChestMountPlayModeImage, sheet);
                if (sidePanels.Count >= 5)
                {
                    WritePng("Logs/PostureBreakTergoChestMountStrike35.png", sidePanels[3]);
                    WritePng("Logs/PostureBreakTergoChestMountStrike42.png", sidePanels[4]);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            string report = new StringBuilder()
                .AppendLine("PostureBreak Tergo waist mount direct Play Mode observation")
                .AppendLine("verificationSceneTargetsManipulated=False")
                .AppendLine("sourceAnimationModified=False")
                .AppendLine("captureSource=actual Play Mode scene objects rendered in place without cloning or target manipulation")
                .AppendLine("sidePhases=" + string.Join(",", capturedPhases.Select(F)))
                .AppendLine("topPhases=0.35,0.416667,0.5")
                .AppendLine("tergoRootPosition=" + Vec(tergoRootPosition))
                .AppendLine("tergoRootEuler=" + Vec(tergoRootRotation.eulerAngles))
                .AppendLine("consoleErrorsBefore=" + consoleErrorsBefore.ToString(CultureInfo.InvariantCulture))
                .AppendLine("directVisualReviewPrimary=True")
                .ToString();
            WriteText(ChestMountPlayModeReport, report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Leg Close Sources")]
        internal static void InspectLegCloseSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject lying = FindUnique(scene, LyingCarrierName);
            AnimationClip sourceClip = RequireSingleControllerClip(lying, LyingCarrierName);
            AnimationClip candidate = UnityEngine.Object.Instantiate(sourceClip);
            candidate.name = "PostureBreak_LegCloseCandidate";
            candidate.hideFlags = HideFlags.HideAndDontSave;
            GameObject sourcePreview = CloneForPreview(target, "PostureBreak_LegCloseSourcePreview");
            GameObject candidatePreview = CloneForPreview(target, "PostureBreak_LegCloseCandidatePreview");
            float[] phases = { 0f, 0.25f, 0.5f, 0.75f, 0.999f };
            var panels = new List<Texture2D>();
            try
            {
                ApplyLegClosureCurves(sourceClip, candidate, target);
                foreach (float phase in phases)
                    panels.Add(RenderSample(sourcePreview, sourceClip, sourceClip.length * phase, PreviewView.Top));
                foreach (float phase in phases)
                    panels.Add(RenderSample(candidatePreview, candidate, candidate.length * phase, PreviewView.Top));
                foreach (float phase in phases)
                    panels.Add(RenderSample(candidatePreview, candidate, candidate.length * phase, PreviewView.Side));
                Texture2D sheet = Combine(panels, 5, 3);
                try { WritePng(LegCloseSourceImage, sheet); }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }

                LegSpreadMetrics sourceMetrics = EvaluateLegSpread(target, sourceClip);
                LegSpreadMetrics candidateMetrics = EvaluateLegSpread(target, candidate);
                RequireLegClosureRatio(sourceMetrics, candidateMetrics);
                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                string report = new StringBuilder()
                    .AppendLine("PostureBreak forty-percent full-leg closure source comparison")
                    .AppendLine("verificationSceneTargetManipulated=False")
                    .AppendLine("row1=source overhead 0%,25%,50%,75%,99.9%")
                    .AppendLine("row2=candidate overhead 0%,25%,50%,75%,99.9%")
                    .AppendLine("row3=candidate side 0%,25%,50%,75%,99.9%")
                    .AppendLine("lateralRemainingRatio=" + F(LegLateralRemainingRatio))
                    .AppendLine("source=" + sourceMetrics.Describe())
                    .AppendLine("candidate=" + candidateMetrics.Describe())
                    .AppendLine("closure=" + candidateMetrics.DescribeClosureFrom(sourceMetrics))
                    .AppendLine("sourceAnimationModified=False")
                    .AppendLine("upperBodyAndRootCurvesModified=False")
                    .AppendLine("sceneSaved=False")
                    .AppendLine("unityConsoleErrors=0")
                    .AppendLine("directVisualReviewPrimary=True")
                    .ToString();
                WriteText(LegCloseSourceReport, report);
                Debug.Log("[PostureBreak] Forty-percent leg-close candidate captured read-only.\n" + report);
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(candidatePreview);
                UnityEngine.Object.DestroyImmediate(sourcePreview);
                UnityEngine.Object.DestroyImmediate(candidate);
            }
        }

        [MenuItem("Bellerophon/Player/Apply Posture Break Leg Close")]
        internal static void ApplyLegClose()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject lying = FindUnique(scene, LyingCarrierName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            Animator targetAnimator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(TargetName + " has no Animator.");
            AnimationClip sourceClip = RequireSingleControllerClip(lying, LyingCarrierName);
            string sourcePath = AssetDatabase.GetAssetPath(sourceClip);
            Hash128 sourceHashBefore = AssetDatabase.GetAssetDependencyHash(sourcePath);
            TransformSnapshot targetRootBefore = TransformSnapshot.Capture(target.transform);
            TransformSnapshot tergoRootBefore = TransformSnapshot.Capture(tergoCopy.transform);

            AnimationClip copiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath);
            if (copiedClip == null)
            {
                if (!AssetDatabase.CopyAsset(sourcePath, CopiedClipPath))
                    throw new InvalidOperationException("Failed to create PostureBreak target-only clip.");
                AssetDatabase.ImportAsset(CopiedClipPath, ImportAssetOptions.ForceSynchronousImport);
                copiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                    throw new InvalidOperationException("PostureBreak target-only clip is missing after copy.");
            }
            else
            {
                EditorUtility.CopySerialized(sourceClip, copiedClip);
            }
            copiedClip.name = "PostureBreak_Lying";
            ApplyLegClosureCurves(sourceClip, copiedClip, target);
            EditorUtility.SetDirty(copiedClip);

            AnimatorController controller = UpdateLoopControllerMotion(copiedClip);
            bool sceneChanged = targetAnimator.runtimeAnimatorController != controller ||
                targetAnimator.applyRootMotion ||
                targetAnimator.cullingMode != AnimatorCullingMode.AlwaysAnimate ||
                !targetAnimator.enabled;
            if (sceneChanged)
            {
                Undo.RecordObject(targetAnimator, "Connect leg-closed PostureBreak animation");
                targetAnimator.runtimeAnimatorController = controller;
                targetAnimator.applyRootMotion = false;
                targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                targetAnimator.enabled = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetAnimator);
                EditorUtility.SetDirty(targetAnimator);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException("Failed to save CargoRunMvp after PostureBreak leg closure.");
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(CopiedClipPath, ImportAssetOptions.ForceSynchronousImport);

            targetRootBefore.RequireUnchanged(target.transform, TargetName);
            tergoRootBefore.RequireUnchanged(tergoCopy.transform, TergoCopyName);
            if (sourceHashBefore != AssetDatabase.GetAssetDependencyHash(sourcePath))
                throw new InvalidOperationException("Source Death_Tergo animation changed during leg closure.");

            InspectLegClose();
            AnimationClip appliedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("Applied PostureBreak clip is missing.");
            LegSpreadMetrics sourceMetrics = EvaluateLegSpread(target, sourceClip);
            LegSpreadMetrics appliedMetrics = EvaluateLegSpread(target, appliedClip);
            string report = new StringBuilder()
                .AppendLine("PostureBreak forty-percent full-leg closure application")
                .AppendLine("target=" + HierarchyPath(target.transform))
                .AppendLine("source=" + sourcePath)
                .AppendLine("targetOnlyClip=" + CopiedClipPath)
                .AppendLine("lateralRemainingRatio=" + F(LegLateralRemainingRatio))
                .AppendLine("sourceMetrics=" + sourceMetrics.Describe())
                .AppendLine("appliedMetrics=" + appliedMetrics.Describe())
                .AppendLine("closure=" + appliedMetrics.DescribeClosureFrom(sourceMetrics))
                .AppendLine("modifiedBones=LeftUpLeg,LeftLeg,RightUpLeg,RightLeg")
                .AppendLine("feetFollowLowerLegsWithOriginalLocalRotation=True")
                .AppendLine("modifiedProperties=m_LocalRotation.x/y/z/w")
                .AppendLine("sourceAnimationModified=False")
                .AppendLine("upperBodyAndRootCurvesModified=False")
                .AppendLine("targetRootModified=False")
                .AppendLine("tergoRootModified=False")
                .AppendLine("sceneSaved=" + sceneChanged)
                .ToString();
            WriteText(LegCloseApplicationReport, report);
            Debug.Log("[PostureBreak] Forty-percent full-leg closure applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Leg Close")]
        internal static void InspectLegClose()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject lying = FindUnique(scene, LyingCarrierName);
            GameObject placement = FindUnique(scene, TergoPlacementRootName);
            GameObject sourceTergo = FindDirectChild(placement.transform, TergoAttackName).gameObject;
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip sourceClip = RequireSingleControllerClip(lying, LyingCarrierName);
            AnimationClip adjustedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("Adjusted PostureBreak clip is missing.");
            RequireOnlyLegRotationCurvesDiffer(sourceClip, adjustedClip);

            LegSpreadMetrics sourceMetrics = EvaluateLegSpread(target, sourceClip);
            LegSpreadMetrics adjustedMetrics = EvaluateLegSpread(target, adjustedClip);
            RequireLegClosureRatio(sourceMetrics, adjustedMetrics);
            Animator targetAnimator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(TargetName + " has no Animator.");
            if (AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController) != ControllerPath)
                throw new InvalidOperationException("PostureBreak controller differs from the target-only controller.");
            RequireLoopController(targetAnimator.runtimeAnimatorController as AnimatorController, adjustedClip);

            if (!string.Equals(HierarchyComponentSignature(sourceTergo), HierarchyComponentSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(RendererMaterialSignature(sourceTergo), RendererMaterialSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(AnimatorSignature(sourceTergo), AnimatorSignature(tergoCopy), StringComparison.Ordinal))
                throw new InvalidOperationException("Tergo duplicate changed during PostureBreak leg closure.");
            if (Vector3.Distance(sourceTergo.transform.localScale, tergoCopy.transform.localScale) > 0.00001f)
                throw new InvalidOperationException("Tergo duplicate scale changed during PostureBreak leg closure.");

            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            ChestMountSolution expected = SolveWaistMountFromSceneObjects(
                target, adjustedClip, tergoCopy, tergoClip);
            Vector3 expectedPosition = target.transform.position + expected.Position;
            float tergoPositionError = Vector3.Distance(expectedPosition, tergoCopy.transform.position);
            float tergoRotationError = Quaternion.Angle(expected.Rotation, tergoCopy.transform.rotation);
            if (tergoPositionError > 0.0001f || tergoRotationError > 0.001f)
                throw new InvalidOperationException(
                    "Tergo mount changed during leg closure. positionError=" + F(tergoPositionError) +
                    " rotationError=" + F(tergoRotationError));

            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            string report = new StringBuilder()
                .AppendLine("PostureBreak forty-percent full-leg closure inspection")
                .AppendLine("verificationSceneTargetManipulated=False")
                .AppendLine("source=" + sourceMetrics.Describe())
                .AppendLine("adjusted=" + adjustedMetrics.Describe())
                .AppendLine("closure=" + adjustedMetrics.DescribeClosureFrom(sourceMetrics))
                .AppendLine("onlyFourLegBoneRotationCurvesDiffer=True")
                .AppendLine("upperBodyAndRootCurvesMatch=True")
                .AppendLine("loopControllerPreserved=True")
                .AppendLine("tergoPositionErrorMeters=" + F(tergoPositionError))
                .AppendLine("tergoRotationErrorDegrees=" + F(tergoRotationError))
                .AppendLine("tergoHierarchyMaterialsControllerScaleMatch=True")
                .AppendLine("unityConsoleErrors=0")
                .AppendLine("directVisualReviewPending=True")
                .ToString();
            WriteText(LegCloseInspectionReport, report);
            Debug.Log("[PostureBreak] Forty-percent full-leg closure inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Leg Close Runtime")]
        internal static void InspectLegCloseRuntime()
        {
            RequireEditMode();
            InspectLegClose();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("Adjusted PostureBreak clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            AnimatorPlaybackMetrics targetMetrics = EvaluateAnimatorPlayback(target, CopiedStateName, targetClip, false);
            AnimatorPlaybackMetrics tergoMetrics = EvaluateAnimatorPlayback(tergoCopy, tergoClip.name, tergoClip, true);
            if (!targetMetrics.MotionObserved || !targetMetrics.RepeatObserved ||
                !tergoMetrics.MotionObserved || !tergoMetrics.RepeatObserved)
                throw new InvalidOperationException(
                    "PostureBreak leg-close runtime repeat failed. target=" + targetMetrics.Describe() +
                    " tergo=" + tergoMetrics.Describe());
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            string report = new StringBuilder()
                .AppendLine("PostureBreak forty-percent full-leg closure runtime inspection")
                .AppendLine("verificationSceneTargetManipulated=False")
                .AppendLine("target=" + targetMetrics.Describe())
                .AppendLine("tergo=" + tergoMetrics.Describe())
                .AppendLine("targetEntireClipRepeats=True")
                .AppendLine("tergoOriginalAnimationRepeats=True")
                .AppendLine("unityConsoleErrors=0")
                .ToString();
            WriteText(LegCloseRuntimeReport, report);
            Debug.Log("[PostureBreak] Forty-percent full-leg closure runtime inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Capture Posture Break Leg Close Final")]
        internal static void CaptureLegCloseFinal()
        {
            RequireEditMode();
            InspectLegClose();
            string playModeImagePath = Absolute(LegClosePlayModeImage);
            string playModeReportPath = Absolute(LegClosePlayModeReport);
            if (!File.Exists(playModeImagePath) || !File.Exists(playModeReportPath))
                throw new InvalidOperationException("PostureBreak leg-close Play Mode evidence is missing.");
            string playModeReport = File.ReadAllText(playModeReportPath);
            if (!playModeReport.Contains("captureSource=actual Play Mode scene object rendered in place without cloning or pose manipulation") ||
                !playModeReport.Contains("directVisualReviewPrimary=True"))
                throw new InvalidOperationException("PostureBreak leg-close evidence is not an actual Play Mode capture.");
            string finalImagePath = Absolute(LegCloseFinalImage);
            Directory.CreateDirectory(Path.GetDirectoryName(finalImagePath));
            File.WriteAllBytes(finalImagePath, File.ReadAllBytes(playModeImagePath));
            string report = new StringBuilder()
                .AppendLine("PostureBreak forty-percent full-leg closure final actual Play Mode contact sheet")
                .AppendLine("row1=actual overhead 0%,12.5%,25%,37.5%")
                .AppendLine("row2=actual overhead 50%,62.5%,75%,87.5%")
                .AppendLine("row3=actual side 0%,12.5%,25%,37.5%")
                .AppendLine("row4=actual side 50%,62.5%,75%,87.5%")
                .AppendLine("lateralRemainingRatio=" + F(LegLateralRemainingRatio))
                .AppendLine("captureSource=actual Play Mode scene object rendered in place without cloning or pose manipulation")
                .AppendLine("sourceAnimationModified=False")
                .AppendLine("upperBodyAndRootCurvesModified=False")
                .AppendLine("tergoModified=False")
                .AppendLine("directVisualReviewPrimary=True")
                .AppendLine("numericAndScriptInspectionSecondary=True")
                .ToString();
            WriteText(LegCloseFinalReport, report);
            Debug.Log("[PostureBreak] Leg-close final actual Play Mode contact sheet captured once.");
        }

        internal static string LegCloseSourceAbsolutePath => Absolute(LegCloseSourceImage);
        internal static string LegClosePlayModeAbsolutePath => Absolute(LegClosePlayModeImage);
        internal static string LegCloseFinalAbsolutePath => Absolute(LegCloseFinalImage);

        [MenuItem("Bellerophon/Player/Inspect Posture Break Upper Body Twitch Sources")]
        internal static void InspectUpperBodyTwitchSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip baseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak leg-closed base clip is missing.");
            Quaternion twitchDelta = SolveUpperBodyTwitchDelta(target, baseClip);
            KneeTwitchSolution kneeTwitch = SolveKneeTwitchDeltas(target, baseClip);
            IReadOnlyDictionary<string, LegLocalPose> firstLegPose =
                CaptureFirstLegPose(target, baseClip);
            float[] peakTimes = { 0.10f, 1.10f, 2.10f, 3.10f };
            float[] returnTimes = { 0.20f, 1.20f, 2.20f, 3.20f };
            GameObject preview = CloneForPreview(target, "PostureBreak_UpperBodyTwitchPreview");
            var panels = new List<Texture2D>();
            try
            {
                foreach (float time in peakTimes)
                    panels.Add(RenderUpperBodyTwitchPreview(
                        preview, baseClip, time, firstLegPose, Quaternion.identity,
                        KneeTwitchSolution.Identity, PreviewView.Side));
                foreach (float time in peakTimes)
                    panels.Add(RenderUpperBodyTwitchPreview(
                        preview, baseClip, time, firstLegPose, twitchDelta,
                        kneeTwitch, PreviewView.Side));
                foreach (float time in returnTimes)
                    panels.Add(RenderUpperBodyTwitchPreview(
                        preview, baseClip, time, firstLegPose, Quaternion.identity,
                        KneeTwitchSolution.Identity, PreviewView.Side));
                foreach (float time in peakTimes)
                    panels.Add(RenderUpperBodyTwitchPreview(
                        preview, baseClip, time, firstLegPose, Quaternion.identity,
                        KneeTwitchSolution.Identity, PreviewView.Top));
                foreach (float time in peakTimes)
                    panels.Add(RenderUpperBodyTwitchPreview(
                        preview, baseClip, time, firstLegPose, twitchDelta,
                        kneeTwitch, PreviewView.Top));
                foreach (float time in returnTimes)
                    panels.Add(RenderUpperBodyTwitchPreview(
                        preview, baseClip, time, firstLegPose, Quaternion.identity,
                        KneeTwitchSolution.Identity, PreviewView.Top));
                Texture2D sheet = Combine(panels, 4, 6);
                try { WritePng(UpperBodyTwitchSourceImage, sheet); }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }

                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                string report = new StringBuilder()
                    .AppendLine("PostureBreak static first-pose legs with synchronized upper-body and knee twitch source comparison")
                    .AppendLine("verificationSceneTargetManipulated=False")
                    .AppendLine("rows1to3=side static first-leg pose, synchronized peak, returned pose")
                    .AppendLine("rows4to6=body-front static first-leg pose, synchronized peak, returned pose")
                    .AppendLine("columns=0.10/1.10/2.10/3.10 seconds for rest and peak; 0.20/1.20/2.20/3.20 seconds for return")
                    .AppendLine("twitchAngleDegrees=" + F(Quaternion.Angle(Quaternion.identity, twitchDelta)))
                    .AppendLine("leftKneeTwitchAngleDegrees=" + F(kneeTwitch.LeftAngle))
                    .AppendLine("rightKneeTwitchAngleDegrees=" + F(kneeTwitch.RightAngle))
                    .AppendLine("periodSeconds=" + F(UpperBodyTwitchPeriod))
                    .AppendLine("peakTimeSeconds=" + F(UpperBodyTwitchPeakTime))
                    .AppendLine("returnTimeSeconds=" + F(UpperBodyTwitchReturnTime))
                    .AppendLine("baseClipModified=False")
                    .AppendLine("sceneSaved=False")
                    .AppendLine("unityConsoleErrors=0")
                    .AppendLine("directVisualReviewPrimary=True")
                    .ToString();
                WriteText(UpperBodyTwitchSourceReport, report);
                Debug.Log("[PostureBreak] Upper-body twitch candidate captured read-only.\n" + report);
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        [MenuItem("Bellerophon/Player/Apply Posture Break Upper Body Twitch")]
        internal static void ApplyUpperBodyTwitch()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject lying = FindUnique(scene, LyingCarrierName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip sourceClip = RequireSingleControllerClip(lying, LyingCarrierName);
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            AnimationClip baseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak leg-closed base clip is missing.");
            string sourcePath = AssetDatabase.GetAssetPath(sourceClip);
            Hash128 sourceHashBefore = AssetDatabase.GetAssetDependencyHash(sourcePath);
            string tergoPath = AssetDatabase.GetAssetPath(tergoClip);
            Hash128 tergoHashBefore = AssetDatabase.GetAssetDependencyHash(tergoPath);
            TransformSnapshot targetRootBefore = TransformSnapshot.Capture(target.transform);
            TransformSnapshot tergoRootBefore = TransformSnapshot.Capture(tergoCopy.transform);

            string nonLegSignatureBefore = ClipContentSignatureExcludingFullLegCurves(baseClip);
            FreezeFullLegCurvesAtFirstPose(baseClip);
            if (!string.Equals(
                    nonLegSignatureBefore,
                    ClipContentSignatureExcludingFullLegCurves(baseClip),
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "PostureBreak non-leg curves changed while freezing the first leg pose.");
            Quaternion twitchDelta = SolveUpperBodyTwitchDelta(target, baseClip);
            KneeTwitchSolution kneeTwitch = SolveKneeTwitchDeltas(target, baseClip);
            AnimationClip twitchClip = CreateUpperBodyTwitchClip(
                target, baseClip, twitchDelta, kneeTwitch, tergoClip.length);
            AvatarMask twitchMask = CreateUpperBodyTwitchMask(target.transform);
            AnimatorController controller = ConfigureUpperBodyTwitchLayer(baseClip, twitchClip, twitchMask);
            Animator animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(TargetName + " has no Animator.");
            if (animator.runtimeAnimatorController != controller)
                throw new InvalidOperationException("PostureBreak target controller changed unexpectedly.");

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(UpperBodyTwitchClipPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(UpperBodyTwitchMaskPath, ImportAssetOptions.ForceSynchronousImport);
            targetRootBefore.RequireUnchanged(target.transform, TargetName);
            tergoRootBefore.RequireUnchanged(tergoCopy.transform, TergoCopyName);
            if (sourceHashBefore != AssetDatabase.GetAssetDependencyHash(sourcePath))
                throw new InvalidOperationException("Source Death_Tergo animation changed during upper-body twitch setup.");
            if (tergoHashBefore != AssetDatabase.GetAssetDependencyHash(tergoPath))
                throw new InvalidOperationException("Source Tergo attack animation changed during twitch synchronization.");

            InspectUpperBodyTwitch();
            string report = new StringBuilder()
                .AppendLine("PostureBreak static first-pose legs with synchronized upper-body and knee twitch application")
                .AppendLine("target=" + HierarchyPath(target.transform))
                .AppendLine("baseClip=" + CopiedClipPath)
                .AppendLine("additiveClip=" + UpperBodyTwitchClipPath)
                .AppendLine("mask=" + UpperBodyTwitchMaskPath)
                .AppendLine("frozenLegBranches=" + string.Join(",", StaticLegPaths))
                .AppendLine("additiveBranches=" + CarrierLowerChestPath + "," + LeftUpperLegPath + "," + RightUpperLegPath)
                .AppendLine("twitchAngleDegrees=" + F(Quaternion.Angle(Quaternion.identity, twitchDelta)))
                .AppendLine("leftKneeTwitchAngleDegrees=" + F(kneeTwitch.LeftAngle))
                .AppendLine("rightKneeTwitchAngleDegrees=" + F(kneeTwitch.RightAngle))
                .AppendLine("attackStartSeconds=" + F(UpperBodyTwitchAttackStartTime))
                .AppendLine("pulsePeriodSeconds=" + F(UpperBodyTwitchPeriod))
                .AppendLine("peakOffsetSeconds=" + F(UpperBodyTwitchPeakTime))
                .AppendLine("returnOffsetSeconds=" + F(UpperBodyTwitchReturnTime))
                .AppendLine("tergoAttackCycleSeconds=" + F(tergoClip.length))
                .AppendLine("baseLegCurvesFrozenAtFirstPose=True")
                .AppendLine("baseNonLegCurvesModified=False")
                .AppendLine("targetRootModified=False")
                .AppendLine("tergoRootModified=False")
                .AppendLine("sceneSaved=False")
                .ToString();
            WriteText(UpperBodyTwitchApplicationReport, report);
            Debug.Log("[PostureBreak] Upper-body twitch applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Upper Body Twitch")]
        internal static void InspectUpperBodyTwitch()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip baseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak leg-closed base clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            AnimationClip twitchClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UpperBodyTwitchClipPath) ??
                throw new InvalidOperationException("PostureBreak upper-body twitch clip is missing.");
            AvatarMask twitchMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyTwitchMaskPath) ??
                throw new InvalidOperationException("PostureBreak upper-body twitch mask is missing.");
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("PostureBreak controller is missing.");
            float legDrift = RequireFullLegCurvesFrozenAtFirstPose(baseClip);
            TwitchMetrics metrics = RequireUpperBodyTwitchClip(twitchClip, tergoClip.length);
            RequireUpperBodyTwitchMask(target.transform, twitchMask);
            RequireUpperBodyTwitchLayer(controller, baseClip, twitchClip, twitchMask);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            string report = new StringBuilder()
                .AppendLine("PostureBreak static first-pose legs with synchronized upper-body and knee twitch inspection")
                .AppendLine("verificationSceneTargetManipulated=False")
                .AppendLine("twitchAngleDegrees=" + F(metrics.UpperBodyAngle))
                .AppendLine("leftKneeTwitchAngleDegrees=" + F(metrics.LeftKneeAngle))
                .AppendLine("rightKneeTwitchAngleDegrees=" + F(metrics.RightKneeAngle))
                .AppendLine("maximumBaseLegDriftDegrees=" + F(legDrift))
                .AppendLine("attackStartSeconds=" + F(UpperBodyTwitchAttackStartTime))
                .AppendLine("pulsePeriodSeconds=" + F(UpperBodyTwitchPeriod))
                .AppendLine("peakOffsetSeconds=" + F(UpperBodyTwitchPeakTime))
                .AppendLine("returnOffsetSeconds=" + F(UpperBodyTwitchReturnTime))
                .AppendLine("tergoAttackCycleSeconds=" + F(twitchClip.length))
                .AppendLine("twitchPulseCountPerAttackCycle=" + CountAttackSynchronizedPulses(twitchClip.length))
                .AppendLine("preAttackTwitch=False")
                .AppendLine("additiveLayerWeight=1")
                .AppendLine("lowerChestAndBothUpperLegsOnlyMask=True")
                .AppendLine("baseLegsFrozenAtFirstPose=True")
                .AppendLine("baseNonLegCurvesPreserved=True")
                .AppendLine("targetRootAndTergoPreserved=True")
                .AppendLine("unityConsoleErrors=0")
                .AppendLine("directVisualReviewPending=True")
                .ToString();
            WriteText(UpperBodyTwitchInspectionReport, report);
            Debug.Log("[PostureBreak] Upper-body twitch inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Upper Body Twitch Runtime")]
        internal static void InspectUpperBodyTwitchRuntime()
        {
            RequireEditMode();
            InspectUpperBodyTwitch();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip baseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak base clip is missing.");
            AnimationClip twitchClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UpperBodyTwitchClipPath) ??
                throw new InvalidOperationException("PostureBreak twitch clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            AnimatorPlaybackMetrics baseMetrics = EvaluateAnimatorPlayback(target, CopiedStateName, baseClip, false);
            AnimatorPlaybackMetrics tergoMetrics = EvaluateAnimatorPlayback(tergoCopy, tergoClip.name, tergoClip, true);
            int twitchRepeats = EvaluateAnimatorLayerRepeats(
                target, 1, UpperBodyTwitchStateName, twitchClip.length * 2.25f);
            if (!baseMetrics.MotionObserved || !baseMetrics.RepeatObserved ||
                !tergoMetrics.MotionObserved || !tergoMetrics.RepeatObserved || twitchRepeats < 2)
                throw new InvalidOperationException(
                    "PostureBreak upper-body twitch runtime repeat failed. base=" + baseMetrics.Describe() +
                    " tergo=" + tergoMetrics.Describe() + " twitchRepeats=" + twitchRepeats);
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            string report = new StringBuilder()
                .AppendLine("PostureBreak upper-body twitch runtime inspection")
                .AppendLine("verificationSceneTargetManipulated=False")
                .AppendLine("base=" + baseMetrics.Describe())
                .AppendLine("tergo=" + tergoMetrics.Describe())
                .AppendLine("twitchObservedRepeatCount=" + twitchRepeats)
                .AppendLine("attackStartSeconds=" + F(UpperBodyTwitchAttackStartTime))
                .AppendLine("twitchPulsePeriodSeconds=" + F(UpperBodyTwitchPeriod))
                .AppendLine("twitchAttackCycleSeconds=" + F(twitchClip.length))
                .AppendLine("preAttackTwitch=False")
                .AppendLine("baseAndTergoLoopsPreserved=True")
                .AppendLine("unityConsoleErrors=0")
                .ToString();
            WriteText(UpperBodyTwitchRuntimeReport, report);
            Debug.Log("[PostureBreak] Upper-body twitch runtime inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Capture Posture Break Upper Body Twitch Final")]
        internal static void CaptureUpperBodyTwitchFinal()
        {
            RequireEditMode();
            InspectUpperBodyTwitch();
            string playModeImagePath = Absolute(UpperBodyTwitchPlayModeImage);
            string playModeReportPath = Absolute(UpperBodyTwitchPlayModeReport);
            if (!File.Exists(playModeImagePath) || !File.Exists(playModeReportPath))
                throw new InvalidOperationException("PostureBreak upper-body twitch Play Mode evidence is missing.");
            string playModeReport = File.ReadAllText(playModeReportPath);
            if (!playModeReport.Contains("captureSource=actual Play Mode scene objects rendered in place without cloning or pose manipulation") ||
                !playModeReport.Contains("preAttackTwitch=False") ||
                !playModeReport.Contains("twitchBeginsWithFirstChestStab=True") ||
                !playModeReport.Contains("directVisualReviewPrimary=True"))
                throw new InvalidOperationException("PostureBreak upper-body twitch evidence is not an actual Play Mode capture.");
            string finalImagePath = Absolute(UpperBodyTwitchFinalImage);
            Directory.CreateDirectory(Path.GetDirectoryName(finalImagePath));
            File.WriteAllBytes(finalImagePath, File.ReadAllBytes(playModeImagePath));
            string report = new StringBuilder()
                .AppendLine("PostureBreak twitch gated by the first Tergo chest stab final actual Play Mode contact sheet")
                .AppendLine("columns=cycle start,0.10 seconds before attack,attack start,first peak,first return,second onset,second peak,second return,third peak")
                .AppendLine("row1=actual Tergo attack cycle 1 side")
                .AppendLine("row2=actual Tergo attack cycle 1 body-front")
                .AppendLine("row3=actual Tergo attack cycle 2 side")
                .AppendLine("row4=actual Tergo attack cycle 2 body-front")
                .AppendLine("captureSource=actual Play Mode scene objects rendered in place without cloning or pose manipulation")
                .AppendLine("attackStartSeconds=" + F(UpperBodyTwitchAttackStartTime))
                .AppendLine("pulsePeriodSeconds=" + F(UpperBodyTwitchPeriod))
                .AppendLine("preAttackTwitch=False")
                .AppendLine("twitchBeginsWithFirstChestStab=True")
                .AppendLine("baseLegCurvesFrozenAtFirstPose=True")
                .AppendLine("baseNonLegCurvesModified=False")
                .AppendLine("tergoModified=False")
                .AppendLine("directVisualReviewPrimary=True")
                .AppendLine("numericAndScriptInspectionSecondary=True")
                .ToString();
            WriteText(UpperBodyTwitchFinalReport, report);
            Debug.Log("[PostureBreak] Upper-body twitch final actual Play Mode contact sheet captured once.");
        }

        internal static string UpperBodyTwitchSourceAbsolutePath => Absolute(UpperBodyTwitchSourceImage);
        internal static string UpperBodyTwitchPlayModeAbsolutePath => Absolute(UpperBodyTwitchPlayModeImage);
        internal static string UpperBodyTwitchFinalAbsolutePath => Absolute(UpperBodyTwitchFinalImage);

        [MenuItem("Bellerophon/Player/Inspect Posture Break Twitch Attack Sync Sources")]
        internal static void InspectTwitchAttackSyncSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergo = FindUnique(scene, TergoCopyName);
            AnimationClip baseClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak base clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergo, TergoCopyName);
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_AttackSyncTargetPreview");
            GameObject tergoPreview = CloneForPreview(tergo, "PostureBreak_AttackSyncTergoPreview");
            var panels = new List<Texture2D>();
            var times = new List<float>();
            try
            {
                const float step = 0.05f;
                for (float time = 0.40f; time <= 0.75f + 0.0001f; time += step)
                    times.Add(time);
                foreach (float time in times)
                {
                    float targetTime = baseClip.length > 0f ? time % baseClip.length : 0f;
                    panels.Add(RenderPairFocused(
                        targetPreview,
                        baseClip,
                        targetTime,
                        tergoPreview,
                        tergoClip,
                        time,
                        PreviewView.Side,
                        0.44f));
                }
                foreach (float time in times)
                {
                    float targetTime = baseClip.length > 0f ? time % baseClip.length : 0f;
                    panels.Add(RenderPairFocused(
                        targetPreview,
                        baseClip,
                        targetTime,
                        tergoPreview,
                        tergoClip,
                        time,
                        PreviewView.Top,
                        0.44f));
                }
                Texture2D sheet = Combine(panels, times.Count, 2);
                try { WritePng(TwitchAttackSyncSourceImage, sheet); }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }

                DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                string[] attackRigCandidates = tergo.GetComponentsInChildren<Transform>(true)
                    .Where(item =>
                        item.name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("drill", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("claw", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("tip", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(item => HierarchyPath(item))
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray();
                string report = new StringBuilder()
                    .AppendLine("PostureBreak and Tergo attack-start direct timing source sheet")
                    .AppendLine("verificationSceneTargetManipulated=False")
                    .AppendLine("captureMethod=preview clones preserving current scene root transforms and original animation curves")
                    .AppendLine("row1=carrier side focused on waist-to-chest attack region")
                    .AppendLine("row2=carrier body-front focused on waist-to-chest attack region")
                    .AppendLine("columns=" + times.Count)
                    .AppendLine("sampleStepSeconds=" + F(step))
                    .AppendLine("tergoClipLengthSeconds=" + F(tergoClip.length))
                    .AppendLine("targetBaseClipLengthSeconds=" + F(baseClip.length))
                    .AppendLine("sampleTimesSeconds=" + string.Join(",", times.Select(F)))
                    .AppendLine("attackRigCandidates=" + string.Join("|", attackRigCandidates))
                    .AppendLine("directVisualReviewPrimary=True")
                    .AppendLine("numericAndScriptInspectionSecondary=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString();
                WriteText(TwitchAttackSyncSourceReport, report);
                Debug.Log("[PostureBreak] Attack-sync source timing sheet captured read-only.\n" + report);
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        internal static string TwitchAttackSyncSourceAbsolutePath => Absolute(TwitchAttackSyncSourceImage);

        [MenuItem("Bellerophon/Player/Apply Posture Break Lying And Tergo Placement")]
        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject lying = FindUnique(scene, LyingCarrierName);
            GameObject placement = FindUnique(scene, TergoPlacementRootName);
            GameObject sourceTergo = FindDirectChild(placement.transform, TergoAttackName).gameObject;
            Animator targetAnimator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(TargetName + " has no root Animator.");
            AnimationClip sourceClip = RequireSingleControllerClip(lying, LyingCarrierName);
            string sourceClipPath = AssetDatabase.GetAssetPath(sourceClip);
            Hash128 sourceClipHashBefore = AssetDatabase.GetAssetDependencyHash(sourceClipPath);
            TransformSnapshot targetRootBefore = TransformSnapshot.Capture(target.transform);
            TransformSnapshot sourceTergoRootBefore = TransformSnapshot.Capture(sourceTergo.transform);
            string sourceTergoHierarchyBefore = HierarchyComponentSignature(sourceTergo);
            string sourceTergoMaterialsBefore = RendererMaterialSignature(sourceTergo);
            string sourceTergoAnimatorsBefore = AnimatorSignature(sourceTergo);

            EnsureAssetFolder(AssetFolder);
            if (AssetDatabase.LoadMainAssetAtPath(CopiedClipPath) != null &&
                !AssetDatabase.DeleteAsset(CopiedClipPath))
                throw new InvalidOperationException("Failed to replace target-only copied clip: " + CopiedClipPath);
            if (!AssetDatabase.CopyAsset(sourceClipPath, CopiedClipPath))
                throw new InvalidOperationException("Failed to copy lying animation exactly: " + sourceClipPath);
            AssetDatabase.ImportAsset(CopiedClipPath, ImportAssetOptions.ForceSynchronousImport);
            AnimationClip copiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("Copied PostureBreak clip is missing.");
            if (!string.Equals(ClipContentSignature(sourceClip), ClipContentSignature(copiedClip), StringComparison.Ordinal))
                throw new InvalidOperationException("PostureBreak copied clip differs from the lying carrier source curves/settings/events.");

            AnimatorController controller = CreateLoopController(copiedClip);
            Undo.RecordObject(targetAnimator, "Connect exact lying animation to PostureBreak");
            targetAnimator.runtimeAnimatorController = controller;
            targetAnimator.applyRootMotion = false;
            targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            targetAnimator.enabled = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetAnimator);
            EditorUtility.SetDirty(targetAnimator);

            GameObject existingCopy = FindOptionalUnique(scene, TergoCopyName);
            if (existingCopy != null)
                UnityEngine.Object.DestroyImmediate(existingCopy);

            GameObject tergoCopy = UnityEngine.Object.Instantiate(sourceTergo, target.transform.parent);
            tergoCopy.name = TergoCopyName;
            tergoCopy.SetActive(sourceTergo.activeSelf);
            Vector3 forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(TargetName + " has no stable horizontal forward direction.");
            Vector3 copyPosition = target.transform.position + forward * PlacementDistance;
            copyPosition.y = target.transform.position.y;
            tergoCopy.transform.SetPositionAndRotation(copyPosition, sourceTergo.transform.rotation);
            tergoCopy.transform.localScale = sourceTergo.transform.localScale;
            tergoCopy.transform.SetSiblingIndex(target.transform.GetSiblingIndex() + 1);

            if (!string.Equals(sourceTergoHierarchyBefore, HierarchyComponentSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(sourceTergoMaterialsBefore, RendererMaterialSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(sourceTergoAnimatorsBefore, AnimatorSignature(tergoCopy), StringComparison.Ordinal))
                throw new InvalidOperationException("Placed Tergo duplicate differs from its source hierarchy, materials, or animation.");
            targetRootBefore.RequireUnchanged(target.transform, TargetName);
            sourceTergoRootBefore.RequireUnchanged(sourceTergo.transform, TergoAttackName);
            if (!string.Equals(sourceTergoHierarchyBefore, HierarchyComponentSignature(sourceTergo), StringComparison.Ordinal) ||
                !string.Equals(sourceTergoMaterialsBefore, RendererMaterialSignature(sourceTergo), StringComparison.Ordinal) ||
                !string.Equals(sourceTergoAnimatorsBefore, AnimatorSignature(sourceTergo), StringComparison.Ordinal))
                throw new InvalidOperationException("Source Tergo changed during duplication.");
            if (sourceClipHashBefore != AssetDatabase.GetAssetDependencyHash(sourceClipPath))
                throw new InvalidOperationException("Source lying animation changed during PostureBreak application.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save CargoRunMvp after PostureBreak application.");
            AssetDatabase.SaveAssets();
            Inspect();

            var report = new StringBuilder()
                .AppendLine("PostureBreak lying animation and Tergo placement application")
                .AppendLine("lyingSourceObject=" + HierarchyPath(lying.transform))
                .AppendLine("lyingSourceClip=" + sourceClipPath)
                .AppendLine("lyingClipCopiedExactly=True")
                .AppendLine("sourceCurvesGeneratedOrModified=False")
                .AppendLine("copiedClip=" + CopiedClipPath)
                .AppendLine("controller=" + ControllerPath)
                .AppendLine("repeatMethod=ZeroDurationSelfTransitionAtExitTime1")
                .AppendLine("tergoSource=" + HierarchyPath(sourceTergo.transform))
                .AppendLine("tergoCopy=" + HierarchyPath(tergoCopy.transform))
                .AppendLine("tergoAnimationControllerCopiedUnchanged=True")
                .AppendLine("tergoRotationCopiedUnchanged=True")
                .AppendLine("tergoScaleCopiedUnchanged=True")
                .AppendLine("placement=PostureBreakHorizontalForward2m")
                .AppendLine("groundHeight=PostureBreakRootY")
                .AppendLine("sourceObjectsModified=False")
                .AppendLine("sceneSaved=True");
            WriteText(ApplicationReport, report.ToString());
            Debug.Log("[PostureBreak] Exact lying animation and exact Tergo attack duplicate applied.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Lying And Tergo Placement")]
        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject lying = FindUnique(scene, LyingCarrierName);
            GameObject placement = FindUnique(scene, TergoPlacementRootName);
            GameObject sourceTergo = FindDirectChild(placement.transform, TergoAttackName).gameObject;
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip sourceClip = RequireSingleControllerClip(lying, LyingCarrierName);
            AnimationClip copiedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            if (!string.Equals(ClipContentSignature(sourceClip), ClipContentSignature(copiedClip), StringComparison.Ordinal))
                throw new InvalidOperationException("PostureBreak copied animation content differs from Death_Tergo.");

            Animator targetAnimator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(TargetName + " has no Animator.");
            if (AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController) != ControllerPath)
                throw new InvalidOperationException("PostureBreak controller differs: " +
                    AssetDatabase.GetAssetPath(targetAnimator.runtimeAnimatorController));
            RequireLoopController(targetAnimator.runtimeAnimatorController as AnimatorController, copiedClip);

            if (!string.Equals(HierarchyComponentSignature(sourceTergo), HierarchyComponentSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(RendererMaterialSignature(sourceTergo), RendererMaterialSignature(tergoCopy), StringComparison.Ordinal) ||
                !string.Equals(AnimatorSignature(sourceTergo), AnimatorSignature(tergoCopy), StringComparison.Ordinal))
                throw new InvalidOperationException("Tergo duplicate no longer matches the source object exactly.");
            if (Quaternion.Angle(sourceTergo.transform.rotation, tergoCopy.transform.rotation) > 0.001f)
                throw new InvalidOperationException("Tergo duplicate rotation differs from the source.");
            if (Vector3.Distance(sourceTergo.transform.localScale, tergoCopy.transform.localScale) > 0.00001f)
                throw new InvalidOperationException("Tergo duplicate scale differs from the source.");

            Vector3 forward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up).normalized;
            Vector3 offset = tergoCopy.transform.position - target.transform.position;
            float forwardDistance = Vector3.Dot(Vector3.ProjectOnPlane(offset, Vector3.up), forward);
            float lateralDistance = Mathf.Abs(Vector3.Dot(Vector3.ProjectOnPlane(offset, Vector3.up), target.transform.right.normalized));
            float groundDifference = Mathf.Abs(tergoCopy.transform.position.y - target.transform.position.y);
            if (Mathf.Abs(forwardDistance - PlacementDistance) > 0.001f ||
                lateralDistance > 0.001f || groundDifference > 0.001f)
                throw new InvalidOperationException(
                    "Tergo placement differs. forward=" + F(forwardDistance) +
                    " lateral=" + F(lateralDistance) + " ground=" + F(groundDifference));

            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            var report = new StringBuilder()
                .AppendLine("PostureBreak structural and sampled inspection")
                .AppendLine("verificationTargetsManipulated=False")
                .AppendLine("lyingSource=" + AssetDatabase.GetAssetPath(sourceClip))
                .AppendLine("copiedClip=" + CopiedClipPath)
                .AppendLine("clipContentSignatureMatches=True")
                .AppendLine("sourceAnimationModified=False")
                .AppendLine("loopSelfTransition=True")
                .AppendLine("tergoSource=" + HierarchyPath(sourceTergo.transform))
                .AppendLine("tergoCopy=" + HierarchyPath(tergoCopy.transform))
                .AppendLine("tergoHierarchyComponentsMatch=True")
                .AppendLine("tergoMaterialsMatch=True")
                .AppendLine("tergoAnimatorControllersMatch=True")
                .AppendLine("tergoRotationMatch=True")
                .AppendLine("tergoScaleMatch=True")
                .AppendLine("forwardDistanceMeters=" + F(forwardDistance))
                .AppendLine("lateralDistanceMeters=" + F(lateralDistance))
                .AppendLine("groundHeightDifferenceMeters=" + F(groundDifference))
                .AppendLine("unityConsoleErrors=0")
                .AppendLine("directVisualReviewPending=True");
            WriteText(InspectionReport, report.ToString());
            Debug.Log("[PostureBreak] Structural inspection passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Inspect Posture Break Runtime Loop")]
        internal static void InspectRuntime()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            AnimatorPlaybackMetrics targetMetrics = EvaluateAnimatorPlayback(target, CopiedStateName, targetClip, false);
            AnimatorPlaybackMetrics tergoMetrics = EvaluateAnimatorPlayback(tergoCopy, tergoClip.name, tergoClip, true);
            if (!targetMetrics.MotionObserved || !targetMetrics.RepeatObserved ||
                !tergoMetrics.MotionObserved || !tergoMetrics.RepeatObserved)
                throw new InvalidOperationException(
                    "PostureBreak runtime-state evaluation failed. target=" + targetMetrics.Describe() +
                    " tergo=" + tergoMetrics.Describe());
            DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
            var report = new StringBuilder()
                .AppendLine("PostureBreak Animator runtime-state evaluation")
                .AppendLine("verificationSceneTargetsManipulated=False")
                .AppendLine("target=" + targetMetrics.Describe())
                .AppendLine("tergo=" + tergoMetrics.Describe())
                .AppendLine("targetEntireClipRepeats=True")
                .AppendLine("tergoOriginalAnimationRepeats=True")
                .AppendLine("unityConsoleErrors=0")
                .AppendLine("directVisualReviewPending=True");
            WriteText(RuntimeInspectionReport, report.ToString());
            Debug.Log("[PostureBreak] Animator runtime-state evaluation passed.\n" + report);
        }

        [MenuItem("Bellerophon/Player/Capture Posture Break Final")]
        internal static void CaptureFinal()
        {
            RequireEditMode();
            Inspect();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_FinalTargetPreview");
            GameObject tergoPreview = CloneForPreview(tergoCopy, "PostureBreak_FinalTergoPreview");
            targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            tergoPreview.transform.position = tergoCopy.transform.position - target.transform.position;
            tergoPreview.transform.rotation = tergoCopy.transform.rotation;
            tergoPreview.transform.localScale = tergoCopy.transform.lossyScale;
            try
            {
                tergoPreview.SetActive(false);
                var panels = new List<Texture2D>
                {
                    RenderSample(targetPreview, targetClip, 0f),
                    RenderSample(targetPreview, targetClip, targetClip.length * 0.5f),
                    RenderSample(targetPreview, targetClip, Mathf.Max(0f, targetClip.length - 0.001f))
                };
                targetPreview.SetActive(true);
                tergoPreview.SetActive(true);
                panels.Add(RenderPair(targetPreview, targetClip, Mathf.Max(0f, targetClip.length - 0.001f),
                    tergoPreview, tergoClip, tergoClip.length * 0.25f));
                panels.Add(RenderPair(targetPreview, targetClip, Mathf.Max(0f, targetClip.length - 0.001f),
                    tergoPreview, tergoClip, tergoClip.length * 0.5f));
                panels.Add(RenderPair(targetPreview, targetClip, Mathf.Max(0f, targetClip.length - 0.001f),
                    tergoPreview, tergoClip, tergoClip.length * 0.75f));
                try
                {
                    Texture2D sheet = Combine(panels, 3, 2);
                    try { WritePng(FinalImage, sheet); }
                    finally { UnityEngine.Object.DestroyImmediate(sheet); }
                }
                finally
                {
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
            WriteText(FinalReport,
                "PostureBreak final direct contact sheet\n" +
                "row1=PostureBreak exact copied lying clip 0%,50%,100%\n" +
                "row2=PostureBreak lying end pose with exact Tergo duplicate at source clip 25%,50%,75%\n" +
                "placement=horizontal forward 2m; same root ground height\n" +
                "sourceAnimationCurvesModified=False\n" +
                "sourceTergoRotationScaleAnimationModified=False\n" +
                "directVisualReviewPrimary=True\n" +
                "numericAndScriptInspectionSecondary=True\n");
            Debug.Log("[PostureBreak] Final direct contact sheet captured once.");
        }

        internal static string FinalAbsolutePath => Absolute(FinalImage);

        private static ChestMountSolution SolveChestMount(
            GameObject target,
            AnimationClip targetClip,
            GameObject tergo,
            AnimationClip tergoClip,
            bool faceCarrierHead,
            float rootHeightOffset,
            string mountPath)
        {
            float targetTime = Mathf.Max(0f, targetClip.length - 0.001f);
            targetClip.SampleAnimation(target, targetTime);
            Transform carrierHips = FindPath(target.transform, CarrierHipsPath);
            Transform carrierChest = FindPath(target.transform, mountPath);
            Transform carrierHead = FindPath(target.transform, CarrierHeadPath);
            Vector3 up = Vector3.up;
            Vector3 bodyAxis = Vector3.ProjectOnPlane(
                carrierHead.position - carrierHips.position,
                up).normalized;
            if (bodyAxis.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("PostureBreak lying body axis could not be resolved from hips to head.");
            Vector3 facing = faceCarrierHead ? bodyAxis : -bodyAxis;
            Quaternion rotation = Quaternion.LookRotation(facing, up);
            tergo.transform.SetPositionAndRotation(
                new Vector3(0f, target.transform.position.y, 0f),
                rotation);

            float[] phases = { 0.25f, 0.5f, 0.75f };
            Vector3 averageHips = Vector3.zero;
            foreach (float phase in phases)
            {
                tergoClip.SampleAnimation(tergo, tergoClip.length * phase);
                averageHips += FindPath(tergo.transform, TergoHipsPath).position;
            }
            averageHips /= phases.Length;
            Vector3 rootToHips = averageHips - tergo.transform.position;
            Vector3 position = carrierChest.position - Vector3.ProjectOnPlane(rootToHips, up);
            position.y = target.transform.position.y + rootHeightOffset;
            return new ChestMountSolution(
                position,
                rotation,
                carrierChest.position,
                bodyAxis,
                rootToHips,
                faceCarrierHead,
                rootHeightOffset,
                mountPath);
        }

        private static ChestMountSolution SolveWaistMount(
            GameObject target,
            AnimationClip targetClip,
            GameObject tergo,
            AnimationClip tergoClip,
            float anchorRatio,
            float rootHeightOffset)
        {
            float targetTime = Mathf.Max(0f, targetClip.length - 0.001f);
            targetClip.SampleAnimation(target, targetTime);
            Transform carrierHips = FindPath(target.transform, CarrierHipsPath);
            Transform carrierLowerChest = FindPath(target.transform, CarrierLowerChestPath);
            Transform carrierHead = FindPath(target.transform, CarrierHeadPath);
            Vector3 bodyAxis = Vector3.ProjectOnPlane(
                carrierHead.position - carrierHips.position,
                Vector3.up).normalized;
            if (bodyAxis.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("PostureBreak lying body axis could not be resolved from hips to head.");
            Vector3 waistAnchor = Vector3.Lerp(
                carrierHips.position,
                carrierLowerChest.position,
                Mathf.Clamp01(anchorRatio));
            Quaternion rotation = Quaternion.LookRotation(bodyAxis, Vector3.up);
            tergo.transform.SetPositionAndRotation(
                new Vector3(0f, target.transform.position.y, 0f),
                rotation);

            float[] phases = { 0.25f, 0.5f, 0.75f };
            Vector3 averageHips = Vector3.zero;
            foreach (float phase in phases)
            {
                tergoClip.SampleAnimation(tergo, tergoClip.length * phase);
                averageHips += FindPath(tergo.transform, TergoHipsPath).position;
            }
            averageHips /= phases.Length;
            Vector3 rootToHips = averageHips - tergo.transform.position;
            Vector3 position = waistAnchor - bodyAxis * ChestAttackRetreatDistance -
                Vector3.ProjectOnPlane(rootToHips, Vector3.up);
            position.y = target.transform.position.y + rootHeightOffset;
            return new ChestMountSolution(
                position,
                rotation,
                waistAnchor,
                bodyAxis,
                rootToHips,
                true,
                rootHeightOffset,
                "Lerp(" + CarrierHipsPath + "," + CarrierLowerChestPath + "," + F(anchorRatio) + ")");
        }

        private static HandAttackMetrics AnalyzeHandAttacks(
            GameObject target,
            AnimationClip targetClip,
            GameObject tergo,
            AnimationClip tergoClip)
        {
            targetClip.SampleAnimation(target, Mathf.Max(0f, targetClip.length - 0.001f));
            Vector3 attackTarget = FindPath(target.transform, CarrierUpperChestPath).position;
            Transform leftHand = FindUniqueDescendantByName(tergo.transform, TergoLeftHandName);
            Transform rightHand = FindUniqueDescendantByName(tergo.transform, TergoRightHandName);
            float leftClosestDistance = float.PositiveInfinity;
            float rightClosestDistance = float.PositiveInfinity;
            float leftClosestTime = 0f;
            float rightClosestTime = 0f;
            const int SampleCount = 120;
            for (int index = 0; index <= SampleCount; index++)
            {
                float time = tergoClip.length * index / SampleCount;
                tergoClip.SampleAnimation(tergo, time);
                float leftDistance = Vector3.Distance(leftHand.position, attackTarget);
                if (leftDistance < leftClosestDistance)
                {
                    leftClosestDistance = leftDistance;
                    leftClosestTime = time;
                }
                float rightDistance = Vector3.Distance(rightHand.position, attackTarget);
                if (rightDistance < rightClosestDistance)
                {
                    rightClosestDistance = rightDistance;
                    rightClosestTime = time;
                }
            }
            return new HandAttackMetrics(
                leftClosestTime,
                rightClosestTime,
                leftClosestDistance,
                rightClosestDistance,
                tergoClip.length,
                attackTarget);
        }

        private static Transform FindUniqueDescendantByName(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(item.name, name, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    root.name + " requires exactly one descendant named " + name +
                    ", found " + matches.Length + ". Candidates=" +
                    string.Join(",", root.GetComponentsInChildren<Transform>(true)
                        .Where(item => item.name.IndexOf("Hand", StringComparison.OrdinalIgnoreCase) >= 0)
                        .Select(item => HierarchyPath(item))));
            return matches[0];
        }

        private static ChestMountSolution SolveWaistMountFromSceneObjects(
            GameObject target,
            AnimationClip targetClip,
            GameObject tergo,
            AnimationClip tergoClip)
        {
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_ChestMountSolveTarget");
            GameObject tergoPreview = CloneForPreview(tergo, "PostureBreak_ChestMountSolveTergo");
            try
            {
                targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
                targetPreview.transform.localScale = target.transform.lossyScale;
                tergoPreview.transform.position = tergo.transform.position - target.transform.position;
                tergoPreview.transform.rotation = tergo.transform.rotation;
                tergoPreview.transform.localScale = tergo.transform.lossyScale;
                return SolveWaistMount(
                    targetPreview,
                    targetClip,
                    tergoPreview,
                    tergoClip,
                    WaistMountAnchorRatio,
                    ChestMountRootHeightOffset);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        private static ChestMountGeometryMetrics EvaluateWaistMountGeometry(
            GameObject target,
            AnimationClip targetClip,
            GameObject tergo,
            AnimationClip tergoClip)
        {
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_ChestMountGeometryTarget");
            GameObject tergoPreview = CloneForPreview(tergo, "PostureBreak_ChestMountGeometryTergo");
            try
            {
                targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
                targetPreview.transform.localScale = target.transform.lossyScale;
                tergoPreview.transform.position = tergo.transform.position - target.transform.position;
                tergoPreview.transform.rotation = tergo.transform.rotation;
                tergoPreview.transform.localScale = tergo.transform.lossyScale;
                targetClip.SampleAnimation(targetPreview, Mathf.Max(0f, targetClip.length - 0.001f));
                Transform carrierHips = FindPath(targetPreview.transform, CarrierHipsPath);
                Transform carrierLowerChest = FindPath(targetPreview.transform, CarrierLowerChestPath);
                Transform carrierHead = FindPath(targetPreview.transform, CarrierHeadPath);
                Vector3 bodyAxis = Vector3.ProjectOnPlane(
                    carrierHead.position - carrierHips.position,
                    Vector3.up).normalized;
                Vector3 waistAnchor = Vector3.Lerp(
                    carrierHips.position,
                    carrierLowerChest.position,
                    WaistMountAnchorRatio);
                Vector3 seatedAttackAnchor = waistAnchor - bodyAxis * ChestAttackRetreatDistance;
                Vector3 tergoFacing = Vector3.ProjectOnPlane(tergoPreview.transform.forward, Vector3.up).normalized;
                float facingDot = Vector3.Dot(tergoFacing, bodyAxis);
                float[] phases = { 0.25f, 0.5f, 0.75f };
                Vector3 averageHips = Vector3.zero;
                float maximumHorizontalError = 0f;
                foreach (float phase in phases)
                {
                    tergoClip.SampleAnimation(tergoPreview, tergoClip.length * phase);
                    Vector3 hipsPosition = FindPath(tergoPreview.transform, TergoHipsPath).position;
                    averageHips += hipsPosition;
                    maximumHorizontalError = Mathf.Max(
                        maximumHorizontalError,
                        Vector3.ProjectOnPlane(hipsPosition - seatedAttackAnchor, Vector3.up).magnitude);
                }
                averageHips /= phases.Length;
                float averageHorizontalHipError =
                    Vector3.ProjectOnPlane(averageHips - seatedAttackAnchor, Vector3.up).magnitude;
                return new ChestMountGeometryMetrics(
                    averageHorizontalHipError,
                    maximumHorizontalError,
                    facingDot);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        private static HandAttackMetrics AnalyzeHandAttacksFromSceneObjects(
            GameObject target,
            AnimationClip targetClip,
            GameObject tergo,
            AnimationClip tergoClip)
        {
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_HandAttackTarget");
            GameObject tergoPreview = CloneForPreview(tergo, "PostureBreak_HandAttackTergo");
            try
            {
                targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
                targetPreview.transform.localScale = target.transform.lossyScale;
                tergoPreview.transform.position = tergo.transform.position - target.transform.position;
                tergoPreview.transform.rotation = tergo.transform.rotation;
                tergoPreview.transform.localScale = tergo.transform.lossyScale;
                return AnalyzeHandAttacks(
                    targetPreview,
                    targetClip,
                    tergoPreview,
                    tergoClip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        private static void CaptureChestMountContactSheet(string outputPath)
        {
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject tergoCopy = FindUnique(scene, TergoCopyName);
            AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CopiedClipPath) ??
                throw new InvalidOperationException("PostureBreak copied clip is missing.");
            AnimationClip tergoClip = RequireSingleControllerClip(tergoCopy, TergoCopyName);
            GameObject targetPreview = CloneForPreview(target, "PostureBreak_ChestMountContactTarget");
            GameObject tergoPreview = CloneForPreview(tergoCopy, "PostureBreak_ChestMountContactTergo");
            targetPreview.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            targetPreview.transform.localScale = target.transform.lossyScale;
            tergoPreview.transform.position = tergoCopy.transform.position - target.transform.position;
            tergoPreview.transform.rotation = tergoCopy.transform.rotation;
            tergoPreview.transform.localScale = tergoCopy.transform.lossyScale;
            float targetTime = Mathf.Max(0f, targetClip.length - 0.001f);
            float[] sidePhases = { 0.25f, 0.35f, 0.416667f, 0.5f, 0.75f, 0.875f };
            float[] topPhases = { 0.35f, 0.416667f, 0.5f };
            try
            {
                var panels = new List<Texture2D>();
                foreach (float phase in sidePhases)
                {
                    float tergoTime = tergoClip.length * phase;
                    panels.Add(RenderPairFocused(
                        targetPreview, targetClip, targetTime,
                        tergoPreview, tergoClip, tergoTime,
                        PreviewView.Side));
                }
                foreach (float phase in topPhases)
                {
                    float tergoTime = tergoClip.length * phase;
                    panels.Add(RenderPairFocused(
                        targetPreview, targetClip, targetTime,
                        tergoPreview, tergoClip, tergoTime,
                        PreviewView.Top));
                }
                try
                {
                    Texture2D sheet = Combine(panels, 3, 3);
                    try { WritePng(outputPath, sheet); }
                    finally { UnityEngine.Object.DestroyImmediate(sheet); }
                }
                finally
                {
                    foreach (Texture2D panel in panels)
                        UnityEngine.Object.DestroyImmediate(panel);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tergoPreview);
                UnityEngine.Object.DestroyImmediate(targetPreview);
            }
        }

        private static Transform FindPath(Transform root, string relativePath)
        {
            Transform match = root.Find(relativePath);
            if (match == null)
                throw new InvalidOperationException(root.name + " is missing transform path: " + relativePath);
            return match;
        }

        private static AnimatorController UpdateLoopControllerMotion(AnimationClip copiedClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return CreateLoopController(copiedClip);
            if (controller.layers.Length < 1 || controller.layers.Length > 2)
                throw new InvalidOperationException("PostureBreak controller must keep one base layer and at most one twitch layer.");
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ChildAnimatorState[] states = stateMachine.states;
            if (states.Length != 1 || !string.Equals(states[0].state.name, CopiedStateName, StringComparison.Ordinal))
                throw new InvalidOperationException("PostureBreak controller state structure changed unexpectedly.");
            AnimatorState state = states[0].state;
            state.motion = copiedClip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            RequireLoopController(controller, copiedClip);
            return controller;
        }

        private static void FreezeFullLegCurvesAtFirstPose(AnimationClip clip)
        {
            EditorCurveBinding[] legBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(IsFullLegBinding)
                .ToArray();
            if (legBindings.Length == 0)
                throw new InvalidOperationException("PostureBreak base clip has no leg curves to freeze.");
            float[] times = { 0f, clip.length };
            foreach (EditorCurveBinding binding in legBindings)
            {
                AnimationCurve source = AnimationUtility.GetEditorCurve(clip, binding) ??
                    throw new InvalidOperationException(
                        "PostureBreak leg curve is missing while freezing: " + binding.path + "/" + binding.propertyName);
                float value = source.Evaluate(0f);
                SetLinearCurve(clip, binding.path, binding.propertyName, times, new[] { value, value });
            }
            if (AnimationUtility.GetObjectReferenceCurveBindings(clip).Any(IsFullLegBinding))
                throw new InvalidOperationException("PostureBreak leg chains contain unsupported object-reference curves.");
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
        }

        private static float RequireFullLegCurvesFrozenAtFirstPose(AnimationClip clip)
        {
            EditorCurveBinding[] legBindings = AnimationUtility.GetCurveBindings(clip)
                .Where(IsFullLegBinding)
                .ToArray();
            foreach (string path in StaticLegPaths)
            {
                int rotationBindings = legBindings.Count(binding =>
                    binding.path == path &&
                    binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal));
                if (rotationBindings != 4)
                    throw new InvalidOperationException(
                        "PostureBreak static leg pose requires one quaternion at " + path +
                        ", found " + rotationBindings + " bindings.");
            }

            float maximumCurveDrift = 0f;
            foreach (EditorCurveBinding binding in legBindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                    throw new InvalidOperationException("PostureBreak static leg curve is missing.");
                float reference = curve.Evaluate(0f);
                for (int index = 1; index <= 16; index++)
                {
                    float value = curve.Evaluate(clip.length * index / 16f);
                    maximumCurveDrift = Mathf.Max(maximumCurveDrift, Mathf.Abs(value - reference));
                }
            }
            if (maximumCurveDrift > 0.00001f)
                throw new InvalidOperationException(
                    "PostureBreak legs move away from their first pose. curveDrift=" + F(maximumCurveDrift));

            float maximumRotationDrift = 0f;
            foreach (string path in StaticLegPaths)
            {
                Quaternion reference = ReadQuaternion(clip, path, 0f);
                for (int index = 1; index <= 16; index++)
                {
                    Quaternion sample = ReadQuaternion(clip, path, clip.length * index / 16f);
                    maximumRotationDrift = Mathf.Max(
                        maximumRotationDrift,
                        Quaternion.Angle(reference, sample));
                }
            }
            if (maximumRotationDrift > 0.01f)
                throw new InvalidOperationException(
                    "PostureBreak leg rotation drift exceeds tolerance. degrees=" + F(maximumRotationDrift));
            return maximumRotationDrift;
        }

        private static bool IsFullLegBinding(EditorCurveBinding binding)
        {
            return StaticLegPaths.Any(path =>
                binding.path == path ||
                binding.path.StartsWith(path + "/", StringComparison.Ordinal));
        }

        private static IReadOnlyDictionary<string, LegLocalPose> CaptureFirstLegPose(
            GameObject actorTemplate,
            AnimationClip baseClip)
        {
            GameObject preview = CloneForPreview(actorTemplate, "PostureBreak_FirstLegPosePreview");
            try
            {
                baseClip.SampleAnimation(preview, 0f);
                return StaticLegPaths.ToDictionary(
                    path => path,
                    path => LegLocalPose.Capture(FindPath(preview.transform, path)),
                    StringComparer.Ordinal);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static KneeTwitchSolution SolveKneeTwitchDeltas(
            GameObject actorTemplate,
            AnimationClip baseClip)
        {
            GameObject preview = CloneForPreview(actorTemplate, "PostureBreak_KneeTwitchDeltaPreview");
            try
            {
                baseClip.SampleAnimation(preview, 0f);
                Transform hips = FindPath(preview.transform, CarrierHipsPath);
                Quaternion left = SolveSingleKneeTwitchDelta(
                    hips,
                    FindPath(preview.transform, LeftUpperLegPath),
                    FindPath(preview.transform, LeftLowerLegPath));
                Quaternion right = SolveSingleKneeTwitchDelta(
                    hips,
                    FindPath(preview.transform, RightUpperLegPath),
                    FindPath(preview.transform, RightLowerLegPath));
                return new KneeTwitchSolution(left, right);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static Quaternion SolveSingleKneeTwitchDelta(
            Transform hips,
            Transform upperLeg,
            Transform lowerLeg)
        {
            Vector3 thighDirection = (lowerLeg.position - upperLeg.position).normalized;
            float kneeSide = Mathf.Sign(hips.InverseTransformPoint(lowerLeg.position).x);
            if (thighDirection.sqrMagnitude < 0.999f || Mathf.Approximately(kneeSide, 0f))
                throw new InvalidOperationException("PostureBreak knee outward direction is unstable.");
            Vector3 outward = hips.TransformDirection(Vector3.right * kneeSide);
            Vector3 outwardTangent = Vector3.ProjectOnPlane(outward, thighDirection).normalized;
            if (outwardTangent.sqrMagnitude < 0.999f)
                throw new InvalidOperationException("PostureBreak knee outward tangent is unstable.");
            Vector3 desiredDirection = Vector3.RotateTowards(
                thighDirection,
                outwardTangent,
                KneeTwitchAngle * Mathf.Deg2Rad,
                0f).normalized;
            Quaternion worldDelta = Quaternion.FromToRotation(thighDirection, desiredDirection);
            Quaternion desiredWorld = worldDelta * upperLeg.rotation;
            Quaternion desiredLocal = upperLeg.parent == null
                ? desiredWorld
                : Quaternion.Inverse(upperLeg.parent.rotation) * desiredWorld;
            Quaternion localDelta = Quaternion.Inverse(upperLeg.localRotation) * desiredLocal;
            localDelta.Normalize();
            float angle = Quaternion.Angle(Quaternion.identity, localDelta);
            if (Mathf.Abs(angle - KneeTwitchAngle) > 0.05f)
                throw new InvalidOperationException(
                    "PostureBreak knee twitch solver produced " + F(angle) + " degrees.");

            float before = Mathf.Abs(hips.InverseTransformPoint(lowerLeg.position).x);
            Quaternion original = upperLeg.localRotation;
            upperLeg.localRotation = original * localDelta;
            float after = Mathf.Abs(hips.InverseTransformPoint(lowerLeg.position).x);
            upperLeg.localRotation = original;
            if (after <= before)
                throw new InvalidOperationException(
                    "PostureBreak knee twitch does not move outward. before=" + F(before) + " after=" + F(after));
            return localDelta;
        }

        private static Quaternion SolveUpperBodyTwitchDelta(
            GameObject actorTemplate,
            AnimationClip baseClip)
        {
            GameObject preview = CloneForPreview(actorTemplate, "PostureBreak_UpperBodyTwitchDeltaPreview");
            try
            {
                baseClip.SampleAnimation(preview, 0f);
                Transform lowerChest = FindPath(preview.transform, CarrierLowerChestPath);
                Transform head = FindPath(preview.transform, CarrierHeadPath);
                Vector3 torso = head.position - lowerChest.position;
                if (torso.sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException("PostureBreak torso direction is unstable.");
                Vector3 liftedDirection = Vector3.RotateTowards(
                    torso.normalized,
                    Vector3.up,
                    UpperBodyTwitchAngle * Mathf.Deg2Rad,
                    0f);
                Quaternion worldDelta = Quaternion.FromToRotation(
                    torso.normalized,
                    liftedDirection);
                Quaternion desiredWorld = worldDelta * lowerChest.rotation;
                Quaternion desiredLocal = lowerChest.parent == null
                    ? desiredWorld
                    : Quaternion.Inverse(lowerChest.parent.rotation) * desiredWorld;
                Quaternion localDelta = Quaternion.Inverse(lowerChest.localRotation) * desiredLocal;
                localDelta.Normalize();
                float angle = Quaternion.Angle(Quaternion.identity, localDelta);
                if (Mathf.Abs(angle - UpperBodyTwitchAngle) > 0.05f)
                    throw new InvalidOperationException(
                        "PostureBreak upper-body twitch solver produced " + F(angle) + " degrees.");
                return localDelta;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static Texture2D RenderUpperBodyTwitchPreview(
            GameObject preview,
            AnimationClip baseClip,
            float time,
            IReadOnlyDictionary<string, LegLocalPose> firstLegPose,
            Quaternion upperBodyDelta,
            KneeTwitchSolution kneeTwitch,
            PreviewView view)
        {
            baseClip.SampleAnimation(preview, Mathf.Clamp(time, 0f, baseClip.length));
            foreach (KeyValuePair<string, LegLocalPose> item in firstLegPose)
                item.Value.Apply(FindPath(preview.transform, item.Key));
            Transform lowerChest = FindPath(preview.transform, CarrierLowerChestPath);
            lowerChest.localRotation = lowerChest.localRotation * upperBodyDelta;
            Transform leftUpperLeg = FindPath(preview.transform, LeftUpperLegPath);
            Transform rightUpperLeg = FindPath(preview.transform, RightUpperLegPath);
            leftUpperLeg.localRotation = leftUpperLeg.localRotation * kneeTwitch.LeftDelta;
            rightUpperLeg.localRotation = rightUpperLeg.localRotation * kneeTwitch.RightDelta;
            var empty = new AnimationClip { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                return RenderSample(preview, empty, 0f, view);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(empty);
            }
        }

        private static AnimationClip CreateUpperBodyTwitchClip(
            GameObject actorTemplate,
            AnimationClip baseClip,
            Quaternion twitchDelta,
            KneeTwitchSolution kneeTwitch,
            float attackCycleDuration)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UpperBodyTwitchClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "PostureBreak_UpperBodyTwitch", frameRate = 30f };
                AssetDatabase.CreateAsset(clip, UpperBodyTwitchClipPath);
            }
            else
            {
                clip.ClearCurves();
                clip.name = "PostureBreak_UpperBodyTwitch";
                clip.frameRate = 30f;
            }

            GameObject preview = CloneForPreview(actorTemplate, "PostureBreak_UpperBodyTwitchReferencePreview");
            try
            {
                baseClip.SampleAnimation(preview, 0f);
                Quaternion reference = FindPath(preview.transform, CarrierLowerChestPath).localRotation;
                Quaternion peak = reference * twitchDelta;
                SetAttackSynchronizedTwitchCurves(
                    clip, CarrierLowerChestPath, reference, peak, attackCycleDuration);
                Quaternion leftReference = FindPath(preview.transform, LeftUpperLegPath).localRotation;
                Quaternion rightReference = FindPath(preview.transform, RightUpperLegPath).localRotation;
                SetAttackSynchronizedTwitchCurves(
                    clip,
                    LeftUpperLegPath,
                    leftReference,
                    leftReference * kneeTwitch.LeftDelta,
                    attackCycleDuration);
                SetAttackSynchronizedTwitchCurves(
                    clip,
                    RightUpperLegPath,
                    rightReference,
                    rightReference * kneeTwitch.RightDelta,
                    attackCycleDuration);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.wrapMode = WrapMode.Loop;
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AvatarMask CreateUpperBodyTwitchMask(Transform target)
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyTwitchMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask { name = "PostureBreak_UpperBodyTwitch" };
                AssetDatabase.CreateAsset(mask, UpperBodyTwitchMaskPath);
            }
            string[] paths = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target)
                .Select(item => AnimationUtility.CalculateTransformPath(item, target))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path.Count(character => character == '/'))
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();
            mask.transformCount = paths.Length;
            for (int index = 0; index < paths.Length; index++)
            {
                mask.SetTransformPath(index, paths[index]);
                mask.SetTransformActive(index,
                    string.Equals(paths[index], CarrierLowerChestPath, StringComparison.Ordinal) ||
                    string.Equals(paths[index], LeftUpperLegPath, StringComparison.Ordinal) ||
                    string.Equals(paths[index], RightUpperLegPath, StringComparison.Ordinal));
            }
            for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static AnimatorController ConfigureUpperBodyTwitchLayer(
            AnimationClip baseClip,
            AnimationClip twitchClip,
            AvatarMask twitchMask)
        {
            AnimatorController controller = UpdateLoopControllerMotion(baseClip);
            if (controller.layers.Length == 1)
                controller.AddLayer(UpperBodyTwitchLayerName);
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length != 2)
                throw new InvalidOperationException("PostureBreak controller could not create exactly one twitch layer.");
            AnimatorControllerLayer layer = layers[1];
            layer.name = UpperBodyTwitchLayerName;
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Additive;
            layer.avatarMask = twitchMask;
            AnimatorStateMachine stateMachine = layer.stateMachine;
            ChildAnimatorState[] states = stateMachine.states;
            AnimatorState state;
            if (states.Length == 0)
                state = stateMachine.AddState(UpperBodyTwitchStateName);
            else if (states.Length == 1)
                state = states[0].state;
            else
                throw new InvalidOperationException("PostureBreak twitch layer must contain exactly one state.");
            state.name = UpperBodyTwitchStateName;
            state.motion = twitchClip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            stateMachine.defaultState = state;
            layers[1] = layer;
            controller.layers = layers;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void SetAttackSynchronizedTwitchCurves(
            AnimationClip clip,
            string path,
            Quaternion reference,
            Quaternion peak,
            float attackCycleDuration)
        {
            if (attackCycleDuration <= UpperBodyTwitchAttackStartTime + UpperBodyTwitchReturnTime)
                throw new InvalidOperationException("Tergo attack cycle is too short for the synchronized twitch.");
            var times = new List<float> { 0f, UpperBodyTwitchAttackStartTime };
            var rotations = new List<Quaternion> { reference, reference };
            for (float onset = UpperBodyTwitchAttackStartTime;
                 onset + UpperBodyTwitchReturnTime <= attackCycleDuration + 0.0001f;
                 onset += UpperBodyTwitchPeriod)
            {
                if (times[times.Count - 1] < onset - 0.0001f)
                {
                    times.Add(onset);
                    rotations.Add(reference);
                }
                times.Add(onset + UpperBodyTwitchPeakTime);
                rotations.Add(peak);
                times.Add(onset + UpperBodyTwitchReturnTime);
                rotations.Add(reference);
            }
            if (times[times.Count - 1] < attackCycleDuration - 0.0001f)
            {
                times.Add(attackCycleDuration);
                rotations.Add(reference);
            }
            SetQuaternionCurves(clip, path, times, rotations);
        }

        private static int CountAttackSynchronizedPulses(float attackCycleDuration)
        {
            int count = 0;
            for (float onset = UpperBodyTwitchAttackStartTime;
                 onset + UpperBodyTwitchReturnTime <= attackCycleDuration + 0.0001f;
                 onset += UpperBodyTwitchPeriod)
                count++;
            return count;
        }

        private static TwitchMetrics RequireUpperBodyTwitchClip(
            AnimationClip clip,
            float tergoAttackCycleDuration)
        {
            if (Mathf.Abs(clip.length - tergoAttackCycleDuration) > 0.0001f)
                throw new InvalidOperationException(
                    "PostureBreak twitch clip must match the Tergo attack cycle. twitch=" + F(clip.length) +
                    " tergo=" + F(tergoAttackCycleDuration));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("PostureBreak twitch clip must loop every second.");
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            string[] expectedPaths = { CarrierLowerChestPath, LeftUpperLegPath, RightUpperLegPath };
            if (bindings.Length != 12 || bindings.Any(binding =>
                    !expectedPaths.Contains(binding.path, StringComparer.Ordinal) ||
                    !binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "PostureBreak twitch clip must animate only lower chest and both upper-leg quaternion rotations.");
            float upperBodyAngle = RequireAttackSynchronizedTwitchPath(
                clip, CarrierLowerChestPath, UpperBodyTwitchAngle);
            float leftKneeAngle = RequireAttackSynchronizedTwitchPath(
                clip, LeftUpperLegPath, KneeTwitchAngle);
            float rightKneeAngle = RequireAttackSynchronizedTwitchPath(
                clip, RightUpperLegPath, KneeTwitchAngle);
            return new TwitchMetrics(upperBodyAngle, leftKneeAngle, rightKneeAngle);
        }

        private static float RequireAttackSynchronizedTwitchPath(
            AnimationClip clip,
            string path,
            float expectedAngle)
        {
            Quaternion rest = ReadQuaternion(clip, path, 0f);
            Quaternion beforeAttack = ReadQuaternion(
                clip, path, UpperBodyTwitchAttackStartTime - (1f / 60f));
            if (Quaternion.Angle(rest, beforeAttack) > 0.01f)
                throw new InvalidOperationException(
                    "PostureBreak twitch moves before the Tergo chest attack at " + path + ".");
            float maximumAngle = 0f;
            int pulses = 0;
            for (float onset = UpperBodyTwitchAttackStartTime;
                 onset + UpperBodyTwitchReturnTime <= clip.length + 0.0001f;
                 onset += UpperBodyTwitchPeriod)
            {
                Quaternion onsetPose = ReadQuaternion(clip, path, onset);
                Quaternion peak = ReadQuaternion(clip, path, onset + UpperBodyTwitchPeakTime);
                Quaternion returned = ReadQuaternion(clip, path, onset + UpperBodyTwitchReturnTime);
                float angle = Quaternion.Angle(rest, peak);
                float onsetError = Quaternion.Angle(rest, onsetPose);
                float returnError = Quaternion.Angle(rest, returned);
                if (Mathf.Abs(angle - expectedAngle) > 0.1f || onsetError > 0.01f || returnError > 0.01f)
                    throw new InvalidOperationException(
                        "PostureBreak attack-synchronized twitch differs at " + path +
                        " onset=" + F(onset) + " angle=" + F(angle) +
                        " onsetError=" + F(onsetError) + " returnError=" + F(returnError));
                maximumAngle = Mathf.Max(maximumAngle, angle);
                pulses++;
            }
            if (pulses != CountAttackSynchronizedPulses(clip.length))
                throw new InvalidOperationException("PostureBreak twitch pulse count differs at " + path + ".");
            return maximumAngle;
        }

        private static Quaternion ReadQuaternion(AnimationClip clip, string path, float time)
        {
            float Read(string property)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), property));
                if (curve == null)
                    throw new InvalidOperationException("PostureBreak quaternion curve is missing: " + property);
                return curve.Evaluate(time);
            }
            Quaternion value = new Quaternion(
                Read("m_LocalRotation.x"),
                Read("m_LocalRotation.y"),
                Read("m_LocalRotation.z"),
                Read("m_LocalRotation.w"));
            value.Normalize();
            return value;
        }

        private static void RequireUpperBodyTwitchMask(Transform target, AvatarMask mask)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < mask.transformCount; index++)
            {
                string path = mask.GetTransformPath(index);
                seen.Add(path);
                bool expected = string.Equals(path, CarrierLowerChestPath, StringComparison.Ordinal) ||
                    string.Equals(path, LeftUpperLegPath, StringComparison.Ordinal) ||
                    string.Equals(path, RightUpperLegPath, StringComparison.Ordinal);
                if (mask.GetTransformActive(index) != expected)
                    throw new InvalidOperationException("PostureBreak twitch mask differs at " + path + ".");
            }
            if (!seen.Contains(CarrierLowerChestPath) ||
                !seen.Contains(LeftUpperLegPath) ||
                !seen.Contains(RightUpperLegPath))
                throw new InvalidOperationException(
                    "PostureBreak twitch mask is missing lower chest or upper-leg paths.");
            for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
                if (mask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)index))
                    throw new InvalidOperationException("PostureBreak twitch mask must use transform masking only.");
        }

        private static void RequireUpperBodyTwitchLayer(
            AnimatorController controller,
            AnimationClip baseClip,
            AnimationClip twitchClip,
            AvatarMask twitchMask)
        {
            RequireLoopController(controller, baseClip);
            if (controller.layers.Length != 2)
                throw new InvalidOperationException("PostureBreak controller must have base and twitch layers.");
            AnimatorControllerLayer layer = controller.layers[1];
            if (layer.name != UpperBodyTwitchLayerName ||
                layer.blendingMode != AnimatorLayerBlendingMode.Additive ||
                !Mathf.Approximately(layer.defaultWeight, 1f) ||
                layer.avatarMask != twitchMask)
                throw new InvalidOperationException("PostureBreak upper-body twitch layer configuration differs.");
            ChildAnimatorState[] states = layer.stateMachine.states;
            if (states.Length != 1 || states[0].state.name != UpperBodyTwitchStateName ||
                states[0].state.motion != twitchClip || !Mathf.Approximately(states[0].state.speed, 1f))
                throw new InvalidOperationException("PostureBreak upper-body twitch state differs.");
        }

        private static int EvaluateAnimatorLayerRepeats(
            GameObject source,
            int layerIndex,
            string stateName,
            float duration)
        {
            GameObject probe = CloneForPreview(source, source.name + "_TwitchRuntimeProbe");
            try
            {
                Animator animator = probe.GetComponent<Animator>() ??
                    throw new InvalidOperationException(source.name + " twitch probe has no Animator.");
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                float previous = 0f;
                int repeats = 0;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    float delta = Mathf.Min(1f / 120f, duration - elapsed);
                    animator.Update(delta);
                    elapsed += delta;
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layerIndex);
                    float phase = info.normalizedTime - Mathf.Floor(info.normalizedTime);
                    if (phase + 0.45f < previous)
                        repeats++;
                    previous = phase;
                }
                AnimatorStateInfo finalInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
                if (finalInfo.shortNameHash != Animator.StringToHash(stateName))
                    throw new InvalidOperationException("PostureBreak twitch runtime state is not active.");
                return repeats;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        private static void ApplyLegClosureCurves(
            AnimationClip sourceClip,
            AnimationClip destinationClip,
            GameObject actorTemplate)
        {
            string[] rotationPaths =
            {
                LeftUpperLegPath,
                LeftLowerLegPath,
                RightUpperLegPath,
                RightLowerLegPath
            };
            var samples = rotationPaths.ToDictionary(
                path => path,
                path => new List<Quaternion>());
            var times = new List<float>();
            GameObject preview = CloneForPreview(actorTemplate, "PostureBreak_LegClosureCurvePreview");
            try
            {
                int frameCount = Mathf.Max(1, Mathf.RoundToInt(sourceClip.length * sourceClip.frameRate));
                for (int frame = 0; frame <= frameCount; frame++)
                {
                    float time = sourceClip.length * frame / frameCount;
                    sourceClip.SampleAnimation(preview, time);
                    Transform hips = FindPath(preview.transform, CarrierHipsPath);
                    CloseLegPose(
                        hips,
                        FindPath(preview.transform, LeftUpperLegPath),
                        FindPath(preview.transform, LeftLowerLegPath),
                        FindPath(preview.transform, LeftFootPath));
                    CloseLegPose(
                        hips,
                        FindPath(preview.transform, RightUpperLegPath),
                        FindPath(preview.transform, RightLowerLegPath),
                        FindPath(preview.transform, RightFootPath));
                    times.Add(time);
                    foreach (string path in rotationPaths)
                        samples[path].Add(FindPath(preview.transform, path).localRotation);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }

            foreach (string path in rotationPaths)
                SetQuaternionCurves(destinationClip, path, times, samples[path]);
            destinationClip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(destinationClip);
        }

        private static void CloseLegPose(
            Transform hips,
            Transform upperLeg,
            Transform lowerLeg,
            Transform foot)
        {
            Vector3 upperRoot = hips.InverseTransformPoint(upperLeg.position);
            Vector3 knee = hips.InverseTransformPoint(lowerLeg.position);
            Vector3 ankle = hips.InverseTransformPoint(foot.position);
            Vector3 desiredKnee = knee;
            Vector3 desiredAnkle = ankle;
            desiredKnee.x *= LegLateralRemainingRatio;
            desiredAnkle.x *= LegLateralRemainingRatio;

            RotateSegmentToward(
                upperLeg,
                lowerLeg,
                hips.TransformDirection(PreserveLengthWithTargetX(
                    knee - upperRoot,
                    desiredKnee.x - upperRoot.x)));
            RotateSegmentToward(
                lowerLeg,
                foot,
                hips.TransformDirection(PreserveLengthWithTargetX(
                    ankle - knee,
                    desiredAnkle.x - desiredKnee.x)));
        }

        private static void RotateSegmentToward(
            Transform segment,
            Transform child,
            Vector3 desiredWorldVector)
        {
            Vector3 currentWorldVector = child.position - segment.position;
            if (currentWorldVector.sqrMagnitude < 0.0000001f || desiredWorldVector.sqrMagnitude < 0.0000001f)
                throw new InvalidOperationException("PostureBreak leg segment has no stable direction.");
            Quaternion delta = Quaternion.FromToRotation(
                currentWorldVector.normalized,
                desiredWorldVector.normalized);
            segment.rotation = delta * segment.rotation;
        }

        private static Vector3 PreserveLengthWithTargetX(Vector3 source, float targetX)
        {
            float length = source.magnitude;
            if (length < 0.00001f)
                throw new InvalidOperationException("PostureBreak leg segment has zero length.");
            float limitedX = Mathf.Clamp(targetX, -length * 0.9999f, length * 0.9999f);
            float sourceYz = Mathf.Sqrt(source.y * source.y + source.z * source.z);
            float targetYz = Mathf.Sqrt(Mathf.Max(0f, length * length - limitedX * limitedX));
            if (sourceYz < 0.00001f)
                return new Vector3(limitedX, targetYz, 0f);
            float yzScale = targetYz / sourceYz;
            return new Vector3(limitedX, source.y * yzScale, source.z * yzScale);
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations)
        {
            if (times.Count != rotations.Count || times.Count < 2)
                throw new InvalidOperationException("PostureBreak leg rotation samples are incomplete.");
            var continuous = new Quaternion[rotations.Count];
            for (int index = 0; index < rotations.Count; index++)
            {
                Quaternion value = rotations[index];
                if (index > 0 && Quaternion.Dot(continuous[index - 1], value) < 0f)
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                continuous[index] = value;
            }

            SetLinearCurve(clip, path, "m_LocalRotation.x", times, continuous.Select(item => item.x).ToArray());
            SetLinearCurve(clip, path, "m_LocalRotation.y", times, continuous.Select(item => item.y).ToArray());
            SetLinearCurve(clip, path, "m_LocalRotation.z", times, continuous.Select(item => item.z).ToArray());
            SetLinearCurve(clip, path, "m_LocalRotation.w", times, continuous.Select(item => item.w).ToArray());
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyList<float> times,
            IReadOnlyList<float> values)
        {
            var keys = new Keyframe[times.Count];
            for (int index = 0; index < keys.Length; index++)
                keys[index] = new Keyframe(times[index], values[index]);
            var curve = new AnimationCurve(keys);
            for (int index = 0; index < keys.Length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static LegSpreadMetrics EvaluateLegSpread(GameObject actorTemplate, AnimationClip clip)
        {
            GameObject preview = CloneForPreview(actorTemplate, "PostureBreak_LegSpreadMetricsPreview");
            float kneeSum = 0f;
            float ankleSum = 0f;
            float toeSum = 0f;
            float minimumKnee = float.PositiveInfinity;
            float minimumAnkle = float.PositiveInfinity;
            float minimumToe = float.PositiveInfinity;
            const int sampleCount = 17;
            try
            {
                for (int index = 0; index < sampleCount; index++)
                {
                    float phase = index / (float)(sampleCount - 1);
                    clip.SampleAnimation(preview, clip.length * phase);
                    LegSpreadSample sample = MeasureCurrentLegSpread(preview);
                    kneeSum += sample.Knee;
                    ankleSum += sample.Ankle;
                    toeSum += sample.Toe;
                    minimumKnee = Mathf.Min(minimumKnee, sample.Knee);
                    minimumAnkle = Mathf.Min(minimumAnkle, sample.Ankle);
                    minimumToe = Mathf.Min(minimumToe, sample.Toe);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
            return new LegSpreadMetrics(
                kneeSum / sampleCount,
                ankleSum / sampleCount,
                toeSum / sampleCount,
                minimumKnee,
                minimumAnkle,
                minimumToe,
                sampleCount);
        }

        private static LegSpreadSample MeasureCurrentLegSpread(GameObject actor)
        {
            Transform hips = FindPath(actor.transform, CarrierHipsPath);
            float leftKnee = hips.InverseTransformPoint(FindPath(actor.transform, LeftLowerLegPath).position).x;
            float rightKnee = hips.InverseTransformPoint(FindPath(actor.transform, RightLowerLegPath).position).x;
            float leftAnkle = hips.InverseTransformPoint(FindPath(actor.transform, LeftFootPath).position).x;
            float rightAnkle = hips.InverseTransformPoint(FindPath(actor.transform, RightFootPath).position).x;
            float leftToe = hips.InverseTransformPoint(FindPath(actor.transform, LeftToePath).position).x;
            float rightToe = hips.InverseTransformPoint(FindPath(actor.transform, RightToePath).position).x;
            return new LegSpreadSample(
                Mathf.Abs(leftKnee - rightKnee),
                Mathf.Abs(leftAnkle - rightAnkle),
                Mathf.Abs(leftToe - rightToe));
        }

        private static void RequireLegClosureRatio(
            LegSpreadMetrics source,
            LegSpreadMetrics adjusted)
        {
            float kneeRatio = adjusted.AverageKnee / source.AverageKnee;
            float ankleRatio = adjusted.AverageAnkle / source.AverageAnkle;
            float toeRatio = adjusted.AverageToe / source.AverageToe;
            if (kneeRatio < 0.55f || kneeRatio > 0.65f ||
                ankleRatio < 0.55f || ankleRatio > 0.65f ||
                toeRatio < 0.50f || toeRatio > 0.75f)
                throw new InvalidOperationException(
                    "PostureBreak leg closure is not within the requested forty-percent range. " +
                    adjusted.DescribeClosureFrom(source));
            if (adjusted.MinimumKnee < 0.03f || adjusted.MinimumAnkle < 0.03f || adjusted.MinimumToe < 0.03f)
                throw new InvalidOperationException(
                    "PostureBreak legs cross the center line after closure. " + adjusted.Describe());
        }

        private static void RequireOnlyLegRotationCurvesDiffer(
            AnimationClip source,
            AnimationClip adjusted)
        {
            if (!string.Equals(
                    ClipContentSignatureExcludingLegRotations(source),
                    ClipContentSignatureExcludingLegRotations(adjusted),
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "PostureBreak root, upper body, settings, events, or non-leg curves changed.");
            EditorCurveBinding[] adjustedLegBindings = AnimationUtility.GetCurveBindings(adjusted)
                .Where(IsLegRotationBinding)
                .ToArray();
            if (adjustedLegBindings.Length != 16)
                throw new InvalidOperationException(
                    "PostureBreak must modify exactly four leg quaternion bindings. count=" +
                    adjustedLegBindings.Length);
        }

        private static bool IsLegRotationBinding(EditorCurveBinding binding)
        {
            if (!binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal))
                return false;
            return binding.path == LeftUpperLegPath ||
                binding.path == LeftLowerLegPath ||
                binding.path == RightUpperLegPath ||
                binding.path == RightLowerLegPath;
        }

        private static string ClipContentSignatureExcludingLegRotations(AnimationClip clip) =>
            ClipContentSignatureExcludingBindings(clip, IsLegRotationBinding);

        private static string ClipContentSignatureExcludingFullLegCurves(AnimationClip clip) =>
            ClipContentSignatureExcludingBindings(clip, IsFullLegBinding);

        private static string ClipContentSignatureExcludingBindings(
            AnimationClip clip,
            Func<EditorCurveBinding, bool> exclude)
        {
            var builder = new StringBuilder()
                .Append("length=").Append(RF(clip.length))
                .Append("|rate=").Append(RF(clip.frameRate))
                .Append("|wrap=").Append((int)clip.wrapMode)
                .Append("|legacy=").Append(clip.legacy)
                .Append("|human=").Append(clip.humanMotion);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            builder.Append("|loop=").Append(settings.loopTime)
                .Append("|loopBlend=").Append(settings.loopBlend)
                .Append("|loopOrientation=").Append(settings.loopBlendOrientation)
                .Append("|loopY=").Append(settings.loopBlendPositionY)
                .Append("|loopXZ=").Append(settings.loopBlendPositionXZ)
                .Append("|keepOrientation=").Append(settings.keepOriginalOrientation)
                .Append("|keepY=").Append(settings.keepOriginalPositionY)
                .Append("|keepXZ=").Append(settings.keepOriginalPositionXZ)
                .Append("|feet=").Append(settings.heightFromFeet)
                .Append("|mirror=").Append(settings.mirror)
                .Append("|start=").Append(RF(settings.startTime))
                .Append("|stop=").Append(RF(settings.stopTime))
                .Append("|cycle=").Append(RF(settings.cycleOffset))
                .Append("|level=").Append(RF(settings.level));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .Where(item => !exclude(item))
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal))
            {
                builder.Append("|curve:").Append(binding.path).Append('|')
                    .Append(binding.type.AssemblyQualifiedName).Append('|').Append(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                    builder.Append('[').Append(RF(key.time)).Append(',').Append(RF(key.value))
                        .Append(',').Append(RF(key.inTangent)).Append(',').Append(RF(key.outTangent))
                        .Append(',').Append(RF(key.inWeight)).Append(',').Append(RF(key.outWeight))
                        .Append(',').Append((int)key.weightedMode).Append(']');
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                builder.Append("|object:").Append(binding.path).Append('|').Append(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                    builder.Append('[').Append(RF(key.time)).Append(',')
                        .Append(key.value == null ? "null" : AssetDatabase.GetAssetPath(key.value))
                        .Append(',').Append(key.value == null ? "null" : key.value.name).Append(']');
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                builder.Append("|event:").Append(RF(item.time)).Append('|').Append(item.functionName)
                    .Append('|').Append(item.stringParameter).Append('|').Append(item.intParameter)
                    .Append('|').Append(RF(item.floatParameter));
            return builder.ToString();
        }

        private static AnimatorController CreateLoopController(AnimationClip copiedClip)
        {
            if (AssetDatabase.LoadMainAssetAtPath(ControllerPath) != null &&
                !AssetDatabase.DeleteAsset(ControllerPath))
                throw new InvalidOperationException("Failed to replace target-only controller: " + ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState state = stateMachine.AddState(CopiedStateName);
            state.motion = copiedClip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            stateMachine.defaultState = state;
            AnimatorStateTransition transition = state.AddTransition(state);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.canTransitionToSelf = true;
            transition.interruptionSource = TransitionInterruptionSource.None;
            EditorUtility.SetDirty(transition);
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RequireLoopController(AnimatorController controller, AnimationClip clip)
        {
            if (controller == null || controller.layers.Length < 1 || controller.layers.Length > 2)
                throw new InvalidOperationException("PostureBreak must use one base layer and at most one twitch layer.");
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ChildAnimatorState[] states = stateMachine.states;
            if (states.Length != 1)
                throw new InvalidOperationException("PostureBreak controller must have exactly one state.");
            AnimatorState state = states[0].state;
            if (!string.Equals(state.name, CopiedStateName, StringComparison.Ordinal) ||
                state.motion != clip || Mathf.Abs(state.speed - 1f) > 0.0001f)
                throw new InvalidOperationException("PostureBreak controller state differs from the copied source animation.");
            AnimatorStateTransition[] transitions = state.transitions;
            if (transitions.Length != 1 || transitions[0].destinationState != state ||
                !transitions[0].hasExitTime || Mathf.Abs(transitions[0].exitTime - 1f) > 0.0001f ||
                Mathf.Abs(transitions[0].duration) > 0.0001f || !transitions[0].canTransitionToSelf)
                throw new InvalidOperationException("PostureBreak exact clip repeat transition differs.");
        }

        private static AnimatorPlaybackMetrics EvaluateAnimatorPlayback(
            GameObject source, string stateName, AnimationClip clip, bool sourceClipLoops)
        {
            GameObject probe = CloneForPreview(source, source.name + "_RuntimeProbe");
            try
            {
                Animator animator = probe.GetComponent<Animator>() ??
                    throw new InvalidOperationException(source.name + " runtime probe has no root Animator.");
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                int stateHash = Animator.StringToHash(stateName);
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                Transform[] transforms = probe.GetComponentsInChildren<Transform>(true);
                LocalTransformState[] start = transforms.Select(LocalTransformState.Capture).ToArray();
                animator.Update(Mathf.Max(0.02f, clip.length * 0.5f));
                LocalTransformState[] middle = transforms.Select(LocalTransformState.Capture).ToArray();
                float maximumRotation = 0f;
                float maximumPosition = 0f;
                for (int index = 0; index < transforms.Length; index++)
                {
                    maximumRotation = Mathf.Max(maximumRotation,
                        Quaternion.Angle(start[index].Rotation, middle[index].Rotation));
                    maximumPosition = Mathf.Max(maximumPosition,
                        Vector3.Distance(start[index].Position, middle[index].Position));
                }

                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                float elapsed = 0f;
                float previousFraction = 0f;
                int repeatCount = 0;
                float duration = Mathf.Max(clip.length, 0.1f);
                float step = Mathf.Min(1f / 60f, duration / 120f);
                float evaluationDuration = duration * 2.25f;
                while (elapsed < evaluationDuration)
                {
                    float delta = Mathf.Min(step, evaluationDuration - elapsed);
                    animator.Update(delta);
                    elapsed += delta;
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    float fraction = info.normalizedTime - Mathf.Floor(info.normalizedTime);
                    if (fraction + 0.45f < previousFraction)
                        repeatCount++;
                    previousFraction = fraction;
                }
                AnimatorStateInfo finalInfo = animator.GetCurrentAnimatorStateInfo(0);
                bool sameState = finalInfo.shortNameHash == stateHash;
                bool motion = maximumRotation > 0.01f || maximumPosition > 0.0001f;
                bool repeated = sameState && repeatCount >= 2;
                return new AnimatorPlaybackMetrics(
                    sourceClipLoops, motion, repeated, repeatCount,
                    maximumRotation, maximumPosition,
                    finalInfo.normalizedTime - Mathf.Floor(finalInfo.normalizedTime));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
        }

        private static Texture2D RenderPair(
            GameObject target, AnimationClip targetClip, float targetTime,
            GameObject tergo, AnimationClip tergoClip, float tergoTime)
        {
            return RenderPair(
                target, targetClip, targetTime,
                tergo, tergoClip, tergoTime,
                PreviewView.Front);
        }

        private static Texture2D RenderPair(
            GameObject target, AnimationClip targetClip, float targetTime,
            GameObject tergo, AnimationClip tergoClip, float tergoTime,
            PreviewView view)
        {
            targetClip.SampleAnimation(target, Mathf.Clamp(targetTime, 0f, targetClip.length));
            tergoClip.SampleAnimation(tergo, Mathf.Clamp(tergoTime, 0f, tergoClip.length));
            var group = new GameObject("PostureBreak_FinalPairPreview");
            var empty = new AnimationClip();
            group.hideFlags = HideFlags.HideAndDontSave;
            empty.hideFlags = HideFlags.HideAndDontSave;
            group.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            target.transform.SetParent(group.transform, true);
            tergo.transform.SetParent(group.transform, true);
            SetLayerRecursively(group, PreviewLayer);
            try
            {
                return RenderSample(group, empty, 0f, view);
            }
            finally
            {
                target.transform.SetParent(null, true);
                tergo.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(empty);
                UnityEngine.Object.DestroyImmediate(group);
            }
        }

        private static Texture2D RenderPairFocused(
            GameObject target, AnimationClip targetClip, float targetTime,
            GameObject tergo, AnimationClip tergoClip, float tergoTime,
            PreviewView view,
            float framingScale = 0.62f)
        {
            targetClip.SampleAnimation(target, Mathf.Clamp(targetTime, 0f, targetClip.length));
            tergoClip.SampleAnimation(tergo, Mathf.Clamp(tergoTime, 0f, tergoClip.length));
            Vector3 carrierHips = FindPath(target.transform, CarrierHipsPath).position;
            Vector3 carrierUpperChest = FindPath(target.transform, CarrierUpperChestPath).position;
            Vector3 focus = Vector3.Lerp(carrierHips, carrierUpperChest, 0.55f) + Vector3.up * 0.16f;
            var group = new GameObject("PostureBreak_FocusedPairPreview");
            var empty = new AnimationClip();
            group.hideFlags = HideFlags.HideAndDontSave;
            empty.hideFlags = HideFlags.HideAndDontSave;
            group.transform.SetPositionAndRotation(Vector3.zero, target.transform.rotation);
            target.transform.SetParent(group.transform, true);
            tergo.transform.SetParent(group.transform, true);
            SetLayerRecursively(group, PreviewLayer);
            try
            {
                return RenderSample(group, empty, 0f, view, focus, framingScale);
            }
            finally
            {
                target.transform.SetParent(null, true);
                tergo.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(empty);
                UnityEngine.Object.DestroyImmediate(group);
            }
        }

        private static string ClipContentSignature(AnimationClip clip)
        {
            var builder = new StringBuilder()
                .Append("length=").Append(RF(clip.length))
                .Append("|rate=").Append(RF(clip.frameRate))
                .Append("|wrap=").Append((int)clip.wrapMode)
                .Append("|legacy=").Append(clip.legacy)
                .Append("|human=").Append(clip.humanMotion);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            builder.Append("|loop=").Append(settings.loopTime)
                .Append("|loopBlend=").Append(settings.loopBlend)
                .Append("|loopOrientation=").Append(settings.loopBlendOrientation)
                .Append("|loopY=").Append(settings.loopBlendPositionY)
                .Append("|loopXZ=").Append(settings.loopBlendPositionXZ)
                .Append("|keepOrientation=").Append(settings.keepOriginalOrientation)
                .Append("|keepY=").Append(settings.keepOriginalPositionY)
                .Append("|keepXZ=").Append(settings.keepOriginalPositionXZ)
                .Append("|feet=").Append(settings.heightFromFeet)
                .Append("|mirror=").Append(settings.mirror)
                .Append("|start=").Append(RF(settings.startTime))
                .Append("|stop=").Append(RF(settings.stopTime))
                .Append("|cycle=").Append(RF(settings.cycleOffset))
                .Append("|level=").Append(RF(settings.level));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal))
            {
                builder.Append("|curve:").Append(binding.path).Append('|')
                    .Append(binding.type.AssemblyQualifiedName).Append('|').Append(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                builder.Append('|').Append((int)curve.preWrapMode).Append('|').Append((int)curve.postWrapMode);
                foreach (Keyframe key in curve.keys)
                    builder.Append('[').Append(RF(key.time)).Append(',').Append(RF(key.value))
                        .Append(',').Append(RF(key.inTangent)).Append(',').Append(RF(key.outTangent))
                        .Append(',').Append(RF(key.inWeight)).Append(',').Append(RF(key.outWeight))
                        .Append(',').Append((int)key.weightedMode).Append(']');
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                builder.Append("|object:").Append(binding.path).Append('|').Append(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                    builder.Append('[').Append(RF(key.time)).Append(',')
                        .Append(key.value == null ? "null" : AssetDatabase.GetAssetPath(key.value))
                        .Append(',').Append(key.value == null ? "null" : key.value.name).Append(']');
            }
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                builder.Append("|event:").Append(RF(item.time)).Append('|').Append(item.functionName)
                    .Append('|').Append(item.stringParameter).Append('|').Append(item.intParameter)
                    .Append('|').Append(RF(item.floatParameter));
            return builder.ToString();
        }

        private static string HierarchyComponentSignature(GameObject root)
        {
            var builder = new StringBuilder();
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => RelativePath(root.transform, item), StringComparer.Ordinal))
            {
                string path = RelativePath(root.transform, transform);
                builder.Append('|').Append(path).Append("|active=").Append(transform.gameObject.activeSelf);
                if (transform != root.transform)
                    builder.Append("|p=").Append(Vec(transform.localPosition))
                        .Append("|r=").Append(Quat(transform.localRotation))
                        .Append("|s=").Append(Vec(transform.localScale));
                foreach (Component component in transform.GetComponents<Component>()
                    .Where(item => item != null)
                    .OrderBy(item => item.GetType().FullName, StringComparer.Ordinal))
                    builder.Append("|c=").Append(component.GetType().AssemblyQualifiedName);
            }
            return builder.ToString();
        }

        private static string RendererMaterialSignature(GameObject root)
        {
            return string.Join("\n", root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => RelativePath(root.transform, item.transform), StringComparer.Ordinal)
                .Select(item => RelativePath(root.transform, item.transform) + "|" +
                    string.Join(",", item.sharedMaterials.Select(material =>
                        material == null ? "null" : AssetDatabase.GetAssetPath(material) + "#" + material.name))));
        }

        private static string AnimatorSignature(GameObject root)
        {
            return string.Join("\n", root.GetComponentsInChildren<Animator>(true)
                .OrderBy(item => RelativePath(root.transform, item.transform), StringComparer.Ordinal)
                .Select(item => RelativePath(root.transform, item.transform) +
                    "|controller=" + AssetDatabase.GetAssetPath(item.runtimeAnimatorController) +
                    "|avatar=" + AssetDatabase.GetAssetPath(item.avatar) +
                    "|rootMotion=" + item.applyRootMotion +
                    "|culling=" + item.cullingMode +
                    "|enabled=" + item.enabled));
        }

        private static string RelativePath(Transform root, Transform item)
        {
            if (item == root) return string.Empty;
            var names = new Stack<string>();
            for (Transform current = item; current != null && current != root; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static GameObject FindOptionalUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => string.Equals(item.name, name, StringComparison.Ordinal))
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length > 1)
                throw new InvalidOperationException("Expected at most one scene object named " + name + ", found " + matches.Length + ".");
            return matches.Length == 0 ? null : matches[0];
        }

        private static string Vec(Vector3 value) =>
            RF(value.x) + "," + RF(value.y) + "," + RF(value.z);

        private static string Quat(Quaternion value) =>
            RF(value.x) + "," + RF(value.y) + "," + RF(value.z) + "," + RF(value.w);

        private static string RF(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string DescribeAnimatedRoot(GameObject root)
        {
            Animator animator = root.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
                return root.name + "|controller=<none>|clips=<none>";
            string controller = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
            string clips = string.Join(",", animator.runtimeAnimatorController.animationClips
                .Where(clip => clip != null)
                .Select(clip => clip.name)
                .Distinct(StringComparer.Ordinal));
            return root.name + "|controller=" + controller + "|clips=" + clips;
        }

        private static AnimationClip RequireSingleControllerClip(GameObject root, string label)
        {
            Animator animator = root.GetComponent<Animator>() ??
                throw new InvalidOperationException(label + " has no root Animator.");
            RuntimeAnimatorController controller = animator.runtimeAnimatorController ??
                throw new InvalidOperationException(label + " has no Animator Controller.");
            AnimationClip[] clips = controller.animationClips
                .Where(clip => clip != null)
                .Distinct()
                .ToArray();
            if (clips.Length != 1)
                throw new InvalidOperationException(label + " must expose exactly one animation clip. count=" + clips.Length);
            return clips[0];
        }

        private static GameObject CloneForPreview(GameObject source, string name)
        {
            GameObject clone = UnityEngine.Object.Instantiate(source);
            clone.name = name;
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetParent(null, false);
            clone.transform.SetPositionAndRotation(Vector3.zero, source.transform.rotation);
            clone.transform.localScale = source.transform.lossyScale;
            SetLayerRecursively(clone, PreviewLayer);
            foreach (Animator animator in clone.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
            foreach (SkinnedMeshRenderer renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.updateWhenOffscreen = true;
            return clone;
        }

        private static Texture2D RenderSample(GameObject actor, AnimationClip clip, float time)
        {
            return RenderSample(actor, clip, time, PreviewView.Front);
        }

        private static Texture2D RenderSample(
            GameObject actor,
            AnimationClip clip,
            float time,
            PreviewView view)
        {
            return RenderSample(actor, clip, time, view, null, 1f);
        }

        private static Texture2D RenderSample(
            GameObject actor,
            AnimationClip clip,
            float time,
            PreviewView view,
            Vector3? focusOverride,
            float sizeMultiplier)
        {
            clip.SampleAnimation(actor, Mathf.Clamp(time, 0f, clip.length));
            var bakedObjects = new List<GameObject>();
            var bakedMeshes = new List<Mesh>();
            var enabledStates = new Dictionary<SkinnedMeshRenderer, bool>();
            foreach (SkinnedMeshRenderer skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                enabledStates.Add(skin, skin.enabled);
                if (!skin.enabled || skin.sharedMesh == null)
                    continue;
                var mesh = new Mesh { name = skin.sharedMesh.name + "_PostureBreakSourceBake" };
                skin.BakeMesh(mesh, false);
                var baked = new GameObject(skin.name + "_PostureBreakSourceBake");
                baked.hideFlags = HideFlags.HideAndDontSave;
                baked.layer = PreviewLayer;
                baked.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                baked.transform.localScale = skin.transform.lossyScale;
                baked.AddComponent<MeshFilter>().sharedMesh = mesh;
                baked.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                bakedMeshes.Add(mesh);
                bakedObjects.Add(baked);
                skin.enabled = false;
            }

            Bounds bounds = RendererBounds(actor, bakedObjects);
            float radius = Mathf.Max(bounds.extents.magnitude, 0.2f);
            Vector3 up = actor.transform.up.normalized;
            Vector3 front = actor.transform.forward.normalized;
            Vector3 right = actor.transform.right.normalized;
            Vector3 focus = focusOverride ?? bounds.center;
            Vector3 position;
            Vector3 cameraUp;
            switch (view)
            {
                case PreviewView.Side:
                    position = focus + right * (radius * 2.35f) + front * (radius * 0.15f) + up * (radius * 0.25f);
                    cameraUp = up;
                    break;
                case PreviewView.Top:
                    position = focus + up * (radius * 2.6f) + front * (radius * 0.12f);
                    cameraUp = front;
                    break;
                default:
                    position = focus + front * (radius * 2.25f) + right * (radius * 0.55f) + up * (radius * 0.25f);
                    cameraUp = up;
                    break;
            }

            var cameraObject = new GameObject("PostureBreak_SourceInspectionCamera");
            var lightObject = new GameObject("PostureBreak_SourceInspectionLight");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            lightObject.hideFlags = HideFlags.HideAndDontSave;
            RenderTexture texture = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << PreviewLayer;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = Mathf.Max(20f, radius * 8f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.y, Mathf.Max(bounds.extents.x, bounds.extents.z)) *
                    1.28f * Mathf.Clamp(sizeMultiplier, 0.2f, 2f);
                camera.transform.SetPositionAndRotation(
                    position,
                    Quaternion.LookRotation((focus - position).normalized, cameraUp));
                camera.targetTexture = texture;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.45f;
                light.cullingMask = 1 << PreviewLayer;
                light.transform.rotation = Quaternion.LookRotation(-front - right * 0.4f - up * 0.65f, up);

                camera.Render();
                RenderTexture.active = texture;
                var image = new Texture2D(PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(texture);
                foreach (KeyValuePair<SkinnedMeshRenderer, bool> item in enabledStates)
                    if (item.Key != null) item.Key.enabled = item.Value;
                foreach (GameObject baked in bakedObjects)
                    UnityEngine.Object.DestroyImmediate(baked);
                foreach (Mesh mesh in bakedMeshes)
                    UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Texture2D Combine(IReadOnlyList<Texture2D> panels, int columns, int rows)
        {
            int width = columns * PanelSize + (columns - 1) * Gap;
            int height = rows * PanelSize + (rows - 1) * Gap;
            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.SetPixels32(Enumerable.Repeat(new Color32(14, 17, 25, 255), width * height).ToArray());
            for (int index = 0; index < panels.Count; index++)
            {
                int column = index % columns;
                int rowFromTop = index / columns;
                int y = (rows - 1 - rowFromTop) * (PanelSize + Gap);
                result.SetPixels(column * (PanelSize + Gap), y, PanelSize, PanelSize, panels[index].GetPixels());
            }
            result.Apply(false, false);
            return result;
        }

        private static Bounds RendererBounds(GameObject root, IReadOnlyList<GameObject> additionalRoots)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Concat(additionalRoots.SelectMany(item => item.GetComponentsInChildren<Renderer>(true)))
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        private static Scene RequireScene()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException("CargoRunMvp must be the active scene. active=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("PostureBreak setup requires Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => string.Equals(item.name, name, StringComparison.Ordinal))
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one scene object named " + name + ", found " + matches.Length + ".");
            return matches[0];
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            Transform[] matches = parent.Cast<Transform>()
                .Where(child => string.Equals(child.name, name, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one direct child named " + name + ", found " + matches.Length + ".");
            return matches[0];
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (Transform current = transform; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));

        private static void WritePng(string relativePath, Texture2D image)
        {
            string path = Absolute(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, image.EncodeToPNG());
        }

        private static void WriteText(string relativePath, string contents)
        {
            string path = Absolute(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        internal static class UpperBodyTwitchPlayModeCapture
        {
            private const string PendingKey = "Bellerophon.PostureBreakUpperBodyTwitch.PlayMode.Pending";
            private const string StateKey = "Bellerophon.PostureBreakUpperBodyTwitch.PlayMode.State";
            private const string FailureKey = "Bellerophon.PostureBreakUpperBodyTwitch.PlayMode.Failure";
            private const string ConsoleErrorsBeforeKey = "Bellerophon.PostureBreakUpperBodyTwitch.PlayMode.ConsoleErrorsBefore";
            private const int WaitingForPlayMode = 0;
            private const int Capturing = 1;
            private const int WaitingForEditModeAfterSuccess = 2;
            private const int WaitingForEditModeAfterFailure = 3;
            private static readonly float[] CaptureTimes =
            {
                0.20f,
                UpperBodyTwitchAttackStartTime - 0.10f,
                UpperBodyTwitchAttackStartTime,
                UpperBodyTwitchAttackStartTime + UpperBodyTwitchPeakTime,
                UpperBodyTwitchAttackStartTime + UpperBodyTwitchReturnTime,
                UpperBodyTwitchAttackStartTime + UpperBodyTwitchPeriod,
                UpperBodyTwitchAttackStartTime + UpperBodyTwitchPeriod + UpperBodyTwitchPeakTime,
                UpperBodyTwitchAttackStartTime + UpperBodyTwitchPeriod + UpperBodyTwitchReturnTime,
                UpperBodyTwitchAttackStartTime + UpperBodyTwitchPeriod * 2f + UpperBodyTwitchPeakTime
            };
            private static readonly List<Texture2D> SidePanels = new List<Texture2D>();
            private static readonly List<Texture2D> TopPanels = new List<Texture2D>();
            private static readonly List<float> CapturedTimes = new List<float>();
            private static readonly List<float> CapturedTergoTimes = new List<float>();
            private static readonly List<float> HeadElevations = new List<float>();
            private static readonly List<LegSpreadSample> LegSpreads = new List<LegSpreadSample>();
            private static Action<string> complete;
            private static Action<Exception> fail;
            private static GameObject target;
            private static GameObject tergo;
            private static Animator targetAnimator;
            private static Animator tergoAnimator;
            private static float twitchCycleDuration;
            private static float tergoCycleDuration;
            private static double startedAt;
            private static float previousTime;
            private static bool observedInitialWrap;
            private static int cycleIndex;
            private static int nextPhaseIndex;

            static UpperBodyTwitchPlayModeCapture()
            {
                if (HasPendingCapture)
                    Subscribe();
            }

            internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

            internal static void ResetStaleCapture()
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    Cleanup();
            }

            internal static bool RecoverStaleCaptureAndExitPlayMode()
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    return false;
                Cleanup();
                if (EditorApplication.isPlaying)
                    EditorApplication.ExitPlaymode();
                else
                    EditorApplication.isPlaying = false;
                return true;
            }

            internal static void Start(Action<string> onComplete, Action<Exception> onFail)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException(
                        "PostureBreak upper-body twitch direct review must start in Edit Mode.");
                InspectUpperBodyTwitch();
                complete = onComplete;
                fail = onFail;
                CleanupPanels();
                SessionState.SetBool(PendingKey, true);
                SessionState.SetInt(StateKey, WaitingForPlayMode);
                SessionState.SetInt(ConsoleErrorsBeforeKey, LightsaberSetupTools.ConsoleErrorCount());
                SessionState.EraseString(FailureKey);
                Subscribe();
                EditorApplication.EnterPlaymode();
            }

            internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
            {
                complete = onComplete;
                fail = onFail;
                if (!HasPendingCapture)
                    throw new InvalidOperationException(
                        "PostureBreak upper-body twitch direct review has no pending state.");
                Subscribe();
            }

            private static void Subscribe()
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }

            private static void Tick()
            {
                if (!HasPendingCapture)
                {
                    EditorApplication.update -= Tick;
                    return;
                }
                int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
                try
                {
                    if (state == WaitingForPlayMode)
                    {
                        if (!EditorApplication.isPlaying)
                            return;
                        InitializeRuntime();
                        SessionState.SetInt(StateKey, Capturing);
                        return;
                    }
                    if (state == Capturing)
                    {
                        if (!EditorApplication.isPlaying)
                            throw new InvalidOperationException(
                                "Play Mode ended before PostureBreak twitch review completed.");
                        if (EditorApplication.timeSinceStartup - startedAt > 45d)
                            throw new TimeoutException(
                                "PostureBreak twitch direct review exceeded 45 seconds.");
                        ObserveAndCapture();
                        return;
                    }
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        return;
                    if (state == WaitingForEditModeAfterFailure)
                    {
                        FinishFailure();
                        return;
                    }
                    InspectUpperBodyTwitch();
                    DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                    Action<string> callback = complete;
                    Cleanup();
                    callback?.Invoke(
                        "PostureBreak pre-attack stillness and one-second upper-body/knee twitch beginning with the first Tergo chest stab were directly captured from two actual Play Mode cycles.");
                }
                catch (Exception exception)
                {
                    SessionState.SetString(FailureKey, exception.ToString());
                    CleanupPanels();
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                        if (EditorApplication.isPlaying)
                            EditorApplication.ExitPlaymode();
                        return;
                    }
                    FinishFailure();
                }
            }

            private static void InitializeRuntime()
            {
                Scene scene = RequireScene();
                target = FindUnique(scene, TargetName);
                targetAnimator = target.GetComponent<Animator>() ??
                    throw new InvalidOperationException("PostureBreak target has no runtime Animator.");
                if (targetAnimator.layerCount != 2)
                    throw new InvalidOperationException("PostureBreak runtime Animator does not have the twitch layer.");
                tergo = FindUnique(scene, TergoCopyName);
                tergoAnimator = tergo.GetComponent<Animator>() ??
                    throw new InvalidOperationException("PostureBreak Tergo copy has no runtime Animator.");
                AnimationClip twitchClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UpperBodyTwitchClipPath) ??
                    throw new InvalidOperationException("PostureBreak twitch clip is missing.");
                AnimationClip tergoClip = RequireSingleControllerClip(tergo, TergoCopyName);
                twitchCycleDuration = twitchClip.length;
                tergoCycleDuration = tergoClip.length;
                if (Mathf.Abs(twitchCycleDuration - tergoCycleDuration) > 0.0001f)
                    throw new InvalidOperationException(
                        "PostureBreak twitch and Tergo attack runtime cycles differ.");
                startedAt = EditorApplication.timeSinceStartup;
                previousTime = CurrentTwitchTime();
                observedInitialWrap = false;
                cycleIndex = 0;
                nextPhaseIndex = 0;
                CleanupPanels();
            }

            private static void ObserveAndCapture()
            {
                float time = CurrentTwitchTime();
                bool wrapped = time + twitchCycleDuration * 0.5f < previousTime;
                if (!observedInitialWrap)
                {
                    if (wrapped)
                        observedInitialWrap = true;
                    previousTime = time;
                    return;
                }
                if (wrapped && nextPhaseIndex >= CaptureTimes.Length)
                {
                    cycleIndex++;
                    nextPhaseIndex = 0;
                }
                while (cycleIndex < 2 && nextPhaseIndex < CaptureTimes.Length &&
                       time >= CaptureTimes[nextPhaseIndex])
                {
                    SidePanels.Add(CaptureActualLivePair(target, tergo, PreviewView.Side));
                    TopPanels.Add(CaptureActualLivePair(target, tergo, PreviewView.Top));
                    CapturedTimes.Add(time);
                    CapturedTergoTimes.Add(CurrentTergoTime());
                    Transform hips = FindPath(target.transform, CarrierHipsPath);
                    Transform head = FindPath(target.transform, CarrierHeadPath);
                    HeadElevations.Add(head.position.y - hips.position.y);
                    LegSpreads.Add(MeasureCurrentLegSpread(target));
                    nextPhaseIndex++;
                }
                previousTime = time;
                if (cycleIndex < 1 || nextPhaseIndex < CaptureTimes.Length)
                    return;
                if (SidePanels.Count != CaptureTimes.Length * 2 ||
                    TopPanels.Count != CaptureTimes.Length * 2)
                    throw new InvalidOperationException(
                        "PostureBreak twitch Play Mode capture did not collect both attack-synchronized cycles.");
                WriteUpperBodyTwitchPlayModeEvidence(
                    SidePanels,
                    TopPanels,
                    CapturedTimes,
                    CapturedTergoTimes,
                    HeadElevations,
                    LegSpreads,
                    target.transform.position,
                    target.transform.rotation,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                CleanupPanels();
                SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                EditorApplication.ExitPlaymode();
            }

            private static float CurrentTwitchTime()
            {
                AnimatorStateInfo state = targetAnimator.GetCurrentAnimatorStateInfo(1);
                return (state.normalizedTime - Mathf.Floor(state.normalizedTime)) * twitchCycleDuration;
            }

            private static float CurrentTergoTime()
            {
                AnimatorStateInfo state = tergoAnimator.GetCurrentAnimatorStateInfo(0);
                return (state.normalizedTime - Mathf.Floor(state.normalizedTime)) * tergoCycleDuration;
            }

            private static void FinishFailure()
            {
                string message = SessionState.GetString(
                    FailureKey,
                    "Unknown PostureBreak upper-body twitch Play Mode review failure.");
                Action<Exception> callback = fail;
                Cleanup();
                callback?.Invoke(new InvalidOperationException(message));
            }

            private static void CleanupPanels()
            {
                foreach (Texture2D panel in SidePanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                foreach (Texture2D panel in TopPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                SidePanels.Clear();
                TopPanels.Clear();
                CapturedTimes.Clear();
                CapturedTergoTimes.Clear();
                HeadElevations.Clear();
                LegSpreads.Clear();
            }

            private static void Cleanup()
            {
                EditorApplication.update -= Tick;
                CleanupPanels();
                target = null;
                tergo = null;
                targetAnimator = null;
                tergoAnimator = null;
                twitchCycleDuration = 0f;
                tergoCycleDuration = 0f;
                complete = null;
                fail = null;
                SessionState.EraseBool(PendingKey);
                SessionState.EraseInt(StateKey);
                SessionState.EraseString(FailureKey);
                SessionState.EraseInt(ConsoleErrorsBeforeKey);
            }
        }

        internal static class LegClosePlayModeCapture
        {
            private const string PendingKey = "Bellerophon.PostureBreakLegClose.PlayMode.Pending";
            private const string StateKey = "Bellerophon.PostureBreakLegClose.PlayMode.State";
            private const string FailureKey = "Bellerophon.PostureBreakLegClose.PlayMode.Failure";
            private const string ConsoleErrorsBeforeKey = "Bellerophon.PostureBreakLegClose.PlayMode.ConsoleErrorsBefore";
            private const int WaitingForPlayMode = 0;
            private const int Capturing = 1;
            private const int WaitingForEditModeAfterSuccess = 2;
            private const int WaitingForEditModeAfterFailure = 3;
            private static readonly float[] Phases =
                { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f };
            private static readonly List<Texture2D> OverheadPanels = new List<Texture2D>();
            private static readonly List<Texture2D> SidePanels = new List<Texture2D>();
            private static readonly List<float> CapturedPhases = new List<float>();
            private static readonly List<LegSpreadSample> Spreads = new List<LegSpreadSample>();
            private static Action<string> complete;
            private static Action<Exception> fail;
            private static GameObject target;
            private static Animator targetAnimator;
            private static double startedAt;
            private static float previousPhase;
            private static bool observedWrap;
            private static int nextPhaseIndex;

            static LegClosePlayModeCapture()
            {
                if (HasPendingCapture)
                    Subscribe();
            }

            internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

            internal static void ResetStaleCapture()
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    Cleanup();
            }

            internal static bool RecoverStaleCaptureAndExitPlayMode()
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    return false;
                Cleanup();
                if (EditorApplication.isPlaying)
                    EditorApplication.ExitPlaymode();
                else
                    EditorApplication.isPlaying = false;
                return true;
            }

            internal static void Start(Action<string> onComplete, Action<Exception> onFail)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException(
                        "PostureBreak leg-close direct review must start in Edit Mode.");
                InspectLegClose();
                complete = onComplete;
                fail = onFail;
                CleanupPanels();
                SessionState.SetBool(PendingKey, true);
                SessionState.SetInt(StateKey, WaitingForPlayMode);
                SessionState.SetInt(ConsoleErrorsBeforeKey, LightsaberSetupTools.ConsoleErrorCount());
                SessionState.EraseString(FailureKey);
                Subscribe();
                EditorApplication.EnterPlaymode();
            }

            internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
            {
                complete = onComplete;
                fail = onFail;
                if (!HasPendingCapture)
                    throw new InvalidOperationException(
                        "PostureBreak leg-close direct review has no pending state.");
                Subscribe();
            }

            private static void Subscribe()
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }

            private static void Tick()
            {
                if (!HasPendingCapture)
                {
                    EditorApplication.update -= Tick;
                    return;
                }
                int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
                try
                {
                    if (state == WaitingForPlayMode)
                    {
                        if (!EditorApplication.isPlaying)
                            return;
                        InitializeRuntime();
                        SessionState.SetInt(StateKey, Capturing);
                        return;
                    }
                    if (state == Capturing)
                    {
                        if (!EditorApplication.isPlaying)
                            throw new InvalidOperationException(
                                "Play Mode ended before PostureBreak leg-close review completed.");
                        if (EditorApplication.timeSinceStartup - startedAt > 30d)
                            throw new TimeoutException(
                                "PostureBreak leg-close direct review exceeded 30 seconds.");
                        ObserveAndCapture();
                        return;
                    }
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        return;
                    if (state == WaitingForEditModeAfterFailure)
                    {
                        FinishFailure();
                        return;
                    }
                    InspectLegClose();
                    DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                    Action<string> callback = complete;
                    Cleanup();
                    callback?.Invoke(
                        "PostureBreak forty-percent full-leg closure was directly captured from actual Play Mode Animator poses.");
                }
                catch (Exception exception)
                {
                    SessionState.SetString(FailureKey, exception.ToString());
                    CleanupPanels();
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                        if (EditorApplication.isPlaying)
                            EditorApplication.ExitPlaymode();
                        return;
                    }
                    FinishFailure();
                }
            }

            private static void InitializeRuntime()
            {
                Scene scene = RequireScene();
                target = FindUnique(scene, TargetName);
                targetAnimator = target.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "PostureBreak target has no runtime Animator.");
                startedAt = EditorApplication.timeSinceStartup;
                previousPhase = CurrentPhase();
                observedWrap = false;
                nextPhaseIndex = 0;
                CleanupPanels();
            }

            private static void ObserveAndCapture()
            {
                float phase = CurrentPhase();
                if (!observedWrap)
                {
                    if (phase + 0.5f < previousPhase)
                        observedWrap = true;
                    previousPhase = phase;
                    return;
                }

                while (nextPhaseIndex < Phases.Length && phase >= Phases[nextPhaseIndex])
                {
                    OverheadPanels.Add(CaptureActualLiveActor(target, PreviewView.Top));
                    SidePanels.Add(CaptureActualLiveActor(target, PreviewView.Side));
                    CapturedPhases.Add(phase);
                    Spreads.Add(MeasureCurrentLegSpread(target));
                    nextPhaseIndex++;
                }
                previousPhase = phase;
                if (nextPhaseIndex < Phases.Length)
                    return;

                if (OverheadPanels.Count != 8 || SidePanels.Count != 8 || Spreads.Count != 8)
                    throw new InvalidOperationException(
                        "PostureBreak leg-close Play Mode capture did not collect all 16 panels.");
                WriteLegClosePlayModeEvidence(
                    OverheadPanels,
                    SidePanels,
                    CapturedPhases,
                    Spreads,
                    target.transform.position,
                    target.transform.rotation,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                CleanupPanels();
                SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                EditorApplication.ExitPlaymode();
            }

            private static float CurrentPhase()
            {
                AnimatorStateInfo state = targetAnimator.GetCurrentAnimatorStateInfo(0);
                return state.normalizedTime - Mathf.Floor(state.normalizedTime);
            }

            private static void FinishFailure()
            {
                string message = SessionState.GetString(
                    FailureKey,
                    "Unknown PostureBreak leg-close Play Mode review failure.");
                Action<Exception> callback = fail;
                Cleanup();
                callback?.Invoke(new InvalidOperationException(message));
            }

            private static void CleanupPanels()
            {
                foreach (Texture2D panel in OverheadPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                foreach (Texture2D panel in SidePanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                OverheadPanels.Clear();
                SidePanels.Clear();
                CapturedPhases.Clear();
                Spreads.Clear();
            }

            private static void Cleanup()
            {
                EditorApplication.update -= Tick;
                CleanupPanels();
                target = null;
                targetAnimator = null;
                complete = null;
                fail = null;
                SessionState.EraseBool(PendingKey);
                SessionState.EraseInt(StateKey);
                SessionState.EraseString(FailureKey);
                SessionState.EraseInt(ConsoleErrorsBeforeKey);
            }
        }

        internal static class ChestMountPlayModeCapture
        {
            private const string PendingKey = "Bellerophon.PostureBreakChestMount.PlayMode.Pending";
            private const string StateKey = "Bellerophon.PostureBreakChestMount.PlayMode.State";
            private const string FailureKey = "Bellerophon.PostureBreakChestMount.PlayMode.Failure";
            private const string ConsoleErrorsBeforeKey = "Bellerophon.PostureBreakChestMount.PlayMode.ConsoleErrorsBefore";
            private const int WaitingForPlayMode = 0;
            private const int Capturing = 1;
            private const int WaitingForEditModeAfterSuccess = 2;
            private const int WaitingForEditModeAfterFailure = 3;
            private static readonly float[] SidePhases =
                { 0f, 0.125f, 0.25f, 0.35f, 0.416667f, 0.5f, 0.75f, 0.875f };
            private static readonly List<Texture2D> SidePanels = new List<Texture2D>();
            private static readonly List<Texture2D> TopPanels = new List<Texture2D>();
            private static readonly List<float> CapturedPhases = new List<float>();
            private static Action<string> complete;
            private static Action<Exception> fail;
            private static GameObject target;
            private static GameObject tergo;
            private static Animator tergoAnimator;
            private static double startedAt;
            private static float previousPhase;
            private static bool observedWrap;
            private static int nextPhaseIndex;

            static ChestMountPlayModeCapture()
            {
                if (HasPendingCapture)
                    Subscribe();
            }

            internal static bool HasPendingCapture => SessionState.GetBool(PendingKey, false);

            internal static void ResetStaleCapture()
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    Cleanup();
            }

            internal static void Start(Action<string> onComplete, Action<Exception> onFail)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException(
                        "PostureBreak chest-mount direct review must start in Edit Mode.");
                InspectChestMount();
                complete = onComplete;
                fail = onFail;
                CleanupPanels();
                SessionState.SetBool(PendingKey, true);
                SessionState.SetInt(StateKey, WaitingForPlayMode);
                SessionState.SetInt(ConsoleErrorsBeforeKey, LightsaberSetupTools.ConsoleErrorCount());
                SessionState.EraseString(FailureKey);
                Subscribe();
                EditorApplication.EnterPlaymode();
            }

            internal static void Resume(Action<string> onComplete, Action<Exception> onFail)
            {
                complete = onComplete;
                fail = onFail;
                if (!HasPendingCapture)
                    throw new InvalidOperationException(
                        "PostureBreak chest-mount direct review has no pending state.");
                Subscribe();
            }

            private static void Subscribe()
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }

            private static void Tick()
            {
                if (!HasPendingCapture)
                {
                    EditorApplication.update -= Tick;
                    return;
                }
                int state = SessionState.GetInt(StateKey, WaitingForPlayMode);
                try
                {
                    if (state == WaitingForPlayMode)
                    {
                        if (!EditorApplication.isPlaying)
                            return;
                        InitializeRuntime();
                        SessionState.SetInt(StateKey, Capturing);
                        return;
                    }
                    if (state == Capturing)
                    {
                        if (!EditorApplication.isPlaying)
                            throw new InvalidOperationException(
                                "Play Mode ended before PostureBreak chest-mount review completed.");
                        if (EditorApplication.timeSinceStartup - startedAt > 30d)
                            throw new TimeoutException(
                                "PostureBreak chest-mount direct review exceeded 30 seconds.");
                        ObserveAndCapture();
                        return;
                    }
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                        return;
                    if (state == WaitingForEditModeAfterFailure)
                    {
                        FinishFailure();
                        return;
                    }
                    InspectChestMount();
                    DetectorAttachedStaticStartSetupTools.RequireNoUnityConsoleErrors();
                    Action<string> callback = complete;
                    Cleanup();
                    callback?.Invoke(
                        "PostureBreak Tergo waist mount and alternating upper-chest strikes were directly captured from actual Play Mode Animator poses.");
                }
                catch (Exception exception)
                {
                    SessionState.SetString(FailureKey, exception.ToString());
                    CleanupPanels();
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        SessionState.SetInt(StateKey, WaitingForEditModeAfterFailure);
                        if (EditorApplication.isPlaying)
                            EditorApplication.ExitPlaymode();
                        return;
                    }
                    FinishFailure();
                }
            }

            private static void InitializeRuntime()
            {
                Scene scene = RequireScene();
                target = FindUnique(scene, TargetName);
                tergo = FindUnique(scene, TergoCopyName);
                tergoAnimator = tergo.GetComponent<Animator>() ??
                    throw new InvalidOperationException(
                        "PostureBreak Tergo duplicate has no runtime Animator.");
                startedAt = EditorApplication.timeSinceStartup;
                previousPhase = CurrentPhase();
                observedWrap = false;
                nextPhaseIndex = 0;
                CleanupPanels();
            }

            private static void ObserveAndCapture()
            {
                float phase = CurrentPhase();
                if (!observedWrap)
                {
                    if (phase + 0.5f < previousPhase)
                        observedWrap = true;
                    previousPhase = phase;
                    return;
                }

                while (nextPhaseIndex < SidePhases.Length && phase >= SidePhases[nextPhaseIndex])
                {
                    float requestedPhase = SidePhases[nextPhaseIndex];
                    SidePanels.Add(CaptureActualLivePair(target, tergo, PreviewView.Side));
                    CapturedPhases.Add(phase);
                    if (Mathf.Abs(requestedPhase - 0.35f) < 0.0001f ||
                        Mathf.Abs(requestedPhase - 0.416667f) < 0.0001f ||
                        Mathf.Abs(requestedPhase - 0.5f) < 0.0001f)
                        TopPanels.Add(CaptureActualLivePair(target, tergo, PreviewView.Top));
                    nextPhaseIndex++;
                }
                previousPhase = phase;
                if (nextPhaseIndex < SidePhases.Length)
                    return;

                if (SidePanels.Count != 8 || TopPanels.Count != 3)
                    throw new InvalidOperationException(
                        "PostureBreak chest-mount Play Mode capture did not collect the required 8 side and 3 top frames.");
                WriteChestMountPlayModeEvidence(
                    SidePanels,
                    TopPanels,
                    CapturedPhases,
                    tergo.transform.position,
                    tergo.transform.rotation,
                    SessionState.GetInt(ConsoleErrorsBeforeKey, 0));
                CleanupPanels();
                SessionState.SetInt(StateKey, WaitingForEditModeAfterSuccess);
                EditorApplication.ExitPlaymode();
            }

            private static float CurrentPhase()
            {
                AnimatorStateInfo state = tergoAnimator.GetCurrentAnimatorStateInfo(0);
                return state.normalizedTime - Mathf.Floor(state.normalizedTime);
            }

            private static void FinishFailure()
            {
                string message = SessionState.GetString(
                    FailureKey,
                    "Unknown PostureBreak chest-mount Play Mode review failure.");
                Action<Exception> callback = fail;
                Cleanup();
                callback?.Invoke(new InvalidOperationException(message));
            }

            private static void CleanupPanels()
            {
                foreach (Texture2D panel in SidePanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                foreach (Texture2D panel in TopPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                SidePanels.Clear();
                TopPanels.Clear();
                CapturedPhases.Clear();
            }

            private static void Cleanup()
            {
                EditorApplication.update -= Tick;
                CleanupPanels();
                target = null;
                tergo = null;
                tergoAnimator = null;
                complete = null;
                fail = null;
                SessionState.EraseBool(PendingKey);
                SessionState.EraseInt(StateKey);
                SessionState.EraseString(FailureKey);
                SessionState.EraseInt(ConsoleErrorsBeforeKey);
            }
        }

        private readonly struct LegSpreadSample
        {
            internal readonly float Knee;
            internal readonly float Ankle;
            internal readonly float Toe;

            internal LegSpreadSample(float knee, float ankle, float toe)
            {
                Knee = knee;
                Ankle = ankle;
                Toe = toe;
            }
        }

        private readonly struct LegLocalPose
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            private LegLocalPose(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }

            internal static LegLocalPose Capture(Transform transform) =>
                new LegLocalPose(transform.localPosition, transform.localRotation, transform.localScale);

            internal void Apply(Transform transform)
            {
                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }
        }

        private readonly struct KneeTwitchSolution
        {
            internal static readonly KneeTwitchSolution Identity =
                new KneeTwitchSolution(Quaternion.identity, Quaternion.identity);

            internal readonly Quaternion LeftDelta;
            internal readonly Quaternion RightDelta;
            internal readonly float LeftAngle;
            internal readonly float RightAngle;

            internal KneeTwitchSolution(Quaternion leftDelta, Quaternion rightDelta)
            {
                LeftDelta = leftDelta;
                RightDelta = rightDelta;
                LeftAngle = Quaternion.Angle(Quaternion.identity, leftDelta);
                RightAngle = Quaternion.Angle(Quaternion.identity, rightDelta);
            }
        }

        private readonly struct TwitchMetrics
        {
            internal readonly float UpperBodyAngle;
            internal readonly float LeftKneeAngle;
            internal readonly float RightKneeAngle;

            internal TwitchMetrics(
                float upperBodyAngle,
                float leftKneeAngle,
                float rightKneeAngle)
            {
                UpperBodyAngle = upperBodyAngle;
                LeftKneeAngle = leftKneeAngle;
                RightKneeAngle = rightKneeAngle;
            }
        }

        private readonly struct LegSpreadMetrics
        {
            internal readonly float AverageKnee;
            internal readonly float AverageAnkle;
            internal readonly float AverageToe;
            internal readonly float MinimumKnee;
            internal readonly float MinimumAnkle;
            internal readonly float MinimumToe;
            private readonly int samples;

            internal LegSpreadMetrics(
                float averageKnee,
                float averageAnkle,
                float averageToe,
                float minimumKnee,
                float minimumAnkle,
                float minimumToe,
                int sampleCount)
            {
                AverageKnee = averageKnee;
                AverageAnkle = averageAnkle;
                AverageToe = averageToe;
                MinimumKnee = minimumKnee;
                MinimumAnkle = minimumAnkle;
                MinimumToe = minimumToe;
                samples = sampleCount;
            }

            internal string Describe()
            {
                return "averageKnee=" + F(AverageKnee) +
                    ", averageAnkle=" + F(AverageAnkle) +
                    ", averageToe=" + F(AverageToe) +
                    ", minimumKnee=" + F(MinimumKnee) +
                    ", minimumAnkle=" + F(MinimumAnkle) +
                    ", minimumToe=" + F(MinimumToe) +
                    ", samples=" + samples.ToString(CultureInfo.InvariantCulture);
            }

            internal string DescribeClosureFrom(LegSpreadMetrics source)
            {
                return "knee=" + F(1f - AverageKnee / source.AverageKnee) +
                    ", ankle=" + F(1f - AverageAnkle / source.AverageAnkle) +
                    ", toe=" + F(1f - AverageToe / source.AverageToe);
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            private TransformSnapshot(Vector3 position, Quaternion rotation, Vector3 scale)
            {
                this.position = position;
                this.rotation = rotation;
                this.scale = scale;
            }

            internal static TransformSnapshot Capture(Transform transform) =>
                new TransformSnapshot(transform.position, transform.rotation, transform.localScale);

            internal void RequireUnchanged(Transform transform, string label)
            {
                if (Vector3.Distance(position, transform.position) > 0.00001f ||
                    Quaternion.Angle(rotation, transform.rotation) > 0.001f ||
                    Vector3.Distance(scale, transform.localScale) > 0.00001f)
                    throw new InvalidOperationException(label + " root transform changed unexpectedly.");
            }
        }

        private readonly struct LocalTransformState
        {
            internal readonly Vector3 Position;
            internal readonly Quaternion Rotation;

            private LocalTransformState(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            internal static LocalTransformState Capture(Transform transform) =>
                new LocalTransformState(transform.localPosition, transform.localRotation);
        }

        private readonly struct AnimatorPlaybackMetrics
        {
            private readonly bool sourceClipLoops;
            internal readonly bool MotionObserved;
            internal readonly bool RepeatObserved;
            private readonly int repeatCount;
            private readonly float maximumRotation;
            private readonly float maximumPosition;
            private readonly float finalNormalizedFraction;

            internal AnimatorPlaybackMetrics(
                bool sourceClipLoops,
                bool motionObserved,
                bool repeatObserved,
                int repeatCount,
                float maximumRotation,
                float maximumPosition,
                float finalNormalizedFraction)
            {
                this.sourceClipLoops = sourceClipLoops;
                MotionObserved = motionObserved;
                RepeatObserved = repeatObserved;
                this.repeatCount = repeatCount;
                this.maximumRotation = maximumRotation;
                this.maximumPosition = maximumPosition;
                this.finalNormalizedFraction = finalNormalizedFraction;
            }

            internal string Describe() =>
                "sourceClipLoopSetting=" + sourceClipLoops +
                ", motionObserved=" + MotionObserved +
                ", repeatObserved=" + RepeatObserved +
                ", observedRepeatCount=" + repeatCount.ToString(CultureInfo.InvariantCulture) +
                ", maximumRotationDelta=" + F(maximumRotation) +
                ", maximumPositionDelta=" + F(maximumPosition) +
                ", finalNormalizedFraction=" + F(finalNormalizedFraction);
        }

        private readonly struct ChestMountSolution
        {
            internal readonly Vector3 Position;
            internal readonly Quaternion Rotation;
            private readonly Vector3 chestPosition;
            private readonly Vector3 bodyAxis;
            private readonly Vector3 rootToHips;
            private readonly bool faceCarrierHead;
            private readonly float rootHeightOffset;
            private readonly string mountPath;

            internal ChestMountSolution(
                Vector3 position,
                Quaternion rotation,
                Vector3 chestPosition,
                Vector3 bodyAxis,
                Vector3 rootToHips,
                bool faceCarrierHead,
                float rootHeightOffset,
                string mountPath)
            {
                Position = position;
                Rotation = rotation;
                this.chestPosition = chestPosition;
                this.bodyAxis = bodyAxis;
                this.rootToHips = rootToHips;
                this.faceCarrierHead = faceCarrierHead;
                this.rootHeightOffset = rootHeightOffset;
                this.mountPath = mountPath;
            }

            internal void Apply(Transform transform)
            {
                transform.SetPositionAndRotation(Position, Rotation);
            }

            internal string Describe() =>
                "faceCarrierHead=" + faceCarrierHead +
                ", rootHeightOffset=" + F(rootHeightOffset) +
                ", mountPath=" + mountPath +
                ", rootPosition=" + Vec(Position) +
                ", rootEuler=" + Vec(Rotation.eulerAngles) +
                ", chestPosition=" + Vec(chestPosition) +
                ", bodyAxis=" + Vec(bodyAxis) +
                ", averageRootToHips=" + Vec(rootToHips);
        }

        private readonly struct ChestMountGeometryMetrics
        {
            internal readonly float AverageHorizontalHipError;
            private readonly float maximumHorizontalHipError;
            internal readonly float FacingDot;

            internal ChestMountGeometryMetrics(
                float averageHorizontalHipError,
                float maximumHorizontalHipError,
                float facingDot)
            {
                AverageHorizontalHipError = averageHorizontalHipError;
                this.maximumHorizontalHipError = maximumHorizontalHipError;
                FacingDot = facingDot;
            }

            internal string Describe() =>
                "averageHorizontalHipError=" + F(AverageHorizontalHipError) +
                ", maximumPhaseHorizontalHipError=" + F(maximumHorizontalHipError) +
                ", facingDot=" + F(FacingDot);
        }

        private readonly struct WaistMountConfiguration
        {
            internal readonly string Label;
            internal readonly float AnchorRatio;
            internal readonly float RootHeightOffset;

            internal WaistMountConfiguration(
                string label,
                float anchorRatio,
                float rootHeightOffset)
            {
                Label = label;
                AnchorRatio = anchorRatio;
                RootHeightOffset = rootHeightOffset;
            }
        }

        private readonly struct HandAttackMetrics
        {
            internal readonly float LeftClosestTime;
            internal readonly float RightClosestTime;
            internal readonly float LeftClosestDistance;
            internal readonly float RightClosestDistance;
            private readonly float clipLength;
            private readonly Vector3 attackTarget;

            internal HandAttackMetrics(
                float leftClosestTime,
                float rightClosestTime,
                float leftClosestDistance,
                float rightClosestDistance,
                float clipLength,
                Vector3 attackTarget)
            {
                LeftClosestTime = leftClosestTime;
                RightClosestTime = rightClosestTime;
                LeftClosestDistance = leftClosestDistance;
                RightClosestDistance = rightClosestDistance;
                this.clipLength = clipLength;
                this.attackTarget = attackTarget;
            }

            internal string Describe() =>
                "leftClosestTime=" + F(LeftClosestTime) +
                ", leftClosestNormalized=" + F(LeftClosestTime / Mathf.Max(clipLength, 0.0001f)) +
                ", leftClosestDistance=" + F(LeftClosestDistance) +
                ", rightClosestTime=" + F(RightClosestTime) +
                ", rightClosestNormalized=" + F(RightClosestTime / Mathf.Max(clipLength, 0.0001f)) +
                ", rightClosestDistance=" + F(RightClosestDistance) +
                ", attackTarget=" + Vec(attackTarget);
        }

        private enum PreviewView
        {
            Front,
            Side,
            Top
        }

        private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
