using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectButton : MonoBehaviour
{
    [Header("Escena principal")]
    [SerializeField] private string escena = "MainMenu";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void backToMainMenu()
    {
        if (NetworkManager.Singleton!=null) {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(escena);
    }
}
