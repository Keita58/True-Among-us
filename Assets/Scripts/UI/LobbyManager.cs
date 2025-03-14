using m17;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] TMP_InputField _Nom;
    [SerializeField] TextMeshProUGUI _Desc;
    [SerializeField] Button _Boto;

    public void SetNom()
    {
        if(NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerBehaviour>() != null)
            NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<PlayerBehaviour>().CanviNomRpc(_Nom.text);
        _Nom.gameObject.SetActive(false);
        _Desc.gameObject.SetActive(false);
        _Boto.gameObject.SetActive(false);
    }
}
