using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public GameObject playerPrefab;
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
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;

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
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        player.GetComponent<Player>().SetData(data);
    }
}