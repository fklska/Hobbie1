using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.TerrainUtils;
using UnityEngine.Tilemaps;

public class BuildManager : MonoBehaviour
{
    private void FixedUpdate()
    {
        Builder();
        BuildRoad();
    }

    public GameObject prefab;
    public GameObject flyingObject;
    public void Builder()
    {
        if(flyingObject == null)
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                flyingObject = Instantiate(prefab);
            }
        }

        if (flyingObject != null)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(worldPos.x);
            int y = Mathf.FloorToInt(worldPos.y);

            flyingObject.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0);

            if (Input.GetKeyDown(KeyCode.B))
            {
                flyingObject = null;
            }
        }
    }

    public Tilemap tilemap;
    public Tilemap ground;
    public Tile roadTile;
    public GameObject roadPrefab;
    public void BuildRoad()
    {
        if (flyingObject == null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                flyingObject = Instantiate(roadPrefab);
            }
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);

        var coor = new Vector3Int(x / 2, y / 2, 0);
        Debug.Log(coor);

        ground.SetColor(coor, Color.red);
        //ground.SetColor(coor, Color.Lerp(new Color(1, 1, 0, 0.2f), new Color(1, 1, 1, 1), Time.deltaTime));
        if (Input.GetMouseButton(0))
        {
            tilemap.SetTile(coor, roadTile);
        }
        if (Input.GetMouseButtonDown(1))
        {
            flyingObject = null;
        }
    }
}
