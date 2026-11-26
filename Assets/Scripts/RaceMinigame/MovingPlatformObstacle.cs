using Unity.Netcode;
using UnityEngine;

public class MovingPlatformObstacle : NetworkBehaviour
{
    [SerializeField] private Vector3 moveOffset = new Vector3(3.5f, 0f, 0f);
    [SerializeField] private float speed = 2f;

    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.localPosition;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        float pingPong = Mathf.PingPong(Time.time * speed, 1f);
        transform.localPosition = startPos + moveOffset * pingPong;
    }
}