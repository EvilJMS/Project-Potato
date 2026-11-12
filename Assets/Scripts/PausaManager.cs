using UnityEngine;
using UnityEngine.InputSystem;

public class PausaManager : MonoBehaviour
{
    public GameObject panel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //panel = GameObject.FindGameObjectWithTag("PanelPausa");
        panel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void activePausa()
    {
        panel.SetActive(true);
    }

    public void desactivePausa() 
    {
        panel.SetActive(false);
    }
}
