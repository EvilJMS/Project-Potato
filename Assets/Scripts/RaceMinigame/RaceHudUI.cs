using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RaceHudUI : MonoBehaviour
{
    [Header("Referencias UI (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI finishersText;
    [SerializeField] private TextMeshProUGUI statusBannerText;

    [Header("Formatos")]
    [SerializeField] private string timerPrefix = "Tiempo: ";
    [SerializeField] private string finishersPrefix = "Meta: ";

    private void Update()
    {
        RaceMinigameMode mode = RaceMinigameMode.Instance;
        if (mode == null) return;

        UpdateTimer(mode.RemainingTimeValue);
        UpdateFinishers(mode);
        UpdateStatusBanner(mode);
    }

    private void UpdateTimer(float secondsLeft)
    {
        if (timerText == null) return;

        float clamped = Mathf.Max(0f, secondsLeft);
        int minutes = Mathf.FloorToInt(clamped / 60f);
        int seconds = Mathf.FloorToInt(clamped % 60f);

        if (minutes > 0)
            timerText.text = $"{timerPrefix}{minutes:00}:{seconds:00}";
        else
            timerText.text = $"{timerPrefix}{clamped:0.0}s";
    }

    private void UpdateFinishers(RaceMinigameMode mode)
    {
        if (finishersText == null) return;

        int totalConnected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            ? NetworkManager.Singleton.ConnectedClientsIds.Count
            : 0;

        if (totalConnected > 0)
            finishersText.text = $"{finishersPrefix}{mode.FinishedCountValue} / {totalConnected}";
        else
            finishersText.text = $"{finishersPrefix}{mode.FinishedCountValue}";
    }

    private void UpdateStatusBanner(RaceMinigameMode mode)
    {
        if (statusBannerText == null) return;

        if (mode.IsRaceFinished)
        {
            statusBannerText.gameObject.SetActive(true);
            statusBannerText.text = "¡Carrera terminada!";
            return;
        }

        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
        if (localId != ulong.MaxValue && mode.HasClientFinished(localId))
        {
            statusBannerText.gameObject.SetActive(true);
            statusBannerText.text = "¡Has cruzado la meta! Esperando al resto...";
        }
        else
        {
            statusBannerText.gameObject.SetActive(false);
        }
    }
}