using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    // Only the existing dagger prop is driven here. Character animation and mesh data are never written.
    [DefaultExecutionOrder(1000)]
    public sealed class DaggerPropMotion : MonoBehaviour
    {
        public enum PropAction { Idle, Aim, Throw, Cancel }
        public PropAction action;
        public Animator animator;
        public Transform dagger;
        public Transform rightHand;
        public Vector3 idleLocalPosition;
        public Quaternion idleLocalRotation;
        public Vector3 originalLocalScale;
        public Vector3 bladeAxis;
        public Vector3 handleCenter;
        public Bounds localBounds;
        public float clipLength;
        public float releaseTime;
        // User-selected blade direction/range; launch elevation is copied from Stick_Throw_Release.
        public float bladeElevation = 10f;
        public float throwDistance = 6f;
        public float throwElevation;
        // Horizontal launch heading in transporter-local space. Elevation is applied separately.
        public Vector3 throwDirectionLocal;

        private Rigidbody body;
        private int cycle = -1;
        private bool released;
        private float flightElapsed;
        private float flightDuration;
        private Vector3 releasePosition;
        private Vector3 launchVelocity;
        private Vector3 flightUp;
        private Quaternion flightRotation;
        // Restores the authored pre-adjustment direction recorded from the historical scene.
        private const float RestoredIdleBladeElevationDegrees = 45f;
        private const float ThrowTrajectoryBlendSeconds = 0.12f;
        public bool IsReleased => released;
        public float FlightElapsed => flightElapsed;
        public float FlightDuration => flightDuration;
        public Vector3 ReleasePosition => releasePosition;
        public Vector3 LaunchVelocity => launchVelocity;
        public Vector3 FlightVelocity => launchVelocity - flightUp * (9.81f * flightElapsed);
        public Quaternion FlightRotation => flightRotation;

        private void Awake()
        {
            if (action != PropAction.Throw) return;
            body = dagger.GetComponent<Rigidbody>();
            if (body == null) body = dagger.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = dagger.GetComponent<BoxCollider>();
            if (collider == null) collider = dagger.gameObject.AddComponent<BoxCollider>();
            collider.center = localBounds.center;
            collider.size = localBounds.size;
            collider.isTrigger = true;
        }

        private void LateUpdate()
        {
            if (animator == null || dagger == null || rightHand == null) return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            int nextCycle = Mathf.FloorToInt(state.normalizedTime);
            float time = (state.normalizedTime - nextCycle) * clipLength;
            if (nextCycle != cycle)
            {
                cycle = nextCycle;
                ResetHeld();
            }
            if (released) return;
            PoseHeld(time);
            if (action == PropAction.Throw && time >= releaseTime) Release();
        }

        private void ResetHeld()
        {
            if (body != null) body.isKinematic = true;
            released = false;
            flightElapsed = 0f;
            dagger.SetParent(rightHand, false);
            dagger.localPosition = idleLocalPosition;
            dagger.localRotation = idleLocalRotation;
            dagger.localScale = originalLocalScale;
        }

        private void PoseHeld(float time)
        {
            Vector3 scaledHandle = Vector3.Scale(originalLocalScale, handleCenter);
            Vector3 grip = rightHand.TransformPoint(idleLocalPosition + idleLocalRotation * scaledHandle);
            Quaternion original = rightHand.rotation * idleLocalRotation;
            Vector3 aimedDirection = DirectionAtElevation(bladeElevation);
            Vector3 restingDirection = DirectionAtElevation(RestoredIdleBladeElevationDegrees);
            Quaternion aimed = AlignBlade(original, aimedDirection);
            Quaternion resting = AlignBlade(original, restingDirection);
            Quaternion rotation;
            if (action == PropAction.Idle)
            {
                rotation = resting;
            }
            else if (action == PropAction.Cancel)
            {
                float returnWeight = Mathf.SmoothStep(0f, 1f, time / clipLength);
                rotation = Quaternion.Slerp(aimed, resting, returnWeight);
            }
            else if (action == PropAction.Throw)
            {
                Vector3 trajectoryDirection = DirectionAtThrowElevation();
                Quaternion trajectoryRotation = AlignBlade(original, trajectoryDirection);
                float trajectoryWeight = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(releaseTime - ThrowTrajectoryBlendSeconds, releaseTime, time));
                rotation = Quaternion.Slerp(aimed, trajectoryRotation, trajectoryWeight);
            }
            else
            {
                rotation = aimed;
            }
            dagger.rotation = rotation;
            dagger.position += grip - dagger.TransformPoint(handleCenter);
        }

        private Vector3 DirectionAtElevation(float elevationDegrees)
        {
            return transform.forward * Mathf.Cos(elevationDegrees * Mathf.Deg2Rad) +
                   transform.up * Mathf.Sin(elevationDegrees * Mathf.Deg2Rad);
        }

        private Quaternion AlignBlade(Quaternion original, Vector3 direction)
        {
            return Quaternion.FromToRotation(original * bladeAxis, direction.normalized) * original;
        }

        private bool ValidReleaseDirection(Vector3 velocity)
        {
            return velocity.sqrMagnitude > 0.01f &&
                   Vector3.Dot(velocity.normalized, transform.forward) > 0.1f;
        }

        private Vector3 DirectionAtThrowElevation()
        {
            Vector3 horizontalDirection = Vector3.ProjectOnPlane(
                transform.TransformDirection(throwDirectionLocal),
                transform.up);
            if (!ValidReleaseDirection(horizontalDirection))
            {
                horizontalDirection = transform.forward;
            }

            horizontalDirection.Normalize();
            float elevationRadians = throwElevation * Mathf.Deg2Rad;
            return (horizontalDirection * Mathf.Cos(elevationRadians) +
                    transform.up * Mathf.Sin(elevationRadians)).normalized;
        }

        private void Release()
        {
            released = true;
            Vector3 heldGrip = dagger.TransformPoint(handleCenter);
            Vector3 releaseDirection = DirectionAtThrowElevation();
            flightRotation = Quaternion.FromToRotation(
                dagger.TransformDirection(bladeAxis),
                releaseDirection) * dagger.rotation;
            dagger.rotation = flightRotation;
            dagger.position += heldGrip - dagger.TransformPoint(handleCenter);
            releasePosition = dagger.position;
            flightUp = transform.up;
            float bottom = float.PositiveInfinity;
            foreach (var renderer in dagger.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            bottom = Mathf.Min(bottom, Vector3.Dot(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z)) - transform.position, flightUp));
            }
            const float gravity = 9.81f;
            float forwardFraction = Mathf.Max(0.1f, Vector3.Dot(releaseDirection, transform.forward));
            float verticalFraction = Vector3.Dot(releaseDirection, flightUp);
            float heightAtTarget = Mathf.Max(
                0.05f,
                Mathf.Max(0f, bottom) + verticalFraction * throwDistance / forwardFraction);
            float launchSpeed = throwDistance / forwardFraction *
                                Mathf.Sqrt(gravity / (2f * heightAtTarget));
            flightDuration = throwDistance / (launchSpeed * forwardFraction);
            launchVelocity = releaseDirection * launchSpeed;
            dagger.SetParent(null, true);
            body.position = releasePosition;
            body.rotation = flightRotation;
            body.isKinematic = false;
            body.linearVelocity = launchVelocity;
            body.angularVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (!released || body == null || body.isKinematic) return;
            // The analytic Stick-style parabola supplies targets; Rigidbody owns all in-flight movement.
            flightElapsed = Mathf.Min(flightElapsed + Time.fixedDeltaTime, flightDuration);
            Vector3 next = releasePosition + launchVelocity * flightElapsed - flightUp * (0.5f * 9.81f * flightElapsed * flightElapsed);
            body.linearVelocity = (next - body.position) / Time.fixedDeltaTime;
            Vector3 tangentVelocity = FlightVelocity;
            if (tangentVelocity.sqrMagnitude > 0.0001f)
            {
                flightRotation = AlignBlade(flightRotation, tangentVelocity);
            }
            body.angularVelocity = Vector3.zero;
            body.MoveRotation(flightRotation);
            if (flightElapsed >= flightDuration)
            {
                body.isKinematic = true;
                body.MovePosition(next);
            }
        }
    }
}
