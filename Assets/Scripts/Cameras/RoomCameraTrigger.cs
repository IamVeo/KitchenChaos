using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(Collider))]
public class RoomCameraTrigger : MonoBehaviour
{
    [Tooltip("Assign the Virtual Camera for this specific room.")]
    [SerializeField] private CinemachineVirtualCamera mainRoomCamera;
    [SerializeField] private List<CinemachineVirtualCamera> othersCameraList;
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            mainRoomCamera.Priority = 10; 
            foreach (var cam in othersCameraList)           
                cam.Priority = 0; 
        }
    }
}