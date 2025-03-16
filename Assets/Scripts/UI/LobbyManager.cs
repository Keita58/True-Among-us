using m17;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
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
         player.GetComponent<PlayerBehaviour>().CanviNomRpc(_Nom.text);
        _Nom.gameObject.SetActive(false);
        _Desc.gameObject.SetActive(false);
        _Boto.gameObject.SetActive(false);

        //player.GetComponent<Rigidbody>().isKinematic = false;
    }

    public void SetPlayer(GameObject player)
    {
        this.player = player;
        this.player.GetComponentInChildren<Camera>().enabled = false;
        this.player.GetComponentInChildren<Camera>().enabled = true;
        
    }
}
