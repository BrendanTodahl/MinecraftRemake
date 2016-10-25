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
        for (float x = player.transform.position.x - viewRange; x < player.transform.position.x + viewRange; x += ChunkManager.chunkWidth)
        {
            for (float z = player.transform.position.z - viewRange; z < player.transform.position.z + viewRange; z += ChunkManager.chunkWidth)
            {
                Vector3 pos = new Vector3(x, 0, z);
                pos.x = Mathf.Floor(pos.x / (float)ChunkManager.chunkWidth) * ChunkManager.chunkWidth;
                pos.z = Mathf.Floor(pos.z / (float)ChunkManager.chunkWidth) * ChunkManager.chunkWidth;

                GameObject chunk = FindChunk(pos);
                if (chunk == null)
                {
                    chunk = Instantiate(chunkPrefab, pos, Quaternion.identity) as GameObject;
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
            if (!(pos.x < cpos.x) && !(pos.z < cpos.z) && !(pos.x >= cpos.x + ChunkManager.chunkWidth) && !(pos.z >= cpos.z + ChunkManager.chunkWidth))
            {
                chunk = chunks[i];
                i = chunks.Count;
            }
        }
        return chunk;
    }
}
