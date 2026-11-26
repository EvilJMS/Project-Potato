using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StartCountdownPlatform : MonoBehaviour
{
    [Header("Visual de Cuenta Atrás")]
    [SerializeField] private Color countdownColor = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField] private float shakeIntensity = 0.03f;

    private Collider platformCollider;
    private Renderer platformRenderer;
    private Material materialInstance;
    private Color originalColor = Color.white;
    private Vector3 originalLocalPos;

    private void Awake()
    {
        platformCollider = GetComponent<Collider>();
        platformCollider.isTrigger = false;

        platformRenderer = GetComponent<Renderer>();
        if (platformRenderer == null)
        {
            platformRenderer = GetComponentInChildren<Renderer>();
        }

        if (platformRenderer != null)
        {
            materialInstance = platformRenderer.material;
            originalColor = materialInstance.color;
        }

        originalLocalPos = transform.localPosition;
    }

    public void StartCountdownLocal(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(CountdownRoutine(duration));
    }

    private IEnumerator CountdownRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (materialInstance != null)
            {
                float pulse = Mathf.PingPong(elapsed * 10f, 1f);
                materialInstance.color = Color.Lerp(originalColor, countdownColor, Mathf.Max(t, pulse * 0.6f));
            }

            Vector3 offset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                0f,
                Random.Range(-shakeIntensity, shakeIntensity)
            );
            transform.localPosition = originalLocalPos + offset;

            yield return null;
        }

        DropNowLocal();
    }

    public void DropNowLocal()
    {
        transform.localPosition = originalLocalPos;
        if (platformCollider != null) platformCollider.enabled = false;
        if (platformRenderer != null) platformRenderer.enabled = false;
    }

    public void ResetPlatformLocal()
    {
        StopAllCoroutines();
        transform.localPosition = originalLocalPos;

        if (materialInstance != null) materialInstance.color = originalColor;
        if (platformCollider != null) platformCollider.enabled = true;
        if (platformRenderer != null) platformRenderer.enabled = true;
    }
}