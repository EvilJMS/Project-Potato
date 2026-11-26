using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HexagoniaMode : NetworkBehaviour
{
    public static HexagoniaMode Instance { get; private set; }

    [Header("Configuración de Partida")]
    [SerializeField] private int winnerPoints = 100;
    [SerializeField] private float endRoundDelay = 3f;

    private HexTile[] allTiles;
    private readonly HashSet<ulong> alivePlayers = new HashSet<ulong>();
    private bool isMatchOver;
    private ulong winnerClientId = ulong.MaxValue;

    public bool IsMatchOver => isMatchOver;
    public int AliveCount => alivePlayers.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        IndexAllTiles();
    }

    private void IndexAllTiles()
    {
        HexTile[] found = FindObjectsByType<HexTile>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Orden determinista por posición para que Host y Clientes tengan el mismo índice exacto
        System.Array.Sort(found, (a, b) =>
        {
            int cmpY = a.transform.position.y.CompareTo(b.transform.position.y);
            if (cmpY != 0) return cmpY;
            int cmpX = a.transform.position.x.CompareTo(b.transform.position.x);
            if (cmpX != 0) return cmpX;
            return a.transform.position.z.CompareTo(b.transform.position.z);
        });

        allTiles = found;
        for (int i = 0; i < allTiles.Length; i++)
        {
            allTiles[i].InitializeIndex(i);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            alivePlayers.Clear();
            isMatchOver = false;
            winnerClientId = ulong.MaxValue;

            if (NetworkManager.Singleton != null)
            {
                foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
                {
                    alivePlayers.Add(kvp.Key);
                }

                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedServer;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedServer;
        }

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnNetworkDespawn();
    }

    private void OnClientDisconnectedServer(ulong clientId)
    {
        if (!IsServer || isMatchOver) return;

        if (alivePlayers.Remove(clientId))
        {
            CheckWinConditionServer();
        }
    }

    public void NotifyTileSteppedServer(int tileIndex)
    {
        if (!IsServer || isMatchOver) return;
        if (allTiles == null || tileIndex < 0 || tileIndex >= allTiles.Length) return;

        HexTile tile = allTiles[tileIndex];
        if (tile == null || tile.IsBroken) return;

        TriggerTileBreakClientRpc(tileIndex);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerTileBreakClientRpc(int tileIndex)
    {
        if (allTiles == null || tileIndex < 0 || tileIndex >= allTiles.Length) return;
        allTiles[tileIndex].StartBreakSequenceLocal();
    }

    public void EliminatePlayerServer(PlayerController player)
    {
        if (!IsServer || isMatchOver || player == null) return;

        ulong clientId = player.OwnerClientId;
        if (!alivePlayers.Remove(clientId)) return;

        player.KillByLavaAndRespawnServer(); // Oculta visuales y bloquea movimiento
        CancelInvoke(); // Evita cualquier respawn automático si hubiera alguno pendiente

        CheckWinConditionServer();
    }

    private void CheckWinConditionServer()
    {
        if (!IsServer || isMatchOver) return;

        // Si había más de 1 jugador al empezar y queda 1 (o 0 si caen a la vez)
        if (alivePlayers.Count <= 1)
        {
            isMatchOver = true;

            foreach (ulong lastId in alivePlayers)
            {
                winnerClientId = lastId;
                break;
            }

            if (winnerClientId != ulong.MaxValue && GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(winnerClientId, winnerPoints);
            }

            AnnounceWinnerClientRpc(winnerClientId);
            Invoke(nameof(FinishMinigameServer), endRoundDelay);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AnnounceWinnerClientRpc(ulong winnerId)
    {
        Debug.Log(winnerId == ulong.MaxValue
            ? "¡Empate! Todos cayeron a la lava."
            : $"¡Jugador {winnerId} gana Hexagonia!");
    }

    private void FinishMinigameServer()
    {
        if (!IsServer) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMinigameFinishedServer();
        }
    }
}