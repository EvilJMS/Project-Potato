using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisappearPlatform : NetworkBehaviour
{
    private Renderer platRenderer;
    private Collider platCollider;
    private Color originalColor;
    private bool isTriggered = false;

    private void Awake()
    {
        platRenderer = GetComponent<Renderer>();
        platCollider = GetComponent<Collider>();
        originalColor = platRenderer.material.color;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Como el servidor controla el movimiento, solo él detecta y valida la colisión
        if (!IsServer || isTriggered) return;

        if (collision.gameObject.TryGetComponent<PlayerController>(out _))
        {
            isTriggered = true;
            // Avisa al Host y a todos los Clientes conectados
            ActivatePlatformClientRpc();
        }
    }

    [ClientRpc]
    private void ActivatePlatformClientRpc()
    {
        StartCoroutine(DelayedAction());
    }

    private IEnumerator DelayedAction()
    {
        // 1. Cambia a rojo
        platRenderer.material.color = Color.red;
        yield return new WaitForSeconds(2f);

        // 2. Desactiva Renderer y Collider (NO SetActive(false), para no matar la corrutina)
        platRenderer.enabled = false;
        platCollider.enabled = false;

        // 3. Si la escena actual es Lobby, reaparece tras 3 segundos
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            yield return new WaitForSeconds(3f);
            platRenderer.material.color = originalColor;
            platRenderer.enabled = true;
            platCollider.enabled = true;
            isTriggered = false;
        }
    }
}
