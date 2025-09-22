using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public CinemachineCamera virtualCamera;

    public Transform target;

    public void SetTarget(Transform player)
    {
        target = player;
        virtualCamera.Follow = target;
    }
}
