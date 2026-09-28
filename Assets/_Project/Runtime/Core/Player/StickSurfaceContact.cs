using UnityEngine;

namespace Bellerophon.Core.Player
{
    // Narrow phase against the visible animated surface, not the navigation capsule.
    internal static class StickSurfaceContact
    {
        public static bool Touches(Vector3 start,Vector3 end,float radius,Vector3[] vertices,int[] triangles,out Vector3 point)
        {
            float squared=radius*radius;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
                var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);bounds.Expand(radius*2);
                var segmentBounds=new Bounds(start,Vector3.zero);segmentBounds.Encapsulate(end);
                if(!bounds.Intersects(segmentBounds))continue;
                var normal=Vector3.Cross(b-a,c-a);float denominator=Vector3.Dot(normal,end-start);
                if(Mathf.Abs(denominator)>1e-10f)
                {
                    float t=Vector3.Dot(normal,a-start)/denominator;
                    if(t>=0 && t<=1){var p=Vector3.Lerp(start,end,t);var q=Closest(p,a,b,c);if((p-q).sqrMagnitude<1e-8f){point=q;return true;}}
                }
                var from=Closest(start,a,b,c);if((start-from).sqrMagnitude<=squared){point=from;return true;}
                var to=Closest(end,a,b,c);if((end-to).sqrMagnitude<=squared){point=to;return true;}
                if(Edges(start,end,a,b,out point)<=squared || Edges(start,end,b,c,out point)<=squared || Edges(start,end,c,a,out point)<=squared)return true;
            }
            point=default;return false;
        }
        static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
            if(d1<=0 && d2<=0)return a;
            var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0 && d4<=d3)return b;
            float vc=d1*d4-d3*d2;if(vc<=0 && d1>=0 && d3<=0)return a+ab*(d1/(d1-d3));
            var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0 && d5<=d6)return c;
            float vb=d5*d2-d1*d6;if(vb<=0 && d2>=0 && d6<=0)return a+ac*(d2/(d2-d6));
            float va=d3*d6-d5*d4;if(va<=0 && d4-d3>=0 && d5-d6>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
            float sum=va+vb+vc;if(Mathf.Abs(sum)<1e-12f)return a;return a+ab*(vb/sum)+ac*(vc/sum);
        }
        static float Edges(Vector3 p,Vector3 q,Vector3 a,Vector3 b,out Vector3 point)
        {
            var d1=q-p;var d2=b-a;var r=p-a;float aa=Vector3.Dot(d1,d1),ee=Vector3.Dot(d2,d2),f=Vector3.Dot(d2,r),s,t;
            if(aa<=1e-12f){s=0;t=ee>1e-12f?Mathf.Clamp01(f/ee):0;}
            else
            {
                float c=Vector3.Dot(d1,r);
                if(ee<=1e-12f){t=0;s=Mathf.Clamp01(-c/aa);}
                else{float bb=Vector3.Dot(d1,d2),denom=aa*ee-bb*bb;s=denom>1e-12f?Mathf.Clamp01((bb*f-c*ee)/denom):0;t=(bb*s+f)/ee;if(t<0){t=0;s=Mathf.Clamp01(-c/aa);}else if(t>1){t=1;s=Mathf.Clamp01((bb-c)/aa);}}
            }
            point=a+d2*t;return (p+d1*s-point).sqrMagnitude;
        }
    }
}
