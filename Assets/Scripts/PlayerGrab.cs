using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerGrab : NetworkBehaviour
{
    public float grabRange = 3f;
    public NetworkGrabbable currentTarget;
    public NetworkGrabbable heldObject;
    public GameObject canvas;
    public Transform heldPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (!IsOwner) return;
        FindGrabbable();
        if (canvas != null)
        {
            //Debug.Log("entra aqui");
            bool showPrompt = currentTarget != null && heldObject==null && !currentTarget.isGrabbed.Value;
            
            //Debug.Log(showPrompt);
            canvas.SetActive(showPrompt);
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            GrabObjectServerRpc(currentTarget.GetComponent<NetworkObject>());
        }

        if(Keyboard.current.lKey.wasPressedThisFrame)
        {
            LaunchObjectServerRpc();
        }
    }

    private void FindGrabbable()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, grabRange);
        float minDis = float.MaxValue;
        NetworkGrabbable closest = null;

        foreach (var col in colliders)
        {
            var grabbable = col.GetComponent<NetworkGrabbable>();
            if (grabbable != null && !grabbable.isGrabbed.Value)
            {
                float dist = Vector3.Distance(transform.position, grabbable.transform.position);
                if (dist < minDis) {
                    minDis = dist;
                    closest = grabbable;
                
                }
            }
        }
        currentTarget = closest;
    }

    [ServerRpc]
    private void GrabObjectServerRpc(NetworkObjectReference target) {
        if (target.TryGet(out NetworkObject netObj)) {
            var grabbable = netObj.GetComponent<NetworkGrabbable>();
            if (grabbable!=null && !grabbable.isGrabbed.Value) {
                grabbable.isGrabbed.Value = true;
                NetworkObject playerNet = GetComponent<NetworkObject>();
                bool parent = netObj.TrySetParent(playerNet, worldPositionStays:false);
                if (!parent) {

                }
                //Debug.Log(parent);
                netObj.transform.localPosition = heldPoint.localPosition;
                netObj.transform.localRotation = Quaternion.identity;
                heldObject = grabbable;
            }
        }
    }

    [ServerRpc]
    private void LaunchObjectServerRpc() {
        var netObj = heldObject.NetworkObject;

        // 1. Desemparentar en red (el objeto se libera del jugador)
        netObj.TryRemoveParent();
        heldObject.isGrabbed.Value = false;

        // 2. Obtener el Rigidbody y aplicar la regla de físicas y limpieza de inercias
        Rigidbody rb = netObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;                 // Reactivamos la gravedad y físicas[cite: 1]
            rb.linearVelocity = Vector3.zero;       // Reseteo de inercias acumuladas[cite: 1]

            // 3. Lanzar hacia adelante usando un impulso físico
            Vector3 throwDirection = transform.forward;
            rb.AddForce(throwDirection * 10f, ForceMode.Impulse); 
        }

        // Limpiamos la referencia local en el servidor
        heldObject = null;

    }

    

    public override void OnNetworkSpawn()
    {
        // Si este avatar NO es el tuyo (es el de otro cliente en la red)
        if (!IsOwner)
        {
            // Apagamos su canvas de la "E" para que no sea visible en los demás
            if (canvas != null)
            {
                canvas.SetActive(false);
            }

            // Desactivamos este script por completo en los avatares ajenos 
            // (así ahorramos recursos y evitamos que otros ejecuten físicas que no les corresponden)
            enabled = false;
            return;
        }
    }
}
