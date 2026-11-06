using Unity.Netcode;
using UnityEngine;

public class BumperNetwork : NetworkBehaviour
{
    [Header("Configuración del Bumper Suave")]
    [SerializeField] private float bounceForce = 12f; // Fuerza equilibrada
    [SerializeField] private float upwardForce = 4f;  // Elevación controlada

    [Header("Efectos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bounceClip;

    

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            // 1. Calculamos la dirección del empuje desde el punto de impacto real (más preciso y natural)
            Vector3 pushDirection = (collision.transform.position - transform.position).normalized;
            pushDirection.y = 0.2f; // Pequeña curvatura hacia arriba para que no sea un plano totalmente horizontal
            pushDirection.Normalize();

            // 2. SUAVIZADO: En lugar de poner velocidad a cero (lo que causa el efecto TP),
            // conservamos una fracción de la velocidad actual (por ejemplo, el 20%) para que la transición sea fluida.
            playerRb.linearVelocity = new Vector3(
                playerRb.linearVelocity.x * 0.2f,
                Mathf.Max(0f, playerRb.linearVelocity.y * 0.2f), // Evita arrastrar caídas muy pesadas
                playerRb.linearVelocity.z * 0.2f
            );

            // 3. Vector final de fuerza combinado
            Vector3 finalForce = (pushDirection * bounceForce) + (Vector3.up * upwardForce);

            // 4. Usamos ForceMode.VelocityChange en lugar de Impulse. 
            // VelocityChange ignora la masa del objeto y aplica un cambio de velocidad inmediato pero progresivo para la física.
            playerRb.AddForce(finalForce, ForceMode.VelocityChange);

            // 5. Sonido
            PlayBounceSoundClientRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayBounceSoundClientRpc()
    {
        if (audioSource != null && bounceClip != null)
        {
            audioSource.PlayOneShot(bounceClip);
        }
    }
}