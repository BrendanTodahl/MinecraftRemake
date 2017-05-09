using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour {

    public const int ChunkWidth = 20;
    public const int ChunkHeight = 20;

    private byte[,,] map = new byte[ChunkWidth, ChunkHeight, ChunkWidth]; // data containing the types of bricks within a chunk
    public byte[,,] Map
    {
        get { return map; }
    }

    private int heightScale = 20; // used by perlin noise for world generation
    private float detailScale = 25.0f; // used by perlin noise for world generation
    private Mesh visualMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start () {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        createMapFromScratch();
        StartCoroutine(CreateVisualMesh());

        // Reskin neighbor chunk meshes for boundary cases
        GameObject leftChunk = LandscapeManager.FindChunk(transform.position + new Vector3(-1, 0, 0));
        if (leftChunk != null)
        {
            StartCoroutine(leftChunk.GetComponent<ChunkManager>().CreateVisualMesh());
        }
        GameObject rightChunk = LandscapeManager.FindChunk(transform.position + new Vector3(ChunkWidth, 0, 0));
        if (rightChunk != null)
        {
            StartCoroutine(rightChunk.GetComponent<ChunkManager>().CreateVisualMesh());
        }
        GameObject frontChunk = LandscapeManager.FindChunk(transform.position + new Vector3(0, 0, -1));
        if (frontChunk != null)
        {
            StartCoroutine(frontChunk.GetComponent<ChunkManager>().CreateVisualMesh());
        }
        GameObject backChunk = LandscapeManager.FindChunk(transform.position + new Vector3(0, 0, ChunkWidth));
        if (backChunk != null)
        {
            StartCoroutine(backChunk.GetComponent<ChunkManager>().CreateVisualMesh());
        }
    }

    // Assign types of bricks to every position in the chunk's map
    private void createMapFromScratch()
    {
        int seed = LandscapeManager.getSeed();
        for (int x = 0; x < ChunkWidth; x++)
        {
            for (int z = 0; z < ChunkWidth; z++)
            {
                int height = (int)(Mathf.PerlinNoise(((int)transform.position.x + x + seed) / detailScale, ((int)transform.position.z + z + seed) / detailScale) * heightScale);
                for (int y = 0; y < ChunkHeight; y++)
                {
                    if (y < height)
                    {
                        map[x, y, z] = 0; // space below visible block. Not visible
                    }
                    else if (y == height)
                    {
                        map[x, y, z] = 1; // visible block
                    }
                    else
                    {
                        map[x, y, z] = 2; // space above visible blocks. Not visible
                    }
                }
            }
        }
    }

    // Coroutine to create the mesh of the chunk
    public IEnumerator CreateVisualMesh()
    {
        visualMesh = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        for (int x = 0; x < ChunkWidth; x++)
        {
            for (int y = 0; y < ChunkHeight; y++)
            {
                for (int z = 0; z < ChunkWidth; z++)
                {
                    if (map[x, y, z] == 2) continue;

                    if (isTransparant(x + 1, y, z)) // right face
                    {
                        buildFace(verts, uvs, tris, new Vector3(x + 1, y, z), Vector3.up, Vector3.forward);
                    }
                    if (isTransparant(x - 1, y, z)) // left face
                    {
                        buildFace(verts, uvs, tris, new Vector3(x, y, z + 1), Vector3.up, Vector3.back);
                    }
                    if (isTransparant(x, y + 1, z)) // top face
                    {
                        buildFace(verts, uvs, tris, new Vector3(x, y + 1, z), Vector3.forward, Vector3.right);
                    }
                    if (isTransparant(x, y - 1, z)) // bottom face
                    {
                        buildFace(verts, uvs, tris, new Vector3(x, y, z + 1), Vector3.back, Vector3.right);
                    }
                    if (isTransparant(x, y, z + 1)) // back face
                    {
                        buildFace(verts, uvs, tris, new Vector3(x + 1, y, z + 1), Vector3.up, Vector3.left);
                    }
                    if (isTransparant(x, y, z - 1)) // front face
                    {
                        buildFace(verts, uvs, tris, new Vector3(x, y, z), Vector3.up, Vector3.right);
                    }
                }
            }
        }

        visualMesh.vertices = verts.ToArray();
        visualMesh.uv = uvs.ToArray();
        visualMesh.triangles = tris.ToArray();
        visualMesh.RecalculateBounds();
        visualMesh.RecalculateNormals();

        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = null;
        meshFilter.sharedMesh = visualMesh;

        meshCollider = GetComponent<MeshCollider>();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = visualMesh;

        yield return 0;
    }

    private bool isTransparant(int x, int y, int z)
    {
        byte brick = GetByte(x, y, z);
        switch (brick)
        {
            case 2:
                return true;
            default:
                return false;
        }
    }

    public byte GetByte(int x, int y, int z)
    {
        if (y < 0)
        {
            return 0;
        }
        else if (y >= ChunkHeight)
        {
            return 2;
        }

        if ((x < 0) || (z < 0) || (x >= ChunkWidth) || (z >= ChunkWidth))
        {

            Vector3 worldPos = new Vector3(x, y, z) + transform.position;
            GameObject chunk = LandscapeManager.FindChunk(worldPos);
            if (chunk == null)
            {
                return 1;
            }

            return chunk.GetComponent<ChunkManager>().GetByte(worldPos);
        }
        return map[x, y, z];
    }

    public byte GetByte(Vector3 worldPos)
    {
        worldPos -= transform.position;
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);
        int z = Mathf.FloorToInt(worldPos.z);
        return GetByte(x, y, z);
    }

    // Helper method for createVisualMesh(). Adds vertices, uvs, and triangles to Lists
    private void buildFace(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector3 bottom_left, Vector3 up, Vector3 right)
    {
        int index = verts.Count;

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
