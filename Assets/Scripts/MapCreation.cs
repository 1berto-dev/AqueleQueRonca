using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;
using UnityEngine;

public class MapCreation : MonoBehaviour
{
    [SerializeField] GameObject[] roomPool;
    [SerializeField] GameObject[] initialRooms;
    [SerializeField] GameObject[] finalRooms;
    [SerializeField] GameObject[] corridorPrefab;

    [SerializeField] LayerMask roomLayer;


    [SerializeField] GameObject closeDoor;
    [SerializeField] GameObject key;
    [SerializeField] GameObject stone;

    
    public List<GameObject> instantiatedRooms = new List<GameObject>();


    void Start()
    {
        // Instancia o quarto inicial
        GameObject initialRoom = Instantiate(initialRooms[Random.Range(0, initialRooms.Length)], Vector3.zero, Quaternion.identity);
        // Salva uma lista das portas do quarto
        List<Transform> initialDoors = GetDoors(initialRoom);

        instantiatedRooms.Add(initialRoom);

        foreach (var door in initialDoors)
        {
            // Instancia um corredor
            GameObject corridor = Instantiate(corridorPrefab[Random.Range(0, corridorPrefab.Length)], door.position, door.rotation);
            corridor.transform.rotation = Quaternion.LookRotation(door.forward, Vector3.up);
            Transform corridorEnd = GetCorridorEnd(corridor);
            Transform corridorStart = GetCorridorStart(corridor);

            FixCorridorPosition(corridor,corridorStart, door);

            Vector3 offset = corridor.transform.position - corridorStart.position;
            corridor.transform.position = door.position + offset;
            corridor.transform.rotation = Quaternion.LookRotation(door.forward, Vector3.up);


            GameObject midRoom = Instantiate(roomPool[Random.Range(0, roomPool.Length)], corridorEnd.position, corridorEnd.rotation);
            instantiatedRooms.Add(midRoom);
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
                finalRoom.GetComponent<Collider>().enabled = false;
                if(!Physics.CheckBox(finalRoom.transform.localPosition, finalRoom.transform.localScale, Quaternion.identity, roomLayer))
                {
                    
                    finalRoom.GetComponent<Collider>().enabled = true;
                    instantiatedRooms.Add(finalRoom);
                }
                else
                {
                    Destroy(corridorMid);
                    Instantiate(stone, midDoors[i].position, midDoors[i].rotation);
                    Destroy(finalRoom);
                }
            }

            
        }

        ChooseFinalRoom();
    }

    private void ChooseFinalRoom()
    {
        GameObject firstRoom = instantiatedRooms[0];
        GameObject finalRoom = null;
        GameObject keyRoom = null;
        float maxDistance = 0;

        foreach(GameObject room in instantiatedRooms)
        {
            
            float currentDistance = (firstRoom.transform.position - room.transform.position).magnitude;

            if(currentDistance > maxDistance)
            {
                maxDistance = currentDistance;
                finalRoom = room;
            }

        }

        if (finalRoom != null)
        {
            Transform door = GetRoomDoor(finalRoom);
            Instantiate(closeDoor, door.position, door.rotation);
            
            foreach(GameObject room in instantiatedRooms)
            {
                
                float currentDistance = (finalRoom.transform.position - room.transform.position).magnitude;

                if(currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    keyRoom = room;
                }

            }
            if(keyRoom != null)
            {
                Instantiate(key, keyRoom.transform.position, keyRoom.transform.rotation);
            }
        }
        
        
        
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
    
    private List<Transform> GetDoors(GameObject room)
    {
        List<Transform> doors = new List<Transform>();
        foreach (Transform child in room.transform)
        {
            if (child.CompareTag("Door")) 
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
            if (child.CompareTag("Door"))
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
            if (child.CompareTag("CorridorEnd")) 
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
            if (child.CompareTag("CorridorBeginning"))
            {
                return child;
            }
        }
        return null;
    }
}
 