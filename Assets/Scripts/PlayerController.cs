using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public TextMeshProUGUI userName;
    public GameObject panel;

    public AudioSource audioSource;
    public AudioClip jumpClip;

    private Renderer playerRenderer;

    public GameObject menuManager;

    private Rigidbody rb;
    private Vector3 inputVector; // Cambiado a Vector3 para recibir la dirección orientada
    public bool isGrounded;


    [Header("Configuración de Disparo")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;


    private readonly NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "AwA",
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
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        panel = GameObject.FindGameObjectWithTag("PanelPausa");
        Debug.Log(panel);

        playerColor.OnValueChanged += OnColorChanged;
        playerName.OnValueChanged += OnNameChanged;

        if (IsServer)
        {
            rb.isKinematic = false;
            playerColor.Value = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
            playerName.Value = $"Player_{OwnerClientId}";
            //userName.text = playerName.Value.ToString();
        }
        else
        {
            rb.isKinematic = true;
        }
        userName.text = playerName.Value.ToString();
        // BLOQUEO AL SPAWNEAR: Si este objeto nos pertenece localmente, bloqueamos el ratón para jugar
        if (IsOwner)
        {
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ApplyColor(playerColor.Value);
        if (!string.IsNullOrEmpty(playerName.Value.ToString()))
        {
            Debug.Log($"Player name: {playerName.Value.ToString()}");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        playerColor.OnValueChanged -= OnColorChanged;
        playerName.OnValueChanged -= OnNameChanged;
    }

    private void OnColorChanged(Color previousValue, Color newValue)
    {
        ApplyColor(newValue);
        Debug.Log($"El color anterior del jugador era {previousValue} y ahora es {newValue}");
    }

    private void OnNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        Debug.Log($"[NOMBRE ACTUALIZADO] El jugador con ID {OwnerClientId} ahora se llama: {newValue}");
        // IMPORTANTE: Actualiza el texto flotante sobre la cabeza del jugador
        if (userName != null)
        {
            userName.text = newValue.ToString();

            userName.ForceMeshUpdate();

            // 3. (Opcional) Si tu Canvas del nombre usa algún componente de organización automática (como Content Size Fitter),
            // esto obliga a Unity a centrar de nuevo el contenedor en el espacio 3D.
            Canvas.ForceUpdateCanvases();
        }
    }

    private void ApplyColor(Color newColor)
    {
        if (playerRenderer != null)
        {
            playerRenderer.material.color = newColor;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestColorChangeServerRpc()
    {
        playerColor.Value = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
    }

    private void Update()
    {
        if (!IsOwner) return;

        // CONDICIÓN 1: Si pulsas Escape, liberas el ratón para menús
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            menuManager = GameObject.FindGameObjectWithTag("PanelPausa");
            if (menuManager != null) {
                menuManager.GetComponent<PausaManager>().activePausa();
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

        }

        // CONDICIÓN 2: Si haces clic en el juego (y no en la UI), vuelves a capturar el ratón
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                menuManager = GameObject.FindGameObjectWithTag("PanelPausa");
                if (menuManager != null)
                {
                    menuManager.GetComponent<PausaManager>().desactivePausa();
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                
            }
        }

        float h = 0f;
        float v = 0f;

        // SOLO LEER TECLAS SI EL RATÓN ESTÁ BLOQUEADO (Para evitar moverse mientras usas menús)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    RequestJumpServerRpc();
                }

                if (Keyboard.current.cKey.wasPressedThisFrame)
                {
                    RequestColorChangeServerRpc();
                }

                if (Keyboard.current.fKey.wasPressedThisFrame)
                {
                    Debug.Log("hola estoy disparando");
                    RequestShootServerRpc();
                }
            }
        }

        // CÁLCULO DE MOVIMIENTO RELATIVO A LA PERSPECTIVA DE LA CÁMARA
        Vector3 direccionInputLocal = new Vector3(h, 0f, v).normalized;
        Vector3 direccionFinalCalculada = Vector3.zero;

        if (direccionInputLocal.sqrMagnitude > 0.01f)
        {
            Transform camaraTransform = Camera.main != null ? Camera.main.transform : null;

            if (camaraTransform != null)
            {
                Vector3 camaraAdelante = camaraTransform.forward;
                Vector3 camaraDerecha = camaraTransform.right;

                camaraAdelante.y = 0f;
                camaraDerecha.y = 0f;
                camaraAdelante.Normalize();
                camaraDerecha.Normalize();

                direccionFinalCalculada = (camaraAdelante * direccionInputLocal.z) + (camaraDerecha * direccionInputLocal.x);
                direccionFinalCalculada.Normalize();
            }
            else
            {
                direccionFinalCalculada = direccionInputLocal;
            }
        }

        // Enviamos la dirección procesada al servidor
        SubmitInputServerRpc(direccionFinalCalculada);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        Vector3 moveDirection = inputVector;
        Vector3 targetVelocity = moveDirection * speed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 lookPoint = transform.position + moveDirection;
            transform.LookAt(lookPoint, Vector3.up);
        }
        rb.angularVelocity = Vector3.zero;
    }

    // RPC modificado para sincronizar un Vector3
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitInputServerRpc(Vector3 inputDireccionFlipped)
    {
        inputVector = inputDireccionFlipped;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestJumpServerRpc()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            PlayJumpSoundClientRpc();
            isGrounded = false;
        }
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
        if (projectilePrefab != null)
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (transform.forward * 1.2f) + Vector3.up * 0.5f;
            Quaternion spawnRot = transform.rotation;

            GameObject projectileInstance = Instantiate(projectilePrefab, spawnPos, spawnRot);

            ProjectilScript projScript = projectileInstance.GetComponent<ProjectilScript>();
            if (projScript != null)
            {
                projScript.ShooterClientId = OwnerClientId;
            }

            NetworkObject netObj = projectileInstance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn(true);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestNameChangeServerRpc(string nuevoNombre)
    {
        // Validar en el servidor que el nombre no esté vacío o sea demasiado largo
        if (string.IsNullOrEmpty(nuevoNombre)) return;

        if (nuevoNombre.Length > 32)
        {
            nuevoNombre = nuevoNombre.Substring(0, 32);
        }

        // Al cambiar la NetworkVariable, se sincroniza automáticamente en todos los clientes
        playerName.Value = nuevoNombre;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
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

}
