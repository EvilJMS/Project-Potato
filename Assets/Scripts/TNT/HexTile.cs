using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HexTile : MonoBehaviour
{
    [Header("Tiempos y Visual")]
    [SerializeField] private float breakDelay = 0.75f;
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float shakeIntensity = 0.04f;

    private int tileIndex = -1;
    private bool isTriggered;
    private bool isBroken;

    private Collider tileCollider;
    private Renderer tileRenderer;
    private Material tileMaterialInstance;
    private Color originalColor = Color.white;
    private Vector3 originalLocalPos;

    public int TileIndex => tileIndex;
    public bool IsBroken => isBroken;

    private void Awake()
    {
        tileCollider = GetComponent<Collider>();
        tileCollider.isTrigger = false;

        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer == null)
        {
            tileRenderer = GetComponentInChildren<Renderer>();
        }

        if (tileRenderer != null)
        {
            tileMaterialInstance = tileRenderer.material;
            originalColor = tileMaterialInstance.color;
        }

        originalLocalPos = transform.localPosition;
    }

    public void InitializeIndex(int index)
    {
        tileIndex = index;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTriggerFromCollider(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryTriggerFromCollider(collision.collider);
    }

    private void TryTriggerFromCollider(Collider other)
    {
        if (isTriggered || isBroken) return;
        if (HexagoniaMode.Instance == null || !HexagoniaMode.Instance.IsServer) return;
        if (HexagoniaMode.Instance.IsMatchOver) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) player = other.GetComponentInParent<PlayerController>();

        if (player == null || player.IsDeadOrRespawning) return;

        // Comprueba que el jugador esté encima de la baldosa y no rozándola desde abajo
        if (player.transform.position.y >= transform.position.y - 0.1f)
        {
            HexagoniaMode.Instance.NotifyTileSteppedServer(tileIndex);
        }
    }

    public void StartBreakSequenceLocal()
    {
        if (isTriggered || isBroken) return;
        isTriggered = true;
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        float elapsed = 0f;

        while (elapsed < breakDelay)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / breakDelay);

            if (tileMaterialInstance != null)
            {
                float pulse = Mathf.PingPong(elapsed * 12f, 1f);
                tileMaterialInstance.color = Color.Lerp(originalColor, warningColor, Mathf.Max(t, pulse * 0.7f));
            }

            Vector3 randomOffset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                0f,
                Random.Range(-shakeIntensity, shakeIntensity)
            );
            transform.localPosition = originalLocalPos + randomOffset;

            yield return null;
        }

        BreakNowLocal();
    }

    public void BreakNowLocal()
    {
        isTriggered = true;
        isBroken = true;
        transform.localPosition = originalLocalPos;

        if (tileCollider != null) tileCollider.enabled = false;
        if (tileRenderer != null) tileRenderer.enabled = false;
    }

    public void ResetTileLocal()
    {
        StopAllCoroutines();
        isTriggered = false;
        isBroken = false;
        transform.localPosition = originalLocalPos;

        if (tileMaterialInstance != null)
        {
            tileMaterialInstance.color = originalColor;
        }

        if (tileCollider != null) tileCollider.enabled = true;
        if (tileRenderer != null) tileRenderer.enabled = true;
    }
}