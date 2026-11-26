using Unity.Netcode;
using UnityEngine;

public class FlagMeleeHitbox : MonoBehaviour
{
    [SerializeField] private float stunDuration = 1.6f;
    [SerializeField] private float pushForce = 7.5f;
    [SerializeField] private float upwardLift = 2.5f;
    [SerializeField] private int hitsToDropFlag = 2;
    [SerializeField] private float hitCooldown = 0.5f;

    private float lastAttackTime;

    public void TryHit(PlayerController attacker, PlayerController target)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (attacker == null || target == null || attacker == target) return;
        if (target.IsDeadOrRespawning) return;

        if (Time.time - lastAttackTime < hitCooldown) return;
        lastAttackTime = Time.time;

        if (CaptureFlagMode.Instance != null)
        {
            int attackerTeam = CaptureFlagMode.Instance.GetPlayerTeam(attacker.OwnerClientId);
            int targetTeam = CaptureFlagMode.Instance.GetPlayerTeam(target.OwnerClientId);
            if (attackerTeam == targetTeam) return;
        }

        Vector3 pushDir = target.transform.position - attacker.transform.position;
        pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 0.001f)
        {
            pushDir = attacker.transform.forward;
        }
        pushDir.Normalize();

        Vector3 impulse = pushDir * pushForce + Vector3.up * upwardLift;
        target.ApplyHitPushAndStunServer(impulse, stunDuration, hitsToDropFlag);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController attacker = GetComponentInParent<PlayerController>();
        PlayerController target = other.GetComponent<PlayerController>();
        if (target == null) target = other.GetComponentInParent<PlayerController>();

        TryHit(attacker, target);
    }
}