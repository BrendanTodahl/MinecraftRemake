﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Landscape : MonoBehaviour {

    public GameObject player;
    public GameObject chunkPrefab;

    public static List<GameObject> chunks = new List<GameObject>();

    private int viewRange = 100;
    public static int seed;

	void Start () {
        seed = (int)Network.time * 10;

        for (float x = player.transform.position.x - viewRange; x < player.transform.position.x + viewRange; x += Chunk.ChunkWidth)
        {
            for (float z = player.transform.position.z - viewRange; z < player.transform.position.z + viewRange; z += Chunk.ChunkWidth)
            {
                Vector3 pos = new Vector3(x, 0, z);
                pos.x = Mathf.Floor(pos.x / Chunk.ChunkWidth) * Chunk.ChunkWidth;
                pos.z = Mathf.Floor(pos.z / Chunk.ChunkWidth) * Chunk.ChunkWidth;

                GameObject chunk = FindChunk(pos);
                if (chunk == null)
                {
                    chunk = Instantiate(chunkPrefab, pos, Quaternion.identity) as GameObject;
                    chunk.name = "Chunk " + chunks.Count;
                    chunks.Add(chunk);
                }
            }
        }
    }
	
	void Update () {
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
            if (!(pos.x < cpos.x) && !(pos.z < cpos.z) && !(pos.x >= cpos.x + Chunk.ChunkWidth) && !(pos.z >= cpos.z + Chunk.ChunkWidth))
            {
                chunk = chunks[i];
                i = chunks.Count;
            }
        }
        return chunk;
    }
}
