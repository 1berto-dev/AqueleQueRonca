using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapCreation : MonoBehaviour
{
    [SerializeField] GameObject[] roomPool;
    [SerializeField] GameObject[] initialRooms;
    [SerializeField] GameObject[] finalRooms;
    [SerializeField] GameObject[] corridorPrefab;

    
    public List<GameObject> instantiatedRooms = new List<GameObject>();


    void Start()
    {
        GameObject initialRoom = Instantiate(initialRooms[Random.Range(0, initialRooms.Length)], Vector3.zero, Quaternion.identity);
        List<Transform> initialDoors = GetDoors(initialRoom);
        foreach (var door in initialDoors)
        {
            // Instanciar o corredor
            GameObject corridor = Instantiate(corridorPrefab[Random.Range(0, corridorPrefab.Length)], door.position, door.rotation);
            corridor.transform.rotation = Quaternion.LookRotation(door.forward, Vector3.up);
            Transform corridorEnd = GetCorridorEnd(corridor);
            Transform corridorStart = GetCorridorStart(corridor);

            FixCorridorPosition(corridor,corridorStart, door);

            Vector3 offset = corridor.transform.position - corridorStart.position;
            corridor.transform.position = door.position + offset;
            corridor.transform.rotation = Quaternion.LookRotation(door.forward, Vector3.up);


            GameObject midRoom = Instantiate(roomPool[Random.Range(0, roomPool.Length)], corridorEnd.position, corridorEnd.rotation);

            RoomPosition(midRoom, corridorEnd);

            List<Transform> midDoors = GetDoors(midRoom);
            for(int i= 1; i < midDoors.Count; i++)
            {
                GameObject corridorMid = Instantiate(corridorPrefab[Random.Range(0, initialRooms.Length)], midDoors[i].position, midDoors[i].rotation);
                corridorMid.transform.rotation = Quaternion.LookRotation(midDoors[i].forward, Vector3.up);
                Transform corridorMidEnd = GetCorridorEnd(corridorMid);
                Transform corridorMidStart = GetCorridorStart(corridorMid);
                FixCorridorPosition(corridorMid, corridorMidStart, midDoors[i]);
                

                GameObject finalRoom = Instantiate(finalRooms[Random.Range(0, finalRooms.Length)], corridorMidEnd.position, corridorMidEnd.rotation);

                RoomPosition(finalRoom, corridorMidEnd);
            }

            
        }
    }
    
    void Update()
    {
       
    }


    private void RoomPosition(GameObject room, Transform corridorEnd)
    {
        Transform door = GetRoomDoor(room);
        
        room.transform.rotation = Quaternion.LookRotation(corridorEnd.forward, Vector3.up);
            
        room.transform.RotateAround(door.position, Vector3.up, Vector3.SignedAngle(door.forward, -corridorEnd.forward, Vector3.up));

        Vector3 offset = room.transform.position - door.position;
            
        room.transform.position = corridorEnd.position + offset;

    }


    private void FixCorridorPosition(GameObject corridor, Transform corridorStart, Transform door)
    {
            Vector3 offset = corridor.transform.position - corridorStart.position;
            corridor.transform.position = door.position + offset;
            corridor.transform.rotation = Quaternion.LookRotation(door.forward, Vector3.up);
        
    }
    // Método para pegar as portas do quarto

    private List<Transform> GetDoors(GameObject room)
    {
        List<Transform> doors = new List<Transform>();
        foreach (Transform child in room.transform)
        {
            if (child.CompareTag("Door")) // Certifique-se de usar a tag "Door" nas portas
            {
                doors.Add(child);
            }
        }
        return doors;
    }

    private Transform GetRoomDoor(GameObject room)
    {
        foreach (Transform child in room.transform)
        {
            if (child.CompareTag("Door")) // Certifique-se de usar a tag "Door" nas portas
            {
                return child;
            }
        }
        return null;
    }
    
    private Transform GetCorridorEnd(GameObject corridor)
    {
        foreach (Transform child in corridor.transform)
        {
            if (child.CompareTag("CorridorEnd")) // Use uma tag ou marcador para o ponto final
            {
                return child;
            }
        }
        return null;
    }
    private Transform GetCorridorStart(GameObject corridor)
    {
        foreach (Transform child in corridor.transform)
        {
            if (child.CompareTag("CorridorBeginning")) // Use uma tag ou marcador para o início do corredor
            {
                return child;
            }
        }
        return null;
    }
}
 