using NavMeshPlus.Components;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


public class MapGenerator : MonoBehaviour
{
    public NavMeshSurface navMesh;
    private void Start()
    {
        //tilemap = GetComponent<Tilemap>();
        //GenerateGround();
        navMesh = GetComponentInParent<NavMeshSurface>();
    }

    [Header("Ground")]
    public Vector2Int size;
    public int Chunks;
    public GameObject[,] map = new GameObject[10,10];
    public GameObject GroundPrefab;
    public void GenerateGround()
    {
        int chunkoffset = 0;
        for (int i = 0; i < Chunks; i++)
        {
            for (int x = -size.x; x < size.x; x++)
            {
                for (int y = -size.y; y < size.y; y++)
                {
                    GameObject tile = Instantiate(GroundPrefab, new Vector3Int(x + chunkoffset, y, 0), Quaternion.identity, gameObject.transform);
                    //tilemap.SetTile(new Vector3Int(x + chunkoffset, y, 0), tile);
                    map[x, y] = tile;
                }
            }
            chunkoffset += 20;
        }
        Debug.Log(map[1, 2].transform.position);
    }

    [Header("PerlinNoise")]
    [Range(0f, 25f)]
    public float scale;
    public int seed;
    public float maxHeight, minHeight;
    public float[,] noiseMap;

    [Header("Prefabs")]
    public GameObject parent;
    public ResourseType[] resTypes;
    public void PerlinNoiseGeneration()
    {
        ClearMap();
        seed = Random.Range(-100000, 100000);
        maxHeight = 0;
        minHeight = 1f;
        float height;
        for (int x = -size.x * 2; x < size.x * 2; x++)
        {
            for (int y = -size.y * 2; y < size.y * 2; y++)
            {
                float sampleX = x / scale + seed;
                float sampleY = y / scale + seed;
                height = (Mathf.PerlinNoise(sampleX, sampleY));
                if (height > maxHeight) maxHeight = height;
                if (height < minHeight) minHeight = height;
                //noiseMap[x, y] = height;
                for (int i = 0; i < resTypes.Length; i++)
                {
                    if (height < resTypes[i].height)
                    {
                        Instantiate(resTypes[i].prefab, new Vector3(x, y, 0), Quaternion.identity, parent.transform);
                        break;
                    }
                }
            }
        }
        navMesh.BuildNavMesh();
    }

    public void ClearMap()
    {
        var objects = gameObject.GetComponentsInChildren<Transform>();
        foreach (var obj in objects)
        {
            if (obj.gameObject.layer == 6)
            {
                DestroyImmediate(obj.gameObject);
            }
        }
    }

    [System.Serializable]
    public struct ResourseType
    {
        public string name;
        public GameObject prefab;
        public float height;
    }
}
