using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Bellerophon.PlayerHands;

namespace Bellerophon.Editor
{
    [InitializeOnLoad]
    internal static class PlayerHandRigTools
    {
        internal const string ModelPath = "Assets/_Project/Art/Player/player.fbx";
        internal const string Evidence = "docs/validation/consumable_item_grips_2026-09-09/hand_weight_review";
        internal const string CandidatePath = "Assets/_Project/Art/Player/HandsRig/player_hands_candidate.fbx";
        private const string RigFolder = "Assets/_Project/Art/Player/HandsRig";
        private const string ReviewPending = "Bellerophon.PlayerHands.ReviewPending";
        private const string ReviewRootName = "PlayerHandsRigReviewSession";
        // Survives script reloads so a temporary rendering comparison cannot lose its restore value.
        private const string OriginalSkinningMode = "Bellerophon.PlayerHands.OriginalSkinningMode";
        private static HandObservation handObservation;
        private static SharedRigObservation sharedObservation;
        private static bool inspectMotionSkin;
        private static bool inspectGripSkin;
        private static readonly HashSet<int> gripCyclePhases=new HashSet<int>();
        private static StringBuilder gripCycleReport;
        private static double gripCycleStarted;
        private static Camera skinInspectionCamera;
        // Read-only snapshots tie camera callbacks to the following natural end-of-frame observation.
        private static StringBuilder skinRenderTrace;
        private static PlayerHandRigReviewObserver skinTraceObserver;
        private static Vector3[] skinBeginWorld;
        static PlayerHandRigTools()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode)
                {SessionState.SetBool(ReviewPending,false);CleanupReviewSession();}
                if(state==PlayModeStateChange.EnteredEditMode)RestorePlayerHandRigSkinning();
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ReviewPending, false))
                {
                    SessionState.SetBool(ReviewPending, false);
                    SpawnHandReview();
                }
            };
        }

        internal static void UsePlayerHandRigGpuSkinning()=>SetTemporarySkinning(MeshDeformation.GPU);
        internal static void UsePlayerHandRigCpuSkinning()=>SetTemporarySkinning(MeshDeformation.CPU);
        private static void SetTemporarySkinning(MeshDeformation mode)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Change the comparison environment only in Edit Mode.");
            if(SessionState.GetInt(OriginalSkinningMode,-1)<0)
                SessionState.SetInt(OriginalSkinningMode,(int)PlayerSettings.meshDeformation);
            PlayerSettings.meshDeformation=mode;
            File.AppendAllText(Evidence+"/skinning_environment.txt",DateTime.Now.ToString("s")+" temporary="+mode+" restore="+(MeshDeformation)SessionState.GetInt(OriginalSkinningMode,-1)+"\n");
            Debug.Log("Temporary hand review skinning="+PlayerSettings.meshDeformation);
        }
        internal static void RestorePlayerHandRigSkinning()
        {
            int original=SessionState.GetInt(OriginalSkinningMode,-1);if(original<0)return;
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop hand review before restoring its rendering environment.");
            PlayerSettings.meshDeformation=(MeshDeformation)original;
            if((int)PlayerSettings.meshDeformation!=original)throw new InvalidOperationException("Skinning restoration failed.");
            SessionState.EraseInt(OriginalSkinningMode);
            File.AppendAllText(Evidence+"/skinning_environment.txt",DateTime.Now.ToString("s")+" restored="+PlayerSettings.meshDeformation+"\n");
        }

        internal static void PreparePlayerHandRigReview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author review assets in Edit Mode.");
            gripFit.Clear();
            GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath);
            Mesh sharedSkin=CreateSharedHandSkin(candidate.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
            foreach (string kind in new[] { "Articulation", "BatteryGrip" })
            {
                GameObject instance = UnityEngine.Object.Instantiate(candidate);
                try
                {
                    instance.name = "PlayerHands_" + kind;
                    foreach(var importedSkin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        // Reconstruct only the disposable candidate's renderer from explicit public bindings.
                        // This leaves imported native renderer state out of the comparison without changing the skin data.
                        GameObject owner=importedSkin.gameObject;Transform[] bones=importedSkin.bones;
                        Transform rootBone=importedSkin.rootBone;Material[] materials=importedSkin.sharedMaterials;
                        Bounds localBounds=importedSkin.localBounds;
                        UnityEngine.Object.DestroyImmediate(importedSkin);
                        var skin=owner.AddComponent<SkinnedMeshRenderer>();
                        skin.sharedMesh=sharedSkin;skin.bones=bones;skin.rootBone=rootBone;
                        skin.sharedMaterials=materials;skin.localBounds=localBounds;skin.updateWhenOffscreen=true;
                    }
                    if (kind == "BatteryGrip")
                    {
                        foreach (string side in new[] { "Left", "Right" }) AttachReviewBattery(instance, side);
                        RecordGripReachAuthoring(instance);
                        // Shared web vertices respond to more than one digit. Refit against the other
                        // digits' proposed rotations instead of assuming every neighbor remains open.
                        for(int iteration=0;iteration<3;iteration++)
                            foreach(string side in new[]{"Left","Right"})
                                foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Little"})
                                {gripFit.Remove(instance.GetInstanceID()+side+finger);GripRotations(instance,side,finger);}
                    }
                    AnimationClip clip = CreateHandReviewClip(instance, kind == "BatteryGrip");
                    clip.name = "Review_" + kind;
                    string clipPath = RigFolder + "/Review_" + kind + ".anim";
                    AnimationClip stored = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (stored == null) { AssetDatabase.CreateAsset(clip, clipPath); stored = clip; }
                    else { EditorUtility.CopySerialized(clip, stored); EditorUtility.SetDirty(stored); UnityEngine.Object.DestroyImmediate(clip); }
                    string controllerPath = RigFolder + "/Review_" + kind + ".controller";
                    AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                    if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    AnimatorStateMachine machine = controller.layers[0].stateMachine;
                    AnimatorState state = machine.states.Length == 0 ? machine.AddState("NaturalHandReview") : machine.states[0].state;
                    state.motion = stored; state.writeDefaultValues = true; machine.defaultState = state;
                    Animator animator = instance.GetComponent<Animator>();
                    if (animator == null) animator = instance.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var observer = instance.AddComponent<PlayerHandRigReviewObserver>();
                    observer.Animator = animator; observer.ReviewKind = kind;
                    PrefabUtility.SaveAsPrefabAsset(instance, RigFolder + "/Review_" + kind + ".prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance); }
            }
            AssetDatabase.SaveAssets();
        }

        internal static void RefreshPlayerHandWeightReview(bool topologyRepair = false)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prepare weight review only in Edit Mode.");
            Directory.CreateDirectory(Evidence);
            AssetDatabase.ImportAsset(CandidatePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var candidate = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath).GetComponentInChildren<SkinnedMeshRenderer>();
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentInChildren<SkinnedMeshRenderer>();
            if(!candidate.bones.Select(b=>b.name).SequenceEqual(original.bones.Select(b=>b.name)) || candidate.bones.Length != 54)
                throw new InvalidOperationException("Weight-only candidate changed the skeleton.");
            Mesh a = original.sharedMesh, b = candidate.sharedMesh;
            if(!topologyRepair && (a.vertexCount != b.vertexCount || !a.triangles.SequenceEqual(b.triangles)))
                throw new InvalidOperationException("Weight-only candidate changed topology or vertex order.");
            Vector3[] av=a.vertices,bv=b.vertices;Vector2[] au=a.uv,bu=b.uv;
            float maxPosition=0,maxUv=0,maxOther=0,maxBudget=0,maxRightChange=0;
            BoneWeight[] aw=a.boneWeights,bw=b.boneWeights;
            bool fingerReconstruction=topologyRepair&&File.Exists(RigFolder+"/right_finger_repair_manifest.json")&&
                File.ReadAllText(RigFolder+"/right_finger_repair_manifest.json").Contains("RightFingerSurfaceReconstruction");
            int[] correspondence=fingerReconstruction?MapRightFingerReconstruction(a,b,original.bones):
                topologyRepair?MapLocalRightHandRepair(a,b,original.bones):Enumerable.Range(0,b.vertexCount).ToArray();
            bool[] sourceRepairRegion=fingerReconstruction?RightFingerRepairRegions(a,original.bones):null;
            for(int candidateVertex=0;candidateVertex<b.vertexCount;candidateVertex++)
            {
                int v=correspondence[candidateVertex];if(v<0)continue;
                if(fingerReconstruction&&sourceRepairRegion[v])continue;
                maxPosition=Mathf.Max(maxPosition,Vector3.Distance(av[v],bv[candidateVertex]));
                maxUv=Mathf.Max(maxUv,Vector2.Distance(au[v],bu[candidateVertex]));
                float before=0,after=0;
                for(int bone=0;bone<original.bones.Length;bone++)
                {
                    float wa=WeightFor(aw[v],bone),wb=WeightFor(bw[candidateVertex],bone);
                    if(original.bones[bone].name=="RightHand"||IsFingerBone(original.bones[bone].name,"Right")) {before+=wa;after+=wb;maxRightChange=Mathf.Max(maxRightChange,Mathf.Abs(wa-wb));}
                    else maxOther=Mathf.Max(maxOther,Mathf.Abs(wa-wb));
                }
                maxBudget=Mathf.Max(maxBudget,Mathf.Abs(before-after));
            }
            if(maxPosition>.00002f||maxUv>.00001f||maxOther>.000001f||maxBudget>.000001f)
                throw new InvalidOperationException("Protected candidate data changed: positions="+maxPosition+" uv="+maxUv+" other="+maxOther+" budget="+maxBudget);
            for(int bone=0;bone<54;bone++)for(int r=0;r<4;r++)for(int c=0;c<4;c++)
                if(Mathf.Abs(a.bindposes[bone][r,c]-b.bindposes[bone][r,c])>.00002f)
                    throw new InvalidOperationException("Candidate bind pose changed.");
            Mesh shared=CreateSharedHandSkin(b);
            foreach(string kind in new[]{"Articulation","BatteryGrip"})
            {
                string path=RigFolder+"/Review_"+kind+".prefab";
                GameObject instance=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var renderer=instance.GetComponentInChildren<SkinnedMeshRenderer>();
                    var names=renderer.bones.ToDictionary(t=>t.name);
                    renderer.sharedMesh=shared;renderer.bones=candidate.bones.Select(t=>names[t.name]).ToArray();
                    PrefabUtility.SaveAsPrefabAsset(instance,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(instance);}
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Evidence+"/candidate_preservation.txt","positions="+maxPosition+" uv="+maxUv+" nonRightWeights="+maxOther+" rightBudget="+maxBudget+" rightWeightChange="+maxRightChange+"\nauthorizedLocalTopologyRepair="+topologyRepair+"\nexistingReviewClipsAndPosesPreserved=True\ndirectVisualReviewRequired=True\n");
            if(fingerReconstruction)
            {
                var report=new StringBuilder("Read-only existing corrective transfer preparation. No actual asset writes.\n");
                foreach(string path in new[]{"Assets/_Project/Art/Player/ShotgunReloadWristDeform/Shotgun_Reload_LeftWristDeform.asset",
                    "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_Breathing.asset"})
                {
                    Mesh transfer=null;
                    try
                    {
                        var skin=Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>().First(r=>r.gameObject.scene.IsValid()&&AssetDatabase.GetAssetPath(r.sharedMesh)==path);
                        transfer=TransferRightFingerCorrective(a,b,skin.sharedMesh,original.bones,skin.bones.Select(t=>t.name).ToArray(),correspondence);
                        report.AppendLine(path+" transferReady=True bones="+transfer.bindposes.Length+" blendShapes="+transfer.blendShapeCount);
                    }
                    catch(Exception error){report.AppendLine(path+" transferReady=False reason="+error.Message);}
                    finally{if(transfer!=null)UnityEngine.Object.DestroyImmediate(transfer);}
                }
                File.WriteAllText(Evidence+"/right_finger_corrective_preparation.txt",report.ToString());
            }
        }

        private static bool[] RightFingerRepairRegions(Mesh mesh,Transform[] bones)
        {
            int hand=Array.FindIndex(bones,b=>b.name=="RightHand");
            Matrix4x4 bind=mesh.bindposes[hand];Vector3[] vertices=mesh.vertices;BoneWeight[] weights=mesh.boneWeights;
            bool[] right=bones.Select(b=>b.name=="RightHand"||IsFingerBone(b.name,"Right")).ToArray();
            return Enumerable.Range(0,vertices.Length).Select(vertex=>
            {
                Vector3 p=bind.MultiplyPoint3x4(vertices[vertex]);BoneWeight w=weights[vertex];
                float total=(right[w.boneIndex0]?w.weight0:0)+(right[w.boneIndex1]?w.weight1:0)+
                    (right[w.boneIndex2]?w.weight2:0)+(right[w.boneIndex3]?w.weight3:0);
                return total>.01f&&p.y>=.06498f&&p.y<.28f&&Mathf.Abs(p.x)<.11f&&Mathf.Abs(p.z)<.11f;
            }).ToArray();
        }

        // Separate compatibility path for the explicitly approved 2026-09-10 finger
        // reconstruction. The original two-hole and weight-only guards remain intact.
        private static int[] MapRightFingerReconstruction(Mesh source,Mesh candidate,Transform[] bones)
        {
            Vector3[] a=source.vertices,b=candidate.vertices;Vector2[] au=source.uv,bu=candidate.uv;
            bool[] oldLocal=RightFingerRepairRegions(source,bones);
            bool[] newLocal=RightFingerRepairRegions(candidate,bones);
            var cells=new Dictionary<Vector3Int,List<int>>();
            Vector3Int Cell(Vector3 p)=>Vector3Int.FloorToInt(p/.00002f);
            for(int v=0;v<a.Length;v++){var key=Cell(a[v]);if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<int>();list.Add(v);}
            var map=Enumerable.Repeat(-1,b.Length).ToArray();var covered=new HashSet<string>();
            string Key(Vector3 p)=>Math.Round(p.x,5)+"/"+Math.Round(p.y,5)+"/"+Math.Round(p.z,5);
            string VertexKey(int v)=>Key(a[v])+"/"+Math.Round(au[v].x,5)+"/"+Math.Round(au[v].y,5);
            for(int v=0;v<b.Length;v++)
            {
                Vector3Int cell=Cell(b[v]);float best=4e-10f;
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                    if(cells.TryGetValue(cell+new Vector3Int(x,y,z),out var list))foreach(int old in list)
                    {
                        float distance=(a[old]-b[v]).sqrMagnitude;
                        if(distance>best||(au[old]-bu[v]).sqrMagnitude>1e-10f)continue;
                        map[v]=old;best=distance;
                    }
                if(map[v]>=0)covered.Add(VertexKey(map[v]));
                if(!newLocal[v]&&map[v]<0)throw new InvalidOperationException("Finger reconstruction changed a protected vertex: "+v+" at "+b[v]);
            }
            for(int v=0;v<a.Length;v++)if(!oldLocal[v]&&!covered.Contains(VertexKey(v)))
                throw new InvalidOperationException("A protected wrist/body/left-hand vertex disappeared: "+v);
            string Triangle(Vector3 p,Vector3 q,Vector3 r)=>string.Join("|",new[]{Key(p),Key(q),Key(r)}.OrderBy(s=>s,StringComparer.Ordinal));
            var triangles=new Dictionary<string,int>();int[] at=source.triangles,bt=candidate.triangles;
            for(int t=0;t<at.Length;t+=3)
            {
                if(oldLocal[at[t]]||oldLocal[at[t+1]]||oldLocal[at[t+2]])continue;
                string key=Triangle(a[at[t]],a[at[t+1]],a[at[t+2]]);triangles[key]=triangles.TryGetValue(key,out int count)?count+1:1;
            }
            for(int t=0;t<bt.Length;t+=3)
            {
                if(newLocal[bt[t]]||newLocal[bt[t+1]]||newLocal[bt[t+2]])continue;
                string key=Triangle(b[bt[t]],b[bt[t+1]],b[bt[t+2]]);
                if(!triangles.TryGetValue(key,out int count)||count<=0)throw new InvalidOperationException("An unrelated face was changed.");
                triangles[key]=count-1;
            }
            if(triangles.Values.Any(count=>count!=0))throw new InvalidOperationException("An unrelated face was lost.");
            File.WriteAllText(Evidence+"/finger_reconstruction_correspondence.txt","originalVertices="+a.Length+" candidateVertices="+b.Length+
                " protectedSourceVertices="+oldLocal.Count(local=>!local)+" protectedTrianglesPreserved=True\nallowedRegion=Right glove distal to 65mm; wrist/body/left hand protected\n");
            return map;
        }

        // Authoring correspondence for the explicitly approved two-hole repair, not a relaxed weight-only check.
        private static int[] MapLocalRightHandRepair(Mesh source,Mesh candidate,Transform[] bones)
        {
            Vector3[] a=source.vertices,b=candidate.vertices;Vector2[] au=source.uv,bu=candidate.uv;
            var cells=new Dictionary<Vector3Int,List<int>>();
            Vector3Int Cell(Vector3 p)=>Vector3Int.FloorToInt(p/.00002f);
            for(int v=0;v<a.Length;v++){var key=Cell(a[v]);if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<int>();list.Add(v);}
            var map=Enumerable.Repeat(-1,b.Length).ToArray();var covered=new HashSet<string>();
            string Key(Vector3 p)=>Math.Round(p.x,5)+"/"+Math.Round(p.y,5)+"/"+Math.Round(p.z,5);
            int hand=Array.FindIndex(bones,t=>t.name=="RightHand");Matrix4x4 handBind=source.bindposes[hand];
            bool Local(Vector3 p){p=handBind.MultiplyPoint3x4(p);return Mathf.Abs(p.x)<.1f&&p.y>.1f&&p.y<.3f&&Mathf.Abs(p.z)<.1f;}
            for(int v=0;v<b.Length;v++)
            {
                Vector3Int cell=Cell(b[v]);float best=4e-10f;
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                    if(cells.TryGetValue(cell+new Vector3Int(x,y,z),out var list))foreach(int old in list)
                    {float distance=(a[old]-b[v]).sqrMagnitude;if(distance>best||(au[old]-bu[v]).sqrMagnitude>1e-10f)continue;map[v]=old;best=distance;}
                if(map[v]>=0)covered.Add(Key(a[map[v]]));
                else
                {
                    if(!Local(b[v]))throw new InvalidOperationException("New vertex outside the approved finger patch.");
                    BoneWeight w=candidate.boneWeights[v];
                    float right=Enumerable.Range(0,bones.Length).Where(i=>i==hand||IsFingerBone(bones[i].name,"Right")).Sum(i=>WeightFor(w,i));
                    if(Mathf.Abs(right-1f)>.000001f)throw new InvalidOperationException("New patch vertex has non-right-hand influence.");
                }
            }
            if(a.Any(p=>!covered.Contains(Key(p))))throw new InvalidOperationException("An original model vertex disappeared.");
            string Triangle(Vector3 p,Vector3 q,Vector3 r)=>string.Join("|",new[]{Key(p),Key(q),Key(r)}.OrderBy(s=>s,StringComparer.Ordinal));
            var original=new Dictionary<string,int>();int[] at=source.triangles,bt=candidate.triangles;
            for(int t=0;t<at.Length;t+=3){string key=Triangle(a[at[t]],a[at[t+1]],a[at[t+2]]);original[key]=original.TryGetValue(key,out int n)?n+1:1;}
            int added=0;
            for(int t=0;t<bt.Length;t+=3)
            {
                int u=bt[t],v=bt[t+1],w=bt[t+2];
                string key=map[u]>=0&&map[v]>=0&&map[w]>=0?Triangle(a[map[u]],a[map[v]],a[map[w]]):null;
                if(key!=null&&original.TryGetValue(key,out int n)&&n>0)original[key]=n-1;
                else{if(!Local(b[u])||!Local(b[v])||!Local(b[w]))throw new InvalidOperationException("Triangle repair escaped the right fingers.");added++;}
            }
            if(original.Values.Any(n=>n!=0)||added<=0)throw new InvalidOperationException("The patch did not preserve all original triangles.");
            File.WriteAllText(Evidence+"/topology_correspondence.txt","originalVertices="+a.Length+" candidateVertices="+b.Length+" newCorrespondenceVertices="+map.Count(i=>i<0)+" originalTrianglesPreserved=True addedTriangles="+added+"\n");
            return map;
        }

        private static Mesh CreateSharedHandSkin(Mesh source)
        {
            if(source.blendShapeCount!=0)throw new InvalidOperationException("The diagnostic skin copy must not discard blend shapes.");
            Mesh mesh=CopySkinData(source);mesh.name="PlayerHandsSkin";
            string path=RigFolder+"/PlayerHandsSkin.asset";
            Mesh stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(stored==null){AssetDatabase.CreateAsset(mesh,path);stored=mesh;}
            else
            {
                string guid=AssetDatabase.AssetPathToGUID(path);
                AssetDatabase.CreateAsset(mesh,path);AssetDatabase.SaveAssetIfDirty(mesh);stored=mesh;
                if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Shared review skin GUID changed.");
            }
            if(!stored.vertices.SequenceEqual(source.vertices)||!stored.triangles.SequenceEqual(source.triangles)||!stored.bindposes.SequenceEqual(source.bindposes))
                throw new InvalidOperationException("Shared hand skin copy changed source data.");
            File.WriteAllText(Evidence+"/shared_skin_copy.txt","positions=identical\ntriangles=identical\nbindposes=identical\nsource="+CandidatePath+"\n");
            return stored;
        }
        private static Mesh CopySkinData(Mesh source)
        {
            // Construct public mesh data explicitly. Cloning an imported mesh carries its
            // native LOD index table, which becomes empty when the clone is edited/saved.
            var mesh=new Mesh{name=source.name,indexFormat=source.indexFormat};
            mesh.vertices=source.vertices;mesh.normals=source.normals;mesh.tangents=source.tangents;
            for(int channel=0;channel<8;channel++)
            {var uv=new List<Vector4>();source.GetUVs(channel,uv);if(uv.Count>0)mesh.SetUVs(channel,uv);}
            if(source.colors32.Length>0)mesh.colors32=source.colors32;
            mesh.bindposes=source.bindposes;mesh.boneWeights=source.boneWeights;
            mesh.subMeshCount=source.subMeshCount;
            for(int sub=0;sub<source.subMeshCount;sub++)mesh.SetIndices(source.GetIndices(sub),source.GetTopology(sub),sub,false);
            for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
            {
                var positions=new Vector3[source.vertexCount];var normals=new Vector3[source.vertexCount];var tangents=new Vector3[source.vertexCount];
                source.GetBlendShapeFrameVertices(shape,frame,positions,normals,tangents);
                mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),source.GetBlendShapeFrameWeight(shape,frame),positions,normals,tangents);
            }
            mesh.bounds=source.bounds;
            return mesh;
        }

        internal static void RepairSharedPlayerCorrectiveSkinData()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Corrective mesh data repair requires Edit Mode.");
            var report=new StringBuilder("Corrective skin native index metadata repair; public geometry, skin and BlendShape data preserved.\n");
            foreach(string path in new[]{"Assets/_Project/Art/Player/ShotgunReloadWristDeform/Shotgun_Reload_LeftWristDeform.asset",
                "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_Breathing.asset"})
            {
                Mesh stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(stored.bindposes.Length<54)throw new InvalidOperationException("The shared hand rig must already be applied.");
                string guid=AssetDatabase.AssetPathToGUID(path);
                var users=Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>().Where(r=>r.gameObject.scene.IsValid()&&r.sharedMesh==stored).ToArray();
                Mesh copy=CopySkinData(stored);
                try
                {
                    if(!copy.vertices.SequenceEqual(stored.vertices)||!copy.triangles.SequenceEqual(stored.triangles)||
                        !copy.boneWeights.SequenceEqual(stored.boneWeights)||!copy.bindposes.SequenceEqual(stored.bindposes)||
                        !copy.normals.SequenceEqual(stored.normals)||!copy.uv.SequenceEqual(stored.uv)||copy.blendShapeCount!=stored.blendShapeCount)
                        throw new InvalidOperationException("Corrective data reconstruction differs from authored data.");
                    // CopySerialized leaves the persistent mesh's old native vertex buffer
                    // layout behind. Replace the asset object at its existing GUID/path.
                    string backup="Backups/PlayerHandsRig_2026-09-09/corrective_buffers_"+DateTime.Now.ToString("HHmmss");
                    Directory.CreateDirectory(backup);File.Copy(path,backup+"/"+Path.GetFileName(path),false);
                    AssetDatabase.CreateAsset(copy,path);AssetDatabase.SaveAssetIfDirty(copy);
                    if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Corrective asset GUID changed unexpectedly.");
                    stored=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    foreach(var renderer in users)
                    {
                        Transform[] bones=renderer.bones;
                        renderer.sharedMesh=null;renderer.bones=Array.Empty<Transform>();
                        renderer.sharedMesh=stored;renderer.bones=bones;
                        EditorUtility.SetDirty(renderer);
                    }
                    report.AppendLine(path+" vertices="+stored.vertexCount+" shapes="+stored.blendShapeCount);
                }
                finally{if(!EditorUtility.IsPersistent(copy))UnityEngine.Object.DestroyImmediate(copy);}
            }
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/_Project/Scenes/CargoRunMvp.unity");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Evidence+"/corrective_index_data_repair.txt",report.ToString());
        }

        private static AnimationClip CreateHandReviewClip(GameObject instance, bool grip)
        {
            var clip = new AnimationClip { name = "HandRigArticulationReview", frameRate = 30f, wrapMode = WrapMode.Loop };
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            foreach (string side in new[] { "Left", "Right" })
            {
                Transform hand = instance.GetComponentsInChildren<Transform>(true).Single(t => t.name == side + "Hand");
                foreach (string finger in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
                    for (int joint = 0; joint < 3; joint++)
                    {
                        string name = side + finger + new[] { "Proximal", "Intermediate", "Distal" }[joint];
                        Transform bone = instance.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
                        Vector3 direction = joint < 2 ? (bone.GetChild(0).position-bone.position).normalized : bone.up;
                        Vector3 palm = hand.TransformDirection(FingerBendDirection(side,finger));
                        Vector3 axis = bone.InverseTransformDirection(Vector3.Cross(direction,palm).normalized);
                        float angle = finger == "Thumb" ? new[] { 25f,45f,35f }[joint] : new[] { 55f,75f,50f }[joint];
                        Quaternion closed=grip?GripRotations(instance,side,finger)[joint]:bone.localRotation*Quaternion.AngleAxis(angle,axis);
                        var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                        Quaternion previous = bone.localRotation;
                        for (int frame = 0; frame <= 180; frame++)
                        {
                            float t = frame / 30f;
                            float amount = t < 1f ? 0f : t < 3f ? Mathf.SmoothStep(0f,1f,(t-1f)/2f) : t < 4f ? 1f : t < 5f ? Mathf.SmoothStep(1f,0f,t-4f) : 0f;
                            Quaternion rotation=grip?ReviewGripRotation(finger,joint,bone.localRotation,closed,amount):Quaternion.Slerp(bone.localRotation,closed,amount);
                            if (Quaternion.Dot(previous,rotation)<0f) rotation = new Quaternion(-rotation.x,-rotation.y,-rotation.z,-rotation.w);
                            for (int c=0;c<4;c++) curves[c].AddKey(t,rotation[c]);
                            previous=rotation;
                        }
                        for (int c=0;c<4;c++) AnimationUtility.SetEditorCurve(clip,
                            EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(bone,instance.transform),typeof(Transform),"m_LocalRotation."+"xyzw"[c]),curves[c]);
                    }
            }
            clip.EnsureQuaternionContinuity();
            return clip;
        }
        // Authoring and the temporary review curves use one identical path definition.
        private static Quaternion ReviewGripRotation(string finger,int joint,Quaternion rest,Quaternion closed,float amount)
        {
            if(finger!="Thumb")return Quaternion.Slerp(rest,closed,joint==0?
                Mathf.SmoothStep(0,1,(amount-.30f)/.70f):Mathf.SmoothStep(0,1,amount/.70f));
            if(joint!=0)return Quaternion.Slerp(rest,closed,Mathf.SmoothStep(0,1,(amount-.35f)/.35f));
            Quaternion outside=Quaternion.AngleAxis(-20,Vector3.right);
            Quaternion openOutside=outside*rest,closedOutside=outside*closed;
            return amount<.35f?Quaternion.Slerp(rest,openOutside,Mathf.SmoothStep(0,1,amount/.35f)):
                amount<.7f?Quaternion.Slerp(openOutside,closedOutside,Mathf.SmoothStep(0,1,(amount-.35f)/.35f)):
                Quaternion.Slerp(closedOutside,closed,Mathf.SmoothStep(0,1,(amount-.7f)/.3f));
        }

        private static void AttachReviewBattery(GameObject instance, string side)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Items/Consumable/AuxiliaryBattery/AuxiliaryBattery.fbx");
            Transform hand = instance.GetComponentsInChildren<Transform>(true).Single(t=>t.name==side+"Hand");
            Transform grip = new GameObject(side+"BatteryGripReview").transform; grip.SetParent(hand,false);
            float sign = side=="Left"?1f:-1f;
            // Opposed grasp: finger pads support the near face and the thumb closes over the far face.
            // Keeping the item distal to the thenar mound avoids requiring the palm volume to collapse.
            // Keep the thumb-side edge near the improved longitudinal candidate's clearance,
            // instead of centering the longer transverse edge over the thumb approach.
            grip.localPosition = new Vector3(sign*.076f,.125f,side=="Left"?.050f:.020f);
            // Plug exits on the little-finger side, as in an opposed tool grasp.
            // The broad faces still oppose the finger pads and thumb; the long axis is not the forearm axis.
            grip.localRotation = Quaternion.LookRotation(Vector3.up*sign,Vector3.right*sign);
            GameObject battery = UnityEngine.Object.Instantiate(source,grip);
            battery.transform.localPosition=source.transform.localPosition;
            battery.transform.localRotation=source.transform.localRotation;
            // Match the existing consumable motion's approved 15 cm item length.
            battery.transform.localScale=source.transform.localScale*.15f;
        }

        // Authoring-only contact fitting on a disposable draft, never a verification target.
        private static readonly Dictionary<string,Quaternion[]> gripFit = new Dictionary<string,Quaternion[]>();
        private static void RecordGripReachAuthoring(GameObject draft)
        {
            Transform Bone(string name)=>draft.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
            Transform leftArm=Bone("LeftArm"),leftForearm=Bone("LeftForeArm"),leftHand=Bone("LeftHand");
            Transform rightShoulder=Bone("RightShoulder"),rightArm=Bone("RightArm"),rightForearm=Bone("RightForeArm"),rightHand=Bone("RightHand");
            MeshFilter item=rightHand.Find("RightBatteryGripReview").GetComponentInChildren<MeshFilter>();
            Bounds bounds=item.sharedMesh.bounds;
            Vector3[] ends=item.sharedMesh.vertices.Where(p=>p.x>bounds.max.x-.000002f).ToArray();
            Vector3 tip=ends.Aggregate(Vector3.zero,(sum,p)=>sum+p)/ends.Length;
            Vector3 tipInHand=rightHand.InverseTransformPoint(item.transform.TransformPoint(tip));
            Vector3 axisInHand=rightHand.InverseTransformDirection(item.transform.right);
            Vector3 socket=leftHand.position-(leftHand.position-leftForearm.position).normalized*.065f+draft.transform.forward*.043f;
            var report=new StringBuilder("Authoring feasibility only; original rest bone lengths and neutral wrist. No posed validation target.\n");
            report.AppendLine("tipInHand="+tipInHand.ToString("F6")+" plugAxisInHand="+axisInHand.ToString("F6"));
            Transform spine=Bone("Spine");
            foreach(float torsoYaw in new[]{0f,-10f,-20f,-30f})
            {
                Quaternion torso=Quaternion.AngleAxis(torsoYaw,draft.transform.up);
                Vector3 movedLeftArm=spine.position+torso*(leftArm.position-spine.position);
                Vector3 movedRightArm=spine.position+torso*(rightArm.position-spine.position);
                Vector3 movedShoulder=spine.position+torso*(rightShoulder.position-spine.position);
                // Keep the accepted straight left arm's world orientation; explore torso reach, not elbow bending.
                Vector3 contact=movedLeftArm+Quaternion.AngleAxis(-90,draft.transform.right)*(socket-leftArm.position);
                float best=float.PositiveInfinity;Vector3 bestElbow=Vector3.zero,bestWrist=Vector3.zero;Quaternion bestForearm=Quaternion.identity;
                for(int yaw=0;yaw<360;yaw++)
                {
                    Quaternion handRotation=Quaternion.AngleAxis(yaw,draft.transform.up)*Quaternion.FromToRotation(axisInHand,-draft.transform.up);
                    Quaternion forearmRotation=handRotation*Quaternion.Inverse(rightHand.localRotation);
                    Vector3 wrist=contact-handRotation*tipInHand;
                    Vector3 elbow=wrist-forearmRotation*rightHand.localPosition;
                    float distance=Vector3.Distance(elbow,movedRightArm);
                    if(distance>=best)continue;
                    best=distance;bestElbow=elbow;bestWrist=wrist;bestForearm=forearmRotation;
                }
                report.AppendLine("torsoYaw="+torsoYaw+" contact="+contact.ToString("F6")+" minUpperArmReach="+best.ToString("F6")+
                    " actualUpperArmLength="+rightForearm.localPosition.magnitude.ToString("F6")+
                    " shoulderToElbow="+Vector3.Distance(movedShoulder,bestElbow).ToString("F6")+
                    " elbow="+bestElbow.ToString("F6")+" wrist="+bestWrist.ToString("F6")+" forearmRotation="+bestForearm.ToString("F6"));
            }
            File.WriteAllText(Evidence+"/grip_reach_authoring.txt",report.ToString());
        }
        private static Quaternion[] GripRotations(GameObject draft,string side,string finger)
        {
            string key=draft.GetInstanceID()+side+finger;
            if(gripFit.TryGetValue(key,out Quaternion[] cached))return cached;
            Transform[] bones=Enumerable.Range(0,3).Select(i=>draft.GetComponentsInChildren<Transform>(true)
                .Single(t=>t.name==side+finger+new[]{"Proximal","Intermediate","Distal"}[i])).ToArray();
            Transform hand=bones[0].parent;
            Quaternion[] rest=bones.Select(b=>b.localRotation).ToArray();
            Vector3[] axes=bones.Select((b,i)=>b.InverseTransformDirection(Vector3.Cross(i<2?(bones[i+1].position-b.position).normalized:b.up,
                hand.TransformDirection(side=="Left"?Vector3.right:Vector3.left)).normalized)).ToArray();
            SkinnedMeshRenderer skin=draft.GetComponentInChildren<SkinnedMeshRenderer>();
            MeshFilter item=hand.Find(side+"BatteryGripReview").GetComponentInChildren<MeshFilter>();
            Matrix4x4 toItem=item.transform.worldToLocalMatrix*hand.localToWorldMatrix;
            Bounds box=item.sharedMesh.bounds;
            Matrix4x4 itemToHand=hand.worldToLocalMatrix*item.transform.localToWorldMatrix;
            Bounds contactBox=box;
            float itemScale=item.transform.lossyScale.x;
            // Author a small 0.2 mm surface separation instead of the earlier coarse 1 mm stand-off.
            // Read-only observation thresholds are independent and are not changed here.
            box.Expand(.0004f/item.transform.lossyScale.x);
            BoneWeight[] weights=skin.sharedMesh.boneWeights;
            int[] ids=bones.Select(b=>Array.IndexOf(skin.bones,b)).ToArray();
            int[] selected=Enumerable.Range(0,weights.Length).Where(v=>ids.Any(id=>WeightFor(weights[v],id)>.05f)).ToArray();
            var movingVertices=new HashSet<int>(selected);int[] meshTriangles=skin.sharedMesh.triangles;
            var nearbyTriangles=new List<int>();
            for(int t=0;t<meshTriangles.Length;t+=3)
                if(movingVertices.Contains(meshTriangles[t])||movingVertices.Contains(meshTriangles[t+1])||movingVertices.Contains(meshTriangles[t+2]))
                {nearbyTriangles.Add(meshTriangles[t]);nearbyTriangles.Add(meshTriangles[t+1]);nearbyTriangles.Add(meshTriangles[t+2]);}
            selected=selected.Concat(nearbyTriangles).Distinct().ToArray();
            var selectedIndex=selected.Select((v,i)=>(v,i)).ToDictionary(p=>p.v,p=>p.i);
            int[] collisionTriangles=nearbyTriangles.Select(v=>selectedIndex[v]).ToArray();
            Vector3[] source=skin.sharedMesh.vertices;
            var proposedRotations=new Dictionary<Transform,Quaternion>();
            foreach(string other in new[]{"Thumb","Index","Middle","Ring","Little"})
                if(gripFit.TryGetValue(draft.GetInstanceID()+side+other,out Quaternion[] otherRotations))
                    for(int j=0;j<3;j++)
                        proposedRotations[draft.GetComponentsInChildren<Transform>(true).Single(t=>t.name==side+other+new[]{"Proximal","Intermediate","Distal"}[j])]=otherRotations[j];
            Matrix4x4 ProposedMatrix(Transform b)
            {
                if(b==hand)return Matrix4x4.identity;
                if(!b.IsChildOf(hand))return hand.worldToLocalMatrix*b.localToWorldMatrix;
                return ProposedMatrix(b.parent)*Matrix4x4.TRS(b.localPosition,proposedRotations.TryGetValue(b,out Quaternion q)?q:b.localRotation,b.localScale);
            }
            Matrix4x4[] fixedMatrices=skin.bones.Select((b,i)=>ProposedMatrix(b)*skin.sharedMesh.bindposes[i]).ToArray();
            var fixedPoints=new Vector3[selected.Length];var bonePoints=new Vector3[3,selected.Length];var boneWeights=new float[3,selected.Length];
            for(int s=0;s<selected.Length;s++)
            {
                int v=selected[s];BoneWeight w=weights[v];
                int[] indices={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] values={w.weight0,w.weight1,w.weight2,w.weight3};
                for(int k=0;k<4;k++)
                {
                    int j=Array.IndexOf(ids,indices[k]);
                    if(j<0)fixedPoints[s]+=fixedMatrices[indices[k]].MultiplyPoint3x4(source[v])*values[k];
                    else{bonePoints[j,s]=skin.sharedMesh.bindposes[indices[k]].MultiplyPoint3x4(source[v]);boneWeights[j,s]+=values[k];}
                }
            }
            // Cache neighboring digits along their actual proposed paths, not only at the closed endpoint.
            var fixedPathPoints=new Vector3[19,selected.Length];
            for(int phase=0;phase<19;phase++)
            {
                float amount=(phase+1)*.05f;
                foreach(string other in new[]{"Thumb","Index","Middle","Ring","Little"})
                    if(gripFit.TryGetValue(draft.GetInstanceID()+side+other,out Quaternion[] otherRotations))
                        for(int j=0;j<3;j++)
                        {
                            Transform b=draft.GetComponentsInChildren<Transform>(true).Single(t=>t.name==side+other+new[]{"Proximal","Intermediate","Distal"}[j]);
                            proposedRotations[b]=ReviewGripRotation(other,j,b.localRotation,otherRotations[j],amount);
                        }
                Matrix4x4[] phaseMatrices=skin.bones.Select((b,i)=>ProposedMatrix(b)*skin.sharedMesh.bindposes[i]).ToArray();
                for(int s=0;s<selected.Length;s++)
                {
                    int v=selected[s];BoneWeight w=weights[v];
                    int[] indices={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] values={w.weight0,w.weight1,w.weight2,w.weight3};
                    for(int k=0;k<4;k++)if(Array.IndexOf(ids,indices[k])<0)
                        fixedPathPoints[phase,s]+=phaseMatrices[indices[k]].MultiplyPoint3x4(source[v])*values[k];
                }
            }
            float[] angles=new float[3];float[] limits=finger=="Thumb"?new[]{40f,55f,40f}:new[]{75f,90f,65f};
            // Fit the glove pad itself, not an assumed radius around the distal bone tip.
            // Source pad selection stays fixed throughout authoring; collision checks still use every selected vertex.
            float sign=side=="Left"?1f:-1f;
            Vector3 distalRest=hand.InverseTransformPoint(bones[2].position);
            Matrix4x4 sourceToHand=skin.sharedMesh.bindposes[Array.IndexOf(skin.bones,hand)];
            int[] pad=Enumerable.Range(0,selected.Length).Where(s=>
            {
                Vector3 p=sourceToHand.MultiplyPoint3x4(source[selected[s]]);
                return boneWeights[2,s]>.15f&&p.y>distalRest.y-.006f&&
                    (finger=="Thumb"?p.z-distalRest.z:sign*(p.x-distalRest.x))>.002f;
            }).ToArray();
            if(pad.Length<4)throw new InvalidOperationException("Insufficient source pad samples: "+side+finger);
            pad=pad.Where((s,i)=>i%Mathf.Max(1,pad.Length/32)==0).ToArray();
            // The broad faces are mesh-local Z, including when the item is tilted in the hand.
            float farFaceSign=Mathf.Sign(Vector3.Dot(itemToHand.MultiplyVector(Vector3.forward),Vector3.right*sign));
            float face=contactBox.center.z+(finger=="Thumb"?farFaceSign:-farFaceSign)*
                (contactBox.extents.z+.0002f/itemScale);
            float bestScore=float.PositiveInfinity,spread=0,twist=0,bestPenetration=0;
            Vector3 spreadAxis=bones[0].InverseTransformDirection(hand.TransformDirection(Vector3.right));
            Quaternion[] result=rest.ToArray();
            // Evaluate skin directly from the candidate bind poses and proposed local joint rotations.
            // No Animator/native skin cache or scene-time evaluation participates in this authoring search.
            for(int pass=0;pass<2;pass++)
            {
                float step=pass==0?15f:3f;float[] center=(float[])angles.Clone();float centerSpread=spread,centerTwist=twist;
                float[] low=Enumerable.Range(0,3).Select(j=>pass==0?0f:Mathf.Max(0,center[j]-15)).ToArray();
                float[] high=Enumerable.Range(0,3).Select(j=>pass==0?limits[j]:Mathf.Min(limits[j],center[j]+15)).ToArray();
                float spreadLimit=finger=="Thumb"?45:15;
                float lowSpread=pass==0?-spreadLimit:Mathf.Max(-spreadLimit,centerSpread-15);
                float highSpread=pass==0?spreadLimit:Mathf.Min(spreadLimit,centerSpread+15);
                float lowTwist=finger!="Thumb"?0:pass==0?-60:Mathf.Max(-60,centerTwist-30);
                float highTwist=finger!="Thumb"?0:pass==0?60:Mathf.Min(60,centerTwist+30);
                for(float roll=lowTwist;roll<=highTwist;roll+=pass==0?30:6)
                for(float s=lowSpread;s<=highSpread;s+=step)
                for(float a=low[0];a<=high[0];a+=step)for(float b=low[1];b<=high[1];b+=step)for(float c=low[2];c<=high[2];c+=step)
                {
                    // This tool grasp uses coordinated IP curl, not a hooked distal joint on an almost straight PIP.
                    if(finger!="Thumb"&&c>b*.8f+15)continue;
                    float[] trial={a,b,c};var rotations=new Quaternion[3];var matrices=new Matrix4x4[3];
                    for(int j=0;j<3;j++)
                    {
                        rotations[j]=rest[j]*(j==0?Quaternion.AngleAxis(s,spreadAxis):Quaternion.identity)*Quaternion.AngleAxis(trial[j],axes[j]);
                        if(j==0)rotations[j]*=Quaternion.AngleAxis(roll,Vector3.up);
                        matrices[j]=(j==0?Matrix4x4.identity:matrices[j-1])*Matrix4x4.TRS(bones[j].localPosition,rotations[j],bones[j].localScale);
                    }
                    var padDistances=new float[pad.Length];
                    for(int p=0;p<pad.Length;p++)
                    {
                        int v=pad[p];Vector3 point=fixedPoints[v];
                        for(int j=0;j<3;j++)point+=matrices[j].MultiplyPoint3x4(bonePoints[j,v])*boneWeights[j,v];
                        Vector3 local=toItem.MultiplyPoint3x4(point);
                        Vector3 target=itemToHand.MultiplyPoint3x4(new Vector3(
                            Mathf.Clamp(local.x,contactBox.min.x+.006f/itemScale,contactBox.max.x-.018f/itemScale),
                            Mathf.Clamp(local.y,contactBox.min.y+.006f/itemScale,contactBox.max.y-.006f/itemScale),face));
                        padDistances[p]=(point-target).sqrMagnitude;
                    }
                    Array.Sort(padDistances);
                    int contactSamples=Mathf.Min(8,pad.Length);float contactScore=0;
                    for(int p=0;p<contactSamples;p++)contactScore+=padDistances[p]/contactSamples;
                    // Prefer a coordinated low-effort grasp when several contact solutions are available.
                    contactScore+=.00001f*Mathf.Deg2Rad*Mathf.Deg2Rad*(a*a+b*b+c*c+s*s+roll*roll);
                    if(contactScore>=bestScore)continue;
                    float penetration=0,maximum=0;var itemPoints=new Vector3[selected.Length];
                    for(int v=0;v<selected.Length;v++)
                    {
                        Vector3 p=fixedPoints[v];for(int j=0;j<3;j++)p+=matrices[j].MultiplyPoint3x4(bonePoints[j,v])*boneWeights[j,v];
                        p=toItem.MultiplyPoint3x4(p);itemPoints[v]=p;if(!box.Contains(p))continue;
                        Vector3 depth=box.extents-new Vector3(Mathf.Abs(p.x-box.center.x),Mathf.Abs(p.y-box.center.y),Mathf.Abs(p.z-box.center.z));
                        float d=Mathf.Min(depth.x,Mathf.Min(depth.y,depth.z))*item.transform.lossyScale.x;
                        penetration+=d*d;maximum=Mathf.Max(maximum,d);
                    }
                    float score=contactScore+1000*(maximum*maximum+penetration/selected.Length);
                    if(score>=bestScore)continue;
                    // Vertex-only fitting misses a triangle cutting across an item corner.
                    // A conservative solid-box face constraint is authoring only; observation still uses the real mesh.
                    for(int t=0;t<collisionTriangles.Length;t+=3)
                    {
                        if(!TriangleOverlapsBox(itemPoints[collisionTriangles[t]],itemPoints[collisionTriangles[t+1]],itemPoints[collisionTriangles[t+2]],box))continue;
                        score+=.01f;if(score>=bestScore)break;
                    }
                    if(score>=bestScore)continue;
                    for(int phase=0;phase<19;phase++)
                    {
                        for(int j=0;j<3;j++)matrices[j]=(j==0?Matrix4x4.identity:matrices[j-1])*Matrix4x4.TRS(bones[j].localPosition,
                            ReviewGripRotation(finger,j,rest[j],rotations[j],(phase+1)*.05f),bones[j].localScale);
                        for(int v=0;v<selected.Length;v++)
                        {
                            Vector3 point=fixedPathPoints[phase,v];
                            for(int j=0;j<3;j++)point+=matrices[j].MultiplyPoint3x4(bonePoints[j,v])*boneWeights[j,v];
                            itemPoints[v]=toItem.MultiplyPoint3x4(point);
                        }
                        for(int t=0;t<collisionTriangles.Length;t+=3)
                        {
                            if(!TriangleOverlapsBox(itemPoints[collisionTriangles[t]],itemPoints[collisionTriangles[t+1]],itemPoints[collisionTriangles[t+2]],box))continue;
                            score+=.01f;break;
                        }
                        if(score>=bestScore)break;
                    }
                    if(score<bestScore){bestScore=score;angles=trial;spread=s;twist=roll;result=rotations;bestPenetration=maximum;}
                }
            }
            gripFit[key]=result;
            File.AppendAllText(Evidence+"/grip_authoring.txt",side+finger+"="+string.Join(",",angles)+" spread="+spread+" twist="+twist+
                " authoringBoxPenetration="+bestPenetration+" score="+bestScore+"\n");
            return result;
        }
        private static bool TriangleOverlapsBox(Vector3 a,Vector3 b,Vector3 c,Bounds box)
        {
            a-=box.center;b-=box.center;c-=box.center;Vector3 extent=box.extents;
            bool Separated(Vector3 axis)
            {
                float pa=Vector3.Dot(a,axis),pb=Vector3.Dot(b,axis),pc=Vector3.Dot(c,axis);
                float radius=extent.x*Mathf.Abs(axis.x)+extent.y*Mathf.Abs(axis.y)+extent.z*Mathf.Abs(axis.z);
                return Mathf.Min(pa,Mathf.Min(pb,pc))>radius||Mathf.Max(pa,Mathf.Max(pb,pc))<-radius;
            }
            if(Separated(Vector3.right)||Separated(Vector3.up)||Separated(Vector3.forward))return false;
            Vector3 ab=b-a,bc=c-b,ca=a-c;if(Separated(Vector3.Cross(ab,bc)))return false;
            return !(Separated(Vector3.Cross(ab,Vector3.right))||Separated(Vector3.Cross(ab,Vector3.up))||Separated(Vector3.Cross(ab,Vector3.forward))||
                Separated(Vector3.Cross(bc,Vector3.right))||Separated(Vector3.Cross(bc,Vector3.up))||Separated(Vector3.Cross(bc,Vector3.forward))||
                Separated(Vector3.Cross(ca,Vector3.right))||Separated(Vector3.Cross(ca,Vector3.up))||Separated(Vector3.Cross(ca,Vector3.forward)));
        }
        private static float WeightFor(BoneWeight w,int id)=>
            (w.boneIndex0==id?w.weight0:0)+(w.boneIndex1==id?w.weight1:0)+(w.boneIndex2==id?w.weight2:0)+(w.boneIndex3==id?w.weight3:0);
        private static Vector3 FingerBendDirection(string side,string finger)=>
            finger=="Thumb"?new Vector3(side=="Left"?1f:-1f,0,1f).normalized:side=="Left"?Vector3.right:Vector3.left;

        internal static void EnterPlayerHandRigReview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Hand review already playing.");
            SessionState.SetBool(ReviewPending,true);
            EditorApplication.EnterPlaymode();
        }
        internal static void InspectPlayerHandRigReviewState()
        {
            var report=new StringBuilder();
            report.AppendLine("playing="+EditorApplication.isPlaying+" paused="+EditorApplication.isPaused);
            report.AppendLine("skinningMode="+PlayerSettings.meshDeformation+" pendingRestore="+SessionState.GetInt(OriginalSkinningMode,-1));
            report.AppendLine("frameDebugger="+UnityEngine.FrameDebugger.enabled+" animationPreview="+AnimationMode.InAnimationMode());
            report.AppendLine("temporaryReviewRoots="+Resources.FindObjectsOfTypeAll<GameObject>().Count(o=>o.scene.IsValid()&&o.name==ReviewRootName));
            report.AppendLine("ownedOrphanReviews="+FindOwnedOrphanReviews().Length);
            report.AppendLine("ownedOrphanReviewCameras="+FindOwnedOrphanReviewCameras().Length);
            foreach(var skin in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>().Where(r=>r.gameObject.scene.IsValid()&&
                (r.transform.parent.name=="Shotgun_Reload"||r.transform.parent.name=="Hands_Throw_Ready"||r.transform.parent.name=="Stick_Throw_Ready")))
            {
                Mesh mesh=skin.sharedMesh;var baked=new Mesh();
                try
                {
                    skin.BakeMesh(baked);
                    report.AppendLine("corrective="+RelativePath(skin.transform,null)+" enabled="+skin.enabled+" forceOff="+skin.forceRenderingOff+
                        " mesh="+AssetDatabase.GetAssetPath(mesh)+" verts="+mesh.vertexCount+" submeshes="+mesh.subMeshCount+" triangles="+mesh.triangles.Length/3+
                        " localBounds="+skin.localBounds+" meshBounds="+mesh.bounds+" bakedBounds="+baked.bounds+" root="+skin.rootBone.name+
                        " bones="+skin.bones.Length+" bindposes="+mesh.bindposes.Length+" finite="+baked.vertices.All(p=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)));
                    for(int sub=0;sub<mesh.subMeshCount;sub++)report.AppendLine("submesh="+sub+" indices="+mesh.GetIndexCount(sub)+" topology="+mesh.GetTopology(sub));
                }
                finally{UnityEngine.Object.DestroyImmediate(baked);}
            }
            foreach(var orphan in Resources.FindObjectsOfTypeAll<PlayerHandRigReviewObserver>().Where(o=>!o.gameObject.scene.IsValid()&&!EditorUtility.IsPersistent(o)))
                report.AppendLine("orphanReview="+RelativePath(orphan.transform,null)+" id="+orphan.gameObject.GetInstanceID()+" kind="+orphan.ReviewKind+
                    " root="+orphan.transform.root.name+" hide="+orphan.gameObject.hideFlags+" position="+orphan.transform.position.ToString("F5")+
                    " controller="+AssetDatabase.GetAssetPath(orphan.Animator==null?null:orphan.Animator.runtimeAnimatorController));
            foreach(var camera in Resources.FindObjectsOfTypeAll<Camera>().Where(c=>c.gameObject.scene.IsValid()||c.name=="PlayerHandsContinuousReviewCamera"||c.name=="ReadOnlyHandReviewCamera"))
                report.AppendLine("camera="+RelativePath(camera.transform,null)+" enabled="+camera.enabled+" depth="+camera.depth+" target="+(camera.targetTexture==null?"screen":camera.targetTexture.name)+
                    " sceneValid="+camera.gameObject.scene.IsValid()+" hide="+camera.gameObject.hideFlags);
            foreach(var observer in Resources.FindObjectsOfTypeAll<PlayerHandRigReviewObserver>().Where(o=>o.gameObject.scene.IsValid()))
            {
                report.AppendLine("observer="+RelativePath(observer.transform,null)+" position="+observer.transform.position+" scene="+observer.gameObject.scene.path);
                report.AppendLine("time="+(Mathf.Repeat(observer.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1)*6));
                foreach(var t in observer.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("Proximal")||t.name.Contains("Intermediate")||t.name.Contains("Distal")))
                    report.AppendLine(t.name+" rotation="+t.localRotation.ToString("F5"));
                foreach(var r in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>().Where(r=>r.gameObject.scene.IsValid()&&Vector3.Distance(r.transform.position,observer.transform.position)<2f))
                    report.AppendLine("nearby="+RelativePath(r.transform,null)+" mesh="+AssetDatabase.GetAssetPath(r.sharedMesh)+" position="+r.transform.position);
                var observerSkin=observer.GetComponentInChildren<SkinnedMeshRenderer>();
                var sourceSkin=AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath).GetComponentInChildren<SkinnedMeshRenderer>();
                for(int bone=0;bone<observerSkin.bones.Length;bone++)
                    if(observerSkin.bones[bone].name!=sourceSkin.bones[bone].name)report.AppendLine("boneOrderMismatch="+bone+":"+observerSkin.bones[bone].name+"/"+sourceSkin.bones[bone].name);
                foreach(var r in Resources.FindObjectsOfTypeAll<Renderer>().Where(r=>r.gameObject.scene.IsValid()&&r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.Intersects(observerSkin.bounds)))
                    report.AppendLine("overlappingBounds="+RelativePath(r.transform,null)+" type="+r.GetType().Name+" bounds="+r.bounds);
            }
            foreach(string path in new[]{ModelPath,CandidatePath})
            {
                var r=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<SkinnedMeshRenderer>();
                report.AppendLine(path+" bounds="+r.sharedMesh.bounds+" bones="+r.bones.Length);
                for(int i=0;i<r.bones.Length;i++)
                {
                    Matrix4x4 bind=r.transform.worldToLocalMatrix*r.bones[i].localToWorldMatrix*r.sharedMesh.bindposes[i];
                    float error=0;for(int row=0;row<4;row++)for(int col=0;col<4;col++)error=Mathf.Max(error,Mathf.Abs(bind[row,col]-(row==col?1:0)));
                    if(error>.00001f)report.AppendLine("bindRest="+r.bones[i].name+" error="+error+" matrix="+bind.ToString("F5"));
                }
                foreach(string side in new[]{"Left","Right"})
                {
                    Transform hand=r.bones.Single(t=>t.name==side+"Hand");
                    Matrix4x4 toHand=r.sharedMesh.bindposes[Array.IndexOf(r.bones,hand)];
                    Vector3[] vertices=r.sharedMesh.vertices;BoneWeight[] weights=r.sharedMesh.boneWeights;
                    var nearby=new Dictionary<string,int>();
                    for(int v=0;v<vertices.Length;v++)
                    {
                        Vector3 p=toHand.MultiplyPoint3x4(vertices[v]);
                        if(p.y<.13f||p.y>.26f||Mathf.Abs(p.x)>.12f||Mathf.Abs(p.z)>.12f)continue;
                        string bone=r.bones[weights[v].boneIndex0].name;
                        if(path==CandidatePath&&bone==side+"Hand"&&p.y>.17f)
                            report.AppendLine("retainedDistal="+side+" position="+p.ToString("F5")+" weight="+weights[v].weight0);
                        nearby[bone]=nearby.TryGetValue(bone,out int count)?count+1:1;
                    }
                    report.AppendLine(side+" geographicDistalWeights="+string.Join(";",nearby.Select(p=>p.Key+":"+p.Value)));
                }
            }
            File.WriteAllText(Evidence+"/review_state.txt",report.ToString());Debug.Log(report.ToString());
        }
        internal static void InspectPlayerHandRigMotionSkin()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new InvalidOperationException("Natural playback required.");
            if(inspectMotionSkin||skinInspectionCamera!=null)throw new InvalidOperationException("A surface inspection is already pending.");
            inspectMotionSkin=true;PlayerHandRigReviewObserver.PoseRendered-=InspectMotionSkinFrame;
            PlayerHandRigReviewObserver.PoseRendered+=InspectMotionSkinFrame;
        }
        internal static void InspectPlayerHandRigGrip()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new InvalidOperationException("Natural playback required.");
            if(inspectGripSkin||gripCycleReport!=null)throw new InvalidOperationException("A grip surface observation is already pending.");
            inspectGripSkin=true;PlayerHandRigReviewObserver.PoseRendered+=InspectGripSkinFrame;
            gripCyclePhases.Clear();gripCycleStarted=EditorApplication.timeSinceStartup;
            gripCycleReport=new StringBuilder("Read-only natural grip cycle; 24 quarter-second bins; no pose/time/visibility writes.\n");
            PlayerHandRigReviewObserver.PoseRendered+=InspectGripCycleFrame;
        }
        private static void InspectGripCycleFrame(PlayerHandRigReviewObserver observer,float time)
        {
            if(gripCycleReport==null||observer.ReviewKind!="BatteryGrip")return;
            if(EditorApplication.timeSinceStartup-gripCycleStarted>120)
            {
                gripCycleReport.AppendLine("coverage=INCOMPLETE; timeout");FinishGripCycleObservation();return;
            }
            // Observe every naturally rendered frame until every quarter-second bin is covered.
            // Retain bin coverage reporting, without dropping subsequent frames within a bin.
            int phase=Mathf.Clamp(Mathf.FloorToInt(time*4),0,23);gripCyclePhases.Add(phase);
            SkinnedMeshRenderer skin=observer.GetComponentInChildren<SkinnedMeshRenderer>();
            var baked=new Mesh();
            try
            {
                skin.BakeMesh(baked);Vector3[] points=baked.vertices;BoneWeight[] weights=skin.sharedMesh.boneWeights;
                foreach(string side in new[]{"Left","Right"})
                {
                    Transform hand=skin.bones.Single(b=>b.name==side+"Hand");
                    MeshFilter item=hand.Find(side+"BatteryGripReview").GetComponentInChildren<MeshFilter>();
                    Matrix4x4 itemToHand=hand.worldToLocalMatrix*item.transform.localToWorldMatrix;
                    Matrix4x4 skinToHand=hand.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                    var surface=new HandItemSurface(item.sharedMesh.vertices.Select(itemToHand.MultiplyPoint3x4).ToArray(),
                        item.sharedMesh.triangles,itemToHand.inverse,item.sharedMesh.bounds);
                    int inside=0;float depth=0;string worst="none";Vector3 worstPoint=Vector3.zero;
                    for(int v=0;v<points.Length;v++)
                    {
                        string name=skin.bones[weights[v].boneIndex0].name;
                        if(!name.StartsWith(side,StringComparison.Ordinal))continue;
                        Vector3 point=skinToHand.MultiplyPoint3x4(points[v]);
                        if(!surface.Bounds.Contains(point))continue;
                        float d=surface.SignedDistance(point);
                        if(d<-.0005f){inside++;if(-d>depth){depth=-d;worst=name;worstPoint=point;}}
                    }
                    int crossed=0,firstCrossed=-1;int[] indices=skin.sharedMesh.triangles;
                    for(int t=0;t<indices.Length;t+=3)
                    {
                        if(!skin.bones[weights[indices[t]].boneIndex0].name.StartsWith(side,StringComparison.Ordinal))continue;
                        if(!surface.CrossesTriangle(skinToHand.MultiplyPoint3x4(points[indices[t]]),
                            skinToHand.MultiplyPoint3x4(points[indices[t+1]]),skinToHand.MultiplyPoint3x4(points[indices[t+2]])))continue;
                        crossed++;if(firstCrossed<0)firstCrossed=t/3;
                    }
                    gripCycleReport.AppendLine("bin="+phase+" time="+time.ToString("F5")+" frame="+Time.frameCount+" side="+side+
                        " insideOver0.5mm="+inside+" maximumPenetration="+depth+" worst="+worst+" point="+worstPoint.ToString("F5")+
                        " crossingHandTriangles="+crossed+" firstCrossingTriangle="+firstCrossed);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(baked);}
            if(gripCyclePhases.Count==24)
            {gripCycleReport.AppendLine("coverage=COMPLETE; observed vertex and non-coplanar triangle crossings, not a substitute for direct visual review or continuous-time proof.");FinishGripCycleObservation();}
        }
        private static void FinishGripCycleObservation()
        {
            PlayerHandRigReviewObserver.PoseRendered-=InspectGripCycleFrame;
            File.WriteAllText(Evidence+"/grip_cycle_"+DateTime.Now.ToString("HHmmss")+".txt",gripCycleReport.ToString());
            gripCycleReport=null;gripCyclePhases.Clear();
        }
        private static void InspectGripSkinFrame(PlayerHandRigReviewObserver observer,float time)
        {
            if(!inspectGripSkin||observer.ReviewKind!="BatteryGrip"||time<3.2f||time>3.8f)return;
            inspectGripSkin=false;PlayerHandRigReviewObserver.PoseRendered-=InspectGripSkinFrame;
            var report=new StringBuilder("Natural grip surface observation time="+time+" frame="+Time.frameCount+"\n");
            var sharedPose=AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(RigFolder+"/SharedBatteryGripPose.asset");
            if(sharedPose!=null)foreach(var joint in sharedPose.Joints)
            {
                Transform bone=observer.GetComponentsInChildren<Transform>(true).Single(t=>t.name==joint.BoneName);
                report.AppendLine("profileComparison="+joint.BoneName+" angle="+Quaternion.Angle(bone.localRotation,joint.LocalRotation)+
                    " actual="+bone.localRotation.ToString("F6")+" position="+bone.localPosition.ToString("F6"));
            }
            SkinnedMeshRenderer skin=observer.GetComponentInChildren<SkinnedMeshRenderer>();
            var baked=new Mesh();skin.BakeMesh(baked);
            try
            {
                Vector3[] vertices=baked.vertices;BoneWeight[] weights=skin.sharedMesh.boneWeights;
                foreach(string side in new[]{"Left","Right"})
                {
                    Transform hand=skin.bones.Single(b=>b.name==side+"Hand");
                    MeshFilter item=hand.Find(side+"BatteryGripReview").GetComponentInChildren<MeshFilter>();
                    Matrix4x4 itemToHand=hand.worldToLocalMatrix*item.transform.localToWorldMatrix;
                    var surface=new HandItemSurface(item.sharedMesh.vertices.Select(itemToHand.MultiplyPoint3x4).ToArray(),item.sharedMesh.triangles,itemToHand.inverse,item.sharedMesh.bounds);
                    Matrix4x4 skinToHand=hand.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                    report.AppendLine(side+" batteryHandBounds="+surface.Bounds+" grip="+item.transform.parent.localPosition);
                    Matrix4x4 bindToHand=skin.sharedMesh.bindposes[Array.IndexOf(skin.bones,hand)];
                    Vector3[] bindVertices=skin.sharedMesh.vertices.Select(bindToHand.MultiplyPoint3x4).ToArray();
                    var edges=new HashSet<(int a,int b)>();int[] triangles=skin.sharedMesh.triangles;
                    for(int t=0;t<triangles.Length;t+=3)for(int e=0;e<3;e++)
                    {
                        int a=triangles[t+e],b=triangles[t+(e+1)%3];
                        if(!skin.bones[weights[a].boneIndex0].name.StartsWith(side+"Thumb",StringComparison.Ordinal)&&
                           !skin.bones[weights[b].boneIndex0].name.StartsWith(side+"Thumb",StringComparison.Ordinal))continue;
                        edges.Add((Mathf.Min(a,b),Mathf.Max(a,b)));
                    }
                    foreach(var edge in edges.Select(e=>(e.a,e.b,rest:Vector3.Distance(bindVertices[e.a],bindVertices[e.b]),
                        posed:Vector3.Distance(skinToHand.MultiplyPoint3x4(vertices[e.a]),skinToHand.MultiplyPoint3x4(vertices[e.b]))))
                        .Where(e=>e.rest>.001f).OrderByDescending(e=>e.posed/e.rest).Take(12))
                    {
                        report.AppendLine(side+" thumbEdge="+edge.a+","+edge.b+" rest="+edge.rest+" posed="+edge.posed+" ratio="+edge.posed/edge.rest);
                        foreach(int v in new[]{edge.a,edge.b})
                        {
                            BoneWeight w=weights[v];report.AppendLine(" edgeVertex="+v+" bind="+bindVertices[v].ToString("F5")+
                                " weights="+skin.bones[w.boneIndex0].name+":"+w.weight0+","+skin.bones[w.boneIndex1].name+":"+w.weight1+","+
                                skin.bones[w.boneIndex2].name+":"+w.weight2+","+skin.bones[w.boneIndex3].name+":"+w.weight3);
                        }
                    }
                    foreach(string finger in new[]{"Hand","Thumb","Index","Middle","Ring","Little"})
                    {
                        int count=0,contacts=0,inside=0;float minimum=float.PositiveInfinity,maximumPenetration=0;
                        Vector3 worst=Vector3.zero;int worstVertex=-1;
                        for(int v=0;v<vertices.Length;v++)
                        {
                            string name=skin.bones[weights[v].boneIndex0].name;
                            if(finger=="Hand"?name!=side+finger:!name.StartsWith(side+finger,StringComparison.Ordinal))continue;
                            Vector3 p=skinToHand.MultiplyPoint3x4(vertices[v]);
                            if(p.y<.035f)continue;
                            float distance=surface.SignedDistance(p);minimum=Mathf.Min(minimum,Mathf.Abs(distance));count++;
                            if(Mathf.Abs(distance)<=.003f)contacts++;
                            if(distance<-.0005f){inside++;if(-distance>maximumPenetration){maximumPenetration=-distance;worst=p;worstVertex=v;}}
                        }
                        report.AppendLine(side+finger+" samples="+count+" within3mm="+contacts+" nearest="+minimum+
                            " insideOver0.5mm="+inside+" maximumPenetration="+maximumPenetration+" worstHandPoint="+worst.ToString("F5"));
                        if(finger!="Hand")
                        {
                            int distal=Array.FindIndex(skin.bones,b=>b.name==side+finger+"Distal");
                            Vector3 distalBind=(bindToHand*skin.sharedMesh.bindposes[distal].inverse).MultiplyPoint3x4(Vector3.zero);
                            float sign=side=="Left"?1:-1;
                            Bounds itemBounds=item.sharedMesh.bounds;
                            float faceSign=Mathf.Sign(Vector3.Dot(itemToHand.MultiplyVector(Vector3.forward),Vector3.right*sign))*(finger=="Thumb"?1:-1);
                            float face=itemBounds.center.z+faceSign*itemBounds.extents.z;
                            int padCount=0,padNear=0;float nearestPad=float.PositiveInfinity;Vector3 nearestPoint=Vector3.zero;
                            for(int v=0;v<vertices.Length;v++)
                            {
                                Vector3 sourcePoint=bindVertices[v];
                                if(WeightFor(weights[v],distal)<=.15f||sourcePoint.y<=distalBind.y-.006f||
                                    (finger=="Thumb"?sourcePoint.z-distalBind.z:sign*(sourcePoint.x-distalBind.x))<=.002f)continue;
                                Vector3 p=skinToHand.MultiplyPoint3x4(vertices[v]),local=itemToHand.inverse.MultiplyPoint3x4(p);
                                Vector3 target=itemToHand.MultiplyPoint3x4(new Vector3(Mathf.Clamp(local.x,itemBounds.min.x,itemBounds.max.x),
                                    Mathf.Clamp(local.y,itemBounds.min.y,itemBounds.max.y),face));
                                float distance=Vector3.Distance(p,target);padCount++;
                                if(distance<nearestPad){nearestPad=distance;nearestPoint=p;}
                                if(distance<=.003f)padNear++;
                            }
                            report.AppendLine(side+finger+"Pad sourceSamples="+padCount+" nearOpposedBroadFace3mm="+padNear+
                                " nearestOpposedBroadFace="+nearestPad+" nearestHandPoint="+nearestPoint.ToString("F6"));
                        }
                        if(worstVertex>=0)
                        {
                            BoneWeight w=weights[worstVertex];
                            report.AppendLine("worstVertex="+worstVertex+" bindHandPoint="+skin.sharedMesh.bindposes[Array.IndexOf(skin.bones,hand)].MultiplyPoint3x4(skin.sharedMesh.vertices[worstVertex]).ToString("F5")+
                                " weights="+skin.bones[w.boneIndex0].name+":"+w.weight0+","+skin.bones[w.boneIndex1].name+":"+w.weight1+","+
                                skin.bones[w.boneIndex2].name+":"+w.weight2+","+skin.bones[w.boneIndex3].name+":"+w.weight3);
                        }
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(baked);}
            File.WriteAllText(Evidence+"/grip_surface_"+DateTime.Now.ToString("HHmmss")+".txt",report.ToString());
        }
        // Pure geometry helper shared by authoring and read-only supporting observations.
        internal sealed class HandItemSurface
        {
            private readonly Vector3[] vertices;
            private readonly int[] triangles;
            private readonly Bounds[] triangleBounds;
            private readonly Matrix4x4 pointToItem;
            private readonly Bounds itemBounds;
            public Bounds Bounds {get;}
            public HandItemSurface(Vector3[] points,int[] indices,Matrix4x4 toItem,Bounds originalBounds)
            {
                vertices=points;triangles=indices;pointToItem=toItem;itemBounds=originalBounds;
                var total=new Bounds(points[0],Vector3.zero);foreach(Vector3 p in points)total.Encapsulate(p);Bounds=total;
                triangleBounds=new Bounds[indices.Length/3];
                for(int t=0;t<indices.Length;t+=3)
                {var b=new Bounds(points[indices[t]],Vector3.zero);b.Encapsulate(points[indices[t+1]]);b.Encapsulate(points[indices[t+2]]);triangleBounds[t/3]=b;}
            }
            public float SignedDistance(Vector3 point)
            {
                float nearest=float.PositiveInfinity;
                for(int t=0;t<triangles.Length;t+=3)
                {
                    if(triangleBounds[t/3].SqrDistance(point)>nearest)continue;
                    Vector3 closest=ClosestTrianglePoint(point,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]]);
                    nearest=Mathf.Min(nearest,(closest-point).sqrMagnitude);
                }
                // A rotated item's hand-space AABB includes empty corners. They cannot be inside its mesh.
                float distance=Mathf.Sqrt(nearest);if(distance<.00001f||!itemBounds.Contains(pointToItem.MultiplyPoint3x4(point)))return distance;
                Vector3 direction=new Vector3(1,.313f,.177f).normalized;var intersections=new List<float>();
                for(int t=0;t<triangles.Length;t+=3)
                {
                    Vector3 a=vertices[triangles[t]],e1=vertices[triangles[t+1]]-a,e2=vertices[triangles[t+2]]-a;
                    Vector3 p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-12f)continue;
                    float inv=1/det;Vector3 offset=point-a;float u=Vector3.Dot(offset,p)*inv;if(u<0||u>1)continue;
                    Vector3 q=Vector3.Cross(offset,e1);float v=Vector3.Dot(direction,q)*inv;if(v<0||u+v>1)continue;
                    float d=Vector3.Dot(e2,q)*inv;if(d<=.000001f)continue;
                    if(!intersections.Any(previous=>Mathf.Abs(previous-d)<.00001f))intersections.Add(d);
                }
                return intersections.Count%2==1?-distance:distance;
            }
            public bool CrossesTriangle(Vector3 a,Vector3 b,Vector3 c)
            {
                var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);
                if(!Bounds.Intersects(bounds))return false;
                for(int t=0;t<triangles.Length;t+=3)
                {
                    if(!triangleBounds[t/3].Intersects(bounds))continue;
                    Vector3 p=vertices[triangles[t]],q=vertices[triangles[t+1]],r=vertices[triangles[t+2]];
                    if(SegmentCrossesTriangle(a,b,p,q,r)||SegmentCrossesTriangle(b,c,p,q,r)||SegmentCrossesTriangle(c,a,p,q,r)||
                        SegmentCrossesTriangle(p,q,a,b,c)||SegmentCrossesTriangle(q,r,a,b,c)||SegmentCrossesTriangle(r,p,a,b,c))return true;
                }
                return false;
            }
            private static bool SegmentCrossesTriangle(Vector3 start,Vector3 end,Vector3 a,Vector3 b,Vector3 c)
            {
                Vector3 e1=b-a,e2=c-a,normal=Vector3.Cross(e1,e2);float magnitude=normal.magnitude;
                if(magnitude<1e-14f)return false;normal/=magnitude;
                float first=Vector3.Dot(start-a,normal),second=Vector3.Dot(end-a,normal);
                // Exclude coplanar/tangent contact and 0.01 mm floating-point ambiguity, not 0.5 mm penetration.
                if(Mathf.Abs(first)<.00001f||Mathf.Abs(second)<.00001f||first*second>=0)return false;
                Vector3 direction=end-start,p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);
                if(Mathf.Abs(det)<1e-14f)return false;
                Vector3 offset=start-a;float u=Vector3.Dot(offset,p)/det;if(u<0||u>1)return false;
                Vector3 q=Vector3.Cross(offset,e1);float v=Vector3.Dot(direction,q)/det;if(v<0||u+v>1)return false;
                float fraction=Vector3.Dot(e2,q)/det;return fraction>0&&fraction<1;
            }
            internal static Vector3 ClosestTrianglePoint(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
            {
                Vector3 ab=b-a,ac=c-a,ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
                if(d1<=0&&d2<=0)return a;
                Vector3 bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
                float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
                Vector3 cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
                float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
                float va=d3*d6-d5*d4;if(va<=0&&d4-d3>=0&&d5-d6>=0)return b+(c-b)*((d4-d3)/(d4-d3+d5-d6));
                float denominator=va+vb+vc;if(Mathf.Abs(denominator)<1e-20f)return a;
                return a+ab*(vb/denominator)+ac*(vc/denominator);
            }
        }
        private static void InspectMotionSkinFrame(PlayerHandRigReviewObserver observer,float time)
        {
            if(!inspectMotionSkin||observer.ReviewKind!="Articulation"||time<3.2f||time>3.8f)return;
            // A freshly allocated observation target has no earlier animation images to retain.
            if(skinInspectionCamera==null)
            {
                skinRenderTrace=new StringBuilder();skinTraceObserver=observer;
                skinInspectionCamera=CreateContinuousHandCamera(observer.gameObject,3);
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering+=TraceHandCameraBegin;
                UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=TraceHandCameraEnd;
                return;
            }
            inspectMotionSkin=false;PlayerHandRigReviewObserver.PoseRendered-=InspectMotionSkinFrame;
            var report=new StringBuilder("Natural playback skin inspection time="+time+"\n");
            report.AppendLine("endOfFrame="+Time.frameCount);
            report.Append(skinRenderTrace);
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=TraceHandCameraBegin;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=TraceHandCameraEnd;
            // Read the exact texture resource independently of RenderTexture.active/ReadPixels.
            var readback=UnityEngine.Rendering.AsyncGPUReadback.Request(skinInspectionCamera.targetTexture,0,TextureFormat.RGBA32);
            readback.WaitForCompletion();
            if(readback.hasError)throw new InvalidOperationException("Hand texture GPU readback failed.");
            Color32[] explicitPixels=readback.GetData<Color32>().ToArray(),activePixels=ReadHandCamera(skinInspectionCamera);
            int pixelMismatch=0,flippedMismatch=0;
            for(int p=0;p<activePixels.Length;p++)
            {
                if(!activePixels[p].Equals(explicitPixels[p]))pixelMismatch++;
                if(!activePixels[p].Equals(explicitPixels[(599-p/600)*600+p%600]))flippedMismatch++;
            }
            report.AppendLine("explicitTextureReadbackMismatch="+pixelMismatch+" flippedMismatch="+flippedMismatch);
            SkinnedMeshRenderer live=observer.GetComponentInChildren<SkinnedMeshRenderer>();
            SkinnedMeshRenderer source=AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath).GetComponentInChildren<SkinnedMeshRenderer>();
            Vector3[] gpuBeforeBake;
            using(var buffer=live.GetVertexBuffer())
            {
                float[] raw=new float[buffer.count*buffer.stride/4];buffer.GetData(raw);int stride=buffer.stride/4;
                Matrix4x4 toMesh=live.transform.worldToLocalMatrix*live.rootBone.localToWorldMatrix;
                gpuBeforeBake=Enumerable.Range(0,buffer.count).Select(v=>toMesh.MultiplyPoint3x4(new Vector3(raw[v*stride],raw[v*stride+1],raw[v*stride+2]))).ToArray();
            }
            Mesh baked=new Mesh();live.BakeMesh(baked);
            try
            {
                Vector3[] rest=source.sharedMesh.vertices,posed=baked.vertices;BoneWeight[] weights=source.sharedMesh.boneWeights;
                report.AppendLine("gpuBeforeBakeMaxVsCpu="+Enumerable.Range(0,posed.Length).Max(v=>Vector3.Distance(gpuBeforeBake[v],posed[v])));
                report.AppendLine("bakedIndicesMatch="+baked.triangles.SequenceEqual(live.sharedMesh.triangles));
                Matrix4x4[] skinMatrices=live.bones.Select((b,i)=>live.transform.worldToLocalMatrix*b.localToWorldMatrix*live.sharedMesh.bindposes[i]).ToArray();
                float linearMax=0;int linearWorst=-1;
                for(int v=0;v<posed.Length;v++)
                {
                    BoneWeight w=weights[v];Vector3 p=rest[v];
                    Vector3 linear=skinMatrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+skinMatrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                        skinMatrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+skinMatrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                    float error=Vector3.Distance(linear,posed[v]);if(error>linearMax){linearMax=error;linearWorst=v;}
                }
                report.AppendLine("explicitLinearMaxVsBake="+linearMax+" worstVertex="+linearWorst+" meshDeformation="+PlayerSettings.meshDeformation+" qualityWeights="+QualitySettings.skinWeights);
                report.AppendLine("renderMatrix="+live.localToWorldMatrix.ToString("F6")+" transformMatrix="+live.transform.localToWorldMatrix.ToString("F6")+" rootMatrix="+live.rootBone.localToWorldMatrix.ToString("F6"));
                report.AppendLine("renderState staticBatch="+live.isPartOfStaticBatch+" forceOff="+live.forceRenderingOff+" quality="+live.quality+" motionVectors="+live.motionVectorGenerationMode+" skinMotion="+live.skinnedMotionVectors+" propertyBlock="+live.HasPropertyBlock());
                report.AppendLine("vertexLayout="+string.Join(";",live.sharedMesh.GetVertexAttributes().Select(a=>a.ToString())));
                using(var indices=live.sharedMesh.GetIndexBuffer())
                {
                    int[] cpuIndices=live.sharedMesh.triangles;
                    int[] gpuIndices;
                    if(live.sharedMesh.indexFormat==UnityEngine.Rendering.IndexFormat.UInt16)
                    {var raw=new ushort[indices.count];indices.GetData(raw);gpuIndices=raw.Select(i=>(int)i).ToArray();}
                    else{var raw=new uint[indices.count];indices.GetData(raw);gpuIndices=raw.Select(i=>(int)i).ToArray();}
                    var submesh=live.sharedMesh.GetSubMesh(0);
                    int[] drawnIndices=gpuIndices.Skip(submesh.indexStart).Take(submesh.indexCount).Select(i=>i+submesh.baseVertex).ToArray();
                    report.AppendLine("gpuDrawIndicesMatch="+cpuIndices.SequenceEqual(drawnIndices)+" gpuIndexCapacity="+indices.count+" cpuIndexCount="+cpuIndices.Length+
                        " submeshStart="+submesh.indexStart+" submeshCount="+submesh.indexCount+" baseVertex="+submesh.baseVertex);
                    report.AppendLine("firstIndexMismatch="+Enumerable.Range(0,Math.Min(cpuIndices.Length,drawnIndices.Length)).Where(i=>cpuIndices[i]!=drawnIndices[i]).DefaultIfEmpty(-1).First());
                }
                report.AppendLine("shader="+string.Join(";",live.sharedMaterials.Select(m=>m.shader.name+" passes="+m.passCount+" keywords="+string.Join(",",m.shaderKeywords))));
                // Compare renderer and transform coordinate conventions without changing either.
                Matrix4x4 meshToRender=live.transform.worldToLocalMatrix*live.localToWorldMatrix;
                report.AppendLine("rendererMatrixPixelRays:");InspectRenderedSurfacePixels(live,posed.Select(meshToRender.MultiplyPoint3x4).ToArray(),skinInspectionCamera,report);
                Vector3[] openPose=new Vector3[rest.Length];
                Matrix4x4[] openMatrices=source.bones.Select((b,i)=>source.transform.worldToLocalMatrix*b.localToWorldMatrix*source.sharedMesh.bindposes[i]).ToArray();
                for(int v=0;v<openPose.Length;v++)
                {
                    BoneWeight w=weights[v];Vector3 p=rest[v];
                    openPose[v]=openMatrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+openMatrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                        openMatrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+openMatrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                }
                report.AppendLine("sourceOpenPosePixelRays:");InspectRenderedSurfacePixels(live,openPose,skinInspectionCamera,report);
                report.AppendLine("unskinnedSourcePixelRays:");InspectRenderedSurfacePixels(live,rest,skinInspectionCamera,report);
                using(var previous=live.GetPreviousVertexBuffer())
                {
                    float[] raw=new float[previous.count*previous.stride/4];previous.GetData(raw);int stride=previous.stride/4;
                    Matrix4x4 toMesh=live.transform.worldToLocalMatrix*live.rootBone.localToWorldMatrix;
                    var vertices=Enumerable.Range(0,previous.count).Select(v=>toMesh.MultiplyPoint3x4(new Vector3(raw[v*stride],raw[v*stride+1],raw[v*stride+2]))).ToArray();
                    report.AppendLine("previousGpuMaxVsCpu="+Enumerable.Range(0,posed.Length).Max(v=>Vector3.Distance(vertices[v],posed[v])));
                    report.AppendLine("previousGpuPixelRays:");InspectRenderedSurfacePixels(live,vertices,skinInspectionCamera,report);
                }
                report.AppendLine("mesh="+live.sharedMesh.name+" vertices="+live.sharedMesh.vertexCount+" submeshes="+live.sharedMesh.subMeshCount+" bounds="+baked.bounds);
                foreach(string side in new[]{"Left","Right"})
                {
                    Transform hand=live.bones.Single(b=>b.name==side+"Hand");
                    int handIndex=Array.IndexOf(live.bones,hand);
                    Matrix4x4 toHand=hand.worldToLocalMatrix*live.transform.localToWorldMatrix;
                    var samples=Enumerable.Range(0,posed.Length).Where(v=>live.bones[weights[v].boneIndex0].name.StartsWith(side)&&
                        (live.bones[weights[v].boneIndex0].name.Contains("Hand")||live.bones[weights[v].boneIndex0].name.Contains("Proximal")||live.bones[weights[v].boneIndex0].name.Contains("Intermediate")||live.bones[weights[v].boneIndex0].name.Contains("Distal")))
                        .Select(v=>new{index=v,p=toHand.MultiplyPoint3x4(posed[v])}).OrderByDescending(v=>v.p.y).Take(12);
                    foreach(var sample in samples)report.AppendLine("furthestPosed="+side+" index="+sample.index+" bone="+live.bones[weights[sample.index].boneIndex0].name+" local="+sample.p.ToString("F5")+" bind="+source.sharedMesh.bindposes[handIndex].MultiplyPoint3x4(rest[sample.index]).ToString("F5"));
                }
                InspectVisibleHandSurfaces(live,posed,report);
                InspectRenderedSurfacePixels(live,posed,skinInspectionCamera,report);
                try
                {
                    using(var gpu=live.GetVertexBuffer())
                    {
                        report.AppendLine("gpu count="+gpu.count+" stride="+gpu.stride+" target="+gpu.target);
                        float[] data=new float[gpu.count*gpu.stride/4];gpu.GetData(data);
                        int stride=data.Length/posed.Length;float maxError=0,meanError=0;int mismatch=0;
                        float maxRootError=0,meanRootError=0;int rootMismatch=0;
                        Matrix4x4 rootToMesh=live.transform.worldToLocalMatrix*live.rootBone.localToWorldMatrix;
                        if(stride>=3)
                        {
                            for(int v=0;v<posed.Length;v++)
                            {
                                Vector3 actual=new Vector3(data[v*stride],data[v*stride+1],data[v*stride+2]);
                                float error=Vector3.Distance(actual,posed[v]);maxError=Mathf.Max(maxError,error);meanError+=error;
                                if(error>.001f)mismatch++;
                                float rootError=Vector3.Distance(rootToMesh.MultiplyPoint3x4(actual),posed[v]);
                                maxRootError=Mathf.Max(maxRootError,rootError);meanRootError+=rootError;if(rootError>.001f)rootMismatch++;
                            }
                            report.AppendLine("gpuPosition strideFloats="+stride+" maxVsCpu="+maxError+" meanVsCpu="+(meanError/posed.Length)+" mismatchOver1mm="+mismatch);
                            report.AppendLine("gpuRootSpace root="+live.rootBone.name+" maxVsCpu="+maxRootError+" meanVsCpu="+(meanRootError/posed.Length)+" mismatchOver1mm="+rootMismatch);
                        }
                    }
                }
                catch(Exception e){report.AppendLine("gpuReadUnavailable="+e.GetType().Name+" "+e.Message);}
                foreach(string side in new[]{"Left","Right"})
                {
                    Transform hand=source.bones.Single(t=>t.name==side+"Hand");int handIndex=Array.IndexOf(source.bones,hand);
                    foreach(int id in Enumerable.Range(0,source.bones.Length).Where(i=>source.bones[i].name.StartsWith(side)&&
                        (source.bones[i].name.Contains("Hand")||source.bones[i].name.Contains("Proximal")||source.bones[i].name.Contains("Intermediate")||source.bones[i].name.Contains("Distal"))))
                    {
                        int[] selected=Enumerable.Range(0,weights.Length).Where(v=>WeightFor(weights[v],id)>.5f).ToArray();
                        report.AppendLine(source.bones[id].name+" vertices="+selected.Length+" maxDisplacement="+selected.Select(v=>Vector3.Distance(rest[v],posed[v])).DefaultIfEmpty().Max());
                    }
                    for(int v=0;v<rest.Length;v++)
                    {
                        Vector3 p=source.sharedMesh.bindposes[handIndex].MultiplyPoint3x4(rest[v]);
                        if(WeightFor(weights[v],handIndex)>.9f&&p.y>.14f)report.AppendLine("unassignedDistal="+side+" vertex="+v+" handLocal="+p.ToString("F5")+" delta="+Vector3.Distance(rest[v],posed[v]));
                    }
                }
                foreach(var t in observer.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("Proximal")||t.name.Contains("Intermediate")||t.name.Contains("Distal")))
                    report.AppendLine(t.name+" rotation="+t.localRotation.ToString("F5"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
                if(skinInspectionCamera!=null)
                {var target=skinInspectionCamera.targetTexture;skinInspectionCamera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(skinInspectionCamera.gameObject);skinInspectionCamera=null;}
            }
            File.WriteAllText(Evidence+"/motion_skin.txt",report.ToString());
        }
        private static Vector3[] ReadWorldSkin(SkinnedMeshRenderer skin)
        {
            Vector3[] vertices=skin.sharedMesh.vertices;BoneWeight[] weights=skin.sharedMesh.boneWeights;
            Matrix4x4[] matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*skin.sharedMesh.bindposes[i]).ToArray();
            return Enumerable.Range(0,vertices.Length).Select(v=>
            {
                BoneWeight w=weights[v];Vector3 p=vertices[v];
                return matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+
                    matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
            }).ToArray();
        }
        private static void TraceHandCameraBegin(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera)
        {
            if(camera!=skinInspectionCamera||skinTraceObserver==null)return;
            var skin=skinTraceObserver.GetComponentInChildren<SkinnedMeshRenderer>();
            skinBeginWorld=ReadWorldSkin(skin);
            TraceHandCameraState(camera,skin,"begin");
        }
        private static void TraceHandCameraEnd(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera)
        {
            if(camera!=skinInspectionCamera||skinTraceObserver==null)return;
            var skin=skinTraceObserver.GetComponentInChildren<SkinnedMeshRenderer>();
            TraceHandCameraState(camera,skin,"end");
            Vector3[] world=ReadWorldSkin(skin);
            skinRenderTrace.AppendLine("beginEndBoneSkinDelta="+world.Select((p,i)=>Vector3.Distance(p,skinBeginWorld[i])).Max());
            // Do not filter by scene validity or renderer bounds: those assumptions could hide a second source.
            foreach(var renderer in Resources.FindObjectsOfTypeAll<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&
                (Vector3.Distance(r.transform.position,skin.transform.position)<10||r.bounds.Intersects(skin.bounds))))
            {
                skinRenderTrace.AppendLine("drawCandidate="+RelativePath(renderer.transform,null)+" id="+renderer.GetInstanceID()+
                    " type="+renderer.GetType().Name+" scene="+renderer.gameObject.scene.name+" sceneValid="+renderer.gameObject.scene.IsValid()+
                    " persistent="+EditorUtility.IsPersistent(renderer)+" hide="+renderer.hideFlags+" layer="+renderer.gameObject.layer+
                    " forceOff="+renderer.forceRenderingOff+" position="+renderer.transform.position+" bounds="+renderer.bounds);
                Mesh mesh=null;Vector3[] points=null;
                if(renderer is SkinnedMeshRenderer s&&s.sharedMesh!=null){mesh=s.sharedMesh;points=ReadWorldSkin(s);}
                else if(renderer.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null&&filter.sharedMesh.isReadable)
                {mesh=filter.sharedMesh;points=mesh.vertices.Select(renderer.localToWorldMatrix.MultiplyPoint3x4).ToArray();}
                if(mesh==null||points==null)continue;
                foreach(float v in new[]{.25f,.75f})
                {
                    Ray ray=camera.ViewportPointToRay(new Vector3(.45f,v,0));float distance=float.PositiveInfinity;int hit=-1;
                    int[] indices=mesh.triangles;
                    for(int t=0;t<indices.Length;t+=3)
                    {
                        Vector3 a=points[indices[t]],e1=points[indices[t+1]]-a,e2=points[indices[t+2]]-a,p=Vector3.Cross(ray.direction,e2);
                        float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-10f)continue;
                        float inv=1/det;Vector3 offset=ray.origin-a;float b=Vector3.Dot(offset,p)*inv;if(b<0||b>1)continue;
                        Vector3 q=Vector3.Cross(offset,e1);float c=Vector3.Dot(ray.direction,q)*inv;if(c<0||b+c>1)continue;
                        float d=Vector3.Dot(e2,q)*inv;if(d<0||d>=distance)continue;distance=d;hit=t;
                    }
                    skinRenderTrace.AppendLine("drawCandidateRay v="+v+" mesh="+mesh.name+" hit="+hit+" distance="+distance);
                }
            }
            File.WriteAllText(Evidence+"/camera_render_trace.txt",skinRenderTrace.ToString());
        }
        private static void TraceHandCameraState(Camera camera,SkinnedMeshRenderer skin,string stage)
        {
            skinRenderTrace.AppendLine("cameraStage="+stage+" frame="+Time.frameCount+" time="+
                (Mathf.Repeat(skinTraceObserver.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime,1)*6)+
                " cameraId="+camera.GetInstanceID()+" rendererId="+skin.GetInstanceID()+" meshId="+skin.sharedMesh.GetInstanceID()+
                " targetId="+camera.targetTexture.GetInstanceID()+" viewport="+camera.pixelRect+" debugger="+UnityEngine.FrameDebugger.enabled+
                " animationPreview="+AnimationMode.InAnimationMode()+" cullingMask="+camera.cullingMask);
            skinRenderTrace.AppendLine("view="+camera.worldToCameraMatrix.ToString("F6")+" projection="+camera.projectionMatrix.ToString("F6")+
                " gpuProjection="+GL.GetGPUProjectionMatrix(camera.projectionMatrix,true).ToString("F6")+
                " shaderViewProjection="+Shader.GetGlobalMatrix("unity_MatrixVP").ToString("F6"));
            skinRenderTrace.AppendLine("cameraComponents="+string.Join(";",camera.GetComponents<Component>().Select(c=>c.GetType().FullName)));
        }
        private static void SpawnHandReview()
        {
            CleanupReviewSession();
            GameObject session=new GameObject(ReviewRootName){hideFlags=HideFlags.DontSave};
            for (int i=0;i<2;i++)
            {
                string kind=i==0?"Articulation":"BatteryGrip";
                GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RigFolder+"/Review_"+kind+".prefab");
                GameObject instance=UnityEngine.Object.Instantiate(prefab);
                instance.transform.SetParent(session.transform,false);
                instance.transform.position=new Vector3(100+i*4,0,100);
                instance.hideFlags=HideFlags.DontSave;
            }
            var cameraObject=new GameObject("PlayerHandsContinuousReviewCamera"){hideFlags=HideFlags.DontSave};
            cameraObject.transform.SetParent(session.transform,false);
            Camera camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.5f;
            camera.transform.position=new Vector3(102,.9f,103);camera.transform.LookAt(new Vector3(102,.9f,100));
            camera.nearClipPlane=.1f;camera.farClipPlane=10f;camera.depth=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.06f,1);
            // Review rendering must not replace the user's gameplay camera output.
            camera.targetTexture=new RenderTexture(1024,768,24){name="PlayerHandsReviewTarget",hideFlags=HideFlags.DontSave};
            Debug.Log("Shared hand review is running in normal unpaused Animator playback.");
        }
        private static void CleanupReviewSession()
        {
            inspectMotionSkin=false;
            inspectGripSkin=false;PlayerHandRigReviewObserver.PoseRendered-=InspectGripSkinFrame;
            if(gripCycleReport!=null)
            {gripCycleReport.AppendLine("coverage=INCOMPLETE; review stopped");FinishGripCycleObservation();}
            PlayerHandRigReviewObserver.PoseRendered-=InspectMotionSkinFrame;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering-=TraceHandCameraBegin;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=TraceHandCameraEnd;
            skinRenderTrace=null;skinTraceObserver=null;skinBeginWorld=null;
            PlayerHandRigReviewObserver.PoseRendered-=ObserveHand;
            if(handObservation!=null){handObservation.Dispose();handObservation=null;}
            PlayerHandRigReviewObserver.PoseRendered-=ObserveSharedRig;
            EditorApplication.update-=FollowSharedRigCameras;
            if(sharedObservation!=null)
            {sharedObservation.Abort(new InvalidOperationException("Review ended before coverage completed."));sharedObservation.Dispose();sharedObservation=null;}
            foreach(var root in Resources.FindObjectsOfTypeAll<GameObject>().Where(o=>o.name==ReviewRootName&&
                !EditorUtility.IsPersistent(o)&&(o.hideFlags&HideFlags.DontSave)==HideFlags.DontSave).ToArray())
            {
                foreach(var camera in root.GetComponentsInChildren<Camera>(true))
                    if(camera.targetTexture!=null){var target=camera.targetTexture;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);}
                UnityEngine.Object.DestroyImmediate(root);
            }
            // Earlier review versions left DontSave roots outside a valid scene after Play Mode.
            // Match our exact observer, controller, name and placement before reclaiming only those temporary clones.
            foreach(var orphan in FindOwnedOrphanReviews())
            {
                File.AppendAllText(Evidence+"/orphan_cleanup.txt",DateTime.Now.ToString("s")+" removed="+orphan.name+
                    " id="+orphan.GetInstanceID()+" position="+orphan.transform.position+" source="+RigFolder+"/Review_"+orphan.ReviewKind+".prefab\n");
                UnityEngine.Object.DestroyImmediate(orphan.gameObject);
            }
            foreach(var camera in FindOwnedOrphanReviewCameras())
            {
                File.AppendAllText(Evidence+"/orphan_cleanup.txt",DateTime.Now.ToString("s")+" removedCamera="+camera.name+
                    " id="+camera.GetInstanceID()+" position="+camera.transform.position+" depth="+camera.depth+"\n");
                if(camera.targetTexture!=null)
                {var target=camera.targetTexture;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);}
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
            skinInspectionCamera=null;
        }
        private static Camera[] FindOwnedOrphanReviewCameras()=>Resources.FindObjectsOfTypeAll<Camera>().Where(c=>
            !c.gameObject.scene.IsValid()&&!EditorUtility.IsPersistent(c)&&c.transform.parent==null&&
            (c.gameObject.hideFlags&HideFlags.DontSave)==HideFlags.DontSave&&
            (c.name=="PlayerHandsContinuousReviewCamera"||c.name=="ReadOnlyHandReviewCamera")).ToArray();
        private static PlayerHandRigReviewObserver[] FindOwnedOrphanReviews()=>Resources.FindObjectsOfTypeAll<PlayerHandRigReviewObserver>().Where(o=>
            !o.gameObject.scene.IsValid()&&!EditorUtility.IsPersistent(o)&&o.transform.parent==null&&
            (o.gameObject.hideFlags&HideFlags.DontSave)==HideFlags.DontSave&&
            (o.ReviewKind=="Articulation"||o.ReviewKind=="BatteryGrip")&&o.name=="Review_"+o.ReviewKind+"(Clone)"&&
            Vector3.Distance(o.transform.position,new Vector3(o.ReviewKind=="Articulation"?100:104,0,100))<.001f&&
            o.Animator!=null&&AssetDatabase.GetAssetPath(o.Animator.runtimeAnimatorController)==RigFolder+"/Review_"+o.ReviewKind+".controller").ToArray();
        private static void InspectVisibleHandSurfaces(SkinnedMeshRenderer renderer,Vector3[] posed,StringBuilder report)
        {
            Vector3[] world=posed.Select(renderer.transform.TransformPoint).ToArray();
            int[] triangles=renderer.sharedMesh.triangles;BoneWeight[] weights=renderer.sharedMesh.boneWeights;
            foreach(string side in new[]{"Left","Right"})
            {
                Transform hand=renderer.bones.Single(t=>t.name==side+"Hand");
                foreach(Vector3 direction in new[]{side=="Left"?Vector3.right:Vector3.left,Vector3.forward})
                {
                Vector3 right=Vector3.Cross(Vector3.up,-direction),center=hand.position+hand.TransformVector(new Vector3(0,.125f,0));
                foreach(float u in new[]{.35f,.45f,.55f,.65f,.75f})foreach(float v in new[]{.5f,.6f,.7f,.8f,.9f})
                {
                    Vector3 origin=center+direction*3+right*((u-.5f)*.34f)+Vector3.up*((.5f-v)*.34f);
                    float nearest=3.22f;int hit=-1;
                    for(int t=0;t<triangles.Length;t+=3)
                    {
                        Vector3 a=world[triangles[t]],edge1=world[triangles[t+1]]-a,edge2=world[triangles[t+2]]-a;
                        Vector3 p=Vector3.Cross(-direction,edge2);float determinant=Vector3.Dot(edge1,p);
                        if(determinant<1e-10f)continue;
                        float inv=1/determinant;Vector3 offset=origin-a;float b=Vector3.Dot(offset,p)*inv;
                        if(b<0||b>1)continue;Vector3 q=Vector3.Cross(offset,edge1);float c=Vector3.Dot(-direction,q)*inv;
                        if(c<0||b+c>1)continue;float distance=Vector3.Dot(edge2,q)*inv;
                        if(distance<2.78f||distance>=nearest)continue;nearest=distance;hit=t;
                    }
                    if(hit<0)continue;
                    report.AppendLine("visibleRay="+side+" dir="+direction+" u="+u+" v="+v+" bones="+string.Join(",",Enumerable.Range(0,3)
                        .Select(i=>renderer.bones[weights[triangles[hit+i]].boneIndex0].name))+" handLocal="+hand.InverseTransformPoint(origin-direction*nearest).ToString("F5"));
                }
                }
            }
        }
        private static void InspectRenderedSurfacePixels(SkinnedMeshRenderer renderer,Vector3[] posed,Camera camera,StringBuilder report)
        {
            var pixels=ReadHandCamera(camera);Vector3[] world=posed.Select(renderer.transform.TransformPoint).ToArray();
            int[] triangles=renderer.sharedMesh.triangles;BoneWeight[] weights=renderer.sharedMesh.boneWeights;
            report.AppendLine("pixelCamera="+camera.transform.position.ToString("F5")+" aspect="+camera.aspect+" ortho="+camera.orthographicSize);
            Vector3[] viewport=world.Select(camera.WorldToViewportPoint).ToArray();
            int projectedHit=-1;float projectedDepth=float.PositiveInfinity;
            Vector2 sample=new Vector2(.45f,.25f);
            for(int t=0;t<triangles.Length;t+=3)
            {
                Vector3 a=viewport[triangles[t]],b=viewport[triangles[t+1]],c=viewport[triangles[t+2]];
                float denominator=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);
                if(Mathf.Abs(denominator)<1e-12f)continue;
                float wa=((b.y-c.y)*(sample.x-c.x)+(c.x-b.x)*(sample.y-c.y))/denominator;
                float wb=((c.y-a.y)*(sample.x-c.x)+(a.x-c.x)*(sample.y-c.y))/denominator;
                float wc=1-wa-wb;if(wa<0||wb<0||wc<0)continue;
                float depth=wa*a.z+wb*b.z+wc*c.z;
                if(depth<camera.nearClipPlane||depth>camera.farClipPlane||depth>=projectedDepth)continue;
                projectedDepth=depth;projectedHit=t;
            }
            report.AppendLine("independentViewportHit="+projectedHit+" depth="+projectedDepth+" cameraType="+camera.cameraType+" clearFlags="+camera.clearFlags);
            foreach(float u in new[]{.35f,.45f,.55f,.65f})foreach(float v in new[]{.2f,.25f,.3f,.35f,.4f,.45f,.5f})
            {
                Ray ray=camera.ViewportPointToRay(new Vector3(u,v,0));float nearest=camera.farClipPlane-camera.nearClipPlane;int hit=-1;
                float unclippedNearest=float.PositiveInfinity;int unclippedHit=-1;
                for(int t=0;t<triangles.Length;t+=3)
                {
                    Vector3 a=world[triangles[t]],e1=world[triangles[t+1]]-a,e2=world[triangles[t+2]]-a,p=Vector3.Cross(ray.direction,e2);
                    float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<1e-10f)continue;
                    float inv=1/det;Vector3 offset=ray.origin-a;float b=Vector3.Dot(offset,p)*inv;if(b<0||b>1)continue;
                    Vector3 q=Vector3.Cross(offset,e1);float c=Vector3.Dot(ray.direction,q)*inv;if(c<0||b+c>1)continue;
                    float d=Vector3.Dot(e2,q)*inv;
                    if(d>=-camera.nearClipPlane&&d<unclippedNearest){unclippedNearest=d;unclippedHit=t;}
                    if(d<0||d>=nearest)continue;nearest=d;hit=t;
                }
                Color32 color=pixels[(int)(v*600)*600+(int)(u*600)];
                report.AppendLine("pixelRay="+u+","+v+" rgb="+color.r+","+color.g+","+color.b+" hit="+(hit<0?"none":string.Join(",",Enumerable.Range(0,3).Select(i=>renderer.bones[weights[triangles[hit+i]].boneIndex0].name)))+
                    " unclipped="+(unclippedHit<0?"none":string.Join(",",Enumerable.Range(0,3).Select(i=>renderer.bones[weights[triangles[unclippedHit+i]].boneIndex0].name)))+" distanceFromNear="+unclippedNearest+" rayOrigin="+ray.origin.ToString("F5"));
            }
        }
        internal static void StopPlayerHandRigReview()
        {
            if(handObservation!=null||sharedObservation!=null) throw new InvalidOperationException("Wait for the active read-only capture.");
            if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();
            else CleanupReviewSession();
        }
        internal static void CapturePlayerHandRigDiagnostic()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isPaused)throw new InvalidOperationException("Unpaused natural playback required.");
            if(handObservation!=null)throw new InvalidOperationException("A hand observation is already active.");
            if(FindOwnedOrphanReviews().Length!=0||FindOwnedOrphanReviewCameras().Length!=0)
                throw new InvalidOperationException("An earlier hand review still has orphaned temporary objects. Stop and clean the review before observing.");
            string folder=NextHandDiagnosticFolder();
            handObservation=new HandObservation(folder);
            PlayerHandRigReviewObserver.PoseRendered+=ObserveHand;
        }
        private static string NextHandDiagnosticFolder()
        {
            // User approved repeated intermediate diagnostics through completion; retain every earlier set.
            for(int i=2;;i++)
            {
                string folder=Evidence+"/diagnostic_"+i.ToString("00");
                if(!Directory.Exists(folder))return folder;
            }
        }
        internal static void CapturePlayerHandRigCandidateRest()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Static imported-pose inspection requires Edit Mode.");
            string folder=NextHandDiagnosticFolder();
            Directory.CreateDirectory(folder);
            foreach(string path in new[]{ModelPath,CandidatePath,RigFolder+"/Review_Articulation.prefab"})
            {
                var preview=new PreviewRenderUtility();
                try
                {
                    var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    preview.AddSingleGO(instance);
                    for(int view=1;view<=8;view++)
                    {
                        ConfigureHandCamera(preview.camera,instance,view);
                        // This comparison must include the entire hand, not a thin slice of it.
                        preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=6f;
                        preview.lights[0].intensity=1.2f;preview.lights[0].transform.rotation=Quaternion.Euler(40,40,0);
                        preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(340,218,177);
                        preview.ambientColor=new Color(.5f,.5f,.5f,1);
                        preview.BeginStaticPreview(new Rect(0,0,600,600));preview.Render(true);
                        var texture=preview.EndStaticPreview();
                        File.WriteAllBytes(folder+"/"+(path==ModelPath?"SharedSourceRest":path==CandidatePath?"ImportedRest":"ReviewPrefabRest")+"_view_"+view+".png",texture.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
                finally{preview.Cleanup();}
            }
            File.WriteAllText(folder+"/observation.txt","Static imported/default pose comparison only. No bone edits, animation sampling, rebind, clock or visibility changes. Not an animation pass.\ncapture=COMPLETE\n");
        }
        private static void ObserveHand(PlayerHandRigReviewObserver observer,float time)
        {
            if(handObservation==null)return;
            try { if(!handObservation.Observe(observer,time))return; handObservation.Finish(); }
            catch(Exception e){handObservation.Abort(e);Debug.LogException(e);}
            PlayerHandRigReviewObserver.PoseRendered-=ObserveHand;
            handObservation.Dispose();handObservation=null;
        }
        private sealed class HandObservation:IDisposable
        {
            private readonly string folder;
            private readonly Texture2D[,] sheets=new Texture2D[2,9];
            private readonly Camera[,] cameras=new Camera[2,9];
            private readonly int[] phase={-1,-1};
            private readonly float[] previous={-1,-1};
            private readonly StringBuilder report=new StringBuilder();
            private readonly double started=EditorApplication.timeSinceStartup;
            private int active;
            public HandObservation(string path)
            {
                folder=path;Directory.CreateDirectory(folder);
                report.AppendLine("Natural Animator playback; observer writes no target pose, clock, skin, visibility or Animator state.");
                report.AppendLine("Camera path=continuous normal frame rendering to texture; no manual Camera.Render calls.");
                report.AppendLine("Comparison environment meshDeformation="+PlayerSettings.meshDeformation+" skinWeights="+QualitySettings.skinWeights+"; not a production approval.");
                report.AppendLine("Grid=4x3; phases=0,0.5,1,1.5,2,2.5,3,3.5,4,4.5,5,5.5; views=body and each hand dorsal,palm,front,oblique.");
                for(int i=0;i<2;i++)for(int v=0;v<9;v++)
                {
                    sheets[i,v]=new Texture2D(2400,1800,TextureFormat.RGBA32,false);
                    sheets[i,v].SetPixels32(Enumerable.Repeat(new Color32(10,12,16,255),2400*1800).ToArray());
                }
            }
            public bool Observe(PlayerHandRigReviewObserver observer,float time)
            {
                if(EditorApplication.timeSinceStartup-started>240)throw new InvalidOperationException("Natural hand observation timeout; incomplete coverage.");
                int i=observer.ReviewKind=="Articulation"?0:1;
                if(cameras[i,0]==null)
                {
                    for(int view=0;view<9;view++)cameras[i,view]=CreateContinuousHandCamera(observer.gameObject,view);
                    return false;
                }
                bool wrapped=previous[i]>5f&&time<.5f;previous[i]=time;
                if(i!=active)return false;
                if(phase[i]<0&&wrapped)phase[i]=0;
                if(phase[i]<0||phase[i]>=12)return false;
                float due=phase[i]*.5f;
                if(time<due||time>due+.18f)return false;
                report.AppendLine(observer.ReviewKind+" phase="+due.ToString("F1")+" actual="+time.ToString("F5")+" frame="+Time.frameCount);
                for(int view=0;view<9;view++)
                {
                    Color32[] pixels=ReadHandCamera(cameras[i,view]);
                    sheets[i,view].SetPixels32((phase[i]%4)*600,(2-phase[i]/4)*600,600,600,pixels);
                }
                phase[i]++;if(phase[i]>=12)active++;
                return phase.All(p=>p>=12);
            }
            public void Finish()
            {
                for(int i=0;i<2;i++)for(int v=0;v<9;v++)
                {sheets[i,v].Apply();File.WriteAllBytes(folder+"/"+(i==0?"Articulation":"BatteryGrip")+"_view_"+v+".png",sheets[i,v].EncodeToPNG());}
                report.AppendLine("capture=COMPLETE; visualApproval=REQUIRED; no numerical pass substitutes for direct review.");
                File.WriteAllText(folder+"/observation.txt",report.ToString());
            }
            public void Abort(Exception e){File.WriteAllText(folder+"/observation.txt",report+"\nABORT="+e);}
            public void Dispose()
            {
                foreach(var texture in sheets)if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
                foreach(var camera in cameras)if(camera!=null)
                {var target=camera.targetTexture;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
            }
        }
        private static Camera CreateContinuousHandCamera(GameObject instance,int view)
        {
            GameObject cameraObject=new GameObject("ReadOnlyHandReviewCamera"){hideFlags=HideFlags.HideAndDontSave};
            cameraObject.transform.SetParent(instance.transform.parent,true);
            Camera camera=cameraObject.AddComponent<Camera>();
            ConfigureHandCamera(camera,instance,view);
            camera.targetTexture=new RenderTexture(600,600,24,RenderTextureFormat.ARGB32){name="PlayerHandObservation_"+instance.name+"_"+view,hideFlags=HideFlags.DontSave};
            return camera;
        }
        private static void ConfigureHandCamera(Camera camera,GameObject instance,int view)
        {
            Transform hand=view==0?null:instance.GetComponentsInChildren<Transform>().Single(t=>t.name==(view<=4?"LeftHand":"RightHand"));
            Vector3 center=hand==null?instance.transform.position+Vector3.up*.9f:hand.position+hand.TransformVector(new Vector3(0,.125f,0));
            Vector3 direction=Vector3.forward;
            if(hand!=null)
            {float sign=view<=4?-1f:1f;direction=new[]{Vector3.right*sign,Vector3.left*sign,Vector3.forward,(Vector3.right*sign+Vector3.forward).normalized}[(view-1)%4];}
            camera.orthographic=true;camera.orthographicSize=hand==null?.95f:.17f;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.045f,.06f,1f);
            camera.transform.position=center+direction*3f;camera.transform.LookAt(center,Vector3.up);
            camera.nearClipPlane=hand==null?2f:2.78f;camera.farClipPlane=hand==null?4f:3.22f;
        }
        private static Color32[] ReadHandCamera(Camera camera)
        {
            RenderTexture previous=RenderTexture.active;
            Texture2D image=new Texture2D(600,600,TextureFormat.RGBA32,false);
            try {RenderTexture.active=camera.targetTexture;image.ReadPixels(new Rect(0,0,600,600),0,0);image.Apply();return image.GetPixels32();}
            finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
        }

        internal static void CaptureSharedPlayerHandRigDiagnostic()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isPaused||handObservation!=null||sharedObservation!=null)
                throw new InvalidOperationException("Unpaused natural review with no active capture is required.");
            sharedObservation=new SharedRigObservation();
            PlayerHandRigReviewObserver.PoseRendered+=ObserveSharedRig;
            EditorApplication.update+=FollowSharedRigCameras;
        }
        private static void FollowSharedRigCameras()=>sharedObservation?.FollowCameras();
        private static void ObserveSharedRig(PlayerHandRigReviewObserver observer,float time)
        {
            if(sharedObservation==null||observer.ReviewKind!="Articulation")return;
            try {if(!sharedObservation.Observe(time))return;sharedObservation.Finish();}
            catch(Exception e){sharedObservation.Abort(e);Debug.LogException(e);}
            PlayerHandRigReviewObserver.PoseRendered-=ObserveSharedRig;
            EditorApplication.update-=FollowSharedRigCameras;
            sharedObservation.Dispose();sharedObservation=null;
        }
        private sealed class SharedRigObservation:IDisposable
        {
            private readonly string folder;
            private readonly GameObject[] models;
            private readonly Camera[,] cameras=new Camera[6,3];
            private readonly Texture2D[,] sheets=new Texture2D[6,3];
            private readonly int[] views={0,2,6};
            private readonly StringBuilder report=new StringBuilder("Existing scene models; natural playback, no target/Animator/clock/visibility edits.\nGrid=4x3 at half-second intervals of the independent six-second observer; each actual Animator phase is recorded.\n");
            private readonly double started=EditorApplication.timeSinceStartup;
            private int phase=-1;private float previous=-1;
            public SharedRigObservation()
            {
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/_Project/Scenes/CargoRunMvp.unity");
                Transform layout=scene.GetRootGameObjects().Single(o=>o.name=="PlayerAnimationLayout").transform;
                string[] names={"Player_Idle","Player_Run_Forward","Player_Walk_Forward","Shotgun_Reload","Hands_Throw_Ready","Stick_Throw_Ready"};
                models=names.Select(n=>layout.Find(n)?.gameObject??throw new InvalidOperationException("Missing existing model: "+n)).ToArray();
                string stage=models[0].GetComponentInChildren<SkinnedMeshRenderer>().bones.Length==24?"before":"after";
                folder=Evidence+"/shared_"+stage+"_"+DateTime.Now.ToString("HHmmss");Directory.CreateDirectory(folder);
                try
                {
                    for(int i=0;i<models.Length;i++)for(int v=0;v<3;v++)
                    {
                        cameras[i,v]=CreateContinuousHandCamera(models[i],views[v]);
                        sheets[i,v]=new Texture2D(2400,1800,TextureFormat.RGBA32,false);
                    }
                    FollowCameras();
                }
                catch {Dispose();throw;}
            }
            public void FollowCameras()
            {
                for(int i=0;i<models.Length;i++)for(int v=0;v<3;v++)if(cameras[i,v]!=null)
                {
                    ConfigureHandCamera(cameras[i,v],models[i],views[v]);
                    // A wider viewing volume follows existing live gestures; no renderer is hidden.
                    if(v>0){cameras[i,v].orthographicSize=.23f;cameras[i,v].nearClipPlane=2.55f;cameras[i,v].farClipPlane=3.45f;}
                }
            }
            public bool Observe(float time)
            {
                if(EditorApplication.timeSinceStartup-started>180)throw new InvalidOperationException("Existing model observation timed out.");
                bool wrapped=previous>5&&time<.5f;previous=time;
                if(phase<0&&wrapped)phase=0;
                if(phase<0||time<phase*.5f||time>phase*.5f+.18f)return false;
                for(int i=0;i<models.Length;i++)
                {
                    Animator animator=models[i].GetComponent<Animator>();
                    var skin=models[i].GetComponentInChildren<SkinnedMeshRenderer>();
                    report.AppendLine(models[i].name+" sample="+phase+" observerTime="+time+" animatorTime="+
                        (animator!=null&&animator.isActiveAndEnabled?animator.GetCurrentAnimatorStateInfo(0).normalizedTime.ToString("F6"):"inactive")+
                        " bones="+skin.bones.Length+" shapes="+skin.sharedMesh.blendShapeCount);
                    for(int v=0;v<3;v++)sheets[i,v].SetPixels32((phase%4)*600,(2-phase/4)*600,600,600,ReadHandCamera(cameras[i,v]));
                }
                return ++phase>=12;
            }
            public void Finish()
            {
                for(int i=0;i<models.Length;i++)for(int v=0;v<3;v++)
                {sheets[i,v].Apply();File.WriteAllBytes(folder+"/"+models[i].name+"_view_"+views[v]+".png",sheets[i,v].EncodeToPNG());}
                File.WriteAllText(folder+"/observation.txt",report+"capture=COMPLETE; direct review required.\n");
            }
            public void Abort(Exception e)=>File.WriteAllText(folder+"/observation.txt",report+"ABORT="+e);
            public void Dispose()
            {
                foreach(var sheet in sheets)if(sheet!=null)UnityEngine.Object.DestroyImmediate(sheet);
                foreach(var camera in cameras)if(camera!=null)
                {var target=camera.targetTexture;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);}
            }
        }

        private struct RigVertexMap
        {
            public int A,B,C;
            public Vector3 Weights;
            public Vector3 Read(Vector3[] values)=>values[A]*Weights.x+values[B]*Weights.y+values[C]*Weights.z;
            public float Read(BoneWeight[] values,int bone)=>WeightFor(values[A],bone)*Weights.x+
                WeightFor(values[B],bone)*Weights.y+WeightFor(values[C],bone)*Weights.z;
        }

        // 54 -> 54 reconstruction is deliberately separate from the original
        // 24 -> 54 rig extension, so existing finger bones are never recreated.
        private static Mesh TransferRightFingerCorrective(Mesh source,Mesh candidate,Mesh variant,
            Transform[] sourceBones,string[] variantNames,int[] map)
        {
            string[] names=sourceBones.Select(b=>b.name).ToArray();
            if(variant.vertexCount!=source.vertexCount||!variant.triangles.SequenceEqual(source.triangles)||
                !variant.uv.SequenceEqual(source.uv)||!variantNames.Take(54).SequenceEqual(names))
                throw new InvalidOperationException("Corrective source correspondence changed: "+variant.name);
            bool[] oldLocal=RightFingerRepairRegions(source,sourceBones),newLocal=RightFingerRepairRegions(candidate,sourceBones);
            var sv=source.vertices;var vv=variant.vertices;var sw=source.boneWeights;var vw=variant.boneWeights;
            var sn=source.normals;var vn=variant.normals;var st=source.tangents;var vt=variant.tangents;
            // Existing effects are confined outside the rebuilt area. Assert that
            // assumption explicitly; never silently discard a right-hand effect.
            for(int v=0;v<sv.Length;v++)if(oldLocal[v])
            {
                if((vv[v]-sv[v]).sqrMagnitude>1e-12f||(vn[v]-sn[v]).sqrMagnitude>1e-12f||(vt[v]-st[v]).sqrMagnitude>1e-12f)
                    throw new InvalidOperationException("Corrective changes the rebuilt right-hand region; surface transfer required: "+variant.name+" / "+v);
                for(int bone=0;bone<variantNames.Length;bone++)
                    if(Mathf.Abs(WeightFor(vw[v],bone)-(bone<54?WeightFor(sw[v],bone):0))>1e-6f)
                        throw new InvalidOperationException("Corrective has a right-hand weight override: "+variant.name+" / "+v);
            }
            var result=CopySkinData(candidate);result.name=variant.name;
            try
            {
                var vertices=result.vertices;var normals=result.normals;var tangents=result.tangents;var weights=result.boneWeights;
                for(int v=0;v<vertices.Length;v++)if(!newLocal[v])
                {
                    int old=map[v];if(old<0)throw new InvalidOperationException("Protected corrective mapping missing.");
                    vertices[v]+=vv[old]-sv[old];normals[v]=vn[old];tangents[v]=vt[old];weights[v]=vw[old];
                }
                result.vertices=vertices;result.normals=normals;result.tangents=tangents;result.boneWeights=weights;
                result.bindposes=variant.bindposes;result.ClearBlendShapes();
                for(int shape=0;shape<variant.blendShapeCount;shape++)for(int frame=0;frame<variant.GetBlendShapeFrameCount(shape);frame++)
                {
                    var dp=new Vector3[sv.Length];var dn=new Vector3[sv.Length];var dt=new Vector3[sv.Length];
                    variant.GetBlendShapeFrameVertices(shape,frame,dp,dn,dt);
                    for(int v=0;v<sv.Length;v++)if(oldLocal[v]&&(dp[v].sqrMagnitude>1e-12f||dn[v].sqrMagnitude>1e-12f||dt[v].sqrMagnitude>1e-12f))
                        throw new InvalidOperationException("BlendShape changes the rebuilt right-hand region: "+variant.GetBlendShapeName(shape));
                    Vector3[] Transfer(Vector3[] values)=>Enumerable.Range(0,vertices.Length).Select(v=>newLocal[v]?Vector3.zero:values[map[v]]).ToArray();
                    result.AddBlendShapeFrame(variant.GetBlendShapeName(shape),variant.GetBlendShapeFrameWeight(shape,frame),Transfer(dp),Transfer(dn),Transfer(dt));
                }
                result.RecalculateBounds();return result;
            }
            catch {UnityEngine.Object.DestroyImmediate(result);throw;}
        }

        internal static void ApplySharedRightFingerRepair()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply right-finger reconstruction only in Edit Mode.");
            const string scenePath="Assets/_Project/Scenes/CargoRunMvp.unity";
            const string weightPath=RigFolder+"/player_hands_skin_weights.json";
            string[] variantPaths={"Assets/_Project/Art/Player/ShotgunReloadWristDeform/Shotgun_Reload_LeftWristDeform.asset",
                "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_Breathing.asset"};
            string evidenceRoot=Path.GetDirectoryName(Evidence);
            string latest=Directory.GetDirectories(evidenceRoot,"diagnostic_*").OrderByDescending(Directory.GetCreationTimeUtc).First();
            string observation=Path.Combine(latest,"observation.txt");
            if(!File.Exists(observation)||!File.ReadAllText(observation).TrimEnd().EndsWith("result=PASS",StringComparison.Ordinal)||
                File.GetLastWriteTimeUtc(CandidatePath)>File.GetLastWriteTimeUtc(observation))
                throw new InvalidOperationException("The current candidate requires a passing natural observation and direct review before promotion.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            if(!scene.IsValid()||!scene.isLoaded||scene.isDirty)throw new InvalidOperationException("The approved CargoRunMvp scene must be open and unchanged.");
            if(!File.ReadAllBytes(ModelPath).SequenceEqual(File.ReadAllBytes("Backups/ConsumableItemGrips_2026-09-09/finger_weights_before/player.fbx")))
                throw new InvalidOperationException("The shared source changed after this repair was authored.");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentInChildren<SkinnedMeshRenderer>();
            var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath).GetComponentInChildren<SkinnedMeshRenderer>();
            string[] names=source.bones.Select(b=>b.name).ToArray();
            if(names.Length!=54||!candidate.bones.Select(b=>b.name).SequenceEqual(names))throw new InvalidOperationException("Expected identical existing 54-bone skeletons.");
            for(int bone=0;bone<54;bone++)for(int r=0;r<4;r++)for(int c=0;c<4;c++)
                if(Mathf.Abs(source.sharedMesh.bindposes[bone][r,c]-candidate.sharedMesh.bindposes[bone][r,c])>.00002f)
                    throw new InvalidOperationException("The candidate bind poses changed.");
            int[] map=MapRightFingerReconstruction(source.sharedMesh,candidate.sharedMesh,source.bones);
            GameObject layout=scene.GetRootGameObjects().Single(o=>o.name=="PlayerAnimationLayout");
            var skins=layout.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>
                AssetDatabase.GetAssetPath(r.sharedMesh)==ModelPath||variantPaths.Contains(AssetDatabase.GetAssetPath(r.sharedMesh))).ToArray();
            if(skins.Length!=144||skins.Count(r=>AssetDatabase.GetAssetPath(r.sharedMesh)==ModelPath)!=141)
                throw new InvalidOperationException("Expected 141 shared and three corrective transporter skins.");
            var skinPaths=skins.ToDictionary(r=>r,r=>AssetDatabase.GetAssetPath(r.sharedMesh));
            var bones=skins.ToDictionary(r=>r,r=>r.bones);var roots=skins.ToDictionary(r=>r,r=>r.rootBone);
            var materials=skins.ToDictionary(r=>r,r=>r.sharedMaterials);
            var animators=skins.Select(r=>r.GetComponentInParent<Animator>()).Where(a=>a!=null).Distinct().ToArray();
            var avatars=animators.ToDictionary(a=>a,a=>a.avatar);var controllers=animators.ToDictionary(a=>a,a=>a.runtimeAnimatorController);
            var shapes=skins.ToDictionary(r=>r,r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount)
                .ToDictionary(i=>r.sharedMesh.GetBlendShapeName(i),r.GetBlendShapeWeight));
            var variants=new Dictionary<string,Mesh>();var backups=new Dictionary<string,string>();
            string backup="Backups/ConsumableItemGrips_2026-09-09/right_finger_apply_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
            int undoGroup=-1;bool promoted=false;
            try
            {
                foreach(string path in variantPaths)
                {
                    var skin=skins.First(r=>skinPaths[r]==path);string[] variantNames=skin.bones.Select(b=>b.name).ToArray();
                    if(skins.Where(r=>skinPaths[r]==path).Any(r=>!r.bones.Select(b=>b.name).SequenceEqual(variantNames)))
                        throw new InvalidOperationException("Corrective bone bindings are inconsistent.");
                    variants[path]=TransferRightFingerCorrective(source.sharedMesh,candidate.sharedMesh,skin.sharedMesh,source.bones,variantNames,map);
                }
                Directory.CreateDirectory(backup);
                foreach(string path in new[]{ModelPath,ModelPath+".meta",weightPath,scenePath}.Concat(variantPaths)
                    .Concat(variantPaths.Select(p=>p+".meta")).Concat(ConsumableRiggedSharedMotionTools.RightFingerGripAssetPaths()))
                {
                    string saved=Path.Combine(backup,Path.GetFileName(path));File.Copy(path,saved,false);backups.Add(path,saved);
                }
                Undo.IncrementCurrentGroup();undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Apply shared right-finger repair");
                foreach(var skin in skins)Undo.RegisterCompleteObjectUndo(skin,"Shared right-finger skin");
                // FBX and authored weights are promoted together before import.
                promoted=true;File.Copy(RigFolder+"/player_hands_candidate_weights.json",weightPath,true);File.Copy(CandidatePath,ModelPath,true);
                AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                var imported=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentInChildren<SkinnedMeshRenderer>();
                if(!imported.bones.Select(b=>b.name).SequenceEqual(names)||imported.sharedMesh.vertexCount!=candidate.sharedMesh.vertexCount)
                    throw new InvalidOperationException("The promoted model differs from the reviewed candidate.");
                foreach(var entry in variants)
                {
                    string guid=AssetDatabase.AssetPathToGUID(entry.Key);AssetDatabase.CreateAsset(entry.Value,entry.Key);AssetDatabase.SaveAssetIfDirty(entry.Value);
                    if(AssetDatabase.AssetPathToGUID(entry.Key)!=guid)throw new InvalidOperationException("A corrective GUID changed.");
                }
                foreach(var skin in skins)
                {
                    string path=skinPaths[skin];skin.sharedMesh=null;skin.bones=Array.Empty<Transform>();
                    skin.sharedMesh=path==ModelPath?imported.sharedMesh:AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    skin.bones=bones[skin];skin.rootBone=roots[skin];
                    foreach(var shape in shapes[skin])skin.SetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(shape.Key),shape.Value);
                    if(!skin.bones.SequenceEqual(bones[skin])||!skin.sharedMaterials.SequenceEqual(materials[skin]))
                        throw new InvalidOperationException("An existing skeleton/material binding changed.");
                    EditorUtility.SetDirty(skin);
                }
                ConsumableRiggedSharedMotionTools.ApplyReviewedRightFingerGrips();
                if(animators.Any(a=>a.avatar!=avatars[a]||a.runtimeAnimatorController!=controllers[a]))
                    throw new InvalidOperationException("An existing avatar/controller changed.");
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the scoped right-finger repair.");
                Undo.CollapseUndoOperations(undoGroup);
                File.WriteAllText(Evidence+"/right_finger_application.txt","renderers=144\nexistingBonesAvatarsControllersMaterialsPreserved=True\ncorrectives=2\nbackup="+backup+"\nactualNaturalReviewRequired=True\n");
            }
            catch
            {
                if(undoGroup>=0)Undo.RevertAllDownToGroup(undoGroup);
                if(promoted)
                {
                    foreach(var entry in backups)File.Copy(entry.Value,entry.Key,true);
                    foreach(string path in new[]{ModelPath}.Concat(variantPaths).Concat(ConsumableRiggedSharedMotionTools.RightFingerGripAssetPaths()))
                        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                    foreach(var skin in skins)if(skin!=null)
                    {skin.sharedMesh=AssetDatabase.LoadAllAssetsAtPath(skinPaths[skin]).OfType<Mesh>().First();skin.bones=bones[skin];skin.rootBone=roots[skin];}
                }
                throw;
            }
            finally {foreach(var mesh in variants.Values)if(mesh!=null&&!EditorUtility.IsPersistent(mesh))UnityEngine.Object.DestroyImmediate(mesh);}
        }

        internal static void ApplySharedPlayerHandRig()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply the shared hand rig in Edit Mode.");
            const string scenePath="Assets/_Project/Scenes/CargoRunMvp.unity";
            string[] variantPaths={"Assets/_Project/Art/Player/ShotgunReloadWristDeform/Shotgun_Reload_LeftWristDeform.asset",
                "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready_Breathing.asset"};
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
            if(!scene.IsValid()||!scene.isLoaded)throw new InvalidOperationException("The approved CargoRunMvp scene must already be open.");
            GameObject layout=scene.GetRootGameObjects().Single(o=>o.name=="PlayerAnimationLayout");
            SkinnedMeshRenderer source=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentInChildren<SkinnedMeshRenderer>();
            SkinnedMeshRenderer candidate=AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath).GetComponentInChildren<SkinnedMeshRenderer>();
            if(source.bones.Length!=24||candidate.bones.Length!=54)throw new InvalidOperationException("Expected the original 24-bone source and checked 54-bone candidate; no repeated promotion is implicit.");
            if(!File.ReadAllBytes(ModelPath).SequenceEqual(File.ReadAllBytes("Backups/PlayerHandsRig_2026-09-09/player.fbx")))
                throw new InvalidOperationException("The user's original FBX changed after this candidate was authored.");
            string[] sourceNames=source.bones.Select(b=>b.name).ToArray(),candidateNames=candidate.bones.Select(b=>b.name).ToArray();
            var renderers=layout.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>
                AssetDatabase.GetAssetPath(r.sharedMesh)==ModelPath||variantPaths.Contains(AssetDatabase.GetAssetPath(r.sharedMesh))).ToArray();
            if(renderers.Length!=144||renderers.Count(r=>AssetDatabase.GetAssetPath(r.sharedMesh)==ModelPath)!=141)
                throw new InvalidOperationException("Shared-rig usage differs from the approved 141 original + 3 corrective renderers.");
            var originalPaths=renderers.ToDictionary(r=>r,r=>AssetDatabase.GetAssetPath(r.sharedMesh));
            var animatorBySkin=renderers.ToDictionary(r=>r,r=>r.GetComponentInParent<Animator>());
            Transform ModelRoot(SkinnedMeshRenderer renderer)
            {
                Transform root=renderer.transform;
                while(root.parent!=layout.transform)
                {root=root.parent;if(root==null)throw new InvalidOperationException("Transporter skin is outside the approved layout.");}
                return root;
            }
            var modelBySkin=renderers.ToDictionary(r=>r,ModelRoot);
            var shapeWeightsBySkin=renderers.ToDictionary(r=>r,r=>Enumerable.Range(0,r.sharedMesh.blendShapeCount)
                .ToDictionary(i=>r.sharedMesh.GetBlendShapeName(i),r.GetBlendShapeWeight));
            var materialsBySkin=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);
            var controllers=animatorBySkin.Values.Where(a=>a!=null).Distinct().ToDictionary(a=>a,a=>a.runtimeAnimatorController);
            var transforms=renderers.ToDictionary(r=>r,r=>modelBySkin[r].GetComponentsInChildren<Transform>(true)
                .Where(t=>candidateNames.Contains(t.name)||r.bones.Contains(t)).ToDictionary(t=>t.name,t=>t));
            foreach(var r in renderers)
            {
                if(!r.transform.IsChildOf(layout.transform))throw new InvalidOperationException("Unexpected transporter hierarchy.");
                foreach(Transform bone in candidate.bones)
                    if(!IsFingerBone(bone.name,"Left")&&!IsFingerBone(bone.name,"Right")&&!transforms[r].ContainsKey(bone.name))
                        throw new InvalidOperationException("An existing transporter bone is missing: "+bone.name);
                    else if((IsFingerBone(bone.name,"Left")||IsFingerBone(bone.name,"Right"))&&transforms[r].ContainsKey(bone.name))
                        throw new InvalidOperationException("An existing user finger chain must not be overwritten: "+bone.name);
            }
            Matrix4x4 candidateToOriginal=source.sharedMesh.bindposes[Array.IndexOf(sourceNames,"Hips")].inverse*
                candidate.sharedMesh.bindposes[Array.IndexOf(candidateNames,"Hips")];
            RigVertexMap[] mapping=MapOriginalSkinToCandidate(source.sharedMesh,candidate.sharedMesh,candidateToOriginal,sourceNames,candidateNames);
            var variants=new Dictionary<string,Mesh>();var savedVariants=new Dictionary<string,Mesh>();var variantBoneNames=new Dictionary<string,string[]>();
            var report=new StringBuilder("Shared transporter hand rig application; only PlayerAnimationLayout transporter skins/bones/avatars.\n");
            string backup="Backups/PlayerHandsRig_2026-09-09/global_apply_"+DateTime.Now.ToString("HHmmss");
            int undoGroup=-1;bool promoted=false,sceneSaved=false;
            try
            {
                foreach(string path in variantPaths)
                {
                    SkinnedMeshRenderer renderer=renderers.First(r=>originalPaths[r]==path);
                    string[] names=renderer.bones.Select(b=>b.name).ToArray();variantBoneNames[path]=names;
                    foreach(var other in renderers.Where(r=>originalPaths[r]==path))
                        if(!other.bones.Select(b=>b.name).SequenceEqual(names))throw new InvalidOperationException("Corrective skin bone arrays differ.");
                    savedVariants[path]=UnityEngine.Object.Instantiate(renderer.sharedMesh);
                    variants[path]=TransferHandRigVariant(source.sharedMesh,candidate.sharedMesh,renderer.sharedMesh,sourceNames,candidateNames,names,mapping,candidateToOriginal);
                    report.AppendLine("corrective="+path+" oldBones="+names.Length+" newBones="+variants[path].bindposes.Length+
                        " preservedBlendShapes="+renderer.sharedMesh.blendShapeCount);
                }
                Directory.CreateDirectory(backup);
                foreach(string path in new[]{ModelPath,ModelPath+".meta",scenePath}.Concat(variantPaths))
                    File.Copy(path,backup+"/"+Path.GetFileName(path),false);
                Undo.IncrementCurrentGroup();undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Apply shared transporter hand rig");
                foreach(var r in renderers){Undo.RegisterCompleteObjectUndo(r,"Shared hand skin");if(animatorBySkin[r]!=null)Undo.RegisterCompleteObjectUndo(animatorBySkin[r],"Shared hand avatar");}
                File.Copy(CandidatePath,ModelPath,true);promoted=true;
                var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.isReadable=true;
                importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=4;importer.minBoneWeight=0.000001f;
                importer.SaveAndReimport();
                GameObject model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                SkinnedMeshRenderer imported=model.GetComponentInChildren<SkinnedMeshRenderer>();
                Avatar avatar=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().Single();
                if(!imported.bones.Select(b=>b.name).SequenceEqual(candidateNames)||imported.sharedMesh.vertexCount!=candidate.sharedMesh.vertexCount||!avatar.isValid)
                    throw new InvalidOperationException("Promoted FBX does not match the checked candidate skeleton.");
                foreach(var entry in variants)
                {
                    string guid=AssetDatabase.AssetPathToGUID(entry.Key);
                    AssetDatabase.CreateAsset(entry.Value,entry.Key);AssetDatabase.SaveAssetIfDirty(entry.Value);
                    if(AssetDatabase.AssetPathToGUID(entry.Key)!=guid)throw new InvalidOperationException("Corrective asset GUID changed.");
                }
                foreach(var group in renderers.GroupBy(r=>modelBySkin[r]))
                {
                    // A connected model prefab may already receive these nodes on FBX import.
                    // Re-read it, and create each chain once per model, not once per skin.
                    var byName=group.Key.GetComponentsInChildren<Transform>(true)
                        .Where(t=>candidateNames.Contains(t.name)||group.Any(r=>r.bones.Contains(t)))
                        .ToDictionary(t=>t.name,t=>t);
                    foreach(Transform bone in candidate.bones.Where(b=>IsFingerBone(b.name,"Left")||IsFingerBone(b.name,"Right")))
                    {
                        if(byName.TryGetValue(bone.name,out Transform existing))
                        {
                            if(existing.parent!=byName[bone.parent.name]||Vector3.Distance(existing.localPosition,bone.localPosition)>.00002f||
                                Quaternion.Angle(existing.localRotation,bone.localRotation)>.05f||Vector3.Distance(existing.localScale,bone.localScale)>.00002f)
                                throw new InvalidOperationException("Auto-imported finger node differs from the checked candidate: "+bone.name);
                            continue;
                        }
                        var added=new GameObject(bone.name);Undo.RegisterCreatedObjectUndo(added,"Shared finger bone");
                        added.layer=byName[bone.parent.name].gameObject.layer;added.transform.SetParent(byName[bone.parent.name],false);
                        added.transform.localPosition=bone.localPosition;added.transform.localRotation=bone.localRotation;added.transform.localScale=bone.localScale;
                        byName.Add(bone.name,added.transform);
                    }
                    foreach(var r in group)transforms[r]=byName;
                }
                foreach(var r in renderers)
                {
                    var byName=transforms[r];
                    string path=originalPaths[r];
                    r.sharedMesh=path==ModelPath?imported.sharedMesh:AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    string[] names=path==ModelPath?candidateNames:candidateNames.Concat(variantBoneNames[path].Where(n=>!candidateNames.Contains(n))).ToArray();
                    r.bones=names.Select(n=>byName[n]).ToArray();
                    if(animatorBySkin[r]!=null)animatorBySkin[r].avatar=avatar;
                    foreach(var shape in shapeWeightsBySkin[r])
                    {
                        int index=r.sharedMesh.GetBlendShapeIndex(shape.Key);
                        if(index<0)throw new InvalidOperationException("A saved BlendShape was lost: "+shape.Key);
                        r.SetBlendShapeWeight(index,shape.Value);
                    }
                    if(!r.sharedMaterials.SequenceEqual(materialsBySkin[r])||
                        (animatorBySkin[r]!=null&&animatorBySkin[r].runtimeAnimatorController!=controllers[animatorBySkin[r]]))
                        throw new InvalidOperationException("Unrelated material/controller binding changed during promotion.");
                    EditorUtility.SetDirty(r);if(animatorBySkin[r]!=null)EditorUtility.SetDirty(animatorBySkin[r]);
                    report.AppendLine("applied="+RelativePath(r.transform,null)+" bones="+r.bones.Length+" controller="+
                        (animatorBySkin[r]==null?"NONE":AssetDatabase.GetAssetPath(animatorBySkin[r].runtimeAnimatorController)));
                }
                SaveCheckedGripPose();
                EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the scoped shared rig application.");
                sceneSaved=true;Undo.CollapseUndoOperations(undoGroup);
                report.AppendLine("renderers="+renderers.Length+" backup="+backup+" runtimeVisualReview=REQUIRED");
                File.WriteAllText(Evidence+"/shared_rig_application.txt",report.ToString());
            }
            catch
            {
                if(promoted)
                {
                    File.Copy(backup+"/player.fbx",ModelPath,true);File.Copy(backup+"/player.fbx.meta",ModelPath+".meta",true);
                    AssetDatabase.ImportAsset(ModelPath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                    foreach(var entry in savedVariants)
                    {AssetDatabase.CreateAsset(entry.Value,entry.Key);AssetDatabase.SaveAssetIfDirty(entry.Value);}
                }
                if(undoGroup>=0)Undo.RevertAllDownToGroup(undoGroup);
                if(promoted)foreach(var renderer in renderers)
                    renderer.sharedMesh=AssetDatabase.LoadAllAssetsAtPath(originalPaths[renderer]).OfType<Mesh>().First();
                if(sceneSaved)EditorSceneManager.SaveScene(scene);
                throw;
            }
            finally
            {foreach(Mesh mesh in variants.Values.Concat(savedVariants.Values))if(mesh!=null&&!EditorUtility.IsPersistent(mesh))UnityEngine.Object.DestroyImmediate(mesh);}
        }

        private static void SaveCheckedGripPose()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RigFolder+"/Review_BatteryGrip.prefab");
            AnimationClip clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(RigFolder+"/Review_BatteryGrip.anim");
            var pose=ScriptableObject.CreateInstance<PlayerHandGripPose>();
            Transform Find(string name)=>prefab.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
            pose.LeftItemPosition=Find("LeftBatteryGripReview").localPosition;pose.LeftItemRotation=Find("LeftBatteryGripReview").localRotation;
            pose.RightItemPosition=Find("RightBatteryGripReview").localPosition;pose.RightItemRotation=Find("RightBatteryGripReview").localRotation;
            pose.Joints=prefab.GetComponentsInChildren<Transform>(true).Where(t=>IsFingerBone(t.name,"Left")||IsFingerBone(t.name,"Right")).Select(bone=>
            {
                Quaternion q=Quaternion.identity;string path=AnimationUtility.CalculateTransformPath(bone,prefab.transform);
                for(int component=0;component<4;component++)
                {
                    AnimationCurve curve=AnimationUtility.GetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[component]));
                    if(curve==null)throw new InvalidOperationException("Missing authored grip channel: "+bone.name);
                    q[component]=curve.Evaluate(3f);
                }
                return new PlayerHandGripPose.JointPose{BoneName=bone.name,LocalRotation=q.normalized};
            }).ToArray();
            string assetPath=RigFolder+"/SharedBatteryGripPose.asset";var stored=AssetDatabase.LoadAssetAtPath<PlayerHandGripPose>(assetPath);
            if(stored==null){AssetDatabase.CreateAsset(pose,assetPath);stored=pose;}
            else{EditorUtility.CopySerialized(pose,stored);EditorUtility.SetDirty(stored);UnityEngine.Object.DestroyImmediate(pose);}
            AssetDatabase.SaveAssetIfDirty(stored);
        }

        private static RigVertexMap[] MapOriginalSkinToCandidate(Mesh original,Mesh candidate,Matrix4x4 candidateToOriginal,string[] originalNames,string[] candidateNames)
        {
            Vector3[] source=original.vertices,points=candidate.vertices.Select(candidateToOriginal.MultiplyPoint3x4).ToArray();
            Vector2[] sourceUv=original.uv,candidateUv=candidate.uv;int[] triangles=original.triangles;
            var cells=new Dictionary<Vector3Int,List<int>>();
            Vector3Int Cell(Vector3 p)=>Vector3Int.FloorToInt(p/.0001f);
            for(int i=0;i<source.Length;i++)
            {Vector3Int key=Cell(source[i]);if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<int>();list.Add(i);}
            var bounds=new Bounds[triangles.Length/3];
            for(int t=0;t<triangles.Length;t+=3)
            {var box=new Bounds(source[triangles[t]],Vector3.zero);box.Encapsulate(source[triangles[t+1]]);box.Encapsulate(source[triangles[t+2]]);bounds[t/3]=box;}
            var result=new RigVertexMap[points.Length];int exact=0;
            BoneWeight[] sourceWeights=original.boneWeights,candidateWeights=candidate.boneWeights;
            int[] collapse=candidateNames.Select(name=>Array.IndexOf(originalNames,
                IsFingerBone(name,"Left")?"LeftHand":IsFingerBone(name,"Right")?"RightHand":name)).ToArray();
            if(collapse.Any(i=>i<0))throw new InvalidOperationException("Unexpected candidate skin bone outside the approved hand extension.");
            var collapsed=new float[candidate.vertexCount,originalNames.Length];
            for(int v=0;v<candidateWeights.Length;v++)
            {
                BoneWeight w=candidateWeights[v];
                collapsed[v,collapse[w.boneIndex0]]+=w.weight0;collapsed[v,collapse[w.boneIndex1]]+=w.weight1;
                collapsed[v,collapse[w.boneIndex2]]+=w.weight2;collapsed[v,collapse[w.boneIndex3]]+=w.weight3;
            }
            bool MatchingWeight(RigVertexMap map,int vertex)
            {
                for(int bone=0;bone<originalNames.Length;bone++)
                    if(Mathf.Abs(map.Read(sourceWeights,bone)-collapsed[vertex,bone])>.0001f)return false;
                return true;
            }
            for(int v=0;v<points.Length;v++)
            {
                Vector3 point=points[v];Vector3Int cell=Cell(point);int match=-1;float nearest=4e-10f;
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                    if(cells.TryGetValue(cell+new Vector3Int(x,y,z),out var nearby))foreach(int i in nearby)
                    {
                        float distance=(source[i]-point).sqrMagnitude;
                        if(distance>nearest||(sourceUv[i]-candidateUv[v]).sqrMagnitude>4e-8f)continue;
                        if(!MatchingWeight(new RigVertexMap{A=i,B=i,C=i,Weights=Vector3.right},v))continue;
                        nearest=distance;match=i;
                    }
                if(match>=0){result[v]=new RigVertexMap{A=match,B=match,C=match,Weights=Vector3.right};exact++;continue;}
                float best=float.PositiveInfinity;bool found=false;
                for(int t=0;t<triangles.Length;t+=3)
                {
                    if(bounds[t/3].SqrDistance(point)>4e-10f)continue;
                    int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                    Vector3 ab=source[b]-source[a],ac=source[c]-source[a],ap=point-source[a];
                    float d00=Vector3.Dot(ab,ab),d01=Vector3.Dot(ab,ac),d11=Vector3.Dot(ac,ac);
                    float denominator=d00*d11-d01*d01;if(Mathf.Abs(denominator)<1e-18f)continue;
                    float beta=(d11*Vector3.Dot(ap,ab)-d01*Vector3.Dot(ap,ac))/denominator;
                    float gamma=(d00*Vector3.Dot(ap,ac)-d01*Vector3.Dot(ap,ab))/denominator;
                    Vector3 bary=new Vector3(1-beta-gamma,beta,gamma);
                    if(bary.x<-.001f||bary.y<-.001f||bary.z<-.001f)continue;
                    Vector3 reconstructed=source[a]*bary.x+source[b]*bary.y+source[c]*bary.z;
                    Vector2 uv=sourceUv[a]*bary.x+sourceUv[b]*bary.y+sourceUv[c]*bary.z;
                    float distance=(reconstructed-point).sqrMagnitude,uvDistance=(uv-candidateUv[v]).sqrMagnitude;
                    if(distance>4e-10f||uvDistance>4e-8f||distance+uvDistance*1e-4f>=best)continue;
                    if(!MatchingWeight(new RigVertexMap{A=a,B=b,C=c,Weights=bary},v))continue;
                    best=distance+uvDistance*1e-4f;found=true;
                    result[v]=new RigVertexMap{A=a,B=b,C=c,Weights=bary};
                }
                if(!found)
                {
                    var detail=new StringBuilder("Unmatched candidate="+v+" position="+point.ToString("F8")+" uv="+candidateUv[v].ToString("F8")+"\n");
                    using(var counts=candidate.GetBonesPerVertex())using(var weights=candidate.GetAllBoneWeights())
                    {
                        int start=0;for(int i=0;i<v;i++)start+=counts[i];
                        for(int i=0;i<counts[v];i++)detail.AppendLine("nativeWeight="+candidateNames[weights[start+i].boneIndex]+"="+weights[start+i].weight.ToString("F9"));
                    }
                    foreach(int i in Enumerable.Range(0,source.Length).OrderBy(i=>(source[i]-point).sqrMagnitude).Take(6))
                    {
                        detail.AppendLine("source="+i+" positionDistance="+Vector3.Distance(source[i],point).ToString("F8")+" uvDistance="+Vector2.Distance(sourceUv[i],candidateUv[v]).ToString("F8"));
                        for(int b=0;b<originalNames.Length;b++)
                            if(WeightFor(sourceWeights[i],b)>0||collapsed[v,b]>0)
                                detail.AppendLine(originalNames[b]+" sourceWeight="+WeightFor(sourceWeights[i],b).ToString("F8")+" candidateBudget="+collapsed[v,b].ToString("F8"));
                    }
                    var nearFaces=new List<(float distance,string text)>();
                    for(int t=0;t<triangles.Length;t+=3)
                    {
                        int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                        Vector3 ab=source[b]-source[a],ac=source[c]-source[a],ap=point-source[a];
                        float d00=Vector3.Dot(ab,ab),d01=Vector3.Dot(ab,ac),d11=Vector3.Dot(ac,ac),den=d00*d11-d01*d01;
                        if(Mathf.Abs(den)<1e-18f)continue;
                        float beta=(d11*Vector3.Dot(ap,ab)-d01*Vector3.Dot(ap,ac))/den,gamma=(d00*Vector3.Dot(ap,ac)-d01*Vector3.Dot(ap,ab))/den;
                        Vector3 bary=new Vector3(1-beta-gamma,beta,gamma);
                        if(bary.x<-.001f||bary.y<-.001f||bary.z<-.001f)continue;
                        var map=new RigVertexMap{A=a,B=b,C=c,Weights=bary};
                        float distance=Vector3.Distance(map.Read(source),point),uvDistance=Vector2.Distance(sourceUv[a]*bary.x+sourceUv[b]*bary.y+sourceUv[c]*bary.z,candidateUv[v]);
                        string line="face="+a+","+b+","+c+" distance="+distance.ToString("F8")+" uvDistance="+uvDistance.ToString("F8")+" bary="+bary.ToString("F8");
                        for(int bone=0;bone<originalNames.Length;bone++)if(map.Read(sourceWeights,bone)>0||collapsed[v,bone]>0)
                            line+=" "+originalNames[bone]+"="+map.Read(sourceWeights,bone).ToString("F8")+"/"+collapsed[v,bone].ToString("F8");
                        nearFaces.Add((distance,line));
                    }
                    foreach(var face in nearFaces.OrderBy(f=>f.distance).Take(5))detail.AppendLine(face.text);
                    File.WriteAllText(Evidence+"/application_mapping_failure.txt",detail.ToString());
                    throw new InvalidOperationException("Candidate surface/UV correspondence missing at vertex "+v);
                }
            }
            File.WriteAllText(Evidence+"/application_vertex_mapping.txt","original="+source.Length+" candidate="+points.Length+" exact="+exact+" interpolated="+(points.Length-exact)+"\n");
            return result;
        }

        private static Mesh TransferHandRigVariant(Mesh original,Mesh candidate,Mesh variant,string[] originalNames,
            string[] candidateNames,string[] variantNames,RigVertexMap[] mapping,Matrix4x4 candidateToOriginal)
        {
            if(variant.vertexCount!=original.vertexCount||!variant.triangles.SequenceEqual(original.triangles)||!variant.uv.SequenceEqual(original.uv))
                throw new InvalidOperationException("Existing corrective topology or UV differs from its recorded original: "+variant.name);
            if(!variant.normals.SequenceEqual(original.normals)||!variant.tangents.SequenceEqual(original.tangents))
                throw new InvalidOperationException("Existing corrective shading needs a separate lossless transfer: "+variant.name);
            for(int i=0;i<originalNames.Length;i++)
            {
                int variantId=Array.IndexOf(variantNames,originalNames[i]);
                if(variantId<0||variant.bindposes[variantId]!=original.bindposes[i])
                    throw new InvalidOperationException("Existing corrective bind pose differs: "+variant.name+" / "+originalNames[i]);
            }
            Mesh result=CopySkinData(candidate);result.name=variant.name;
            try
            {
            var candidateIds=candidateNames.Select((name,id)=>(name,id)).ToDictionary(p=>p.name,p=>p.id);
            var allNames=candidateNames.ToList();var bindposes=candidate.bindposes.ToList();
            for(int i=0;i<variantNames.Length;i++)if(!candidateIds.ContainsKey(variantNames[i]))
            {candidateIds[variantNames[i]]=allNames.Count;allNames.Add(variantNames[i]);bindposes.Add(variant.bindposes[i]*candidateToOriginal);}
            Vector3[] originalVertices=original.vertices,variantVertices=variant.vertices;
            Vector3[] deltas=Enumerable.Range(0,originalVertices.Length).Select(i=>variantVertices[i]-originalVertices[i]).ToArray();
            Vector3[] newVertices=candidate.vertices;Matrix4x4 originalToCandidate=candidateToOriginal.inverse;
            BoneWeight[] oldWeights=variant.boneWeights,newWeights=candidate.boneWeights;var outputWeights=new BoneWeight[newWeights.Length];
            for(int v=0;v<newWeights.Length;v++)
            {
                newVertices[v]+=originalToCandidate.MultiplyVector(mapping[v].Read(deltas));
                var values=new Dictionary<int,float>();
                for(int i=0;i<variantNames.Length;i++)
                {
                    string name=variantNames[i];float weight=mapping[v].Read(oldWeights,i);if(weight<=.000001f)continue;
                    if(name!="LeftHand"&&name!="RightHand"){values[candidateIds[name]]=weight;continue;}
                    string side=name=="LeftHand"?"Left":"Right";
                    int[] handIds=Enumerable.Range(0,candidateNames.Length).Where(id=>candidateNames[id]==name||IsFingerBone(candidateNames[id],side)).ToArray();
                    float total=handIds.Sum(id=>WeightFor(newWeights[v],id));
                    if(total<=.000001f){values[candidateIds[name]]=weight;continue;}
                    foreach(int id in handIds){float portion=WeightFor(newWeights[v],id)*weight/total;if(portion>.000001f)values[id]=portion;}
                }
                var ordered=values.Where(p=>p.Value>.000001f).OrderByDescending(p=>p.Value).ToArray();
                if(ordered.Length>4)throw new InvalidOperationException("Corrective transfer would discard a skin influence: "+variant.name+" vertex "+v);
                float sum=ordered.Sum(p=>p.Value);if(Mathf.Abs(sum-1)>0.0001f)throw new InvalidOperationException("Corrective weight budget changed.");
                int Index(int i)=>i<ordered.Length?ordered[i].Key:0;float Weight(int i)=>i<ordered.Length?ordered[i].Value/sum:0;
                outputWeights[v]=new BoneWeight{boneIndex0=Index(0),boneIndex1=Index(1),boneIndex2=Index(2),boneIndex3=Index(3),
                    weight0=Weight(0),weight1=Weight(1),weight2=Weight(2),weight3=Weight(3)};
            }
            result.vertices=newVertices;result.boneWeights=outputWeights;result.bindposes=bindposes.ToArray();
            result.ClearBlendShapes();
            for(int shape=0;shape<variant.blendShapeCount;shape++)for(int frame=0;frame<variant.GetBlendShapeFrameCount(shape);frame++)
            {
                var positions=new Vector3[variant.vertexCount];var normals=new Vector3[variant.vertexCount];var tangents=new Vector3[variant.vertexCount];
                variant.GetBlendShapeFrameVertices(shape,frame,positions,normals,tangents);
                result.AddBlendShapeFrame(variant.GetBlendShapeName(shape),variant.GetBlendShapeFrameWeight(shape,frame),
                    mapping.Select(m=>originalToCandidate.MultiplyVector(m.Read(positions))).ToArray(),
                    mapping.Select(m=>candidateToOriginal.transpose.MultiplyVector(m.Read(normals))).ToArray(),
                    mapping.Select(m=>originalToCandidate.MultiplyVector(m.Read(tangents))).ToArray());
            }
            result.RecalculateBounds();return result;
            }
            catch {UnityEngine.Object.DestroyImmediate(result);throw;}
        }
        private static bool IsFingerBone(string name,string side)=>new[]{"Thumb","Index","Middle","Ring","Little"}.Any(f=>name.StartsWith(side+f,StringComparison.Ordinal));

        internal static void InspectPlayerHandRigCandidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Candidate import comparison requires Edit Mode.");
            var originalImporter = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            var importer = (ModelImporter)AssetImporter.GetAtPath(CandidatePath);
            foreach (var entry in originalImporter.GetExternalObjectMap()) importer.AddRemap(entry.Key, entry.Value);
            importer.animationType = originalImporter.animationType;
            // The extended skeleton needs its own Generic avatar, including the new finger nodes.
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importNormals = originalImporter.importNormals;
            importer.importTangents = originalImporter.importTangents;
            importer.globalScale = originalImporter.globalScale;
            importer.isReadable = true;
            // Subdivision interpolates existing forearm influences below the default .001
            // importer cutoff. Preserve those source weights instead of silently pruning them.
            importer.skinWeights=ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex=4;importer.minBoneWeight=0.000001f;
            importer.SaveAndReimport();
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(CandidatePath);
            var report = new StringBuilder();
            report.AppendLine("candidateAnimator="+(candidate.GetComponent<Animator>()==null?"none":"present"));
            foreach(var a in candidate.GetComponentsInChildren<Animator>(true))report.AppendLine("avatar="+(a.avatar==null?"none":a.avatar.name)+" hasHierarchy="+a.hasTransformHierarchy);
            float maxPosition = 0f, maxRotation = 0f, maxScale = 0f;
            foreach (Transform sourceBone in original.GetComponentsInChildren<Transform>(true))
            {
                if (sourceBone == original.transform) continue;
                string path = RelativePath(sourceBone, original.transform);
                Transform destination = candidate.transform.Find(path);
                if (destination == null) throw new InvalidOperationException("Existing node missing: " + path);
                float position = Vector3.Distance(sourceBone.localPosition, destination.localPosition);
                float rotation = Quaternion.Angle(sourceBone.localRotation, destination.localRotation);
                float scale = Vector3.Distance(sourceBone.localScale, destination.localScale);
                maxPosition = Mathf.Max(maxPosition, position); maxRotation = Mathf.Max(maxRotation, rotation); maxScale = Mathf.Max(maxScale, scale);
                report.AppendLine(path + " position=" + position.ToString("F8") + " rotation=" + rotation.ToString("F6") + " scale=" + scale.ToString("F8"));
            }
            foreach (SkinnedMeshRenderer renderer in candidate.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                report.AppendLine("candidateMesh=" + renderer.sharedMesh.name + " vertices=" + renderer.sharedMesh.vertexCount +
                    " bones=" + renderer.bones.Length + " material=" + string.Join(",",renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            report.AppendLine("maxPosition=" + maxPosition.ToString("F8") + "; maxRotation=" + maxRotation.ToString("F6") + "; maxScale=" + maxScale.ToString("F8"));
            report.AppendLine("compatibility=" + (maxPosition <= 0.00002f && maxRotation <= 0.05f && maxScale <= 0.00002f ? "PASS" : "FAIL"));
            File.WriteAllText(Evidence + "/candidate_compatibility.txt", report.ToString(), new UTF8Encoding(false));
            var originalSkin=original.GetComponentInChildren<SkinnedMeshRenderer>();
            var candidateSkin=candidate.GetComponentInChildren<SkinnedMeshRenderer>();
            string[] originalNames=originalSkin.bones.Select(b=>b.name).ToArray(),candidateNames=candidateSkin.bones.Select(b=>b.name).ToArray();
            Matrix4x4 candidateToOriginal=originalSkin.sharedMesh.bindposes[Array.IndexOf(originalNames,"Hips")].inverse*
                candidateSkin.sharedMesh.bindposes[Array.IndexOf(candidateNames,"Hips")];
            MapOriginalSkinToCandidate(originalSkin.sharedMesh,candidateSkin.sharedMesh,candidateToOriginal,originalNames,candidateNames);
            Debug.Log("Shared hand candidate structural comparison recorded. Visual review still required.");
        }

        [Serializable] private class HandVertex
        {
            public int index;
            public Vector3 handLocal, modelLocal;
            public float handWeight;
        }
        [Serializable] private class HandSurface
        {
            public string side, mesh;
            public Vector3 wristPosition, wristEuler, wristScale;
            public HandVertex[] vertices;
            public int[] triangles;
        }
        [Serializable] private class SourceData { public HandSurface[] surfaces; }

        internal static void InspectPlayerHandRigSources()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Inspect the source in Edit Mode without changing live poses.");
            Directory.CreateDirectory(Evidence);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Shared transporter model is missing.");
            var report = new StringBuilder();
            report.AppendLine("source=" + ModelPath);
            report.AppendLine("sourceUnmodified=True; sceneUnmodified=True");
            foreach (Transform bone in source.GetComponentsInChildren<Transform>(true))
                report.AppendLine("node=" + RelativePath(bone, source.transform) + " position=" + bone.localPosition.ToString("F6") +
                    " rotation=" + bone.localRotation.ToString("F6") + " scale=" + bone.localScale.ToString("F6"));

            var surfaces = new List<HandSurface>();
            foreach (SkinnedMeshRenderer renderer in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                report.AppendLine("mesh=" + mesh.name + " vertices=" + mesh.vertexCount + " triangles=" + mesh.triangles.Length / 3 +
                    " bones=" + renderer.bones.Length + " blendShapes=" + mesh.blendShapeCount);
                for (int i = 0; i < renderer.bones.Length; i++)
                    report.AppendLine("skinBone[" + i + "]=" + RelativePath(renderer.bones[i], source.transform));
            }
            var preview = new PreviewRenderUtility();
            try
            {
                GameObject instance = UnityEngine.Object.Instantiate(source);
                preview.AddSingleGO(instance);
                foreach (string side in new[] { "Left", "Right" })
                {
                    Transform hand = instance.GetComponentsInChildren<Transform>(true).Single(t => t.name == side + "Hand");
                    var points = new List<Vector3>();
                    foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        int boneIndex = Array.IndexOf(renderer.bones, hand);
                        if (boneIndex < 0) continue;
                        var baked = new Mesh();
                        renderer.BakeMesh(baked);
                        try
                        {
                            Vector3[] vertices = baked.vertices;
                            BoneWeight[] weights = renderer.sharedMesh.boneWeights;
                            var selected = new List<HandVertex>();
                            for (int v = 0; v < vertices.Length; v++)
                            {
                                BoneWeight w = weights[v];
                                float weight = (w.boneIndex0 == boneIndex ? w.weight0 : 0f) +
                                    (w.boneIndex1 == boneIndex ? w.weight1 : 0f) + (w.boneIndex2 == boneIndex ? w.weight2 : 0f) +
                                    (w.boneIndex3 == boneIndex ? w.weight3 : 0f);
                                if (weight < 0.05f) continue;
                                Vector3 world = renderer.transform.TransformPoint(vertices[v]);
                                selected.Add(new HandVertex { index = v, handLocal = hand.InverseTransformPoint(world),
                                    modelLocal = instance.transform.InverseTransformPoint(world), handWeight = weight });
                                if (weight > 0.75f) points.Add(world);
                            }
                            var indices = new HashSet<int>(selected.Select(v => v.index));
                            int[] triangles = baked.triangles;
                            var faces = new List<int>();
                            for (int t = 0; t < triangles.Length; t += 3)
                                if (indices.Contains(triangles[t]) && indices.Contains(triangles[t+1]) && indices.Contains(triangles[t+2]))
                                    faces.AddRange(new[] { triangles[t], triangles[t+1], triangles[t+2] });
                            surfaces.Add(new HandSurface { side = side, mesh = renderer.sharedMesh.name,
                                wristPosition = instance.transform.InverseTransformPoint(hand.position), wristEuler = hand.eulerAngles,
                                wristScale = hand.lossyScale, vertices = selected.ToArray(), triangles = faces.ToArray() });
                            report.AppendLine(side + " handSurface=" + selected.Count + " handTriangles=" + faces.Count / 3);
                        }
                        finally { UnityEngine.Object.DestroyImmediate(baked); }
                    }
                    Vector3 center = points.Aggregate(Vector3.zero, (sum, p) => sum + p) / points.Count;
                    float sign = side == "Right" ? 1f : -1f;
                    Vector3[] views = { Vector3.right * sign, Vector3.left * sign, Vector3.forward,
                        (Vector3.right * sign + Vector3.forward).normalized };
                    string baseline = Evidence + "/baseline";
                    // Exactly one baseline set; later calls only refresh the read-only inventory.
                    if (!Directory.Exists(baseline + "/" + side))
                    {
                        Directory.CreateDirectory(baseline + "/" + side);
                        for (int v = 0; v < views.Length; v++)
                        {
                            Camera camera = preview.camera;
                            camera.orthographic = true; camera.orthographicSize = 0.11f;
                            camera.transform.position = center + views[v] * 2f;
                            camera.transform.LookAt(center, Vector3.up);
                            camera.nearClipPlane = 1.89f; camera.farClipPlane = 2.11f;
                            camera.clearFlags = CameraClearFlags.SolidColor;
                            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
                            preview.lights[0].intensity = 1.2f; preview.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
                            preview.lights[1].intensity = 0.8f; preview.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f);
                            preview.ambientColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                            preview.BeginStaticPreview(new Rect(0, 0, 900, 900));
                            preview.Render(true);
                            Texture2D texture = preview.EndStaticPreview();
                            File.WriteAllBytes(baseline + "/" + side + "/view_" + v + ".png", texture.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(texture);
                        }
                    }
                }
            }
            finally { preview.Cleanup(); }
            File.WriteAllText(Evidence + "/source_hands.json", JsonUtility.ToJson(new SourceData { surfaces = surfaces.ToArray() }, true), new UTF8Encoding(false));
            foreach (SkinnedMeshRenderer renderer in Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>()
                .Where(r => r.gameObject.scene.IsValid() && r.gameObject.scene.isLoaded && !EditorUtility.IsPersistent(r)))
            {
                string path = AssetDatabase.GetAssetPath(renderer.sharedMesh);
                if (!path.StartsWith("Assets/_Project/Art/Player/", StringComparison.Ordinal)) continue;
                Animator animator = renderer.GetComponentInParent<Animator>();
                report.AppendLine("sceneUse=" + renderer.gameObject.scene.path + "|" + RelativePath(renderer.transform, null) +
                    "|mesh=" + path + "|bones=" + renderer.bones.Length + "|controller=" +
                    (animator == null ? "NONE" : AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)));
            }
            File.WriteAllText(Evidence + "/source_inventory.txt", report.ToString(), new UTF8Encoding(false));
            Debug.Log("Shared transporter hand source inspected. No model, skin or scene changes applied.");
        }

        internal static void CapturePlayerHandRigTopology()
        {
            string folder = Evidence + "/diagnostic_01_topology";
            if (Directory.Exists(folder)) throw new InvalidOperationException("The first diagnostic set already exists.");
            SourceData data = JsonUtility.FromJson<SourceData>(File.ReadAllText(Evidence + "/source_hands.json"));
            Directory.CreateDirectory(folder);
            foreach (HandSurface surface in data.surfaces)
            {
                var byId = surface.vertices.ToDictionary(v => v.index);
                var texture = new Texture2D(1200, 1200, TextureFormat.RGBA32, false);
                Color32[] pixels = Enumerable.Repeat(new Color32(245,247,250,255), 1200*1200).ToArray();
                Func<Vector3, Vector2Int> project = p => new Vector2Int(
                    Mathf.RoundToInt(600 + p.z * 4000), Mathf.RoundToInt(180 + p.y * 4000));
                for (int i = 0; i < surface.triangles.Length; i += 3)
                    for (int e = 0; e < 3; e++)
                    {
                        Vector3 a = byId[surface.triangles[i+e]].handLocal;
                        Vector3 b = byId[surface.triangles[i+(e+1)%3]].handLocal;
                        Vector2Int p = project(a), q = project(b);
                        int count = Mathf.Max(Mathf.Abs(q.x-p.x), Mathf.Abs(q.y-p.y));
                        for (int j = 0; j <= count; j++)
                        {
                            Vector2 v = Vector2.Lerp(p, q, count == 0 ? 0f : j/(float)count);
                            int x = Mathf.RoundToInt(v.x), y = Mathf.RoundToInt(v.y);
                            if (x >= 0 && x < 1200 && y >= 0 && y < 1200) pixels[y*1200+x] = new Color32(70,80,100,255);
                        }
                    }
                foreach (HandVertex vertex in surface.vertices)
                {
                    Vector2Int p = project(vertex.handLocal);
                    for (int x = p.x-2; x <= p.x+2; x++) for (int y = p.y-2; y <= p.y+2; y++)
                        if (x >= 0 && x < 1200 && y >= 0 && y < 1200) pixels[y*1200+x] = new Color32(190,35,35,255);
                }
                texture.SetPixels32(pixels); texture.Apply();
                File.WriteAllBytes(folder + "/" + surface.side + "_hand_yz_topology.png", texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            File.WriteAllText(folder + "/README.txt", "Read-only topology projection of the previously observed original hand. Horizontal=hand-local Z, vertical=hand-local Y. No vertices, skin weights, bones or target poses edited. This diagram is supporting evidence, not a motion pass.");
        }

        private static string RelativePath(Transform value, Transform root)
        {
            if (value == null) return "NULL";
            string path = value.name;
            while (value.parent != null && value.parent != root) { value = value.parent; path = value.name + "/" + path; }
            return path;
        }
    }
}
