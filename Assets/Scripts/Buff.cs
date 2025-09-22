using Unity.Netcode;
using UnityEngine;

public class Buff : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            int buffAmount = Random.Range(1, 4);
            player.IncreaseAttackServerRpc(buffAmount);
            SimpleDespawn();
        }
    }
    public void SimpleDespawn()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
}