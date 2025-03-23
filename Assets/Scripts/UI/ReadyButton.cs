using UnityEngine;
using UnityEngine.UI;

public class ReadyButton : MonoBehaviour
{
    [SerializeField] GameObject button;
    [SerializeField] bool empezarDesactivado;

    private void Start()
    {
        if (empezarDesactivado) 
            this.GetComponent<Button>().interactable = false;
    }
    public void ready()
    {
        LobbyManager.Instance.afegirLlistaReady();
        this.gameObject.SetActive(false);
        button.SetActive(true);
    }

    public void notReady()
    {
        LobbyManager.Instance.treureLlistaReady();
        this.gameObject.SetActive(false);
        button.SetActive(true);

    }
}

