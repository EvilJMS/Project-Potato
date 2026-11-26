using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishLineTrigger : MonoBehaviour
{
    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor valida la meta para evitar trampas o duplicados
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        if (player != null && RaceMinigameMode.Instance != null)
        {
            RaceMinigameMode.Instance.PlayerCrossedFinishServer(player.OwnerClientId);
        }
    }
}