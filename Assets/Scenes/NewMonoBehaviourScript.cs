using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TunnelGenerator : MonoBehaviour
{
    public Transform pointA; // Primeiro ponto
    public Transform pointB; // Segundo ponto
    public int segments = 50; // Número de divisões
    public float heightScale;
    public float widthScale;
    public float waveAmplitude = 0.5f; // Amplitude da ondulação
    public float waveFrequency = 2f; // Frequência da ondulação

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GenerateTunnelMesh();
    }

    Mesh GenerateTunnelMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[segments * 8];
        int[] triangles = new int[(segments - 1) * 8 * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        // Calcular direção principal e eixo perpendicular
        Vector3 direction = (pointB.position - pointA.position).normalized;
        Vector3 up = Vector3.up; // Eixo "up" base
        if (Vector3.Dot(direction, up) > 0.99f)
        {
            up = Vector3.right; // Ajusta "up" se estiver alinhado com o túnel
        }
        Vector3 side = Vector3.Cross(direction, up).normalized; // Eixo perpendicular

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            // Posição ao longo da curva do túnel
            Vector3 position = Vector3.Lerp(pointA.position, pointB.position, t);

            // Adiciona ondulação perpendicular ao eixo do túnel
            position += side * Mathf.Sin(t * waveFrequency * Mathf.PI * 2) * waveAmplitude;

            // Rotação do anel
            Quaternion rotation = Quaternion.LookRotation(direction, up);

            // Gera vértices do anel atual
            for (int j = 0; j < 8; j++)
            {
                float angle = (j / 8f) * Mathf.PI * 2;

                // Offset elíptico
                Vector3 localOffset = new Vector3(Mathf.Cos(angle) *heightScale , Mathf.Sin(angle) *widthScale , 0);
                vertices[i * 8 + j] = position + rotation * localOffset;

                uv[i * 8 + j] = new Vector2(j / 8f, t);
            }

            // Gera triângulos para os segmentos
            if (i < segments - 1)
            {
                for (int j = 0; j < 8; j++)
                {
                    int nextJ = (j + 1) % 8;
                    int start = i * 8 + j;
                    int next = i * 8 + nextJ;
                    int upper = (i + 1) * 8 + j;
                    int upperNext = (i + 1) * 8 + nextJ;

                    int triangleIndex = (i * 8 + j) * 6;
                    triangles[triangleIndex] = start;
                    triangles[triangleIndex + 1] = upper;
                    triangles[triangleIndex + 2] = next;

                    triangles[triangleIndex + 3] = next;
                    triangles[triangleIndex + 4] = upper;
                    triangles[triangleIndex + 5] = upperNext;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }


}


