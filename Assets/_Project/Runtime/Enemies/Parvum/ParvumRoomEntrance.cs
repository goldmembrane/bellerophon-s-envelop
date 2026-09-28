using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.Enemies.Parvum
{
    // Runtime-only references to the existing room shell. No markers, geometry or scene edits.
    internal readonly struct ParvumRoomEntrance
    {
        public readonly Vector3 Point, Inward;
        public readonly string Name;
        public ParvumRoomEntrance(Vector3 point, Vector3 inward, string name)
        { Point=point;Inward=inward;Name=name; }

        public static List<ParvumRoomEntrance> Read(Transform root)
        {
            var result=new List<ParvumRoomEntrance>();
            var meshes=root.GetComponentsInChildren<MeshFilter>();
            Bounds roomBounds=default;bool hasBounds=false;
            foreach(var wall in ParvumTarget.Active)
                if(wall.IsRoomWall && wall.Surface && wall.transform.root==root)
                {if(hasBounds)roomBounds.Encapsulate(wall.Surface.bounds);else{roomBounds=wall.Surface.bounds;hasBounds=true;}}
            if(!hasBounds)return result;
            foreach(var mesh in meshes)
            {
                var name=mesh.name;
                if((name.Contains("doorway upper header") && !name.Contains("continuous") && !name.Contains("internal partition")) ||
                    (name.StartsWith("Warehouse wall ") && name.EndsWith(" header")))
                {
                    var center=mesh.GetComponent<Renderer>().bounds.center;
                    // Imported box headers can be CPU-unreadable. Their local bounds retain
                    // the exact box planes without changing import settings or adding colliders.
                    if(!mesh.sharedMesh.isReadable)
                    {
                        var local=mesh.sharedMesh.bounds;var axes=new[]{Vector3.right,Vector3.up,Vector3.forward};
                        float shortest=float.PositiveInfinity;Vector3 inward=default;
                        for(int axis=0;axis<3;axis++)
                        {
                            var extent=mesh.transform.TransformVector(axes[axis]*local.extents[axis]);
                            if(Mathf.Abs(extent.normalized.y)>.1f || extent.magnitude>=shortest)continue;
                            shortest=extent.magnitude;inward=extent.normalized;
                        }
                        if(float.IsInfinity(shortest))continue;
                        if(Vector3.Dot(inward,roomBounds.center-center)<0)inward=-inward;
                        result.Add(new ParvumRoomEntrance(center+inward*shortest,inward,name));continue;
                    }
                    var vertices=WorldVertices(mesh);var indices=mesh.sharedMesh.triangles;
                    float largest=0;Vector3 normal=default;
                    // Broad vertical header face identifies the doorway plane, including baked rotations.
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var cross=Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]);
                        if(cross.sqrMagnitude>largest && Mathf.Abs(cross.normalized.y)<.1f){largest=cross.sqrMagnitude;normal=cross.normalized;}
                    }
                    if(largest==0)continue;
                    if(Vector3.Dot(normal,roomBounds.center-center)<0)normal=-normal;
                    float inside=float.NegativeInfinity;foreach(var vertex in vertices)inside=Mathf.Max(inside,Vector3.Dot(vertex-center,normal));
                    result.Add(new ParvumRoomEntrance(center+normal*inside,normal,name));
                }
            }
            if(root.name=="Approved Cockpit 01 Structure")
            {
                var rearLeft=Find(meshes,"rear bay wall left shoulder");var rearRight=Find(meshes,"rear bay wall right shoulder");
                if(rearLeft && rearRight)
                {
                    var a=rearLeft.GetComponent<Renderer>().bounds;var b=rearRight.GetComponent<Renderer>().bounds;
                    var point=(a.center+b.center)*.5f;point.z=Mathf.Max(a.max.z,b.max.z);
                    result.Add(new ParvumRoomEntrance(point,Vector3.forward,"rear bay room doorway"));
                }
                AddCockpitSide(meshes,result,"left", "03",Vector3.right);
                AddCockpitSide(meshes,result,"right","01",Vector3.left);
            }
            if(root.name=="Approved Engine Room 01 Shell")
            {
                var ring=Find(meshes,"ER-01 smooth interior pressure wall liner continuous upper doorway header");
                if(ring)
                {
                    var center=ring.GetComponent<Renderer>().bounds.center;
                    foreach(var mesh in meshes)
                        if(mesh.name.StartsWith("ShipSpaceCeiling_EngineRoom_Entrance_Slab_"))
                        {
                            var outward=Vector3.ProjectOnPlane(mesh.GetComponent<Renderer>().bounds.center-center,Vector3.up).normalized;
                            var ringBounds=ring.GetComponent<Renderer>().bounds;
                            for(int sample=1;sample<10;sample++)
                            {
                                var origin=center;origin.y=Mathf.Lerp(ringBounds.min.y,ringBounds.max.y,sample/10f);
                                if(!RayMesh(ring,origin,outward,out var hit))continue;
                                result.Add(new ParvumRoomEntrance(hit,-outward,mesh.name+" / inner wall header"));break;
                            }
                        }
                }
            }
            return result;
        }
        private static void AddCockpitSide(MeshFilter[] meshes,List<ParvumRoomEntrance> result,string side,string slab,Vector3 inward)
        {
            var wall=Find(meshes,side+" bay outer wall segment 1");var roof=Find(meshes,"ShipSpaceCeiling_Cockpit_Entrance_Slab_"+slab);
            if(!wall || !roof)return;
            var bounds=wall.GetComponent<Renderer>().bounds;var point=roof.GetComponent<Renderer>().bounds.center;
            point.x=inward.x>0?bounds.max.x:bounds.min.x;
            result.Add(new ParvumRoomEntrance(point,inward,side+" bay room doorway"));
        }
        private static MeshFilter Find(MeshFilter[] meshes,string name)
        {foreach(var mesh in meshes)if(mesh.name==name)return mesh;return null;}
        private static Vector3[] WorldVertices(MeshFilter mesh)
        {var vertices=mesh.sharedMesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=mesh.transform.TransformPoint(vertices[i]);return vertices;}
        private static bool RayMesh(MeshFilter mesh,Vector3 origin,Vector3 direction,out Vector3 hit)
        {
            var v=WorldVertices(mesh);var t=mesh.sharedMesh.triangles;float nearest=float.PositiveInfinity;
            for(int i=0;i<t.Length;i+=3)
            {
                var edge1=v[t[i+1]]-v[t[i]];var edge2=v[t[i+2]]-v[t[i]];var cross=Vector3.Cross(direction,edge2);
                float determinant=Vector3.Dot(edge1,cross);if(Mathf.Abs(determinant)<.000001f)continue;
                var offset=origin-v[t[i]];float u=Vector3.Dot(offset,cross)/determinant;if(u<0 || u>1)continue;
                var q=Vector3.Cross(offset,edge1);float w=Vector3.Dot(direction,q)/determinant;if(w<0 || u+w>1)continue;
                float distance=Vector3.Dot(edge2,q)/determinant;if(distance>0)nearest=Mathf.Min(nearest,distance);
            }
            hit=origin+direction*nearest;return !float.IsInfinity(nearest);
        }
    }
}
