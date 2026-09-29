using System;
using System.Collections.Generic;
using Bellerophon.Enemies.Parvum;
using Bellerophon.Enemies.LongaArma;
using UnityEngine;

namespace Bellerophon.Core.Player
{
    // Collision geometry is the rendered body, not the smaller locomotion capsule.
    // Previous/current world vertices account for both actors moving between rendered frames.
    internal sealed class StickAnimatedSurface : IDisposable
    {
        public readonly MonoBehaviour Enemy;
        SkinnedMeshRenderer renderer;
        public bool Alive=>Enemy && (Enemy is ParvumBrain p?p.Health>0:Enemy is Bellerophon.Enemies.Fuga.FugaBrain f?f.Health>0:Enemy is LongaArmaBrain l && l.Health>0);
        public void Damage(float damage,ParvumTarget source)
        {if(Enemy is ParvumBrain p)p.ReceiveDamage(damage,source);else if(Enemy is Bellerophon.Enemies.Fuga.FugaBrain f)f.ReceiveDamage(damage,source);else if(Enemy is LongaArmaBrain l)l.ReceiveDamage(damage,source);}
        readonly Mesh baked = new Mesh { name = "Stick moving body contact" };
        Mesh source;
        SkinnedMeshRenderer previousRenderer;
        Vector3[] previous, current, sampled;
        Vector3[] bufferA,bufferB,vertexMin,vertexMax;
        readonly List<Vector3> bakedVertices=new List<Vector3>();
        int[] triangles;
        // A stable mesh-topology tree is refitted to the animated swept surface each frame.
        // Temporal samples traverse only nearby leaves, without reducing sweep precision.
        sealed class SurfaceNode { public Vector3 min,max; public int start,count,left=-1,right=-1; }
        sealed class SurfaceTree { public int[] triangles,order; public List<SurfaceNode> nodes; }
        readonly Dictionary<Mesh,SurfaceTree> trees=new Dictionary<Mesh,SurfaceTree>();
        List<SurfaceNode> nodes=new List<SurfaceNode>();
        int[] triangleOrder;
        readonly int[] leafTriangles=new int[24];
        Bounds sweptBounds;
        public float Movement { get; private set; }
        public double BakeMilliseconds,RefitMilliseconds;

        public StickAnimatedSurface(MonoBehaviour enemy)
        {
            Enemy=enemy;renderer=enemy.GetComponentInChildren<SkinnedMeshRenderer>();
            // State slots can have distinct meshes. Prepare each visible authored mesh once,
            // before the first strike, instead of rebuilding at consume/hit/move transitions.
            foreach(var skin in enemy.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=skin.sharedMesh;
                if(!skin.enabled || !skin.gameObject.activeSelf || !mesh || trees.ContainsKey(mesh) || !mesh.isReadable)continue;
                var vertices=new List<Vector3>();mesh.GetVertices(vertices);
                SelectTree(mesh,mesh.triangles,vertices);
            }
            source=null;
        }

        void SelectTree(Mesh mesh,int[] indices,List<Vector3> vertices)
        {
            if(!trees.TryGetValue(mesh,out var tree))
            {
                triangles=indices;nodes=new List<SurfaceNode>();triangleOrder=new int[triangles.Length/3];
                for(int i=0;i<triangleOrder.Length;i++)triangleOrder[i]=i;
                if(triangleOrder.Length>0)BuildNode(vertices,0,triangleOrder.Length);
                tree=new SurfaceTree { triangles=triangles,order=triangleOrder,nodes=nodes };trees.Add(mesh,tree);
            }
            triangles=tree.triangles;triangleOrder=tree.order;nodes=tree.nodes;source=mesh;
        }

        public void ResetHistory() { previous=null;current=null;previousRenderer=null;Movement=0; }

        public void Capture()
        {
            if(Enemy is Bellerophon.Enemies.Fuga.FugaBrain)renderer=Enemy.GetComponent<Bellerophon.Enemies.Fuga.FugaAnimationView>().Surface;
            if(Enemy is LongaArmaBrain)renderer=Enemy.GetComponent<LongaArmaAnimationView>().Surface;
            if(!Alive || !renderer || !renderer.enabled)return;
            long timer=System.Diagnostics.Stopwatch.GetTimestamp();
            if(Enemy is Bellerophon.Enemies.Fuga.FugaBrain || Enemy is LongaArmaBrain)renderer.BakeMesh(baked,true);
            else renderer.BakeMesh(baked);
            BakeMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-timer)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            timer=System.Diagnostics.Stopwatch.GetTimestamp();
            baked.GetVertices(bakedVertices);
            bool continuous=previousRenderer==renderer && source==renderer.sharedMesh && current!=null && current.Length==bakedVertices.Count;
            previousRenderer=renderer;
            previous=continuous?current:null;
            if(source!=renderer.sharedMesh)
            {
                SelectTree(renderer.sharedMesh,trees.ContainsKey(renderer.sharedMesh)?null:baked.triangles,bakedVertices);
            }
            if(bufferA==null || bufferA.Length!=bakedVertices.Count)
            {bufferA=new Vector3[bakedVertices.Count];bufferB=new Vector3[bakedVertices.Count];vertexMin=new Vector3[bakedVertices.Count];vertexMax=new Vector3[bakedVertices.Count];}
            current=ReferenceEquals(current,bufferA)?bufferB:bufferA;Movement=0;
            if(current.Length==0)return;
            var matrix=renderer.localToWorldMatrix;
            for(int i=0;i<current.Length;i++)current[i]=matrix.MultiplyPoint3x4(bakedVertices[i]);
            var min=current[0];var max=min;
            for(int i=0;i<current.Length;i++)
            {
                var lo=current[i];var hi=lo;
                if(previous!=null){Extend(ref lo,ref hi,previous[i],previous[i]);Movement=Mathf.Max(Movement,(current[i]-previous[i]).sqrMagnitude);}
                vertexMin[i]=lo;vertexMax[i]=hi;Extend(ref min,ref max,lo,hi);
            }
            sweptBounds=new Bounds((min+max)*.5f,max-min);
            Movement=Mathf.Sqrt(Movement);
            if(sampled==null || sampled.Length!=current.Length)sampled=new Vector3[current.Length];
            Refit();
            RefitMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-timer)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }

        int BuildNode(List<Vector3> vertices,int start,int count)
        {
            int index=nodes.Count;var node=new SurfaceNode { start=start,count=count };nodes.Add(node);
            if(count<=8)return index;
            Vector3 Center(int triangle)=>(vertices[triangles[triangle*3]]+vertices[triangles[triangle*3+1]]+vertices[triangles[triangle*3+2]])/3;
            var bounds=new Bounds(Center(triangleOrder[start]),Vector3.zero);
            for(int i=start+1;i<start+count;i++)bounds.Encapsulate(Center(triangleOrder[i]));
            int axis=bounds.size.x>bounds.size.y?0:1;if(bounds.size.z>bounds.size[axis])axis=2;
            Array.Sort(triangleOrder,start,count,Comparer<int>.Create((a,b)=>Center(a)[axis].CompareTo(Center(b)[axis])));
            int half=count/2;node.left=BuildNode(vertices,start,half);node.right=BuildNode(vertices,start+half,count-half);
            return index;
        }

        static void Extend(ref Vector3 min,ref Vector3 max,Vector3 lo,Vector3 hi)
        {
            if(lo.x<min.x)min.x=lo.x;if(lo.y<min.y)min.y=lo.y;if(lo.z<min.z)min.z=lo.z;
            if(hi.x>max.x)max.x=hi.x;if(hi.y>max.y)max.y=hi.y;if(hi.z>max.z)max.z=hi.z;
        }

        void Refit()
        {
            for(int n=nodes.Count-1;n>=0;n--)
            {
                var node=nodes[n];
                if(node.left>=0)
                {
                    var left=nodes[node.left];var right=nodes[node.right];
                    node.min=left.min;node.max=left.max;Extend(ref node.min,ref node.max,right.min,right.max);continue;
                }
                int first=triangles[triangleOrder[node.start]*3];node.min=vertexMin[first];node.max=vertexMax[first];
                for(int i=node.start;i<node.start+node.count;i++)for(int j=0;j<3;j++)
                {int vertex=triangles[triangleOrder[i]*3+j];Extend(ref node.min,ref node.max,vertexMin[vertex],vertexMax[vertex]);}
            }
        }

        public bool Intersects(Bounds shaftSweep)
            => Alive && current!=null && current.Length>0 && sweptBounds.Intersects(shaftSweep);

        public bool Touches(Vector3 a,Vector3 b,float radius,float fraction,out Vector3 point)
        {
            var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Expand(radius*2);
            if(nodes.Count==0){point=default;return false;}
            return TouchNode(0,bounds.min,bounds.max,a,b,radius,fraction,out point);
        }

        bool TouchNode(int index,Vector3 min,Vector3 max,Vector3 a,Vector3 b,float radius,float fraction,out Vector3 point)
        {
            var node=nodes[index];point=default;
            if(node.min.x>max.x || node.max.x<min.x || node.min.y>max.y || node.max.y<min.y || node.min.z>max.z || node.max.z<min.z)return false;
            if(node.left>=0)return TouchNode(node.left,min,max,a,b,radius,fraction,out point) || TouchNode(node.right,min,max,a,b,radius,fraction,out point);
            int count=0;
            for(int i=node.start;i<node.start+node.count;i++)for(int j=0;j<3;j++)
            {
                int vertex=triangles[triangleOrder[i]*3+j];leafTriangles[count++]=vertex;
                if(previous!=null)sampled[vertex]=Vector3.Lerp(previous[vertex],current[vertex],fraction);
            }
            return StickSurfaceContact.Touches(a,b,radius,previous==null?current:sampled,leafTriangles,count,out point);
        }

        public void Dispose(){UnityEngine.Object.Destroy(baked);}
    }
}
