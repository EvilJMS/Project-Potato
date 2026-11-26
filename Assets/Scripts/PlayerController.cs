using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento y Salto")]
    public float speed = 5f;
    public float jumpForce = 5f;
    [SerializeField] private float coyoteTime = 0.14f;
    [SerializeField] private float knockbackDuration = 0.25f;

    [Header("Golpe Cuerpo a Cuerpo (Empuje + Stun)")]
    [SerializeField] private Key meleeKey = Key.Q;
    [SerializeField] private float meleeCooldown = 0.7f;
    [SerializeField] private float meleeRange = 2.2f;
    [SerializeField] private float meleeRadius = 1.3f;
    [SerializeField] private float meleeStunDuration = 1.6f;
    [SerializeField] private float meleePushForce = 7.5f;
    [SerializeField] private float meleeUpwardLift = 2.5f;
    [SerializeField] private int meleeHitsToDropFlag = 2;
    [SerializeField] private AudioClip meleeSwingClip;
    [SerializeField] private Color meleeVisualColor = new Color(1f, 0.45f, 0.1f, 0.65f);

    [Header("Referencias UI y Audio")]
    public TextMeshProUGUI userName;
    public GameObject panel;
    public GameObject menuManager;
    public ColorSelectionManager colorSelectionManager;
    public AudioSource audioSource;
    public AudioClip jumpClip;

    [Header("Configuración de Disparo")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private Renderer playerRenderer;
    private Material playerMaterialInstance;
    private Rigidbody rb;

    private Vector3 inputVector;
    private Vector3 lastSentInput;
    private bool hasSentZeroInput = true;

    public bool isGrounded;
    private float coyoteTimer;
    private float knockbackTimer;
    private float stunTimer;
    private float lastMeleeTimeServer;

    private FlagEntity carriedFlag;
    private int flagHitCount;
    private bool isDeadOrRespawning;

    private Vector3? lastCheckpointPos;
    private Quaternion lastCheckpointRot;

    public FlagEntity CarriedFlag => carriedFlag;
    public bool IsDeadOrRespawning => isDeadOrRespawning;

    private readonly NetworkVariable<Color> playerColor = new(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<FixedString32Bytes> playerName = new(
        "AwA",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> playerTeam = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<bool> isReady = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<bool> isStunnedNet = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<int> flagHitsNet = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<bool> isCarryingFlagNet = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public readonly NetworkVariable<bool> isDeadNet = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }

        if (playerRenderer != null)
        {
            playerMaterialInstance = playerRenderer.material;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        panel = GameObject.FindGameObjectWithTag("PanelPausa");
        menuManager = panel;

        playerColor.OnValueChanged += OnColorChanged;
        playerName.OnValueChanged += OnNameChanged;

        if (IsServer)
        {
            if (rb != null) rb.isKinematic = false;

            ColorSelectionManager colorMgr = ColorSelectionManager.Instance != null
                ? ColorSelectionManager.Instance
                : colorSelectionManager != null
                    ? colorSelectionManager
                    : FindFirstObjectByType<ColorSelectionManager>();

            playerColor.Value = colorMgr != null
                ? colorMgr.AssignFirstAvailableColorServer(OwnerClientId)
                : Color.white;

            playerName.Value = $"Player_{OwnerClientId}";

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadedServer;
            }

            PlaceAtSpawnPointServer();
        }
        else
        {
            if (rb != null) rb.isKinematic = true;
        }

        if (userName != null)
        {
            userName.text = playerName.Value.ToString();
        }

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isReady.OnValueChanged += OnReadyStateChanged;
        }

        ApplyColor(playerColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnColorChanged;
        playerName.OnValueChanged -= OnNameChanged;

        if (IsServer)
        {
            inputVector = Vector3.zero;
            knockbackTimer = 0f;
            stunTimer = 0f;

            ColorSelectionManager colorMgr = ColorSelectionManager.Instance != null
                ? ColorSelectionManager.Instance
                : FindFirstObjectByType<ColorSelectionManager>();

            if (colorMgr != null)
            {
                colorMgr.ReleaseColorServer(OwnerClientId);
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadedServer;
            }
        }

        if (IsOwner)
        {
            isReady.OnValueChanged -= OnReadyStateChanged;
        }

        base.OnNetworkDespawn();
    }

    private void OnSceneLoadedServer(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;
        PlaceAtSpawnPointServer();
    }

    public void SetCheckpointServer(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return;

        if (!lastCheckpointPos.HasValue || Vector3.Distance(lastCheckpointPos.Value, position) > 0.25f)
        {
            lastCheckpointPos = position;
            lastCheckpointRot = rotation;
        }
    }

    public void ResetCheckpointServer()
    {
        if (!IsServer) return;
        lastCheckpointPos = null;
    }

    public void RespawnAtLastCheckpointServer()
    {
        if (!IsServer) return;

        if (lastCheckpointPos.HasValue)
        {
            TeleportToServer(lastCheckpointPos.Value, lastCheckpointRot);
        }
        else
        {
            PlaceAtSpawnPointServer();
        }
    }

    public void ApplyKnockbackServer(Vector3 impulse)
    {
        if (!IsServer || rb == null || isDeadOrRespawning) return;

        knockbackTimer = knockbackDuration;
        rb.AddForce(impulse, ForceMode.Impulse);
    }

    public void SetCarriedFlagServer(FlagEntity flag)
    {
        if (!IsServer) return;
        carriedFlag = flag;
        flagHitCount = 0;
        isCarryingFlagNet.Value = true;
        flagHitsNet.Value = 0;
    }

    public void ClearCarriedFlagServer()
    {
        if (!IsServer) return;
        carriedFlag = null;
        flagHitCount = 0;
        isCarryingFlagNet.Value = false;
        flagHitsNet.Value = 0;
    }

    public void DropCarriedFlagServer()
    {
        if (!IsServer || carriedFlag == null) return;
        FlagEntity flagToDrop = carriedFlag;
        ClearCarriedFlagServer();
        flagToDrop.DropAtPositionServer(transform.position);
    }

    public void ApplyHitPushAndStunServer(Vector3 pushImpulse, float duration, int hitsToDrop)
    {
        if (!IsServer || isDeadOrRespawning) return;

        stunTimer = Mathf.Max(stunTimer, duration);
        knockbackTimer = Mathf.Max(knockbackTimer, duration * 0.65f);
        isStunnedNet.Value = true;
        inputVector = Vector3.zero;

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, Mathf.Max(0f, rb.linearVelocity.y), 0f);
            rb.AddForce(pushImpulse, ForceMode.Impulse);
        }

        if (carriedFlag != null)
        {
            flagHitCount++;
            flagHitsNet.Value = flagHitCount;

            if (flagHitCount >= hitsToDrop)
            {
                DropCarriedFlagServer();
            }
        }
    }

    public void KillByLavaAndRespawnServer()
    {
        if (!IsServer || isDeadOrRespawning) return;

        isDeadOrRespawning = true;
        isDeadNet.Value = true;
        stunTimer = 0f;
        isStunnedNet.Value = false;
        knockbackTimer = 0f;
        inputVector = Vector3.zero;

        if (carriedFlag != null)
        {
            DropCarriedFlagServer();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        SetPlayerVisualsVisibleClientRpc(false);

        if (CaptureFlagMode.Instance != null)
        {
            CaptureFlagMode.Instance.SchedulePlayerRespawnServer(this);
        }
        else if (HexagoniaMode.Instance != null)
        {
            // En Hexagonia no respawnea: queda eliminado hasta fin de ronda
        }
        else
        {
            RespawnFromLavaServer();
        }
    }

    public void RespawnFromLavaServer()
    {
        if (!IsServer) return;

        isDeadOrRespawning = false;
        isDeadNet.Value = false;
        flagHitCount = 0;
        flagHitsNet.Value = 0;
        stunTimer = 0f;
        isStunnedNet.Value = false;
        knockbackTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        PlaceAtSpawnPointServer();
        SetPlayerVisualsVisibleClientRpc(true);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetPlayerVisualsVisibleClientRpc(bool visible)
    {
        if (playerRenderer != null)
        {
            playerRenderer.enabled = visible;
        }
        if (userName != null)
        {
            userName.gameObject.SetActive(visible);
        }
    }

    private void OnReadyStateChanged(bool previousValue, bool newValue)
    {
        if (IsOwner && LobbyManager.Instance != null)
        {
            LobbyManager.Instance.UpdateLocalReadyUI(newValue);
        }
    }

    public void PlaceAtSpawnPointServer()
    {
        if (!IsServer) return;

        SceneSpawnPoints spawnContainer = FindFirstObjectByType<SceneSpawnPoints>();
        if (spawnContainer == null || spawnContainer.spawnPoints == null || spawnContainer.spawnPoints.Length == 0)
            return;

        List<Transform> validSpawns = new List<Transform>(spawnContainer.spawnPoints.Length);
        foreach (Transform sp in spawnContainer.spawnPoints)
        {
            if (sp != null) validSpawns.Add(sp);
        }

        if (validSpawns.Count == 0) return;

        for (int i = validSpawns.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (validSpawns[i], validSpawns[j]) = (validSpawns[j], validSpawns[i]);
        }

        int index = (int)(OwnerClientId % (ulong)validSpawns.Count);
        Transform targetSpawn = validSpawns[index];
        TeleportToServer(targetSpawn.position, targetSpawn.rotation);
    }

    public void TeleportToServer(Vector3 position, Quaternion rotation)
    {
        if (!IsServer) return;

        knockbackTimer = 0f;
        stunTimer = 0f;
        isStunnedNet.Value = false;

        if (rb != null)
        {
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
            rb.isKinematic = wasKinematic;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (TryGetComponent<NetworkTransform>(out NetworkTransform netTransform))
        {
            netTransform.Teleport(position, rotation, transform.localScale);
        }
    }

    private void OnColorChanged(Color previousValue, Color newValue)
    {
        ApplyColor(newValue);
    }

    private void OnNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (userName != null)
        {
            userName.text = newValue.ToString();
        }
    }

    private void ApplyColor(Color newColor)
    {
        if (playerMaterialInstance != null)
        {
            playerMaterialInstance.color = newColor;
        }
        else if (playerRenderer != null)
        {
            playerRenderer.material.color = newColor;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestColorChangeServerRpc()
    {
        playerColor.Value = new Color(Random.value, Random.value, Random.value, 1f);
    }

    public void ToggleReady()
    {
        if (IsOwner)
        {
            ToggleReadyServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ToggleReadyServerRpc()
    {
        isReady.Value = !isReady.Value;

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.UpdateButtons();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandlePauseAndCursorInput();

        float h = 0f;
        float v = 0f;

        if (Cursor.lockState == CursorLockMode.Locked && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                RequestJumpServerRpc();
            }

            if (Keyboard.current[meleeKey].wasPressedThisFrame)
            {
                RequestMeleeAttackServerRpc();
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                RequestColorChangeServerRpc();
            }

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                RequestShootServerRpc();
            }
        }

        Vector3 desiredMove = ComputeCameraRelativeDirection(h, v);
        SendInputIfChanged(desiredMove);
    }

    private void HandlePauseAndCursorInput()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PausaManager pause = GetPauseManager();
            if (pause != null) pause.activePausa();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            bool overUi = UnityEngine.EventSystems.EventSystem.current != null &&
                          UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (!overUi)
            {
                PausaManager pause = GetPauseManager();
                if (pause != null) pause.desactivePausa();

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    private PausaManager GetPauseManager()
    {
        if (menuManager == null)
        {
            menuManager = GameObject.FindGameObjectWithTag("PanelPausa");
        }
        return menuManager != null ? menuManager.GetComponent<PausaManager>() : null;
    }

    private Vector3 ComputeCameraRelativeDirection(float h, float v)
    {
        Vector3 localInput = new Vector3(h, 0f, v);
        if (localInput.sqrMagnitude < 0.01f)
        {
            return Vector3.zero;
        }

        localInput.Normalize();

        Camera mainCam = Camera.main;
        if (mainCam == null) return localInput;

        Vector3 forward = mainCam.transform.forward;
        Vector3 right = mainCam.transform.right;
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude < 0.0001f && right.sqrMagnitude < 0.0001f)
            return localInput;

        forward.Normalize();
        right.Normalize();

        Vector3 dir = (forward * localInput.z) + (right * localInput.x);
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    private void SendInputIfChanged(Vector3 desiredMove)
    {
        if (desiredMove.sqrMagnitude < 0.0001f)
        {
            if (!hasSentZeroInput)
            {
                lastSentInput = Vector3.zero;
                hasSentZeroInput = true;
                SubmitInputServerRpc(Vector3.zero);
            }
            return;
        }

        hasSentZeroInput = false;
        if ((desiredMove - lastSentInput).sqrMagnitude > 0.0025f)
        {
            lastSentInput = desiredMove;
            SubmitInputServerRpc(desiredMove);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || rb == null) return;
        if (isDeadOrRespawning) return;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                isStunnedNet.Value = false;
            }
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
        }
        else if (stunTimer <= 0f)
        {
            Vector3 targetVelocity = inputVector * speed;
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }

        if (stunTimer <= 0f && inputVector.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputVector, Vector3.up);
            rb.MoveRotation(targetRot);
        }

        rb.angularVelocity = Vector3.zero;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitInputServerRpc(Vector3 newInput)
    {
        if (stunTimer > 0f || isDeadOrRespawning)
        {
            inputVector = Vector3.zero;
            return;
        }

        inputVector = newInput;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestJumpServerRpc()
    {
        if (rb == null || isDeadOrRespawning || stunTimer > 0f) return;

        if (isGrounded || coyoteTimer > 0f)
        {
            Vector3 vel = rb.linearVelocity;
            if (vel.y < 0f) vel.y = 0f;
            rb.linearVelocity = vel;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            PlayJumpSoundClientRpc();

            isGrounded = false;
            coyoteTimer = 0f;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestMeleeAttackServerRpc()
    {
        if (isDeadOrRespawning || stunTimer > 0f) return;
        if (Time.time - lastMeleeTimeServer < meleeCooldown) return;

        lastMeleeTimeServer = Time.time;

        Vector3 hitCenter = transform.position + transform.forward * (meleeRange * 0.65f) + Vector3.up * 0.8f;
        PlayMeleeSwingClientRpc(hitCenter, transform.rotation, meleeRadius);

        Collider[] hits = Physics.OverlapSphere(hitCenter, meleeRadius, ~0, QueryTriggerInteraction.Collide);

        int attackerTeam = CaptureFlagMode.Instance != null
            ? CaptureFlagMode.Instance.GetPlayerTeam(OwnerClientId)
            : -1;

        HashSet<PlayerController> hitTargets = new HashSet<PlayerController>();

        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject || col.transform.IsChildOf(transform))
                continue;

            PlayerController target = col.GetComponent<PlayerController>();
            if (target == null) target = col.GetComponentInParent<PlayerController>();

            if (target == null || target == this || target.IsDeadOrRespawning)
                continue;

            if (!hitTargets.Add(target))
                continue;

            if (CaptureFlagMode.Instance != null)
            {
                int targetTeam = CaptureFlagMode.Instance.GetPlayerTeam(target.OwnerClientId);
                if (attackerTeam == targetTeam) continue;
            }

            Vector3 pushDir = target.transform.position - transform.position;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.001f)
            {
                pushDir = transform.forward;
            }
            pushDir.Normalize();

            Vector3 impulse = pushDir * meleePushForce + Vector3.up * meleeUpwardLift;
            target.ApplyHitPushAndStunServer(impulse, meleeStunDuration, meleeHitsToDropFlag);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayMeleeSwingClientRpc(Vector3 center, Quaternion rot, float radius)
    {
        if (audioSource != null && meleeSwingClip != null)
        {
            audioSource.PlayOneShot(meleeSwingClip);
        }

        GameObject slashFx = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slashFx.name = "MeleeHitIndicator";
        slashFx.transform.SetPositionAndRotation(center, rot);
        slashFx.transform.localScale = new Vector3(radius * 1.4f, radius * 0.6f, radius * 0.75f);

        Collider col = slashFx.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        Renderer rend = slashFx.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = meleeVisualColor;
        }

        Destroy(slashFx, 0.16f);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayJumpSoundClientRpc()
    {
        if (audioSource != null && jumpClip != null)
        {
            audioSource.PlayOneShot(jumpClip);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ChangeColorServerRpc(Color newColor)
    {
        playerColor.Value = newColor;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestShootServerRpc()
    {
        if (projectilePrefab == null || isDeadOrRespawning || stunTimer > 0f) return;

        Vector3 spawnPos = firePoint != null
            ? firePoint.position
            : transform.position + (transform.forward * 1.2f) + Vector3.up * 0.5f;

        GameObject projectileInstance = Instantiate(projectilePrefab, spawnPos, transform.rotation);

        if (projectileInstance.TryGetComponent<ProjectilScript>(out ProjectilScript projScript))
        {
            projScript.ShooterClientId = OwnerClientId;
        }

        if (projectileInstance.TryGetComponent<NetworkObject>(out NetworkObject netObj))
        {
            netObj.Spawn(true);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestNameChangeServerRpc(string nuevoNombre)
    {
        if (string.IsNullOrEmpty(nuevoNombre)) return;

        if (nuevoNombre.Length > 32)
        {
            nuevoNombre = nuevoNombre[..32];
        }

        playerName.Value = nuevoNombre;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            coyoteTimer = coyoteTime;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            coyoteTimer = coyoteTime;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    public void SetColorFromServer(Color newColor)
    {
        if (!IsServer) return;
        playerColor.Value = newColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.45f);
        Vector3 hitCenter = transform.position + transform.forward * (meleeRange * 0.65f) + Vector3.up * 0.8f;
        Gizmos.DrawWireSphere(hitCenter, meleeRadius);
    }
}