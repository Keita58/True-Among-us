using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

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
        NetworkList<Color> colors = new NetworkList<Color>();
        NetworkVariable<Color> selfColor= new NetworkVariable<Color>();
        private static Dictionary<ulong, Color> playerColors = new();
        bool isKiller=false;

        //per a saber quins clients estàn preparats.
        //per a fer la llista privada.
        [SerializeField] public Collider[] enemigos { get; private set; }

        Vector2 _LookRotation = Vector2.zero;
        [SerializeField] LayerMask layerMask;

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

            //Si no la podem updatejar, com ho fem aleshores?
            //Li demanem al servidor que ho faci via un RPC
            //Per a mostrar el resultat al nostre client, utilitzarem
            //els callback de modificaci�
            m_Speed.OnValueChanged += CallbackModificacio;

            colors.OnListChanged += (NetworkListEvent<Color> changeEvent) =>
            {
                List<Color> aux = new List<Color>();
                foreach (Color color in colors)
                {
                    aux.Add(color);
                }
                LobbyManager.Instance.desactivarColorsJaEscollits(aux);
            };

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
            m_Speed.OnValueChanged -= CallbackModificacio;
        }

        //Sempre tindran aquest format amb oldValue i newValue
        private void CallbackModificacio(float oldValue, float newValue)
        {
            Debug.Log($"{OwnerClientId} => M'han modificat la velocitat i ara �s {newValue} en comptes de {oldValue}");
        }

        void Update()
        {
            //Aquest update nom�s per a qui li pertany
            if (!IsOwner)
            {
                return;
            }

            //RPC a servidor
            //Demanem al servidor que modifiqui la variable. Perqu� nosaltres no en som els propietaris
            if (Input.GetKeyDown(KeyCode.Space))
                ChangeSpeedRpc(UnityEngine.Random.Range(1f, 5f), NetworkObjectId);

            //RPC a clients
            //Com a servidor, enviem un missatge als nostres clients.
            //Si es vol, es pot passar com a par�metre els ClientRpcParams, del qual al seu send
            //es poden posar les ID de client que volem que rebin el missatge.
            //Alerta, aquest codi nom�s el pot invocar el servidor i far� que s'executi a tots els clients
            if (Input.GetKeyDown(KeyCode.M))
                SendClientMessageClientRpc("Aix� �s un missatge pels clients");

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
            

            ////Debug.Log("Clients: " + NetworkManager.Singleton.ConnectedClientsList.Count);
            //foreach (ulong client in m_PlayersReady)
            //{
            //    Debug.Log("ClientReady:"+client);
            //}
            //Debug.Log("Clients ready: " + m_PlayersReady.Count);

            if (!GetComponentInChildren<Canvas>().GetComponentInChildren<TextMeshProUGUI>().text.Equals("") && SceneManager.GetActiveScene().name.Equals("Lobby")){
                LobbyManager.Instance.ActivarBotonReady();
            }
        }

        //No funciona pel mousePosition!!!
        [Rpc(SendTo.Server)]
        private void MovimentCameraRpc(Vector2 lookInput)
        {
            Vector2 _LookRotation = Vector2.zero;

            _LookRotation.x += lookInput.x;
            _LookRotation.y += -1 * lookInput.y;

            _LookRotation.y = Mathf.Clamp(_LookRotation.y, -20, 60);

            m_Rigidbody.MoveRotation(Quaternion.Euler(0, _LookRotation.x, 0));
            //Debug.Log($"{NetworkBehaviourId} -> " + lookInput);
        }

        //De nou, nom�s data i tipus base com a par�metres.
        //hem afegit el segon par�metre per a saber qui ens d�na la comanda (no es necessari per a aquest exemple, per� potser us �s �til)
        [Rpc(SendTo.Server)]
        private void ChangeSpeedRpc(float speed, ulong sourceNetworkObjectId)
        {
            //Ens assegurem com a servidor que la velocitat que ens demanen �s v�lida (no negativa en aquest cas)
            m_Speed.Value = Mathf.Abs(speed);
            Debug.Log($"{OwnerClientId} => El client {sourceNetworkObjectId} vol una nova velocitat {m_Speed.Value}");

            //Aquesta resposta nom�s la rebr� el client amb la id inicial
            //Client RPC target a nom�s les ID indicades
            SendClientMessageClientRpc("Aix� �s un missatge pel client 1" + sourceNetworkObjectId,
                        new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] { sourceNetworkObjectId }
                            }
                        }
                    );
        }

        //Com veiem, controlar el moviment per servidor �s impracticable si hem de treballar amb deltaTime
        //ja que no podem pretendre que la velocitat d'un update es propagui per la xarxa, on sempre tindrem
        //un enrederiment.
        //En aquest cas tenim dues opcions:
        //  A) Podem dir-li als clients que puguin modificar les seves transform.
        //      �s una soluci�, per� estem donant autoritat al client i aix� en xarxa no �s recomanable.
        //
        //  B) Ad�u deltaTime i treballem per f�siques amb valors a la velocitat o acceleracions
        //      els c�lculs els seguiria fent el servidor via rpc (caldria el component NetworkRigidbody/2d)
        //      seguirem tenint delay, per� ser� m�s acceptable.
        //      Nota: Quan afegim el NetworkRigidbody, aquest passa a ser kinematic a no ser que estigui al servidor
        //          caldria aleshores gestionar totes les col�lisions i d'altre al servidor.
        [Rpc(SendTo.Server)]
        private void MoveCharacterRpc(Vector3 newPosition)
        {
            transform.position = newPosition;
        }

        //Aix� seria el moviment a f�sica
        [Rpc(SendTo.Server)]
        private void MoveCharacterPhysicsServerRpc(Vector3 velocity, Vector2 lookInput)
        {
            _LookRotation.x += lookInput.x;

            m_Rigidbody.MoveRotation(Quaternion.Euler(0, _LookRotation.x, 0));
            //Debug.Log($"{OwnerClientId} -> " + lookInput);

            m_Rigidbody.linearVelocity = velocity;
        }

        //Funci� que nom�s ser� executada als clients
        // Noteu que aqu� es fa servir una nomenclatura diferent [ClientRpc] en comptes de [Rpc(SendTo.ClientsAndHost)].
        // Aix� �s degut al fet que estem afegint els ClientRpcParams (per a enviar nom�s a alguns clients o a tothom).
        // Aquest tipus d'�s d'arguments nom�s es pot fer amb [ClientRpc]. En qualsevol altre cas, utilitzeu la sintaxi
        // que pertocaria [Rpc(SendTo.ClientsAndHost)].
        [ClientRpc]
        private void SendClientMessageClientRpc(string message, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"El servidor ha enviat un missatge al client {OwnerClientId} => {message}");
        }

        private void OnClientConnected(ulong clientId)
        {
            // Enviar el nombre del jugador al nuevo cliente
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
            foreach (var player in FindObjectsByType<PlayerBehaviour>(FindObjectsSortMode.None))
            {
                if (player.OwnerClientId == clientId)
                {
                    player.GetComponent<MeshRenderer>().material.color = color;
                    break;
                }
            }

        }

        public void CanviColor()
        {
            List<Color> aux = new List<Color>();
            foreach (Color c in colors)
            {
                aux.Add(c);
            }
            LobbyManager.Instance.desactivarColorsJaEscollits(aux);
        }

        [Rpc(SendTo.Server)]
        private void AfegirColorLlistRpc(Color color)
        {
            if (!colors.Contains(color))
            {
                selfColor.Value = color;
                playerColors[OwnerClientId] = color;
                UpdatecolorClientRpc(OwnerClientId, color);
                CanviColor();
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
            ulong player = NetworkManager.Singleton.ConnectedClientsIds[random];
            Debug.Log("Player killer: " + player);
            EscogerKillerRpc(player);
            StartCoroutine(detectarEnemigos());
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void EscogerKillerRpc(ulong id)
        {
            if (id == OwnerClientId)
            {
                GameManager.Instance.setKiller(this.gameObject);
                this.gameObject.layer = 7;

                isKiller = true;
            }
        }


        private IEnumerator detectarEnemigos()
        {
            while (true)
            {
                enemigos = Physics.OverlapSphere(this.transform.position, 5.0f, layerMask);
                foreach (Collider collider in enemigos)
                {
                    Debug.Log(collider.gameObject.name);
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
                yield return new WaitForSeconds(2f);
            }
        }

        public IEnumerator ChangeSceneGame()
        {
            yield return new WaitForSeconds(2f);
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

        public void CanviarEscena()
        {
            //Debug.Log($"NetworkManager: {NetworkManager.Singleton.ConnectedClientsList.Count} PlayersReady: {m_PlayersReady.Count}");
            StartCoroutine(ChangeSceneGame());
          
        }

        public void MatarEnemigo(ulong id) =>MatarEnemigoRpc(id);

        [Rpc(SendTo.Server)]
        private void MatarEnemigoRpc(ulong id)
        {
            if (OwnerClientId == id)
            {
                GameManager.Instance.Matar();

            }
        }
    }
}
