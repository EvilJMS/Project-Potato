using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : GameModeBase
{
    public static LobbyManager Instance { get; private set; }

    [Header("UI del Lobby")]
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startButton;
    [SerializeField] private int minPlayers = 2;

    private readonly NetworkVariable<int> connectedPlayersCount = new(0);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    protected override void OnModeStart()
    {
        UpdateUIText(0, connectedPlayersCount.Value);
        connectedPlayersCount.OnValueChanged += UpdateUIText;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
            connectedPlayersCount.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }

        UpdateButtons();
    }

    protected override void OnModeEnd()
    {
        connectedPlayersCount.OnValueChanged -= UpdateUIText;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        if (!IsServer || NetworkManager.Singleton == null) return;

        int count = NetworkManager.Singleton.ConnectedClients.Count;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId) ||
            NetworkManager.Singleton.ShutdownInProgress)
        {
            connectedPlayersCount.Value = count;
        }
        else
        {
            connectedPlayersCount.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        }

        UpdateButtons();
    }

    private void UpdateUIText(int previousValue, int newValue)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Connected Players: {newValue}";
        }
    }

    public void UpdateButtons()
    {
        if (!IsServer || NetworkManager.Singleton == null)
        {
            if (startButton != null)
            {
                startButton.interactable = false;
                TMP_Text btnText = startButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = "Waiting for Host...";
            }
            return;
        }

        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;
        int readyCount = 0;
        bool allReady = true;

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        if (players.Length < minPlayers || players.Length < totalPlayers)
        {
            allReady = false;
        }
        else
        {
            foreach (PlayerController player in players)
            {
                if (player.isReady.Value)
                {
                    readyCount++;
                }
                else
                {
                    allReady = false;
                }
            }
        }

        bool canStart = allReady && readyCount >= minPlayers && totalPlayers >= minPlayers;

        if (startButton != null)
        {
            startButton.interactable = canStart;
            TMP_Text btnText = startButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = $"Start Game ({readyCount}/{totalPlayers} Ready)";
        }
    }

    public void OnReadyButtonClicked()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            if (player.IsOwner)
            {
                player.ToggleReady();
                break;
            }
        }
    }

    public void UpdateLocalReadyUI(bool isReady)
    {
        if (readyButton != null)
        {
            TMP_Text btnText = readyButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
            {
                btnText.text = isReady ? "Cancel Ready" : "Ready!";
            }

            Image btnImage = readyButton.GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = isReady ? Color.green : Color.white;
            }
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;
        FinishModeServer();
    }

    public override void FinishModeServer()
    {
        if (!IsServer) return;

        if (Game != null)
        {
            Game.LoadRandomMinigame();
        }
    }
}