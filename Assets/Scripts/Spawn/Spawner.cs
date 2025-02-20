using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    [SerializeField] private GameObject _Jugador;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            transform.position = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * Random.Range(0f, 5f);

        if (!IsOwner)
            return;
    }

    public void Update()
    {
        if (!IsOwner)
            return;

        //Aquest script funciona perqu s al player
        //Noms podeu efectuar RPC des d'un objecte que tingueu
        //ownership
        if (Input.GetKeyDown(KeyCode.G))
            SpawnRpc();
    }

    [Rpc(SendTo.Server)]
    private void SpawnRpc()
    {
        Debug.Log("Spawning on server");

        //Aix crea l'objecte (com ja haurieu de saber, estem a UF4, cal guardar una referncia a l'objecte instanciat)
        GameObject cat = Instantiate(_Jugador);
        cat.transform.position = transform.position + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * Random.Range(0f, 2f);
        //Aix instancia l'objecte per la xarxa, i d'aquesta forma apareixer tamb als altres clients connectats.
        cat.GetComponent<NetworkObject>().Spawn();
        //cat.GetComponent<GeroColorController>().ColorChangeRpc(color);
    }

}
