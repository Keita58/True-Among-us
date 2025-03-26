using m17;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] Button botonMatar;
    PlayerBehaviour killer;
    [SerializeField] TextMeshProUGUI textoMuerte;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void setKiller(PlayerBehaviour player)
    {
        killer = player;
        botonMatar.gameObject.SetActive(true);
    }

    public void ActivarBotonKill()
    {
        botonMatar.GetComponent<Button>().interactable = true;
    }

    public void ActivarTextoMuerte()
    {
        textoMuerte.gameObject.SetActive(true);
    }

    public void Matar()
    {
        
        if (killer.GetComponent<PlayerBehaviour>().enemigos != null)
        {
            Collider enemigo = killer.GetComponent<PlayerBehaviour>().enemigos[0];
            enemigo.TryGetComponent(out PlayerBehaviour muelto);
            killer.GetComponent<PlayerBehaviour>().MatarEnemigoRpc(muelto.OwnerClientId);
        }
    }
}
