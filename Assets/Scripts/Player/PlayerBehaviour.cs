using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace m17
{
    public class PlayerBehaviour : NetworkBehaviour
    {
        //Variable senzilla, per defecte el client no la pot updatejar
        //recordem que aquestes variables nom�s poden ser de DATA
        //(a no ser que us feu la vostra propia adaptaci�)
        NetworkVariable<float> m_Speed = new NetworkVariable<float>(1);

        NetworkVariable<FixedString512Bytes> nick = new NetworkVariable<FixedString512Bytes>();

        Rigidbody m_Rigidbody;
        [SerializeField] GameObject _Camera;
        NetworkList<Color> colors = new NetworkList<Color>();

        // No es recomana fer servir perqu� estem en el m�n de la xarxa
        // per� podem per initialitzar components i variables per a totes les inst�ncies
        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        // Aix� s� que seria el nou awake
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //Aquest awake nom�s per a qui li pertany, perqu� tocarem variables on nom�s
            //a nosaltres ens interessa llegir el seu valor
            if (!IsOwner || !IsClient)
                return;


            LobbyManager.Instance.SetPlayer(this.gameObject);

            Camera.main.transform.position = _Camera.transform.position;
            Camera.main.transform.parent = this.transform;

            //Si no la podem updatejar, com ho fem aleshores?
            //Li demanem al servidor que ho faci via un RPC
            //Per a mostrar el resultat al nostre client, utilitzarem
            //els callback de modificaci�
            m_Speed.OnValueChanged += CallbackModificacio;
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
            if (!IsOwner || !IsClient)
            {
                return;
            }

            //RPC a servidor
            //Demanem al servidor que modifiqui la variable. Perqu� nosaltres no en som els propietaris
            if (Input.GetKeyDown(KeyCode.Space))
                ChangeSpeedRpc(Random.Range(1f, 5f), NetworkObjectId);

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
            MoveCharacterPhysicsServerRpc(movement.normalized * m_Speed.Value);
            MovimentCameraRpc(Input.mousePosition);
        }

        [Rpc(SendTo.Server)]
        private void MovimentCameraRpc(Vector2 lookInput)
        {
            Vector2 _LookRotation = Vector2.zero;

            _LookRotation.x += lookInput.x;
            _LookRotation.y += lookInput.y;

            _LookRotation.y = Mathf.Clamp(_LookRotation.y, -35, 35);

            m_Rigidbody.MoveRotation(Quaternion.Euler(0, _LookRotation.x, 0));
            _Camera.transform.localRotation = Quaternion.Euler(_LookRotation.y, 0, 0);
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
        private void MoveCharacterPhysicsServerRpc(Vector3 velocity)
        {
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

        [Rpc(SendTo.Server)]
        public void CanviNomRpc(string nom)
        {
            Debug.Log("entro");
            GetComponentInChildren<Canvas>().GetComponentInChildren<TextMeshProUGUI>().text = nom;
            nick.Value = nom;
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void CanviColorRpc(Color color)
        {
            GetComponent<MeshRenderer>().material.color= color;
            AfegirColorLlistServerRpc(color);
        }


        [ServerRpc(RequireOwnership = false)]
        public void AfegirColorLlistServerRpc(Color color)
        {
            if (!colors.Contains(color))
            {
                colors.Add(color);
                Debug.Log("Añado " + color);
                Debug.Log(colors.Count);
            }
        }

        public List<Color> llistaColors()
        {
            List<Color> aux = new List<Color>();
            foreach (Color color in colors)
            {
                aux.Add(color);
            }
            return aux;
        }

    }
}
