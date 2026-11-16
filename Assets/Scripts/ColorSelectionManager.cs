using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ColorSelectionManager : NetworkBehaviour
{
    [Header("Assign your color buttons in order")]
    [SerializeField] private Button[] colorButtons;


    // Stores the ClientId of the player who picked each color.
    // ulong.MaxValue means the color is free.
    private NetworkList<ulong> colorOwners;

    private const ulong UNCLAIMED = ulong.MaxValue;

    private void Awake()
    {
        colorOwners = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        // Server initializes the list with all colors marked as unclaimed
        if (IsServer)
        {
            colorOwners.Clear();
            for (int i = 0; i < colorButtons.Length; i++)
            {
                colorOwners.Add(UNCLAIMED);
            }

            // Free up a color if a player disconnects
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // Hook up button click events locally
        for (int i = 0; i < colorButtons.Length; i++)
        {
            int colorIndex = i; // Capture index for the lambda
            colorButtons[i].onClick.AddListener(() => OnColorButtonClicked(colorIndex));
        }

        // Listen for changes and refresh UI immediately for late joiners
        colorOwners.OnListChanged += OnColorListChanged;
        RefreshUI();
    }

    public override void OnNetworkDespawn()
    {
        colorOwners.OnListChanged -= OnColorListChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnColorButtonClicked(int colorIndex)
    {
        // Ask the server to claim this color
        RequestColorServerRpc(colorIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestColorServerRpc(int colorIndex, RpcParams rpcParams = default)
    {
        if (colorIndex < 0 || colorIndex >= colorOwners.Count) return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        // Block if another player already owns this color
        if (colorOwners[colorIndex] != UNCLAIMED) return;

        // Release any previous color this player had selected
        for (int i = 0; i < colorOwners.Count; i++)
        {
            if (colorOwners[i] == senderId)
            {
                colorOwners[i] = UNCLAIMED;
            }
        }

        // Assign the new color to this player
        colorOwners[colorIndex] = senderId;
    }

    private void OnColorListChanged(NetworkListEvent<ulong> changeEvent)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        for (int i = 0; i < colorButtons.Length && i < colorOwners.Count; i++)
        {
            ulong ownerId = colorOwners[i];

            // Button is interactable ONLY if no one has claimed it.
            // (If you want the player to be able to click their own selected color, 
            // change this to: ownerId == UNCLAIMED || ownerId == localClientId)
            colorButtons[i].interactable = (ownerId == UNCLAIMED);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        for (int i = 0; i < colorOwners.Count; i++)
        {
            if (colorOwners[i] == clientId)
            {
                colorOwners[i] = UNCLAIMED;
            }
        }
    }
}
