using Unity.Netcode;
using UnityEngine;

public class PlayerLobby : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new(false);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleReady()
    {
        if (!IsOwner) return;
        SetReadyServerRpc(!IsReady.Value);
    }

    // Petición al servidor para cambiar la variable de forma autoritativa
    [ServerRpc]
    private void SetReadyServerRpc(bool readyState)
    {
        IsReady.Value = readyState;
    }
}
