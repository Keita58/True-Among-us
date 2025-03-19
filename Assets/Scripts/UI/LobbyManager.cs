using m17;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] TMP_InputField _Nom;
    [SerializeField] TextMeshProUGUI _Desc;
    [SerializeField] Button _Boto;
    private GameObject player;

    public static LobbyManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void SetNom()
    {
        Debug.Log("lobby");
        player.GetComponent<PlayerBehaviour>().CanviNomRpc(_Nom.text);
        _Nom.gameObject.SetActive(false);
        _Desc.gameObject.SetActive(false);
        _Boto.gameObject.SetActive(false);

        //player.GetComponent<Rigidbody>().isKinematic = false;
    }

    public void SetPlayer(GameObject player)
    {
        this.player = player;        
    }
}
