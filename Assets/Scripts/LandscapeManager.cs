using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LandscapeManager : MonoBehaviour {

    public GameObject player;
    public GameObject chunkPrefab;

    public static List<GameObject> chunks = new List<GameObject>();

    private int viewRange = 30;
    public static int seed;

	void Start () {
        seed = (int)Network.time * 10;
    }
	
	void Update () {
        for (float x = player.transform.position.x - viewRange; x < player.transform.position.x + viewRange; x += ChunkManager.ChunkWidth)
        {
            for (float z = player.transform.position.z - viewRange; z < player.transform.position.z + viewRange; z += ChunkManager.ChunkWidth)
            {
                Vector3 pos = new Vector3(x, 0, z);
                pos.x = Mathf.Floor(pos.x / ChunkManager.ChunkWidth) * ChunkManager.ChunkWidth;
                pos.z = Mathf.Floor(pos.z / ChunkManager.ChunkWidth) * ChunkManager.ChunkWidth;

                GameObject chunk = FindChunk(pos);
                if (chunk == null)
                {
                    chunk = Instantiate(chunkPrefab, pos, Quaternion.identity) as GameObject;
                    chunk.name = "Chunk " + chunks.Count;
                    chunks.Add(chunk);
                }
            }
        }

        // Quit the app
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKey(KeyCode.Q))
        {
            Application.Quit();
        }
    }

    public static int getSeed()
    {
        return seed;
    }

    public static GameObject FindChunk(Vector3 pos)
    {
        GameObject chunk = null;
        for (int i = 0; i < chunks.Count; i++)
        {
            Vector3 cpos = chunks[i].transform.position;
            if (!(pos.x < cpos.x) && !(pos.z < cpos.z) && !(pos.x >= cpos.x + ChunkManager.ChunkWidth) && !(pos.z >= cpos.z + ChunkManager.ChunkWidth))
            {
                chunk = chunks[i];
                i = chunks.Count;
            }
        }
        return chunk;
    }
}
