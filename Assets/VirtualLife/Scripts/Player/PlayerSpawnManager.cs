using UnityEngine;
using VirtualLife.CameraRig;
using VirtualLife.UI;
using VirtualLife.World;

namespace VirtualLife.Player
{
    /// <summary>
    /// Scene-level spawner (lives in CommunityOne, not DontDestroyOnLoad, so nothing outlives the
    /// scene). Spawns exactly one player, wires the camera and mobile controls, and handles respawns.
    /// </summary>
    public sealed class PlayerSpawnManager : MonoBehaviour
    {
        [SerializeField] private PlayerController playerPrefab;
        [SerializeField] private SpawnPoint spawnPoint;
        [SerializeField] private ThirdPersonCameraController cameraRig;
        [SerializeField] private MobileControlsUI mobileControls; // optional

        private PlayerController player;

        private void Start() => Spawn();

        private void OnDestroy()
        {
            if (player != null) player.FellOutOfWorld -= Respawn;
        }

        private void Spawn()
        {
            if (player != null) return;
            if (spawnPoint == null) { Debug.LogError("[Spawn] No SpawnPoint assigned; using this object's transform.", this); }
            if (cameraRig == null) { Debug.LogError("[Spawn] No camera rig assigned.", this); return; }

            // Adopt an existing player instead of ever creating a duplicate.
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                if (playerPrefab == null) { Debug.LogError("[Spawn] No player prefab assigned.", this); return; }
                player = Instantiate(playerPrefab);
                player.name = "Player";
            }

            Place();
            cameraRig.SetTarget(player.CameraTarget, player.InputSource);
            cameraRig.Snap(SpawnTransform.eulerAngles.y);
            player.SetCameraTransform(cameraRig.transform);
            if (mobileControls != null) mobileControls.Bind(player.VirtualSink);
            player.FellOutOfWorld += Respawn;
        }

        private Transform SpawnTransform => spawnPoint != null ? spawnPoint.transform : transform;

        private void Place()
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; // required to teleport a CharacterController
            Transform t = SpawnTransform;
            player.transform.SetPositionAndRotation(t.position + Vector3.up * 0.05f, Quaternion.Euler(0f, t.eulerAngles.y, 0f));
            if (cc != null) cc.enabled = true;
            player.Movement.ResetMotion();
        }

        private void Respawn()
        {
            if (player == null) return;
            Place();
            cameraRig.Snap(SpawnTransform.eulerAngles.y);
        }
    }
}
