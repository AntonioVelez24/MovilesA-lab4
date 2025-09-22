using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accountID = new();
    public NetworkVariable<int> health = new();
    public NetworkVariable<int> attack = new();

    public float speed = 5f;

    public GameObject projectilePrefab;
    public Transform firePoint;

    public void SetData(PlayerData playerData)
    {
        accountID.Value = playerData.accountID;
        health.Value = playerData.health;
        attack.Value = playerData.attack;
        transform.position = playerData.position;
    }

    private void Start()
    {
        if (IsOwner)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                cam.SetTarget(transform);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner && IsClient && !IsServer)
        {
            RegisterWithServer();
        }
    }

    void RegisterWithServer()
    {
        string myAccountID = "Player_" + OwnerClientId;
        GameManager.Instance.RegisterPlayerServerRpc(myAccountID, OwnerClientId);
    }

    public override void OnNetworkDespawn()
    {
        GameManager.Instance.playerStateByAccountID[accountID.Value.ToString()] = new PlayerData(accountID.ToString(), transform.position, health.Value, attack.Value);
        Debug.Log("Jugador se ha desconectado: " + NetworkManager.Singleton.LocalClientId + ", data guardada: " + accountID.Value);
    }

    private void Update()
    {
        if (!IsOwner) return;

        float moveX = Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime;
        float moveZ = Input.GetAxisRaw("Vertical") * speed * Time.deltaTime;
        transform.position += new Vector3(moveX, 0, moveZ);

        RotatePlayerTowardsMovement();

        if (Input.GetMouseButtonDown(0))
        {
            ShootServerRpc();
        }
    }

    private void RotatePlayerTowardsMovement()
    {
        Vector3 moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 6f * Time.deltaTime);
        }
    }

    [ServerRpc]
    public void ShootServerRpc()
    {
        Vector3 direction = firePoint.transform.forward;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        proj.GetComponent<NetworkObject>().Spawn(true);
        proj.GetComponent<Rigidbody>().AddForce(direction * 5f, ForceMode.Impulse);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            TakeDamageServerRpc(attack.Value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount)
    {
        health.Value -= amount;
        ShowHealthClientRpc(health.Value);

        if (health.Value <= 0)
        {
            PlayerRespawnRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void PlayerRespawnRpc()
    {
        Vector3 randomPosition = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
        transform.position = randomPosition;
        RestoreHealthServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc()
    {
        health.Value = 100;
        ShowHealthClientRpc(health.Value);
    }
    [ClientRpc]
    public void ShowHealthClientRpc(int currentHealth)
    {
        if (IsOwner)
        {
            Debug.Log("Vida del jugador: " + currentHealth);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncreaseAttackServerRpc(int amount)
    {
        attack.Value += amount;
        Debug.Log("Ataque aumentado a: " + attack.Value);
    }
}

public class PlayerData
{
    public string accountID;
    public Vector3 position;
    public int health;
    public int attack;

    public PlayerData(string id, Vector3 pos, int hp, int atk)
    {
        accountID = id;
        position = pos;
        health = hp;
        attack = atk;
    }
}