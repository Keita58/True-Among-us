using Unity.Netcode;
using UnityEngine;

namespace m17
{
    public class PlayerBehaviour : NetworkBehaviour
    {
        //Variable senzilla, per defecte el client no la pot updatejar
        //recordem que aquestes variables només poden ser de DATA
        //(a no ser que us feu la vostra propia adaptació)
        NetworkVariable<float> m_Speed = new NetworkVariable<float>(1);

        Rigidbody2D m_Rigidbody;

        // No es recomana fer servir perquè estem en el món de la xarxa
        // però podem per initialitzar components i variables per a totes les instàncies
        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody2D>();
        }

        // Això sí que seria el nou awake
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //Aquest awake només per a qui li pertany, perquè tocarem variables on només
            //a nosaltres ens interessa llegir el seu valor
            if (!IsOwner)
                return;

            //Si no la podem updatejar, com ho fem aleshores?
            //Li demanem al servidor que ho faci via un RPC
            //Per a mostrar el resultat al nostre client, utilitzarem
            //els callback de modificació
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
            Debug.Log($"{OwnerClientId} => M'han modificat la velocitat i ara és {newValue} en comptes de {oldValue}");
        }

        void Update()
        {
            //Aquest update només per a qui li pertany
            if (!IsOwner)
                return;

            //RPC a servidor
            //Demanem al servidor que modifiqui la variable. Perquè nosaltres no en som els propietaris
            if (Input.GetKeyDown(KeyCode.Space))
                ChangeSpeedRpc(Random.Range(1f, 5f), NetworkObjectId);

            //RPC a clients
            //Com a servidor, enviem un missatge als nostres clients.
            //Si es vol, es pot passar com a paràmetre els ClientRpcParams, del qual al seu send
            //es poden posar les ID de client que volem que rebin el missatge.
            //Alerta, aquest codi només el pot invocar el servidor i farà que s'executi a tots els clients
            if (Input.GetKeyDown(KeyCode.M))
                SendClientMessageClientRpc("Això és un missatge pels clients");

            //moviment a deltaTime
            Vector3 movement = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                movement += Vector3.up;
            if (Input.GetKey(KeyCode.S))
                movement -= Vector3.up;
            if (Input.GetKey(KeyCode.A))
                movement -= Vector3.right;
            if (Input.GetKey(KeyCode.D))
                movement += Vector3.right;

            //Qui farà els moviments serà el servidor, alleugerim i només canvis quan hi hagi input
            if (movement != Vector3.zero)
                MoveCharacterRpc(transform.position + movement.normalized * m_Speed.Value * Time.deltaTime);

            //moviment a física
            movement = Vector3.zero;

            if (Input.GetKey(KeyCode.I))
                movement += Vector3.up;
            if (Input.GetKey(KeyCode.K))
                movement -= Vector3.up;
            if (Input.GetKey(KeyCode.J))
                movement -= Vector3.right;
            if (Input.GetKey(KeyCode.L))
                movement += Vector3.right;

            //Qui farà els moviments serà el servidor, alleugerim i només canvis quan hi hagi input
            MoveCharacterPhysicsServerRpc(movement.normalized * m_Speed.Value);

        }

        //De nou, només data i tipus base com a paràmetres.
        //hem afegit el segon paràmetre per a saber qui ens dóna la comanda (no es necessari per a aquest exemple, però potser us és útil)
        [Rpc(SendTo.Server)]
        private void ChangeSpeedRpc(float speed, ulong sourceNetworkObjectId)
        {
            //Ens assegurem com a servidor que la velocitat que ens demanen és vàlida (no negativa en aquest cas)
            m_Speed.Value = Mathf.Abs(speed);
            Debug.Log($"{OwnerClientId} => El client {sourceNetworkObjectId} vol una nova velocitat {m_Speed.Value}");

            //Aquesta resposta només la rebrà el client amb la id inicial
            //Client RPC target a només les ID indicades
            SendClientMessageClientRpc("Això és un missatge pel client 1" + sourceNetworkObjectId,
                        new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new ulong[] { sourceNetworkObjectId }
                            }
                        }
                    );
        }

        //Com veiem, controlar el moviment per servidor és impracticable si hem de treballar amb deltaTime
        //ja que no podem pretendre que la velocitat d'un update es propagui per la xarxa, on sempre tindrem
        //un enrederiment.
        //En aquest cas tenim dues opcions:
        //  A) Podem dir-li als clients que puguin modificar les seves transform.
        //      És una solució, però estem donant autoritat al client i això en xarxa no és recomanable.
        //
        //  B) Adéu deltaTime i treballem per físiques amb valors a la velocitat o acceleracions
        //      els càlculs els seguiria fent el servidor via rpc (caldria el component NetworkRigidbody/2d)
        //      seguirem tenint delay, però serà més acceptable.
        //      Nota: Quan afegim el NetworkRigidbody, aquest passa a ser kinematic a no ser que estigui al servidor
        //          caldria aleshores gestionar totes les col·lisions i d'altre al servidor.
        [Rpc(SendTo.Server)]
        private void MoveCharacterRpc(Vector3 newPosition)
        {
            transform.position = newPosition;
        }

        //Això seria el moviment a física
        [Rpc(SendTo.Server)]
        private void MoveCharacterPhysicsServerRpc(Vector3 velocity)
        {
            m_Rigidbody.velocity = velocity;
        }

        //Funció que només serà executada als clients
        // Noteu que aquí es fa servir una nomenclatura diferent [ClientRpc] en comptes de [Rpc(SendTo.ClientsAndHost)].
        // Això és degut al fet que estem afegint els ClientRpcParams (per a enviar només a alguns clients o a tothom).
        // Aquest tipus d'ús d'arguments només es pot fer amb [ClientRpc]. En qualsevol altre cas, utilitzeu la sintaxi
        // que pertocaria [Rpc(SendTo.ClientsAndHost)].
        [ClientRpc]
        private void SendClientMessageClientRpc(string message, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"El servidor ha enviat un missatge al client {OwnerClientId} => {message}");
        }
    }
}
