using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public Vector3 GetRespawnPosition()
    {
        return respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up * 0.5f;
    }

    public Quaternion GetRespawnRotation()
    {
        return respawnPoint != null ? respawnPoint.rotation : transform.rotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        if (player != null)
        {
            player.SetCheckpointServer(GetRespawnPosition(), GetRespawnRotation());
        }
    }
}