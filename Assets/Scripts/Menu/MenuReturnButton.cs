using UnityEngine;

public class MenuReturnButton : MonoBehaviour
{
    public void ReturnToMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenuByButton();
        }
        else
        {
            Debug.LogWarning("GameManager.Instance no encontrado.");
        }
    }
}