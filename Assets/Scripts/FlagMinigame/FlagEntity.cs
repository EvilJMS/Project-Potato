using Unity.Netcode;
using UnityEngine;

public enum FlagState : byte
{
    AtBase = 0,
    Carried = 1,
    Dropped = 2
}

[RequireComponent(typeof(Collider))]
public class FlagEntity : NetworkBehaviour
{
    [Header("Identidad de Equipo")]
    [SerializeField] private int teamId = 0;
    [SerializeField] private Transform baseSpawnPoint;

    [Header("Reglas de Bandera y Puntuación")]
    [SerializeField] private float basePointInterval = 1f;
    [SerializeField] private int pointsPerTick = 1;
    [SerializeField] private float returnToBaseDelay = 12f;

    public int TeamId => teamId;
    public FlagState State => flagState.Value;
    public ulong CarrierClientId => carrierClientId.Value;

    private readonly NetworkVariable<FlagState> flagState = new(
        FlagState.AtBase,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<ulong> carrierClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float pointTickTimer;
    private float droppedTimer;
    private Collider triggerCollider;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (baseSpawnPoint == null)
        {
            baseSpawnPoint = transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            ReturnToBaseServer();
        }
    }

    private void Update()
    {
        if (!IsServer || CaptureFlagMode.Instance == null || CaptureFlagMode.Instance.IsMatchOver)
            return;

        switch (flagState.Value)
        {
            case FlagState.AtBase:
                pointTickTimer += Time.deltaTime;
                if (pointTickTimer >= basePointInterval)
                {
                    pointTickTimer -= basePointInterval;
                    CaptureFlagMode.Instance.AddTeamScoreServer(teamId, pointsPerTick);
                }
                break;

            case FlagState.Carried:
                FollowCarrierServer();
                break;

            case FlagState.Dropped:
                droppedTimer += Time.deltaTime;
                if (droppedTimer >= returnToBaseDelay)
                {
                    ReturnToBaseServer();
                }
                break;
        }
    }

    private void FollowCarrierServer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.ConnectedClients.TryGetValue(carrierClientId.Value, out var client))
        {
            DropAtPositionServer(transform.position);
            return;
        }

        if (client.PlayerObject != null)
        {
            transform.position = client.PlayerObject.transform.position + Vector3.up * 1.6f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || CaptureFlagMode.Instance == null || CaptureFlagMode.Instance.IsMatchOver)
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDeadOrRespawning) return;

        int playerTeam = CaptureFlagMode.Instance.GetPlayerTeam(player.OwnerClientId);

        if (flagState.Value == FlagState.AtBase || flagState.Value == FlagState.Dropped)
        {
            if (flagState.Value == FlagState.Dropped || playerTeam != teamId)
            {
                PickUpByServer(player);
            }
        }
    }

    public void PickUpByServer(PlayerController player)
    {
        if (!IsServer || player == null) return;
        if (player.CarriedFlag != null) return;

        carrierClientId.Value = player.OwnerClientId;
        flagState.Value = FlagState.Carried;
        droppedTimer = 0f;
        pointTickTimer = 0f;

        if (triggerCollider != null) triggerCollider.enabled = false;
        player.SetCarriedFlagServer(this);
    }

    public void DropAtPositionServer(Vector3 dropPos)
    {
        if (!IsServer) return;

        if (carrierClientId.Value != ulong.MaxValue &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(carrierClientId.Value, out var client) &&
            client.PlayerObject != null &&
            client.PlayerObject.TryGetComponent<PlayerController>(out var pc))
        {
            pc.ClearCarriedFlagServer();
        }

        carrierClientId.Value = ulong.MaxValue;
        flagState.Value = FlagState.Dropped;
        droppedTimer = 0f;

        Vector3 safePos = dropPos;
        safePos.y += 0.6f;
        transform.position = safePos;

        if (triggerCollider != null) triggerCollider.enabled = true;
    }

    public void ReturnToBaseServer()
    {
        if (!IsServer) return;

        if (carrierClientId.Value != ulong.MaxValue &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(carrierClientId.Value, out var client) &&
            client.PlayerObject != null &&
            client.PlayerObject.TryGetComponent<PlayerController>(out var pc))
        {
            pc.ClearCarriedFlagServer();
        }

        carrierClientId.Value = ulong.MaxValue;
        flagState.Value = FlagState.AtBase;
        droppedTimer = 0f;
        pointTickTimer = 0f;

        if (baseSpawnPoint != null)
        {
            transform.position = baseSpawnPoint.position;
        }

        if (triggerCollider != null) triggerCollider.enabled = true;
    }

    public bool IsContestedOrActiveObjective()
    {
        return flagState.Value != FlagState.AtBase;
    }
}