using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    private void FixedUpdate()
    {
        Builder();
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

            flyingObject.transform.position = new Vector3(x, y, 0);

            if (Input.GetKeyDown(KeyCode.B))
            {
                Destroy(flyingObject);
                flyingObject = null;
            }
        }
    }
}
