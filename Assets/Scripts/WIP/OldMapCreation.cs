using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyCsg;
using System.Linq;

public class OldMapCreation : MonoBehaviour
{
    [SerializeField] GameObject[] roomPool;
    [SerializeField] GameObject corridorPrefab;

    [Header("Mapa")]
    [SerializeField] int mapSize;
    [SerializeField] int maxAttempts;

    [Header("Quartos")]
    [SerializeField] int RoomMax;
    [SerializeField] int RoomMin;
    [SerializeField] float roomSpacing;
    
    [Header("Corredores")]
    [SerializeField] float corridorHeight;
    [SerializeField] float corridorWidth;
    [SerializeField] LayerMask lookingLayer;


    [SerializeField] float holeHeight;
    [SerializeField] float holeWidth;

    public List<GameObject> instantiatedRooms = new List<GameObject>();
    public List<GameObject> doorHoles = new List<GameObject>();
    public List<GameObject> instantiatedCorridors = new List<GameObject>();

    public List<Collider> collisions = new List<Collider>();

    private List<Vector3> roomCenters = new List<Vector3>();

    private Vector3 lastRoomPosition = Vector3.zero;

    int roomNumber;

    void Start()
    {
        //Cria o quarto inicial
        Vector3 initialPosition = new Vector3(0, 0, 0);
        CreateRoom(initialPosition);
        lastRoomPosition = initialPosition;

        for (int i = 0; i < Random.Range(RoomMin,RoomMax); i++)
        {
            roomNumber = Random.Range(0, roomPool.Length);
            Vector3 position = GetValidPosition();

            // Usando uma posição inválida para verificar falhas
            if (position != new Vector3(-1, -1, -1)) 
            {
                CreateRoom(position);
                
                // Verifica se as posições são diferentes antes de criar o corredor
                if (position != lastRoomPosition)
                {
                    CreateCorridor(lastRoomPosition, position);
                    lastRoomPosition = position;
                }
                else
                {
                    Debug.LogWarning("A posição do quarto gerado é a mesma do anterior. Ignorando a criação do corredor.");
                }
            }
            else
            {
                Debug.LogWarning("Não foi possível encontrar uma posição válida para um novo quarto.");
            }
            
        }
        CreateDoor();
        
    }


    void CreateRoom(Vector3 position)
    {
        GameObject room = Instantiate(roomPool[roomNumber], position, Quaternion.identity);
        roomCenters.Add(position);
        instantiatedRooms.Add(room);
    }

    Vector3 GetValidPosition()
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Gera uma posição aleatória dentro dos limites do mapa
            Vector3 randomPosition = new Vector3(Random.Range(0, mapSize), 0, Random.Range(0, mapSize));

            // Verifica se a posição está livre
            if (IsPositionFree(randomPosition))
            {
                return randomPosition;
            }
        }

        // Se não conseguir encontrar uma posição válida, loga e retorna uma posição inválida
        Debug.LogWarning("Falha ao encontrar uma posição válida após várias tentativas.");
        return new Vector3(-1, -1, -1); // Usando uma posição fora dos limites válidos
    }

    bool IsPositionFree(Vector3 position)
    {
        Transform roomTransform = roomPool[roomNumber].transform;
        
        // Definindo as dimensões aproximadas do quarto
        Vector3 roomSize = new Vector3(roomTransform.localScale.x, roomTransform.localScale.x, roomTransform.localScale.x);
        Vector3 halfExtents = roomSize * roomSpacing / 2;

        // Verifica se a posição está livre usando Physics.CheckBox
        bool isFree = !Physics.CheckBox(position, halfExtents, Quaternion.identity);
        
        if (!isFree)
        {
            Debug.LogWarning($"Posição ocupada: {position}");
        }

        return isFree;
    }

    
    void CreateCorridor(Vector3 from, Vector3 to)
    {
        // Calcula a direção, posição e o comprimento
        Vector3 direction = (to - from).normalized;
        Vector3 corridorPosition = (from + to) / 2f;
        float corridorLength = Vector3.Distance(from, to);

        GameObject corridor = Instantiate(corridorPrefab);
        corridor.transform.position = corridorPosition;

        // Ajusta a escala do corredor (assumindo que o eixo Z é o comprimento)
        corridor.transform.localScale = new Vector3(corridorWidth, corridorHeight, corridorLength/10);
        // Ajusta a rotação do corredor para alinhar na direção correta
        corridor.transform.rotation = Quaternion.LookRotation(direction);

        instantiatedCorridors.Add(corridor); 
        
        GameObject doorHole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        doorHole.transform.position = new Vector3 (corridorPosition.x, holeHeight/2,corridorPosition.z);
        doorHole.transform.localScale = new Vector3(holeWidth, holeHeight, corridorLength);
        doorHole.transform.rotation = Quaternion.LookRotation(direction);
        doorHole.layer = 6;
        doorHole.GetComponent<BoxCollider>().providesContacts = true;
        doorHoles.Add(doorHole); 
    }

    void CreateDoor()
    {
        for(int i = 0; i < instantiatedRooms.Count; i++)
        {    

            instantiatedRooms[i].AddComponent<CutHole>();
                
            
            //Destroy(instantiatedRooms[i]);
        }     
    }
    private void OnDrawGizmos()
    {
        //Gizmos.DrawCube(instantiatedRooms[0].transform.position, instantiatedRooms[0].transform.localScale);
    }
    void Update()
    {
       
    }

    
}
 