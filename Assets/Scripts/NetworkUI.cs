using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    [Header("Referencias de UI (TextMeshPro)")]
    public TextMeshProUGUI ipTextServer;       // Texto para mostrar la IP local al ser Host
    public TMP_InputField ipClientInputField;  // Campo de entrada donde el Cliente escribe la IP del Host

    private void OnGUI()
    {
        // Verificar que NetworkManager existe para evitar excepciones
        if (NetworkManager.Singleton == null) return;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            // BOTÓN HOST
            if (GUI.Button(new Rect(10, 10, 100, 30), "Host"))
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
                    Debug.Log("Configurando Host en puerto 7777 y escuchando en 0.0.0.0 (todas las interfaces)");
                }
                ipClientInputField.gameObject.SetActive(false);
                NetworkManager.Singleton.StartHost();

                // Asignar IP al componente de texto si está configurado
                if (ipTextServer != null)
                {
                    ipTextServer.text = GetLocalIPAdress();
                }
            }

            // BOTÓN CLIENT
            if (GUI.Button(new Rect(10, 50, 100, 30), "Client"))
            {
                var transport = NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    // Obtener la IP escrita por el usuario (o localhost por defecto si el campo está vacío)
                    string targetIP = "127.0.0.1";
                    if (ipClientInputField != null && !string.IsNullOrWhiteSpace(ipClientInputField.text))
                    {
                        targetIP = ipClientInputField.text.Trim();
                    }

                    // Se corrigió el puerto a 7777 para coincidir con el Host
                    transport.SetConnectionData(targetIP, 7777);
                    transport.ConnectTimeoutMS = 1000;
                    transport.MaxConnectAttempts = 3;
                }

                NetworkManager.Singleton.StartClient();
                ipClientInputField.gameObject.SetActive(false);
            }
        }
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
        return "127.0.0.1"; // Si falla la búsqueda de IP, devuelve localhost por defecto
    }
}