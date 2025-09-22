using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public GameObject playerPrefab;
    public GameObject buffPrefab;

    public static GameManager Instance;
    public Dictionary<string, PlayerData> playerStateByAccountID = new();

    public Action OnConnection;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
            StartCoroutine(SpawnBuffs());
        }

        OnConnection?.Invoke();
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
    }
    private void HandleDisconnect(ulong clientID)
    {
        print("El jugador" + clientID + "Se a desconectado");
    }
    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string accoundID, ulong ID)
    {
        if (!playerStateByAccountID.TryGetValue(accoundID, out PlayerData data))
        {
            PlayerData newData = new PlayerData(accoundID, Vector3.zero, 100, 5);
            playerStateByAccountID[accoundID] = newData;
            SpawnPlayerServer(ID, newData);
        }
        else
        {
            SpawnPlayerServer(ID, data);
        }
    }
    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ID, out var client))
        {
            if (client.PlayerObject != null) 
                return;
        }
        GameObject player = Instantiate(playerPrefab, data.position, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        player.GetComponent<Player>().SetData(data);
    }
    IEnumerator SpawnBuffs()
    {
        while (true)
        {
            Vector3 newPos = new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
            GameObject buff = Instantiate(buffPrefab, newPos, Quaternion.identity);
            buff.GetComponent<NetworkObject>().Spawn();

            yield return new WaitForSeconds(10f);
        }
    }
}