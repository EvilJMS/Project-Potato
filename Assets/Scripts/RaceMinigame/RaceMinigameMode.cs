using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RaceMinigameMode : GameModeBase
{
    public static RaceMinigameMode Instance { get; private set; }

    [Header("Configuración de Carrera")]
    [SerializeField] private float raceDurationSeconds = 60f;
    [SerializeField] private int[] placementPoints = { 30, 20, 10, 5 }; // Puntos según orden de llegada
    [SerializeField] private bool endWhenAllFinish = true;

    private readonly NetworkVariable<float> remainingTime = new(
        60f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly HashSet<ulong> finishedPlayers = new();
    private bool isRaceFinished = false;
    private Coroutine timerRoutine;
    private readonly NetworkVariable<int> finishedCount = new(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    public float RemainingTimeValue => remainingTime.Value;
    public int FinishedCountValue => finishedCount.Value;
    public bool IsRaceFinished => isRaceFinished;

    public bool HasClientFinished(ulong clientId)
    {
        return finishedPlayers.Contains(clientId);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    protected override void OnModeStart()
    {
        finishedPlayers.Clear();
        isRaceFinished = false;

        if (IsServer)
        {
            remainingTime.Value = raceDurationSeconds;
            timerRoutine = StartCoroutine(RaceTimerRoutine());
            finishedCount.Value = 0;
        }
    }

    protected override void OnModeEnd()
    {
        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (Instance == this) Instance = null;
    }

    private IEnumerator RaceTimerRoutine()
    {
        while (remainingTime.Value > 0f && !isRaceFinished)
        {
            yield return new WaitForSeconds(1f);
            remainingTime.Value = Mathf.Max(0f, remainingTime.Value - 1f);
        }

        if (!isRaceFinished)
        {
            FinishModeServer();
        }
    }

    /// <summary>
    /// Llamado en el servidor cuando un jugador cruza la meta.
    /// </summary>
    public void PlayerCrossedFinishServer(ulong clientId)
    {
        if (!IsServer || isRaceFinished) return;

        // Evita contar dos veces al mismo jugador si toca el trigger más de una vez
        if (!finishedPlayers.Add(clientId)) return;

        finishedCount.Value = finishedPlayers.Count;
        int rankIndex = finishedPlayers.Count - 1; // 0 = 1er puesto, 1 = 2do puesto...
        int pointsAwarded = rankIndex < placementPoints.Length
            ? placementPoints[rankIndex]
            : (placementPoints.Length > 0 ? placementPoints[^1] : 0);

        if (Game != null && pointsAwarded > 0)
        {
            Game.AddScore(clientId, pointsAwarded);
        }

        Debug.Log($"[Carrera] Jugador {clientId} cruzó la meta en posición #{rankIndex + 1} (+{pointsAwarded} pts).");

        if (endWhenAllFinish && NetworkManager.Singleton != null)
        {
            int connectedCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            if (connectedCount > 0 && finishedPlayers.Count >= connectedCount)
            {
                FinishModeServer();
            }
        }
    }

    public override void FinishModeServer()
    {
        if (!IsServer || isRaceFinished) return;
        isRaceFinished = true;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        if (Game == null) return;

        if (Game.currentRound.Value < Game.maxRounds)
        {
            Game.LoadRandomMinigame();
        }
        else
        {
            Game.ReturnToLobby();
        }
    }
}