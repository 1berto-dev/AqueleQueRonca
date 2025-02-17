using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;
using UnityEditor.ShaderGraph;
using UnityEditor.Search;

public class Generator2D : MonoBehaviour {
    enum CellType {
        None,
        Room,
        Hallway
    }

    class Room {
        public RectInt bounds;

        public Room(Vector2Int location, Vector2Int size) {
            bounds = new RectInt(location, size);
        }

        public static bool Intersect(Room a, Room b) {
            return !((a.bounds.position.x >= (b.bounds.position.x + b.bounds.size.x)) || ((a.bounds.position.x + a.bounds.size.x) <= b.bounds.position.x)
                || (a.bounds.position.y >= (b.bounds.position.y + b.bounds.size.y)) || ((a.bounds.position.y + a.bounds.size.y) <= b.bounds.position.y));
        }
    }

    [SerializeField]
    Vector2Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    Vector2Int roomMaxSize;
    [SerializeField]
    Vector2Int roomMinSize;
    [SerializeField]
    GameObject cubePrefab;
    [SerializeField]
    Material redMaterial;
    [SerializeField]
    Material blueMaterial;

    [Header("")]
    [SerializeField]
    GameObject playerPrefab;
    [SerializeField]
    GameObject cameraPrefab;

    [Header("")]
    [SerializeField]
    GameObject roomWallPrefab; 
    [SerializeField]
    GameObject roomFloorPrefab; 
    [SerializeField]
    GameObject roomDoorPrefab; 
    [SerializeField]
    GameObject hallwayWallPrefab; 
    [SerializeField]
    GameObject hallwayFloorPrefab;
    [SerializeField]
    GameObject key;


    Random random;
    Grid2D<CellType> grid;
    List<Room> rooms;
    Delaunay2D delaunay;
    HashSet<Prim.Edge> selectedEdges;
    HashSet<Vector2Int>[] connectionPoints;
    GameObject player;


    [Header("")]
    [SerializeField] int seed;

    void Start() 
    {
        if(seed == 0)
        seed = System.DateTime.Now.Millisecond;

        Debug.Log(seed);
        random = new Random(seed);
        Generate();
    }

    void Generate() 
    {

        grid = new Grid2D<CellType>(size, Vector2Int.zero);
        rooms = new List<Room>();

        PlaceRooms(); // Gera as salas
        Triangulate(); // Cria a triangulação de Delaunay
        CreateHallways(); // Cria as conexões entre as salas
        PathfindHallways(); // Gera os corredores
        FillGridWithPieces(); // Preenche o grid com peças 3D

        SpawnPlayerInRandomRoom();

        PlaceDoorInFarthestRoom(player.transform.position);
    }

    void FillGridWithPieces() 
    {
        for (int x = 0; x < size.x; x++) 
        {
            for (int y = 0; y < size.y; y++) 
            {
                Vector2Int position = new Vector2Int(x, y);
                CellType cellType = grid[position];

                if (cellType == CellType.Room) 
                {
                    // Instancia o chão da sala
                    Instantiate(roomFloorPrefab, new Vector3(x, 0, y), Quaternion.identity);

                    foreach((int, int, int) direction in Grid2D<CellType>.Directions)
                    {
                        Vector2Int testPos = new Vector2Int(x+direction.Item1, y+direction.Item2);
                        if (testPos.x >= 0 && testPos.x <= size.x - 1 && testPos.y >= 0 && testPos.y <= size.y - 1)
                        {
                            if (grid[testPos] != CellType.Room)
                            {
                                bool contain = false;
                                for(int i = 0; i < selectedEdges.Count; i++)
                                {
                                    if (connectionPoints[i].Contains(position))
                                    {
                                        contain = true;
                                    }
                                }
                                if (!contain)
                                    Instantiate(roomWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, direction.Item3, 0));
                            }
                        }
                    }

                    if (x == 0)
                    {
                        Instantiate(roomWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 90, 0));
                    }

                    if (x == size.x - 1)
                    {
                        Instantiate(roomWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, -90, 0));
                    }


                    if (y == 0)
                    {
                        Instantiate(roomWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 0, 0));
                    }

                    if (y == size.y - 1)
                    {
                        Instantiate(roomWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 180, 0));
                    }
                    // // Verifica as células adjacentes para colocar paredes
                    // if (x > 0 && grid[x - 1, y] != CellType.Room && !connectionPoints.Contains(position)) 
                    // {
                    //     // Parede à esquerda
                    //     Instantiate(roomWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 90, 0));
                    // }
                    // if (x < size.x - 1 && grid[x + 1, y] != CellType.Room && !connectionPoints.Contains(position)) 
                    // {
                    //     // Parede à direita
                    //     Instantiate(roomWallPrefab, new Vector3(x + 1, 0, y), Quaternion.Euler(0, 90, 0));
                    // }
                    // if (y > 0 && grid[x, y - 1] != CellType.Room && !connectionPoints.Contains(position)) 
                    // {
                    //     // Parede abaixo
                    //     Instantiate(roomWallPrefab, new Vector3(x, 0, y), Quaternion.identity);
                    // }
                    // if (y < size.y - 1 && grid[x, y + 1] != CellType.Room && !connectionPoints.Contains(position)) 
                    // {
                    //     // Parede acima
                    //     Instantiate(roomWallPrefab, new Vector3(x, 0, y + 1), Quaternion.identity);
                    // }
                } 
                else if (cellType == CellType.Hallway) 
                {
                    // Instancia o chão do corredor
                    Instantiate(hallwayFloorPrefab, new Vector3(x, 0, y), Quaternion.identity);
                    
                    foreach((int, int, int) direction in Grid2D<CellType>.Directions)
                    {
                        if (x > 0 && x < size.x - 1 && y > 0 && y < size.y - 1)
                        {
                            if (grid[x+direction.Item1, y+direction.Item2] == CellType.None)
                            {
                                Instantiate(hallwayWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, direction.Item3, 0));
                            }
                        }
                        
                    }

                    if (x == 0)
                    {
                        Instantiate(hallwayWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 90, 0));
                    }

                    if (x == size.x - 1)
                    {
                        Instantiate(hallwayWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, -90, 0));
                    }


                    if (y == 0)
                    {
                        Instantiate(hallwayWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 0, 0));
                    }

                    if (y == size.y - 1)
                    {
                        Instantiate(hallwayWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 180, 0));
                    }
                    // // Verifica as células adjacentes para colocar paredes
                    // if (x > 0 && grid[x - 1, y] == CellType.None) 
                    // {
                    //     // Parede à esquerda
                    //     Instantiate(hallwayWallPrefab, new Vector3(x, 0, y), Quaternion.Euler(0, 90, 0));
                    // }
                    // if (x < size.x - 1 && grid[x + 1, y] != CellType.Hallway &&  !connectionPoints.Contains(position)) 
                    // {
                    //     // Parede à direita
                    //     Instantiate(hallwayWallPrefab, new Vector3(x + 1, 0, y), Quaternion.Euler(0, 90, 0));
                    // }
                    // if (y > 0 && grid[x, y - 1] != CellType.Hallway &&  !connectionPoints.Contains(position)) 
                    // {
                    //     // Parede abaixo
                    //     Instantiate(hallwayWallPrefab, new Vector3(x, 0, y), Quaternion.identity);
                    // }
                    // if (y < size.y - 1 && grid[x, y + 1] != CellType.Hallway && !connectionPoints.Contains(position)) 
                    // {
                    //     // Parede acima
                    //     Instantiate(hallwayWallPrefab, new Vector3(x, 0, y + 1), Quaternion.identity);
                    // }
                }
            }
        }
    }

    void PlaceRooms() {
        for (int i = 0; i < roomCount; i++) {
            Vector2Int location = new Vector2Int(
                random.Next(0, size.x),
                random.Next(0, size.y)
            );

            Vector2Int roomSize = new Vector2Int(
                random.Next(roomMinSize.x, roomMaxSize.x + 1),
                random.Next(roomMinSize.y, roomMaxSize.y + 1)
            );

            bool add = true;
            Room newRoom = new Room(location, roomSize);
            Room buffer = new Room(location + new Vector2Int(-1, -1), roomSize + new Vector2Int(2, 2));

            foreach (var room in rooms) {
                if (Room.Intersect(room, buffer)) {
                    add = false;
                    break;
                }
            }

            if (newRoom.bounds.xMin < 0 || newRoom.bounds.xMax >= size.x
                || newRoom.bounds.yMin < 0 || newRoom.bounds.yMax >= size.y) {
                add = false;
            }

            if (add) {
                rooms.Add(newRoom);
                //PlaceRoom(newRoom.bounds.position, newRoom.bounds.size);

                foreach (var pos in newRoom.bounds.allPositionsWithin) {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    void Triangulate() 
    {
        List<Vertex> vertices = new List<Vertex>();

        foreach (var room in rooms) {
            vertices.Add(new Vertex<Room>((Vector2)room.bounds.position + ((Vector2)room.bounds.size) / 2, room));
        }

        delaunay = Delaunay2D.Triangulate(vertices);
    }

    void CreateHallways() 
    {
        List<Prim.Edge> edges = new List<Prim.Edge>();

        foreach (var edge in delaunay.Edges) 
        {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> mst = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(mst);
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        // remainingEdges.ExceptWith(selectedEdges);
        
        // Garantir que cada sala tenha pelo menos uma conexão
        foreach (var room in rooms) 
        {
            bool hasConnection = false;
            foreach (var edge in selectedEdges)
            {
                if ((edge.U as Vertex<Room>).Item == room || (edge.V as Vertex<Room>).Item == room) {
                    hasConnection = true;
                    break;
                }
            }

            if (!hasConnection) {
                Debug.LogWarning($"Sala em {room.bounds.position} não está conectada. Adicionando conexão manualmente.");

                // Encontra a sala mais próxima para conectar
                Room closestRoom = null;
                float minDistance = float.MaxValue;

                foreach (var otherRoom in rooms) 
                {
                    if (otherRoom == room) continue;

                    float distance = Vector2.Distance(room.bounds.center, otherRoom.bounds.center);
                    if (distance < minDistance) 
                    {
                        minDistance = distance;
                        closestRoom = otherRoom;
                    }
                }

                // Adiciona uma aresta entre a sala desconectada e a sala mais próxima
                if (closestRoom != null) 
                {
                    var newEdge = new Prim.Edge(
                        new Vertex<Room>(room.bounds.center, room),
                        new Vertex<Room>(closestRoom.bounds.center, closestRoom)
                    );
                    selectedEdges.Add(newEdge);
                }
            }
        }

        // Adiciona arestas extras aleatoriamente (opcional)
        remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (var edge in remainingEdges) 
        {
            if (random.NextDouble() < 0.125) 
            {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways() 
    {
        DungeonPathfinder2D aStar = new DungeonPathfinder2D(size);
        connectionPoints = new HashSet<Vector2Int>[selectedEdges.Count];
        int j = 0;
        foreach (var edge in selectedEdges) {
            connectionPoints[j] = new HashSet<Vector2Int>();
            var startRoom = (edge.U as Vertex<Room>).Item;
            var endRoom = (edge.V as Vertex<Room>).Item;

            var startPosf = startRoom.bounds.center;
            var endPosf = endRoom.bounds.center;
            var startPos = new Vector2Int((int)startPosf.x, (int)startPosf.y);
            var endPos = new Vector2Int((int)endPosf.x, (int)endPosf.y);

            var path = aStar.FindPath(startPos, endPos, (DungeonPathfinder2D.Node a, DungeonPathfinder2D.Node b) => {
                var pathCost = new DungeonPathfinder2D.PathCost();
                
                pathCost.cost = Vector2Int.Distance(b.Position, endPos); 

                if (grid[b.Position] == CellType.Room) {
                    pathCost.cost += 10;
                } else if (grid[b.Position] == CellType.None) {
                    pathCost.cost += 5;
                } else if (grid[b.Position] == CellType.Hallway) {
                    pathCost.cost += 1;
                }

                pathCost.traversable = true;

                return pathCost;
            });

            if (path != null) {
                for (int i = path.Count-1; i >= 0 ; i--) 
                {
                    var current = path[i];

                    if (grid[current] == CellType.None) 
                    {
                        grid[current] = CellType.Hallway;
                    }

                    if (i < path.Count-1) {
                        var prev = path[i + 1];
                        var delta = current - prev;

                        // Adiciona a posição da conexão entre quarto e corredor
                        if (grid[prev] == CellType.Room) {
                            connectionPoints[j].Add(prev); // Adiciona a posição da sala (entrada)
                        }
                    }
                }
            }
            j++;
        }
        
        // Verifica se todas as salas estão conectadas
        foreach (var room in rooms) 
        {
        bool isConnected = false;
        foreach (var edge in selectedEdges) 
        {
            if ((edge.U as Vertex<Room>).Item == room || (edge.V as Vertex<Room>).Item == room) {
                isConnected = true;
                break;
            }
        }

        if (!isConnected) 
        {
            Debug.LogWarning($"Sala em {room.bounds.position} não está conectada!");
        }
    }
}

    void SpawnPlayerInRandomRoom() 
    {
        if (rooms.Count == 0) 
        {
            Debug.LogWarning("Nenhuma sala foi gerada.");
            return;
        }

        // Escolhe uma sala aleatória
        int randomIndex = random.Next(0, rooms.Count);
        Room spawnRoom = rooms[randomIndex];

        // Obtém a posição central da sala
        Vector2Int spawnPosition = new Vector2Int(
            spawnRoom.bounds.x + spawnRoom.bounds.width / 2,
            spawnRoom.bounds.y + spawnRoom.bounds.height / 2
        );

        // Instancia o jogador na posição central da sala
        player = Instantiate(playerPrefab, new Vector3(spawnPosition.x, 0, spawnPosition.y), Quaternion.identity);
        Instantiate(cameraPrefab);
    }


    void PlaceDoorInFarthestRoom(Vector3 playerSpawnPosition) 
    {
        Room farthestRoom = null;
        float maxDistance = 0;

        // Encontra a sala mais distante da posição do jogador
        foreach (var room in rooms) 
        {
            
            Vector3 roomCenter = new Vector3(room.bounds.center.x, 0, room.bounds.center.y);
            float distance = Vector3.Distance(playerSpawnPosition, roomCenter);

            if (distance > maxDistance) {
                maxDistance = distance;
                farthestRoom = room;
            }
            
        } 

        if (farthestRoom != null) {
            for (int x = 0; x < farthestRoom.bounds.size.x; x++) 
            {
                for (int y = 0; y < farthestRoom.bounds.size.y; y++) 
                {
                    foreach((int, int, int) direction in Grid2D<CellType>.Directions)
                    {
                        Vector2Int testPos = new Vector2Int(farthestRoom.bounds.position.x+x+direction.Item1, farthestRoom.bounds.position.y+y+direction.Item2);
                        
                        if (testPos.x >= 0 && testPos.x < size.x && testPos.y >= 0 && testPos.y < size.y)
                        {
                            if(grid[testPos] == CellType.Hallway)
                            {
                                    for(int i = 0; i < selectedEdges.Count; i++)
                                    {
                                        if (connectionPoints[i].Contains(new Vector2Int(farthestRoom.bounds.position.x+x, farthestRoom.bounds.position.y+y)))
                                        {
                                            Instantiate(roomDoorPrefab, new Vector3(farthestRoom.bounds.position.x+x, 0, farthestRoom.bounds.position.y+y), Quaternion.Euler(0, direction.Item3, 0));
                                        }
                                    }
                            }
                        }
                    }
                    
                }
            }

            // Encontra a sala mais distante para a chave
            Room keyRoom = FindFarthestRoomForKey(playerSpawnPosition, farthestRoom);

            if (keyRoom != null) 
            {
                // Instancia a chave no centro da sala
                Vector3 keyPosition = new Vector3(keyRoom.bounds.center.x, 0, keyRoom.bounds.center.y);
                Instantiate(key, keyPosition, Quaternion.identity);
            }
            
        }
    }

    Room FindFarthestRoomForKey(Vector3 playerSpawnPosition, Room doorRoom) 
    {
        Room farthestRoom = null;
        float maxDistance = 0;

        foreach (var room in rooms) 
        {
            // Ignora a sala de spawn e a sala com a porta
            if (room == doorRoom || room.bounds.center == new Vector2(playerSpawnPosition.x, playerSpawnPosition.z)) 
            {
                continue;
            }

            // Calcula a distância da sala até o spawn do jogador e até a sala com a porta
            Vector3 roomCenter = new Vector3(room.bounds.center.x, 0, room.bounds.center.y);
            float distanceToPlayer = Vector3.Distance(playerSpawnPosition, roomCenter);
            float distanceToDoor = Vector3.Distance(new Vector3(doorRoom.bounds.center.x, 0, doorRoom.bounds.center.y), roomCenter);

            // Usa a média das distâncias para encontrar a sala mais distante de ambos
            float averageDistance = (distanceToPlayer + distanceToDoor) / 2;

            if (averageDistance > maxDistance) 
            {
                maxDistance = averageDistance;
                farthestRoom = room;
            }
        }

        return farthestRoom;
    }

}
