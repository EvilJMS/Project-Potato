using Unity.Netcode;
using UnityEngine;

public abstract class GameModeBase : NetworkBehaviour
{
    protected GameManager Game => GameManager.Instance;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        OnModeStart();
    }

    public override void OnNetworkDespawn()
    {
        OnModeEnd();
        base.OnNetworkDespawn();
    }

    protected virtual void OnModeStart() { }
    protected virtual void OnModeEnd() { }

    public abstract void FinishModeServer();
}