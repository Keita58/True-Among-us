using m17;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] TMP_InputField _Nom;
    [SerializeField] TextMeshProUGUI _Desc;
    [SerializeField] Button _Boto;
    private GameObject player;
    [SerializeField] GameObject ButtonsRoot;
    private List<Color> colors;


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
        colors = this.player.GetComponent<PlayerBehaviour>().llistaColors();
        if (colors.Count > 0)
        {
            Debug.Log("Entro colors");
            desactivarColorsJaEscollits();
        }
    }

    public void setColor(Color color)
    {
        player.GetComponent<PlayerBehaviour>().CanviColorRpc(color);
        desactivarTotsColors();
    }
   
    public void desactivarTotsColors()
    {
        for (int i = 0; i < ButtonsRoot.transform.childCount; i++)
        {
            ButtonsRoot.transform.GetChild(i).gameObject.SetActive(false);
        }

    }   
    public void desactivarColorsJaEscollits()
    {
        for (int i = 0; i < ButtonsRoot.transform.childCount; i++)
        {
            foreach (Color color in colors)
            {
                if (color == ButtonsRoot.transform.GetChild(i).GetComponent<Image>().color)
                {
                    ButtonsRoot.transform.GetChild(i).GetComponent<Button>().interactable = false;
                }

            }
        }

    }
}
