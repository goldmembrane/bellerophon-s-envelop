using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Bellerophon.Enemies.Parvum
{
    public enum ParvumMotion { Idle, Move, Attack, Hit, Death }

    public sealed class ParvumAnimationView : MonoBehaviour
    {
        [Serializable] public struct MotionSource
        {
            public Mesh mesh;
            public AnimationClip clip;
        }
        [SerializeField] private Animator animator;
        [SerializeField] private SkinnedMeshRenderer body;
        [SerializeField] private Transform model;
        [SerializeField] private MotionSource[] sources;
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private AnimationClipPlayable hitPlayable;
        private AnimationLayerMixerPlayable layers;
        private float hitElapsed=-1;
        private Vector3 restPosition, restScale;
        private Quaternion restRotation;
        private float elapsed;
        private bool initialized;
        private Mesh contactMesh;
        private int contactCycle=-1;
        private readonly System.Collections.Generic.List<Vector3> contactVertices=new System.Collections.Generic.List<Vector3>();
        private Vector3[] previousLipVertices;
        // Nearby mesh vertices share a contact cell; avoid thousands of identical wall queries.
        private readonly System.Collections.Generic.HashSet<Vector3Int> lipCells=new System.Collections.Generic.HashSet<Vector3Int>();
        public Vector3 MouthTip { get; private set; }
        public int BiteCycle => sources==null ? -1 : Mathf.FloorToInt(elapsed/sources[(int)ParvumMotion.Attack].clip.length);
        public bool IsBiteContactPhase => Motion==ParvumMotion.Attack &&
            elapsed%sources[(int)Motion].clip.length/sources[(int)Motion].clip.length>=.55f &&
            elapsed%sources[(int)Motion].clip.length/sources[(int)Motion].clip.length<=.9f;
        public ParvumMotion Motion { get; private set; }

        public void Configure(Animator target, SkinnedMeshRenderer renderer, Transform modelRoot, MotionSource[] motions)
        { animator=target; body=renderer; model=modelRoot; sources=motions; }
        public MotionSource[] Sources => sources;
        public void UseCombinedMesh(Mesh mesh)
        {for(int i=0;i<sources.Length;i++)sources[i].mesh=mesh;body.sharedMesh=mesh;}

        private void Awake()
        {
            restPosition=model.localPosition;restScale=model.localScale;restRotation=model.localRotation;
            animator.runtimeAnimatorController=null;animator.applyRootMotion=false;
            SetMotion(ParvumMotion.Idle);
        }

        public void SetMotion(ParvumMotion motion, bool restart=false)
        {
            if(motion==ParvumMotion.Hit)
            {
                if(Motion==ParvumMotion.Death)return;
                hitElapsed=0;
                return;
            }
            if(initialized && Motion==motion && !restart) return;
            if(graph.IsValid()) graph.Destroy();
            model.localPosition=restPosition;model.localRotation=restRotation;model.localScale=restScale;
            Motion=motion;elapsed=0;initialized=true;
            contactCycle=-1;
            if(motion==ParvumMotion.Death)hitElapsed=-1;
            var source=sources[(int)motion];body.sharedMesh=source.mesh;
            for(int i=0;i<source.mesh.blendShapeCount;i++) body.SetBlendShapeWeight(i,0);
            graph=PlayableGraph.Create("Parvum existing motion");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            playable=AnimationClipPlayable.Create(graph,source.clip);
            playable.SetApplyFootIK(false);playable.SetApplyPlayableIK(false);
            hitPlayable=AnimationClipPlayable.Create(graph,sources[(int)ParvumMotion.Hit].clip);
            hitPlayable.SetApplyFootIK(false);hitPlayable.SetApplyPlayableIK(false);
            layers=AnimationLayerMixerPlayable.Create(graph,2);
            graph.Connect(playable,0,layers,0);layers.SetInputWeight(0,1);
            graph.Connect(hitPlayable,0,layers,1);layers.SetInputWeight(1,0);layers.SetLayerAdditive(1,true);
            AnimationPlayableOutput.Create(graph,"Body",animator).SetSourcePlayable(layers);
            graph.Play();graph.Evaluate(0);
        }

        private void Update()
        {
            if(!graph.IsValid()) return;
            var clip=sources[(int)Motion].clip;
            // Existing three-second bite is replayed on the specified half-second combat cadence.
            float speed=Motion==ParvumMotion.Attack ? clip.length/.5f : 1f;
            elapsed+=Time.deltaTime*speed;
            double sample=Motion==ParvumMotion.Death || Motion==ParvumMotion.Hit
                ? Math.Min(elapsed,clip.length-.0001f) : elapsed%clip.length;
            playable.SetTime(sample);graph.Evaluate(0);
            if(hitElapsed>=0)
            {
                hitElapsed+=Time.deltaTime;
                if(hitElapsed>=sources[(int)ParvumMotion.Hit].clip.length)hitElapsed=-1;
            }
            layers.SetInputWeight(1,hitElapsed>=0?1:0);
            hitPlayable.SetTime(Math.Max(0,hitElapsed));
            graph.Evaluate(0);
        }
        private void LateUpdate()
        {
            if(Motion!=ParvumMotion.Attack)return;
            bool sameCycle=contactCycle==BiteCycle;contactCycle=BiteCycle;
            if(!contactMesh)contactMesh=new Mesh{name="Parvum live mouth contact"};
            body.BakeMesh(contactMesh,true);contactMesh.GetVertices(contactVertices);
            var brain=GetComponentInParent<ParvumBrain>();var root=brain.transform;
            if(previousLipVertices==null || previousLipVertices.Length!=contactVertices.Count){previousLipVertices=new Vector3[contactVertices.Count];sameCycle=false;}
            float front=float.NegativeInfinity;
            // Find the live lip silhouette, then sweep its front band using stable vertex IDs.
            // A single extremum can jump between opposite lips and miss a narrow wall entirely.
            foreach(var vertex in contactVertices)
            {
                var world=body.transform.TransformPoint(vertex);var local=root.InverseTransformPoint(world);
                if(local.z>front){front=local.z;MouthTip=world;}
            }
            lipCells.Clear();
            for(int i=0;i<contactVertices.Count;i++)
            {
                var world=body.transform.TransformPoint(contactVertices[i]);var local=root.InverseTransformPoint(world);
                if(IsBiteContactPhase && local.z>=front-.12f && lipCells.Add(Vector3Int.FloorToInt(local/.02f)))brain.CommitMouthContact(world,previousLipVertices[i],sameCycle);
                previousLipVertices[i]=world;
            }
        }
        private void OnDestroy() { if(graph.IsValid()) graph.Destroy();if(contactMesh)Destroy(contactMesh); }
    }
}
