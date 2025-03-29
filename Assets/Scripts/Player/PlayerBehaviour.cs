using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using static Unity.Burst.Intrinsics.X86;

namespace m17
{
    public class PlayerBehaviour : NetworkBehaviour
    {
        //Variable senzilla, per defecte el client no la pot updatejar
        //recordem que aquestes variables nom�s poden ser de DATA
        //(a no ser que us feu la vostra propia adaptaci�)
        private static Dictionary<ulong, FixedString512Bytes> playerNames = new(); // Almacena los nombres de todos los jugadores en el servidor

        NetworkVariable<float> m_Speed = new NetworkVariable<float>(1);

        NetworkVariable<FixedString512Bytes> nick = new NetworkVariable<FixedString512Bytes>();

        Rigidbody m_Rigidbody;
        [SerializeField] GameObject _Camera;
        List<Color> colors = new List<Color>();
        NetworkVariable<Color> selfColor= new NetworkVariable<Color>();
        private static Dictionary<ulong, Color> playerColors = new();
        bool isKiller=false;

        //per a saber quins clients estàn preparats.
        //per a fer la llista privada.
        [SerializeField] public Collider[] enemigos { get; private set; }

        Vector2 _LookRotation = Vector2.zero;
        [SerializeField] LayerMask layerMask;
        public event Action onStopTimer;

        // No es recomana fer servir perqu� estem en el m�n de la xarxa
        // per� podem per initialitzar components i variables per a totes les inst�ncies
        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }


        void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "Mapa" && IsOwner)
            {
                RequestSpawnPositionRpc();
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestSpawnPositionRpc()
        {
            SetSpawnPositionRpc(new Vector3(-85, 0, -62));
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SetSpawnPositionRpc(Vector3 position)
        {
            transform.position = position;
        }

        // Aix� s� que seria el nou awake
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //Cursor.visible = false;

            //Aquest awake nom�s per a qui li pertany, perqu� tocarem variables on nom�s
            //a nosaltres ens interessa llegir el seu valor
            if (!IsOwner || !IsClient)
                return;


            LobbyManager.Instance.SetPlayer(this.gameObject);

            Camera.main.transform.position = _Camera.transform.position;
            Camera.main.transform.parent = _Camera.transform;


            //colors.OnListChanged += (NetworkListEvent<Color> changeEvent) =>
            //{
            //    List<Color> aux = new List<Color>();
            //    foreach (Color color in colors)
            //    {
            //        aux.Add(color);
            //    }
            //    DesactivarColorsRpc(aux);

            //};

            nick.OnValueChanged += (oldName, newName) =>
            {
                GetComponentInChildren<Canvas>().GetComponentInChildren<TextMeshProUGUI>().text = newName.ToString();
            };
            
            GetComponentInChildren<Canvas>().GetComponentInChildren<TextMeshProUGUI>().text = nick.Value.ToString();

            selfColor.OnValueChanged += (oldColor, newColor) =>
            {
                GetComponent<MeshRenderer>().material.color=newColor;
            };
            GetComponent<MeshRenderer>().material.color = selfColor.Value;


            if (IsServer)
            {
                playerNames[OwnerClientId] = nick.Value;
                playerColors[OwnerClientId]=selfColor.Value;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        void Update()
        {
            //Aquest update nom�s per a qui li pertany
            if (!IsOwner)
            {
                return;
            }

            //moviment a f�sica
            Vector3 movement = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                movement += transform.forward;
            if (Input.GetKey(KeyCode.S))
                movement -= transform.forward;
            if (Input.GetKey(KeyCode.A))
                movement -= transform.right;
            if (Input.GetKey(KeyCode.D))
                movement += transform.right;

            //Qui far� els moviments ser� el servidor, alleugerim i nom�s canvis quan hi hagi input
            MoveCharacterPhysicsServerRpc(movement.normalized * m_Speed.Value, Input.mousePositionDelta);
            
            if (!GetComponentInChildren<Canvas>().GetComponentInChildren<TextMeshProUGUI>().text.Equals("") && SceneManager.GetActiveScene().name.Equals("Lobby")){
                LobbyManager.Instance.ActivarBotonReady();
            }
        }
        //De nou, nom�s data i tipus base com a par�metres.
        //hem afegit el segon par�metre per a saber qui ens d�na la comanda (no es necessari per a aquest exemple, per� potser us �s �til)
 

        //Aix� seria el moviment a f�sica
        [Rpc(SendTo.Server)]
        private void MoveCharacterPhysicsServerRpc(Vector3 velocity, Vector2 lookInput)
        {
            _LookRotation.x += lookInput.x;

            m_Rigidbody.MoveRotation(Quaternion.Euler(0, _LookRotation.x, 0));
            //Debug.Log($"{OwnerClientId} -> " + lookInput);

            m_Rigidbody.linearVelocity = velocity;
        }

        private void OnClientConnected(ulong clientId)
        {
            // Enviar el nombre del jugador al nuevo cliente
            foreach (var entry in playerColors)
            {
                colors.Add(entry.Value);
            }
            foreach (var entry in playerNames)
            {
                UpdateNicknameClientRpc(entry.Key, entry.Value.ToString(), new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }

            foreach (var entry in playerColors)
            {
                UpdatecolorClientRpc(entry.Key, entry.Value, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestNameChangeRpc(string nom)
        {
            nick.Value = nom;
            playerNames[OwnerClientId] = nom;
            UpdateNicknameClientRpc(OwnerClientId, nom);
        }

        public void RequestNameChange(string nom)=>RequestNameChangeRpc(nom);

        [ClientRpc]
        private void UpdateNicknameClientRpc(ulong clientId, string nom, ClientRpcParams rpcParams = default)
        {
            foreach (var player in FindObjectsByType<PlayerBehaviour>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId == clientId)
                {
                    player.GetComponentInChildren<TextMeshProUGUI>().text = nom.ToString();
                    break;
                }
            }
        }

        [ClientRpc]
        private void UpdatecolorClientRpc(ulong clientId, Color color, ClientRpcParams rpcParams = default)
        {
            foreach (var entry in playerColors)
            {
                colors.Add(entry.Value);
            }
            foreach (var player in FindObjectsByType<PlayerBehaviour>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId == clientId)
                {
                    player.GetComponent<MeshRenderer>().material.color = color;
                    break;
                }
                DesactivarColorsRpc();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void DesactivarColorsRpc()
        {
            Debug.Log(colors.Count);
            LobbyManager.Instance.desactivarColorsJaEscollits(colors);

        }


        [Rpc(SendTo.Everyone)]
        private void AfegirColorLlistRpc(Color color)
        {
            if (!colors.Contains(color))
            {
                colors.Add(color);
                selfColor.Value = color;
                playerColors[OwnerClientId] = color;
                UpdatecolorClientRpc(OwnerClientId, color);
            }
        }

        public void AfegirColorLlist(Color color) => AfegirColorLlistRpc(color);

        public void AfegirReadyPlayer()=>AfegirReadyPlayerRpc();

        [Rpc(SendTo.Server)]
        private void AfegirReadyPlayerRpc()
        {
            LobbyManager.Instance.Ready(OwnerClientId);
        }

        public void TreureReadyPlayer() => TreureReadyPlayerRpc();


        [Rpc(SendTo.Server)]
        private void TreureReadyPlayerRpc()
        {
            LobbyManager.Instance.NotReady(OwnerClientId);
        }

        [Rpc(SendTo.Server)]
        private void RandomKillerRpc()
        {
            int random = UnityEngine.Random.Range(0, NetworkManager.Singleton.ConnectedClientsIds.Count);
            ulong player = 1; //NetworkManager.Singleton.ConnectedClientsIds[random];
            Debug.Log("Player killer: " + player);
            EscogerKillerClientRpc(player);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EscogerKillerClientRpc(ulong id)
        {
            foreach (var player in FindObjectsByType<PlayerBehaviour>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId == id)
                {
                    Debug.Log("KILLER CLIENT :" + id);
                    GameManager.Instance.setKiller(player.gameObject);
                    player.gameObject.layer = 7;
                    isKiller = true;
                    StartCoroutine(player.detectarEnemigos());
                    break;
                }
            }
        }

        private IEnumerator detectarEnemigos()
        {
            while (true)
            {
                enemigos = Physics.OverlapSphere(this.transform.position, 5.0f, layerMask);
                foreach (Collider collider in enemigos)
                {
                    Debug.Log(collider.gameObject.name + collider.GetComponent<PlayerBehaviour>().OwnerClientId);
                }
                Debug.Log("ENEMIGOS:"+enemigos.ToString());
                if (enemigos.Length > 0)
                {
                    GameManager.Instance.ToggleBotonKill(true);
                }
                else
                {
                    GameManager.Instance.ToggleBotonKill(false);
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        public IEnumerator ChangeSceneGame()
        {
            yield return new WaitForSeconds(10f);
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
            NetworkManager.Singleton.SceneManager.LoadScene("Mapa", UnityEngine.SceneManagement.LoadSceneMode.Single);

        }

        private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (sceneName == "Mapa" && IsServer)
            {
                RandomKillerRpc();
            }

            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
        }

        public void CanviarEscena(bool start) => CanviarEscenaRpc(start);

        [Rpc(SendTo.ClientsAndHost)]
        public void CanviarEscenaRpc(bool start)
        {
            if (start)
            {
                StartCoroutine(ChangeSceneGame());
                StartCoroutine(LobbyManager.Instance.ActivarTimer());
            }
            else
            {
                onStopTimer?.Invoke();
                StopAllCoroutines();
            }

        }


        public void MatarEnemigo(ulong id) =>MatarEnemigoRpc(id);

        [Rpc(SendTo.ClientsAndHost)]
        private void MatarEnemigoRpc(ulong id)
        {
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsIds.Count; i++)
            {
                if (NetworkManager.Singleton.ConnectedClientsIds[i] == id)
                {
                    Debug.Log("Activo: " + id);
                    this.m_Rigidbody.isKinematic = true;
                }
            }
        }

    }
}
