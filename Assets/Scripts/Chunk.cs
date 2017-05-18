using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chunk : MonoBehaviour {

    public const int ChunkWidth = 20;
    public const int ChunkHeight = 20;

    private byte[,,] brickType = new byte[ChunkWidth, ChunkHeight, ChunkWidth];
    public byte[,,] BrickType
    {
        get { return brickType; }
    }

    private int heightScale = 20; // used by perlin noise for world generation
    private float detailScale = 30.0f; // used by perlin noise for world generation
    private Mesh visualMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start () {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        assignBrickTypes();
        StartCoroutine(CreateVisualMesh());

        // Reskin neighbor chunk meshes for boundary cases
        GameObject leftChunk = Landscape.FindChunk(transform.position + new Vector3(-1, 0, 0));
        if (leftChunk != null)
        {
            StartCoroutine(leftChunk.GetComponent<Chunk>().CreateVisualMesh());
        }
        GameObject rightChunk = Landscape.FindChunk(transform.position + new Vector3(ChunkWidth, 0, 0));
        if (rightChunk != null)
        {
            StartCoroutine(rightChunk.GetComponent<Chunk>().CreateVisualMesh());
        }
        GameObject frontChunk = Landscape.FindChunk(transform.position + new Vector3(0, 0, -1));
        if (frontChunk != null)
        {
            StartCoroutine(frontChunk.GetComponent<Chunk>().CreateVisualMesh());
        }
        GameObject backChunk = Landscape.FindChunk(transform.position + new Vector3(0, 0, ChunkWidth));
        if (backChunk != null)
        {
            StartCoroutine(backChunk.GetComponent<Chunk>().CreateVisualMesh());
        }
    }

    private void assignBrickTypes()
    {
        int seed = Landscape.getSeed();
        for (int x = 0; x < ChunkWidth; x++)
        {
            for (int z = 0; z < ChunkWidth; z++)
            {
                int height = (int)(Mathf.PerlinNoise(((int)transform.position.x + x + seed) / detailScale, ((int)transform.position.z + z + seed) / detailScale) * heightScale);
                if (height < 0)
                {
                    height = 0; // Lowest possible height for the world
                }
                for (int y = 0; y < ChunkHeight; y++)
                {
                    if (y < height)
                    {
                        brickType[x, y, z] = 0; // bricks 'in the ground'. Not visible
                    }
                    else if (y == height)
                    {
                        brickType[x, y, z] = 1; // visible block
                    }
                    else
                    {
                        brickType[x, y, z] = 2; // brick doesn't 'exist'
                    }
                }
            }
        }
    }

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
                    if (brickType[x, y, z] == 2) continue;

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
            GameObject chunk = Landscape.FindChunk(worldPos);
            if (chunk == null)
            {
                return 1;
            }

            return chunk.GetComponent<Chunk>().GetByte(worldPos);
        }
        return brickType[x, y, z];
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

        verts.Add(bottom_left);
        verts.Add(bottom_left + up);
        verts.Add(bottom_left + up + right);
        verts.Add(bottom_left + right);

        uvs.Add(new Vector2(0.0f, 0.0f));
        uvs.Add(new Vector2(0.0f, 1.0f));
        uvs.Add(new Vector2(1.0f, 1.0f));
        uvs.Add(new Vector2(1.0f, 0.0f));

        tris.Add(index);
        tris.Add(index + 1);
        tris.Add(index + 2);
        tris.Add(index);
        tris.Add(index + 2);
        tris.Add(index + 3);
    }
}
