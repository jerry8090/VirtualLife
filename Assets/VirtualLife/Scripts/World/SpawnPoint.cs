using UnityEngine;

namespace VirtualLife.World
{
    /// <summary>Marks where the local player spawns. Stage 2 will read this.</summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId = "Default";
        public string SpawnId => spawnId;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}
