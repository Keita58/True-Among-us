using m17;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] Button botonMatar;
    GameObject killer;
    [SerializeField] TextMeshProUGUI textoMuerte;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void setKiller(GameObject player)
    {
        killer = player;
        Debug.Log("KILLER"+killer.GetComponent<PlayerBehaviour>().OwnerClientId);
        botonMatar.gameObject.SetActive(true);
    }

    public void ToggleBotonKill(bool isInteractable)
    {
        if (isInteractable)
        {
            botonMatar.GetComponent<Button>().interactable = true;
        }
        else
        {
            botonMatar.GetComponent<Button>().interactable = false;
        }
    }

    public void ActivarTextoMuerte()
    {
        textoMuerte.gameObject.SetActive(true);
    }

    public void Matar()
    {
        
        if (killer.GetComponent<PlayerBehaviour>().enemigos.Length>0)
        {
            Collider enemigo = killer.GetComponent<PlayerBehaviour>().enemigos[0];
            enemigo.TryGetComponent(out PlayerBehaviour muelto);
            Debug.Log(muelto.OwnerClientId);
            muelto.GetComponent<PlayerBehaviour>().MatarEnemigo(muelto.OwnerClientId);
        }
    }
}
