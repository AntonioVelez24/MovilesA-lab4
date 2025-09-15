using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accountID = new();
    public NetworkVariable<int> health = new();
    public NetworkVariable<int> attack = new();

    public float speed = 5;

    public GameObject projectilePrefab;
    public Transform firePoint;

    public void SetData(PlayerData playerData)
    {
        accountID.Value = playerData.accountID;
        health.Value = playerData.health;
        attack.Value = playerData.attack;
        transform.position = playerData.position;
    }
    public override void OnNetworkDespawn()
    {
        GameManager.Instance.playerStateByAccountID[accountID.Value.ToString()] = new PlayerData(accountID.ToString(), transform.position, health.Value, attack.Value);
        print("Jugador se ha desconectado: " + NetworkManager.Singleton.LocalClientId + ", y se ha guardado la data de: " + accountID.Value);
    }
    public void Update()
    {
        if (!IsOwner) return;

        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            float VelX = Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime;
            float VelY = Input.GetAxisRaw("Vertical") * speed * Time.deltaTime;
            transform.position += new Vector3(VelX, 0, VelY);
        }

        RotatePlayerTowardsMovement();

        if (Input.GetMouseButtonDown(0))
        {
            ShootRpc();
        }
    }

    public void RotatePlayerTowardsMovement()
    {
        Vector3 moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 6f * Time.deltaTime);
        }
    }


    [Rpc(SendTo.Server)]
    public void ShootRpc()
    {
        Vector3 direction = firePoint.transform.forward;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        proj.GetComponent<NetworkObject>().Spawn(true);
        proj.GetComponent<Rigidbody>().AddForce(direction * 5, ForceMode.Impulse);

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            health.Value -= 1;
            Debug.Log("Vida del jugador: " + health.Value);
        }
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