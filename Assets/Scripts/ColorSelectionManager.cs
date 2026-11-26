using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ColorSelectionManager : NetworkBehaviour
{
    public static ColorSelectionManager Instance { get; private set; }

    [Header("Assign your color buttons in order")]
    [SerializeField] private Button[] colorButtons;
    public Color[] colors;

    private NetworkList<ulong> colorOwners;
    private const ulong UNCLAIMED = ulong.MaxValue;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        colorOwners = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (colors == null || colors.Length == 0)
        {
            Debug.LogWarning("[ColorSelectionManager] El array 'colors' está vacío en el Inspector.");
        }

        if (colorButtons == null || colorButtons.Length == 0)
        {
            Debug.LogWarning("[ColorSelectionManager] El array 'colorButtons' está vacío en el Inspector.");
        }

        if (IsServer)
        {
            colorOwners.Clear();
            int count = Mathf.Max(
                colorButtons != null ? colorButtons.Length : 0,
                colors != null ? colors.Length : 0
            );

            for (int i = 0; i < count; i++)
            {
                colorOwners.Add(UNCLAIMED);
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            // Si ya había jugadores spawneados antes que este manager, asignarles color ahora
            PlayerController[] existingPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (PlayerController player in existingPlayers)
            {
                Color assigned = AssignFirstAvailableColorServer(player.OwnerClientId);
                player.SetColorFromServer(assigned);
            }
        }

        if (colorButtons != null)
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                if (colorButtons[i] == null) continue;
                int colorIndex = i;
                colorButtons[i].onClick.RemoveAllListeners();
                colorButtons[i].onClick.AddListener(() => OnColorButtonClicked(colorIndex));
            }
        }

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

        if (Instance == this) Instance = null;
        base.OnNetworkDespawn();
    }

    public Color AssignFirstAvailableColorServer(ulong clientId)
    {
        if (!IsServer || colors == null || colors.Length == 0)
            return Color.white;

        for (int i = 0; i < colorOwners.Count && i < colors.Length; i++)
        {
            if (colorOwners[i] == clientId)
                return colors[i];
        }

        for (int i = 0; i < colorOwners.Count && i < colors.Length; i++)
        {
            if (colorOwners[i] == UNCLAIMED)
            {
                colorOwners[i] = clientId;
                return colors[i];
            }
        }

        return colors[0];
    }

    public void ReleaseColorServer(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < colorOwners.Count; i++)
        {
            if (colorOwners[i] == clientId)
            {
                colorOwners[i] = UNCLAIMED;
            }
        }
    }

    private void OnColorButtonClicked(int colorIndex)
    {
        Debug.Log($"[ColorSelectionManager] Click en botón de color {colorIndex}");
        RequestColorServerRpc(colorIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestColorServerRpc(int colorIndex, RpcParams rpcParams = default)
    {
        if (colorIndex < 0 || colorIndex >= colorOwners.Count || colors == null || colorIndex >= colors.Length)
            return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        if (colorOwners[colorIndex] != UNCLAIMED) return;

        for (int i = 0; i < colorOwners.Count; i++)
        {
            if (colorOwners[i] == senderId)
            {
                colorOwners[i] = UNCLAIMED;
            }
        }

        colorOwners[colorIndex] = senderId;

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            if (player.OwnerClientId == senderId)
            {
                player.SetColorFromServer(colors[colorIndex]);
                break;
            }
        }
    }

    private void OnColorListChanged(NetworkListEvent<ulong> changeEvent)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (colorButtons == null) return;

        for (int i = 0; i < colorButtons.Length; i++)
        {
            if (colorButtons[i] == null) continue;

            if (i < colorOwners.Count)
            {
                colorButtons[i].interactable = (colorOwners[i] == UNCLAIMED);
            }
            else
            {
                colorButtons[i].interactable = true;
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        ReleaseColorServer(clientId);
    }
}