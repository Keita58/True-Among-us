using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonsInici : MonoBehaviour
{
    [SerializeField]
    private Button m_ServerButton;
    [SerializeField]
    private Button m_ClientButton;
    [SerializeField]
    private Button m_HostButton;

    private string tipo;


    void Awake()
    {
        m_ServerButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Lobby");
            tipo = "Server";
            SceneManager.sceneLoaded += OnSceneLoaded;
        });

        m_ClientButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Lobby");
            tipo = "Client";
            SceneManager.sceneLoaded += OnSceneLoaded;
        });

        m_HostButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("Lobby");
            tipo = "Host";
            SceneManager.sceneLoaded += OnSceneLoaded;
        });
    }
 

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (tipo)
        {
            case "Server":
                NetworkManager.Singleton.StartServer();
                break;
            case "Host":
                NetworkManager.Singleton.StartHost();
                break;
            case "Client":
                NetworkManager.Singleton.StartClient();
                break;
        }

    }
}
