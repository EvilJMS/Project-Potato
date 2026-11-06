using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkGrabbable : NetworkBehaviour
{
    [HideInInspector]
    public NetworkVariable<bool> isGrabbed = new NetworkVariable<bool>(false);

    private Rigidbody rb;
    private Collider col;
    private NetworkTransform netTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        netTransform = GetComponent<NetworkTransform>();
    }

    public override void OnNetworkSpawn()
    {
        isGrabbed.OnValueChanged += OnGrabbedStateChanged;
        UpdateGrabState(isGrabbed.Value);
    }

    public override void OnNetworkDespawn()
    {
        isGrabbed.OnValueChanged -= OnGrabbedStateChanged;

        // Si el jugador se desconecta con el objeto, el servidor lo recupera
        if (IsServer && isGrabbed.Value)
        {
            var netObj = GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.TryRemoveParent();
            }

            isGrabbed.Value = false;

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }

            if (col != null)
            {
                col.enabled = true;
            }

            if (netTransform != null)
            {
                netTransform.enabled = true;
            }
        }
    }

    private void OnGrabbedStateChanged(bool oldValue, bool newValue)
    {
        UpdateGrabState(newValue);
    }

    private void UpdateGrabState(bool grabbed)
    {
        if (rb != null)
        {
            rb.isKinematic = grabbed;
        }

        if (col != null)
        {
            col.enabled = !grabbed;
        }

        // CLAVE: Desactivamos el NetworkTransform mientras está cogido. 
        // Así evitamos que interfiera y permitimos que rote fluidamente con el jugador en la red.
        if (netTransform != null)
        {
            netTransform.enabled = !grabbed;
        }
    }
}