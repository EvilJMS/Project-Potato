using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El personaje que la cámara debe seguir.")]
    public Transform objetivo;

    [Header("Configuración de Distancia")]
    public float distancia = 4.0f;
    public float altura = 1.2f;

    [Header("Sensibilidad (Nuevo Input System)")]
    public float sensibilidadX = 0.4f;
    public float sensibilidadY = 0.4f;

    [Header("Límites de Ángulo (Vertical)")]
    public float anguloMinY = -5f;
    public float anguloMaxY = 45f;

    [Header("Suavizado Elástico")]
    [Tooltip("Velocidad de suavizado para el movimiento de posición.")]
    public float suavizadoPosicion = 10.0f;

    [Tooltip("Velocidad de suavizado para el giro. Valores entre 8 y 15 dan el feeling de Fall Guys.")]
    public float suavizadoRotacion = 12.0f;

    // Valores directos del ratón (sin suavizar)
    private float targetRotacionX = 0.0f;
    private float targetRotacionY = 0.0f;

    // Valores actuales aplicados a la cámara (suavizados)
    private float currentRotacionX = 0.0f;
    private float currentRotacionY = 0.0f;

    void Start()
    {
        Vector3 angulos = transform.eulerAngles;
        targetRotacionX = angulos.y;
        targetRotacionY = angulos.x;

        currentRotacionX = targetRotacionX;
        currentRotacionY = targetRotacionY;
    }

    void LateUpdate()
    {
        if (!objetivo) return;

        // CONDICIÓN: Si el cursor está libre (menús), la cámara sigue al jugador pero no rota con el ratón
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Vector3 posicionBloqueada = objetivo.position - (Quaternion.Euler(currentRotacionY, currentRotacionX, 0) * Vector3.forward * distancia) + (Vector3.up * altura);
            transform.position = Vector3.Lerp(transform.position, posicionBloqueada, suavizadoPosicion * Time.deltaTime);
            return;
        }

        // 1. Leer el ratón de forma directa si está bloqueado
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 deltaRaton = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();

            targetRotacionX += deltaRaton.x * sensibilidadX;
            targetRotacionY -= deltaRaton.y * sensibilidadY;
        }

        // 2. Limitar el objetivo vertical
        targetRotacionY = Mathf.Clamp(targetRotacionY, anguloMinY, anguloMaxY);

        // 3. Suavizado angular mediante LerpAngle
        currentRotacionX = Mathf.LerpAngle(currentRotacionX, targetRotacionX, suavizadoRotacion * Time.deltaTime);
        currentRotacionY = Mathf.LerpAngle(currentRotacionY, targetRotacionY, suavizadoRotacion * Time.deltaTime);

        // 4. Calcular transformaciones finales
        Quaternion rotacionFinal = Quaternion.Euler(currentRotacionY, currentRotacionX, 0);
        Vector3 posicionDeseada = objetivo.position - (rotacionFinal * Vector3.forward * distancia) + (Vector3.up * altura);

        // 5. Aplicar posición y rotación de manera fluida
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizadoPosicion * Time.deltaTime);
        transform.rotation = rotacionFinal;
    }
}
