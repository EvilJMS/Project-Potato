using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CaptureFlagMode : NetworkBehaviour
{
    public static CaptureFlagMode Instance { get; private set; }

    [Header("Configuración de Partida")]
    [SerializeField] private float matchDurationSeconds = 600f;
    [SerializeField] private int winScore = 100;
    [SerializeField] private int minTeams = 2;
    [SerializeField] private int maxTeams = 3;
    [SerializeField] private float respawnDelaySeconds = 3f;

    [Header("Puntos Globales (Leaderboard)")]
    [SerializeField] private int winMatchPoints = 25;

    [Header("Overtime Estilo 2CP")]
    [SerializeField] private float overtimeGracePeriod = 4f;

    [Header("Banderas de la Escena")]
    [SerializeField] private List<FlagEntity> flags = new();

    public bool IsMatchOver => matchEnded.Value;
    public bool IsOvertime => isOvertime.Value;
    public float RemainingTime => remainingTime.Value;
    public int WinningTeam => winningTeam.Value;

    private readonly NetworkVariable<float> remainingTime = new(
        600f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<bool> isOvertime = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<bool> matchEnded = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> winningTeam = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkList<int> teamScores = new();

    private readonly Dictionary<ulong, int> playerTeamMap = new();
    private int activeTeamCount = 3;
    private float overtimeDrainTimer;
    private bool finishingRoutineStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;

        remainingTime.Value = matchDurationSeconds;
        isOvertime.Value = false;
        matchEnded.Value = false;
        winningTeam.Value = -1;
        finishingRoutineStarted = false;

        int playerCount = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
        activeTeamCount = Mathf.Clamp(playerCount <= 3 ? minTeams : maxTeams, minTeams, maxTeams);

        teamScores.Clear();
        for (int i = 0; i < 3; i++)
        {
            teamScores.Add(0);
        }

        AssignTeamsToConnectedPlayers();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedServer;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedServer;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedServer;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedServer;
        }

        if (Instance == this)
        {
            Instance = null;
        }

        base.OnNetworkDespawn();
    }

    private void OnClientConnectedServer(ulong clientId)
    {
        if (!IsServer || matchEnded.Value) return;
        AssignTeamToPlayerServer(clientId);
    }

    private void OnClientDisconnectedServer(ulong clientId)
    {
        if (!IsServer) return;
        playerTeamMap.Remove(clientId);
    }

    private void AssignTeamsToConnectedPlayers()
    {
        if (NetworkManager.Singleton == null) return;

        int idx = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            playerTeamMap[clientId] = idx % activeTeamCount;
            idx++;
        }
    }

    private void AssignTeamToPlayerServer(ulong clientId)
    {
        if (playerTeamMap.ContainsKey(clientId)) return;

        int[] counts = new int[activeTeamCount];
        foreach (int t in playerTeamMap.Values)
        {
            if (t >= 0 && t < activeTeamCount) counts[t]++;
        }

        int bestTeam = 0;
        for (int i = 1; i < activeTeamCount; i++)
        {
            if (counts[i] < counts[bestTeam]) bestTeam = i;
        }

        playerTeamMap[clientId] = bestTeam;
    }

    public int GetPlayerTeam(ulong clientId)
    {
        if (playerTeamMap.TryGetValue(clientId, out int team)) return team;
        return (int)(clientId % (ulong)Mathf.Max(1, activeTeamCount));
    }

    public int GetTeamScore(int teamId)
    {
        if (teamId >= 0 && teamId < teamScores.Count) return teamScores[teamId];
        return 0;
    }

    public void AddTeamScoreServer(int teamId, int delta)
    {
        if (!IsServer || matchEnded.Value) return;
        if (teamId < 0 || teamId >= teamScores.Count) return;

        int next = Mathf.Min(winScore, teamScores[teamId] + delta);
        teamScores[teamId] = next;

        if (next >= winScore)
        {
            DeclareWinnerServer(teamId);
        }
    }

    private void Update()
    {
        if (!IsServer || matchEnded.Value) return;

        if (!isOvertime.Value)
        {
            remainingTime.Value = Mathf.Max(0f, remainingTime.Value - Time.deltaTime);

            if (remainingTime.Value <= 0f)
            {
                if (ShouldEnterOvertime())
                {
                    isOvertime.Value = true;
                    overtimeDrainTimer = overtimeGracePeriod;
                }
                else
                {
                    ResolveTimeExpiredNoOvertime();
                }
            }
        }
        else
        {
            if (AnyObjectiveInProgress())
            {
                overtimeDrainTimer = overtimeGracePeriod;
            }
            else
            {
                overtimeDrainTimer -= Time.deltaTime;
                if (overtimeDrainTimer <= 0f)
                {
                    ResolveTimeExpiredNoOvertime();
                }
            }
        }
    }

    private bool ShouldEnterOvertime()
    {
        if (AnyObjectiveInProgress()) return true;

        int topScore = -1;
        int topCount = 0;
        for (int i = 0; i < activeTeamCount; i++)
        {
            int s = GetTeamScore(i);
            if (s > topScore)
            {
                topScore = s;
                topCount = 1;
            }
            else if (s == topScore)
            {
                topCount++;
            }
        }

        return topCount > 1;
    }

    private bool AnyObjectiveInProgress()
    {
        foreach (FlagEntity flag in flags)
        {
            if (flag != null && flag.IsContestedOrActiveObjective())
            {
                return true;
            }
        }
        return false;
    }

    private void ResolveTimeExpiredNoOvertime()
    {
        int bestTeam = -1;
        int bestScore = -1;

        for (int i = 0; i < activeTeamCount; i++)
        {
            int score = GetTeamScore(i);
            if (score > bestScore)
            {
                bestScore = score;
                bestTeam = i;
            }
        }

        if (bestScore > 0)
        {
            DeclareWinnerServer(bestTeam);
        }
        else
        {
            DeclareWinnerServer(-1);
        }
    }

    public void DeclareWinnerServer(int winnerTeamId)
    {
        if (!IsServer || matchEnded.Value) return;

        winningTeam.Value = winnerTeamId;
        matchEnded.Value = true;

        if (winnerTeamId >= 0)
        {
            AwardPointsToWinningTeamServer(winnerTeamId);
        }

        if (!finishingRoutineStarted)
        {
            finishingRoutineStarted = true;
            StartCoroutine(FinishMinigameRoutine());
        }
    }

    private void AwardPointsToWinningTeamServer(int winnerTeamId)
    {
        if (GameManager.Instance == null || NetworkManager.Singleton == null) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (GetPlayerTeam(clientId) == winnerTeamId)
            {
                GameManager.Instance.AddScore(clientId, winMatchPoints);
            }
        }
    }

    public void SchedulePlayerRespawnServer(PlayerController player)
    {
        if (!IsServer || player == null) return;
        StartCoroutine(RespawnPlayerAfterDelayRoutine(player, respawnDelaySeconds));
    }

    private IEnumerator RespawnPlayerAfterDelayRoutine(PlayerController player, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (player != null && player.IsSpawned)
        {
            player.RespawnFromLavaServer();
        }
    }

    private IEnumerator FinishMinigameRoutine()
    {
        yield return new WaitForSeconds(3.5f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnMinigameFinishedServer();
        }
    }
}