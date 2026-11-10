using UnityEngine;
using TMPro;
using Unity.Netcode;

public class GameSceneUI : MonoBehaviour
{
    [Header("Arrastra aquí el TextMeshPro de la IP en el juego")]
    public TextMeshProUGUI textoIPJuego;

    void Start()
    {
        if (textoIPJuego != null)
        {
            // Verificamos si este equipo es el Host (Servidor)
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // Si eres el Host, activas el texto y muestras la IP guardada
                textoIPJuego.gameObject.SetActive(true);
                textoIPJuego.text = "IP del Host: " + ServerData.IPLocalGuardada;
            }
            else
            {
                // Si eres un Cliente normal, ocultas el texto para que no lo veas
                textoIPJuego.gameObject.SetActive(false);
            }
        }
    }
}