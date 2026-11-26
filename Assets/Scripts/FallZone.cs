using UnityEngine;

public class FallZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject);
        other.gameObject.transform.position = new Vector3(0, 0, 0);
    }
}
