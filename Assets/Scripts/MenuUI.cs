using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MenuUI : MonoBehaviour
{

    [Header("Componentes de UI")]
    public GameObject panelMenu;           // El objeto Canvas que contiene el menú
    public TMP_InputField inputFieldNombre;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GuardarNombre()
    {
        string textoEscrito = inputFieldNombre.text;

        if (!string.IsNullOrEmpty(textoEscrito))
        {
            // Buscamos el objeto de nuestro propio jugador local en la red
            NetworkClient localClient = NetworkManager.Singleton.LocalClient;

            if (localClient != null && localClient.PlayerObject != null)
            {
                PlayerController jugadorLocal = localClient.PlayerObject.GetComponent<PlayerController>();

                if (jugadorLocal != null)
                {
                    // Llamamos al ServerRpc del jugador para que el servidor actualice el nombre
                    jugadorLocal.RequestNameChangeServerRpc(textoEscrito);
                }
            }

            // Cerramos el menú tras guardar
            //ToggleMenu();
        }
    }
}
