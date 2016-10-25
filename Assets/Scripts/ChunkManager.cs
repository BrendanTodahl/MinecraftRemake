using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour {
    // used for buildFace debugging
    //Instantiate(cube, new Vector3(transform.position.x + x, transform.position.y + y, transform.position.z + z), Quaternion.identity);

    [SerializeField]
    GameObject cube;
    public static int chunkWidth = 20, chunkHeight = 20;

    private bool isDone = false; // Flag to indicate that the chunk has been generated in the world
    public bool IsDone
    {
        get { return isDone; }
    }

    private byte[,,] map = new byte[chunkWidth, chunkHeight, chunkWidth]; // data containing the types of bricks within a chunk
    public byte[,,] Map
    {
        get { return map; }
    }

    private int heightScale = 20; // used by perlin noise for world generation
    private float detailScale = 25.0f; // used by perlin noise for world generation
    private Mesh visualMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private MeshRenderer meshRenderer;

    private Object semaphore = new Object();

    void Start () {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        createMapFromScratch();
        StartCoroutine(CreateVisualMesh());
        isDone = true;
    }

    // Assign types of bricks to every position in the chunk's map
    private void createMapFromScratch()
    {
        int seed = LandscapeManager.getSeed();
        for (int x = 0; x < chunkWidth; x++)
        {
            for (int z = 0; z < chunkWidth; z++)
            {
                int height = (int)(Mathf.PerlinNoise(((int)transform.position.x + x + seed) / detailScale, ((int)transform.position.z + z + seed) / detailScale) * heightScale);
                for (int y = 0; y < chunkHeight; y++)
                {
                    if (y < height)
                    {
                        map[x, y, z] = 0; // hidden block
                    }
                    else if (y == height)
                    {
                        map[x, y, z] = 1; // visible block
                    }
                    else
                    {
                        map[x, y, z] = 2; // space above visible blocks 
                    }
                }
            }
        }
    }

    // Coroutine to create the mesh of the chunk
    public virtual IEnumerator CreateVisualMesh()
    {
        visualMesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {
                    GameObject chunk = null;
                    if (x == 0)
                    {
                        chunk = LandscapeManager.FindChunk(new Vector3(transform.position.x - 20, transform.position.y, transform.position.z));
                        if (chunk != null && chunk.GetComponent<ChunkManager>().IsDone)
                        {
                            byte[,,] adjacentMap = chunk.GetComponent<ChunkManager>().Map;
                            determineIfBuildableFaceLR(verts, uvs, tris, x, y, z, adjacentMap);
                        }
                    }
                    else if (x == chunkWidth - 1)
                    {
                        chunk = LandscapeManager.FindChunk(new Vector3(transform.position.x + 20, transform.position.y, transform.position.z));
                        if (chunk != null && chunk.GetComponent<ChunkManager>().IsDone)
                        {
                            byte[,,] adjacentMap = chunk.GetComponent<ChunkManager>().Map;
                            determineIfBuildableFaceLR(verts, uvs, tris, x, y, z, adjacentMap);
                        }
                    }
                    if (z == 0)
                    {
                        chunk = LandscapeManager.FindChunk(new Vector3(transform.position.x, transform.position.y, transform.position.z - 20));
                        if (chunk != null && chunk.GetComponent<ChunkManager>().IsDone)
                        {
                            byte[,,] adjacentMap = chunk.GetComponent<ChunkManager>().Map;
                            determineIfBuildableFace(verts, uvs, tris, x, y, z, adjacentMap);
                        }
                    }
                    else if (z == chunkWidth - 1)
                    {
                        chunk = LandscapeManager.FindChunk(new Vector3(transform.position.x, transform.position.y, transform.position.z + 20));
                        if (chunk != null && chunk.GetComponent<ChunkManager>().IsDone)
                        {
                            byte[,,] adjacentMap = chunk.GetComponent<ChunkManager>().Map;
                            determineIfBuildableFace(verts, uvs, tris, x, y, z, adjacentMap);
                        }
                    }
                    if (map[x, y, z] != 2) // if not above visible layer, continue
                    {
                        determineIfBuildableFaceLR(verts, uvs, tris, x, y, z, null);
                        determineIfBuildableFace(verts, uvs, tris, x, y, z, null);
                    }
                }
            }
        }

        visualMesh.vertices = verts.ToArray();
        visualMesh.uv = uvs.ToArray();
        visualMesh.triangles = tris.ToArray();
        visualMesh.RecalculateBounds();
        visualMesh.RecalculateNormals();

        meshFilter.mesh = visualMesh;

        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = visualMesh;

        yield return 0;
    }

    // Helper method for CreateVisualMesh(). Determines whether to build left and right faces
    private void determineIfBuildableFaceLR(List<Vector3> verts, List<Vector2> uvs, List<int> tris, int x, int y, int z, byte[,,] adjacentMap)
    {
        // Left face
        if (x - 1 >= 0 && map[x - 1, y, z] == 2 && adjacentMap == null) // normal left face
        {
            buildFace(verts, uvs, tris, new Vector3(x, y, z + 1), Vector3.up, Vector3.back);
        }
        else if (adjacentMap != null && x == 0 && map[0, y, z] == 2 && (adjacentMap[chunkWidth - 1, y, z] == 0 || adjacentMap[chunkWidth - 1, y, z] == 1)) // left face as player walks along -x-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x, y, z), Vector3.up, Vector3.forward);
        }
        else if (adjacentMap != null && x == 0 && (map[0, y, z] == 0 || map[x, y, z] == 1) && adjacentMap[chunkWidth - 1, y, z] == 2) // left face as player walks along -x-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x, y, z + 1), Vector3.up, Vector3.back);
        }
        // Right face
        if (x + 1 < chunkWidth && map[x + 1, y, z] == 2 && adjacentMap == null) // normal right face
        {
            buildFace(verts, uvs, tris, new Vector3(x + 1, y, z), Vector3.up, Vector3.forward);
        }
        else if (adjacentMap != null && x == chunkWidth - 1 && (map[chunkWidth - 1, y, z] == 0 || map[chunkWidth - 1, y, z] == 1) && adjacentMap[0, y, z] == 2) // right face as player walks along +x-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x + 1, y, z), Vector3.up, Vector3.forward);
        }
        else if (adjacentMap != null && x == chunkWidth - 1 && map[chunkWidth - 1, y, z] == 2 && (adjacentMap[0, y, z] == 0 || adjacentMap[0, y, z] == 1)) // right face as player walks along +x-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x + 1, y, z + 1), Vector3.up, Vector3.back);
        }
    }

    // Helper method for CreateVisualMesh(). Determines whether to build front, back, and top faces
    private void determineIfBuildableFace(List<Vector3> verts, List<Vector2> uvs, List<int> tris, int x, int y, int z, byte[,,] adjacentMap)
    {
        // Front face
        if (z - 1 >= 0 && map[x, y, z - 1] == 2 && adjacentMap == null) // normal front face
        {
            buildFace(verts, uvs, tris, new Vector3(x, y, z), Vector3.up, Vector3.right);
        }
        else if (adjacentMap != null && z == chunkWidth - 1 && map[x, y, chunkWidth - 1] == 2 && (adjacentMap[x, y, 0] == 0 || adjacentMap[x, y, 0] == 1)) // front face as player walks along -z-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x, y, z + 1), Vector3.up, Vector3.right);
        }
        else if (adjacentMap != null && z == chunkWidth - 1 && (map[x, y, chunkWidth - 1] == 0 || map[x, y, chunkWidth - 1] == 1) && adjacentMap[x, y, 0] == 2) // back face as player walks along -z-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x + 1, y, z + 1), Vector3.up, Vector3.left);
        }
        // Back face
        if (z + 1 < chunkWidth && map[x, y, z + 1] == 2 && adjacentMap == null) // normal back face
        {
            buildFace(verts, uvs, tris, new Vector3(x + 1, y, z + 1), Vector3.up, Vector3.left);
        }
        else if (adjacentMap != null && z == 0 && map[x, y, 0] == 2 && (adjacentMap[x, y, chunkWidth - 1] == 0 || adjacentMap[x, y, chunkWidth - 1] == 1)) // back face as player walks along +z-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x + 1, y, z), Vector3.up, Vector3.left);
        }
        else if (adjacentMap != null && z == 0 && (map[x, y, 0] == 0 || map[x, y, 0] == 1) && adjacentMap[x, y, chunkWidth - 1] == 2) // front face as player walks along +z-axis
        {
            buildFace(verts, uvs, tris, new Vector3(x, y, z), Vector3.up, Vector3.right);
        }
        // Top face
        if (y + 1 < chunkHeight && map[x, y + 1, z] == 2 && adjacentMap == null) // normal top face
        {
            buildFace(verts, uvs, tris, new Vector3(x, y + 1, z), Vector3.forward, Vector3.right);
        }
        // bottom face
        //if (y - 1 >= 0 && map[x, y - 1, z] == 2)
        //{
        //    buildFace(verts, uvs, tris, new Vector3(x, y, z + 1), Vector3.back, Vector3.right);
        //}
    }

    // Helper method for createVisualMesh(). Adds vertices, uvs, and triangles to Lists
    private void buildFace(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 bottom_left, Vector3 up, Vector3 right)
    {
        int index = verts.Count; // Verts current count

        // add vertices
        verts.Add(bottom_left);
        verts.Add(bottom_left + up);
        verts.Add(bottom_left + up + right);
        verts.Add(bottom_left + right);

        // add uvs
        uvs.Add(new Vector2(0.0f, 0.0f));
        uvs.Add(new Vector2(0.0f, 1.0f));
        uvs.Add(new Vector2(1.0f, 1.0f));
        uvs.Add(new Vector2(1.0f, 0.0f));

        // add tris
        tris.Add(index);
        tris.Add(index + 1);
        tris.Add(index + 2);
        tris.Add(index);
        tris.Add(index + 2);
        tris.Add(index + 3);
    }
}
