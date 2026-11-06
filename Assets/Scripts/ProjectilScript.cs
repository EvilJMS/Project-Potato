using Unity.Netcode;
using UnityEngine;

public class ProjectilScript : NetworkBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float lifeTime = 3f;

    // Guardamos el ID del jugador que disparó para evitar que se autolesione
    public ulong ShooterClientId { get; set; } = 9999;

    private void Start()
    {
        if (IsServer)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        transform.Translate(Vector3.forward * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Comprobamos si ha chocado con un jugador
        PlayerController hitPlayer = other.GetComponent<PlayerController>();
        if (hitPlayer != null)
        {
            // SI ES EL MISMO JUGADOR QUE DISPARÓ, IGNORAMOS LA COLISIÓN
            NetworkObject hitNetObj = other.GetComponent<NetworkObject>();
            if (hitNetObj != null && hitNetObj.OwnerClientId == ShooterClientId)
            {
                return; // Sale del método sin hacer nada
            }

            // Si es otro jugador, le cambiamos el color
            hitPlayer.ChangeColorServerRpc(new Color(Random.value, Random.value, Random.value, 1f));
        }

        // El proyectil desaparece al chocar con cualquier otra cosa válida (obstáculos o jugadores)
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}