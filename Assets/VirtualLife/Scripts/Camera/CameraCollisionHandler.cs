using UnityEngine;

namespace VirtualLife.CameraRig
{
    /// <summary>Pulls the camera in front of walls using one non-allocating sphere cast per frame.</summary>
    public sealed class CameraCollisionHandler : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;

        private static readonly RaycastHit[] Hits = new RaycastHit[8];
        private Transform ignoreRoot;

        /// <summary>Colliders under this root (the player) are ignored.</summary>
        public void SetIgnoreRoot(Transform root) => ignoreRoot = root;

        /// <summary>Returns the distance along pivot->desired that is free of obstacles.</summary>
        public float ResolveDistance(Vector3 pivot, Vector3 desiredPosition)
        {
            Vector3 toDesired = desiredPosition - pivot;
            float desired = toDesired.magnitude;
            if (config == null || desired < 0.0001f) return desired;

            int count = Physics.SphereCastNonAlloc(pivot, config.CollisionRadius, toDesired / desired, Hits,
                                                   desired, config.CollisionMask, QueryTriggerInteraction.Ignore);
            float best = desired;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (ignoreRoot != null && hit.collider.transform.IsChildOf(ignoreRoot)) continue;
                if (hit.distance < best) best = hit.distance;
            }
            return best >= desired ? desired : Mathf.Max(config.MinDistance, best - config.CollisionBuffer);
        }
    }
}
