using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [Header("HealthBar")]
    public Transform healthbar;
    public Slider slider;
    public Text text;

    [Header("Stats")]
    public Transform stats;
    public Text strenght;
    public Text agility;
    public Text bonus;
    public Image avatar;

    [Header("Inventory")]
    public Transform inventory;
    public List<Transform> slots;

    private void Start()
    {
        healthbar = gameObject.transform.GetChild(0);
        slider = healthbar.GetComponentInChildren<Slider>();
        text = healthbar.GetComponentInChildren<Text>();

        stats = gameObject.transform.GetChild(1);
        strenght = stats.transform.GetChild(0).gameObject.GetComponent<Text>();
        agility = stats.transform.GetChild(1).gameObject.GetComponent<Text>();
        bonus = stats.transform.GetChild(2).gameObject.GetComponent<Text>();
        avatar = stats.transform.GetChild(3).gameObject.GetComponent<Image>();

        inventory = gameObject.transform.GetChild(2);
        for (int i = 0; i < inventory.childCount; i++)
        {
            slots.Add(inventory.GetChild(i));
        }
    }


    private void FixedUpdate()
    {
        GetSelectedInfo();
    }

    public List<GameObject> selected;
    public Characteristic ch;
    public Inventory inv;
    public void GetSelectedInfo()
    {
        selected = Camera.main.GetComponent<SelectManager>().selectedObjects;
        if (selected.Count != 0)
        {
            ch = selected[0].GetComponent<Characteristic>();
            slider.value = ch.HEALTH / 100f;
            text.text = Convert.ToString(ch.HEALTH);

            strenght.text = Convert.ToString(ch.STRENGHT);
            agility.text = Convert.ToString(ch.AGILITY);
            bonus.text = Convert.ToString(ch.RoadBonus);
            avatar.sprite = selected[0].GetComponent<SpriteRenderer>().sprite;

            bool is_inv = selected[0].TryGetComponent<Inventory>(out inv);

            if (is_inv)
            {
                //DrawInventoryInterface(inv.frontInv);
            }
        }
    }

    public void DrawInventoryInterface(Inventory.Inv[] inventory)
    {
        int count = 0;
        foreach (var item in slots)
        {
            if (inventory[count].res != null)
            {
                item.gameObject.SetActive(true);
                item.GetComponentInChildren<Text>().text = inventory[count].amount.ToString();
                slots[count].GetComponentInChildren<Image>().sprite = inventory[count].res.GetComponent<SpriteRenderer>().sprite;

                count++;
            }

            else
            {
                item.gameObject.SetActive(false);

                count++;
            }
        }
    }
}
