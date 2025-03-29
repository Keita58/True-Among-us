using m17;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
    [SerializeField] GameObject ButtonsReadyRoot;
    [SerializeField] TextMeshProUGUI timer;
    List<ulong> m_PlayersReady = new List<ulong>();

    public static LobbyManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }


    public void SetNom()
    {
        Debug.Log("lobby");
        player.GetComponent<PlayerBehaviour>().RequestNameChange(_Nom.text);
        _Nom.gameObject.SetActive(false);
        _Desc.gameObject.SetActive(false);
        _Boto.gameObject.SetActive(false);

        //player.GetComponent<Rigidbody>().isKinematic = false;
    }

    public void SetPlayer(GameObject player)
    {
        this.player = player;
        this.player.GetComponent<PlayerBehaviour>().onStopTimer += StopTimer;
    }

    private void StopTimer()
    {
        timer.text = "";
    }

    public void setColor(Color color)
    {
        player.GetComponent<PlayerBehaviour>().AfegirColorLlist(color);
        desactivarTotsColors();
    }
   
    public void desactivarTotsColors()
    {
        for (int i = 0; i < ButtonsRoot.transform.childCount; i++)
        {
            ButtonsRoot.transform.GetChild(i).gameObject.SetActive(false);
        }

    }   
    public void desactivarColorsJaEscollits(List <Color> colors)
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
    public void ActivarBotonReady()
    {
        for (int i = 0; i < ButtonsReadyRoot.transform.childCount; i++)
        {
            ButtonsReadyRoot.transform.GetChild(i).GetComponent<Button>().interactable=true;
        }
    }

    public void afegirLlistaReady()
    {
        player.GetComponent<PlayerBehaviour>().AfegirReadyPlayer();

    } 
    public void treureLlistaReady()
    {
        player.GetComponent<PlayerBehaviour>().TreureReadyPlayer();

    }

    public void Ready(ulong id)
    {
        if (!this.m_PlayersReady.Contains(id))
            this.m_PlayersReady.Add(id);

        ComprovarCanviEscena();
    }

    public void NotReady(ulong id)
    {
        if (this.m_PlayersReady.Contains(id))
            this.m_PlayersReady.Remove(id);

        ComprovarCanviEscena();
    }

    private void ComprovarCanviEscena()
    {
         if (m_PlayersReady.Count >= 2)
        {
            player.GetComponent<PlayerBehaviour>().CanviarEscena(true);
        }
        else
        {
            player.GetComponent<PlayerBehaviour>().CanviarEscena(false);
        }
    }

    public IEnumerator ActivarTimer()
    {
        int i = 10;
        while (i > 0)
        {
            timer.text = i.ToString();
            i--;
            yield return new WaitForSeconds(1f);
        }
    }
}
