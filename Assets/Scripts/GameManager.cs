using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    
    [SerializeField] private TMP_Text playerCountText;
    public int minPlayers = 2;
    public Button readyButton;
    public Button startButton;

    // NetworkVariable automatically syncs from Server to all Clients
    private NetworkVariable<int> connectedPlayersCount = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        // 1. Update UI immediately when this object spawns (handles late joiners)
        UpdateUIText(0, connectedPlayersCount.Value);

        // 2. Subscribe to value changes on all clients
        connectedPlayersCount.OnValueChanged += UpdateUIText;

        // 3. Only the Server/Host listens to ConnectedClients
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;

            // Set initial count
            connectedPlayersCount.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }

        updateButtons();
    }

    public void Update()
    {
        if (IsServer) {
            if (connectedPlayersCount.Value == minPlayers) {
                //Debug.Log("Jugadores minimos para empezar partida");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        connectedPlayersCount.OnValueChanged -= UpdateUIText;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        // Note: During OnClientDisconnectCallback, the disconnecting client is still 
        // in ConnectedClients until the callback finishes.
        int count = NetworkManager.Singleton.ConnectedClients.Count;

        // Check if this callback was triggered by a disconnect
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId) ||
            NetworkManager.Singleton.ShutdownInProgress)
        {
            connectedPlayersCount.Value = count;
        }
        else
        {
            // Adjust count if the client is actively disconnecting
            connectedPlayersCount.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        }

        updateButtons();

        
    }

    private void updateButtons()
    {
        if (connectedPlayersCount.Value < minPlayers)
        {
            startButton.interactable = false;
        }
        else
        {
            startButton.interactable = true;
        }
        startButton.GetComponentInChildren<TMP_Text>().text = "Start Game(" + connectedPlayersCount.Value + "/" + minPlayers + ")";
        Debug.Log("Start Game(" + connectedPlayersCount.Value + "/" + minPlayers + ")");
    }

    private void UpdateUIText(int previousValue, int newValue)
    {
        playerCountText.text = $"Connected Players: {newValue}";
    }
}
