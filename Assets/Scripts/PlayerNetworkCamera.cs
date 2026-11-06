using UnityEngine;
using Unity.Netcode; // Requiere tener instalado Netcode for GameObjects

public class PlayerNetworkCamera : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // VERIFICACIÓN CLAVE: Solo ejecutamos esto en la pantalla del jugador dueño de este personaje
        if (IsOwner)
        {
            // Buscamos la cámara principal de la escena
            Camera camaraPrincipal = Camera.main;

            if (camaraPrincipal != null)
            {
                // Intentamos obtener el script de la cámara
                FollowCamera scriptCamara = camaraPrincipal.GetComponent<FollowCamera>();

                if (scriptCamara != null)
                {
                    // Le asignamos NUESTRO transform como objetivo
                    scriptCamara.objetivo = this.transform;
                }
            }
        }
    }
}
