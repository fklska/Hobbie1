using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class SelectManager : MonoBehaviour
{
    [Header("React")]
    public Rect rect;
    public Vector2 startPos;
    public Vector2 endPos;
    public Vector2 size;
    public bool draw;

    [Header("Objects")]
    public List<GameObject> objs;
    public List<GameObject> selectedObjects;

    private void Awake()
    {
        objs = GameObject.FindGameObjectsWithTag("Player").ToList<GameObject>();
        objs.AddRange(GameObject.FindGameObjectsWithTag("Skeleton").ToList<GameObject>());
    }

    public bool IsSelected(GameObject obj)
    {
        if (selectedObjects.Contains(obj))
            return true;
        return false;
    }

    public void Clear()
    {
        foreach (GameObject obj in selectedObjects)
        {
            obj.transform.Find("Circle").GetComponent<SpriteRenderer>().enabled = false;
        }
        selectedObjects.Clear();
    }

    public GameObject mainHero;
    private void OnGUI()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Clear();
            startPos = Input.mousePosition;
            draw = true;
        }

        if (Input.GetMouseButtonUp(1))
        {
            draw= false;
        }

        if (draw)
        {
            endPos = Input.mousePosition;
            size = new Vector2(endPos.x - startPos.x, endPos.y - startPos.y);
            rect = new Rect(
                Mathf.Min(endPos.x, startPos.x),
                Screen.height - Mathf.Max(endPos.y, startPos.y),
                Mathf.Max(endPos.x, startPos.x) - Mathf.Min(endPos.x, startPos.x),
                Mathf.Max(endPos.y, startPos.y) - Mathf.Min(endPos.y, startPos.y)
                );
            GUI.Box(rect, "TEST");
            Clear();
            foreach (GameObject obj in objs)
            {
                Vector2 tmp = new Vector2(Camera.main.WorldToScreenPoint(obj.transform.position).x, Screen.height - Camera.main.WorldToScreenPoint(obj.transform.position).y);

                if (rect.Contains(tmp))
                {
                    if (!selectedObjects.Contains(obj))
                    {
                        selectedObjects.Add(obj);
                    }
                }
            }

        }

        if(Input.GetKeyDown("w") || Input.GetKeyDown("a") || Input.GetKeyDown("s") || Input.GetKeyDown("d"))
        {
            Clear();
            selectedObjects.Add(mainHero);
        }

        foreach (GameObject obj in selectedObjects)
        {
            obj.transform.Find("Circle").GetComponent<SpriteRenderer>().enabled = true;
        }
    }
}
