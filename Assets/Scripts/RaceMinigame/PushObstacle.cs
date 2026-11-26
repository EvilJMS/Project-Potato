using Unity.Netcode;
using UnityEngine;

public class PushObstacle : NetworkBehaviour
{
    [SerializeField] private float pushForce = 12f;
    [SerializeField] private float upwardLift = 3.5f;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null)
        {
            player = collision.gameObject.GetComponentInParent<PlayerController>();
        }

        if (player != null && player.TryGetComponent<Rigidbody>(out Rigidbody playerRb))
        {
            Vector3 pushDir = (player.transform.position - transform.position).normalized;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.01f) pushDir = -transform.forward;

            Vector3 impulse = pushDir.normalized * pushForce + Vector3.up * upwardLift;
            playerRb.AddForce(impulse, ForceMode.Impulse);
        }
    }
}