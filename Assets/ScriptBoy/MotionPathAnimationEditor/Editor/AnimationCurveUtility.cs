using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class AnimationCurveUtility
    {
        public static Keyframe CreateKeyframeBetween(Keyframe a, Keyframe c, float time)
        {
            float t = (time - a.time) / (c.time - a.time);
            float dt = c.time - a.time;

            float m0 = a.outTangent * dt;
            float m1 = c.inTangent * dt;

            float t2 = t * t;
            float t3 = t2 * t;

            float h00 = 2 * t3 - 3 * t2 + 1;
            float h10 = t3 - 2 * t2 + t;
            float h01 = -2 * t3 + 3 * t2;
            float h11 = t3 - t2;

            float value = h00 * a.value + h10 * m0 + h01 * c.value + h11 * m1;

            float dh00 = 6 * t2 - 6 * t;
            float dh10 = 3 * t2 - 4 * t + 1;
            float dh01 = -6 * t2 + 6 * t;
            float dh11 = 3 * t2 - 2 * t;

            float tangent = (dh00 * a.value + dh10 * m0 + dh01 * c.value + dh11 * m1) / dt;

            return new Keyframe(time, value, tangent, tangent);
        }
    }
}