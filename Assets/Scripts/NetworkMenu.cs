using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkMenu : MonoBehaviour
{
    [Header("Referencias de Botones de UI")]
    public Button hostButton;
    public Button clientButton;

    [Header("Referencias de UI (TextMeshPro)")]
    public TMP_InputField ipClientInputField;

    [Header("Configuración de Escena")]
    [SerializeField] private string nombreEscenaJuego = "LobbyScene";

    private void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);

        if (clientButton != null)
            clientButton.onClick.AddListener(OnClientClicked);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    public void OnHostClicked()
    {
        if (NetworkManager.Singleton == null) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            Debug.Log("Configurando Host en puerto 7777...");
        }

        if (NetworkManager.Singleton.StartHost())
        {
            if (ipClientInputField != null)
                ipClientInputField.gameObject.SetActive(false);

            string ipObtenida = GetLocalIPAdress();
            ServerData.IPLocalGuardada = ipObtenida;

            NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaJuego, LoadSceneMode.Single);
        }
    }

    public void OnClientClicked()
    {
        if (NetworkManager.Singleton == null) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            string targetIP = "127.0.0.1";
            if (ipClientInputField != null && !string.IsNullOrWhiteSpace(ipClientInputField.text))
            {
                targetIP = ipClientInputField.text.Trim();
            }

            transport.SetConnectionData(targetIP, 7777);
            transport.ConnectTimeoutMS = 1000;
            transport.MaxConnectAttempts = 3;
            Debug.Log($"Conectando Cliente a {targetIP}:7777...");
        }

        // Opcional: deshabilitar interacción mientras intenta conectar, sin ocultar el campo
        SetClientUIInteractable(false);

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogWarning("No se pudo iniciar el Cliente.");
            SetClientUIInteractable(true);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (ipClientInputField != null)
                ipClientInputField.gameObject.SetActive(false);
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Si falla la conexión o se desconecta, restauramos los controles de UI
            if (ipClientInputField != null)
                ipClientInputField.gameObject.SetActive(true);

            SetClientUIInteractable(true);
        }
    }

    private void SetClientUIInteractable(bool interactable)
    {
        if (ipClientInputField != null)
            ipClientInputField.interactable = interactable;

        if (clientButton != null)
            clientButton.interactable = interactable;
    }

    private string GetLocalIPAdress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    string ipStr = ip.ToString();
                    if (!ipStr.StartsWith("127."))
                    {
                        return ipStr;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo obtener la IP local: {e.Message}");
        }
        return "127.0.0.1";
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}