using Unity.Netcode;
using UnityEngine;

public class RotatingObstacle : NetworkBehaviour
{
    [SerializeField] private Vector3 rotationSpeedEuler = new Vector3(0f, 90f, 0f);

    public void SetRotationSpeed(Vector3 speedEuler)
    {
        rotationSpeedEuler = speedEuler;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;
        transform.Rotate(rotationSpeedEuler * Time.fixedDeltaTime, Space.Self);
    }
}