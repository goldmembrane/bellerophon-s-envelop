using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.Enemies.LongaArma
{
    // Presentation follows physical floor contact; never moves the gameplay rigidbody.
    [DefaultExecutionOrder(-80)]
    public sealed class LongaArmaGrounding : MonoBehaviour
    {
        LongaArmaAnimationView view;
        Transform slot;
        Vector3 originalPosition;
        Quaternion originalRotation;
        readonly List<Transform[]> legs=new List<Transform[]>();
        readonly List<Quaternion[]> saved=new List<Quaternion[]>();
        readonly RaycastHit[] hits=new RaycastHit[48];
        readonly List<Vector3> vertices=new List<Vector3>();
        Mesh baked;
        bool applied;
        // Smooth only locomotion's presentation correction across ramp triangle/foot changes.
        Vector3 movementNormal=Vector3.up;
        float movementLift,liftVelocity;
        bool followingGround;
        void Awake(){view=GetComponent<LongaArmaAnimationView>();}
        void Update(){Restore();}
        void Restore()
        {
            if(!applied || !slot)return;
            slot.localPosition=originalPosition;slot.localRotation=originalRotation;
            for(int i=0;i<legs.Count;i++)for(int j=0;j<2;j++)legs[i][j].localRotation=saved[i][j];
            applied=false;
        }
        bool Floor(Vector3 position,out RaycastHit floor)
        {
            floor=default;float nearest=float.PositiveInfinity;
            var origin=new Vector3(position.x,transform.position.y+1.2f,position.z);
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,hits,3.5f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(!hits[i].transform.IsChildOf(transform) && hits[i].normal.y>.5f && hits[i].distance<nearest)
                {nearest=hits[i].distance;floor=hits[i];}
            return floor.collider;
        }
        void LateUpdate()
        {
            if(!view || !view.ActiveSlot || !view.Surface)return;
            if(slot!=view.ActiveSlot.transform)
            {
                Restore();slot=view.ActiveSlot.transform;originalPosition=slot.localPosition;originalRotation=slot.localRotation;
                legs.Clear();saved.Clear();
                foreach(string prefix in new[]{"frontleg","backleg","R_backleg"})
                {
                    var chain=new Transform[3];
                    foreach(var bone in view.Surface.bones)
                        for(int j=0;j<3;j++)if(bone && bone.name==prefix+j)chain[j]=bone;
                    if(chain[0] && chain[1] && chain[2]){legs.Add(chain);saved.Add(new Quaternion[2]);}
                }
            }
            for(int i=0;i<legs.Count;i++)for(int j=0;j<2;j++)saved[i][j]=legs[i][j].localRotation;
            applied=true;
            if(view.Motion==LongaArmaMotion.Death)
            {
                if(!Floor(transform.position,out var floor))return;
                if(!baked)baked=new Mesh {name="Longa death ground bounds"};
                view.Surface.BakeMesh(baked,true);baked.GetVertices(vertices);
                float min=float.PositiveInfinity;var matrix=view.Surface.localToWorldMatrix;
                foreach(var point in vertices)min=Mathf.Min(min,matrix.MultiplyPoint3x4(point).y);
                if(!float.IsInfinity(min))slot.position+=Vector3.up*(floor.point.y+.005f-min);
                return;
            }
            Vector3 normal=Vector3.zero;int found=0;
            foreach(var leg in legs)if(Floor(leg[2].position,out var floor)){normal+=floor.normal;found++;}
            if(found==0)return;
            bool moving=view.Motion==LongaArmaMotion.Move;
            if(!moving || !followingGround)movementNormal=normal.normalized;
            else movementNormal=Vector3.Slerp(movementNormal,normal.normalized,1-Mathf.Exp(-10f*Time.deltaTime)).normalized;
            slot.rotation=Quaternion.FromToRotation(Vector3.up,movementNormal)*slot.rotation;
            float lowest=float.PositiveInfinity;
            foreach(var leg in legs)if(Floor(leg[2].position,out var floor))lowest=Mathf.Min(lowest,leg[2].position.y-floor.point.y);
            if(float.IsInfinity(lowest))return;
            float lift=.04f-lowest;
            if(!moving || !followingGround){movementLift=lift;liftVelocity=0;}
            else movementLift=Mathf.SmoothDamp(movementLift,lift,ref liftVelocity,.08f,4f,Time.deltaTime);
            followingGround=moving;
            slot.position+=Vector3.up*movementLift;
            foreach(var leg in legs)
            {
                if(!Floor(leg[2].position,out var floor))continue;
                // Preserve authored lifted swing feet; solve only the supporting feet.
                float gap=leg[2].position.y-floor.point.y;
                if(gap>.17f)continue;
                var goal=leg[2].position;goal.y=floor.point.y+.04f;
                Solve(leg[0],leg[1],leg[2],goal);
            }
        }
        static void Solve(Transform upper,Transform knee,Transform foot,Vector3 goal)
        {
            var start=upper.position;float a=Vector3.Distance(start,knee.position),b=Vector3.Distance(knee.position,foot.position);
            var direction=(goal-start).normalized;float distance=Mathf.Clamp(Vector3.Distance(start,goal),.001f,a+b-.001f);
            var bend=Vector3.ProjectOnPlane(knee.position-start,direction).normalized;
            if(bend.sqrMagnitude<.001f)bend=Vector3.Cross(direction,Vector3.right).normalized;
            float along=(a*a+distance*distance-b*b)/(2*distance);
            var joint=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(knee.position-start,joint-start)*upper.rotation;
            knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,goal-knee.position)*knee.rotation;
        }
        void OnDisable(){Restore();}
        void OnDestroy(){if(baked)Destroy(baked);}
    }
}
