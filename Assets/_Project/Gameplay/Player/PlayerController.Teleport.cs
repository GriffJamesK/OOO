using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay
{
    // Moving the player through a paired threshold without losing momentum. A threshold pair is a
    // rigid map from one opening to the other, so everything that has a direction (velocity, the
    // walking direction, the body's facing) is carried through with that map's rotation. The body
    // stays upright: a floor hatch that comes out of a wall turns the fall into a forward launch.
    public sealed partial class PlayerController
    {
        /// <summary>
        /// Moves the player through a threshold pair. <paramref name="map"/> is the rigid transform
        /// from the entry opening to the exit opening (ThresholdMath.PairMap). The capsule centre is
        /// mapped exactly; velocity is rotated and clamped to <paramref name="maxExitSpeed"/>; chain
        /// and slide state carry on; a wall run ends (its wall is behind you now). Publishes
        /// PlayerTeleported so the camera re-aims by the same rotation in the same frame.
        /// </summary>
        public void TeleportThrough(Matrix4x4 map, float maxExitSpeed)
        {
            Vector3 oldPosition = transform.position;
            Vector3 centreOffset = transform.rotation * controller.center;
            Vector3 newCentre = map.MultiplyPoint3x4(oldPosition + centreOffset);

            // Facing: the mapped forward, flattened. Looking straight along the map's axis (a floor
            // hatch) flattens to nothing, so fall back to the mapped up vector.
            Vector3 facing = Vector3.ProjectOnPlane(map.MultiplyVector(transform.forward), Vector3.up);
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.ProjectOnPlane(map.MultiplyVector(Vector3.up), Vector3.up);
            Quaternion newRotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : transform.rotation;
            Vector3 newPosition = newCentre - newRotation * controller.center;

            Vector3 velocity = Vector3.ClampMagnitude(map.MultiplyVector(horizontal + Vector3.up * verticalVelocity), maxExitSpeed);
            bool keepsUp = Vector3.Dot(map.MultiplyVector(Vector3.up).normalized, Vector3.up) > 0.99f;

            controller.enabled = false; // a CharacterController overrides direct transform writes
            transform.SetPositionAndRotation(newPosition, newRotation);
            controller.enabled = true;
            Physics.SyncTransforms();

            horizontal = new Vector3(velocity.x, 0f, velocity.z);
            verticalVelocity = velocity.y;
            HorizontalSpeed = horizontal.magnitude;
            Vector3 walk = Vector3.ProjectOnPlane(map.MultiplyVector(moveDirection), Vector3.up);
            moveDirection = walk.sqrMagnitude > 0.0001f ? walk.normalized : Vector3.zero;

            EndWallRun();
            ResetWallRun();
            surfing = false;
            surfTimer = 0f;
            surfNormal = Vector3.zero;
            contactCount = 0;
            bestUpNormal = -1f;
            if (!keepsUp)
            {
                // Came out of a tilted opening: airborne, with the speed treated as carried momentum.
                onWalkableGround = false;
                wasGrounded = false;
                carryingMomentum = true;
            }

            EventBus.Publish(new PlayerTeleported(map, newPosition - oldPosition));
        }
    }
}
