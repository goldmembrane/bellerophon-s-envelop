using Bellerophon.Enemies.Parvum;
using UnityEngine;

namespace Bellerophon.Enemies.Seed
{
    // Shared by seed consumers: proximity to a cached point is not proof of a reachable surface.
    public static class SeedMetalApproach
    {
        public static bool HasSurface(ParvumTarget target, Vector3 origin, Transform actor)
        {
            if (!target || !target.Surface || !target.Surface.enabled) return false;
            var point = target.ClosestPoint(origin);
            var delta = point - origin;
            if (delta.sqrMagnitude < .000001f) return false;
            // Existing room liners can expose the reverse side of their collider triangles.
            // Check that same surface from both directions without enabling global backface queries.
            if (!target.Surface.Raycast(new Ray(origin, delta.normalized), out var contact, delta.magnitude + .04f) &&
                !target.Surface.Raycast(new Ray(point + delta.normalized * .04f, -delta.normalized), out contact, delta.magnitude + .04f)) return false;
            if (Vector3.Distance(contact.point, point) > .06f) return false;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, Vector3.Distance(origin,contact.point), ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(actor) && hit.collider != target.Surface && !hit.transform.IsChildOf(target.transform)) return false;
            return true;
        }

        public static bool ClearFlightLeg(SphereCollider hull, Vector3 start, Vector3 end, ParvumTarget target)
        {
            var delta = end - start;
            if (delta.sqrMagnitude < .000001f) return true;
            foreach (var hit in Physics.SphereCastAll(start, hull.radius, delta.normalized, delta.magnitude + .025f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(hull.transform) || hit.transform.IsChildOf(target.transform)) continue;
                if (hit.distance <= .0001f &&
                    (!Physics.ComputePenetration(hull, start, hull.transform.rotation, hit.collider, hit.transform.position, hit.transform.rotation, out var normal, out _) || Vector3.Dot(delta, normal) >= 0)) continue;
                return false;
            }
            return true;
        }
    }
}
