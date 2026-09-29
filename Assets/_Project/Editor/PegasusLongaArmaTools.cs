using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Enemies.Fuga;
using Bellerophon.Enemies.LongaArma;
using Bellerophon.Enemies.Parvum;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bellerophon.Editor
{
    internal static class PegasusLongaArmaTools
    {
        const string SourceScene="Assets/_Project/Scenes/CargoRunMvp.unity";
        const string PrefabPath="Assets/_Project/Prefabs/Enemies/LongaArma/Gameplay/LongaArmaGameplay.prefab";
        const string ActorName="Longa Arma Gameplay 01";
        // The visible approved walking FBX, unlike its hidden legacy rig, faces +Z.
        static readonly Quaternion GameplayFacing=Quaternion.identity;
        static readonly string[] States={
            "LongaArma_01_Idle", "LongaArma_02_Move_Crawl", "LongaArma_03_Attack_SlamDrag",
            "LongaArma_04_Hit_Recoil", "LongaArma_06_Death_MeltPuddle", "LongaArma_05_Consume_Peck"
        };

        public static void InspectImportedClips()
        {
            const string path="Assets/_Project/Art/Enemies/LongaArma/Models/longa_arma_runtime_lowpoly.fbx";
            var report=new StringBuilder();
            foreach (var clip in AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<AnimationClip>())
            {
                var bindings=AnimationUtility.GetCurveBindings(clip);
                report.AppendLine($"{clip.name} length={clip.length:F3} firstPath={(bindings.Length>0?bindings[0].path:"NONE")} blade={bindings.Count(x=>x.path.Contains("DEF_blade_"))} bladePath={bindings.FirstOrDefault(x=>x.path.Contains("DEF_blade_tip_l")).path} mouth={bindings.Count(x=>x.path.Contains("DEF_mouth"))}");
            }
            var actor=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name==ActorName);
            var animation=actor.transform.Find("LongaArma_03_Attack_SlamDrag").GetComponentInChildren<Animator>(true);
            var skin=animation.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var tip=skin.bones.Single(x=>x.name=="DEF_blade_tip_l");
            report.AppendLine($"animator={animation.name} skinPath={AnimationUtility.CalculateTransformPath(skin.transform,animation.transform)} tipPath={AnimationUtility.CalculateTransformPath(tip,animation.transform)}");
            var action=AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<AnimationClip>().Single(x=>x.name.EndsWith("|LongaArma_Attack_SlamDrag",StringComparison.Ordinal));
            report.AppendLine($"avatar={(animation.avatar?animation.avatar.name:"NONE")} human={action.humanMotion} legacy={action.legacy}");
            foreach (var component in actor.transform.Find("LongaArma_03_Attack_SlamDrag").GetComponentsInChildren<Animator>(true))
                report.AppendLine($"animator component path={AnimationUtility.CalculateTransformPath(component.transform,actor.transform)} avatar={(component.avatar?component.avatar.name:"NONE")} controller={(component.runtimeAnimatorController?component.runtimeAnimatorController.name:"NONE")} enabled={component.enabled}");
            foreach (var avatar in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>())
                report.AppendLine($"imported avatar={avatar.name} valid={avatar.isValid} human={avatar.isHuman}");
            foreach (var binding in AnimationUtility.GetCurveBindings(action).Where(x=>x.path.Contains("DEF_blade_shoulder_l") && (x.propertyName.Contains("Rotation") || x.propertyName.Contains("Position"))).Take(12))
            {
                var keys=AnimationUtility.GetEditorCurve(action,binding).keys;
                report.AppendLine($"{binding.path} {binding.propertyName} keys={keys.Length} min={keys.Min(x=>x.value):F3} max={keys.Max(x=>x.value):F3}");
            }
            Debug.Log(report.ToString());
        }

        public static void InspectGroundAndDeath()
        {
            var actor=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name==ActorName);
            foreach(int state in new[]{0,1,4})
            {
                var slot=actor.transform.Find(States[state]);
                var skin=slot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x=>x.enabled && LongaArmaAnimationView.VisibleWithin(x.transform,slot));
                var driver=skin.GetComponentInParent<Animator>(true);var clip=driver.runtimeAnimatorController.animationClips.Single();
                Debug.Log($"GROUND slot={slot.name} offset={slot.localPosition:F3} scale={slot.lossyScale:F3} clip={AssetDatabase.GetAssetPath(clip)} length={clip.length} loop={clip.isLooping} bones="+string.Join(";",skin.bones.Where(b=>b).Select(b=>$"{b.name}:{(b.position-actor.transform.position).ToString("F3")}")));
                if(state!=4)continue;
                var copy=UnityEngine.Object.Instantiate(slot.gameObject);copy.hideFlags=HideFlags.HideAndDontSave;
                var copySkin=copy.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x=>x.enabled && LongaArmaAnimationView.VisibleWithin(x.transform,copy.transform));
                var copyDriver=copySkin.GetComponentInParent<Animator>(true);var mesh=new Mesh();
                try
                {
                    for(int i=0;i<=20;i++)
                    {
                        float time=clip.length*i/20;clip.SampleAnimation(copyDriver.gameObject,time);copySkin.BakeMesh(mesh,true);
                        var points=mesh.vertices.Select(v=>copySkin.transform.TransformPoint(v)).ToArray();
                        Debug.Log($"DEATH_SAMPLE t={time:F3} min={points.Min(v=>v.y):F3} max={points.Max(v=>v.y):F3} height={points.Max(v=>v.y)-points.Min(v=>v.y):F3}");
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(copy); }
            }
        }

        public static void FixGameplayAnimations()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required.");
            var scene=SceneManager.GetActiveScene();
            if (scene.name!="Pegasus") throw new InvalidOperationException("Pegasus must be active.");
            var actor=scene.GetRootGameObjects().Single(x=>x.name==ActorName);
            const string modelPath="Assets/_Project/Art/Enemies/LongaArma/Models/longa_arma_runtime_lowpoly.fbx";
            var imported=AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath).OfType<AnimationClip>().ToArray();
            var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                for (int i=0;i<States.Length;i++)
                {
                    var live=actor.transform.Find(States[i]).GetComponentInChildren<Animator>(true);
                    var saved=prefab.transform.Find(States[i]).GetComponentInChildren<Animator>(true);
                    var baseController=live.runtimeAnimatorController is AnimatorOverrideController existing ? existing.runtimeAnimatorController : live.runtimeAnimatorController;
                    var original=baseController.animationClips.Single();
                    var matching=imported.Single(x=>x.name.EndsWith("|"+original.name,StringComparison.Ordinal));
                    var bindings=AnimationUtility.GetCurveBindings(matching);
                    if (!bindings.Any(x=>x.path.Contains("DEF_blade_"))) throw new InvalidOperationException("Imported clip is not for detailed rig: "+matching.name);
                    var clipPath=$"Assets/_Project/Prefabs/Enemies/LongaArma/Gameplay/{original.name}_Gameplay.anim";
                    var gameplayClip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (!gameplayClip)
                    {
                        gameplayClip=UnityEngine.Object.Instantiate(matching);
                        gameplayClip.name=original.name+"_Gameplay";
                        var settings=AnimationUtility.GetAnimationClipSettings(gameplayClip);
                        settings.loopTime=i!=3 && i!=4;
                        AnimationUtility.SetAnimationClipSettings(gameplayClip,settings);
                        AssetDatabase.CreateAsset(gameplayClip,clipPath);
                    }
                    var assetPath=$"Assets/_Project/Prefabs/Enemies/LongaArma/Gameplay/{original.name}_Gameplay.overrideController";
                    var overrideController=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(assetPath);
                    if (!overrideController)
                    {
                        overrideController=new AnimatorOverrideController(baseController) { name=original.name+"_Gameplay" };
                        AssetDatabase.CreateAsset(overrideController,assetPath);
                    }
                    var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();
                    overrideController.GetOverrides(overrides);
                    for (int j=0;j<overrides.Count;j++) overrides[j]=new KeyValuePair<AnimationClip,AnimationClip>(overrides[j].Key,gameplayClip);
                    overrideController.ApplyOverrides(overrides);
                    EditorUtility.SetDirty(overrideController);
                    live.runtimeAnimatorController=overrideController;
                    saved.runtimeAnimatorController=overrideController;
                    Debug.Log($"Longa gameplay action {States[i]}: {matching.name} ({matching.length:F3}s, {bindings.Length} bindings)");
                }
                PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        public static void Inspect()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required.");
            var active=SceneManager.GetActiveScene();
            if (active.name!="Pegasus") throw new InvalidOperationException("Pegasus must be active.");
            var fuga=active.GetRootGameObjects().Single(x=>x.name=="Fuga Gameplay 01");
            var source=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
            try
            {
                var root=source.GetRootGameObjects().Single(x=>x.name=="Approved Longa Arma Enemy Placement");
                var report=new StringBuilder($"Fuga active={fuga.activeSelf} pos={fuga.transform.position:F3}; Longa source root={root.name}\n");
                foreach (var name in States)
                {
                    var slot=root.transform.Find(name);
                    if (!slot) throw new InvalidOperationException("Missing source slot: "+name);
                    var skin=slot.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    var animator=slot.GetComponentInChildren<Animator>(true);
                    report.AppendLine($"{name} active={slot.gameObject.activeSelf} rotation={slot.rotation.eulerAngles:F2} localRotation={slot.localRotation.eulerAngles:F2} scale={slot.lossyScale:F3} skin={(skin?skin.name:"NONE")} bounds={(skin?skin.bounds.ToString():"NONE")} animator={(animator?animator.name:"NONE")}");
                    if (animator && animator.runtimeAnimatorController)
                        foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                            report.AppendLine($"  clip={clip.name} duration={clip.length:F3} path={AssetDatabase.GetAssetPath(clip)}");
                    if (skin) report.AppendLine("  bones="+string.Join(",",skin.bones.Where(x=>x && (x.name.Contains("blade") || x.name.Contains("mouth"))).Select(x=>x.name)));
                    foreach(var visible in slot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        report.AppendLine($"  RENDER {AnimationUtility.CalculateTransformPath(visible.transform,slot)} active={visible.gameObject.activeInHierarchy} enabled={visible.enabled} vertices={visible.sharedMesh.vertexCount} bounds={visible.bounds} bones={string.Join(",",visible.bones.Where(x=>x).Select(x=>x.name))}");
                    foreach(var driver in slot.GetComponentsInChildren<Animator>(true))
                        report.AppendLine($"  DRIVER {AnimationUtility.CalculateTransformPath(driver.transform,slot)} active={driver.gameObject.activeInHierarchy} enabled={driver.enabled} localEuler={driver.transform.localEulerAngles} clips={(driver.runtimeAnimatorController?string.Join(",",driver.runtimeAnimatorController.animationClips.Select(x=>x.name)):"NONE")}");
                }
                Debug.Log(report.ToString());
            }
            finally { EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(active); }
        }

        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required.");
            var scene=SceneManager.GetActiveScene();
            if (scene.name!="Pegasus" || scene.isDirty) throw new InvalidOperationException("Clean Pegasus edit scene required.");
            var fuga=scene.GetRootGameObjects().Single(x=>x.name=="Fuga Gameplay 01");
            if (scene.GetRootGameObjects().Any(x=>x.name==ActorName)) throw new InvalidOperationException("Longa Arma already placed.");
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if (!player) throw new InvalidOperationException("Player is missing.");
            var source=EditorSceneManager.OpenScene(SourceScene,OpenSceneMode.Additive);
            GameObject actor=null;
            try
            {
                var original=source.GetRootGameObjects().Single(x=>x.name=="Approved Longa Arma Enemy Placement");
                var from=fuga.transform.position+Vector3.up*1f;
                var floor=Physics.RaycastAll(from,Vector3.down,5,~0,QueryTriggerInteraction.Ignore)
                    .Where(x=>!x.transform.IsChildOf(fuga.transform) && !x.transform.IsChildOf(player.transform) && x.normal.y>.45f)
                    .OrderBy(x=>x.distance).FirstOrDefault();
                if (!floor.collider) throw new InvalidOperationException("No walkable floor under Fuga placement.");
                var position=new Vector3(fuga.transform.position.x,floor.point.y+.02f,fuga.transform.position.z);
                if (Vector3.ProjectOnPlane(position-player.transform.position,Vector3.up).magnitude<1f)
                    throw new InvalidOperationException("Fuga placement overlaps player; do not guess another position.");
                actor=new GameObject(ActorName);actor.SetActive(false);SceneManager.MoveGameObjectToScene(actor,scene);
                actor.transform.SetPositionAndRotation(position,fuga.transform.rotation);
                var slots=new GameObject[States.Length];
                for (int i=0;i<States.Length;i++)
                {
                    var originalSlot=original.transform.Find(States[i]);
                    if (!originalSlot) throw new InvalidOperationException("Source slot missing: "+States[i]);
                    var copy=UnityEngine.Object.Instantiate(originalSlot.gameObject);
                    copy.SetActive(false);SceneManager.MoveGameObjectToScene(copy,scene);
                    copy.transform.SetParent(actor.transform,false);copy.name=States[i];
                    copy.transform.localPosition=Vector3.zero;
                    copy.transform.localRotation=GameplayFacing;
                    copy.transform.localScale=originalSlot.lossyScale;
                    // Presentation-only copies: no source review physics or drivers survive in Pegasus.
                    foreach (var component in copy.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(component);
                    foreach (var component in copy.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(component);
                    foreach (var component in copy.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(component);
                    var renderer=copy.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    if (!renderer) throw new InvalidOperationException("Source slot has no skin: "+States[i]);
                    renderer.updateWhenOffscreen=true;
                    copy.transform.position+=Vector3.up*(position.y-renderer.bounds.min.y);
                    var animator=copy.GetComponentInChildren<Animator>(true);
                    if (!animator || !animator.runtimeAnimatorController) throw new InvalidOperationException("Source slot has no animation: "+States[i]);
                    animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                    slots[i]=copy;
                }
                var physics=actor.AddComponent<Rigidbody>();physics.mass=4;physics.useGravity=true;
                physics.constraints=RigidbodyConstraints.FreezeRotation;
                physics.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                var hull=actor.AddComponent<CapsuleCollider>();hull.radius=.36f;hull.height=.9f;hull.center=Vector3.up*.45f;
                var view=actor.AddComponent<LongaArmaAnimationView>();view.Configure(slots);
                var brain=actor.AddComponent<LongaArmaBrain>();
                var sourceAgent=new SerializedObject(fuga.GetComponent<FugaBrain>()).FindProperty("navigationAgentType").intValue;
                brain.Configure(sourceAgent);
                SetMouthApproach(brain);
                slots[0].SetActive(true);actor.SetActive(true);
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                AssetDatabase.Refresh();PrefabUtility.SaveAsPrefabAsset(actor,PrefabPath);
                fuga.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(fuga);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Debug.Log($"Pegasus Longa Arma placed at {position:F3}; Fuga hidden. CargoRunMvp unchanged.");
            }
            catch { if (actor) UnityEngine.Object.DestroyImmediate(actor);throw; }
            finally { SceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(source,true); }
        }

        public static void Play() { if (!EditorApplication.isPlaying) EditorApplication.isPlaying=true; }
        const string ContactReviewPath="docs/validation/longa_engine_metal";
        static bool engineInteriorReview;
        static bool engineTourStarted;
        static bool reverseEngineTour;
        static bool enginePinReview;
        static float pinStarted;
        static Vector3 pinPosition;
        static bool pinWest;
        public static void ReviewEnginePin()
        {
            ReviewEngineInterior();enginePinReview=true;pinStarted=0;pinWest=false;
        }
        public static void ReviewEnginePinWest(){ReviewEnginePin();pinWest=true;}
        public static void ReviewEngineInterior()
        {
            ReviewEngineCorridor();engineInteriorReview=true;engineTourStarted=false;reverseEngineTour=false;
        }
        public static void ReviewEngineViaCockpit()
        {
            ReviewCockpitCorridor();engineInteriorReview=true;engineTourStarted=false;reverseEngineTour=false;
        }
        public static void ReviewEngineViaControl()
        {
            ReviewControlCorridor();engineInteriorReview=true;engineTourStarted=false;reverseEngineTour=true;
        }
        public static void InspectEngineInterior()
        {
            Directory.CreateDirectory(ContactReviewPath);
            var actor=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();
            var text=new System.Text.StringBuilder();
            foreach(var collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(!collider.enabled || collider.isTrigger || collider.bounds.center.x<26 || collider.bounds.center.x>43 || collider.bounds.center.z<7 || collider.bounds.center.z>23 || collider.bounds.center.y<3 || collider.bounds.center.y>7)continue;
                text.AppendLine($"{collider.transform.parent?.name}/{collider.name} {collider.GetType().Name} center={collider.bounds.center:F3} size={collider.bounds.size:F3}");
            }
            if(actor){var route=(Vector3[])typeof(LongaArmaBrain).GetField("route",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(actor);text.AppendLine("ROUTE "+string.Join(";",route??Array.Empty<Vector3>()));}
            if(actor && EditorApplication.isPlaying)
            {
                var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(actor).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
                var target=actor.CurrentTarget;var feet=target ? target.Surface.bounds.center : actor.transform.position;
                if(target)feet.y=target.Surface.bounds.min.y+.1f;
                foreach(float radius in new[]{.6f,.75f,1f,1.5f,2f,3f})
                {
                    bool from=UnityEngine.AI.NavMesh.SamplePosition(actor.transform.position,out var a,radius,filter);
                    bool to=UnityEngine.AI.NavMesh.SamplePosition(feet,out var b,radius,filter);
                    var path=new UnityEngine.AI.NavMeshPath();bool pathFound=from && to && UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,filter,path);
                    text.AppendLine($"NAV radius={radius} from={from}:{a.position:F3} to={to}:{b.position:F3} path={pathFound}:{path.status}");
                }
                foreach(var c in Physics.OverlapSphere(actor.transform.position+Vector3.up,2,~0,QueryTriggerInteraction.Ignore))
                    if(!c.transform.IsChildOf(actor.transform) && Physics.ComputePenetration(actor.GetComponent<CapsuleCollider>(),actor.transform.position,actor.transform.rotation,c,c.transform.position,c.transform.rotation,out var dir,out float depth))text.AppendLine($"OVERLAP {c.name} depth={depth} direction={dir}");
                var method=typeof(LongaArmaBrain).GetMethod("CanApproach",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                foreach(var wall in ParvumTarget.Active.Where(x=>x.IsRoomWall && x.Room.ToString()=="EngineRoom"))
                {
                    var args=new object[]{wall,null,false};bool reachable=(bool)method.Invoke(actor,args);
                    text.AppendLine($"WALL {wall.name} gap={Vector3.Distance(actor.transform.position,wall.ClosestPoint(actor.transform.position)):F3} reachable={reachable} reason={actor.ApproachDiagnostic}");
                }
            }
            File.WriteAllText(ContactReviewPath+"/engine_geometry.txt",text.ToString());
        }
        static Camera sequenceCamera;
        static RenderTexture sequenceTarget;
        static Texture2D sequenceSheet;
        static int sequenceFrame,sequenceIndex;
        static float sequenceNext;
        static string sequenceRun;
        static Vector3 sequenceOffset;
        // Explicitly enabled, editor-only survival aid for the approved movement review.
        static FirstPersonPlayerStatus survivalReviewPlayer;
        public static void EnableMovementReviewSurvival()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode required.");
            DisableMovementReviewSurvival();
            survivalReviewPlayer=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            if(!survivalReviewPlayer || survivalReviewPlayer.IsDead)
                throw new InvalidOperationException("Enable survival review before the player dies.");
            survivalReviewPlayer.DamageTaken+=KeepReviewPlayerAlive;
            EditorApplication.playModeStateChanged+=EndSurvivalReviewOnExit;
        }
        static void KeepReviewPlayerAlive()
        {
            if(survivalReviewPlayer && survivalReviewPlayer.CurrentHealth<=0)
                survivalReviewPlayer.SetVitalsForValidation(1,survivalReviewPlayer.CurrentShield);
        }
        public static void DisableMovementReviewSurvival()
        {
            if(survivalReviewPlayer)survivalReviewPlayer.DamageTaken-=KeepReviewPlayerAlive;
            survivalReviewPlayer=null;
            EditorApplication.playModeStateChanged-=EndSurvivalReviewOnExit;
        }
        static void EndSurvivalReviewOnExit(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.ExitingPlayMode)DisableMovementReviewSurvival();
        }
        public static void ObserveJitter()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode required.");
            EndJitter();Directory.CreateDirectory(ContactReviewPath);
            sequenceRun=DateTime.Now.ToString("HHmmss");sequenceIndex=0;sequenceNext=0;
            sequenceCamera=new GameObject("Temporary continuous Longa observation").AddComponent<Camera>();
            sequenceCamera.nearClipPlane=.03f;sequenceCamera.farClipPlane=80;sequenceCamera.fieldOfView=82;
            sequenceTarget=new RenderTexture(320,240,24);sequenceCamera.targetTexture=sequenceTarget;
            sequenceSheet=new Texture2D(1280,720,TextureFormat.RGB24,false);
            EditorApplication.update+=ObserveJitterFrame;
            EditorApplication.playModeStateChanged+=EndJitterOnExit;
        }
        static void EndJitterOnExit(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)EndJitter();}
        static void EndJitter()
        {
            EditorApplication.update-=ObserveJitterFrame;EditorApplication.playModeStateChanged-=EndJitterOnExit;
            if(sequenceCamera)UnityEngine.Object.DestroyImmediate(sequenceCamera.gameObject);
            if(sequenceTarget)UnityEngine.Object.DestroyImmediate(sequenceTarget);
            if(sequenceSheet)UnityEngine.Object.DestroyImmediate(sequenceSheet);
            sequenceFrame=0;
        }
        static void ObserveJitterFrame()
        {
            if(!EditorApplication.isPlaying || !sequenceCamera || Time.time<sequenceNext)return;
            var actor=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();if(!actor)return;
            var view=actor.GetComponent<LongaArmaAnimationView>();
            sequenceNext=Time.time+.10f;
            var center=view.Surface.bounds.center;
            if(sequenceFrame==0)
            {
                sequenceOffset=new[]{actor.transform.forward,-actor.transform.forward,actor.transform.right,-actor.transform.right}
                    .OrderBy(d=>Physics.RaycastAll(center,d,2.8f,~0,QueryTriggerInteraction.Ignore).Count(h=>!h.transform.IsChildOf(actor.transform))).First()*2.8f+Vector3.up*.55f;
            }
            // Follow translation only within each strip, so camera yaw cannot hide body oscillation.
            float cameraDistance=sequenceOffset.magnitude;
            foreach(var obstruction in Physics.RaycastAll(center,sequenceOffset.normalized,cameraDistance,~0,QueryTriggerInteraction.Ignore))
                if(!obstruction.transform.IsChildOf(actor.transform))cameraDistance=Mathf.Min(cameraDistance,Mathf.Max(.5f,obstruction.distance-.15f));
            sequenceCamera.transform.position=center+sequenceOffset.normalized*cameraDistance;sequenceCamera.transform.LookAt(center);
            if(enginePinReview && pinStarted>0)
            {
                var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
                if(player && player.PlayerCamera)sequenceCamera.transform.SetPositionAndRotation(player.PlayerCamera.position,player.PlayerCamera.rotation);
            }
            var previous=RenderTexture.active;
            sequenceCamera.Render();RenderTexture.active=sequenceTarget;
            sequenceSheet.ReadPixels(new Rect(0,0,320,240),(sequenceFrame%4)*320,(2-sequenceFrame/4)*240);
            RenderTexture.active=previous;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var route=(Vector3[])typeof(LongaArmaBrain).GetField("route",flags).GetValue(actor);
            var corner=(int)typeof(LongaArmaBrain).GetField("corner",flags).GetValue(actor);
            var mouth=view.MouthTip.position;
            float mouthGap=actor.CurrentTarget ? Vector3.Distance(mouth,actor.CurrentTarget.ClosestPoint(mouth)) : -1;
            File.AppendAllText(ContactReviewPath+"/sequence_"+sequenceRun+".txt",$"{sequenceIndex}/{sequenceFrame} t={Time.time:F4} room={corridorReviewRoom} state={actor.Behaviour} target={actor.CurrentTarget?.name} body={actor.transform.position:F4} yaw={actor.transform.eulerAngles.y:F3} slot={view.ActiveSlot.transform.localPosition:F4} tilt={view.ActiveSlot.transform.localEulerAngles:F3} velocity={actor.GetComponent<Rigidbody>().linearVelocity:F3} route={corner}/{route?.Length} goal={(route!=null && route.Length>0?route[route.Length-1]:Vector3.zero):F4} mouthGap={mouthGap:F4} frameMs={Time.unscaledDeltaTime*1000:F2} reason={actor.Diagnostic} approach={actor.ApproachDiagnostic}\n");
            if(++sequenceFrame<12)return;
            sequenceSheet.Apply();File.WriteAllBytes(ContactReviewPath+"/sequence_"+sequenceRun+"_"+sequenceIndex.ToString("D3")+".png",sequenceSheet.EncodeToPNG());
            sequenceIndex++;sequenceFrame=0;
        }
        static float nextContactSample;
        static int contactCycle=-1,contactFrame;
        public static void ObserveAttackContact()
        {
            Directory.CreateDirectory(ContactReviewPath);
            File.AppendAllText(ContactReviewPath+"/attack.txt","NEW RUN: actual animation and actual player input, no forced damage or pose.\n");
            nextContactSample=0;contactCycle=-1;contactFrame=0;
            EditorApplication.update-=ObserveContactFrame;EditorApplication.update+=ObserveContactFrame;
            ProvokeForReview();
        }
        static void ObserveContactFrame()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=ObserveContactFrame;return;}
            var actor=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();if(!actor)return;
            var view=actor.GetComponent<LongaArmaAnimationView>();
            if(view.Motion!=LongaArmaMotion.Attack || Time.time<nextContactSample)return;
            nextContactSample=Time.time+(view.MotionTime>.85f && view.MotionTime<1.15f ? .015f : .08f);
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            var local=actor.transform.InverseTransformPoint(view.BladeTip.position);
            File.AppendAllText(ContactReviewPath+"/attack.txt",$"time={Time.time:F3} cycle={view.AttackCycle} phase={view.MotionTime:F3} tipLocal={local:F3} tipWorld={view.BladeTip.position:F3} health={player.CurrentHealth} shield={player.CurrentShield} gap={actor.BladeSurfaceDistance:F4}\n");
            if(contactCycle<0)contactCycle=view.AttackCycle;
            var frames=new[]{0f,.4f,.8f,.94f,.98f,1.03f,1.3f,2.3f};
            if(view.AttackCycle!=contactCycle || contactFrame>=frames.Length || view.MotionTime<frames[contactFrame])return;
            ViewLive();File.Copy("Logs/PegasusLongaArmaLive.png",ContactReviewPath+"/attack_"+contactFrame+".png",true);contactFrame++;
        }
        public static void Stop() { if (EditorApplication.isPlaying) EditorApplication.isPlaying=false; }

        public static void ProvokeForReview()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play mode required.");
            ReviewDevices("AcquireReviewDevices");
            inputStarted=Time.time;nextClick=Time.time+2;inputFrame=-1;
            reviewInitialHits=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>().GetComponent<PegasusStickController>().HitCount;
            File.WriteAllText("Logs/LongaActualStickInput.txt","Actual keyboard/mouse input only; no target teleport or direct damage.\n");
            EditorWindow.GetWindow(typeof(EditorApplication).Assembly.GetType("UnityEditor.GameView")).Focus();
            InputSystem.onBeforeUpdate-=DriveActualInput;InputSystem.onBeforeUpdate+=DriveActualInput;
            EditorApplication.playModeStateChanged-=StopInputOnExit;EditorApplication.playModeStateChanged+=StopInputOnExit;
        }

        static float inputStarted,nextClick;
        static int reviewInitialHits;
        static bool retreatReview,retreatStarted;
        static bool deathReview;
        static float observedDeathStart=-1;
        public static void ProvokeDeath()
        {
            ProvokeForReview();deathReview=true;observedDeathStart=-1;observedStages.Clear();
            EditorApplication.update-=ObserveDeath;EditorApplication.update+=ObserveDeath;
        }
        static void ObserveDeath()
        {
            if(!EditorApplication.isPlaying || !deathReview)return;
            var actor=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();
            if(!actor)
            {
                File.AppendAllText("Logs/LongaActualStickInput.txt",$"DEATH_REMOVED time={Time.time:F3} elapsed={Time.time-observedDeathStart:F3}\n");
                EndInput();return;
            }
            if(actor.Health>0)return;
            if(observedDeathStart<0)observedDeathStart=Time.time;
            var view=actor.GetComponent<LongaArmaAnimationView>();
            string stage=view.MotionTime>3.25f ? "HoldLate" : view.MotionTime>2.45f ? "HoldMiddle" : view.MotionTime>1.96f ? "Liquid" : view.MotionTime>.8f ? "Collapse" : "Start";
            if(!observedStages.Add(stage))return;
            ViewLive();File.Copy("Logs/PegasusLongaArmaLive.png","Logs/LongaDeath"+stage+".png",true);
            File.AppendAllText("Logs/LongaActualStickInput.txt",$"DEATH {stage} time={Time.time:F3} elapsed={view.MotionTime:F3} normalized={view.Driver.GetCurrentAnimatorStateInfo(0).normalizedTime:F3} speed={view.Driver.speed:F3} collider={actor.GetComponent<Collider>().enabled}\n");
        }
        static Vector3[] retreatPath;
        static string corridorReviewRoom;
        public static void ReviewCockpitCorridor(){corridorReviewRoom="Cockpit";ProvokeRetreat();}
        public static void ReviewEngineCorridor(){corridorReviewRoom="Engine Room";ProvokeRetreat();}
        public static void ReviewControlCorridor(){corridorReviewRoom="Control Room";ProvokeRetreat();}
        public static void ReviewArmoryCorridor(){corridorReviewRoom="Armory";ProvokeRetreat();}
        public static void ReviewSupplyCorridor(){corridorReviewRoom="Supply Room";ProvokeRetreat();}
        public static void ReviewWarehouseCorridor(){ArchiveCorridor();corridorReviewRoom="Cargo Hold";ProvokeRetreat();}
        static void ArchiveCorridor()
        {
            if(string.IsNullOrEmpty(corridorReviewRoom)||!File.Exists("Logs/LongaActualStickInput.txt"))return;
            Directory.CreateDirectory(ContactReviewPath);
            var prefix=corridorReviewRoom.Replace(" ","")+"_"+DateTime.Now.ToString("HHmmss");
            File.Copy("Logs/LongaActualStickInput.txt",ContactReviewPath+"/"+prefix+".txt",true);
            foreach(var stage in observedStages)
            {var file="Logs/LongaSequence"+stage+".png";if(File.Exists(file))File.Copy(file,ContactReviewPath+"/"+prefix+"_"+stage+".png",true);}
        }
        static int retreatCorner;
        static float nextObservation;
        static readonly HashSet<string> observedStages=new HashSet<string>();
        public static void ProvokeRetreat()
        {
            if(File.Exists("Logs/LongaActualStickInput.txt"))File.Copy("Logs/LongaActualStickInput.txt","Logs/LongaPreviousRetreat.txt",true);
            ProvokeForReview();retreatReview=true;retreatStarted=false;retreatPath=null;nextObservation=0;observedStages.Clear();
            EditorApplication.update-=ObserveRetreat;EditorApplication.update+=ObserveRetreat;
        }
        static void ObserveRetreat()
        {
            if(!EditorApplication.isPlaying || !retreatReview)return;
            var actor=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();if(!actor)return;
            var view=actor.GetComponent<LongaArmaAnimationView>();string stage=null;
            if(retreatStarted && view.Motion==LongaArmaMotion.Attack)
                stage=view.MotionTime>2.25f ? "AttackEnd" : view.MotionTime>1.2f ? "AttackMiddle" : "AttackStart";
            else if(retreatStarted && actor.Behaviour==LongaArmaBehaviour.RelocateRoom)stage="Return";
            else if(actor.EntryCrossed)stage=actor.EntryDepth>1.5f ? "EntryLate" : "EntryEarly";
            else if(retreatStarted && view.Motion==LongaArmaMotion.Consume && !string.IsNullOrEmpty(actor.LastCompletedEntryName))stage="ConsumeAfterReturn";
            else if(retreatStarted && actor.Behaviour==LongaArmaBehaviour.PursueAttacker && view.Motion==LongaArmaMotion.Move)stage="Pursue";
            if(view.Motion==LongaArmaMotion.Move && actor.transform.position.y>-4 && actor.transform.position.y<-2)
                stage=actor.Behaviour==LongaArmaBehaviour.PursueAttacker ? "GroundRampUp" : "GroundRampReturn";
            if(view.Motion==LongaArmaMotion.Move && actor.transform.position.y>-3.5f && actor.transform.position.y<1.5f)
                stage="CorridorHeight"+Mathf.FloorToInt(actor.transform.position.y);
            if(stage==null || !observedStages.Add(stage))return;
            ViewLive();File.Copy("Logs/PegasusLongaArmaLive.png","Logs/LongaSequence"+stage+".png",true);
        }
        static int inputFrame;
        static void ReviewDevices(string method) => typeof(PegasusStickTools).GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,null);
        static void StopInputOnExit(PlayModeStateChange state) { if(state==PlayModeStateChange.ExitingPlayMode) EndInput(); }
        static void EndInput()
        {
            enginePinReview=false;
            engineInteriorReview=false;
            if(retreatReview)ArchiveCorridor();
            retreatReview=false;
            deathReview=false;EditorApplication.update-=ObserveDeath;
            EditorApplication.update-=ObserveRetreat;
            InputSystem.onBeforeUpdate-=DriveActualInput;
            if(Mouse.current!=null)InputSystem.QueueStateEvent(Mouse.current,new MouseState());
            if(Keyboard.current!=null)InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            ReviewDevices("ReleaseReviewDevices");
        }
        static void DriveActualInput()
        {
            if(!EditorApplication.isPlaying) { EndInput();return; }
            if(InputState.currentUpdateType!=InputUpdateType.Dynamic || inputFrame==Time.frameCount)return;
            inputFrame=Time.frameCount;
            var longa=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if(!longa || !player) { if(!deathReview)EndInput();return; }
            var stick=player.GetComponent<PegasusStickController>();
            if(Time.time-inputStarted>(retreatReview ? 180 : 22))
            {
                File.AppendAllText("Logs/LongaActualStickInput.txt",$"END t={Time.time:F3} hits={stick.HitCount} health={longa.Health} peakSweepMs={stick.PeakSweepMilliseconds:F3} captureMs={stick.PeakCaptureMilliseconds:F3} damageMs={stick.PeakDamageMilliseconds:F3} bakeMs={stick.PeakBakeMilliseconds:F3} refitMs={stick.PeakRefitMilliseconds:F3}\n");
                EndInput();return;
            }
            ReviewDevices("EnableReviewDevices");
            var camera=player.GetComponentInChildren<Camera>();
            var center=longa.GetComponent<LongaArmaAnimationView>().Surface.bounds.center;
            var look=Quaternion.LookRotation(center-camera.transform.position).eulerAngles;
            var settings=(FirstPersonPlayerSettings)new SerializedObject(player).FindProperty("settings").objectReferenceValue;
            float yaw=Mathf.DeltaAngle(camera.transform.eulerAngles.y,look.y);
            float pitch=Mathf.DeltaAngle(camera.transform.eulerAngles.x,look.x);
            float distance=Vector3.ProjectOnPlane(center-player.transform.position,Vector3.up).magnitude;
            var mouse=new MouseState { delta=new Vector2(Mathf.Clamp(yaw,-4,4),-Mathf.Clamp(pitch,-4,4))/settings.MouseSensitivity };
            if((deathReview || stick.HitCount==reviewInitialHits) && longa.Health>0 && distance<1.5f && Time.time>=nextClick)
            { mouse=mouse.WithButton(MouseButton.Left);nextClick=Time.time+(deathReview?1.4f:2.5f);File.AppendAllText("Logs/LongaActualStickInput.txt",$"CLICK t={Time.time:F3} distance={distance:F3} hits={stick.HitCount} health={longa.Health} state={longa.Behaviour} peakSweepMs={stick.PeakSweepMilliseconds:F3}\n"); }
            var keys=distance>1.05f && Mathf.Abs(yaw)<20 ? new KeyboardState(Key.W) : new KeyboardState();
            if(retreatReview && !retreatStarted && distance>2.2f)
            {
                var approach=new UnityEngine.AI.NavMeshPath();
                var approachFilter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(longa).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
                if(UnityEngine.AI.NavMesh.SamplePosition(player.transform.position,out var approachStart,1,approachFilter) &&
                   UnityEngine.AI.NavMesh.SamplePosition(longa.transform.position,out var approachEnd,1,approachFilter) &&
                   UnityEngine.AI.NavMesh.CalculatePath(approachStart.position,approachEnd.position,approachFilter,approach) && approach.corners.Length>1)
                {
                    int next=1;while(next<approach.corners.Length-1 && Vector3.ProjectOnPlane(approach.corners[next]-player.transform.position,Vector3.up).magnitude<.35f)next++;
                    var walk=Vector3.ProjectOnPlane(approach.corners[next]-player.transform.position,Vector3.up);
                    float turn=Mathf.DeltaAngle(camera.transform.eulerAngles.y,Quaternion.LookRotation(walk).eulerAngles.y);
                    mouse=new MouseState{delta=new Vector2(Mathf.Clamp(turn,-8,8),Mathf.Clamp(Mathf.DeltaAngle(0,camera.transform.eulerAngles.x),-4,4))/settings.MouseSensitivity};
                    keys=Mathf.Abs(turn)<20 ? new KeyboardState(Key.W) : new KeyboardState();
                }
            }
            var view=longa.GetComponent<LongaArmaAnimationView>();
            if(retreatReview && retreatStarted && corridorReviewRoom=="Cargo Hold" && longa.transform.position.y < -3.8f &&
                (longa.EntryCrossed && longa.EntryDepth>=2f && longa.ActiveEntryName.Contains("Cargo Hold") || !string.IsNullOrEmpty(longa.LastCompletedEntryName) && longa.LastCompletedEntryName.Contains("Cargo Hold")))
            {
                File.AppendAllText("Logs/LongaActualStickInput.txt",$"ROUNDTRIP_FINISHED actor={longa.transform.position:F3} depth={longa.EntryDepth:F3}\n");
                EndInput();return;
            }
            if(retreatReview && retreatStarted && corridorReviewRoom=="Cargo Hold" && longa.Health>30 &&
                (longa.Behaviour==LongaArmaBehaviour.Consume || longa.Behaviour==LongaArmaBehaviour.ApproachMetal))
            {
                retreatStarted=false;retreatPath=null;reviewInitialHits=stick.HitCount;
                File.AppendAllText("Logs/LongaActualStickInput.txt","Sight lost on return; approach for another real stick hit.\n");
            }
            if(retreatReview && retreatStarted && corridorReviewRoom!="Cargo Hold" && retreatPath!=null && retreatCorner>=retreatPath.Length &&
                (longa.EntryCrossed && longa.EntryDepth>=2f && longa.ActiveEntryName.Contains(corridorReviewRoom) || !string.IsNullOrEmpty(longa.LastCompletedEntryName) && longa.LastCompletedEntryName.Contains(corridorReviewRoom)))
            {
                if(engineInteriorReview)
                {
                    if(corridorReviewRoom!="Engine Room")
                    {
                        ArchiveCorridor();corridorReviewRoom="Engine Room";retreatStarted=false;retreatPath=null;
                        File.AppendAllText("Logs/LongaActualStickInput.txt","Continue actual input to Engine Room via upper doorway.\n");return;
                    }
                    if(engineTourStarted && !enginePinReview){File.AppendAllText("Logs/LongaActualStickInput.txt","ENGINE TOUR finished; release input for natural AI.\n");EndInput();return;}
                    if(engineTourStarted && enginePinReview && pinStarted==0){pinStarted=Time.time;pinPosition=player.transform.position;}
                    if(!engineTourStarted)
                    {
                    engineTourStarted=true;
                    var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(longa).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
                    var points=new[]{new Vector3(29.7f,3.3f,10),new Vector3(27.9f,3.3f,14.5f),new Vector3(29.7f,3.3f,19),new Vector3(34.2f,3.3f,20.8f),new Vector3(39.2f,3.3f,18.5f),new Vector3(40.5f,3.3f,14.5f),new Vector3(39.2f,3.3f,10.5f),new Vector3(34.2f,3.3f,8.2f)};
                    if(reverseEngineTour)Array.Reverse(points);
                    if(enginePinReview)points=pinWest ? new[]{new Vector3(30f,3.3f,10f),new Vector3(28f,3.3f,14.5f)} : new[]{new Vector3(39.2f,3.3f,10.5f),new Vector3(40.5f,3.3f,14.5f),new Vector3(39.2f,3.3f,18.5f),new Vector3(34.2f,3.3f,21f)};
                    var tour=new List<Vector3>();var from=player.transform.position;
                    foreach(var point in points)
                    {
                        var path=new UnityEngine.AI.NavMeshPath();
                        if(!UnityEngine.AI.NavMesh.SamplePosition(from,out var start,1,filter) || !UnityEngine.AI.NavMesh.SamplePosition(point,out var end,1,filter) || !UnityEngine.AI.NavMesh.CalculatePath(start.position,end.position,filter,path))continue;
                        tour.AddRange(path.corners.Skip(1));from=end.position;
                    }
                    retreatPath=tour.ToArray();retreatCorner=0;
                    File.AppendAllText("Logs/LongaActualStickInput.txt","ENGINE TOUR input waypoints="+string.Join(";",retreatPath)+"\n");
                    }
                }
                else
                {
                ArchiveCorridor();File.AppendAllText("Logs/LongaActualStickInput.txt","RETURN TO WAREHOUSE: actual player input continues.\n");
                corridorReviewRoom="Cargo Hold";retreatStarted=false;retreatPath=null;observedStages.Clear();
                if(longa.Behaviour!=LongaArmaBehaviour.PursueAttacker && longa.Behaviour!=LongaArmaBehaviour.Attack)reviewInitialHits=stick.HitCount;
                }
            }
            if(retreatReview && stick.HitCount>reviewInitialHits)
            {
                if(!retreatStarted)
                {
                    retreatStarted=true;float best=float.PositiveInfinity;
                    var filter=new UnityEngine.AI.NavMeshQueryFilter { agentTypeID=new SerializedObject(longa).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas };
                    foreach(var door in longa.Doorways.Where(x=>(corridorReviewRoom=="Cargo Hold" || !x.Name.Contains("Warehouse")) && (string.IsNullOrEmpty(corridorReviewRoom)||x.Name.Contains(corridorReviewRoom))))
                    {
                      foreach(float depth in new[]{3f,3.5f,4f})
                      {
                        var point=door.Point+door.Inward*depth;
                        var floor=Physics.RaycastAll(point+Vector3.up,Vector3.down,20,~0,QueryTriggerInteraction.Ignore)
                            .Where(x=>x.normal.y>.5f && x.point.y<door.Point.y-.5f && !x.transform.IsChildOf(longa.transform) && !x.transform.IsChildOf(player.transform)).OrderBy(x=>x.distance).FirstOrDefault();
                        if(!floor.collider)continue;point.y=floor.point.y;
                        var path=new UnityEngine.AI.NavMeshPath();
                        if(!UnityEngine.AI.NavMesh.SamplePosition(player.transform.position,out var start,2,filter) ||
                           !UnityEngine.AI.NavMesh.SamplePosition(point,out var end,1.5f,filter) ||
                           !UnityEngine.AI.NavMesh.CalculatePath(start.position,end.position,filter,path) || path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)continue;
                        float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                        if(length>=best)continue;best=length;retreatPath=path.corners;retreatCorner=1;
                      }
                    }
                    if(retreatPath!=null)
                    {
                        bool returnFromEngine=corridorReviewRoom=="Cargo Hold" && player.transform.position.x<43 && player.transform.position.y>2 && player.transform.position.z>5;
                        if(corridorReviewRoom=="Armory" || returnFromEngine || corridorReviewRoom=="Cargo Hold" && player.transform.position.z < -10 && player.transform.position.x<62)
                        {
                            // Observed clear warehouse-side approach, before the armory bend.
                            var via=corridorReviewRoom=="Armory" ? new Vector3(74.316f,-4.423f,.727f) : returnFromEngine ? new Vector3(37.68f,3.3f,8.16f) : new Vector3(59.15f,2.8f,-14.44f);
                            var first=new UnityEngine.AI.NavMeshPath();var second=new UnityEngine.AI.NavMeshPath();
                            if(UnityEngine.AI.NavMesh.SamplePosition(via,out var viaHit,1.5f,filter) &&
                                UnityEngine.AI.NavMesh.CalculatePath(retreatPath[0],viaHit.position,filter,first) && first.status==UnityEngine.AI.NavMeshPathStatus.PathComplete &&
                                UnityEngine.AI.NavMesh.CalculatePath(viaHit.position,retreatPath[retreatPath.Length-1],filter,second) && second.status==UnityEngine.AI.NavMeshPathStatus.PathComplete)
                                retreatPath=first.corners.Concat(second.corners.Skip(1)).ToArray();
                        }
                        // Walk the player around wall corners rather than along the NavMesh edge.
                        // This changes input waypoints only, never the actor or scene geometry.
                        for(int i=1;i<retreatPath.Length-1;i++)
                        for(int pass=0;pass<4;pass++)
                        {
                            var probe=retreatPath[i]+Vector3.up*.9f;
                            foreach(var obstacle in Physics.OverlapSphere(probe,1.2f,~0,QueryTriggerInteraction.Ignore))
                            {
                                if(obstacle.transform.IsChildOf(longa.transform)||obstacle.transform.IsChildOf(player.transform))continue;
                                if(obstacle is MeshCollider meshCollider && !meshCollider.convex)continue;
                                var away=probe-obstacle.ClosestPoint(probe);var planar=Vector3.ProjectOnPlane(away,Vector3.up);
                                if(Mathf.Abs(away.y)<.2f && planar.magnitude>.001f && planar.magnitude<.95f)
                                    retreatPath[i]+=planar.normalized*(.95f-planar.magnitude);
                            }
                        }
                    }
                    File.AppendAllText("Logs/LongaActualStickInput.txt",$"RETREAT path={string.Join(";",retreatPath??Array.Empty<Vector3>())}\n");
                    if(retreatPath==null){File.AppendAllText("Logs/LongaActualStickInput.txt","NO REVIEW PATH; input released. Doors: "+string.Join(";",longa.Doorways.Select(d=>d.Name))+"\n");EndInput();return;}
                }
                keys=new KeyboardState();mouse=new MouseState();
                if(retreatPath!=null && retreatCorner<retreatPath.Length)
                {
                    var delta=Vector3.ProjectOnPlane(retreatPath[retreatCorner]-player.transform.position,Vector3.up);
                    if(delta.magnitude<.25f)retreatCorner++;
                    else
                    {
                        var toActor=Vector3.ProjectOnPlane(longa.transform.position-player.transform.position,Vector3.up);
                        float ahead=Vector3.Dot(toActor,delta.normalized);
                        if(!enginePinReview && corridorReviewRoom=="Cargo Hold" && ahead>.6f && ahead<3 && (toActor-delta.normalized*ahead).magnitude<1.2f)
                        {
                            // Keyboard-steer around the live pursuer instead of trying to walk through it.
                            var side=Vector3.Cross(Vector3.up,delta.normalized);
                            var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=new SerializedObject(longa).FindProperty("navigationAgentType").intValue,areaMask=UnityEngine.AI.NavMesh.AllAreas};
                            if(UnityEngine.AI.NavMesh.SamplePosition(player.transform.position,out var floor,1,filter) &&
                                UnityEngine.AI.NavMesh.Raycast(floor.position,floor.position+side*1.5f,out _,filter))side=-side;
                            delta=toActor+side*1.6f;
                        }
                        float turn=Mathf.DeltaAngle(camera.transform.eulerAngles.y,Quaternion.LookRotation(delta).eulerAngles.y);
                        mouse=new MouseState { delta=new Vector2(Mathf.Clamp(turn,-8,8),Mathf.Clamp(Mathf.DeltaAngle(0,camera.transform.eulerAngles.x),-4,4))/settings.MouseSensitivity };
                        if(Mathf.Abs(turn)<20)keys=new KeyboardState(Key.W);
                        var room=typeof(LongaArmaBrain).GetMethod("RoomBelow",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(longa,new object[]{longa.transform.position});
                        if(view.Motion!=LongaArmaMotion.Attack && Vector3.Distance(player.transform.position,longa.transform.position)>(engineTourStarted && engineInteriorReview ? 4.2f : 2.7f))keys=new KeyboardState();
                        var predicted=player.transform.position+Vector3.up*.9f+delta.normalized*.35f;
                        var sight=predicted-view.MouthTip.position;
                        if(distance>2.4f && view.Motion!=LongaArmaMotion.Attack && Physics.RaycastAll(view.MouthTip.position,sight.normalized,sight.magnitude,~0,QueryTriggerInteraction.Ignore)
                            .Any(h=>!h.transform.IsChildOf(longa.transform)&&!h.transform.IsChildOf(player.transform)))keys=new KeyboardState();
                    }
                }
                else if(retreatPath!=null && distance<3.5f && (corridorReviewRoom!="Engine Room" || engineInteriorReview))
                {
                    var away=Vector3.ProjectOnPlane(player.transform.position-longa.transform.position,Vector3.up);
                    float turn=Mathf.DeltaAngle(camera.transform.eulerAngles.y,Quaternion.LookRotation(away).eulerAngles.y);
                    mouse=new MouseState{delta=new Vector2(Mathf.Clamp(turn,-8,8),0)/settings.MouseSensitivity};
                    if(Mathf.Abs(turn)<20)keys=new KeyboardState(Key.W);
                }
            }
            if(retreatReview && Time.time>=nextObservation)
            {
                nextObservation=Time.time+.25f;
                File.AppendAllText("Logs/LongaActualStickInput.txt",$"PLAYER health={player.GetComponent<FirstPersonPlayerStatus>().CurrentHealth} {player.MovementDiagnostic}\n");
                var target=player.GetComponent<ParvumTarget>();var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                bool oldSight=(bool)typeof(LongaArmaBrain).GetMethod("VisibleFrom",flags).Invoke(longa,new object[]{longa.transform.position+Vector3.up*.45f,target});
                bool newSight=(bool)typeof(LongaArmaBrain).GetMethod("Visible",flags).Invoke(longa,new object[]{target});
                File.AppendAllText("Logs/LongaActualStickInput.txt",$"SIGHT t={Time.time:F3} oldLowRay={oldSight} headSight={newSight} distance={Vector3.Distance(player.transform.position,longa.transform.position):F3}\n");
                if(!newSight)
                {
                    var origin=view.MouthTip.position;var delta=target.Surface.bounds.center-origin;
                    var blockers=Physics.RaycastAll(origin,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore)
                        .Where(h=>!h.transform.IsChildOf(longa.transform) && h.collider!=target.Surface && !h.transform.IsChildOf(target.transform)).OrderBy(h=>h.distance);
                    File.AppendAllText("Logs/LongaActualStickInput.txt","SIGHT_BLOCKERS "+string.Join(";",blockers.Select(h=>$"{h.collider.name}@{h.distance:F3}"))+"\n");
                }
                File.AppendAllText("Logs/LongaActualStickInput.txt",$"t={Time.time:F3} state={longa.Behaviour} motion={view.Motion} phase={view.MotionTime:F3} actor={longa.transform.position:F3} velocity={longa.GetComponent<Rigidbody>().linearVelocity.magnitude:F3} player={player.transform.position:F3} entry={longa.ActiveEntryName} depth={longa.EntryDepth:F3} completed={longa.LastCompletedEntryName} advance={longa.LastCompletedEntryAdvance:F3} diagnostic={longa.Diagnostic}\n");
            }
            if(enginePinReview && pinStarted>0)
            {
                float elapsed=Time.time-pinStarted;
                mouse=new MouseState{delta=new Vector2(Mathf.Clamp(yaw,-4,4),-Mathf.Clamp(pitch,-4,4))/settings.MouseSensitivity};
                keys=elapsed<3 ? new KeyboardState(Key.S) : elapsed<5 ? new KeyboardState(Key.D) : elapsed<7 ? new KeyboardState(Key.A) : new KeyboardState(Key.W);
                File.AppendAllText(ContactReviewPath+"/pin_"+sequenceRun+".txt",$"t={elapsed:F3} player={player.transform.position:F3} displacement={Vector3.Distance(player.transform.position,pinPosition):F3} actor={longa.transform.position:F3} state={longa.Behaviour} health={player.GetComponent<FirstPersonPlayerStatus>().CurrentHealth} {player.MovementDiagnostic}\n");
                if(elapsed>9){EndInput();return;}
            }
            InputSystem.QueueStateEvent(Mouse.current,mouse);
            InputSystem.QueueStateEvent(Keyboard.current,keys);
        }

        public static void DefeatForReview()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play mode required.");
            var longa=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var attacker=player ? player.GetComponent<ParvumTarget>() : null;
            if (!longa || !attacker) throw new InvalidOperationException("Live actor or player attack source is missing.");
            longa.ReceiveDamage(longa.Health,attacker);
            Debug.Log($"Longa death review event: state={longa.Behaviour}, collider={longa.GetComponent<CapsuleCollider>().enabled}.");
        }

        public static void ReviewDeathPresence()
        {
            var scene=SceneManager.GetActiveScene();
            var entries=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");
            object[] counts={0,0,0};entries.GetMethod("GetCountsByType").Invoke(null,counts);
            Debug.Log($"Editor state: scene={scene.path} play={EditorApplication.isPlaying} paused={EditorApplication.isPaused} dirty={scene.isDirty} compiling={EditorApplication.isCompiling} consoleErrors={counts[0]} consoleWarnings={counts[1]}");
            var actor=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>(FindObjectsInactive.Include);
            Debug.Log($"Longa death presence: play={EditorApplication.isPlaying} time={Time.time:F3} actor={(actor?actor.name:"REMOVED")} state={(actor?actor.Behaviour.ToString():"NONE")} collider={(actor&&actor.GetComponent<CapsuleCollider>().enabled)}");
        }

        static void SetMouthApproach(LongaArmaBrain brain)
        {
            var serialized=new SerializedObject(brain);
            serialized.FindProperty("mouthContactApproach").floatValue=.025f;
            serialized.FindProperty("combatApproach").floatValue=MeasureBladeReach(brain.gameObject);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static float MeasureBladeReach(GameObject actor)
        {
            // Sample a disposable copy only; never force poses on the live validation target.
            var copy=UnityEngine.Object.Instantiate(actor.transform.Find(States[2]).gameObject);
            copy.hideFlags=HideFlags.HideAndDontSave;copy.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            copy.transform.localScale=actor.transform.Find(States[2]).lossyScale;
            var baked=new Mesh();
            try
            {
                var skin=copy.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x=>x.enabled && LongaArmaAnimationView.VisibleWithin(x.transform,copy.transform));
                var animator=skin.GetComponentInParent<Animator>(true);
                var clip=animator.runtimeAnimatorController.animationClips.Single();
                var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;
                bool Blade(int index)=>index<bones.Length && bones[index] && bones[index].name.StartsWith("R_frontleg",StringComparison.Ordinal);
                var indices=Enumerable.Range(0,weights.Length).Where(i=>
                    weights[i].weight0>.05f && Blade(weights[i].boneIndex0) || weights[i].weight1>.05f && Blade(weights[i].boneIndex1) ||
                    weights[i].weight2>.05f && Blade(weights[i].boneIndex2) || weights[i].weight3>.05f && Blade(weights[i].boneIndex3)).ToArray();
                float reach=0;var vertices=new List<Vector3>();
                for(int frame=0;frame<=30;frame++)
                {
                    clip.SampleAnimation(animator.gameObject,clip.length*frame/30f);skin.BakeMesh(baked,true);baked.GetVertices(vertices);
                    var matrix=skin.transform.localToWorldMatrix;
                    foreach(int index in indices)
                    {var point=matrix.MultiplyPoint3x4(vertices[index]);reach=Mathf.Max(reach,new Vector2(point.x,point.z).magnitude);}
                }
                Debug.Log($"Visible authored left-blade approach reach={reach:F3}m; actual animated contact remains required for damage.");
                return Mathf.Min(reach,Bellerophon.Core.Session.SeedIntruderRules.LongaArmaAttackRange);
            }
            finally { UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(copy); }
        }

        public static void AlignExisting()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode required.");
            var scene=SceneManager.GetActiveScene();
            if (scene.name!="Pegasus" || scene.isDirty) throw new InvalidOperationException("Clean Pegasus edit scene required.");
            var actor=scene.GetRootGameObjects().Single(x=>x.name==ActorName);
            AlignVisibleSlots(actor);
            SetMouthApproach(actor.GetComponent<LongaArmaBrain>());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                AlignVisibleSlots(prefab);
                SetMouthApproach(prefab.GetComponent<LongaArmaBrain>());
                PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
            Debug.Log("Longa Arma Pegasus presentation aligned to gameplay forward; original source untouched.");
        }

        public static void EnlargeExisting()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
            var scene=SceneManager.GetActiveScene();
            if(scene.name!="Pegasus" || scene.isDirty)throw new InvalidOperationException("Clean Pegasus required.");
            var actor=scene.GetRootGameObjects().Single(x=>x.name==ActorName);
            if(actor.transform.localScale!=Vector3.one)throw new InvalidOperationException("Expected original scale 1; do not apply twice.");
            actor.transform.localScale=Vector3.one*2;
            SetMouthApproach(actor.GetComponent<LongaArmaBrain>());
            var prefab=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                prefab.transform.localScale=actor.transform.localScale;
                SetMouthApproach(prefab.GetComponent<LongaArmaBrain>());
                PrefabUtility.SaveAsPrefabAsset(prefab,PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Only Longa gameplay actor/prefab scaled to 2; capsule scales with root; blade approach remeasured.");
        }

        static void AlignVisibleSlots(GameObject actor)
        {
            foreach(var state in States)
            {
                var slot=actor.transform.Find(state);
                var skin=slot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x=>x.enabled && LongaArmaAnimationView.VisibleWithin(x.transform,slot));
                var animator=skin.GetComponentInParent<Animator>(true);
                foreach(var driver in slot.GetComponentsInChildren<Animator>(true))
                { driver.enabled=driver==animator;if(driver!=animator)driver.runtimeAnimatorController=null; }
                slot.localRotation=Quaternion.identity;
                var head=skin.bones.Single(x=>x.name=="head");
                var chest=skin.bones.Single(x=>x.name=="chest");
                var front=Vector3.ProjectOnPlane(actor.transform.InverseTransformDirection(head.position-chest.position),Vector3.up);
                // The visible walking FBX faces +Z; head lean is authored motion, not root facing.
                slot.localRotation=Quaternion.identity;
                Debug.Log($"Visible alignment {state}: front={front:F3} rotation={slot.localEulerAngles:F2} mouth={actor.transform.InverseTransformPoint(skin.bones.Single(x=>x.name=="headend").position):F3} renderer={skin.name} controller={animator.runtimeAnimatorController.name}");
            }
        }

        public static void Review()
        {
            if(!EditorApplication.isPlaying){ReviewDeathPresence();return;}
            var scene=SceneManager.GetActiveScene();
            var longa=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>(FindObjectsInactive.Include);
            var fuga=UnityEngine.Object.FindFirstObjectByType<FugaBrain>(FindObjectsInactive.Include);
            var player=UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var stick=player ? player.GetComponent<PegasusStickController>() : null;
            var playerStatus=player ? player.GetComponent<FirstPersonPlayerStatus>() : null;
            if (!longa || !fuga) throw new InvalidOperationException("Longa Arma/Fuga not found.");
            var samples=longa.AttackTimings;
            if(samples.Count>0)
            {
                var costs=samples.Select(s=>s.x).OrderBy(x=>x).ToArray();
                var frames=samples.Select(s=>s.z).OrderBy(x=>x).ToArray();
                Debug.Log($"ATTACK_PROFILE frames={samples.Count} cpuMeanMs={costs.Average():F3} cpuP95Ms={costs[(int)((costs.Length-1)*.95f)]:F3} cpuMaxMs={costs.Last():F3} allocatedMeanBytes={samples.Average(s=>s.y):F0} allocatedMaxBytes={samples.Max(s=>s.y):F0} frameMedianMs={frames[frames.Length/2]:F3} frameP95Ms={frames[(int)((frames.Length-1)*.95f)]:F3} frameMaxMs={frames.Last():F3}");
            }
            var view=longa.GetComponent<LongaArmaAnimationView>();
            var metal=ParvumTarget.Active.Where(x=>x.IsMetal && x.IsAlive).OrderBy(x=>Vector3.Distance(longa.transform.position,x.ClosestPoint(longa.transform.position))).Take(3)
                .Select(x=>$"{x.name}:{Vector3.Distance(longa.transform.position,x.ClosestPoint(longa.transform.position)):F2} bounds={x.Surface.bounds.min:F2}/{x.Surface.bounds.max:F2}");
            var entries=typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries");
            object[] counts={0,0,0};entries.GetMethod("GetCountsByType").Invoke(null,counts);
            var probe=typeof(LongaArmaBrain).GetMethod("CanApproach",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var attempts=new StringBuilder();
            foreach (var food in ParvumTarget.Active.Where(x=>x.IsMetal && x.IsAlive).OrderBy(x=>Vector3.Distance(longa.transform.position,x.ClosestPoint(longa.transform.position))).Take(3))
            {
                object[] args={food,null,false};bool reachable=(bool)probe.Invoke(longa,args);
                attempts.Append($" [{food.name} {reachable} {longa.ApproachDiagnostic}]");
            }
            var mouth=view.MouthTip;
            var chosen=longa.CurrentTarget;
            var mouthGap=mouth && chosen ? Vector3.Distance(mouth.position,chosen.ClosestPoint(mouth.position)) : -1;
            var facing=mouth ? Vector3.Dot(longa.transform.forward,Vector3.ProjectOnPlane(mouth.position-longa.transform.position,Vector3.up).normalized) : 0;
            var durability=chosen && chosen.Ship ? chosen.Kind==ParvumTargetKind.MetalCargo ? chosen.Ship.CurrentCargoState.DurabilityPercent.ToString("F2") : chosen.Ship.CurrentShipState.GetRoom(chosen.Room).CurrentDurability.ToString() : "NONE";
            var mouthContactAt=(float)typeof(LongaArmaBrain).GetField("mouthContactAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(longa);
            var consumeStartedAt=(float)typeof(LongaArmaBrain).GetField("consumeStartedAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(longa);
            var mouthContactApproach=(float)typeof(LongaArmaBrain).GetField("mouthContactApproach",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(longa);
            var route=(Vector3[])typeof(LongaArmaBrain).GetField("route",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(longa);
            var corner=(int)typeof(LongaArmaBrain).GetField("corner",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(longa);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var raised=(bool)typeof(LongaArmaBrain).GetField("raised",flags).GetValue(longa);
            var sampledCycle=(int)typeof(LongaArmaBrain).GetField("sampledCycle",flags).GetValue(longa);
            var lastStrikeCycle=(int)typeof(LongaArmaBrain).GetField("lastStrikeCycle",flags).GetValue(longa);
            var initialBladeTip=(Vector3)typeof(LongaArmaBrain).GetField("initialBladeTip",flags).GetValue(longa);
            var bladeTip=view.BladeTip;
            var animator=view.Driver;
            Debug.Log($"Longa animation review: animator={(animator?animator.name:"NONE")} enabled={(animator&&animator.enabled)} speed={(animator?animator.speed:0):F3} normalized={(animator?animator.GetCurrentAnimatorStateInfo(0).normalizedTime:0):F3} clip={(animator&&animator.runtimeAnimatorController?string.Join(",",animator.runtimeAnimatorController.animationClips.Select(c=>c.name)):"NONE")} bone={(bladeTip?bladeTip.position.ToString("F3"):"NONE")}");
            Debug.Log($"Longa review: scene={scene.path} play={EditorApplication.isPlaying} paused={EditorApplication.isPaused} time={Time.time:F3} timeScale={Time.timeScale:F2} dirty={scene.isDirty} compiling={EditorApplication.isCompiling} actor={longa.transform.position:F3} forward={longa.transform.forward:F3} health={longa.Health:F2} state={longa.Behaviour} motion={view.Motion} motionTime={view.MotionTime:F3} target={(chosen?chosen.name:"NONE")} durability={durability} mouth={(mouth?mouth.position.ToString("F3"):"NONE")} mouthOffset={(mouth?longa.transform.InverseTransformPoint(mouth.position).ToString("F3"):"NONE")} mouthGap={mouthGap:F3} mouthThreshold={mouthContactApproach:F3} mouthContactAt={mouthContactAt:F3} consumeStart={consumeStartedAt:F3} facing={facing:F3} route={corner}/{(route==null?0:route.Length)} routeGoal={(route!=null && route.Length>0?route[route.Length-1].ToString("F3"):"NONE")} entry={longa.ActiveEntryName} crossed={longa.EntryCrossed} depth={longa.EntryDepth:F3} goal={longa.EntryGoal:F3} entryInfo={longa.EntryDiagnostic} completed={longa.LastCompletedEntryName} advance={longa.LastCompletedEntryAdvance:F3} bladeGap={longa.BladeSurfaceDistance:F3} bladeTip={(bladeTip?bladeTip.position.ToString("F3"):"NONE")} bladeRise={(bladeTip?bladeTip.position.y-initialBladeTip.y:0):F3} raised={raised} cycle={sampledCycle}/{lastStrikeCycle} playerStatus={(playerStatus?$"{playerStatus.CurrentHealth:F1}/{playerStatus.CurrentShield:F1}":"NONE")} nearestMetal={string.Join(";",metal)} approach={attempts} diagnostic={longa.Diagnostic} player={(player?player.transform.position.ToString("F3"):"NONE")} stickHit={(stick?stick.HitCount:-1)} stickLast={(stick?stick.LastHit:"NONE")} FugaActive={fuga.gameObject.activeInHierarchy} consoleErrors={counts[0]} consoleWarnings={counts[1]}");
        }

        // Transient live observation frame; the final evidence capture is a separate one-time command.
        public static void ViewLive()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Live view requires Play mode.");
            var longa=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();
            if (!longa) throw new InvalidOperationException("Longa Arma is not active.");
            var skin=longa.GetComponent<LongaArmaAnimationView>().Surface;
            var cameraObject=new GameObject("Temporary Longa Arma live camera");
            var camera=cameraObject.AddComponent<Camera>();
            camera.nearClipPlane=.03f;camera.farClipPlane=100;camera.fieldOfView=82;
            var target=new RenderTexture(960,720,24);
            var image=new Texture2D(1920,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                var center=skin.bounds.center;
                var directions=new[] { longa.transform.forward,-longa.transform.forward,longa.transform.right,-longa.transform.right,
                    (longa.transform.forward+longa.transform.right).normalized,(-longa.transform.forward+longa.transform.right).normalized,
                    (longa.transform.forward-longa.transform.right).normalized,(-longa.transform.forward-longa.transform.right).normalized }
                    .OrderBy(x=>Physics.RaycastAll(center,x,2.8f,~0,QueryTriggerInteraction.Ignore)
                        .Count(h=>!h.transform.IsChildOf(longa.transform))).Take(2).ToArray();
                for (int i=0;i<2;i++)
                {
                    camera.transform.position=center+directions[i]*2.8f+Vector3.up*.55f;
                    camera.transform.LookAt(center);
                    camera.Render();RenderTexture.active=target;
                    image.ReadPixels(new Rect(0,0,960,720),i*960,0);
                }
                image.Apply();File.WriteAllBytes("Logs/PegasusLongaArmaLive.png",image.EncodeToPNG());
                Debug.Log("Live Longa Arma front/side frame refreshed for direct review.");
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=null;
                UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        public static void CaptureFinal()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Final capture requires Play mode.");
            var longa=UnityEngine.Object.FindFirstObjectByType<LongaArmaBrain>();
            if (!longa) throw new InvalidOperationException("Longa Arma is not active.");
            var skin=longa.GetComponent<LongaArmaAnimationView>().Surface;
            var cameraObject=new GameObject("Temporary Longa Arma final review camera");
            var camera=cameraObject.AddComponent<Camera>();
            var center=skin.bounds.center;
            camera.transform.position=center-longa.transform.forward*2.2f+longa.transform.right*1.2f+Vector3.up*.6f;
            camera.transform.LookAt(center);camera.nearClipPlane=.03f;camera.farClipPlane=100;camera.fieldOfView=55;
            var texture=new RenderTexture(960,720,24);var pixels=new Texture2D(960,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
                pixels.ReadPixels(new Rect(0,0,960,720),0,0);pixels.Apply();
                File.WriteAllBytes("Logs/PegasusLongaArmaFinal.png",pixels.EncodeToPNG());
                Debug.Log("Pegasus Longa Arma final capture: Logs/PegasusLongaArmaFinal.png");
            }
            finally
            {
                RenderTexture.active=previous;camera.targetTexture=null;
                UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
