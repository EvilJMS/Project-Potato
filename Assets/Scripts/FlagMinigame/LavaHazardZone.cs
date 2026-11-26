using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LavaHazardZone : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDeadOrRespawning) return;

        player.KillByLavaAndRespawnServer();
    }
}