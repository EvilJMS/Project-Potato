using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Escenas del Juego")]
    public string[] minigameScenes;
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = "Lobby";

    [Header("Rondas")]
    public int maxRounds = 3;
    public readonly NetworkVariable<int> currentRound = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public struct PlayerScoreData : INetworkSerializable, IEquatable<PlayerScoreData>
    {
        public ulong clientId;
        public FixedString32Bytes playerName;
        public int score;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref score);
        }

        public bool Equals(PlayerScoreData other)
        {
            return clientId == other.clientId &&
                   score == other.score &&
                   playerName == other.playerName;
        }
    }

    public NetworkList<PlayerScoreData> leaderboard;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            leaderboard = new NetworkList<PlayerScoreData>();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectLocal;
        }

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnServerClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnServerClientDisconnected;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                EnsurePlayerInLeaderboard(client.ClientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectLocal;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnServerClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnected;
        }

        base.OnNetworkDespawn();
    }

    private void OnClientDisconnectLocal(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        if (clientId == 0 || clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Se perdió la conexión con el servidor o el host cerró la partida. Volviendo al menú...");
            ExecuteReturnToMenu();
        }
    }

    private void OnServerClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        EnsurePlayerInLeaderboard(clientId);
    }

    private void OnServerClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        for (int i = 0; i < leaderboard.Count; i++)
        {
            if (leaderboard[i].clientId == clientId)
            {
                leaderboard.RemoveAt(i);
                break;
            }
        }
    }

    private void EnsurePlayerInLeaderboard(ulong clientId)
    {
        for (int i = 0; i < leaderboard.Count; i++)
        {
            if (leaderboard[i].clientId == clientId) return;
        }

        leaderboard.Add(new PlayerScoreData
        {
            clientId = clientId,
            playerName = $"Player {clientId}",
            score = 0
        });
    }

    public void AddScore(ulong clientId, int points)
    {
        if (!IsServer) return;

        for (int i = 0; i < leaderboard.Count; i++)
        {
            if (leaderboard[i].clientId == clientId)
            {
                PlayerScoreData data = leaderboard[i];
                data.score += points;
                leaderboard[i] = data;
                break;
            }
        }
    }

    public void ResetAllReadyStatesServer()
    {
        if (!IsServer) return;

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (PlayerController player in players)
        {
            player.isReady.Value = false;
        }
    }

    public void ReturnToMenuByButton()
    {
        ExecuteReturnToMenu();
    }

    public void ExecuteReturnToMenu()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            if (NetworkManager.Singleton.gameObject != null)
            {
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    public void LoadSceneServer(string sceneName)
    {
        if (!IsServer || NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null)
            return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("El nombre de escena está vacío.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadRandomMinigame()
    {
        if (!IsServer) return;

        if (minigameScenes == null || minigameScenes.Length == 0)
        {
            Debug.LogError("No hay escenas de minijuegos configuradas en minigameScenes.");
            return;
        }

        currentRound.Value += 1;
        int randomIndex = UnityEngine.Random.Range(0, minigameScenes.Length);
        string selectedScene = minigameScenes[randomIndex];

        Debug.Log($"Ronda {currentRound.Value} -> Cargando minijuego: {selectedScene}");
        LoadSceneServer(selectedScene);
    }

    public void OnMinigameFinishedServer()
    {
        if (!IsServer) return;

        if (currentRound.Value >= maxRounds)
        {
            ReturnToLobby();
        }
        else
        {
            LoadRandomMinigame();
        }
    }

    public void ReturnToLobby()
    {
        if (!IsServer) return;

        currentRound.Value = 0;
        ResetAllReadyStatesServer();
        LoadSceneServer(lobbySceneName);
    }
}