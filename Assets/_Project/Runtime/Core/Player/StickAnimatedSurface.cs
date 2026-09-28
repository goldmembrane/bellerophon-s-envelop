using System;
using Bellerophon.Enemies.Parvum;
using UnityEngine;

namespace Bellerophon.Core.Player
{
    // Collision geometry is the rendered body, not the smaller locomotion capsule.
    // Previous/current world vertices account for both actors moving between rendered frames.
    internal sealed class StickAnimatedSurface : IDisposable
    {
        public readonly MonoBehaviour Enemy;
        SkinnedMeshRenderer renderer;
        public bool Alive=>Enemy && (Enemy is ParvumBrain p?p.Health>0:Enemy is Bellerophon.Enemies.Fuga.FugaBrain f && f.Health>0);
        public void Damage(float damage,ParvumTarget source)
        {if(Enemy is ParvumBrain p)p.ReceiveDamage(damage,source);else if(Enemy is Bellerophon.Enemies.Fuga.FugaBrain f)f.ReceiveDamage(damage,source);}
        readonly Mesh baked = new Mesh { name = "Stick moving body contact" };
        Mesh source;
        Vector3[] previous, current, sampled;
        int[] triangles;
        Bounds sweptBounds;
        public float Movement { get; private set; }

        public StickAnimatedSurface(MonoBehaviour enemy)
        { Enemy=enemy;renderer=enemy.GetComponentInChildren<SkinnedMeshRenderer>(); }

        public void Capture()
        {
            if(Enemy is Bellerophon.Enemies.Fuga.FugaBrain)renderer=Enemy.GetComponent<Bellerophon.Enemies.Fuga.FugaAnimationView>().Surface;
            if(!Alive || !renderer || !renderer.enabled)return;
            if(Enemy is Bellerophon.Enemies.Fuga.FugaBrain)renderer.BakeMesh(baked,true);
            else renderer.BakeMesh(baked);
            var vertices=baked.vertices;
            bool continuous=source==renderer.sharedMesh && current!=null && current.Length==vertices.Length;
            previous=continuous?current:null;
            if(source!=renderer.sharedMesh){triangles=baked.triangles;source=renderer.sharedMesh;}
            current=vertices;Movement=0;
            if(current.Length==0)return;
            for(int i=0;i<current.Length;i++)current[i]=renderer.transform.TransformPoint(current[i]);
            sweptBounds=new Bounds(current[0],Vector3.zero);
            for(int i=0;i<current.Length;i++)
            {
                sweptBounds.Encapsulate(current[i]);
                if(previous!=null){sweptBounds.Encapsulate(previous[i]);Movement=Mathf.Max(Movement,Vector3.Distance(current[i],previous[i]));}
            }
            if(sampled==null || sampled.Length!=current.Length)sampled=new Vector3[current.Length];
        }

        public bool Intersects(Bounds shaftSweep)
            => Alive && current!=null && current.Length>0 && sweptBounds.Intersects(shaftSweep);

        public bool Touches(Vector3 a,Vector3 b,float radius,float fraction,out Vector3 point)
        {
            if(previous==null)return StickSurfaceContact.Touches(a,b,radius,current,triangles,out point);
            for(int i=0;i<current.Length;i++)sampled[i]=Vector3.Lerp(previous[i],current[i],fraction);
            return StickSurfaceContact.Touches(a,b,radius,sampled,triangles,out point);
        }

        public void Dispose(){UnityEngine.Object.Destroy(baked);}
    }
}
