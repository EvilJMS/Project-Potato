using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkMenu : MonoBehaviour
{
    [Header("Referencias de Botones de UI")]
    public Button hostButton;
    public Button clientButton;

    [Header("Referencias de UI (TextMeshPro)")]
    //public TextMeshProUGUI ipTextServer;
    public TMP_InputField ipClientInputField;

    [Header("Configuración de Escena")]
    [SerializeField] private string nombreEscenaJuego = "GameScene"; // Nombre de la escena a la que quieres cambiar

    private void Start()
    {
        // Asignar los listeners a los botones de la UI
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);

        if (clientButton != null)
            clientButton.onClick.AddListener(OnClientClicked);
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

        if (ipClientInputField != null)
            ipClientInputField.gameObject.SetActive(false);

        // Iniciar el Host
        if (NetworkManager.Singleton.StartHost())
        {
            // 1. Obtenemos la IP local automáticamente
            string ipObtenida = GetLocalIPAdress();

            // 2. La guardamos en el puente global para que la lea la siguiente escena
            ServerData.IPLocalGuardada = ipObtenida;

            // 3. Cambiar de escena (Solo el Host da la orden)
            NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaJuego, UnityEngine.SceneManagement.LoadSceneMode.Single);
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
        }

        if (ipClientInputField != null)
            ipClientInputField.gameObject.SetActive(false);

        // Iniciar el Cliente (el cambio de escena le llegará automáticamente del servidor al conectarse con éxito)
        NetworkManager.Singleton.StartClient();
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
}
