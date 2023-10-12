using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject woodPrefab;
    public GameObject stonePrefab;
    public GameObject ironPrefab;
    public GameObject goldPrefab;

    [Header("Inventory")]
    public Dictionary<GameObject, int> inventory = new Dictionary<GameObject, int>();
    public Inv[] frontInv = new Inv[4];

    [Header("UI")]
    public SelectManager selector;
    public UI UserInterface;
    private void Start()
    {
        selector = Camera.main.GetComponent<SelectManager>();
        UserInterface = GameObject.FindGameObjectWithTag("UI").GetComponent<UI>();
    }

    public GameObject selectPrefab(GameObject res)
    {
        if (res.tag == "Wood") return woodPrefab;
        if (res.tag == "Stone") return stonePrefab;
        if (res.tag == "Iron") return ironPrefab;
        if (res.tag == "Gold") return goldPrefab;
        else return null;
    }

    public bool Add(GameObject item, int amount)
    {
        var prefab = selectPrefab(item);
        if (inventory.Count < 4)
        {
            if (!inventory.ContainsKey(prefab))
            {
                inventory.Add(prefab, 0);
            }
            inventory[prefab] = inventory[prefab] + amount;
            SyncInventory();
            return true;
        }
        else
        {
            if (inventory.ContainsKey(prefab))
            {
                inventory[prefab] = inventory[prefab] + amount;
                SyncInventory();
                return true;
            }
            else Debug.Log("Inventory Full"); return false;
        }
    }

    public void SyncInventory()
    {
        int i = 0;
        foreach (var item in inventory)
        {
            frontInv[i].res = item.Key; 
            frontInv[i].amount = item.Value;

            i++;
        }

        if(selector.IsSelected(gameObject))
        {
            UserInterface.DrawInventoryInterface(frontInv);
        }
    }

    [Serializable]
    public struct Inv
    {
        public GameObject res;
        public int amount;
    }
}
