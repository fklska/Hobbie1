using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BuildManager : MonoBehaviour
{
    [Header("MainMap")]
    public Dictionary<Vector2Int, GameObject> map;
    private void Start()
    {
        map = GameObject.FindGameObjectWithTag("MapGenerator").GetComponent<MapGenerator>().map;
    }

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

    [Header("Road")]
    public Color defaultcolor = Color.white;
    public Color currentcolor;
    public Color selectedcolor = new Color(1, 1, 0, 0.2f);
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
        var coor = new Vector2Int(x, y);

        // map[coor].GetComponent<SpriteRenderer>().color = selectedcolor;
        // map[coor].GetComponent<SpriteRenderer>().color = Color.Lerp(selectedcolor, defaultcolor, Time.deltaTime);

        //ground.SetColor(coor, Color.Lerp(new Color(1, 1, 0, 0.2f), new Color(1, 1, 1, 1), Time.deltaTime));
        if (Input.GetMouseButtonDown(0))
        {
            Instantiate(roadPrefab, new Vector3Int(x, y, 0), Quaternion.identity, gameObject.transform);
        }
        if (Input.GetMouseButtonDown(1))
        {
            Destroy(flyingObject);
            flyingObject = null;
        }
    }
}
